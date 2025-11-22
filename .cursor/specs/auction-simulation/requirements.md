# Requirements Document

## Introduction

The current WebSocket echo benchmark will be extended with a realistic auction simulation mode. The servers will support both echo functionality (existing) and auction simulation (new) modes. Clients can connect and choose which mode to use. In auction mode, the server maintains in-memory auction state including auctions, lots, and bids. Clients can subscribe to lots and place bids. The server processes bids with auction rules, updates lot state atomically, and broadcasts updates to subscribers. This provides a more realistic workload for benchmarking throughput, latency, CPU, and memory usage across .NET, Go, and Rust implementations while preserving the existing echo benchmark functionality.

## Requirements

### Requirement 1: Dual Mode Support

**User Story:** As a benchmark user, I want servers to support both echo and auction modes, so that I can run both types of benchmarks without maintaining separate server implementations.

#### Acceptance Criteria

1. WHEN a client connects to the server THEN the server SHALL support both echo and auction modes
2. WHEN a client sends a message in echo mode THEN the server SHALL echo the message back (existing behavior)
3. WHEN a client sends auction messages (JoinLot, PlaceBid) THEN the server SHALL process them in auction mode
4. WHEN the server receives a message THEN it SHALL determine the mode based on message type (echo for non-auction messages, auction for JoinLot/PlaceBid)
5. WHEN echo mode is used THEN the server SHALL behave identically to the current echo server implementation
6. WHEN auction mode is used THEN the server SHALL process auction messages and maintain auction state
7. WHEN both modes are used concurrently THEN they SHALL operate independently without interference

### Requirement 2: Auction State Management

**User Story:** As an auction server, I want to maintain in-memory state for auctions, lots, and bids, so that I can process bid operations and track auction progress.

#### Acceptance Criteria

1. WHEN the server starts THEN it SHALL initialize an empty in-memory repository for auctions, lots, and bids
2. WHEN a lot is created THEN it SHALL have a unique lot ID, auction ID, starting price, and status (Open/Closed)
3. WHEN a lot exists THEN it SHALL track the current highest bid amount and bidder ID
4. WHEN a bid is placed THEN it SHALL be stored with a bid ID, lot ID, bidder ID, bid amount, and timestamp
5. WHEN multiple lots exist THEN each lot SHALL maintain its own independent state
6. WHEN the server restarts THEN all state SHALL be lost (in-memory only, no persistence)

### Requirement 3: WebSocket Message Protocol

**User Story:** As a client, I want to send structured messages over WebSocket to join lots and place bids, so that I can participate in the auction.

#### Acceptance Criteria

1. WHEN a client sends a `JoinLot` message THEN the message SHALL contain a `lotId` field
2. WHEN a client sends a `PlaceBid` message THEN the message SHALL contain `lotId`, `bidderId`, and `amount` fields
3. WHEN the server receives a `JoinLot` message THEN it SHALL subscribe the client to receive updates for that lot
4. WHEN the server receives a `PlaceBid` message THEN it SHALL process the bid according to auction rules
5. WHEN the server processes a message THEN it SHALL respond with a `LotUpdate` message containing the current lot state
6. WHEN the server encounters an invalid message format THEN it SHALL respond with an error message
7. WHEN a client subscribes to multiple lots THEN it SHALL receive updates for all subscribed lots

### Requirement 4: Bid Processing Rules

**User Story:** As an auction server, I want to enforce auction rules when processing bids, so that only valid bids are accepted and lot state is updated correctly.

#### Acceptance Criteria

1. WHEN a bid is placed on a lot that is closed THEN the server SHALL reject the bid and return an error
2. WHEN a bid is placed with an amount less than or equal to the current highest bid THEN the server SHALL reject the bid and return an error
3. WHEN a bid is placed with an amount greater than the current highest bid AND the lot is open THEN the server SHALL accept the bid
4. WHEN a bid is accepted THEN the server SHALL update the lot's current bid amount and bidder ID atomically
5. WHEN a bid is accepted THEN the server SHALL store the bid in the repository
6. WHEN a bid is rejected THEN the server SHALL NOT update the lot state
7. WHEN a bid is rejected THEN the server SHALL return an error message to the client

