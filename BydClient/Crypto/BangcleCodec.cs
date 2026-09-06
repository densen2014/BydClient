using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using BydClient.Exceptions;

namespace BydClient.Crypto;

/// <summary>
/// Encode and decode Bangcle envelopes using white-box AES.
/// </summary>
public class BangcleCodec
{
    private Dictionary<string, byte[]>? _tables = null;
    private readonly string? _tablesPath;
    private const string EmbeddedTablesResourceName = "BydClient.Data.bangcle_tables.bin";

    // Binary table file format constants
    private const string Magic = "BGTB";
    private const int Version = 1;
    private const int TableCount = 8;
    private const int HeaderSize = 8; // 4 (magic) + 2 (version) + 2 (count)
    private const int IndexEntrySize = 8; // 4 (offset) + 4 (length)
    private const int IndexSize = TableCount * IndexEntrySize; // 64

    private static readonly byte[] ZeroIv = new byte[16];
    private static readonly (string Name, int ExpectedLength)[] TableSpecs = new[]
    {
        ("inv_round", 0x28000),
        ("inv_xor", 0x3C000),
        ("inv_first", 0x1000),
        ("round", 0x28000),
        ("xor", 0x3C000),
        ("final", 0x1000),
        ("perm_decrypt", 8),
        ("perm_encrypt", 8)
    };

    public BangcleCodec(string? tablesPath = null)
    {
        _tablesPath = tablesPath;
    }

    /// <summary>
    /// Load tables from binary file.
    /// </summary>
    private Dictionary<string, byte[]> LoadTables()
    {
        if(_tables != null) return _tables;

        byte[] raw;
        if(_tablesPath != null)
        {
            if(!File.Exists(_tablesPath))
                throw new BangcleException($"Table file not found: {_tablesPath}");
            raw = File.ReadAllBytes(_tablesPath);
        }
        else
        {
            var assembly = typeof(BangcleCodec).Assembly;
            using var resourceStream = assembly.GetManifestResourceStream(EmbeddedTablesResourceName);
            if(resourceStream != null)
            {
                using var buffer = new MemoryStream();
                resourceStream.CopyTo(buffer);
                raw = buffer.ToArray();
            }
            else
            {
                // Preserve the legacy loose-file fallback for existing desktop deployments.
                var assemblyDir = Path.GetDirectoryName(assembly.Location) ?? AppContext.BaseDirectory;
                var defaultPath = Path.Combine(assemblyDir, "..", "data", "bangcle_tables.bin");

                if(File.Exists(defaultPath))
                    raw = File.ReadAllBytes(defaultPath);
                else
                    throw new BangcleException("bangcle_tables.bin was not found as an embedded resource or external file.");
            }
        }

        _tables = ParseTables(raw);
        return _tables;
    }

    /// <summary>
    /// Parse the binary table file.
    /// </summary>
    private Dictionary<string, byte[]> ParseTables(byte[] data)
    {
        if(data.Length < HeaderSize + IndexSize)
            throw new BangcleException("Table file too short");

        string magic = Encoding.ASCII.GetString(data, 0, 4);
        if(magic != Magic)
            throw new BangcleException($"Bad magic: expected {Magic}, got {BitConverter.ToString(data, 0, 4).Replace("-", "")}");

        ushort version = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(4));
        if(version != Version)
            throw new BangcleException($"Unsupported table version: {version}");

        ushort count = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(6));
        if(count != TableCount)
            throw new BangcleException($"Expected {TableCount} tables, got {count}");

        var tables = new Dictionary<string, byte[]>(TableCount);

        for(int i = 0; i < TableCount; i++)
        {
            int idxOffset = HeaderSize + i * IndexEntrySize;
            uint offset = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(idxOffset));
            uint length = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(idxOffset + 4));

            var (expectedName, expectedLength) = TableSpecs[i];

            if(length != expectedLength)
                throw new BangcleException($"Table {expectedName}: expected {expectedLength} bytes, got {length}");

            if(offset + length > data.Length)
                throw new BangcleException($"Table {expectedName}: data extends beyond file");

            tables[expectedName] = data.AsSpan().Slice((int)offset, (int)length).ToArray();
        }

        return tables;
    }

    /// <summary>
    /// Normalize a Bangcle envelope string for decoding.
    /// </summary>
    private string NormalizeEnvelopeInput(string envelope)
    {
        var cleaned = envelope.Trim()
            .Replace(" ", "")
            .Replace("\t", "")
            .Replace("\n", "")
            .Replace("\r", "");

        // URL-safe base64 normalization
        cleaned = cleaned.Replace("-", "+").Replace("_", "/");

        if(string.IsNullOrEmpty(cleaned))
            throw new BangcleException("Bangcle input is empty");

        if(!cleaned.StartsWith("F", StringComparison.Ordinal))
            throw new BangcleException("Bangcle envelope must start with \"F\"");

        cleaned = cleaned.Substring(1); // strip F prefix
        int remainder = cleaned.Length % 4;
        if(remainder != 0)
            cleaned += new string('=', 4 - remainder);

        return cleaned;
    }

    /// <summary>
    /// Encode plaintext into a Bangcle envelope (F + base64).
    /// </summary>
    public string EncodeEnvelope(string plaintext)
    {
        var tables = LoadTables();
        // PHP treats strings as raw bytes; we use UTF-8 for C# string compatibility
        var plainBytes = Encoding.UTF8.GetBytes(plaintext);
        var padded = Pkcs7.AddPkcs7(plainBytes);
        var ciphertext = BangcleBlock.EncryptCbc(tables, padded, ZeroIv);

        return "F" + Convert.ToBase64String(ciphertext);
    }

    /// <summary>
    /// Decode a Bangcle envelope back to plaintext.
    /// </summary>
    public string DecodeEnvelope(string envelope)
    {
        var tables = LoadTables();
        var b64Payload = NormalizeEnvelopeInput(envelope);

        byte[] ciphertext;
        try
        {
            ciphertext = Convert.FromBase64String(b64Payload);
        }
        catch(FormatException)
        {
            throw new BangcleException("Invalid base64 in Bangcle envelope");
        }

        if(ciphertext.Length == 0)
            throw new BangcleException("Bangcle ciphertext is empty");

        if(ciphertext.Length % 16 != 0)
            throw new BangcleException($"Bangcle ciphertext length {ciphertext.Length} is not a multiple of 16");

        var plaintextBytes = BangcleBlock.DecryptCbc(tables, ciphertext, ZeroIv);
        return Encoding.UTF8.GetString(Pkcs7.StripPkcs7(plaintextBytes));
    }
}

