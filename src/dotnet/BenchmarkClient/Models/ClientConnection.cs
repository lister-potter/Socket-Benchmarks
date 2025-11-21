using System.Net.WebSockets;
using System.Text;

namespace BenchmarkClient.Models;

public class ClientConnection
{
    public int ClientId { get; set; }
    public ClientWebSocket? WebSocket { get; set; }
    public DateTime ConnectedAt { get; set; }
    public int MessagesSent { get; set; }
    public int MessagesReceived { get; set; }
    public List<LatencyMeasurement> LatencyMeasurements { get; set; } = new();
    private bool _isConnected;
    private readonly SemaphoreSlim _sendLock = new(1, 1);

    /// <summary>
    /// Gets whether the connection is actually connected and ready to send/receive.
    /// Checks both the flag and the actual WebSocket state.
    /// </summary>
    public bool IsConnected
    {
        get => _isConnected && WebSocket != null && WebSocket.State == WebSocketState.Open;
        set => _isConnected = value;
    }

    public async Task<bool> ConnectAsync(string serverUrl, CancellationToken cancellationToken)
    {
        try
        {
            WebSocket = new ClientWebSocket();
            await WebSocket.ConnectAsync(new Uri(serverUrl), cancellationToken);
            
            // Verify the connection is actually open
            if (WebSocket.State == WebSocketState.Open)
            {
                ConnectedAt = DateTime.UtcNow;
                _isConnected = true;
                return true;
            }
            else
            {
                _isConnected = false;
                return false;
            }
        }
        catch (Exception ex)
        {
            _isConnected = false;
            // Only log connection failures for first few clients to avoid spam
            // This helps identify server issues without flooding the console
            if (ClientId < 10)
            {
                Console.WriteLine($"Connection failed for client {ClientId}: {ex.Message}");
            }
            return false;
        }
    }

    public async Task<bool> SendMessageAsync(BenchmarkMessage message, CancellationToken cancellationToken)
    {
        if (WebSocket == null || WebSocket.State != WebSocketState.Open)
        {
            return false;
        }

        await _sendLock.WaitAsync(cancellationToken);
        try
        {
            var json = message.ToJson();
            var bytes = Encoding.UTF8.GetBytes(json);
            await WebSocket.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                true,
                cancellationToken);
            MessagesSent++;
            return true;
        }
        catch
        {
            _isConnected = false;
            return false;
        }
        finally
        {
            _sendLock.Release();
        }
    }

    public async Task<BenchmarkMessage?> ReceiveMessageAsync(CancellationToken cancellationToken)
    {
        if (WebSocket == null || WebSocket.State != WebSocketState.Open)
        {
            return null;
        }

        try
        {
            var buffer = new byte[1024 * 64];
            var result = await WebSocket.ReceiveAsync(
                new ArraySegment<byte>(buffer),
                cancellationToken);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                _isConnected = false;
                return null;
            }

            var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
            var message = BenchmarkMessage.FromJson(json);
            if (message != null)
            {
                MessagesReceived++;
            }
            return message;
        }
        catch
        {
            IsConnected = false;
            return null;
        }
    }

    public async Task CloseAsync()
    {
        if (WebSocket != null)
        {
            try
            {
                if (WebSocket.State == WebSocketState.Open)
                {
                    await WebSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Closing",
                        CancellationToken.None);
                }
            }
            catch { }
            finally
            {
                WebSocket?.Dispose();
                _isConnected = false;
            }
        }
    }
}

