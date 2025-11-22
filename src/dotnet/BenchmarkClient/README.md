# Benchmark Client

A .NET/C# 10 benchmark client for testing WebSocket servers. The client supports both echo mode (for basic benchmarking) and auction simulation mode (for realistic workload testing). It can simulate various load patterns and collect comprehensive performance metrics.

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
  --server-pid <pid>          Server process ID for resource monitoring (auto-detected from port if not specified)
  --duration <seconds>        Test duration in seconds (default: 30)
  --rate <msgs/sec>           Messages per second per client (default: 100)
  --message-size <bytes>      Message size in bytes (default: 64)
  --mode <mode>              Benchmark mode: Echo, Auction (default: Echo)
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

Run 1000-client steady load with resource monitoring (auto-detected PID):
```bash
dotnet run -- 1000-client-steady-load --server-url ws://localhost:8080 --duration 120
```

Run 1000-client steady load with explicit server PID:
```bash
dotnet run -- 1000-client-steady-load --server-pid 12345 --duration 120
```

Run auction mode benchmark:
```bash
dotnet run -- single-client --mode Auction --server-url ws://localhost:8080 --duration 60
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

## Automatic PID Detection

The benchmark client can automatically detect the server process ID (PID) by querying the operating system to find which process is listening on the TCP port specified in the server URL. This eliminates the need to manually specify `--server-pid` in most cases.

### How It Works

1. When `--server-pid` is **not** provided, the client extracts the port number from `--server-url`
2. The client queries the OS to find the process listening on that port:
   - **Windows**: Uses `netstat -ano`
   - **Linux/macOS**: Uses `lsof -i :<port>`
3. If a process is found, its PID is automatically set for resource monitoring
4. If no process is found or detection fails, a warning is logged and the benchmark continues without resource monitoring

### Prerequisites

- **Linux/macOS**: The `lsof` command must be available in PATH
- **Windows**: `netstat` is built-in and always available
- **Permissions**: The user must have sufficient permissions to query process information (usually not an issue for local processes)

### Examples

Automatic detection (recommended):
```bash
# Start your server first, then run:
dotnet run -- single-client --server-url ws://localhost:8080
# PID will be auto-detected from port 8080
```

Explicit PID (overrides auto-detection):
```bash
dotnet run -- single-client --server-url ws://localhost:8080 --server-pid 12345
# Uses the explicitly provided PID, skips auto-detection
```

### Troubleshooting

If automatic PID detection fails, you may see warnings like:
- `Warning: No process found listening on port {port}.` - The server may not be running or listening on a different port
- `Warning: 'lsof' command not found.` - Install `lsof` on Linux/macOS: `sudo apt-get install lsof` or `brew install lsof`
- `Warning: Insufficient permissions to query process information.` - Run with appropriate permissions or use `--server-pid` explicitly

## Dual Mode Support

The benchmark client supports both echo and auction modes:

### Echo Mode (Default)
When using `--mode Echo` (or omitting the `--mode` option), the client sends `BenchmarkMessage` objects that the server echoes back. This is the traditional echo benchmark behavior.

### Auction Simulation Mode
When using `--mode Auction`, the client:
1. Automatically sends `JoinLot` messages to subscribe each client to a lot (round-robin across lots 1-10)
2. Sends `PlaceBid` messages at the configured rate
3. Receives `LotUpdate` or `Error` responses from the server
4. Tracks throughput and latency for bid operations

**Auction Mode Behavior:**
- Each client is assigned to a lot based on client ID (lot-1 through lot-10)
- Bid amounts start from the lot's base price and increment with each bid
- The client tracks all received messages (LotUpdate, Error) for throughput metrics
- Latency is measured from bid sent to response received

**Example:**
```bash
# Run auction benchmark
dotnet run -- single-client --mode Auction --rate 50 --duration 60

# Run echo benchmark (default)
dotnet run -- single-client --rate 100 --duration 30
```

## Features

- Configurable message rate and patterns
- Multiple test scenarios (single, burst, steady load)
- Comprehensive metrics collection (throughput, latency, resource usage)
- Cross-language server support (.NET, Go, Rust)
- JSON report generation for programmatic analysis
- Resource monitoring (CPU, memory) with automatic PID detection
- Supports both echo and auction server modes

