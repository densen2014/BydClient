using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using TimeZoneConverter;

namespace BydClient.Config;

/**
 * Client configuration.
 */
public sealed class BydConfig
{
    public string Username { get; }
    public string Password { get; }
    public string BaseUrl { get; }
    public string CountryCode { get; }
    public string Language { get; }
    public string TimeZone { get; } // IANA time zone format, e.g. "Europe/Amsterdam"
    public string AppVersion { get; }
    public string AppInnerVersion { get; }
    public string SoftType { get; }
    private string tboxVersion;
    public string IsAuto { get; }
    public string? ControlPin { get; }

    public double SessionTtl { get; }

    // 12 hours
    private bool mqttEnabled;
    private int mqttKeepalive;
    private double mqttTimeout;
    public DeviceProfile? Device { get; }

    public BydConfig(
        string username,
        string password,
        string baseUrl = "https://dilinkappoversea-eu.byd.auto",
        string countryCode = "NL",
        string language = "en",
        string timeZone = "Europe/Amsterdam",  // IANA time zone format, e.g. "Europe/Amsterdam"
        string appVersion = "3.2.2",
        string appInnerVersion = "322",
        string softType = "0",
        string tboxVersion = "3",  //  T-Box version for vehicle communication.
        string isAuto = "1",
        string? controlPin = null,  // 6-digit remote control PIN set in the BYD app. Required for
                                    // vehicle control commands (lock, unlock, climate, etc.).
                                    // The PIN is hashed with MD5 before sending to the API.
        double sessionTtl = 43200.0,  // Session token time-to-live in seconds.  After this interval
                                      // the client will automatically re-authenticate on the next API
                                      // call.  Defaults to 12 hours.  Set to ``0`` to disable
                                      // automatic expiry (the session will only refresh on auth errors).
        bool mqttEnabled = true,  // Enable MQTT background listener for realtime state enrichment.
        int mqttKeepalive = 120,  // MQTT keepalive in seconds.
        double mqttTimeout = 10.0,  // Seconds to wait for an MQTT reply before falling back to HTTP
                                    // polling.  Applies to all trigger-then-poll endpoints (realtime,
                                    // GPS, remote commands).
        DeviceProfile? device = null
    )
    {
        this.Username = username;
        this.Password = password;
        this.BaseUrl = baseUrl;
        this.CountryCode = countryCode;
        this.Language = language;
        this.TimeZone = timeZone;
        this.AppVersion = appVersion;
        this.AppInnerVersion = appInnerVersion;
        this.SoftType = softType;
        this.tboxVersion = tboxVersion;
        this.IsAuto = isAuto;
        this.ControlPin = controlPin;
        this.SessionTtl = sessionTtl;
        this.mqttEnabled = mqttEnabled;
        this.mqttKeepalive = mqttKeepalive;
        this.mqttTimeout = mqttTimeout;
        this.Device = device ?? new DeviceProfile(username);

        //if (Device.ImeiMd5 == "00000000000000000000000000000000")
        //    Device.ImeiMd5 = Client.ComputeMd5(Device.ImeiMd5);

        TimeZone = TZConvert.TryWindowsToIana(timeZone, out var ianaTimeZone)
            ? ianaTimeZone
            : timeZone;
        BaseUrl = GetBaseUrlFromCountryCode(CountryCode, BaseUrl);
        Language = GetLanguage(CountryCode, Language);
    }

    private string GetBaseUrlFromCountryCode(string countryCode, string baseUrl)
    {
        if(!string.IsNullOrEmpty(baseUrl))
        {
            return baseUrl;
        }

        // read local file
        if (countryCode != string.Empty)
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "Country to Node Mapping.CSV");
            var country2node = File.ReadAllLines(path)
                .SkipWhile(line => line.StartsWith("#"))
                .Select(line => line.Split(';'))
                .Where(parts => parts.Length == 4)
                .ToDictionary(parts => parts[2], parts => parts[0]);
            var pathNode2Server = Path.Combine(Directory.GetCurrentDirectory(), "Node to Server Mapping.CSV");
            var node2server = File.ReadAllLines(pathNode2Server)
                .SkipWhile(line => line.StartsWith("#"))
                .Select(line => line.Split(';'))
                .Where(parts => parts.Length == 5)
                .ToDictionary(parts => parts[0], parts => parts[3]);

