using BenchmarkClient.Models;
using Xunit;

namespace BenchmarkClient.Tests;

public class BenchmarkConfigTests
{
    [Fact]
    public void BenchmarkConfig_DefaultValues_AreSetCorrectly()
    {
        var config = new BenchmarkConfig();

        Assert.Equal("ws://localhost:8080", config.ServerUrl);
        Assert.Equal(1, config.ClientCount);
        Assert.Equal(100, config.MessagesPerSecondPerClient);
        Assert.Equal(64, config.MessageSizeBytes);
        Assert.Equal(TimeSpan.FromSeconds(30), config.Duration);
        Assert.Equal(MessagePattern.FixedRate, config.Pattern);
        Assert.Equal("single-client", config.ScenarioName);
        Assert.Equal("dotnet", config.ServerLanguage);
    }

    [Fact]
    public void BenchmarkConfig_CanSetCustomValues()
    {
        var config = new BenchmarkConfig
        {
            ServerUrl = "ws://example.com:9000",
            ClientCount = 100,
            MessagesPerSecondPerClient = 50,
            MessageSizeBytes = 128,
            Duration = TimeSpan.FromSeconds(60),
            Pattern = MessagePattern.Burst,
            ScenarioName = "test-scenario",
            ServerLanguage = "go"
        };

        Assert.Equal("ws://example.com:9000", config.ServerUrl);
        Assert.Equal(100, config.ClientCount);
        Assert.Equal(50, config.MessagesPerSecondPerClient);
        Assert.Equal(128, config.MessageSizeBytes);
        Assert.Equal(TimeSpan.FromSeconds(60), config.Duration);
        Assert.Equal(MessagePattern.Burst, config.Pattern);
        Assert.Equal("test-scenario", config.ScenarioName);
        Assert.Equal("go", config.ServerLanguage);
    }
}

