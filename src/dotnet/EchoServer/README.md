# .NET 10 Echo Server

A WebSocket server implemented in .NET 10 using Kestrel with Native AOT compilation. Supports both echo mode (for benchmarking) and auction simulation mode (for realistic workload testing).

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

## Dual Mode Support

The server supports two modes of operation:

### Echo Mode (Default)
Any message that is not an auction message will be echoed back to the client. This preserves the original echo server functionality for benchmarking.

### Auction Simulation Mode
The server maintains in-memory auction state (auctions, lots, and bids). Clients can subscribe to lots and place bids. The server processes bids with auction rules and broadcasts updates to subscribers.

**Message Routing:**
- Messages with `type: "JoinLot"` or `type: "PlaceBid"` → Auction mode
- All other messages → Echo mode

## Auction Message Protocol

### JoinLot Message
Subscribe to receive updates for a specific lot:
```json
{
  "type": "JoinLot",
  "lotId": "lot-1"
}
```

**Response:** `LotUpdate` message with current lot state

### PlaceBid Message
Place a bid on a lot:
```json
{
  "type": "PlaceBid",
  "lotId": "lot-1",
  "bidderId": "bidder-123",
  "amount": 150.50
}
```

**Response:** `LotUpdate` message if bid succeeds, or `Error` message if bid fails

### LotUpdate Message
Broadcast to all subscribers when a lot's state changes:
```json
{
  "type": "LotUpdate",
  "lotId": "lot-1",
  "currentBid": 150.50,
  "currentBidder": "bidder-123",
  "status": "Open"
}
```

### Error Message
Returned when an operation fails:
```json
{
  "type": "Error",
  "message": "Bid amount must be greater than current bid of 100.00"
}
```

## Auction Rules

- **Lot must be open:** Bids on closed lots are rejected
- **Bid must be higher:** Bid amount must be greater than the current bid
- **Atomic updates:** Lot state (current bid and bidder) is updated atomically with per-lot locking
- **Broadcast updates:** All subscribers to a lot receive `LotUpdate` messages when bids are accepted

## Initial State

On server startup, 10 default lots are created:
- `lot-1` through `lot-10`
- Starting prices: 100.0, 200.0, 300.0, ..., 1000.0
- All lots start in `Open` status
- All lots belong to `auction-1`

## Features

- **Dual mode operation:** Echo and auction modes work on the same connection
- **In-memory auction state:** Auctions, lots, and bids stored in memory
- **Per-lot locking:** Concurrent bids on different lots processed in parallel
- **Subscription management:** Clients can subscribe to multiple lots
- **Broadcast updates:** Real-time updates to all subscribers
- **Native AOT compilation:** Optimal performance
- **Supports text and binary messages:** Binary messages use echo mode
- **Handles multiple concurrent connections:** Efficient connection handling
- **Graceful connection cleanup:** Automatic subscription cleanup on disconnect

