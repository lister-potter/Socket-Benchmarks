using System.Diagnostics;
using BenchmarkClient.Interfaces;
using BenchmarkClient.Models;

namespace BenchmarkClient.Services;

public class MetricsCollector : IMetricsCollector
{
    private int _totalMessagesSent;
    private int _totalMessagesReceived;
    private int _totalConnectionErrors;
    private int _totalMessageMismatches;
    private readonly List<LatencyMeasurement> _latencyMeasurements = new();
    private readonly object _lock = new();
    private DateTime _testStartTime;
    private DateTime _testEndTime;

    public void RecordMessageSent(DateTime timestamp, int clientId, int messageId, int size)
    {
        lock (_lock)
        {
            _totalMessagesSent++;
            if (_testStartTime == default)
            {
                _testStartTime = timestamp;
            }
            // Update test end time on send (fallback if no messages received)
            _testEndTime = timestamp;
        }
    }

    public void RecordMessageReceived(DateTime timestamp, int clientId, int messageId, double latencyMilliseconds)
    {
        lock (_lock)
        {
            _totalMessagesReceived++;
            _latencyMeasurements.Add(new LatencyMeasurement
            {
                MessageId = messageId,
                ClientId = clientId,
                LatencyMilliseconds = latencyMilliseconds
            });
            // Always update test end time when message received
            _testEndTime = timestamp;
        }
    }
    
    /// <summary>
    /// Sets the test end time explicitly (used when test completes).
    /// </summary>
    public void SetTestEndTime(DateTime endTime)
    {
        lock (_lock)
        {
            _testEndTime = endTime;
        }
    }

    public void RecordConnectionError(int clientId, string error)
    {
        lock (_lock)
        {
            _totalConnectionErrors++;
        }
    }

    public void RecordMessageMismatch(int clientId, int messageId)
    {
        lock (_lock)
        {
            _totalMessageMismatches++;
        }
    }

    public BenchmarkMetrics GetMetrics()
    {
        lock (_lock)
        {
            var duration = _testEndTime > _testStartTime 
                ? _testEndTime - _testStartTime 
                : TimeSpan.Zero;

            var calculator = new StatisticsCalculator();
            var percentiles = calculator.CalculatePercentiles(_latencyMeasurements);
            var throughput = calculator.CalculateThroughput(_totalMessagesReceived, duration);

            return new BenchmarkMetrics
            {
                TotalMessagesSent = _totalMessagesSent,
                TotalMessagesReceived = _totalMessagesReceived,
                TotalConnectionErrors = _totalConnectionErrors,
                TotalMessageMismatches = _totalMessageMismatches,
                MessagesPerSecond = throughput,
                Latency = percentiles,
                TestStartTime = _testStartTime,
                TestEndTime = _testEndTime
            };
        }
    }
}

