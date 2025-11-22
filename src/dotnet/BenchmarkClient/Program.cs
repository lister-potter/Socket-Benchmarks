using BenchmarkClient.Interfaces;
using BenchmarkClient.Models;
using BenchmarkClient.Scenarios;
using BenchmarkClient.Services;

if (args.Length == 0)
{
    Console.WriteLine("Usage: BenchmarkClient <scenario> [options]");
    Console.WriteLine("Scenarios: single-client, 100-client-burst, 1000-client-steady-load");
    Console.WriteLine("Options:");
    Console.WriteLine("  --server-url <url>     WebSocket server URL (default: ws://localhost:8080)");
    Console.WriteLine("  --server-language <lang>  Server language: dotnet, go, rust (default: dotnet)");
    Console.WriteLine("  --server-pid <pid>    Server process ID for resource monitoring (auto-detected if not specified)");
    Console.WriteLine("  --client-count <n>    Number of concurrent clients (overrides scenario default)");
    Console.WriteLine("  --duration <seconds>   Test duration in seconds (default: 30)");
    Console.WriteLine("  --rate <msgs/sec>     Messages per second per client (default: 100)");
    Console.WriteLine("  --message-size <bytes> Message size in bytes (default: 64)");
    Console.WriteLine("  --pattern <pattern>   Message pattern: FixedRate, Burst, RampUp (default: FixedRate)");
    Console.WriteLine("  --mode <mode>        Benchmark mode: Echo, Auction (default: Echo)");
    Environment.Exit(1);
}

var scenarioName = args[0];
var config = ParseConfig(args);

// Automatic PID detection if not explicitly provided
if (!config.ServerProcessId.HasValue)
{
    var port = UrlPortExtractor.ExtractPort(config.ServerUrl);
    if (port.HasValue)
    {
        var pidDetector = new PidDetector();
        var detectedPid = pidDetector.DetectPidByPort(port.Value);
        if (detectedPid.HasValue)
        {
            config.ServerProcessId = detectedPid;
            Console.WriteLine($"Detected server PID: {detectedPid} (listening on port {port.Value})");
        }
    }
    else
    {
        Console.WriteLine($"Error: Could not parse server URL '{config.ServerUrl}' to extract port. Skipping automatic PID detection.");
    }
}
else
{
    Console.WriteLine($"Using explicit server PID: {config.ServerProcessId.Value}");
}

IScenario scenario = scenarioName switch
{
    "single-client" => new SingleClientScenario(),
    "100-client-burst" => new BurstScenario(),
    "1000-client-steady-load" => new SteadyLoadScenario(),
    _ => throw new ArgumentException($"Unknown scenario: {scenarioName}")
};

config.ScenarioName = scenario.Name;

Console.WriteLine($"Starting benchmark: {scenario.Name}");
Console.WriteLine($"Server URL: {config.ServerUrl}");
Console.WriteLine($"Mode: {config.Mode}");
Console.WriteLine($"Duration: {config.Duration.TotalSeconds}s");
Console.WriteLine($"Message rate: {config.MessagesPerSecondPerClient} msg/s per client");
Console.WriteLine($"Target client count: {config.ClientCount} (will use scenario default if 1)");

var cancellationTokenSource = new CancellationTokenSource();
var metrics = await scenario.ExecuteAsync(config, cancellationTokenSource.Token);

Console.WriteLine("\nBenchmark Results:");
Console.WriteLine($"Actual client count: {config.ClientCount}");
Console.WriteLine($"Messages sent: {metrics.TotalMessagesSent}");
Console.WriteLine($"Messages received: {metrics.TotalMessagesReceived}");
Console.WriteLine($"Throughput: {metrics.MessagesPerSecond:F2} msg/s");
Console.WriteLine($"Throughput per client: {metrics.MessagesPerSecond / Math.Max(1, config.ClientCount):F2} msg/s");
Console.WriteLine($"Latency - P50: {metrics.Latency.P50:F2}ms, P90: {metrics.Latency.P90:F2}ms, P99: {metrics.Latency.P99:F2}ms");
Console.WriteLine($"Errors: {metrics.TotalConnectionErrors} connections, {metrics.TotalMessageMismatches} mismatches");

// Generate report
var reportGenerator = new JsonReportGenerator();
var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
var reportPath = $"benchmark-{scenario.Name}-{config.ServerLanguage}-{timestamp}.json";
reportGenerator.GenerateReport(metrics, config, reportPath);
Console.WriteLine($"\nReport saved to: {reportPath}");

static BenchmarkConfig ParseConfig(string[] args)
{
    var config = new BenchmarkConfig();

    for (int i = 1; i < args.Length; i++)
    {
        switch (args[i])
        {
            case "--server-url" when i + 1 < args.Length:
                config.ServerUrl = args[++i];
                break;
            case "--server-language" when i + 1 < args.Length:
                config.ServerLanguage = args[++i];
                break;
            case "--server-pid" when i + 1 < args.Length:
                if (int.TryParse(args[++i], out var pid))
                {
                    config.ServerProcessId = pid;
                }
                break;
            case "--client-count" when i + 1 < args.Length:
                if (int.TryParse(args[++i], out var clientCount) && clientCount > 0)
                {
                    config.ClientCount = clientCount;
                }
                break;
            case "--duration" when i + 1 < args.Length:
                if (int.TryParse(args[++i], out var duration))
                {
                    config.Duration = TimeSpan.FromSeconds(duration);
                }
                break;
            case "--rate" when i + 1 < args.Length:
                if (int.TryParse(args[++i], out var rate))
                {
                    config.MessagesPerSecondPerClient = rate;
                }
                break;
            case "--message-size" when i + 1 < args.Length:
                if (int.TryParse(args[++i], out var size))
                {
                    config.MessageSizeBytes = size;
                }
                break;
            case "--pattern" when i + 1 < args.Length:
                if (Enum.TryParse<MessagePattern>(args[++i], true, out var pattern))
                {
                    config.Pattern = pattern;
                }
                break;
            case "--mode" when i + 1 < args.Length:
                if (Enum.TryParse<BenchmarkMode>(args[++i], true, out var mode))
                {
                    config.Mode = mode;
                }
                break;
        }
    }

    return config;
}

