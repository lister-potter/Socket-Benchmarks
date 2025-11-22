using BenchmarkClient.Interfaces;
using BenchmarkClient.Models;
using BenchmarkClient.Services;
using BenchmarkClient.Scenarios;
using System.Net.WebSockets;
using Xunit;

namespace BenchmarkClient.IntegrationTests;

/// <summary>
/// Smoke test for auction bid logic.
/// Tests with a single client sending increasing bids on lot-1 starting at 100.
/// Expected: bidsAccepted should match totalBidsPlaced and bidsFailed should be zero.
/// </summary>
public class AuctionBidSmokeTest : IDisposable
{
    private readonly BidMetricsCollector _bidMetricsCollector;
    private readonly string _serverUrl;
    
    public AuctionBidSmokeTest()
    {
        _bidMetricsCollector = new BidMetricsCollector();
        _serverUrl = "ws://localhost:8080"; // Assume server is running
    }

    [Fact(Skip = "Requires running server - run manually")]
    public async Task SmokeTest_SingleClient_IncreasingBids_AllAccepted()
    {
        // Arrange
        var config = new BenchmarkConfig
        {
            ServerUrl = _serverUrl,
            ClientCount = 1,
            MessagesPerSecondPerClient = 10, // Slow rate for testing
            Duration = TimeSpan.FromSeconds(5), // Short duration
            Pattern = MessagePattern.FixedRate,
            Mode = BenchmarkMode.Auction,
            ScenarioName = "smoke-test",
            ServerLanguage = "dotnet"
        };

        // Act - Run a scenario that sends bids
        var scenario = new SingleClientScenario();
        var metrics = await scenario.ExecuteAsync(config, CancellationToken.None);

        // Assert - Bid metrics should show all bids accepted
        Assert.NotNull(metrics.BidMetrics);
        Assert.True(metrics.BidMetrics.TotalBidsPlaced > 0, "Expected at least one bid to be placed");
        Assert.Equal(metrics.BidMetrics.TotalBidsPlaced, metrics.BidMetrics.BidsAccepted);
        Assert.Equal(0, metrics.BidMetrics.BidsFailed);
        Assert.Equal(1.0, metrics.BidMetrics.AcceptanceRate, 2); // 100% acceptance rate
        Assert.Equal(0.0, metrics.BidMetrics.FailureRate, 2); // 0% failure rate
    }

    [Fact]
    public void BidMetricsCollector_IncreasingBids_AllAccepted()
    {
        // Unit test version - test the collector logic directly
        var collector = new BidMetricsCollector();
        var timestamp = DateTime.UtcNow;

        // Simulate sending increasing bids on lot-1 starting at 100
        var lotId = "lot-1";
        var bidderId = "bidder-1";
        var baseAmount = 100m;

        // Place 10 bids with increasing amounts (all should be accepted)
        for (int i = 0; i < 10; i++)
        {
            var bidAmount = baseAmount + i + 1; // 101, 102, 103, ..., 110
            collector.RecordBidPlaced(lotId, bidderId, bidAmount, timestamp.AddMilliseconds(i * 100));
            collector.RecordBidAccepted(lotId, bidderId, bidAmount, timestamp.AddMilliseconds(i * 100 + 50));
        }

        var metrics = collector.GetMetrics();

        // Assert
        Assert.Equal(10, metrics.TotalBidsPlaced);
        Assert.Equal(10, metrics.BidsAccepted);
        Assert.Equal(0, metrics.BidsFailed);
        Assert.Equal(1.0, metrics.AcceptanceRate, 2); // 100% acceptance
        Assert.Equal(0.0, metrics.FailureRate, 2); // 0% failure
    }

    [Fact]
    public void BidMetricsCollector_TooLowBids_SomeRejected()
    {
        // Unit test version - test rejection logic
        var collector = new BidMetricsCollector();
        var timestamp = DateTime.UtcNow;

        var lotId = "lot-1";
        var bidderId = "bidder-1";

        // Place first bid at 101 (accepted)
        collector.RecordBidPlaced(lotId, bidderId, 101m, timestamp);
        collector.RecordBidAccepted(lotId, bidderId, 101m, timestamp.AddMilliseconds(10));

        // Place second bid at 100 (rejected - too low)
        collector.RecordBidPlaced(lotId, bidderId, 100m, timestamp.AddMilliseconds(100));
        collector.RecordBidFailed(lotId, bidderId, 100m, BidFailureReason.BidTooLow, timestamp.AddMilliseconds(110));

        // Place third bid at 102 (accepted)
        collector.RecordBidPlaced(lotId, bidderId, 102m, timestamp.AddMilliseconds(200));
        collector.RecordBidAccepted(lotId, bidderId, 102m, timestamp.AddMilliseconds(210));

        var metrics = collector.GetMetrics();

        // Assert
        Assert.Equal(3, metrics.TotalBidsPlaced);
        Assert.Equal(2, metrics.BidsAccepted);
        Assert.Equal(1, metrics.BidsFailed);
        Assert.Equal(1, metrics.FailureReasonBreakdown[BidFailureReason.BidTooLow]);
    }

    public void Dispose()
    {
        // Cleanup if needed
    }
}

