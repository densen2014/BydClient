using System;
using System.Collections.Generic;
using System.Text;

namespace BydClient.Models;

using System;
using System.Collections.Generic;

    public class RemoteControlResult : BaseModel
    {
        public string ResultCode { get; set; } = string.Empty;
        public string ResultMsg { get; set; } = string.Empty;
        public string? SerialNumber { get; private set; }

        public DateTimeOffset? Timestamp { get; private set; }

        public string? CommandType { get; private set; } = null;
        public string? Vin { get; private set; }

        public string? Uuid { get; private set; }
        public int? Result { get; private set; }

        /// <summary>
        /// Response data as a dictionary. If original payload uses a different shape, adapt parsing before Populate.
        /// </summary>
        public IDictionary<string, object?> ResponseData { get; private set; } = new Dictionary<string, object?>();
    public int? ControlState { get; private set; }

    public RemoteControlResult(IDictionary<string, object?> data) : base(data) { }

        protected override void Populate(IDictionary<string, object?> data)
        {
            if(data == null) throw new ArgumentNullException(nameof(data));

            ResultCode = GetValue(data, "resultCode", "code")?.ToString() ?? "0";
            ResultMsg = GetValue(data, "resultMsg", "message", "msg")?.ToString() ?? string.Empty;
            SerialNumber = GetValue(data, "serialNumber", "requestSerial")?.ToString();
            CommandType = GetValue(data, "commandType")?.ToString();
            Vin = GetValue(data, "vin")?.ToString();
            Uuid = GetValue(data, "uuid")?.ToString();
            ControlState = ToNullableInt(GetValue(data, "controlState"));
            Result = ToNullableInt(GetValue(data, "res", "result"));

            if(data.TryGetValue("timestamp", out var tsVal) && tsVal != null)
            {
                Timestamp = ParseTimestamp(tsVal);
            }

        // responseData may be an array/object; try to cast to dictionary, otherwise keep empty
        //var resp = data.GetValueOrDefault("responseData");
        //if(resp is IDictionary<string, object?> dict)
        //{
        //    ResponseData = dict;
        //}
        //else if(resp is IDictionary<string, object> dict2)
        //{
        //    // handle non-nullable variant
        //    var tmp = new Dictionary<string, object?>();
        //    foreach(var kv in dict2) tmp[kv.Key] = kv.Value;
        //    ResponseData = tmp;
        //}
        //else
        //{
        //    // If responseData is JSON-like string, try to parse it externally before calling Populate.
        //    ResponseData = new Dictionary<string, object?>();
        //}

        ResponseData = GetValue(data, "responseData") as IDictionary<string, object?> ?? new Dictionary<string, object?>();
    }

        private static object? GetValue(IDictionary<string, object?> data, params string[] keys)
        {
            foreach(var key in keys)
            {
                if(data.TryGetValue(key, out var value))
                    return value;
            }

            return null;
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

        private static int? ToNullableInt(object? v)
        {
            if(v == null) return null;
            return int.TryParse(v.ToString(), out var i) ? i : null;
        }

        // Factory helper
        public static RemoteControlResult FromDictionary(IDictionary<string, object?> data)
        {
            var inst = new RemoteControlResult(data);
            //inst.Populate(data);
            return inst;
        }

        public bool IsSuccess()
        {
            var codeSucceeded = string.Equals(ResultCode, "success", StringComparison.OrdinalIgnoreCase)
                                || ResultCode == "0";

            return codeSucceeded && ControlState != 2 && (Result == null || Result >= 2);
        }
    }
