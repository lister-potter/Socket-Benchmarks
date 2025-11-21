using System.Net;
using System.Net.WebSockets;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace EchoServer.Tests;

public class EchoServerTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private HttpClient? _httpClient;

    public EchoServerTests(WebApplicationFactory<Program> factory)
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
    public async Task Server_AcceptsWebSocketConnections()
    {
        var client = _factory.Server.CreateWebSocketClient();
        var uri = new Uri("http://localhost/");
        
        var webSocket = await client.ConnectAsync(uri, CancellationToken.None);
        
        Assert.NotNull(webSocket);
        Assert.Equal(WebSocketState.Open, webSocket.State);
        
        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test complete", CancellationToken.None);
    }

    [Fact]
    public async Task Server_RejectsNonWebSocketRequests()
    {
        var response = await _httpClient!.GetAsync("/");
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Server_EchoesTextMessages()
    {
        var client = _factory.Server.CreateWebSocketClient();
        var uri = new Uri("http://localhost/");
        
        var webSocket = await client.ConnectAsync(uri, CancellationToken.None);
        
        var message = "Hello, WebSocket!";
        var messageBytes = Encoding.UTF8.GetBytes(message);
        
        await webSocket.SendAsync(
            new ArraySegment<byte>(messageBytes),
            WebSocketMessageType.Text,
            true,
            CancellationToken.None);
        
        var buffer = new byte[1024];
        var result = await webSocket.ReceiveAsync(
            new ArraySegment<byte>(buffer),
            CancellationToken.None);
        
        var echoedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
        
        Assert.Equal(message, echoedMessage);
        Assert.Equal(WebSocketMessageType.Text, result.MessageType);
        
        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test complete", CancellationToken.None);
    }

    [Fact]
    public async Task Server_EchoesBinaryMessages()
    {
        var client = _factory.Server.CreateWebSocketClient();
        var uri = new Uri("http://localhost/");
        
        var webSocket = await client.ConnectAsync(uri, CancellationToken.None);
        
        var message = new byte[] { 1, 2, 3, 4, 5 };
        
        await webSocket.SendAsync(
            new ArraySegment<byte>(message),
            WebSocketMessageType.Binary,
            true,
            CancellationToken.None);
        
        var buffer = new byte[1024];
        var result = await webSocket.ReceiveAsync(
            new ArraySegment<byte>(buffer),
            CancellationToken.None);
        
        var echoedMessage = buffer.Take(result.Count).ToArray();
        
        Assert.Equal(message, echoedMessage);
        Assert.Equal(WebSocketMessageType.Binary, result.MessageType);
        
        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test complete", CancellationToken.None);
    }

    [Fact]
    public async Task Server_EchoesEmptyMessages()
    {
        var client = _factory.Server.CreateWebSocketClient();
        var uri = new Uri("http://localhost/");
        
        var webSocket = await client.ConnectAsync(uri, CancellationToken.None);
        
        var emptyMessage = Array.Empty<byte>();
        
        await webSocket.SendAsync(
            new ArraySegment<byte>(emptyMessage),
            WebSocketMessageType.Text,
            true,
            CancellationToken.None);
        
        var buffer = new byte[1024];
        var result = await webSocket.ReceiveAsync(
            new ArraySegment<byte>(buffer),
            CancellationToken.None);
        
        Assert.Equal(0, result.Count);
        
        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test complete", CancellationToken.None);
    }

    [Fact]
    public async Task Server_EchoesLargeMessages()
    {
        var client = _factory.Server.CreateWebSocketClient();
        var uri = new Uri("http://localhost/");
        
        var webSocket = await client.ConnectAsync(uri, CancellationToken.None);
        
        var largeMessage = new byte[64 * 1024]; // 64KB
        new Random().NextBytes(largeMessage);
        
        await webSocket.SendAsync(
            new ArraySegment<byte>(largeMessage),
            WebSocketMessageType.Binary,
            true,
            CancellationToken.None);
        
        var buffer = new byte[64 * 1024];
        var totalReceived = 0;
        
        while (totalReceived < largeMessage.Length)
        {
            var result = await webSocket.ReceiveAsync(
                new ArraySegment<byte>(buffer, totalReceived, buffer.Length - totalReceived),
                CancellationToken.None);
            
            totalReceived += result.Count;
            
            if (result.EndOfMessage)
                break;
        }
        
        var echoedMessage = buffer.Take(totalReceived).ToArray();
        Assert.Equal(largeMessage, echoedMessage);
        
        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test complete", CancellationToken.None);
    }

    [Fact]
    public async Task Server_HandlesMultipleConcurrentConnections()
    {
        var client = _factory.Server.CreateWebSocketClient();
        var uri = new Uri("http://localhost/");
        
        var tasks = new List<Task>();
        
        for (int i = 0; i < 10; i++)
        {
            var clientId = i;
            tasks.Add(Task.Run(async () =>
            {
                var webSocket = await client.ConnectAsync(uri, CancellationToken.None);
                var message = $"Client {clientId}";
                var messageBytes = Encoding.UTF8.GetBytes(message);
                
                await webSocket.SendAsync(
                    new ArraySegment<byte>(messageBytes),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None);
                
                var buffer = new byte[1024];
                var result = await webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    CancellationToken.None);
                
                var echoedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
                Assert.Equal(message, echoedMessage);
                
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test complete", CancellationToken.None);
            }));
        }
        
        await Task.WhenAll(tasks);
    }

    [Fact]
    public async Task Server_HandlesClientDisconnect()
    {
        var client = _factory.Server.CreateWebSocketClient();
        var uri = new Uri("http://localhost/");
        
        var webSocket = await client.ConnectAsync(uri, CancellationToken.None);
        
        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
        
        Assert.Equal(WebSocketState.Closed, webSocket.State);
    }

    [Fact]
    public async Task Server_HandlesConnectionCloseGracefully()
    {
        var client = _factory.Server.CreateWebSocketClient();
        var uri = new Uri("http://localhost/");
        
        var webSocket = await client.ConnectAsync(uri, CancellationToken.None);
        
        // Send a close frame
        await webSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Test close", CancellationToken.None);
        
        // Should receive close frame
        var buffer = new byte[1024];
        var result = await webSocket.ReceiveAsync(
            new ArraySegment<byte>(buffer),
            CancellationToken.None);
        
        Assert.Equal(WebSocketMessageType.Close, result.MessageType);
    }
}
