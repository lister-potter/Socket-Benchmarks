using System.Diagnostics;
using BenchmarkClient.Models;

namespace BenchmarkClient.Services;

public class LatencyTracker
{
    private readonly Dictionary<int, long> _pendingMessages = new();
    private readonly List<LatencyMeasurement> _measurements = new();
    private readonly object _lock = new();

    public void RecordSent(int messageId, long sentTimestamp)
    {
        lock (_lock)
        {
            _pendingMessages[messageId] = sentTimestamp;
        }
    }

    public void RecordReceived(int messageId, long receivedTimestamp)
    {
        lock (_lock)
        {
            if (_pendingMessages.TryGetValue(messageId, out var sentTimestamp))
            {
                // Calculate elapsed milliseconds using monotonic time
                var elapsedTicks = receivedTimestamp - sentTimestamp;
                var elapsedMs = (elapsedTicks * 1000.0) / Stopwatch.Frequency;
                
                _measurements.Add(new LatencyMeasurement
                {
                    MessageId = messageId,
                    LatencyMilliseconds = elapsedMs
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

