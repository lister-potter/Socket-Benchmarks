using System.Text.Json;
using EchoServer.Models;
using Xunit;

namespace EchoServer.Tests.Models;

public class LotTests
{
    [Fact]
    public void Lot_SerializesToJson_WithCorrectPropertyNames()
    {
        var lot = new Lot
        {
            LotId = "lot-123",
            AuctionId = "auction-456",
            StartingPrice = 100.50m,
            CurrentBid = 150.75m,
            CurrentBidder = "bidder-789",
            Status = LotStatus.Open
        };

        var json = JsonSerializer.Serialize(lot);
        var doc = JsonDocument.Parse(json);

        Assert.Equal("lot-123", doc.RootElement.GetProperty("lotId").GetString());
        Assert.Equal("auction-456", doc.RootElement.GetProperty("auctionId").GetString());
        Assert.Equal(100.50m, doc.RootElement.GetProperty("startingPrice").GetDecimal());
        Assert.Equal(150.75m, doc.RootElement.GetProperty("currentBid").GetDecimal());
        Assert.Equal("bidder-789", doc.RootElement.GetProperty("currentBidder").GetString());
        var statusValue = doc.RootElement.GetProperty("status");
        Assert.Equal(JsonValueKind.String, statusValue.ValueKind);
        Assert.Equal("Open", statusValue.GetString());
    }

    [Fact]
    public void Lot_DeserializesFromJson_WithCorrectValues()
    {
        var json = """
        {
            "lotId": "lot-123",
            "auctionId": "auction-456",
            "startingPrice": 100.50,
            "currentBid": 150.75,
            "currentBidder": "bidder-789",
            "status": "Open"
        }
        """;

        var lot = JsonSerializer.Deserialize<Lot>(json);

        Assert.NotNull(lot);
        Assert.Equal("lot-123", lot!.LotId);
        Assert.Equal("auction-456", lot.AuctionId);
        Assert.Equal(100.50m, lot.StartingPrice);
        Assert.Equal(150.75m, lot.CurrentBid);
        Assert.Equal("bidder-789", lot.CurrentBidder);
        Assert.Equal(LotStatus.Open, lot.Status);
    }

    [Fact]
    public void Lot_WithNullCurrentBidder_SerializesCorrectly()
    {
        var lot = new Lot
        {
            LotId = "lot-123",
            AuctionId = "auction-456",
            StartingPrice = 100.50m,
            CurrentBid = 100.50m,
            CurrentBidder = null,
            Status = LotStatus.Open
        };

        var json = JsonSerializer.Serialize(lot);
        var doc = JsonDocument.Parse(json);

        Assert.True(doc.RootElement.GetProperty("currentBidder").ValueKind == JsonValueKind.Null);
    }

    [Fact]
    public void Lot_WithClosedStatus_SerializesCorrectly()
    {
        var lot = new Lot
        {
            LotId = "lot-123",
            AuctionId = "auction-456",
            StartingPrice = 100.50m,
            CurrentBid = 200.00m,
            CurrentBidder = "bidder-789",
            Status = LotStatus.Closed
        };

        var json = JsonSerializer.Serialize(lot);
        var doc = JsonDocument.Parse(json);

        Assert.Equal("Closed", doc.RootElement.GetProperty("status").GetString());
    }
}

