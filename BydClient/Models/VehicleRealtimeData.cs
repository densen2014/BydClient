using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using static BydClient.Models.HvacStatus;

namespace BydClient.Models;

public enum OnlineState : int
{
    UNKNOWN = -1,
    ONLINE = 1,
    OFFLINE = 2
}

public enum ConnectState : int
{
    UNKNOWN = -1,
    DISCONNECTED = 0,
    CONNECTED = 1
}

public enum VehicleState : int
{
    UNKNOWN = -1,
    STARTED = 0,
    DRIVING = 1,
    POWER_OFF = 2
}

public enum PowerGear : int
{
    UNKNOWN = -1,
    OFF = 1,
    ON = 3
}

public enum DoorOpenState : int
{
    UNKNOWN = -1,
    CLOSED = 0,
    OPEN = 1
}

public enum LockState : int
{
    UNKNOWN = -1,
    UNAVAILABLE = 0,
    UNLOCKED = 1,
    LOCKED = 2
}

public enum WindowState : int
{
    UNKNOWN = -1,
    CLOSED = 1,
    OPEN = 2
}

public enum TirePressureUnit : int
{
    UNKNOWN = -1,
    BAR = 1,
    PSI = 2,
    KPA = 3
}

public class VehicleRealtimeData : BaseModel
{
    // Connection & state
    public OnlineState OnlineState { get; private set; } = OnlineState.UNKNOWN;
    public ConnectState ConnectState { get; private set; } = ConnectState.UNKNOWN;
    public VehicleState VehicleState { get; private set; } = VehicleState.UNKNOWN;
    public string? RequestSerial { get; private set; }

    // Battery & range
    public float? ElecPercent { get; private set; }
    public float? PowerBattery { get; private set; }
    public float? EnduranceMileage { get; private set; }
    public float? EvEndurance { get; private set; }
    public float? EnduranceMileageV2 { get; private set; }
    public string? EnduranceMileageV2Unit { get; private set; }
    public float? TotalMileage { get; private set; }
    public float? TotalMileageV2 { get; private set; }
    public string? TotalMileageV2Unit { get; private set; }

    // Driving
    public float? Speed { get; private set; }
    public PowerGear PowerGear { get; private set; } = PowerGear.UNKNOWN;

    // Climate
    public float? TempInCar { get; private set; }
    public int? MainSettingTemp { get; private set; }
    public float? MainSettingTempNew { get; private set; }
    public AirCirculationMode AirRunState { get; private set; } = AirCirculationMode.UNKNOWN;

    // Seat heating/ventilation
    public SeatHeatVentState MainSeatHeatState { get; private set; } = SeatHeatVentState.UNKNOWN;
    public SeatHeatVentState MainSeatVentilationState { get; private set; } = SeatHeatVentState.UNKNOWN;
    public SeatHeatVentState CopilotSeatHeatState { get; private set; } = SeatHeatVentState.UNKNOWN;
    public SeatHeatVentState CopilotSeatVentilationState { get; private set; } = SeatHeatVentState.UNKNOWN;
    public StearingWheelHeat SteeringWheelHeatState { get; private set; } = StearingWheelHeat.OFF;
    public SeatHeatVentState LrSeatHeatState { get; private set; } = SeatHeatVentState.UNKNOWN;
    public SeatHeatVentState LrSeatVentilationState { get; private set; } = SeatHeatVentState.UNKNOWN;
    public SeatHeatVentState LrThirdHeatState { get; private set; } = SeatHeatVentState.UNKNOWN;
    public SeatHeatVentState LrThirdVentilationState { get; private set; } = SeatHeatVentState.UNKNOWN;
    public SeatHeatVentState RrSeatHeatState { get; private set; } = SeatHeatVentState.UNKNOWN;
    public SeatHeatVentState RrSeatVentilationState { get; private set; } = SeatHeatVentState.UNKNOWN;
    public SeatHeatVentState RrThirdHeatState { get; private set; } = SeatHeatVentState.UNKNOWN;
    public SeatHeatVentState RrThirdVentilationState { get; private set; } = SeatHeatVentState.UNKNOWN;

