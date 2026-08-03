using System;
using System.Collections.Generic;
using System.Reflection;
using Sentry.Extensibility;

namespace Sentry.Unity.Metrics;

/// <summary>
/// Periodically emits basic multiplayer network metrics for Netcode for GameObjects (NGO): the
/// round-trip time to the server (on clients) and the connected-client count (on the server).
/// <para>
/// NGO is an optional package, so it is accessed via reflection to avoid a hard dependency. Only
/// low-cost, already-cached values are read (RTT and client count); no packet/byte instrumentation
/// is enabled, so there is no measurable runtime overhead. When NGO is not present, or no session
/// is running, the monitor is inert.
/// </para>
/// </summary>
internal class NetworkMetricsMonitor : IGameMetricMonitor
{
    internal const string PingMetric = "game.perf.net.ping";
    internal const string NumClientsMetric = "game.perf.net.num_clients";

    // In NGO the server is always client id 0.
    private const ulong ServerClientId = 0;

    private readonly GameMetricAttributes _attributes;
    private readonly IDiagnosticLogger? _logger;

    private readonly PropertyInfo? _singletonProperty;
    private readonly PropertyInfo? _isListeningProperty;
    private readonly PropertyInfo? _isServerProperty;
    private readonly PropertyInfo? _connectedClientsIdsProperty;
    private readonly PropertyInfo? _networkConfigProperty;

    // Resolved lazily - the concrete transport type depends on the configured transport.
    private PropertyInfo? _networkTransportProperty;
    private bool _loggedMissingRtt;

    public NetworkMetricsMonitor(SentryUnityOptions options)
    {
        _attributes = options.GameMetricAttributes;
        _logger = options.DiagnosticLogger;

        var networkManagerType = Type.GetType("Unity.Netcode.NetworkManager, Unity.Netcode.Runtime");
        if (networkManagerType is null)
        {
            _logger?.LogInfo("Network metrics are enabled but Netcode for GameObjects was not found - skipping.");
            return;
        }

        _singletonProperty = networkManagerType.GetProperty("Singleton", BindingFlags.Public | BindingFlags.Static);
        _isListeningProperty = networkManagerType.GetProperty("IsListening");
        _isServerProperty = networkManagerType.GetProperty("IsServer");
        _connectedClientsIdsProperty = networkManagerType.GetProperty("ConnectedClientsIds");
        _networkConfigProperty = networkManagerType.GetProperty("NetworkConfig");
    }

    internal bool IsAvailable => _singletonProperty is not null;

    public void Sample()
    {
        if (_singletonProperty is null)
        {
            return;
        }

        try
        {
            var networkManager = _singletonProperty.GetValue(null);
            if (networkManager is null)
            {
                return;
            }

            // No active session - nothing to report.
            if (_isListeningProperty?.GetValue(networkManager) is not true)
            {
                return;
            }

            var attributes = _attributes.Current;

            if (_isServerProperty?.GetValue(networkManager) is true)
            {
                if (_connectedClientsIdsProperty?.GetValue(networkManager) is IReadOnlyCollection<ulong> clients)
                {
                    SentrySdk.Metrics.EmitGauge(NumClientsMetric, clients.Count, MeasurementUnit.None, attributes);
                }
            }
            else
            {
                var rtt = GetCurrentRtt(networkManager);
                if (rtt.HasValue)
                {
                    SentrySdk.Metrics.EmitGauge(
                        PingMetric, (double)rtt.Value, MeasurementUnit.Duration.Millisecond, attributes);
                }
            }
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Failed to emit network metrics.");
        }
    }

    private ulong? GetCurrentRtt(object networkManager)
    {
        var networkConfig = _networkConfigProperty?.GetValue(networkManager);
        if (networkConfig is null)
        {
            return null;
        }

        _networkTransportProperty ??= networkConfig.GetType().GetProperty("NetworkTransport");
        var transport = _networkTransportProperty?.GetValue(networkConfig);
        if (transport is null)
        {
            return null;
        }

        var getCurrentRtt = transport.GetType().GetMethod("GetCurrentRtt", new[] { typeof(ulong) });
        if (getCurrentRtt is null)
        {
            if (!_loggedMissingRtt)
            {
                _logger?.LogDebug("Network transport does not expose 'GetCurrentRtt' - skipping ping metric.");
                _loggedMissingRtt = true;
            }

            return null;
        }

        return getCurrentRtt.Invoke(transport, new object[] { ServerClientId }) as ulong?;
    }
}
