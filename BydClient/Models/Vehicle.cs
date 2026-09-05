using System.Text.Json;
using System.Text.Json.Serialization;

namespace BydClient.Models;

/// <summary>
/// A vehicle associated with the user's account.
/// </summary>
public sealed class Vehicle
{
    public string AutoAlias { get; set; } = string.Empty;

    [JsonPropertyName("autoBoughtTime")]
    public long? AutoBoughtTimestamp { get; set; }

    public string AutoPlate { get; set; } = string.Empty;
    public string AutoUpgradeCheck { get; set; } = string.Empty;
    public JsonElement? BluetoothInfo { get; set; }
    public int? BrandId { get; set; }
    public string BrandName { get; set; } = string.Empty;
    public int? CarType { get; set; }
    public VehiclePicture? CfPic { get; set; }
    public string CloudServiceStatue { get; set; } = string.Empty;
    public string CrmModelId { get; set; } = string.Empty;
    public string CrmStyleId { get; set; } = string.Empty;
    public string DealerRegionCode { get; set; } = string.Empty;

    [JsonPropertyName("defaultCar")]
    public int? DefaultCarValue { get; set; }

    public int? EmpowerType { get; set; }
    public string EnergyType { get; set; } = string.Empty;
    public int? ModelId { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public string NotUpgradeNowSwitch { get; set; } = string.Empty;
    public bool OpenCloudServiceStatue { get; set; }
    public string OutModelType { get; set; } = string.Empty;
    public int? PermissionStatus { get; set; }
    public List<EmpowerRange> RangeDetailList { get; set; } = [];
    public int? ResetPwdState { get; set; }
    public string SupportOtaNightUpgrade { get; set; } = string.Empty;
    public string TboxVersion { get; set; } = string.Empty;
    public double? TotalMileage { get; set; }
    public string UserManualUrl { get; set; } = string.Empty;
    public VehicleFunctionLearnInfo? VehicleFunLearnInfo { get; set; }
    public JsonElement? VehicleMqttLearnInfo { get; set; }
    public string VehicleState { get; set; } = string.Empty;
    public string VehicleTimeZone { get; set; } = string.Empty;
    public string VehicleType { get; set; } = string.Empty;
    public string Vin { get; set; } = string.Empty;

    [JsonPropertyName("yunActiveTime")]
    public long? YunActiveTimestamp { get; set; }

    [JsonIgnore]
    public bool DefaultCar => DefaultCarValue == 1;

    [JsonIgnore]
    public DateTime? AutoBoughtTime => ToDateTime(AutoBoughtTimestamp);

    [JsonIgnore]
    public DateTime? YunActiveTime => ToDateTime(YunActiveTimestamp);

    [JsonIgnore]
    public string PicMainUrl => CfPic?.PicMainUrl ?? string.Empty;

    [JsonIgnore]
    public string PicSetUrl => CfPic?.PicSetUrl ?? string.Empty;

    public EmpowerRange[] GetRangeDetailList() => RangeDetailList.ToArray();

    public bool IsShared() => EmpowerType.HasValue && EmpowerType.Value < 0;

    private static DateTime? ToDateTime(long? timestamp)
    {
        if(!timestamp.HasValue)
            return null;

        try
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(timestamp.Value).LocalDateTime;
        }
        catch(ArgumentOutOfRangeException)
        {
            return null;
        }
    }
}

public sealed class VehiclePicture
{
    public string ClrCode { get; set; } = string.Empty;
    public int? Flag { get; set; }
    public string PicDoorZipUrl { get; set; } = string.Empty;
    public string PicFrontUrl { get; set; } = string.Empty;
    public string PicMainUrl { get; set; } = string.Empty;
    public string PicRatioUrl { get; set; } = string.Empty;
    public string PicSetUrl { get; set; } = string.Empty;
    public string PicTireUrl { get; set; } = string.Empty;
}

public sealed class VehicleFunctionLearnInfo
{
    public int? AcCurrentFunctionLimitLearnInfo { get; set; }
    public int? AcCurrentLimit499LearnInfo { get; set; }
    public int? AcCurrentLimitLearnInfo { get; set; }
    public int? AirAccuracy { get; set; }
    public int? AirRange { get; set; }
    public int? BatteryHeating { get; set; }
    public int? BatteryHeating245 { get; set; }
    public int? BatteryHeating499 { get; set; }
    public int? BookingCar { get; set; }
    public int? BookingCharge { get; set; }
    public int? BucketSeat { get; set; }
    public int? Can2F4LearnInfo { get; set; }
    public int? Can433LearnInfo { get; set; }
    public int? CarCloudLearnInfo { get; set; }
    public int? ChargeFullLearnDmInfo { get; set; }
    public int? ChargeFullLearnInfo { get; set; }
    public int? ChargeFullLearnInfo499 { get; set; }
    public int? ChargeHeating245 { get; set; }
    public int? ChargeHeating499 { get; set; }
    public int? ChargingHeating { get; set; }
    public int? DomainControlLearnInfo { get; set; }
    public int? EleDefrost396LearnInfo { get; set; }
    public int? EleDefrost499LearnInfo { get; set; }
    public int? EleDefrostLearnInfo { get; set; }
    public int? EleDefrostSignalLearnInfo { get; set; }
    public int? EnergyLearnInfo { get; set; }
    public int? FrontDefrostLearnInfo { get; set; }
    public int? GpsLearnInfo { get; set; }
    public int? HighPressure499LearnInfo { get; set; }
    public int? LightStopPadConfigLearnInfo { get; set; }
    public int? LightStopVehConfigLearnInfo { get; set; }
    public int? NavigationLearnInfo { get; set; }
    public int? NfcDigitalLearnInfo { get; set; }
    public int? NfcLearnInfo { get; set; }
    public int? NfcUwbSwLearnInfo { get; set; }
    public int? OpenWindow499LearnInfo { get; set; }
    public int? OpenWindowLearnInfo { get; set; }
    public int? OpenWindowSignalLearnInfo { get; set; }
    public int? OtaUpgrade { get; set; }
    public int? RapidTempUpAndDown { get; set; }
    public int? RefrigeratorLearnInfo { get; set; }
    public int? RefrigeratorModeLearnInfo { get; set; }
    public int? RefrigeratorTempLearnColdInfo { get; set; }
    public int? RudderType { get; set; }
    public int? SentryStatusLearnInfo { get; set; }
    public int? SteeringWheelHeating { get; set; }
    public int? TrunkLearnInfo { get; set; }
    public int? UpperLowerTailgateLearnInfo { get; set; }
    public int? VehicleSafetyVerified { get; set; }
    public int? VehicleStatusPushLearnInfo { get; set; }
    public int? WiperHeat499LearnInfo { get; set; }
    public int? WiperHeatLearnInfo { get; set; }
    public int? WiperHeatSignalLearnInfo { get; set; }
}