    // Charging
    public ChargingState ChargingState { get; private set; } = ChargingState.UNKNOWN;
    public ChargingState ChargeState { get; private set; } = ChargingState.UNKNOWN;
    public int? WaitStatus { get; private set; }
    public int? FullHour { get; private set; }
    public int? FullMinute { get; private set; }
    public int? RemainingHours { get; private set; }
    public int? RemainingMinutes { get; private set; }
    public int? BookingChargeState { get; private set; }
    public int? BookingChargingHour { get; private set; }
    public int? BookingChargingMinute { get; private set; }

    // Doors
    public DoorOpenState LeftFrontDoor { get; private set; } = DoorOpenState.UNKNOWN;
    public DoorOpenState RightFrontDoor { get; private set; } = DoorOpenState.UNKNOWN;
    public DoorOpenState LeftRearDoor { get; private set; } = DoorOpenState.UNKNOWN;
    public DoorOpenState RightRearDoor { get; private set; } = DoorOpenState.UNKNOWN;
    public DoorOpenState TrunkLid { get; private set; } = DoorOpenState.UNKNOWN;
    public DoorOpenState SlidingDoor { get; private set; } = DoorOpenState.UNKNOWN;
    public DoorOpenState Forehold { get; private set; } = DoorOpenState.UNKNOWN;

    // Locks
    public LockState LeftFrontDoorLock { get; private set; } = LockState.UNKNOWN;
    public LockState RightFrontDoorLock { get; private set; } = LockState.UNKNOWN;
    public LockState LeftRearDoorLock { get; private set; } = LockState.UNKNOWN;
    public LockState RightRearDoorLock { get; private set; } = LockState.UNKNOWN;
    public LockState SlidingDoorLock { get; private set; } = LockState.UNKNOWN;

    // Windows
    public WindowState LeftFrontWindow { get; private set; } = WindowState.UNKNOWN;
    public WindowState RightFrontWindow { get; private set; } = WindowState.UNKNOWN;
    public WindowState LeftRearWindow { get; private set; } = WindowState.UNKNOWN;
    public WindowState RightRearWindow { get; private set; } = WindowState.UNKNOWN;
    public WindowState Skylight { get; private set; } = WindowState.UNKNOWN;

    // Tire pressure
    public float? LeftFrontTirePressure { get; private set; }
    public float? RightFrontTirePressure { get; private set; }
    public float? LeftRearTirePressure { get; private set; }
    public float? RightRearTirePressure { get; private set; }
    public int? LeftFrontTireStatus { get; private set; }
    public int? RightFrontTireStatus { get; private set; }
    public int? LeftRearTireStatus { get; private set; }
    public int? RightRearTireStatus { get; private set; }
    public TirePressureUnit TirePressUnit { get; private set; } = TirePressureUnit.UNKNOWN;
    public int? TirepressureSystem { get; private set; }
    public int? RapidTireLeak { get; private set; }

    // Energy consumption
    public float? TotalPower { get; private set; }
    public float? Gl { get; private set; }
    public string? TotalEnergy { get; private set; }
    public string? NearestEnergyConsumption { get; private set; }
    public string? NearestEnergyConsumptionUnit { get; private set; }
    public string? Recent50kmEnergy { get; private set; }
    public float? EnergyConsumption { get; private set; }

    // Fuel (hybrid)
    public float? OilEndurance { get; private set; }
    public float? OilPercent { get; private set; }
    public float? TotalOil { get; private set; }

    // System indicators
    public int? PowerSystem { get; private set; }
    public int? EngineStatus { get; private set; }
    public int? Epb { get; private set; }
    public int? Eps { get; private set; }
    public int? Esp { get; private set; }
    public int? AbsWarning { get; private set; }
    public int? Svs { get; private set; }
    public int? Srs { get; private set; }
    public int? Ect { get; private set; }
    public int? EctValue { get; private set; }
    public int? Pwr { get; private set; }

