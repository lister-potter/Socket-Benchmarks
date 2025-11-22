using System.Text.Json.Serialization;

namespace EchoServer.Models;

public class Lot
{
    [JsonPropertyName("lotId")]
    public string LotId { get; set; } = string.Empty;

    [JsonPropertyName("auctionId")]
    public string AuctionId { get; set; } = string.Empty;

    [JsonPropertyName("startingPrice")]
    public decimal StartingPrice { get; set; }

    [JsonPropertyName("currentBid")]
    public decimal CurrentBid { get; set; }

    [JsonPropertyName("currentBidder")]
    public string? CurrentBidder { get; set; }

    [JsonPropertyName("status")]
    [JsonConverter(typeof(JsonStringEnumConverter<LotStatus>))]
    public LotStatus Status { get; set; } = LotStatus.Open;
}

public enum LotStatus
{
    Open,
    Closed
}

