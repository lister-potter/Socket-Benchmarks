using BenchmarkClient.Models;
using BenchmarkClient.Services;

namespace BenchmarkClient.Scenarios;

public class SingleClientScenario : BaseScenario
{
    public override string Name => "single-client";

    protected override async Task<List<ClientConnection>> CreateConnectionsAsync(
        ClientFactory factory,
        BenchmarkConfig config,
        CancellationToken cancellationToken)
    {
        // Use config.ClientCount, defaulting to 1 if not set
        var clientCount = config.ClientCount > 0 ? config.ClientCount : 1;
        var connections = await factory.CreateConnectionsAsync(
            config.ServerUrl,
            clientCount,
            cancellationToken);
        return connections;
    }
}

