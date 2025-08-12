using HostMonitor.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace HostMonitor.Services;

public class HostMonitorService : BackgroundService
{
    private readonly ILogger<HostMonitorService> _logger;
    private readonly IHubContext<Hubs.MonitorHub> _hub;
    private readonly IConfiguration _config;

    private readonly ConcurrentDictionary<string, HostRecord> _records = new();
    private string[] _hosts = Array.Empty<string>();
    private int _intervalSeconds = 5;
    private int _tcpFallbackPort = 80;    // port to try for TCP fallback
    private int _historySize = 60;

    public HostMonitorService(ILogger<HostMonitorService> logger, IHubContext<Hubs.MonitorHub> hub, IConfiguration config)
    {
        _logger = logger;
        _hub = hub;
        _config = config;

        // read settings from command line or config
        // use builder.Configuration.AddCommandLine(args) in Program.cs so these are available
        var hostsArg = _config["hosts"] ?? "google.com,github.com";
        _hosts = hostsArg.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        _intervalSeconds = int.TryParse(_config["interval"], out var iv) ? iv : 5;
        _tcpFallbackPort = int.TryParse(_config["tcpport"], out var tp) ? tp : 80;
        _historySize = int.TryParse(_config["history"], out var hs) ? hs : 60;

        foreach (var h in _hosts) _records.TryAdd(h, new HostRecord(h, _historySize));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("HostMonitorService starting. Hosts: {hosts}, interval {interval}s", string.Join(", ", _hosts), _intervalSeconds);

		// Main monitoring Loop
        while (!stoppingToken.IsCancellationRequested)
        {
            var tasks = _hosts.Select(h => PingOnceAsync(h, stoppingToken)).ToArray();
            await Task.WhenAll(tasks);

            // build snapshot and broadcast
            var snapshot = _records.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToStats());
            try
            {
                await _hub.Clients.All.SendAsync("update", snapshot, cancellationToken: stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "SignalR broadcast failed (non-fatal).");
            }

            await Task.Delay(TimeSpan.FromSeconds(_intervalSeconds), stoppingToken);
        }
    }

    private async Task PingOnceAsync(string host, CancellationToken ct)
    {
        long latency = -1;

        // try ICMP (Ping). On Linux this may require CAP_NET_RAW -- we fallback to TCP if it fails.
        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(host, 2000); // 2s timeout
            if (reply.Status == IPStatus.Success)
            {
                latency = reply.RoundtripTime; // milliseconds
            }
        }
        catch (Exception ex)
        {
            // often a permission or platform issue; we log at debug
            _logger.LogDebug(ex, "Ping failed for {host} (will try TCP fallback).", host);
        }

        // fallback to TCP connect if ICMP failed
        if (latency < 0)
        {
            try
            {
                using var tcp = new TcpClient();
                var sw = Stopwatch.StartNew();
                var connectTask = tcp.ConnectAsync(host, _tcpFallbackPort);
                var completed = await Task.WhenAny(connectTask, Task.Delay(2000, ct));
                if (completed == connectTask && tcp.Connected)
                {
                    sw.Stop();
                    latency = sw.ElapsedMilliseconds;
                    tcp.Close();
                }
                else
                {
                    latency = -1;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "TCP fallback failed for {host}", host);
                latency = -1;
            }
        }

        // record sample
        var record = _records.GetOrAdd(host, _ => new HostRecord(host, _historySize));
        record.AddSample(latency);

        // optional: log very infrequent
        if (latency < 0)
            _logger.LogInformation("{host} is DOWN (sample).", host);
        else
            _logger.LogDebug("{host} latency {lat} ms", host, latency);
    }
}