    // Feature states
    public int? SentryStatus { get; private set; }
    public int? BatteryHeatState { get; private set; }
    public int? ChargeHeatState { get; private set; }
    public int? UpgradeStatus { get; private set; }

    // Metadata
    public DateTimeOffset? Timestamp { get; private set; }

    public VehicleRealtimeData(IDictionary<string, object?> data) : base(data) { }

    // --- Populate method (maps PHP populate) ---
    protected override void Populate(IDictionary<string, object?> data)
    {
        // Apply aliases
        var aliases = new Dictionary<string, string>
        {
            ["backCover"] = "trunkLid",
            ["leftFrontTirepressure"] = "leftFrontTirePressure",
            ["rightFrontTirepressure"] = "rightFrontTirePressure",
            ["leftRearTirepressure"] = "leftRearTirePressure",
            ["rightRearTirepressure"] = "rightRearTirePressure",
            ["abs"] = "absWarning",
            ["time"] = "timestamp",
            ["stearingWheelHeatState"] = "steeringWheelHeatState"
        };

        foreach(var (oldKey, newKey) in aliases)
        {
            if(data.ContainsKey(oldKey) && !data.ContainsKey(newKey))
            {
                data[newKey] = data[oldKey];
                data.Remove(oldKey);
            }
        }

        // Helper local funcs
        static TEnum TryParseEnum<TEnum>(object? value, TEnum fallback) where TEnum : struct, Enum
        {
            if(value == null) return fallback;
            if(int.TryParse(value.ToString(), out var iv) && Enum.IsDefined(typeof(TEnum), iv))
                return (TEnum)Enum.ToObject(typeof(TEnum), iv);
            return fallback;
        }

        static int? ToNullableInt(object? v)
        {
            if(v == null) return null;
            if(int.TryParse(v.ToString(), out var i)) return i;
            return null;
        }

        static float? ToNullableFloat(object? v)
        {
            if(v == null) return null;
            if(float.TryParse(v.ToString(), out var f)) return f;
            return null;
        }

        // Connection & state
        OnlineState = TryParseEnum(data["onlineState"] ?? -1, OnlineState.UNKNOWN);
        ConnectState = TryParseEnum(data["connectState"] ?? -1, ConnectState.UNKNOWN);
        VehicleState = TryParseEnum(data["vehicleState"] ?? -1, VehicleState.UNKNOWN);
        RequestSerial = data.TryGetValue("requestSerial", out var requestSerial) ? requestSerial?.ToString() : null;

        // Battery & range
        ElecPercent = ToNullableFloat(data["elecPercent"]);
        PowerBattery = ToNullableFloat(data["powerBattery"]);
        EnduranceMileage = ToNullableFloat(data["enduranceMileage"]);
        EvEndurance = ToNullableFloat(data["evEndurance"]);
        EnduranceMileageV2 = ToNullableFloat(data["enduranceMileageV2"]);
        EnduranceMileageV2Unit = data["enduranceMileageV2Unit"]?.ToString();
        TotalMileage = ToNullableFloat(data["totalMileage"]);
        TotalMileageV2 = ToNullableFloat(data["totalMileageV2"]);
        TotalMileageV2Unit = data["totalMileageV2Unit"]?.ToString();

        // Driving
        Speed = ToNullableFloat(data["speed"]);
        PowerGear = TryParseEnum(data["powerGear"] ?? -1, PowerGear.UNKNOWN);

        // Climate — tempInCar sentinel handling (-100)
        var rawTempInCar = ToNullableFloat(data["tempInCar"]);
        TempInCar = (rawTempInCar != null && rawTempInCar <= -100.0f) ? null : rawTempInCar;

        MainSettingTemp = ToNullableInt(data["mainSettingTemp"]);
        MainSettingTempNew = ToNullableFloat(data["mainSettingTempNew"]);
        AirRunState = TryParseEnum(data["airRunState"] ?? -1, AirCirculationMode.UNKNOWN);

        // Seat heating/ventilation
        MainSeatHeatState = TryParseEnum(data["mainSeatHeatState"] ?? -1, SeatHeatVentState.UNKNOWN);
        MainSeatVentilationState = TryParseEnum(data["mainSeatVentilationState"] ?? -1, SeatHeatVentState.UNKNOWN);
        CopilotSeatHeatState = TryParseEnum(data["copilotSeatHeatState"] ?? -1, SeatHeatVentState.UNKNOWN);
        CopilotSeatVentilationState = TryParseEnum(data["copilotSeatVentilationState"] ?? -1, SeatHeatVentState.UNKNOWN);
        SteeringWheelHeatState = TryParseEnum(data["steeringWheelHeatState"] ?? -1, StearingWheelHeat.OFF);
        LrSeatHeatState = TryParseEnum(data["lrSeatHeatState"] ?? -1, SeatHeatVentState.UNKNOWN);
        LrSeatVentilationState = TryParseEnum(data["lrSeatVentilationState"] ?? -1, SeatHeatVentState.UNKNOWN);
        LrThirdHeatState = TryParseEnum(data["lrThirdHeatState"] ?? -1, SeatHeatVentState.UNKNOWN);
        LrThirdVentilationState = TryParseEnum(data["lrThirdVentilationState"] ?? -1, SeatHeatVentState.UNKNOWN);
        RrSeatHeatState = TryParseEnum(data["rrSeatHeatState"] ?? -1, SeatHeatVentState.UNKNOWN);
        RrSeatVentilationState = TryParseEnum(data["rrSeatVentilationState"] ?? -1, SeatHeatVentState.UNKNOWN);
        RrThirdHeatState = TryParseEnum(data["rrThirdHeatState"] ?? -1, SeatHeatVentState.UNKNOWN);
        RrThirdVentilationState = TryParseEnum(data["rrThirdVentilationState"] ?? -1, SeatHeatVentState.UNKNOWN);

        // Charging — -1 sentinel => null for hours/minutes
        ChargingState = TryParseEnum(data["chargingState"] ?? -1, ChargingState.UNKNOWN);
        ChargeState = TryParseEnum(data["chargeState"] ?? -1, ChargingState.UNKNOWN);
        WaitStatus = ToNullableInt(data["waitStatus"]);

        var rawFullHour = ToNullableInt(data["fullHour"]);
        FullHour = (rawFullHour != null && rawFullHour < 0) ? null : rawFullHour;

        var rawFullMinute = ToNullableInt(data["fullMinute"]);
        FullMinute = (rawFullMinute != null && rawFullMinute < 0) ? null : rawFullMinute;

        var rawRemainingHours = ToNullableInt(data["remainingHours"]);
        RemainingHours = (rawRemainingHours != null && rawRemainingHours < 0) ? null : rawRemainingHours;

        var rawRemainingMinutes = ToNullableInt(data["remainingMinutes"]);
        RemainingMinutes = (rawRemainingMinutes != null && rawRemainingMinutes < 0) ? null : rawRemainingMinutes;

        BookingChargeState = ToNullableInt(data["bookingChargeState"]);
        BookingChargingHour = ToNullableInt(data["bookingChargingHour"]);
        BookingChargingMinute = ToNullableInt(data["bookingChargingMinute"]);

        // Doors
        LeftFrontDoor = TryParseEnum(data["leftFrontDoor"] ?? -1, DoorOpenState.UNKNOWN);
        RightFrontDoor = TryParseEnum(data["rightFrontDoor"] ?? -1, DoorOpenState.UNKNOWN);
        LeftRearDoor = TryParseEnum(data["leftRearDoor"] ?? -1, DoorOpenState.UNKNOWN);
        RightRearDoor = TryParseEnum(data["rightRearDoor"] ?? -1, DoorOpenState.UNKNOWN);
        TrunkLid = TryParseEnum(data["trunkLid"] ?? -1, DoorOpenState.UNKNOWN);
        SlidingDoor = TryParseEnum(data["slidingDoor"] ?? -1, DoorOpenState.UNKNOWN);
        Forehold = TryParseEnum(data["forehold"] ?? -1, DoorOpenState.UNKNOWN);

        // Locks
        LeftFrontDoorLock = TryParseEnum(data["leftFrontDoorLock"] ?? -1, LockState.UNKNOWN);
        RightFrontDoorLock = TryParseEnum(data["rightFrontDoorLock"] ?? -1, LockState.UNKNOWN);
        LeftRearDoorLock = TryParseEnum(data["leftRearDoorLock"] ?? -1, LockState.UNKNOWN);
        RightRearDoorLock = TryParseEnum(data["rightRearDoorLock"] ?? -1, LockState.UNKNOWN);
        SlidingDoorLock = TryParseEnum(data["slidingDoorLock"] ?? -1, LockState.UNKNOWN);

        // Windows
        LeftFrontWindow = TryParseEnum(data["leftFrontWindow"] ?? -1, WindowState.UNKNOWN);
        RightFrontWindow = TryParseEnum(data["rightFrontWindow"] ?? -1, WindowState.UNKNOWN);
        LeftRearWindow = TryParseEnum(data["leftRearWindow"] ?? -1, WindowState.UNKNOWN);
        RightRearWindow = TryParseEnum(data["rightRearWindow"] ?? -1, WindowState.UNKNOWN);
        Skylight = TryParseEnum(data["skylight"] ?? -1, WindowState.UNKNOWN);

        // Tire pressure
        LeftFrontTirePressure = ToNullableFloat(data["leftFrontTirePressure"]);
        RightFrontTirePressure = ToNullableFloat(data["rightFrontTirePressure"]);
        LeftRearTirePressure = ToNullableFloat(data["leftRearTirePressure"]);
        RightRearTirePressure = ToNullableFloat(data["rightRearTirePressure"]);
        LeftFrontTireStatus = ToNullableInt(data["leftFrontTireStatus"]);
        RightFrontTireStatus = ToNullableInt(data["rightFrontTireStatus"]);
        LeftRearTireStatus = ToNullableInt(data["leftRearTireStatus"]);
        RightRearTireStatus = ToNullableInt(data["rightRearTireStatus"]);
        TirePressUnit = TryParseEnum(data["tirePressUnit"] ?? -1, TirePressureUnit.UNKNOWN);
        TirepressureSystem = ToNullableInt(data["tirepressureSystem"]);
        RapidTireLeak = ToNullableInt(data["rapidTireLeak"]);

        // Energy consumption
        TotalPower = ToNullableFloat(data["totalPower"]);
        Gl = ToNullableFloat(data["gl"]);
        TotalEnergy = data["totalEnergy"]?.ToString();
        NearestEnergyConsumption = data["nearestEnergyConsumption"]?.ToString();
        NearestEnergyConsumptionUnit = data["nearestEnergyConsumptionUnit"]?.ToString();
        Recent50kmEnergy = data["recent50kmEnergy"]?.ToString();
        EnergyConsumption = ToNullableFloat(data["energyConsumption"]);

        // Fuel (oilEndurance sentinel -1 => null)
        var rawOilEndurance = ToNullableFloat(data["oilEndurance"]);
        OilEndurance = (rawOilEndurance != null && rawOilEndurance < 0) ? null : rawOilEndurance;
        OilPercent = ToNullableFloat(data["oilPercent"]);
        TotalOil = ToNullableFloat(data["totalOil"]);

        // System indicators (ectValue sentinel -1 => null)
        PowerSystem = ToNullableInt(data["powerSystem"]);
        EngineStatus = ToNullableInt(data["engineStatus"]);
        Epb = ToNullableInt(data["epb"]);
        Eps = ToNullableInt(data["eps"]);
        Esp = ToNullableInt(data["esp"]);
        AbsWarning = ToNullableInt(data["absWarning"]);
        Svs = ToNullableInt(data["svs"]);
        Srs = ToNullableInt(data["srs"]);
        Ect = ToNullableInt(data["ect"]);

        var rawEctValue = ToNullableInt(data["ectValue"]);
        EctValue = (rawEctValue != null && rawEctValue < 0) ? null : rawEctValue;

        Pwr = ToNullableInt(data["pwr"]);

        // Feature states
        SentryStatus = ToNullableInt(data["sentryStatus"]);
        BatteryHeatState = ToNullableInt(data["batteryHeatState"]);
        ChargeHeatState = ToNullableInt(data["chargeHeatState"]);
        UpgradeStatus = ToNullableInt(data["upgradeStatus"]);

        // Metadata timestamp
        if(data.TryGetValue("timestamp", out var tsVal) && tsVal != null)
        {
            Timestamp = ParseTimestamp(tsVal);
        }
    }

