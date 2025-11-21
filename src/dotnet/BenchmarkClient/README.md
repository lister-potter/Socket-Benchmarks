# Benchmark Client

A .NET/C# 10 benchmark client for testing WebSocket echo servers. The client can simulate various load patterns and collect comprehensive performance metrics.

## Installation and Build

```bash
cd src/dotnet/BenchmarkClient
dotnet build -c Release
```

## Command-Line Usage

```
Usage: BenchmarkClient <scenario> [options]

Scenarios:
  single-client              Single client benchmark
  100-client-burst           100 clients connecting simultaneously
  1000-client-steady-load    1000 clients with gradual ramp-up

Options:
  --server-url <url>          WebSocket server URL (default: ws://localhost:8080)
  --server-language <lang>    Server language: dotnet, go, rust (default: dotnet)
  --server-pid <pid>          Server process ID for resource monitoring
  --duration <seconds>        Test duration in seconds (default: 30)
  --rate <msgs/sec>           Messages per second per client (default: 100)
  --message-size <bytes>      Message size in bytes (default: 64)
```

## Usage Examples

Run single client benchmark:
```bash
dotnet run -- single-client --server-url ws://localhost:8080
```

Run 100-client burst with custom duration:
```bash
dotnet run -- 100-client-burst --duration 60 --rate 50
```

Run 1000-client steady load with resource monitoring:
```bash
dotnet run -- 1000-client-steady-load --server-pid 12345 --duration 120
```

## Scenario Configuration

### Single Client Scenario
- **Clients**: 1
- **Default Duration**: 30 seconds
- **Default Rate**: 100 messages/second

### 100-Client Burst Scenario
- **Clients**: 100
- **Connection Pattern**: All clients connect simultaneously
- **Default Duration**: 60 seconds
- **Default Rate**: 10 messages/second per client

### 1000-Client Steady Load Scenario
- **Clients**: 1000
- **Connection Pattern**: Gradual ramp-up over 10 seconds
- **Default Duration**: 120 seconds
- **Default Rate**: 5 messages/second per client

## JSON Report Format

The benchmark client generates JSON reports with the following structure:

```json
{
  "schemaVersion": "1.0",
  "metadata": {
    "serverLanguage": "dotnet|go|rust",
    "serverVersion": "1.0.0",
    "scenarioName": "string",
    "testStartTime": "ISO8601 timestamp",
    "testEndTime": "ISO8601 timestamp",
    "testDurationSeconds": 0.0
  },
  "clientConfig": {
    "clientCount": 0,
    "messagesPerSecondPerClient": 0,
    "messageSizeBytes": 0,
    "messagePattern": "FixedRate|Burst|RampUp"
  },
  "serverConfig": {
    "port": 0,
    "language": "dotnet|go|rust",
    "buildConfiguration": "string"
  },
  "throughput": {
    "totalMessagesSent": 0,
    "totalMessagesReceived": 0,
    "messagesPerSecond": 0.0,
    "messagesPerSecondPerClient": 0.0
  },
  "latency": {
    "p50Milliseconds": 0.0,
    "p90Milliseconds": 0.0,
    "p99Milliseconds": 0.0,
    "maxMilliseconds": 0.0,
    "minMilliseconds": 0.0,
    "meanMilliseconds": 0.0
  },
  "errors": {
    "totalConnectionErrors": 0,
    "totalMessageMismatches": 0,
    "errorRatePerSecond": 0.0
  },
  "resourceUsage": {
    "cpu": [
      {
        "timestamp": "ISO8601 timestamp",
        "cpuPercent": 0.0
      }
    ],
    "memory": [
      {
        "timestamp": "ISO8601 timestamp",
        "memoryBytes": 0
      }
    ]
  }
}
```

Reports are saved with the naming pattern:
`benchmark-{scenarioName}-{serverLanguage}-{timestamp}.json`

## Features

- Configurable message rate and patterns
- Multiple test scenarios (single, burst, steady load)
- Comprehensive metrics collection (throughput, latency, resource usage)
- Cross-language server support (.NET, Go, Rust)
- JSON report generation for programmatic analysis
- Resource monitoring (CPU, memory) when server process ID is provided

