using System.Text.Json;
using EchoServer.Models;
using Xunit;

namespace EchoServer.Tests.Models;

public class BidTests
{
    [Fact]
    public void Bid_SerializesToJson_WithCorrectPropertyNames()
    {
        var bid = new Bid
        {
            BidId = "bid-123",
            LotId = "lot-456",
            BidderId = "bidder-789",
            Amount = 150.75m,
            Timestamp = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc)
        };

        var json = JsonSerializer.Serialize(bid);
        var doc = JsonDocument.Parse(json);

        Assert.Equal("bid-123", doc.RootElement.GetProperty("bidId").GetString());
        Assert.Equal("lot-456", doc.RootElement.GetProperty("lotId").GetString());
        Assert.Equal("bidder-789", doc.RootElement.GetProperty("bidderId").GetString());
        Assert.Equal(150.75m, doc.RootElement.GetProperty("amount").GetDecimal());
        Assert.True(doc.RootElement.GetProperty("timestamp").GetDateTime() == new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void Bid_DeserializesFromJson_WithCorrectValues()
    {
        var json = """
        {
            "bidId": "bid-123",
            "lotId": "lot-456",
            "bidderId": "bidder-789",
            "amount": 150.75,
            "timestamp": "2024-01-01T12:00:00Z"
        }
        """;

        var bid = JsonSerializer.Deserialize<Bid>(json);

        Assert.NotNull(bid);
        Assert.Equal("bid-123", bid!.BidId);
        Assert.Equal("lot-456", bid.LotId);
        Assert.Equal("bidder-789", bid.BidderId);
        Assert.Equal(150.75m, bid.Amount);
        Assert.Equal(new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc), bid.Timestamp);
    }
}

