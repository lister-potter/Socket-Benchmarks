using System.Text.Json;
using EchoServer.Models;
using Xunit;

namespace EchoServer.Tests.Models;

public class MessageTests
{
    [Fact]
    public void JoinLotMessage_SerializesToJson_WithCorrectFormat()
    {
        var message = new JoinLotMessage
        {
            LotId = "lot-123"
        };

        var json = JsonSerializer.Serialize(message);
        var doc = JsonDocument.Parse(json);

        Assert.Equal("JoinLot", doc.RootElement.GetProperty("type").GetString());
        Assert.Equal("lot-123", doc.RootElement.GetProperty("lotId").GetString());
    }

    [Fact]
    public void JoinLotMessage_DeserializesFromJson_WithCorrectValues()
    {
        var json = """
        {
            "type": "JoinLot",
            "lotId": "lot-123"
        }
        """;

        var message = AuctionMessage.Deserialize(json);

        Assert.NotNull(message);
        Assert.IsType<JoinLotMessage>(message);
        Assert.Equal("JoinLot", message!.Type);
        Assert.Equal("lot-123", ((JoinLotMessage)message).LotId);
    }

    [Fact]
    public void PlaceBidMessage_SerializesToJson_WithCorrectFormat()
    {
        var message = new PlaceBidMessage
        {
            LotId = "lot-123",
            BidderId = "bidder-456",
            Amount = 150.75m
        };

        var json = JsonSerializer.Serialize(message);
        var doc = JsonDocument.Parse(json);

        Assert.Equal("PlaceBid", doc.RootElement.GetProperty("type").GetString());
        Assert.Equal("lot-123", doc.RootElement.GetProperty("lotId").GetString());
        Assert.Equal("bidder-456", doc.RootElement.GetProperty("bidderId").GetString());
        Assert.Equal(150.75m, doc.RootElement.GetProperty("amount").GetDecimal());
    }

    [Fact]
    public void PlaceBidMessage_DeserializesFromJson_WithCorrectValues()
    {
        var json = """
        {
            "type": "PlaceBid",
            "lotId": "lot-123",
            "bidderId": "bidder-456",
            "amount": 150.75
        }
        """;

        var message = AuctionMessage.Deserialize(json);

        Assert.NotNull(message);
        Assert.IsType<PlaceBidMessage>(message);
        Assert.Equal("PlaceBid", message!.Type);
        var placeBid = (PlaceBidMessage)message;
        Assert.Equal("lot-123", placeBid.LotId);
        Assert.Equal("bidder-456", placeBid.BidderId);
        Assert.Equal(150.75m, placeBid.Amount);
    }

    [Fact]
    public void LotUpdateMessage_SerializesToJson_WithCorrectFormat()
    {
        var message = new LotUpdateMessage
        {
            LotId = "lot-123",
            CurrentBid = 150.75m,
            CurrentBidder = "bidder-456",
            Status = "Open"
        };

        var json = JsonSerializer.Serialize(message);
        var doc = JsonDocument.Parse(json);

        Assert.Equal("LotUpdate", doc.RootElement.GetProperty("type").GetString());
        Assert.Equal("lot-123", doc.RootElement.GetProperty("lotId").GetString());
        Assert.Equal(150.75m, doc.RootElement.GetProperty("currentBid").GetDecimal());
        Assert.Equal("bidder-456", doc.RootElement.GetProperty("currentBidder").GetString());
        Assert.Equal("Open", doc.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public void LotUpdateMessage_WithNullCurrentBidder_SerializesCorrectly()
    {
        var message = new LotUpdateMessage
        {
            LotId = "lot-123",
            CurrentBid = 100.50m,
            CurrentBidder = null,
            Status = "Open"
        };

        var json = JsonSerializer.Serialize(message);
        var doc = JsonDocument.Parse(json);

        Assert.True(doc.RootElement.GetProperty("currentBidder").ValueKind == JsonValueKind.Null);
    }

    [Fact]
    public void ErrorMessage_SerializesToJson_WithCorrectFormat()
    {
        var message = new ErrorMessage
        {
            Message = "Lot is closed"
        };

        var json = JsonSerializer.Serialize(message);
        var doc = JsonDocument.Parse(json);

        Assert.Equal("Error", doc.RootElement.GetProperty("type").GetString());
        Assert.Equal("Lot is closed", doc.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public void ErrorMessage_DeserializesFromJson_WithCorrectValues()
    {
        var json = """
        {
            "type": "Error",
            "message": "Lot is closed"
        }
        """;

        var message = AuctionMessage.Deserialize(json);

        Assert.NotNull(message);
        Assert.IsType<ErrorMessage>(message);
        Assert.Equal("Error", message!.Type);
        Assert.Equal("Lot is closed", ((ErrorMessage)message).Message);
    }
}