    // --- parseTimestamp equivalent ---
    private static DateTimeOffset? ParseTimestamp(object timestamp)
    {
        if(timestamp == null) return null;
        if(!long.TryParse(timestamp.ToString(), out var ts)) return null;

        // PHP code: if >= 1_000_000_000_000 treat as ms and divide by 1000
        if(ts >= 1_000_000_000_000L) ts = ts / 1000;
        try
        {
            return DateTimeOffset.FromUnixTimeSeconds(ts);
        }
        catch
        {
            return null;
        }
    }

    // --- isReadyRaw equivalent ---
    public static bool IsReadyRaw(IDictionary<string, object?>? vehicleInfo)
    {
        if(vehicleInfo == null || vehicleInfo.Count == 0) return false;

        if(vehicleInfo.TryGetValue("onlineState", out var os) && int.TryParse(os?.ToString(), out var osInt) && osInt == 2)
            return false;

        var tireFields = new[]
        {
                "leftFrontTirepressure",
                "rightFrontTirepressure",
                "leftRearTirepressure",
                "rightRearTirepressure"
            };

        foreach(var field in tireFields)
        {
            if(vehicleInfo.TryGetValue(field, out var val) && float.TryParse(val?.ToString(), out var f) && f > 0)
                return true;
        }

        if(vehicleInfo.TryGetValue("time", out var timeVal) && int.TryParse(timeVal?.ToString(), out var t) && t > 0)
            return true;

        return vehicleInfo.TryGetValue("enduranceMileage", out var em) && float.TryParse(em?.ToString(), out var emf) && emf > 0;
    }

