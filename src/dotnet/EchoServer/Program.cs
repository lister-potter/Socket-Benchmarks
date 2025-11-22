using System.Net.WebSockets;
using System.Text;
using EchoServer.Repositories;
using EchoServer.Services;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var port = args.Length > 0 && int.TryParse(args[0], out var p) ? p : 8080;

// Initialize auction services
var lotRepository = new InMemoryLotRepository();
var bidRepository = new InMemoryBidRepository();
var lockManager = new LockManager();
var subscriptionService = new SubscriptionService();
var connectionManager = new ConnectionManager();
var bidService = new BidService(lotRepository, bidRepository, lockManager, subscriptionService);
var messageRouter = new MessageRouter(subscriptionService, bidService, lotRepository);

// Initialize default lots
await InitializeLotsAsync(lotRepository);

app.UseWebSockets();

app.Map("/", async (HttpContext context) =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = 400;
        await context.Response.WriteAsync("WebSocket connection required");
        return;
    }

    var webSocket = await context.WebSockets.AcceptWebSocketAsync();
    await HandleWebSocket(webSocket, messageRouter, subscriptionService, connectionManager);
});

await app.RunAsync($"http://localhost:{port}");

static async Task InitializeLotsAsync(ILotRepository lotRepository)
{
    // Create 10 default lots for testing
    for (int i = 1; i <= 10; i++)
    {
        await lotRepository.CreateLotAsync($"lot-{i}", "auction-1", 100.0m * i);
    }
}

static async Task HandleWebSocket(
    WebSocket webSocket,
    IMessageRouter messageRouter,
    ISubscriptionService subscriptionService,
    IConnectionManager connectionManager)
{
    var clientId = connectionManager.GenerateClientId();
    connectionManager.AddConnection(clientId, webSocket);
    
    try
    {
        var buffer = new byte[1024 * 64]; // 64KB buffer

        while (webSocket.State == WebSocketState.Open)
        {
            var result = await webSocket.ReceiveAsync(
                new ArraySegment<byte>(buffer), CancellationToken.None);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                await webSocket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "Connection closed",
                    CancellationToken.None);
                break;
            }

            if (result.MessageType == WebSocketMessageType.Text)
            {
                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                await messageRouter.RouteMessageAsync(message, webSocket, clientId);
            }
            else if (result.MessageType == WebSocketMessageType.Binary)
            {
                // Echo binary messages (existing behavior)
                await webSocket.SendAsync(
                    new ArraySegment<byte>(buffer, 0, result.Count),
                    WebSocketMessageType.Binary,
                    result.EndOfMessage,
                    CancellationToken.None);
            }
        }
    }
    catch (WebSocketException)
    {
        // Connection closed or error occurred
    }
    finally
    {
        // Cleanup on disconnect
        subscriptionService.UnsubscribeAll(clientId);
        connectionManager.RemoveConnection(clientId);
        
        if (webSocket.State == WebSocketState.Open)
        {
            await webSocket.CloseAsync(
                WebSocketCloseStatus.InternalServerError,
                "Internal error",
                CancellationToken.None);
        }
    }
}

// Make Program class available for testing
public partial class Program { }
