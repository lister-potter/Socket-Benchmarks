using BenchmarkClient.Models;
using BenchmarkClient.Services;

namespace BenchmarkClient.Scenarios;

public class BurstScenario : BaseScenario
{
    public override string Name => "100-client-burst";

    protected override async Task<List<ClientConnection>> CreateConnectionsAsync(
        ClientFactory factory,
        BenchmarkConfig config,
        CancellationToken cancellationToken)
    {
        // Use config.ClientCount if explicitly set (> 1), otherwise use scenario default (100)
        // If config.ClientCount is 1 (the default), use scenario default instead
        var clientCount = config.ClientCount > 1 ? config.ClientCount : 100;
        var connections = await factory.CreateConnectionsAsync(
            config.ServerUrl,
            clientCount,
            cancellationToken);
        return connections;
    }
}