    // --- Convenience helpers ---
    public bool IsOnline() => OnlineState == OnlineState.ONLINE;
    public bool IsCharging() => ChargingState == ChargingState.CHARGING;
    public int? GetTimeToFullMinutes()
    {
        if(FullHour == null || FullMinute == null) return null;
        return FullHour * 60 + FullMinute;
    }
    public bool IsInteriorTempAvailable() => TempInCar != null;

    public bool? IsLocked()
    {
        var locks = new[] { LeftFrontDoorLock, RightFrontDoorLock, LeftRearDoorLock, RightRearDoorLock };
        var skip = new[] { LockState.UNKNOWN, LockState.UNAVAILABLE };
        var known = locks.Where(l => !skip.Contains(l)).ToArray();
        if(known.Length == 0) return null;
        return known.Count(l => l == LockState.LOCKED) == known.Length;
    }

    public bool IsAnyDoorOpen()
    {
        var doors = new[] { LeftFrontDoor, RightFrontDoor, LeftRearDoor, RightRearDoor, TrunkLid, SlidingDoor, Forehold };
        return doors.Any(d => d == DoorOpenState.OPEN);
    }

    public bool IsAnyWindowOpen()
    {
        var windows = new[] { LeftFrontWindow, RightFrontWindow, LeftRearWindow, RightRearWindow, Skylight };
        return windows.Any(w => w == WindowState.OPEN);
    }

    public bool IsVehicleOn() => PowerGear == PowerGear.ON;
    public bool? IsBatteryHeating() => BatteryHeatState == null ? null : (bool?)(BatteryHeatState != 0);
    public bool IsSteeringWheelHeating() => SteeringWheelHeatState == StearingWheelHeat.ON;
}
