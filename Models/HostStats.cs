namespace HostMonitor.Models;

public class HostStats
{
    public string Host { get; set; } = "";
    public long LastLatencyMs { get; set; }      // -1 => failure/down
    public double AvgLatencyMs { get; set; }     // rolling average of successful pings
    public int PacketsSent { get; set; }
    public int PacketsLost { get; set; }
    public double PacketLossPercent => PacketsSent == 0 ? 0 : (double)PacketsLost / PacketsSent * 100.0;
}