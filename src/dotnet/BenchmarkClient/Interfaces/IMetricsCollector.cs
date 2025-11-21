using BenchmarkClient.Models;

namespace BenchmarkClient.Interfaces;

public interface IMetricsCollector
{
    void RecordMessageSent(DateTime timestamp, int clientId, int messageId, int size);
    void RecordMessageReceived(DateTime timestamp, int clientId, int messageId, TimeSpan latency);
    void RecordConnectionError(int clientId, string error);
    void RecordMessageMismatch(int clientId, int messageId);
    BenchmarkMetrics GetMetrics();
}

