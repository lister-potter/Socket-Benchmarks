using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace EchoServer.Services;

public interface IConnectionManager
{
    string GenerateClientId();
    void AddConnection(string clientId, WebSocket webSocket);
    void RemoveConnection(string clientId);
    WebSocket? GetConnection(string clientId);
    List<string> GetAllClientIds();
}

public class ConnectionManager : IConnectionManager
{
    private readonly ConcurrentDictionary<string, WebSocket> _connections = new();

    public string GenerateClientId()
    {
        return Guid.NewGuid().ToString();
    }

    public void AddConnection(string clientId, WebSocket webSocket)
    {
        _connections.TryAdd(clientId, webSocket);
    }

    public void RemoveConnection(string clientId)
    {
        _connections.TryRemove(clientId, out _);
    }

    public WebSocket? GetConnection(string clientId)
    {
        _connections.TryGetValue(clientId, out var webSocket);
        return webSocket;
    }

    public List<string> GetAllClientIds()
    {
        return _connections.Keys.ToList();
    }
}

