# .NET 10 Echo Server

A minimal WebSocket echo server implemented in .NET 10 using Kestrel with Native AOT compilation.

## Build Instructions

```bash
cd src/dotnet/EchoServer
dotnet publish -c Release -r <RID> --self-contained
```

Where `<RID>` is the runtime identifier (e.g., `linux-x64`, `win-x64`, `osx-x64`).

For Native AOT:
```bash
dotnet publish -c Release -r <RID> --self-contained /p:PublishAot=true
```

## Command-Line Arguments

```
Usage: EchoServer [port]

Arguments:
  port    Port number to listen on (default: 8080)
```

## Usage Examples

Start server on default port (8080):
```bash
./EchoServer
```

Start server on custom port:
```bash
./EchoServer 9000
```

## Features

- Minimal WebSocket echo server
- Native AOT compilation for optimal performance
- Supports text and binary messages
- Handles multiple concurrent connections
- Graceful connection cleanup

