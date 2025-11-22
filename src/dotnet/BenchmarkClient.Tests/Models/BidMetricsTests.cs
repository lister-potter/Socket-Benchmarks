using BenchmarkClient.Models;
using System.Text.Json;
using Xunit;

namespace BenchmarkClient.Tests.Models;

public class BidMetricsTests
{
    [Fact]
    public void BidMetrics_InitializesWithDefaults()
    {
        var metrics = new BidMetrics();

        Assert.Equal(0, metrics.TotalBidsPlaced);
        Assert.Equal(0, metrics.BidsAccepted);
        Assert.Equal(0, metrics.BidsFailed);
        Assert.Equal(0.0, metrics.AcceptanceRate);
        Assert.Equal(0.0, metrics.FailureRate);
        Assert.NotNull(metrics.FailureReasonBreakdown);
        Assert.Empty(metrics.FailureReasonBreakdown);
        Assert.NotNull(metrics.BidDetails);
        Assert.Empty(metrics.BidDetails);
    }

    [Fact]
    public void BidMetrics_CalculatesAcceptanceAndFailureRates()
    {
        var metrics = new BidMetrics
        {
            TotalBidsPlaced = 1000,
            BidsAccepted = 750,
            BidsFailed = 250
        };

        // Acceptance and failure rates should be calculated externally, but we can verify the values
        metrics.AcceptanceRate = metrics.TotalBidsPlaced > 0 
            ? (double)metrics.BidsAccepted / metrics.TotalBidsPlaced 
            : 0.0;
        metrics.FailureRate = metrics.TotalBidsPlaced > 0 
            ? (double)metrics.BidsFailed / metrics.TotalBidsPlaced 
            : 0.0;

        Assert.Equal(0.75, metrics.AcceptanceRate);
        Assert.Equal(0.25, metrics.FailureRate);
    }

    [Fact]
    public void BidMetrics_SerializesToJson()
    {
        var metrics = new BidMetrics
        {
            TotalBidsPlaced = 100,
            BidsAccepted = 80,
            BidsFailed = 20,
            AcceptanceRate = 0.8,
            FailureRate = 0.2,
            FailureReasonBreakdown = new Dictionary<BidFailureReason, int>
            {
                { BidFailureReason.BidTooLow, 15 },
                { BidFailureReason.LotClosed, 3 },
                { BidFailureReason.Error, 2 }
            }
        };

        var json = JsonSerializer.Serialize(metrics);
        
        Assert.NotNull(json);
        Assert.Contains("\"TotalBidsPlaced\":100", json);
        Assert.Contains("\"BidsAccepted\":80", json);
        Assert.Contains("\"BidsFailed\":20", json);
        Assert.Contains("\"AcceptanceRate\":0.8", json);
        Assert.Contains("\"FailureRate\":0.2", json);
        Assert.Contains("\"BidTooLow\":15", json);
        Assert.Contains("\"LotClosed\":3", json);
        Assert.Contains("\"Error\":2", json);
    }

    [Fact]
    public void BidMetrics_DeserializesFromJson()
    {
        var json = @"{
            ""TotalBidsPlaced"": 100,
            ""BidsAccepted"": 80,
            ""BidsFailed"": 20,
            ""AcceptanceRate"": 0.8,
            ""FailureRate"": 0.2,
            ""FailureReasonBreakdown"": {
                ""BidTooLow"": 15,
                ""LotClosed"": 3,
                ""Error"": 2
            }
        }";

        var metrics = JsonSerializer.Deserialize<BidMetrics>(json);

        Assert.NotNull(metrics);
        Assert.Equal(100, metrics!.TotalBidsPlaced);
        Assert.Equal(80, metrics.BidsAccepted);
        Assert.Equal(20, metrics.BidsFailed);
        Assert.Equal(0.8, metrics.AcceptanceRate);
        Assert.Equal(0.2, metrics.FailureRate);
        Assert.Equal(3, metrics.FailureReasonBreakdown.Count);
        Assert.Equal(15, metrics.FailureReasonBreakdown[BidFailureReason.BidTooLow]);
        Assert.Equal(3, metrics.FailureReasonBreakdown[BidFailureReason.LotClosed]);
        Assert.Equal(2, metrics.FailureReasonBreakdown[BidFailureReason.Error]);
    }

    [Fact]
    public void BidDetail_InitializesCorrectly()
    {
        var detail = new BidDetail
        {
            LotId = "lot-1",
            BidderId = "bidder-123",
            Amount = 100.50m,
            Timestamp = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            Outcome = BidOutcome.Accepted,
            FailureReason = null
        };

        Assert.Equal("lot-1", detail.LotId);
        Assert.Equal("bidder-123", detail.BidderId);
        Assert.Equal(100.50m, detail.Amount);
        Assert.Equal(BidOutcome.Accepted, detail.Outcome);
        Assert.Null(detail.FailureReason);
    }

    [Fact]
    public void BidDetail_WithFailureReason()
    {
        var detail = new BidDetail
        {
            LotId = "lot-2",
            BidderId = "bidder-456",
            Amount = 50.00m,
            Timestamp = new DateTime(2024, 1, 1, 13, 0, 0, DateTimeKind.Utc),
            Outcome = BidOutcome.Failed,
            FailureReason = BidFailureReason.BidTooLow
        };

        Assert.Equal(BidOutcome.Failed, detail.Outcome);
        Assert.Equal(BidFailureReason.BidTooLow, detail.FailureReason);
    }

    [Fact]
    public void BidFailureReason_EnumValues()
    {
        Assert.Equal(0, (int)BidFailureReason.BidTooLow);
        Assert.Equal(1, (int)BidFailureReason.LotClosed);
        Assert.Equal(2, (int)BidFailureReason.Error);
    }

    [Fact]
    public void BidOutcome_EnumValues()
    {
        Assert.Equal(0, (int)BidOutcome.Accepted);
        Assert.Equal(1, (int)BidOutcome.Failed);
    }

    [Fact]
    public void BidMetrics_BidDetails_ExcludedFromJsonByDefault()
    {
        var metrics = new BidMetrics
        {
            TotalBidsPlaced = 10,
            BidDetails = new List<BidDetail>
            {
                new BidDetail { LotId = "lot-1", BidderId = "bidder-1", Amount = 100m }
            }
        };

        var json = JsonSerializer.Serialize(metrics);

        // BidDetails should not be in JSON due to [JsonIgnore] attribute
        Assert.DoesNotContain("BidDetails", json);
        Assert.DoesNotContain("lot-1", json);
    }
}

