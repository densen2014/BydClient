using BydClient.Exceptions;
using System;
using System.Collections.Generic;
using System.Buffers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Linq;

namespace BydClient.Crypto;

    /// <summary>
    /// Standard AES-CBC encryption for BYD inner payloads.
    /// Faithful port of the PHP Aes class with identical PKCS#7 handling, zero-IV usage,
    /// and JSON detection logic.
    /// </summary>
    public static class Aes
    {
        private static readonly byte[] ZeroIv = new byte[16];

        /// <summary>
        /// Parse hex string to bytes.
        /// </summary>
        private static byte[] ParseHexBytes(string value, string name, int[]? allowedNbytes = null)
        {
            if(string.IsNullOrWhiteSpace(value))
                throw new BangcleException($"{name} is empty");

            string text = value.Trim();
            if(text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                text = text.Substring(2);

            if(text.Length % 2 != 0)
                throw new BangcleException($"{name} hex length must be even (got {text.Length})");

            if(!text.All(char.IsAsciiHexDigit))
                throw new BangcleException($"{name} must be hex-encoded");

            byte[] data = Convert.FromHexString(text);

            if(allowedNbytes != null && !allowedNbytes.Contains(data.Length))
            {
                throw new BangcleException($"{name} must be {string.Join(", ", allowedNbytes)} bytes (got {data.Length})");
            }

            return data;
        }

        /// <summary>
        /// AES-CBC encrypt with zero IV and PKCS#7 padding, returning uppercase hex.
        /// </summary>
        public static string AesEncryptHex(string plaintext, string keyHex)
        {
            try
            {
                byte[] key = ParseHexBytes(keyHex, "AES key", [16, 24, 32]);
                byte[] data = Encoding.UTF8.GetBytes(plaintext);
                byte[] padded = PadPkcs7(data, 16);

                using System.Security.Cryptography.Aes aes = System.Security.Cryptography.Aes.Create();
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.None; // Manual padding matches PHP behavior
                aes.Key = key;
                aes.IV = ZeroIv;

                using ICryptoTransform encryptor = aes.CreateEncryptor();
                byte[] cipher = encryptor.TransformFinalBlock(padded, 0, padded.Length);

                return Convert.ToHexString(cipher).ToUpperInvariant();
            }
            catch(BangcleException)
            {
                throw;
            }
            catch(Exception ex)
            {
                throw new BangcleException($"AES encryption failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// AES-CBC decrypt from hex with zero IV.
        /// Returns raw UTF-8 string if valid JSON, otherwise applies PKCS#7 unpadding.
        /// </summary>
        public static string AesDecryptUtf8(string cipherHex, string keyHex)
        {
            try
            {
                byte[] key = ParseHexBytes(keyHex, "AES key", [16, 24, 32]);
                byte[] cipher = ParseHexBytes(cipherHex, "AES ciphertext");

                using System.Security.Cryptography.Aes aes = System.Security.Cryptography.Aes.Create();
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.None;
                aes.Key = key;
                aes.IV = ZeroIv;

                using ICryptoTransform decryptor = aes.CreateDecryptor();
                byte[] decryptedBytes = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);

                // Always remove PKCS#7 padding before interpreting as UTF-8/JSON to avoid
                // JsonDocument throwing on trailing padding bytes (first-chance exceptions).
                string unpadded = UnpadPkcs7(decryptedBytes);

                try
                {
                    // JSON_THROW_ON_ERROR equivalent
                    using JsonDocument _ = JsonDocument.Parse(unpadded);
                    return unpadded;
                }
                catch(JsonException)
                {
                    // If unpadded isn't valid JSON, return unpadded plaintext anyway.
                    return unpadded;
                }
            }
            catch(BangcleException)
            {
                throw;
            }
            catch(Exception ex)
            {
                throw new BangcleException($"AES decryption failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// PKCS#7 padding.
        /// </summary>
        private static byte[] PadPkcs7(byte[] data, int blockSize)
        {
            int pad = blockSize - (data.Length % blockSize);
            byte[] result = new byte[data.Length + pad];
            Buffer.BlockCopy(data, 0, result, 0, data.Length);
            Array.Fill(result, (byte)pad, data.Length, pad);
            return result;
        }

        /// <summary>
        /// PKCS#7 unpadding.
        /// </summary>
        private static string UnpadPkcs7(byte[] data)
        {
            int pad = data[data.Length - 1];
            return Encoding.UTF8.GetString(data, 0, data.Length - pad);
        }
    }