            if (country2node.TryGetValue(countryCode, out string? node))
            {
                if (node2server.TryGetValue(node, out string? serverUrl))
                {
                    return serverUrl;
                }
            }
        }

        return "https://dilinkappoversea-eu.byd.auto";  // or "https://dilinkappoverseatest-eu.byd.auto"
    }

    private string GetLanguage(string countryCode, string language)
    {
        if (!string.IsNullOrEmpty(language))
        {
            return language;
        }

        // read local file
        if (countryCode != string.Empty)
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "Country Label.CSV");
            var country2language = File.ReadAllLines(path)
                .SkipWhile(line => line.StartsWith("#"))
                .Select(line => line.Split(';'))
                .Where(parts => parts.Length == 3)
                .ToDictionary(parts => parts[1], parts => parts[2]);

            if (country2language.TryGetValue(countryCode, out string? lang))
            {
                return lang ?? "en";
            }
        }

        return "en"; // default language
    }

    public BydConfig FromEnv(Dictionary<string, object?> overrides = null!)
    {
        overrides ??= new Dictionary<string, object?>();

        // Replicating PHP's $_ENV behavior using Environment.GetEnvironmentVariable
        Func<string, string?> getEnv = (key) => Environment.GetEnvironmentVariable(key);

        object? deviceOverrides = overrides.ContainsKey("device") ? overrides["device"] : null;
        overrides.Remove("device");

        DeviceProfile? device = null;
        if(deviceOverrides is DeviceProfile profile)
        {
            device = profile;
        }
        else if(deviceOverrides is IDictionary deviceDict)
        {
            device = new DeviceProfile(
                OsType: deviceDict.Contains("ostype") ? deviceDict["ostype"]?.ToString() ?? "15" : "15",
                Imei: deviceDict.Contains("imei") ? deviceDict["imei"]?.ToString() ?? "BANGCLE01234" : "BANGCLE01234",
                Mac: deviceDict.Contains("mac") ? deviceDict["mac"]?.ToString() ?? "00:00:00:00:00:00" : "00:00:00:00:00:00",
                Model: deviceDict.Contains("model") ? deviceDict["model"]?.ToString() ?? "POCO F1" : "POCO F1",
                Sdk: deviceDict.Contains("sdk") ? deviceDict["sdk"]?.ToString() ?? "35" : "35",
                Mod: deviceDict.Contains("mod") ? deviceDict["mod"]?.ToString() ?? "Xiaomi" : "Xiaomi",
                ImeiMd5: deviceDict.Contains("imei_md5") ? deviceDict["imei_md5"]?.ToString() ?? "00000000000000000000000000000000" : "00000000000000000000000000000000",
                MobileBrand: deviceDict.Contains("mobile_brand") ? deviceDict["mobile_brand"]?.ToString() ?? "XIAOMI" : "XIAOMI",
                MobileModel: deviceDict.Contains("mobile_model") ? deviceDict["mobile_model"]?.ToString() ?? "POCO F1" : "POCO F1",
                DeviceType: deviceDict.Contains("device_type") ? deviceDict["device_type"]?.ToString() ?? "0" : "0",
                NetworkType: deviceDict.Contains("network_type") ? deviceDict["network_type"]?.ToString() ?? "wifi" : "wifi",
                OsVersion: deviceDict.Contains("os_version") ? deviceDict["os_version"]?.ToString() ?? "35" : "35"
            );
            device.SetImeiMd5FromUsername(Username);
        }

        var config = new Dictionary<string, object?>
        {
            { "username", getEnv("BYD_USERNAME") ?? "" },
            { "password", getEnv("BYD_PASSWORD") ?? "" },
            { "baseUrl", getEnv("BYD_BASE_URL") ?? "https://dilinkappoversea-eu.byd.auto" },
            { "countryCode", getEnv("BYD_COUNTRY_CODE") ?? "NL" },
            { "language", getEnv("BYD_LANGUAGE") ?? "en" },
            { "timeZone", getEnv("BYD_TIME_ZONE") ?? "Europe/Amsterdam" },
            { "appVersion", getEnv("BYD_APP_VERSION") ?? "3.2.2" },
            { "appInnerVersion", getEnv("BYD_APP_INNER_VERSION") ?? "322" },
            { "softType", getEnv("BYD_SOFT_TYPE") ?? "0" },
            { "tboxVersion", getEnv("BYD_TBOX_VERSION") ?? "3" },
            { "isAuto", getEnv("BYD_IS_AUTO") ?? "1" },
            { "controlPin", getEnv("BYD_CONTROL_PIN") ?? null },
            { "sessionTtl", double.TryParse(getEnv("BYD_SESSION_TTL"), out double ttl) ? ttl : 43200.0 },
            { "mqttEnabled", GetBoolEnv("BYD_MQTT_ENABLED", true) },
            { "mqttKeepalive", int.TryParse(getEnv("BYD_MQTT_KEEPALIVE"), out int ka) ? ka : 120 },
            { "mqttTimeout", double.TryParse(getEnv("BYD_MQTT_TIMEOUT"), out double to) ? to : 10.0 },
            { "device", device }
        };

        // Apply overrides
        foreach(var kvp in overrides)
        {
            config[kvp.Key] = kvp.Value;
        }

        return new BydConfig(
            (string)config["username"]!,
            (string)config["password"]!,
            (string)config["baseUrl"]!,
            (string)config["countryCode"]!,
            (string)config["language"]!,
            (string)config["timeZone"]!,
            (string)config["appVersion"]!,
            (string)config["appInnerVersion"]!,
            (string)config["softType"]!,
            (string)config["tboxVersion"]!,
            (string)config["isAuto"]!,
            (string?)config["controlPin"],
            (double)config["sessionTtl"]!,
            (bool)config["mqttEnabled"]!,
            (int)config["mqttKeepalive"]!,
            (double)config["mqttTimeout"]!,
            (DeviceProfile?)config["device"]
        );
    }

    private static bool GetBoolEnv(string key, bool defaultValue)
    {
        string? val = Environment.GetEnvironmentVariable(key);
        if(string.IsNullOrEmpty(val)) return defaultValue;

        // Replicating PHP filter_var(..., FILTER_VALIDATE_BOOLEAN)
        string lower = val.ToLowerInvariant();
        if(lower == "1" || lower == "true" || lower == "on" || lower == "yes") return true;
        if(lower == "0" || lower == "false" || lower == "off" || lower == "no" || lower == "") return false;

        return defaultValue;
    }

    // Getters
    //public string GetUsername()
    //{
    //    return this.Username;
    //}

    //public string GetPassword()
    //{
    //    return this.Password;
    //}

    //public string GetBaseUrl()
    //{
    //    return this.baseUrl;
    //}

    //public string GetCountryCode()
    //{
    //    return this.CountryCode;
    //}

    //public string GetLanguage()
    //{
    //    return this.Language;
    //}

    //public string GetTimeZone()
    //{
    //    return this.TimeZone;
    //}

    //public string GetAppVersion()
    //{
    //    return this.AppVersion;
    //}

    //public string GetSoftType()
    //{
    //    return this.SoftType;
    //}

    public string GetTboxVersion()
    {
        return this.tboxVersion;
    }

    public string GetIsAuto()
    {
        return this.IsAuto;
    }

    //public string? GetControlPin()
    //{
    //    return this.controlPin;
    //}

    //public double GetSessionTtl()
    //{
    //    return this.SessionTtl;
    //}

    public bool IsMqttEnabled()
    {
        return this.mqttEnabled;
    }

    public int GetMqttKeepalive()
    {
        return this.mqttKeepalive;
    }

    public double GetMqttTimeout()
    {
        return this.mqttTimeout;
    }

    //public DeviceProfile GetDevice()
    //{
    //    return this.Device!;
    //}
}