### Requirement 5: Atomic Bid Updates with Per-Lot Locking

**User Story:** As an auction server, I want to update lot state atomically using per-lot locks, so that concurrent bids on the same lot are processed correctly without race conditions.

#### Acceptance Criteria

1. WHEN multiple bids are placed on the same lot concurrently THEN the server SHALL use per-lot locking to serialize bid processing
2. WHEN a bid is being processed for a lot THEN other bids for that lot SHALL wait until the current bid completes
3. WHEN bids are placed on different lots concurrently THEN they SHALL be processed in parallel (no global lock)
4. WHEN a lot's state is updated THEN the update SHALL be atomic (bid amount and bidder ID updated together)
5. WHEN a lot's state is updated THEN the update SHALL be visible to all subsequent operations on that lot
6. WHEN a bid is processed THEN the server SHALL acquire the lock for that lot, validate the bid, update state, and release the lock

### Requirement 6: Broadcast Updates to Subscribers

**User Story:** As a subscribed client, I want to receive updates when lots I'm subscribed to change, so that I can track auction progress in real-time.

#### Acceptance Criteria

1. WHEN a lot's state is updated (bid accepted) THEN the server SHALL broadcast a `LotUpdate` message to all clients subscribed to that lot
2. WHEN a `LotUpdate` message is broadcast THEN it SHALL contain the lot ID, current bid amount, current bidder ID, and lot status
3. WHEN a client is subscribed to a lot THEN it SHALL receive all updates for that lot
4. WHEN a client is not subscribed to a lot THEN it SHALL NOT receive updates for that lot
5. WHEN a client disconnects THEN it SHALL be removed from all lot subscriptions
6. WHEN multiple clients are subscribed to the same lot THEN all SHALL receive the same update message
7. WHEN a broadcast fails for a specific client THEN it SHALL NOT affect broadcasts to other clients

### Requirement 7: Repository Abstraction

**User Story:** As a developer, I want a repository abstraction for auction data, so that I can swap in different storage implementations (Postgres, DynamoDB) in the future without changing business logic.

#### Acceptance Criteria

1. WHEN the server accesses auction data THEN it SHALL use a repository interface/abstraction
2. WHEN a lot is created or retrieved THEN it SHALL use repository methods (e.g., `GetLot`, `CreateLot`, `UpdateLot`)
3. WHEN a bid is stored or retrieved THEN it SHALL use repository methods (e.g., `AddBid`, `GetBidsForLot`)
4. WHEN the repository is implemented THEN it SHALL provide in-memory storage for the initial implementation
5. WHEN the repository interface is defined THEN it SHALL be language-appropriate (interface in .NET/Go, trait in Rust)
6. WHEN the repository is swapped for a different implementation THEN the business logic SHALL remain unchanged

### Requirement 8: Identical Behavior Across Languages

**User Story:** As a performance engineer, I want identical auction behavior in .NET, Go, and Rust implementations, so that benchmark results are comparable and fair.

#### Acceptance Criteria

1. WHEN a bid is placed in any language implementation THEN the validation rules SHALL be identical
2. WHEN lot state is updated in any language implementation THEN the update logic SHALL be identical
3. WHEN messages are broadcast in any language implementation THEN the message format SHALL be identical
4. WHEN error conditions occur in any language implementation THEN the error responses SHALL be identical
5. WHEN a client subscribes to a lot in any language implementation THEN the subscription behavior SHALL be identical
6. WHEN concurrent bids are processed in any language implementation THEN the locking behavior SHALL be identical (per-lot, no global lock)

### Requirement 9: Message Format and Serialization

**User Story:** As a client, I want to send and receive messages in a consistent JSON format, so that I can interact with any server implementation.

#### Acceptance Criteria

