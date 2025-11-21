using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using BenchmarkClient.Models;
using BenchmarkClient.Scenarios;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace BenchmarkClient.IntegrationTests;

public class ClientServerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private string _serverUrl = "ws://localhost/";

    public ClientServerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
        });
    }

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task SingleClient_EchoesTextMessages()
    {
        var client = _factory.Server.CreateWebSocketClient();
        var uri = new Uri(_serverUrl);
        
        var webSocket = await client.ConnectAsync(uri, CancellationToken.None);
        
        var message = new BenchmarkMessage
        {
            MessageId = 1,
            ClientId = 0,
            SentTimestamp = DateTime.UtcNow,
            Payload = Encoding.UTF8.GetBytes("Test message")
        };
        
        var json = message.ToJson();
        var bytes = Encoding.UTF8.GetBytes(json);
        
        await webSocket.SendAsync(
            new ArraySegment<byte>(bytes),
            WebSocketMessageType.Text,
            true,
            CancellationToken.None);
        
        var buffer = new byte[1024];
        var result = await webSocket.ReceiveAsync(
            new ArraySegment<byte>(buffer),
            CancellationToken.None);
        
        var echoedJson = Encoding.UTF8.GetString(buffer, 0, result.Count);
        var echoedMessage = BenchmarkMessage.FromJson(echoedJson);
        
        Assert.NotNull(echoedMessage);
        Assert.Equal(message.MessageId, echoedMessage!.MessageId);
        Assert.Equal(message.ClientId, echoedMessage.ClientId);
        Assert.Equal(message.Payload, echoedMessage.Payload);
        
        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test complete", CancellationToken.None);
    }

    [Fact]
    public async Task SingleClient_EchoesBinaryMessages()
    {
        var client = _factory.Server.CreateWebSocketClient();
        var uri = new Uri(_serverUrl);
        
        var webSocket = await client.ConnectAsync(uri, CancellationToken.None);
        
        var payload = new byte[] { 1, 2, 3, 4, 5 };
        var message = new BenchmarkMessage
        {
            MessageId = 1,
            ClientId = 0,
            SentTimestamp = DateTime.UtcNow,
            Payload = payload
        };
        
        var json = message.ToJson();
        var bytes = Encoding.UTF8.GetBytes(json);
        
        await webSocket.SendAsync(
            new ArraySegment<byte>(bytes),
            WebSocketMessageType.Text,
            true,
            CancellationToken.None);
        
        var buffer = new byte[1024];
        var result = await webSocket.ReceiveAsync(
            new ArraySegment<byte>(buffer),
            CancellationToken.None);
        
        var echoedJson = Encoding.UTF8.GetString(buffer, 0, result.Count);
        var echoedMessage = BenchmarkMessage.FromJson(echoedJson);
        
        Assert.NotNull(echoedMessage);
        Assert.Equal(payload, echoedMessage!.Payload);
        
        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test complete", CancellationToken.None);
    }

    [Fact]
    public async Task MultipleClients_HandleConcurrentConnections()
    {
        var client = _factory.Server.CreateWebSocketClient();
        var uri = new Uri(_serverUrl);
        
        var tasks = new List<Task>();
        
        for (int i = 0; i < 5; i++)
        {
            var clientId = i;
            tasks.Add(Task.Run(async () =>
            {
                var webSocket = await client.ConnectAsync(uri, CancellationToken.None);
                var message = new BenchmarkMessage
                {
                    MessageId = clientId,
                    ClientId = clientId,
                    SentTimestamp = DateTime.UtcNow,
                    Payload = Encoding.UTF8.GetBytes($"Client {clientId}")
                };
                
                var json = message.ToJson();
                var bytes = Encoding.UTF8.GetBytes(json);
                
                await webSocket.SendAsync(
                    new ArraySegment<byte>(bytes),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None);
                
                var buffer = new byte[1024];
                var result = await webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    CancellationToken.None);
                
                var echoedJson = Encoding.UTF8.GetString(buffer, 0, result.Count);
                var echoedMessage = BenchmarkMessage.FromJson(echoedJson);
                
                Assert.NotNull(echoedMessage);
                Assert.Equal(clientId, echoedMessage!.ClientId);
                
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test complete", CancellationToken.None);
            }));
        }
        
        await Task.WhenAll(tasks);
    }

    [Fact]
    public async Task ConnectionLifecycle_HandlesConnectAndDisconnect()
    {
        var client = _factory.Server.CreateWebSocketClient();
        var uri = new Uri(_serverUrl);
        
        var webSocket = await client.ConnectAsync(uri, CancellationToken.None);
        Assert.Equal(WebSocketState.Open, webSocket.State);
        
        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
        Assert.Equal(WebSocketState.Closed, webSocket.State);
    }
}

