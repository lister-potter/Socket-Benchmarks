using BenchmarkClient.Interfaces;
using BenchmarkClient.Models;
using BenchmarkClient.Services;
using Xunit;

namespace BenchmarkClient.Tests.Services;

public class BidMetricsCollectorTests
{
    [Fact]
    public void RecordBidPlaced_IncrementsCounter()
    {
        var collector = new BidMetricsCollector();
        var timestamp = DateTime.UtcNow;

        collector.RecordBidPlaced("lot-1", "bidder-1", 100.50m, timestamp);

        var metrics = collector.GetMetrics();
        Assert.Equal(1, metrics.TotalBidsPlaced);
        Assert.Equal(0, metrics.BidsAccepted);
        Assert.Equal(0, metrics.BidsFailed);
    }

    [Fact]
    public void RecordBidAccepted_IncrementsCounter()
    {
        var collector = new BidMetricsCollector();
        var timestamp = DateTime.UtcNow;

        collector.RecordBidPlaced("lot-1", "bidder-1", 100.50m, timestamp);
        collector.RecordBidAccepted("lot-1", "bidder-1", 100.50m, timestamp.AddMilliseconds(10));

        var metrics = collector.GetMetrics();
        Assert.Equal(1, metrics.TotalBidsPlaced);
        Assert.Equal(1, metrics.BidsAccepted);
        Assert.Equal(0, metrics.BidsFailed);
    }

    [Fact]
    public void RecordBidFailed_IncrementsCounterAndBreakdown()
    {
        var collector = new BidMetricsCollector();
        var timestamp = DateTime.UtcNow;

        collector.RecordBidPlaced("lot-1", "bidder-1", 100.50m, timestamp);
        collector.RecordBidFailed("lot-1", "bidder-1", 100.50m, BidFailureReason.BidTooLow, timestamp.AddMilliseconds(10));

        var metrics = collector.GetMetrics();
        Assert.Equal(1, metrics.TotalBidsPlaced);
        Assert.Equal(0, metrics.BidsAccepted);
        Assert.Equal(1, metrics.BidsFailed);
        Assert.Equal(1, metrics.FailureReasonBreakdown[BidFailureReason.BidTooLow]);
    }

    [Fact]
    public void RecordBidFailed_TracksMultipleFailureReasons()
    {
        var collector = new BidMetricsCollector();
        var timestamp = DateTime.UtcNow;

        collector.RecordBidPlaced("lot-1", "bidder-1", 100.50m, timestamp);
        collector.RecordBidFailed("lot-1", "bidder-1", 100.50m, BidFailureReason.BidTooLow, timestamp);

        collector.RecordBidPlaced("lot-2", "bidder-2", 200.00m, timestamp);
        collector.RecordBidFailed("lot-2", "bidder-2", 200.00m, BidFailureReason.LotClosed, timestamp);

        collector.RecordBidPlaced("lot-3", "bidder-3", 300.00m, timestamp);
        collector.RecordBidFailed("lot-3", "bidder-3", 300.00m, BidFailureReason.Error, timestamp);

        var metrics = collector.GetMetrics();
        Assert.Equal(3, metrics.TotalBidsPlaced);
        Assert.Equal(0, metrics.BidsAccepted);
        Assert.Equal(3, metrics.BidsFailed);
        Assert.Equal(1, metrics.FailureReasonBreakdown[BidFailureReason.BidTooLow]);
        Assert.Equal(1, metrics.FailureReasonBreakdown[BidFailureReason.LotClosed]);
        Assert.Equal(1, metrics.FailureReasonBreakdown[BidFailureReason.Error]);
    }

    [Fact]
    public void RecordBidFailed_TracksMultipleFailuresOfSameReason()
    {
        var collector = new BidMetricsCollector();
        var timestamp = DateTime.UtcNow;

        collector.RecordBidPlaced("lot-1", "bidder-1", 100.50m, timestamp);
        collector.RecordBidFailed("lot-1", "bidder-1", 100.50m, BidFailureReason.BidTooLow, timestamp);

        collector.RecordBidPlaced("lot-2", "bidder-2", 200.00m, timestamp);
        collector.RecordBidFailed("lot-2", "bidder-2", 200.00m, BidFailureReason.BidTooLow, timestamp);

        var metrics = collector.GetMetrics();
        Assert.Equal(2, metrics.FailureReasonBreakdown[BidFailureReason.BidTooLow]);
    }

