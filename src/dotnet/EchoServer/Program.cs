using System.Net.WebSockets;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var port = args.Length > 0 && int.TryParse(args[0], out var p) ? p : 8080;

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
    await HandleWebSocket(webSocket);
});

await app.RunAsync($"http://localhost:{port}");

static async Task HandleWebSocket(WebSocket webSocket)
{
    var buffer = new byte[1024 * 64]; // 64KB buffer

    try
    {
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

            // Echo the message back
            await webSocket.SendAsync(
                new ArraySegment<byte>(buffer, 0, result.Count),
                result.MessageType,
                result.EndOfMessage,
                CancellationToken.None);
        }
    }
    catch (WebSocketException)
    {
        // Connection closed or error occurred
    }
    finally
    {
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
