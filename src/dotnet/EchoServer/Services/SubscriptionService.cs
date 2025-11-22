using System.Collections.Concurrent;
using System.Net.WebSockets;
using EchoServer.Models;

namespace EchoServer.Services;

public interface ISubscriptionService
{
    void Subscribe(string lotId, string clientId, WebSocket webSocket);
    void Unsubscribe(string clientId, string lotId);
    void UnsubscribeAll(string clientId);
    List<WebSocket> GetSubscribers(string lotId);
    Task BroadcastUpdateAsync(string lotId, LotUpdateMessage message);
}

public class SubscriptionService : ISubscriptionService
{
    // lotId -> Set of clientIds
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, WebSocket>> _subscriptions = new();
    
    // clientId -> Set of lotIds (for efficient cleanup)
    private readonly ConcurrentDictionary<string, HashSet<string>> _clientSubscriptions = new();

    public void Subscribe(string lotId, string clientId, WebSocket webSocket)
    {
        _subscriptions.AddOrUpdate(
            lotId,
            new ConcurrentDictionary<string, WebSocket> { [clientId] = webSocket },
            (key, existing) =>
            {
                existing[clientId] = webSocket;
                return existing;
            });

        _clientSubscriptions.AddOrUpdate(
            clientId,
            new HashSet<string> { lotId },
            (key, existing) =>
            {
                existing.Add(lotId);
                return existing;
            });
    }

    public void Unsubscribe(string clientId, string lotId)
    {
        if (_subscriptions.TryGetValue(lotId, out var subscribers))
        {
            subscribers.TryRemove(clientId, out _);
            if (subscribers.IsEmpty)
            {
                _subscriptions.TryRemove(lotId, out _);
            }
        }

        if (_clientSubscriptions.TryGetValue(clientId, out var lots))
        {
            lots.Remove(lotId);
            if (lots.Count == 0)
            {
                _clientSubscriptions.TryRemove(clientId, out _);
            }
        }
    }

    public void UnsubscribeAll(string clientId)
    {
        if (_clientSubscriptions.TryGetValue(clientId, out var lots))
        {
            foreach (var lotId in lots.ToList())
            {
                Unsubscribe(clientId, lotId);
            }
        }
    }

    public List<WebSocket> GetSubscribers(string lotId)
    {
        if (_subscriptions.TryGetValue(lotId, out var subscribers))
        {
            return subscribers.Values.Where(ws => ws.State == WebSocketState.Open).ToList();
        }
        return new List<WebSocket>();
    }

    public async Task BroadcastUpdateAsync(string lotId, LotUpdateMessage message)
    {
        var subscribers = GetSubscribers(lotId);
        var tasks = new List<Task>();

        foreach (var webSocket in subscribers)
        {
            tasks.Add(SendMessageAsync(webSocket, message));
        }

        await Task.WhenAll(tasks);
    }

    private async Task SendMessageAsync(WebSocket webSocket, LotUpdateMessage message)
    {
        try
        {
            if (webSocket.State == WebSocketState.Open)
            {
                var context = Models.AuctionMessageJsonContext.Default;
                var json = System.Text.Json.JsonSerializer.Serialize(message, context.LotUpdateMessage);
                var bytes = System.Text.Encoding.UTF8.GetBytes(json);
                await webSocket.SendAsync(
                    new ArraySegment<byte>(bytes),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None);
            }
        }
        catch
        {
            // If send fails, remove the subscriber
            // Note: We'd need the clientId to properly unsubscribe, but for now we'll just skip
        }
    }
}

