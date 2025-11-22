using System.Text.Json.Serialization;

namespace EchoServer.Models;

/// <summary>
/// JSON source generation context for Native AOT compatibility.
/// This enables JSON serialization/deserialization for auction messages without reflection.
/// </summary>
[JsonSourceGenerationOptions]
[JsonSerializable(typeof(JoinLotMessage))]
[JsonSerializable(typeof(PlaceBidMessage))]
[JsonSerializable(typeof(LotUpdateMessage))]
[JsonSerializable(typeof(ErrorMessage))]
public partial class AuctionMessageJsonContext : JsonSerializerContext
{
}

