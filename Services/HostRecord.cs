using HostMonitor.Models;
using System.Collections.Generic;
using System.Linq;

namespace HostMonitor.Services;

internal class HostRecord
{
    private readonly List<long> _history = new();
    private readonly object _lock = new();
    private readonly int _maxSamples;

    public string Host { get; }
    public int PacketsSent { get; private set; }
    public int PacketsLost { get; private set; }

    public HostRecord(string host, int maxSamples = 60)
    {
        Host = host;
        _maxSamples = maxSamples;
    }

    // Add one sample (latencyMs >=0 success, <0 failure)
    public void AddSample(long latencyMs)
    {
        lock (_lock)
        {
            PacketsSent++;
            if (latencyMs < 0) PacketsLost++;
            _history.Add(latencyMs);
            if (_history.Count > _maxSamples) _history.RemoveAt(0);
        }
    }

    public HostStats ToStats()
    {
        lock (_lock)
        {
            var successes = _history.Where(x => x >= 0).ToArray();
            double avg = successes.Length == 0 ? 0 : successes.Average();
            long last = _history.Count == 0 ? -1 : _history.Last();
            return new HostStats
            {
                Host = Host,
                LastLatencyMs = last,
                AvgLatencyMs = avg,
                PacketsSent = PacketsSent,
                PacketsLost = PacketsLost
            };
        }
    }
}