using BydClient.Config;
using BydClient.Crypto;
using BydClient.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Linq;
using BydClient.Transport;

namespace BydClient.Api;

    /// <summary>
    /// Handles token-authenticated JSON requests to BYD API endpoints.
    /// Faithful port of the PHP TokenJson class with identical encryption, signing,
    /// and error handling logic.
    /// </summary>
    public static class TokenJson
    {
        private static readonly IReadOnlyList<int> EndpointNotSupportedCodes = new List<int> { 1004, 1005 };

        public static async Task<T?> PostTokenJsonAsync<T>(
            string endpoint,
            BydConfig config,
            Session session,
            ITransport transport,
            Dictionary<string, string?> inner,
            long? nowMs = null,
            string? vin = null,
            string? userType = null,
            IEnumerable<int>? extraErrorCodes = null)
        {
            var decoded = await PostTokenJsonAsync(
                endpoint,
                config,
                session,
                transport,
                inner,
                nowMs,
                vin,
                userType,
                extraErrorCodes);

            if(decoded == null)
                return default;

            object value = decoded.TryGetValue("items", out var items) ? items : decoded;
            return JsonSerializer.Deserialize<T>(
                JsonSerializer.Serialize(value),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        /// <summary>
        /// Post a token-authenticated JSON request to a BYD endpoint.
        /// </summary>
        /// <param name="endpoint">API endpoint path</param>
        /// <param name="config">BYD configuration</param>
        /// <param name="session">Current session containing keys</param>
        /// <param name="transport">HTTP transport interface</param>
        /// <param name="inner">Inner payload to encrypt and send</param>
        /// <param name="nowMs">Optional timestamp in milliseconds</param>
        /// <param name="vin">Optional VIN (reserved for future use)</param>
        /// <param name="userType">Optional user type</param>
        /// <param name="extraErrorCodes">Additional error codes to handle</param>
        /// <returns>Decrypted inner response as a dictionary, or null if no respondData</returns>
        public static async Task<Dictionary<string, object>?> PostTokenJsonAsync(
            string endpoint,
            BydConfig config,
            Session session,
            ITransport transport,
            Dictionary<string, string?> inner,
            long? nowMs = null,
            string? vin = null,
            string? userType = null,
            IEnumerable<int>? extraErrorCodes = null)
        {
            if(nowMs == null)
            {
                nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }

            string reqTimestamp = nowMs.Value.ToString();

            // Get keys from session
            string contentKey = session.ContentKey();
            string signKey = session.SignKey();

            // Encrypt inner payload
            string encryData = Aes.AesEncryptHex(
                JsonSerializer.Serialize(inner, new JsonSerializerOptions
                {
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                    WriteIndented = false
                }),
                contentKey);

            // Build sign fields (array_merge equivalent: inner + override/add metadata)
            var signFields = new Dictionary<string, string?>(inner)
            {
                ["countryCode"] = config.CountryCode,
                ["identifier"] = session.UserId,
                ["imeiMD5"] = config.Device?.ImeiMd5,
                ["language"] = config.Language,
                ["reqTimestamp"] = reqTimestamp
            };

            string signString = Signing.BuildSignString(signFields, signKey);
            string sign = Hashing.Sha1Mixed(signString);

            // Build outer payload
            var outer = new Dictionary<string, object>
            {
                ["countryCode"] = config.CountryCode,
                ["encryData"] = encryData,
                ["identifier"] = session.UserId,
                ["imeiMD5"] = config.Device is not null ? config.Device.ImeiMd5 : string.Empty,
                ["language"] = config.Language,
                ["reqTimestamp"] = reqTimestamp,
                ["sign"] = sign,
                ["ostype"] = config.Device is not null ? config.Device.OsType : string.Empty,
                ["imei"] = config.Device is not null ? config.Device.Imei : string.Empty,
                ["mac"] = config.Device is not null ? config.Device.Mac : string.Empty,
                ["model"] = config.Device is not null ? config.Device.Model : string.Empty,
                ["sdk"] = config.Device is not null ? config.Device.Sdk : string.Empty,
                ["mod"] = config.Device is not null ? config.Device.Mod : string.Empty,
                ["serviceTime"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString()
            };

            if(userType != null)
            {
                outer["userType"] = userType;
            }

            // Add checkcode at the end
            outer["checkcode"] = Hashing.ComputeCheckcode(outer);

            // Send request
            var decoded = await transport.PostSecureAsync(endpoint, outer);

            // Check outer response code before attempting decryption
            string outerCode = GetFieldValue(decoded, "code", "0");
            if(outerCode != "0")
            {
                int errorCode = int.Parse(outerCode);
                string errorMessage = GetErrorMessage(decoded, $"API error {outerCode}");

                if(errorCode == 1017)
                    throw new BydSessionExpiredException(errorMessage, errorCode, endpoint);

                if(EndpointNotSupportedCodes.Contains(errorCode))
                    throw new BydVehicleNotSupportedException(errorMessage, errorCode, endpoint);

                if(extraErrorCodes?.Contains(errorCode) == true)
                    throw new BydApiException(errorMessage, errorCode, endpoint);

                throw new BydApiException(errorMessage, errorCode, endpoint);
            }

            // Extract and decrypt inner response payload
            if(!decoded.TryGetValue("respondData", out var payloadObj) || payloadObj == null)
            {
                return null;
            }

            string payload = payloadObj.ToString() ?? "";
            string plaintext = Aes.AesDecryptUtf8(payload, contentKey);

            // json_decode($plaintext, true) equivalent: returns null on invalid JSON
            Dictionary<string, object>? innerResponse = null;
            try
            {
                JsonElement root = JsonSerializer.Deserialize<JsonElement>(plaintext);

                if(root.ValueKind == JsonValueKind.Object)
                {
                    innerResponse = JsonSerializer.Deserialize<Dictionary<string, object>>(root.GetRawText());
                }
                else if(root.ValueKind == JsonValueKind.Array)
                {
                    var items = new List<object>();
                    foreach(var item in root.EnumerateArray())
                    {
                        if(item.ValueKind != JsonValueKind.Object)
                            continue;

                        var dictItem = JsonSerializer.Deserialize<Dictionary<string, object>>(item.GetRawText());
                        if(dictItem != null)
                            items.Add(dictItem);
                    }

                    innerResponse = new Dictionary<string, object>
                    {
                        ["items"] = items
                    };
                }
            }
            catch(JsonException)
            {
                innerResponse = null;
            }

            // Check for error code in decrypted inner response
            if(innerResponse != null)
            {
                string innerCodeStr = GetFieldValue(innerResponse, "code", "0");
                if(innerCodeStr != "0")
                {
                    int errorCode = int.Parse(innerCodeStr);
                    string errorMessage = GetErrorMessage(innerResponse, $"API error {errorCode}");

                    if(errorCode == 1017)
                        throw new BydSessionExpiredException(errorMessage, errorCode, endpoint);

                    if(EndpointNotSupportedCodes.Contains(errorCode))
                        throw new BydVehicleNotSupportedException(errorMessage, errorCode, endpoint);

                    if(extraErrorCodes?.Contains(errorCode) == true)
                        throw new BydApiException(errorMessage, errorCode, endpoint);

                    throw new BydApiException(errorMessage, errorCode, endpoint);
                }
            }

            return innerResponse;
        }

        /// <summary>
        /// Helper to safely extract a string value from a dictionary with fallback.
        /// Mirrors PHP's $arr['key'] ?? $fallback behavior.
        /// </summary>
        private static string GetFieldValue(IDictionary<string, object> dict, string key, string fallback)
        {
            return dict.TryGetValue(key, out var value) && value != null ? value.ToString() ?? fallback : fallback;
        }

        /// <summary>
        /// Helper to extract error message following PHP's null-coalescing chain.
        /// Mirrors: $decoded['message'] ?? $decoded['msg'] ?? $fallback
        /// </summary>
        private static string GetErrorMessage(IDictionary<string, object> dict, string fallback)
        {
            return GetFieldValue(dict, "message", GetFieldValue(dict, "msg", fallback));
        }
    }