using BenchmarkClient.Models;

namespace BenchmarkClient.Services;

public class StatisticsCalculator
{
    public LatencyPercentiles CalculatePercentiles(List<LatencyMeasurement> measurements)
    {
        if (measurements.Count == 0)
        {
            return new LatencyPercentiles();
        }

        var latencies = measurements
            .Select(m => m.Latency.TotalMilliseconds)
            .OrderBy(l => l)
            .ToArray();

        return new LatencyPercentiles
        {
            Min = latencies[0],
            Max = latencies[latencies.Length - 1],
            Mean = latencies.Average(),
            P50 = GetPercentile(latencies, 0.50),
            P90 = GetPercentile(latencies, 0.90),
            P99 = GetPercentile(latencies, 0.99)
        };
    }

    public double CalculateThroughput(int totalMessages, TimeSpan duration)
    {
        if (duration.TotalSeconds == 0) return 0;
        return totalMessages / duration.TotalSeconds;
    }

    private static double GetPercentile(double[] sortedValues, double percentile)
    {
        if (sortedValues.Length == 0) return 0;
        var index = (int)Math.Ceiling(sortedValues.Length * percentile) - 1;
        if (index < 0) index = 0;
        if (index >= sortedValues.Length) index = sortedValues.Length - 1;
        return sortedValues[index];
    }
}

