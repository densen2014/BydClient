using System;
using System.Collections.Generic;
using System.Text;

namespace BydClient.Models;

// --- Placeholder enum (adatta ai valori reali) ---
public enum ChargingState { UNKNOWN = -1, NOT_CHARGING = 0, CHARGING = 1, NOT_CONNECTED = 15 }


public class ChargingStatus : BaseModel
{
    public ChargingState ChargingState { get; private set; } = ChargingState.UNKNOWN;
    public float? ChargerPower { get; private set; }
    public float? ChargerVoltage { get; private set; }
    public float? ChargerCurrent { get; private set; }
    public float? BatteryCapacity { get; private set; }
    public float? BatteryVoltage { get; private set; }
    public float? BatteryCurrent { get; private set; }
    public float? BatteryTemperature { get; private set; }
    public float? BatterySOC { get; private set; }
    public float? ChargingPower { get; private set; }
    public int? ChargingTime { get; private set; }
    public int? RemainingTime { get; private set; }
    public float? MileageOfCharge { get; private set; }
    public float? MileageOfDay { get; private set; }
    public float? MileageOfWeek { get; private set; }
    public float? MileageOfMonth { get; private set; }
    public DateTimeOffset? StartTime { get; private set; }
    public DateTimeOffset? EndTime { get; private set; }
    public string? ChargingPileName { get; private set; }
    public string? ChargingPileSN { get; private set; }
    public int? ChargingType { get; private set; }
    public float? ChargingCost { get; private set; }
    public float? ElectricPrice { get; private set; }
    public float? ServiceFee { get; private set; }
    public float? TotalFee { get; private set; }

    public ChargingStatus(IDictionary<string, object?> data) : base(data)
    {
        
    }

    protected override void Populate(IDictionary<string, object?> data)
    {
        object? GetValue(string key) => data.TryGetValue(key, out var value) ? value : null;

        ChargingState = TryParseEnum(GetValue("chargingState") ?? -1, ChargingState.UNKNOWN);
        ChargerPower = ToNullableFloat(GetValue("chargerPower"));
        ChargerVoltage = ToNullableFloat(GetValue("chargerVoltage"));
        ChargerCurrent = ToNullableFloat(GetValue("chargerCurrent"));
        BatteryCapacity = ToNullableFloat(GetValue("batteryCapacity"));
        BatteryVoltage = ToNullableFloat(GetValue("batteryVoltage"));
        BatteryCurrent = ToNullableFloat(GetValue("batteryCurrent"));
        BatteryTemperature = ToNullableFloat(GetValue("batteryTemperature"));
        BatterySOC = ToNullableFloat(GetValue("batterySOC"));
        ChargingPower = ToNullableFloat(GetValue("chargingPower"));
        ChargingTime = ToNullableInt(GetValue("chargingTime"));
        RemainingTime = ToNullableInt(GetValue("remainingTime"));
        MileageOfCharge = ToNullableFloat(GetValue("mileageOfCharge"));
        MileageOfDay = ToNullableFloat(GetValue("mileageOfDay"));
        MileageOfWeek = ToNullableFloat(GetValue("mileageOfWeek"));
        MileageOfMonth = ToNullableFloat(GetValue("mileageOfMonth"));

        if(data.TryGetValue("startTime", out var st) && st != null)
            StartTime = ParseTimestamp(st);

        if(data.TryGetValue("endTime", out var et) && et != null)
            EndTime = ParseTimestamp(et);

        ChargingPileName = GetValue("chargingPileName")?.ToString();
        ChargingPileSN = GetValue("chargingPileSN")?.ToString();
        ChargingType = ToNullableInt(GetValue("chargingType"));
        ChargingCost = ToNullableFloat(GetValue("chargingCost"));
        ElectricPrice = ToNullableFloat(GetValue("electricPrice"));
        ServiceFee = ToNullableFloat(GetValue("serviceFee"));
        TotalFee = ToNullableFloat(GetValue("totalFee"));
    }

    private static DateTimeOffset? ParseTimestamp(object timestamp)
    {
        if(timestamp == null) return null;
        if(!long.TryParse(timestamp.ToString(), out var ts)) return null;

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

    // --- Helper converters ---
    private static int? ToNullableInt(object? v)
    {
        if(v == null) return null;
        return int.TryParse(v.ToString(), out var i) ? i : null;
    }

    private static float? ToNullableFloat(object? v)
    {
        if(v == null) return null;
        return float.TryParse(v.ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var f) ? f : null;
    }

    private static TEnum TryParseEnum<TEnum>(object? value, TEnum fallback) where TEnum : struct, Enum
    {
        if(value == null) return fallback;
        if(int.TryParse(value.ToString(), out var iv))
        {
            // If enum contains the numeric value, return it; otherwise fallback
            if(Enum.IsDefined(typeof(TEnum), iv))
                return (TEnum)Enum.ToObject(typeof(TEnum), iv);
        }
        return fallback;
    }
}


