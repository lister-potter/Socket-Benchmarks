using BenchmarkClient.Models;

namespace BenchmarkClient.Services;

public class LatencyTracker
{
    private readonly Dictionary<int, DateTime> _pendingMessages = new();
    private readonly List<LatencyMeasurement> _measurements = new();
    private readonly object _lock = new();

    public void RecordSent(int messageId, DateTime sentTime)
    {
        lock (_lock)
        {
            _pendingMessages[messageId] = sentTime;
        }
    }

    public void RecordReceived(int messageId, DateTime receivedTime)
    {
        lock (_lock)
        {
            if (_pendingMessages.TryGetValue(messageId, out var sentTime))
            {
                var latency = receivedTime - sentTime;
                _measurements.Add(new LatencyMeasurement
                {
                    MessageId = messageId,
                    SentTime = sentTime,
                    ReceivedTime = receivedTime,
                    Latency = latency
                });
                _pendingMessages.Remove(messageId);
            }
        }
    }

    public List<LatencyMeasurement> GetMeasurements()
    {
        lock (_lock)
        {
            return new List<LatencyMeasurement>(_measurements);
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _pendingMessages.Clear();
            _measurements.Clear();
        }
    }
}