1. WHEN a client sends a message THEN it SHALL be in JSON format
2. WHEN the server sends a message THEN it SHALL be in JSON format
3. WHEN a `JoinLot` message is sent THEN it SHALL have the format: `{"type": "JoinLot", "lotId": "string"}`
4. WHEN a `PlaceBid` message is sent THEN it SHALL have the format: `{"type": "PlaceBid", "lotId": "string", "bidderId": "string", "amount": number}`
5. WHEN a `LotUpdate` message is sent THEN it SHALL have the format: `{"type": "LotUpdate", "lotId": "string", "currentBid": number, "currentBidder": "string", "status": "Open|Closed"}`
6. WHEN an error message is sent THEN it SHALL have the format: `{"type": "Error", "message": "string"}`
7. WHEN messages are serialized/deserialized THEN all language implementations SHALL use the same field names and types

### Requirement 10: Connection Management

**User Story:** As an auction server, I want to manage client connections and subscriptions, so that clients can participate in auctions and receive updates.

#### Acceptance Criteria

1. WHEN a client connects THEN the server SHALL accept the WebSocket connection
2. WHEN a client disconnects THEN the server SHALL remove all subscriptions for that client
3. WHEN a client sends an invalid message THEN the server SHALL log the error and continue processing other messages
4. WHEN a client's connection is lost THEN the server SHALL clean up subscriptions without affecting other clients
5. WHEN multiple clients connect THEN each SHALL maintain independent subscriptions
6. WHEN a client reconnects THEN it SHALL be treated as a new client (previous subscriptions are lost)

### Requirement 11: Benchmarking Compatibility

**User Story:** As a performance engineer, I want the auction simulation to work with the existing benchmark client, so that I can measure throughput, latency, CPU, and memory usage.

#### Acceptance Criteria

1. WHEN the benchmark client connects THEN it SHALL be able to send `JoinLot` and `PlaceBid` messages
2. WHEN the benchmark client sends messages THEN the server SHALL process them and respond with `LotUpdate` messages
3. WHEN the benchmark client measures latency THEN it SHALL measure the time from sending `PlaceBid` to receiving `LotUpdate`
4. WHEN the benchmark client measures throughput THEN it SHALL count successful bid operations per second
5. WHEN the benchmark client monitors resources THEN it SHALL be able to detect the server process and collect CPU/memory metrics
6. WHEN benchmark scenarios run THEN they SHALL be able to simulate realistic auction load patterns
7. WHEN the benchmark client runs echo scenarios THEN it SHALL use echo mode (existing behavior)
8. WHEN the benchmark client runs auction scenarios THEN it SHALL use auction mode (new behavior)
9. WHEN the benchmark client is configured THEN it SHALL specify which mode to use (echo or auction)

### Requirement 12: Bid Logging and Performance Metrics

**User Story:** As a performance engineer, I want to log bid details along with performance metrics, so that I can analyze bid acceptance rates, failure patterns, and understand the auction workload characteristics during benchmarking.

#### Acceptance Criteria

1. WHEN a bid is placed THEN the system SHALL log bid details including lotId, bidderId, amount, and timestamp
2. WHEN a bid is placed THEN the system SHALL increment the total number of bids placed counter
3. WHEN a bid is accepted THEN the system SHALL increment the number of bids accepted counter
4. WHEN a bid is rejected THEN the system SHALL increment the number of bid failures counter
5. WHEN a bid is rejected due to bid amount being too low THEN the system SHALL record the failure reason as "BidTooLow"
6. WHEN a bid is rejected due to lot being closed THEN the system SHALL record the failure reason as "LotClosed"
7. WHEN a bid is rejected due to an error or invalid message format THEN the system SHALL record the failure reason as "Error"
8. WHEN bid metrics are collected THEN the system SHALL track the count of each failure reason type
9. WHEN performance metrics are generated THEN the report SHALL include bid statistics (total bids placed, bids accepted, bids failed)
10. WHEN performance metrics are generated THEN the report SHALL include failure reason breakdown (count per failure reason type)
11. WHEN bid details are logged THEN they SHALL include sufficient information to trace each bid operation (lotId, bidderId, amount, outcome, failure reason if applicable)
12. WHEN the benchmark client runs in auction mode THEN it SHALL collect and report bid metrics in addition to throughput and latency metrics

