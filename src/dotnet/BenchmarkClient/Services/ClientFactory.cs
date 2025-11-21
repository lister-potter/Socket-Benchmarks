using BenchmarkClient.Models;

namespace BenchmarkClient.Services;

public class ClientFactory
{
    public async Task<List<ClientConnection>> CreateConnectionsAsync(
        string serverUrl,
        int clientCount,
        CancellationToken cancellationToken)
    {
        var connections = new List<ClientConnection>();
        var tasks = new List<Task<bool>>();

        // Stagger connection attempts slightly to avoid overwhelming the server
        // This is especially important for servers that might have connection limits
        const int batchSize = 100; // Connect in batches of 100
        var semaphore = new SemaphoreSlim(batchSize, batchSize);
        
        for (int i = 0; i < clientCount; i++)
        {
            var clientId = i;
            var connection = new ClientConnection { ClientId = clientId };
            connections.Add(connection);

            // Capture connection for closure
            var conn = connection;
            tasks.Add(Task.Run(async () =>
            {
                // Limit concurrent connection attempts
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    return await conn.ConnectAsync(serverUrl, cancellationToken);
                }
                finally
                {
                    semaphore.Release();
                }
            }, cancellationToken));
        }

        var results = await Task.WhenAll(tasks);
        var connectedCount = results.Count(r => r);
        
        // Give connections a moment to fully establish (especially important for Rust server)
        await Task.Delay(100, cancellationToken);
        
        // Re-check connection states after delay
        var actuallyConnected = connections.Count(c => c.IsConnected);
        
        if (actuallyConnected < clientCount)
        {
            Console.WriteLine($"Warning: Only {actuallyConnected} out of {clientCount} connections are fully established (initial connect: {connectedCount})");
        }
        else
        {
            Console.WriteLine($"Successfully connected {actuallyConnected} clients");
        }

        return connections;
    }

    public async Task CloseAllConnectionsAsync(List<ClientConnection> connections)
    {
        var tasks = connections.Select(c => c.CloseAsync());
        await Task.WhenAll(tasks);
    }
}

