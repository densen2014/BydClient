using System;
using System.Collections.Generic;
using System.Text;

namespace BydClient.Models.Control;

/// <summary>
/// Climate start parameters.
/// </summary>
public class ClimateStartParams : IControlParams
{
    public int? Temperature { get; private set; }
    public bool AcOn { get; private set; } = false;
    public bool Heating { get; private set; } = false;
    public bool Defrost { get; private set; } = false;
    public bool FrontDefrost { get; private set; } = false;
    public bool RearDefrost { get; private set; } = false;

    public ClimateStartParams(
        int? temperature = null,
        bool acOn = true,
        bool heating = false,
        bool defrost = false,
        bool frontDefrost = false,
        bool rearDefrost = false)
    {
        if (temperature is < 16 or > 32)
            throw new ArgumentOutOfRangeException(nameof(temperature), "Temperature must be between 16 and 32 Celsius.");

        Temperature = temperature;
        AcOn = acOn;
        Heating = heating;
        Defrost = defrost;
        FrontDefrost = frontDefrost;
        RearDefrost = rearDefrost;
    }

    /// <summary>
    /// Convert to control parameters map.
    /// </summary>
    public IDictionary<string, string?> ToControlParamsMap()
    {
        var paramsMap = new Dictionary<string, string?>();

        if(Temperature.HasValue)
        {
            paramsMap["temperature"] = Temperature.Value.ToString();
        }

        paramsMap["acOn"] = AcOn ? "1" : "0";
        paramsMap["heating"] = Heating ? "1" : "0";
        paramsMap["defrost"] = Defrost ? "1" : "0";
        paramsMap["frontDefrost"] = FrontDefrost ? "1" : "0";
        paramsMap["rearDefrost"] = RearDefrost ? "1" : "0";

        return paramsMap;
    }
}
