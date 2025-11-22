using System.Text.Json;
using System.Text.Json.Serialization;

namespace EchoServer.Models;

public abstract class AuctionMessage
{
    [JsonIgnore]
    public abstract string Type { get; }

    public static AuctionMessage? Deserialize(string json)
    {
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("type", out var typeElement))
        {
            return null;
        }

        var type = typeElement.GetString();
        var context = AuctionMessageJsonContext.Default;
        return type switch
        {
            "JoinLot" => JsonSerializer.Deserialize(json, context.JoinLotMessage),
            "PlaceBid" => JsonSerializer.Deserialize(json, context.PlaceBidMessage),
            "LotUpdate" => JsonSerializer.Deserialize(json, context.LotUpdateMessage),
            "Error" => JsonSerializer.Deserialize(json, context.ErrorMessage),
            _ => null
        };
    }
}

public class JoinLotMessage : AuctionMessage
{
    [JsonIgnore]
    public override string Type => "JoinLot";

    [JsonPropertyName("type")]
    public string MessageType => "JoinLot";

    [JsonPropertyName("lotId")]
    public string LotId { get; set; } = string.Empty;
}

public class PlaceBidMessage : AuctionMessage
{
    [JsonIgnore]
    public override string Type => "PlaceBid";

    [JsonPropertyName("type")]
    public string MessageType => "PlaceBid";

    [JsonPropertyName("lotId")]
    public string LotId { get; set; } = string.Empty;

    [JsonPropertyName("bidderId")]
    public string BidderId { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }
}

public class LotUpdateMessage : AuctionMessage
{
    [JsonIgnore]
    public override string Type => "LotUpdate";

    [JsonPropertyName("type")]
    public string MessageType => "LotUpdate";

    [JsonPropertyName("lotId")]
    public string LotId { get; set; } = string.Empty;

    [JsonPropertyName("currentBid")]
    public decimal CurrentBid { get; set; }

    [JsonPropertyName("currentBidder")]
    public string? CurrentBidder { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "Open";
}

public class ErrorMessage : AuctionMessage
{
    [JsonIgnore]
    public override string Type => "Error";

    [JsonPropertyName("type")]
    public string MessageType => "Error";

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

