namespace BenchmarkClient.Models;

public class BenchmarkMetrics
{
    public int TotalMessagesSent { get; set; }
    public int TotalMessagesReceived { get; set; }
    public int TotalConnectionErrors { get; set; }
    public int TotalMessageMismatches { get; set; }
    public double MessagesPerSecond { get; set; }
    public LatencyPercentiles Latency { get; set; } = new();
    public List<ResourceSnapshot> ResourceUsage { get; set; } = new();
    public DateTime TestStartTime { get; set; }
    public DateTime TestEndTime { get; set; }
    
    /// <summary>
    /// Bid metrics for auction mode benchmarks. Null when not in auction mode.
    /// </summary>
    public BidMetrics? BidMetrics { get; set; }
}

public class LatencyPercentiles
{
    public double P50 { get; set; }  // milliseconds
    public double P90 { get; set; }  // milliseconds
    public double P99 { get; set; }  // milliseconds
    public double Max { get; set; }  // milliseconds
    public double Min { get; set; }  // milliseconds
    public double Mean { get; set; } // milliseconds
}

public class ResourceSnapshot
{
    public DateTime Timestamp { get; set; }
    public double CpuPercent { get; set; }
    public long MemoryBytes { get; set; }
}

