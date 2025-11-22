namespace BenchmarkClient.Models;

public class LatencyMeasurement
{
    public int MessageId { get; set; }
    public int ClientId { get; set; }
    /// <summary>
    /// Latency in milliseconds (elapsed time, not absolute timestamp).
    /// </summary>
    public double LatencyMilliseconds { get; set; }
}

