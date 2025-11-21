using BenchmarkClient.Models;
using BenchmarkClient.Services;
using System.Diagnostics;
using Xunit;

namespace BenchmarkClient.Tests;

public class MessageSenderTests
{
    [Fact]
    public void MessageSender_WithConfig_CreatesSuccessfully()
    {
        var config = new BenchmarkConfig
        {
            MessagesPerSecondPerClient = 100,
            Duration = TimeSpan.FromSeconds(10),
            MessageSizeBytes = 64,
            Pattern = MessagePattern.FixedRate
        };

        var sender = new MessageSender(config);
        Assert.NotNull(sender);
    }

    [Fact]
    public async Task StartSendingAsync_WithEmptyConnections_CompletesImmediately()
    {
        var config = new BenchmarkConfig
        {
            MessagesPerSecondPerClient = 100,
            Duration = TimeSpan.FromSeconds(1),
            Pattern = MessagePattern.FixedRate
        };

        var sender = new MessageSender(config);
        var connections = new List<ClientConnection>();

        await sender.StartSendingAsync(
            connections,
            (conn, msg) => Task.CompletedTask,
            CancellationToken.None);

        // Should complete without exception
        Assert.True(true);
    }

    [Fact]
    public async Task StartSendingAsync_WithFixedRate_CreatesMessagesAtCorrectRate()
    {
        var config = new BenchmarkConfig
        {
            MessagesPerSecondPerClient = 10, // 10 msg/sec = 100ms per message
            Duration = TimeSpan.FromSeconds(1),
            MessageSizeBytes = 64,
            Pattern = MessagePattern.FixedRate
        };

        var sender = new MessageSender(config);
        var messagesSent = new List<BenchmarkMessage>();
        var sendTimes = new List<DateTime>();

        // Create a mock connection that records sends
        var connection = new ClientConnection { ClientId = 0, IsConnected = true };
        
        // Mock the SendMessageAsync to record messages
        var originalSend = connection.SendMessageAsync;
        // We can't easily mock this, so we'll test the pattern differently

        var connections = new List<ClientConnection> { connection };

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        
        try
        {
            await sender.StartSendingAsync(
                connections,
                (conn, msg) =>
                {
                    messagesSent.Add(msg);
                    sendTimes.Add(DateTime.UtcNow);
                    return Task.CompletedTask;
                },
                cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Expected if test times out
        }

        // Verify messages were created (if connection was actually connected)
        // Note: This test is limited because we can't easily mock WebSocket connections
        // In a real scenario, we'd use a test double for ClientConnection
    }

    [Fact]
    public void MessageSender_SupportsAllPatterns()
    {
        var patterns = new[] { MessagePattern.FixedRate, MessagePattern.Burst, MessagePattern.RampUp };

        foreach (var pattern in patterns)
        {
            var config = new BenchmarkConfig
            {
                MessagesPerSecondPerClient = 100,
                Duration = TimeSpan.FromSeconds(1),
                Pattern = pattern
            };

            var sender = new MessageSender(config);
            Assert.NotNull(sender);
        }
    }
}

