using System.Text.Json;
using BenchmarkClient.Interfaces;
using BenchmarkClient.Models;

namespace BenchmarkClient.Services;

public class JsonReportGenerator : IReportGenerator
{
    public void GenerateReport(BenchmarkMetrics metrics, BenchmarkConfig config, string outputPath)
    {
        // Log the client count being used in the report for debugging
        Console.WriteLine($"Generating report with clientCount: {config.ClientCount}");
        
        var report = new
        {
            schemaVersion = "1.0",
            metadata = new
            {
                serverLanguage = config.ServerLanguage,
                serverVersion = "1.0.0",
                scenarioName = config.ScenarioName,
                testStartTime = metrics.TestStartTime.ToString("O"),
                testEndTime = metrics.TestEndTime.ToString("O"),
                testDurationSeconds = (metrics.TestEndTime - metrics.TestStartTime).TotalSeconds,
                mode = config.Mode.ToString()
            },
            clientConfig = new
            {
                clientCount = config.ClientCount,
                messagesPerSecondPerClient = config.MessagesPerSecondPerClient,
                messageSizeBytes = config.MessageSizeBytes,
                messagePattern = config.Pattern.ToString()
            },
            serverConfig = new
            {
                port = UrlPortExtractor.ExtractPort(config.ServerUrl) ?? 8080,
                language = config.ServerLanguage,
                buildConfiguration = config.ServerLanguage == "dotnet" ? "NativeAOT" : "default"
            },
            throughput = new
            {
                totalMessagesSent = metrics.TotalMessagesSent,
                totalMessagesReceived = metrics.TotalMessagesReceived,
                messagesPerSecond = metrics.MessagesPerSecond,
                messagesPerSecondPerClient = metrics.MessagesPerSecond / Math.Max(1, config.ClientCount)
            },
            latency = new
            {
                p50Milliseconds = metrics.Latency.P50,
                p90Milliseconds = metrics.Latency.P90,
                p99Milliseconds = metrics.Latency.P99,
                maxMilliseconds = metrics.Latency.Max,
                minMilliseconds = metrics.Latency.Min,
                meanMilliseconds = metrics.Latency.Mean
            },
            errors = new
            {
                totalConnectionErrors = metrics.TotalConnectionErrors,
                totalMessageMismatches = metrics.TotalMessageMismatches,
                errorRatePerSecond = metrics.TotalConnectionErrors / Math.Max(1, (metrics.TestEndTime - metrics.TestStartTime).TotalSeconds)
            },
            bidMetrics = config.Mode == BenchmarkMode.Auction && metrics.BidMetrics != null ? new
            {
                totalBidsPlaced = metrics.BidMetrics.TotalBidsPlaced,
                bidsAccepted = metrics.BidMetrics.BidsAccepted,
                bidsFailed = metrics.BidMetrics.BidsFailed,
                acceptanceRate = metrics.BidMetrics.AcceptanceRate,
                failureRate = metrics.BidMetrics.FailureRate,
                failureReasonBreakdown = new
                {
                    BidTooLow = metrics.BidMetrics.FailureReasonBreakdown.GetValueOrDefault(BidFailureReason.BidTooLow, 0),
                    LotClosed = metrics.BidMetrics.FailureReasonBreakdown.GetValueOrDefault(BidFailureReason.LotClosed, 0),
                    Error = metrics.BidMetrics.FailureReasonBreakdown.GetValueOrDefault(BidFailureReason.Error, 0)
                }
            } : null,
            resourceUsage = new
            {
                cpu = metrics.ResourceUsage.Select(s => new
                {
                    timestamp = s.Timestamp.ToString("O"),
                    cpuPercent = s.CpuPercent
                }).ToArray(),
                memory = metrics.ResourceUsage.Select(s => new
                {
                    timestamp = s.Timestamp.ToString("O"),
                    memoryBytes = s.MemoryBytes
                }).ToArray()
            }
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        var json = JsonSerializer.Serialize(report, options);
        File.WriteAllText(outputPath, json);
    }
}

