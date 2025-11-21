namespace BenchmarkClient.Models;

public class LatencyMeasurement
{
    public int MessageId { get; set; }
    public int ClientId { get; set; }
    public DateTime SentTime { get; set; }
    public DateTime ReceivedTime { get; set; }
    public TimeSpan Latency { get; set; }
}

