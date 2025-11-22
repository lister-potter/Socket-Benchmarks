using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using EchoServer.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace EchoServer.Tests;

public class AuctionIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private HttpClient? _httpClient;

    public AuctionIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
        });
    }

    public async Task InitializeAsync()
    {
        _httpClient = _factory.CreateClient();
    }

    public Task DisposeAsync()
    {
        _httpClient?.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task EndToEnd_JoinLotAndPlaceBid_UpdatesLotState()
    {
        var wsClient = _factory.Server.CreateWebSocketClient();
        var uri = new Uri(_factory.Server.BaseAddress, "/");
        using var webSocket = await wsClient.ConnectAsync(uri, CancellationToken.None);

        // Join lot
        var joinLot = new JoinLotMessage { LotId = "lot-1" };
        var joinJson = JsonSerializer.Serialize(joinLot);
        await SendMessageAsync(webSocket, joinJson);

        // Receive lot update
        var response = await ReceiveMessageAsync(webSocket);
        Assert.NotNull(response);
        var lotUpdate = JsonSerializer.Deserialize<LotUpdateMessage>(response);
        Assert.NotNull(lotUpdate);
        Assert.Equal("lot-1", lotUpdate!.LotId);

        // Place bid
        var placeBid = new PlaceBidMessage
        {
            LotId = "lot-1",
            BidderId = "bidder-1",
            Amount = 200.0m
        };
        var bidJson = JsonSerializer.Serialize(placeBid);
        await SendMessageAsync(webSocket, bidJson);

        // Receive updated lot state
        response = await ReceiveMessageAsync(webSocket);
        Assert.NotNull(response);
        lotUpdate = JsonSerializer.Deserialize<LotUpdateMessage>(response);
        Assert.NotNull(lotUpdate);
        Assert.Equal(200.0m, lotUpdate!.CurrentBid);
        Assert.Equal("bidder-1", lotUpdate.CurrentBidder);

        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test complete", CancellationToken.None);
    }

    [Fact]
    public async Task DualMode_EchoMessage_WorksCorrectly()
    {
        var wsClient = _factory.Server.CreateWebSocketClient();
        var uri = new Uri(_factory.Server.BaseAddress, "/");
        using var webSocket = await wsClient.ConnectAsync(uri, CancellationToken.None);

        // Send echo message (not an auction message)
        var echoMessage = "Hello, World!";
        await SendMessageAsync(webSocket, echoMessage);

        // Should receive echo back
        var response = await ReceiveMessageAsync(webSocket);
        Assert.Equal(echoMessage, response);

        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test complete", CancellationToken.None);
    }

    [Fact]
    public async Task ErrorHandling_InvalidBid_ReturnsError()
    {
        var wsClient = _factory.Server.CreateWebSocketClient();
        var uri = new Uri(_factory.Server.BaseAddress, "/");
        using var webSocket = await wsClient.ConnectAsync(uri, CancellationToken.None);

        // Join lot first
        var joinLot = new JoinLotMessage { LotId = "lot-1" };
        await SendMessageAsync(webSocket, JsonSerializer.Serialize(joinLot));
        await ReceiveMessageAsync(webSocket); // Consume initial update

        // Place bid with amount too low
        var placeBid = new PlaceBidMessage
        {
            LotId = "lot-1",
            BidderId = "bidder-1",
            Amount = 50.0m // Lower than starting price of 100
        };
        await SendMessageAsync(webSocket, JsonSerializer.Serialize(placeBid));

        // Should receive error
        var response = await ReceiveMessageAsync(webSocket);
        Assert.NotNull(response);
        var error = JsonSerializer.Deserialize<ErrorMessage>(response);
        Assert.NotNull(error);
        Assert.Equal("Error", error!.Type);
        Assert.Contains("greater than", error.Message);

        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test complete", CancellationToken.None);
    }

    private async Task SendMessageAsync(WebSocket webSocket, string message)
    {
        var bytes = Encoding.UTF8.GetBytes(message);
        await webSocket.SendAsync(
            new ArraySegment<byte>(bytes),
            WebSocketMessageType.Text,
            true,
            CancellationToken.None);
    }

    private async Task<string?> ReceiveMessageAsync(WebSocket webSocket)
    {
        var buffer = new byte[1024 * 4];
        var result = await webSocket.ReceiveAsync(
            new ArraySegment<byte>(buffer),
            CancellationToken.None);

        if (result.MessageType == WebSocketMessageType.Text)
        {
            return Encoding.UTF8.GetString(buffer, 0, result.Count);
        }

        return null;
    }
}