    [Fact]
    public void GetMetrics_CalculatesAcceptanceAndFailureRates()
    {
        var collector = new BidMetricsCollector();
        var timestamp = DateTime.UtcNow;

        // Place 10 bids: 7 accepted, 3 failed
        for (int i = 0; i < 10; i++)
        {
            collector.RecordBidPlaced($"lot-{i}", $"bidder-{i}", 100m + i, timestamp);
            if (i < 7)
            {
                collector.RecordBidAccepted($"lot-{i}", $"bidder-{i}", 100m + i, timestamp);
            }
            else
            {
                collector.RecordBidFailed($"lot-{i}", $"bidder-{i}", 100m + i, BidFailureReason.BidTooLow, timestamp);
            }
        }

        var metrics = collector.GetMetrics();
        Assert.Equal(10, metrics.TotalBidsPlaced);
        Assert.Equal(7, metrics.BidsAccepted);
        Assert.Equal(3, metrics.BidsFailed);
        Assert.Equal(0.7, metrics.AcceptanceRate);
        Assert.Equal(0.3, metrics.FailureRate);
    }

    [Fact]
    public void GetMetrics_CalculatesRatesWithZeroBids()
    {
        var collector = new BidMetricsCollector();

        var metrics = collector.GetMetrics();
        Assert.Equal(0, metrics.TotalBidsPlaced);
        Assert.Equal(0, metrics.AcceptanceRate);
        Assert.Equal(0, metrics.FailureRate);
    }

    [Fact]
    public void RecordBidPlaced_StoresBidDetails()
    {
        var collector = new BidMetricsCollector();
        var timestamp = DateTime.UtcNow;

        collector.RecordBidPlaced("lot-1", "bidder-123", 100.50m, timestamp);

        var metrics = collector.GetMetrics();
        Assert.Single(metrics.BidDetails);
        var detail = metrics.BidDetails[0];
        Assert.Equal("lot-1", detail.LotId);
        Assert.Equal("bidder-123", detail.BidderId);
        Assert.Equal(100.50m, detail.Amount);
        Assert.Equal(timestamp, detail.Timestamp);
        Assert.Equal(BidOutcome.Failed, detail.Outcome); // Initially Failed until we get response
        Assert.Null(detail.FailureReason);
    }

    [Fact]
    public void RecordBidAccepted_UpdatesBidDetailOutcome()
    {
        var collector = new BidMetricsCollector();
        var timestamp = DateTime.UtcNow;

        collector.RecordBidPlaced("lot-1", "bidder-1", 100.50m, timestamp);
        collector.RecordBidAccepted("lot-1", "bidder-1", 100.50m, timestamp.AddMilliseconds(10));

        var metrics = collector.GetMetrics();
        var detail = metrics.BidDetails[0];
        Assert.Equal(BidOutcome.Accepted, detail.Outcome);
        Assert.Null(detail.FailureReason);
    }

    [Fact]
    public void RecordBidFailed_UpdatesBidDetailOutcomeAndReason()
    {
        var collector = new BidMetricsCollector();
        var timestamp = DateTime.UtcNow;

        collector.RecordBidPlaced("lot-1", "bidder-1", 100.50m, timestamp);
        collector.RecordBidFailed("lot-1", "bidder-1", 100.50m, BidFailureReason.BidTooLow, timestamp.AddMilliseconds(10));

        var metrics = collector.GetMetrics();
        var detail = metrics.BidDetails[0];
        Assert.Equal(BidOutcome.Failed, detail.Outcome);
        Assert.Equal(BidFailureReason.BidTooLow, detail.FailureReason);
    }

    [Fact]
    public void RecordBidPlaced_ThrowsOnNullLotId()
    {
        var collector = new BidMetricsCollector();
        var timestamp = DateTime.UtcNow;

        Assert.Throws<ArgumentException>(() => collector.RecordBidPlaced(null!, "bidder-1", 100m, timestamp));
        Assert.Throws<ArgumentException>(() => collector.RecordBidPlaced("", "bidder-1", 100m, timestamp));
    }

    [Fact]
    public void RecordBidPlaced_ThrowsOnNullBidderId()
    {
        var collector = new BidMetricsCollector();
        var timestamp = DateTime.UtcNow;

        Assert.Throws<ArgumentException>(() => collector.RecordBidPlaced("lot-1", null!, 100m, timestamp));
        Assert.Throws<ArgumentException>(() => collector.RecordBidPlaced("lot-1", "", 100m, timestamp));
    }

    [Fact]
    public void RecordBidAccepted_ThrowsOnNullLotId()
    {
        var collector = new BidMetricsCollector();
        var timestamp = DateTime.UtcNow;

        Assert.Throws<ArgumentException>(() => collector.RecordBidAccepted(null!, "bidder-1", 100m, timestamp));
        Assert.Throws<ArgumentException>(() => collector.RecordBidAccepted("", "bidder-1", 100m, timestamp));
    }

