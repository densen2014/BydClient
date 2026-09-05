using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace BydClient.Models;

/// <summary>
/// GPS information for a vehicle.
/// </summary>
public class GpsInfo : BaseModel
{
    public float? Latitude { get; private set; }
    public float? Longitude { get; private set; }
    public float? Altitude { get; private set; }
    public float? Speed { get; private set; }
    public float? Heading { get; private set; }
    public float? Direction { get; private set; }
    public DateTimeOffset? Timestamp { get; private set; }
    public string? PositionType { get; private set; }

    public GpsInfo() { }
    public GpsInfo(IDictionary<string, object?> data) : base(data) { }

    protected override void Populate(IDictionary<string, object?> data)
    {
        if(data == null) throw new ArgumentNullException(nameof(data));

        IDictionary<string, object?> source = data.TryGetValue("data", out var nested)
            && nested is IDictionary<string, object?> nestedData
                ? nestedData
                : data;

        object? GetValue(string key) => source.TryGetValue(key, out var value) ? value : null;

        Latitude = ToNullableFloat(GetValue("latitude"));
        Longitude = ToNullableFloat(GetValue("longitude"));
        Altitude = ToNullableFloat(GetValue("altitude"));
        Speed = ToNullableFloat(GetValue("speed"));
        Heading = ToNullableFloat(GetValue("heading"));
        Direction = ToNullableFloat(GetValue("direction"));

        if(GetValue("gpsTimeStamp") is { } timestamp)
            Timestamp = ParseTimestamp(timestamp);

        PositionType = GetValue("positionType")?.ToString();
    }

    private static DateTimeOffset? ParseTimestamp(object timestamp)
    {
        if(timestamp == null) return null;
        if(!long.TryParse(timestamp.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var ts)) return null;

        // If value looks like milliseconds (>= 1_000_000_000_000), convert to seconds
        if(ts >= 1_000_000_000_000L) ts /= 1000L;

        try
        {
            return DateTimeOffset.FromUnixTimeSeconds(ts);
        }
        catch
        {
            return null;
        }
    }

    private static float? ToNullableFloat(object? v)
    {
        if(v == null) return null;
        return float.TryParse(v.ToString(), NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var f) ? f : null;
    }

    /// <summary>
    /// Check if GPS data has meaningful content.
    /// Mirrors PHP: returns false for empty payloads or payloads that only contain requestSerial.
    /// </summary>
    public static bool IsGpsInfoReady(IDictionary<string, object?> gpsInfo)
    {
        if(gpsInfo == null || gpsInfo.Count == 0) return false;

        // If the only key is "requestSerial", treat as not ready
        if(gpsInfo.Count == 1 && gpsInfo.ContainsKey("requestSerial")) return false;

        return true;
    }

    // Optional factory
    public static GpsInfo FromDictionary(IDictionary<string, object?> data)
    {
        var inst = new GpsInfo();
        inst.Populate(data);
        return inst;
    }
}
