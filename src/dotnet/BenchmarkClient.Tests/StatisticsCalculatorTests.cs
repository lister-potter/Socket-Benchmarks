using BenchmarkClient.Models;
using BenchmarkClient.Services;
using Xunit;

namespace BenchmarkClient.Tests;

public class StatisticsCalculatorTests
{
    [Fact]
    public void CalculatePercentiles_WithEmptyList_ReturnsZeroValues()
    {
        var calculator = new StatisticsCalculator();
        var result = calculator.CalculatePercentiles(new List<LatencyMeasurement>());

        Assert.Equal(0, result.P50);
        Assert.Equal(0, result.P90);
        Assert.Equal(0, result.P99);
        Assert.Equal(0, result.Min);
        Assert.Equal(0, result.Max);
        Assert.Equal(0, result.Mean);
    }

    [Fact]
    public void CalculatePercentiles_WithSingleValue_ReturnsThatValue()
    {
        var calculator = new StatisticsCalculator();
        var measurements = new List<LatencyMeasurement>
        {
            new() { Latency = TimeSpan.FromMilliseconds(10) }
        };

        var result = calculator.CalculatePercentiles(measurements);

        Assert.Equal(10, result.P50);
        Assert.Equal(10, result.P90);
        Assert.Equal(10, result.P99);
        Assert.Equal(10, result.Min);
        Assert.Equal(10, result.Max);
        Assert.Equal(10, result.Mean);
    }

    [Fact]
    public void CalculatePercentiles_WithMultipleValues_CalculatesCorrectly()
    {
        var calculator = new StatisticsCalculator();
        var measurements = new List<LatencyMeasurement>
        {
            new() { Latency = TimeSpan.FromMilliseconds(10) },
            new() { Latency = TimeSpan.FromMilliseconds(20) },
            new() { Latency = TimeSpan.FromMilliseconds(30) },
            new() { Latency = TimeSpan.FromMilliseconds(40) },
            new() { Latency = TimeSpan.FromMilliseconds(50) }
        };

        var result = calculator.CalculatePercentiles(measurements);

        Assert.Equal(10, result.Min);
        Assert.Equal(50, result.Max);
        Assert.Equal(30, result.Mean);
    }

    [Fact]
    public void CalculateThroughput_WithValidInput_ReturnsCorrectValue()
    {
        var calculator = new StatisticsCalculator();
        var throughput = calculator.CalculateThroughput(1000, TimeSpan.FromSeconds(10));

        Assert.Equal(100, throughput);
    }

    [Fact]
    public void CalculateThroughput_WithZeroDuration_ReturnsZero()
    {
        var calculator = new StatisticsCalculator();
        var throughput = calculator.CalculateThroughput(1000, TimeSpan.Zero);

        Assert.Equal(0, throughput);
    }
}

