using BenchmarkClient.Models;
using Xunit;

namespace BenchmarkClient.Tests;

public class BenchmarkMessageTests
{
    [Fact]
    public void ToJson_SerializesCorrectly()
    {
        var message = new BenchmarkMessage
        {
            MessageId = 1,
            ClientId = 2,
            SentTimestamp = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            Payload = new byte[] { 1, 2, 3, 4 }
        };

        var json = message.ToJson();
        Assert.NotNull(json);
        Assert.Contains("\"MessageId\":1", json);
        Assert.Contains("\"ClientId\":2", json);
    }

    [Fact]
    public void FromJson_DeserializesCorrectly()
    {
        var original = new BenchmarkMessage
        {
            MessageId = 1,
            ClientId = 2,
            SentTimestamp = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            Payload = new byte[] { 1, 2, 3, 4 }
        };

        var json = original.ToJson();
        var deserialized = BenchmarkMessage.FromJson(json);

        Assert.NotNull(deserialized);
        Assert.Equal(original.MessageId, deserialized!.MessageId);
        Assert.Equal(original.ClientId, deserialized.ClientId);
        Assert.Equal(original.Payload, deserialized.Payload);
    }
}

