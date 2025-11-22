using System.Text.Json.Serialization;

namespace EchoServer.Models;

public class Bid
{
    [JsonPropertyName("bidId")]
    public string BidId { get; set; } = string.Empty;

    [JsonPropertyName("lotId")]
    public string LotId { get; set; } = string.Empty;

    [JsonPropertyName("bidderId")]
    public string BidderId { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