// Assuming DeviceProfile exists based on usage in PHP code.
// Included to ensure the translated code is complete and functional.
//public class DeviceProfile
//{
//    public string Ostype { get; }
//    public string Imei { get; }
//    public string Mac { get; }
//    public string Model { get; }
//    public string Sdk { get; }
//    public string Mod { get; }
//    public string ImeiMd5 { get; }
//    public string MobileBrand { get; }
//    public string MobileModel { get; }
//    public string DeviceType { get; }
//    public string NetworkType { get; }
//    public string OsVersion { get; }

//    public DeviceProfile(
//        string ostype = "15",
//        string imei = "BANGCLE01234",
//        string mac = "00:00:00:00:00:00",
//        string model = "POCO F1",
//        string sdk = "35",
//        string mod = "Xiaomi",
//        string imei_md5 = "00000000000000000000000000000000",
//        string mobile_brand = "XIAOMI",
//        string mobile_model = "POCO F1",
//        string device_type = "0",
//        string network_type = "wifi",
//        string os_version = "35"
//    )
//    {
//        Ostype = ostype;
//        Imei = imei;
//        Mac = mac;
//        Model = model;
//        Sdk = sdk;
//        Mod = mod;
//        ImeiMd5 = imei_md5;
//        MobileBrand = mobile_brand;
//        MobileModel = mobile_model;
//        DeviceType = device_type;
//        NetworkType = network_type;
//        OsVersion = os_version;
//    }
//}
