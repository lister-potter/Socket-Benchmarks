using BenchmarkClient.Models;

namespace BenchmarkClient.Scenarios;

public interface IScenario
{
    string Name { get; }
    Task<BenchmarkMetrics> ExecuteAsync(BenchmarkConfig config, CancellationToken cancellationToken);
}

