using BydClient.Config;
using BydClient.Models;
using BydClient.Transport;

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
    }