    [Fact]
    public void RecordBidAccepted_ThrowsOnNullBidderId()
    {
        var collector = new BidMetricsCollector();
        var timestamp = DateTime.UtcNow;

        Assert.Throws<ArgumentException>(() => collector.RecordBidAccepted("lot-1", null!, 100m, timestamp));
        Assert.Throws<ArgumentException>(() => collector.RecordBidAccepted("lot-1", "", 100m, timestamp));
    }

    [Fact]
    public void RecordBidFailed_ThrowsOnNullLotId()
    {
        var collector = new BidMetricsCollector();
        var timestamp = DateTime.UtcNow;

        Assert.Throws<ArgumentException>(() => collector.RecordBidFailed(null!, "bidder-1", 100m, BidFailureReason.Error, timestamp));
        Assert.Throws<ArgumentException>(() => collector.RecordBidFailed("", "bidder-1", 100m, BidFailureReason.Error, timestamp));
    }

    [Fact]
    public void RecordBidFailed_ThrowsOnNullBidderId()
    {
        var collector = new BidMetricsCollector();
        var timestamp = DateTime.UtcNow;

        Assert.Throws<ArgumentException>(() => collector.RecordBidFailed("lot-1", null!, 100m, BidFailureReason.Error, timestamp));
        Assert.Throws<ArgumentException>(() => collector.RecordBidFailed("lot-1", "", 100m, BidFailureReason.Error, timestamp));
    }

    [Fact]
    public async Task GetMetrics_IsThreadSafe()
    {
        var collector = new BidMetricsCollector();
        var timestamp = DateTime.UtcNow;
        const int numberOfThreads = 10;
        const int bidsPerThread = 100;

        var tasks = new List<Task>();

        // Launch multiple threads to record bids concurrently
        for (int i = 0; i < numberOfThreads; i++)
        {
            int threadId = i;
            tasks.Add(Task.Run(() =>
            {
                for (int j = 0; j < bidsPerThread; j++)
                {
                    var lotId = $"lot-{threadId}";
                    var bidderId = $"bidder-{threadId}-{j}";
                    var amount = 100m + j;

                    collector.RecordBidPlaced(lotId, bidderId, amount, timestamp);
                    
                    // Randomly accept or fail
                    if (j % 2 == 0)
                    {
                        collector.RecordBidAccepted(lotId, bidderId, amount, timestamp);
                    }
                    else
                    {
                        collector.RecordBidFailed(lotId, bidderId, amount, BidFailureReason.BidTooLow, timestamp);
                    }
                }
            }));
        }

        await Task.WhenAll(tasks);

        var metrics = collector.GetMetrics();
        var expectedTotal = numberOfThreads * bidsPerThread;
        var expectedAccepted = expectedTotal / 2; // Half accepted (even indices)
        var expectedFailed = expectedTotal / 2; // Half failed (odd indices)

        Assert.Equal(expectedTotal, metrics.TotalBidsPlaced);
        Assert.Equal(expectedAccepted, metrics.BidsAccepted);
        Assert.Equal(expectedFailed, metrics.BidsFailed);
        Assert.Equal(expectedAccepted + expectedFailed, metrics.TotalBidsPlaced);
    }

    [Fact]
    public void GetMetrics_ReturnsCopyOfFailureReasonBreakdown()
    {
        var collector = new BidMetricsCollector();
        var timestamp = DateTime.UtcNow;

        collector.RecordBidPlaced("lot-1", "bidder-1", 100m, timestamp);
        collector.RecordBidFailed("lot-1", "bidder-1", 100m, BidFailureReason.BidTooLow, timestamp);

        var metrics1 = collector.GetMetrics();
        var breakdown1 = metrics1.FailureReasonBreakdown;

        // Modify the breakdown
        breakdown1[BidFailureReason.BidTooLow] = 999;

        // Get metrics again - should not be affected
        var metrics2 = collector.GetMetrics();
        Assert.Equal(1, metrics2.FailureReasonBreakdown[BidFailureReason.BidTooLow]);
    }

    [Fact]
    public void GetMetrics_ReturnsCopyOfBidDetails()
    {
        var collector = new BidMetricsCollector();
        var timestamp = DateTime.UtcNow;

        collector.RecordBidPlaced("lot-1", "bidder-1", 100m, timestamp);

        var metrics1 = collector.GetMetrics();
        var details1 = metrics1.BidDetails;

        // Modify the details
        details1.Clear();

        // Get metrics again - should not be affected
        var metrics2 = collector.GetMetrics();
        Assert.Single(metrics2.BidDetails);
    }
}

