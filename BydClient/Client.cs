using BydClient.Api;
using BydClient.Config;
using BydClient.Crypto;
using BydClient.Exceptions;
using BydClient.Models;
using BydClient.Models.Control;
using BydClient.Transport;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace BydClient;

/// <summary>
/// Main client for the BYD vehicle API.
/// </summary>
public class Client : IDisposable
{
    private HttpClient? _httpClient;
    private BangcleCodec? _codec;
    private SecureTransport? _transport;
    private Session? _session;
    private readonly BydConfig _config;
    private readonly ILogger<SecureTransport> _logger;
    private bool _disposed;

    public Client(BydConfig config, ILogger<SecureTransport>? logger = null)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? NullLogger<SecureTransport>.Instance;
    }

    /// <summary>
    /// Initialise the client transport and codec.
    /// Called automatically by constructor, but can also be invoked directly
    /// when the lifecycle is managed manually.
    /// </summary>
    public void Init()
    {
        if(_httpClient == null)
        {
            var handler = new HttpClientHandler
            {
                UseCookies = true
            };
            _httpClient = new HttpClient(handler);
        }

        if(_codec == null)
        {
            // Try to find the tables file in the package data directory
            var defaultPath = Path.Combine(AppContext.BaseDirectory, "Data", "bangcle_tables.bin");
            _codec = new BangcleCodec(File.Exists(defaultPath) ? defaultPath : null);
        }

        if(_transport == null)
        {
            if(_httpClient == null || _codec == null)
                throw new BydException("HTTP client or codec not initialized");

            _transport = new SecureTransport(_config, _codec, _httpClient, _logger);
        }
    }

    /// <summary>
    /// Authenticate against the BYD API and obtain session tokens.
    /// </summary>
    public async Task LoginAsync(CancellationToken cancellationToken = default)
    {
        // Ensure transport is initialized
        if(_transport == null)
        {
            Init();
        }

        long nowMs = (long)(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        var outer = Login.BuildLoginRequest(_config, nowMs);

        if(_transport == null)
        {
            throw new BydException("Transport not initialized");
        }

        var response = await _transport.PostSecureAsync("/app/account/login", outer, cancellationToken);
        var token = Login.ParseLoginResponse(response, _config.Password);

        int ttl = (int)(_config.SessionTtl > 0 ? _config.SessionTtl : int.MaxValue);
        _session = new Session(token.UserId, token.SignToken, token.EncryToken, ttl);
    }

    /// <summary>
    /// Return an active session, re-authenticating if expired.
    /// </summary>
    public async Task<Session> EnsureSessionAsync(CancellationToken cancellationToken = default)
    {
        if(_session != null && !_session.IsExpired())
        {
            return _session;
        }

        await LoginAsync(cancellationToken);

        if(_session == null)
        {
            throw new BydException("Failed to create session");
        }

        return _session;
    }

    /// <summary>
    /// Force session invalidation (next call will re-authenticate).
    /// </summary>
    public void InvalidateSession()
    {
        _session = null;
    }

    /// <summary>
    /// Fetch all vehicles associated with the account.
    /// </summary>
    public async Task<IReadOnlyList<Vehicle>> GetVehiclesAsync(CancellationToken cancellationToken = default)
    {
        var session = await EnsureSessionAsync(cancellationToken);
        var transport = RequireTransport();

        return await VehicleApi.FetchVehicleListAsync(_config, session, transport, cancellationToken);
    }

    /// <summary>
    /// Trigger + wait for realtime vehicle data.
    /// </summary>
    /// <param name="vin">Vehicle identification number</param>
    /// <param name="pollAttempts">Number of polling attempts before giving up</param>
    /// <param name="pollInterval">Interval between polling attempts in seconds</param>
    /// <param name="timeout">Maximum time to wait for data in seconds</param>
    public async Task<VehicleRealtimeData> GetVehicleRealtimeAsync(
        string vin,
        int pollAttempts = 10,
        double pollInterval = 1.5,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        var session = await EnsureSessionAsync(cancellationToken);
        var transport = RequireTransport();

        // Phase 1: Trigger
        var (triggerInfo, serial) = await RealtimeApi.FetchRealtimeEndpointAsync(
            "/vehicleInfo/vehicle/vehicleRealTimeRequest",
            _config,
            session,
            transport,
            vin,
            null,
            cancellationToken);

        var mergedLatest = triggerInfo;

        if(triggerInfo != null && VehicleRealtimeData.IsReadyRaw(triggerInfo))
        {
            return new VehicleRealtimeData(triggerInfo);
        }

        if(serial == null)
        {
            return new VehicleRealtimeData(mergedLatest);
        }

        // Phase 2: Poll for results
        using var cts = timeout.HasValue ? new CancellationTokenSource(timeout.Value) : null;
        var linkedToken = cts != null
            ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token).Token
            : cancellationToken;

        for(int attempt = 1; attempt <= pollAttempts; attempt++)
        {
            if(pollInterval > 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(pollInterval), linkedToken);
            }

            try
            {
                var (latest, newSerial) = await RealtimeApi.FetchRealtimeEndpointAsync(
                    "/vehicleInfo/vehicle/vehicleRealTimeResult",
                    _config,
                    session,
                    transport,
                    vin,
                    serial, linkedToken);

                if(latest != null)
                {
                    mergedLatest = latest;
                }

                if(VehicleRealtimeData.IsReadyRaw(latest))
                {
                    break;
                }

                serial = newSerial;
            }
            catch(BydException)
            {
                // Continue polling on API errors
            }
        }

        return new VehicleRealtimeData(mergedLatest);
    }

    /// <summary>
    /// Trigger + wait for GPS info.
    /// </summary>
    public async Task<GpsInfo> GetGpsInfoAsync(
        string vin,
        int pollAttempts = 10,
        double pollInterval = 1.5,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        var session = await EnsureSessionAsync(cancellationToken);
        var transport = RequireTransport();

        // Phase 1: Trigger
        var (triggerInfo, serial) = await GpsApi.FetchGpsEndpointAsync(
            "/control/getGpsInfo",
            _config,
            session,
            transport,
            vin,
            null,
            cancellationToken);

        var mergedLatest = triggerInfo;

        if(triggerInfo != null && GpsInfo.IsGpsInfoReady(triggerInfo))
        {
            return new GpsInfo(triggerInfo);
        }

        if(serial == null)
        {
            return new GpsInfo(mergedLatest);
        }

        // Phase 2: Poll for results
        using var cts = timeout.HasValue ? new CancellationTokenSource(timeout.Value) : null;
        var linkedToken = cts != null
            ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token).Token
            : cancellationToken;

        for(int attempt = 1; attempt <= pollAttempts; attempt++)
        {
            if(pollInterval > 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(pollInterval), linkedToken);
            }

            try
            {
                var (latest, newSerial) = await GpsApi.FetchGpsEndpointAsync(
                    "/control/getGpsInfoResult",
                    _config,
                    session,
                    transport,
                    vin,
                    serial,
                    linkedToken);

                if(latest != null)
                {
                    mergedLatest = latest;
                }

                if(GpsInfo.IsGpsInfoReady(latest!))
                {
                    break;
                }

                serial = newSerial;
            }
            catch(BydException)
            {
                // Continue polling on API errors
            }
        }

        return new GpsInfo(mergedLatest);
    }

    /// <summary>
    /// Fetch HVAC / climate status.
    /// </summary>
    public async Task<HvacStatus> GetHvacStatusAsync(string vin, CancellationToken cancellationToken = default)
    {
        var session = await EnsureSessionAsync(cancellationToken);
        var transport = RequireTransport();

        return await HvacApi.FetchHvacStatusAsync(_config, session, transport, vin, cancellationToken);
    }

    /// <summary>
    /// Fetch charging status.
    /// </summary>
    public async Task<ChargingStatus> GetChargingStatusAsync(string vin, CancellationToken cancellationToken = default)
    {
        var session = await EnsureSessionAsync(cancellationToken);
        var transport = RequireTransport();

        return await ChargingApi.FetchChargingStatusAsync(_config, session, transport, vin, cancellationToken);
    }

    /// <summary>
    /// Fetch energy consumption data.
    /// </summary>
    public async Task<EnergyConsumption> GetEnergyConsumptionAsync(string vin, CancellationToken cancellationToken = default)
    {
        var session = await EnsureSessionAsync(cancellationToken);
        var transport = RequireTransport();

        return await EnergyApi.FetchEnergyConsumptionAsync(_config, session, transport, vin, cancellationToken);
    }

    /// <summary>
    /// Fetch push notification state.
    /// </summary>
    public async Task<PushNotificationState> GetPushStateAsync(string vin, CancellationToken cancellationToken = default)
    {
        var session = await EnsureSessionAsync(cancellationToken);
        var transport = RequireTransport();

        return await PushNotificationsApi.FetchPushStateAsync(_config, session, transport, vin, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Enable or disable push notifications.
    /// </summary>
    public async Task<CommandAck> SetPushStateAsync(string vin, bool enable, CancellationToken cancellationToken = default)
    {
        var session = await EnsureSessionAsync(cancellationToken);
        var transport = RequireTransport();

        return await PushNotificationsApi.SetPushStateAsync(_config, session, transport, vin, enable, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Verify the remote control PIN.
    /// </summary>
    public async Task<VerifyControlPasswordResponse> VerifyControlPasswordAsync(
        string vin,
        string? commandPwd = null,
        CancellationToken cancellationToken = default)
    {
        var resolvedPwd = ResolveCommandPwd(commandPwd);
        var session = await EnsureSessionAsync(cancellationToken);
        var transport = RequireTransport();

        return await ControlApi.VerifyControlPasswordAsync(_config, session, transport, vin, resolvedPwd, cancellationToken);
    }

    /// <summary>
    /// Lock the vehicle.
    /// </summary>
    public async Task<RemoteControlResult> LockAsync(
        string vin,
        string? commandPwd = null,
        CancellationToken cancellationToken = default)
    {
        var resolvedPwd = ResolveCommandPwd(commandPwd);
        var session = await EnsureSessionAsync(cancellationToken);
        var transport = RequireTransport();

        return await ControlApi.PollRemoteControlAsync(_config, session, transport, vin, "LOCKDOOR", null,
            resolvedPwd, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Unlock the vehicle.
    /// </summary>
    public async Task<RemoteControlResult> UnlockAsync(
        string vin,
        string? commandPwd = null,
        CancellationToken cancellationToken = default)
    {
        var resolvedPwd = ResolveCommandPwd(commandPwd);
        var session = await EnsureSessionAsync(cancellationToken);
        var transport = RequireTransport();

        return await ControlApi.PollRemoteControlAsync(_config, session, transport, vin, "OPENDOOR", null,
            resolvedPwd, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Start climate control with the given parameters.
    /// </summary>
    public async Task<RemoteControlResult> StartClimateAsync(
        string vin,
        ClimateStartParams @params,
        string? commandPwd = null,
        CancellationToken cancellationToken = default)
    {
        var resolvedPwd = ResolveCommandPwd(commandPwd);
        var session = await EnsureSessionAsync(cancellationToken);
        var transport = RequireTransport();

        return await ControlApi.PollRemoteControlAsync(_config, session, transport, vin, "OPENAIR", @params.ToControlParamsMap(),
            resolvedPwd, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Stop climate control.
    /// </summary>
    public async Task<RemoteControlResult> StopClimateAsync(
        string vin,
        string? commandPwd = null,
        CancellationToken cancellationToken = default)
    {
        var resolvedPwd = ResolveCommandPwd(commandPwd);
        var session = await EnsureSessionAsync(cancellationToken);
        var transport = RequireTransport();

        return await ControlApi.PollRemoteControlAsync(_config, session, transport, vin, "CLOSEAIR", null,
            resolvedPwd, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Flash vehicle lights.
    /// </summary>
    public async Task<RemoteControlResult> FlashLightsAsync(
        string vin,
        string? commandPwd = null,
        CancellationToken cancellationToken = default)
    {
        var resolvedPwd = ResolveCommandPwd(commandPwd);
        var session = await EnsureSessionAsync(cancellationToken);
        var transport = RequireTransport();

        return await ControlApi.PollRemoteControlAsync(_config, session, transport, vin, "FLASHLIGHTNOWHISTLE", null,
            resolvedPwd, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Close all windows.
    /// </summary>
    public async Task<RemoteControlResult> CloseWindowsAsync(
        string vin,
        string? commandPwd = null,
        CancellationToken cancellationToken = default)
    {
        var resolvedPwd = ResolveCommandPwd(commandPwd);
        var session = await EnsureSessionAsync(cancellationToken);
        var transport = RequireTransport();

        return await ControlApi.PollRemoteControlAsync(_config, session, transport, vin, "CLOSEWINDOW", null,
            resolvedPwd, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Activate find-my-car (horn + lights).
    /// </summary>
    public async Task<RemoteControlResult> FindCarAsync(
        string vin,
        string? commandPwd = null,
        CancellationToken cancellationToken = default)
    {
        var resolvedPwd = ResolveCommandPwd(commandPwd);
        var session = await EnsureSessionAsync(cancellationToken);
        var transport = RequireTransport();

        return await ControlApi.PollRemoteControlAsync(_config, session, transport, vin, "FINDCAR", null,
            resolvedPwd, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Schedule climate control.
    /// </summary>
    public async Task<RemoteControlResult> ScheduleClimateAsync(
        string vin,
        ClimateScheduleParams @params,
        string? commandPwd = null,
        CancellationToken cancellationToken = default)
    {
        var resolvedPwd = ResolveCommandPwd(commandPwd);
        var session = await EnsureSessionAsync(cancellationToken);
        var transport = RequireTransport();

        return await ControlApi.PollRemoteControlAsync(_config, session, transport, vin, "BOOKINGAIR", @params.ToControlParamsMap(),
            resolvedPwd, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Set seat heating/ventilation.
    /// </summary>
    public async Task<RemoteControlResult> SetSeatClimateAsync(
        string vin,
        SeatClimateParams @params,
        string? commandPwd = null,
        CancellationToken cancellationToken = default)
    {
        var resolvedPwd = ResolveCommandPwd(commandPwd);
        var session = await EnsureSessionAsync(cancellationToken);
        var transport = RequireTransport();

        return await ControlApi.PollRemoteControlAsync(_config, session, transport, vin, "VENTILATIONHEATING", @params.ToControlParamsMap(),
            resolvedPwd, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Enable or disable battery heating.
    /// </summary>
    public async Task<RemoteControlResult> SetBatteryHeatAsync(
        string vin,
        BatteryHeatParams @params,
        string? commandPwd = null,
        CancellationToken cancellationToken = default)
    {
        var resolvedPwd = ResolveCommandPwd(commandPwd);
        var session = await EnsureSessionAsync(cancellationToken);
        var transport = RequireTransport();

        return await ControlApi.PollRemoteControlAsync(_config, session, transport, vin, "BATTERYHEAT", @params.ToControlParamsMap(),
            resolvedPwd, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Save a smart charging schedule.
    /// </summary>
    public async Task<CommandAck> SaveChargingScheduleAsync(
        string vin,
        SmartChargingSchedule schedule,
        CancellationToken cancellationToken = default)
    {
        if(schedule.TargetSoc == null ||
            schedule.StartHour == null ||
            schedule.StartMinute == null ||
            schedule.EndHour == null ||
            schedule.EndMinute == null)
        {
            throw new BydException("SmartChargingSchedule must have all time fields set");
        }

        var session = await EnsureSessionAsync(cancellationToken);
        var transport = RequireTransport();

        return await SmartChargingApi.SaveChargingScheduleAsync(
            _config,
            session,
            transport,
            vin,
            schedule.TargetSoc.Value,
            schedule.StartHour.Value,
            schedule.StartMinute.Value,
            schedule.EndHour.Value,
            schedule.EndMinute.Value,
            cancellationToken);
    }

    /// <summary>
    /// Enable or disable smart charging.
    /// </summary>
    public async Task<CommandAck> ToggleSmartChargingAsync(
        string vin,
        bool enable,
        CancellationToken cancellationToken = default)
    {
        var session = await EnsureSessionAsync(cancellationToken);
        var transport = RequireTransport();

        return await SmartChargingApi.ToggleSmartChargingAsync(_config, session, transport, vin, enable, cancellationToken);
    }

    /// <summary>
    /// Rename a vehicle.
    /// </summary>
    public async Task<CommandAck> RenameVehicleAsync(
        string vin,
        string name,
        CancellationToken cancellationToken = default)
    {
        var session = await EnsureSessionAsync(cancellationToken);
        var transport = RequireTransport();

        return await VehicleSettingsApi.RenameVehicleAsync(_config, session, transport, vin, name, cancellationToken);
    }

    /// <summary>
    /// Get the transport, throwing if not initialized.
    /// </summary>
    private SecureTransport RequireTransport()
    {
        if(_transport == null)
        {
            throw new BydException("Client not initialized. Call Init() first.");
        }

        return _transport;
    }

    /// <summary>
    /// Normalize control password (uppercase MD5 hex of PIN).
    /// </summary>
    private string ResolveCommandPwd(string? commandPwd)
    {
        if(commandPwd != null)
        {
            var stripped = commandPwd.Trim();
            if(stripped.Length == 32 && IsHexString(stripped))
            {
                return stripped.ToUpperInvariant();
            }

            return ComputeMd5(stripped).ToUpperInvariant();
        }

        var pin = _config.ControlPin;
        if(pin != null)
        {
            return ComputeMd5(pin).ToUpperInvariant();
        }

        return string.Empty;
    }

    /// <summary>
    /// Compute MD5 hash and return as uppercase hex string.
    /// </summary>
    public static string ComputeMd5(string input)
    {
        using var md5 = MD5.Create();
        var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
        return BitConverter.ToString(hash).Replace("-", "").ToUpperInvariant();
    }

    /// <summary>
    /// Check if a string contains only hexadecimal characters.
    /// </summary>
    private static bool IsHexString(string value)
    {
        foreach(char c in value)
        {
            if(!((c >= '0' && c <= '9') || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f')))
            {
                return false;
            }
        }
        return true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if(!_disposed)
        {
            if(disposing)
            {
                _httpClient?.Dispose();
            }

            _disposed = true;
        }
    }
}
