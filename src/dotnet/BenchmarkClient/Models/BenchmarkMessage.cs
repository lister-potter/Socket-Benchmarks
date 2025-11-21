using System.Text.Json;

namespace BenchmarkClient.Models;

public class BenchmarkMessage
{
    public int MessageId { get; set; }
    public int ClientId { get; set; }
    public DateTime SentTimestamp { get; set; }
    public byte[] Payload { get; set; } = Array.Empty<byte>();

    public string ToJson()
    {
        return JsonSerializer.Serialize(this);
    }

    public static BenchmarkMessage? FromJson(string json)
    {
        return JsonSerializer.Deserialize<BenchmarkMessage>(json);
    }
}

