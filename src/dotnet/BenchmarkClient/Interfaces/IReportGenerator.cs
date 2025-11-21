using BenchmarkClient.Models;

namespace BenchmarkClient.Interfaces;

public interface IReportGenerator
{
    void GenerateReport(BenchmarkMetrics metrics, BenchmarkConfig config, string outputPath);
}

