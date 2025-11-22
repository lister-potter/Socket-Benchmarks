using System.Text.Json;
using System.Text.Json.Serialization;

namespace BenchmarkClient.Models;

// Auction message types for benchmark client
// These match the server's auction message protocol

public class JoinLotMessage
{
    [JsonPropertyName("type")]
    public string Type => "JoinLot";

    [JsonPropertyName("lotId")]
    public string LotId { get; set; } = string.Empty;

    public string ToJson() => JsonSerializer.Serialize(this);
}

public class PlaceBidMessage
{
    [JsonPropertyName("type")]
    public string Type => "PlaceBid";

    [JsonPropertyName("lotId")]
    public string LotId { get; set; } = string.Empty;

    [JsonPropertyName("bidderId")]
    public string BidderId { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    public string ToJson() => JsonSerializer.Serialize(this);
}

public class LotUpdateMessage
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("lotId")]
    public string LotId { get; set; } = string.Empty;

    [JsonPropertyName("currentBid")]
    public decimal CurrentBid { get; set; }

    [JsonPropertyName("currentBidder")]
    public string? CurrentBidder { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    public static LotUpdateMessage? FromJson(string json)
    {
        return JsonSerializer.Deserialize<LotUpdateMessage>(json);
    }
}

public class ErrorMessage
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    public static ErrorMessage? FromJson(string json)
    {
        return JsonSerializer.Deserialize<ErrorMessage>(json);
    }
}

