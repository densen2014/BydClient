using BydClient.Config;
using BydClient.Models;
using BydClient.Transport;
using System.Text.Json;

namespace BydClient.Api;

public static class VehicleApi
{
    /// <summary>
    /// Fetch all vehicles associated with the authenticated user.
    /// </summary>
    public static async Task<List<Vehicle>> FetchVehicleListAsync(BydConfig config, Session session, ITransport transport, CancellationToken cancellationToken = default)
    {
        if(config == null) throw new ArgumentNullException(nameof(config));
        if(session == null) throw new ArgumentNullException(nameof(session));
        if(transport == null) throw new ArgumentNullException(nameof(transport));

        var inner = Common.BuildInnerBase(config);
        return await TokenJson.PostTokenJsonAsync<List<Vehicle>>(
            "/app/account/getAllListByUserId",
            config,
            session,
            transport,
            inner) ?? [];
    }

    /// <summary>
    /// Fetch the complete decrypted vehicle-list response without discarding unknown fields.
    /// </summary>
    public static async Task<Dictionary<string, object>?> FetchVehicleListRawAsync(BydConfig config, Session session, ITransport transport, CancellationToken cancellationToken = default)
    {
        if(config == null) throw new ArgumentNullException(nameof(config));
        if(session == null) throw new ArgumentNullException(nameof(session));
        if(transport == null) throw new ArgumentNullException(nameof(transport));

        var inner = Common.BuildInnerBase(config);
        return await TokenJson.PostTokenJsonAsync(
            "/app/account/getAllListByUserId",
            config,
            session,
            transport,
            inner);
    }

    /// <summary>
    /// Fetch the complete decrypted latest configuration for the requested vehicles.
    /// </summary>
    public static async Task<Dictionary<string, object>?> FetchLatestConfigsRawAsync(
        BydConfig config,
        Session session,
        ITransport transport,
        IEnumerable<string> vins,
        CancellationToken cancellationToken = default)
    {
        if(config == null) throw new ArgumentNullException(nameof(config));
        if(session == null) throw new ArgumentNullException(nameof(session));
        if(transport == null) throw new ArgumentNullException(nameof(transport));
        if(vins == null) throw new ArgumentNullException(nameof(vins));

        var vinList = vins.Where(vin => !string.IsNullOrWhiteSpace(vin)).Distinct().ToArray();
        if(vinList.Length == 0) throw new ArgumentException("At least one VIN is required.", nameof(vins));

        var inner = Common.BuildInnerBase(config);
        inner["appConfigVersion"] = "2";
        inner["terminalType"] = "0";
        inner["vinList"] = JsonSerializer.Serialize(vinList);

        return await TokenJson.PostTokenJsonAsync(
            "/vehicle/vehicleswitch/getLatestConfig",
            config,
            session,
            transport,
            inner);
    }
}
