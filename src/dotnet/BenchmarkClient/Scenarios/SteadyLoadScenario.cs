using BenchmarkClient.Models;
using BenchmarkClient.Services;

namespace BenchmarkClient.Scenarios;

public class SteadyLoadScenario : BaseScenario
{
    public override string Name => "1000-client-steady-load";

    protected override async Task<List<ClientConnection>> CreateConnectionsAsync(
        ClientFactory factory,
        BenchmarkConfig config,
        CancellationToken cancellationToken)
    {
        var connections = new List<ClientConnection>();
        // Use config.ClientCount if explicitly set (> 1), otherwise use scenario default (1000)
        // If config.ClientCount is 1 (the default), use scenario default instead
        var totalClients = config.ClientCount > 1 ? config.ClientCount : 1000;
        Console.WriteLine($"SteadyLoadScenario: Creating {totalClients} clients (config.ClientCount was {config.ClientCount})");
        const int rampUpSeconds = 10;
        var clientsPerSecond = Math.Max(1, totalClients / rampUpSeconds);

        var startTime = DateTime.UtcNow;
        var endTime = startTime.AddSeconds(rampUpSeconds);

        while (DateTime.UtcNow < endTime && !cancellationToken.IsCancellationRequested)
        {
            var batchSize = Math.Min(clientsPerSecond, totalClients - connections.Count);
            if (batchSize <= 0) break;

            var batch = await factory.CreateConnectionsAsync(
                config.ServerUrl,
                batchSize,
                cancellationToken);
            connections.AddRange(batch);

            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }

        // Create remaining connections if any
        var remaining = totalClients - connections.Count;
        if (remaining > 0)
        {
            var remainingConnections = await factory.CreateConnectionsAsync(
                config.ServerUrl,
                remaining,
                cancellationToken);
            connections.AddRange(remainingConnections);
        }

        return connections;
    }
}

