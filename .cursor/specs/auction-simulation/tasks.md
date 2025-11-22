# Implementation Plan

- [x] 1. Create data models and message types
    - [x] 1.1 Create Lot model with LotId, AuctionId, StartingPrice, CurrentBid, CurrentBidder, Status fields
        - Write Lot class/struct in .NET, Go, and Rust
        - Add JSON serialization attributes/annotations
        - Write unit tests for serialization/deserialization
        - _Requirements: 2.1, 2.2, 2.3, 9.1, 9.2, 9.7_

    - [x] 1.2 Create Bid model with BidId, LotId, BidderId, Amount, Timestamp fields
        - Write Bid class/struct in .NET, Go, and Rust
        - Add JSON serialization attributes/annotations
        - Write unit tests for serialization/deserialization
        - _Requirements: 2.4, 9.1, 9.2, 9.7_

    - [x] 1.3 Create message type models (JoinLot, PlaceBid, LotUpdate, Error)
        - Write message classes/structs in .NET, Go, and Rust
        - Add JSON serialization with type field
        - Write unit tests for message serialization/deserialization
        - _Requirements: 3.1, 3.2, 9.3, 9.4, 9.5, 9.6, 9.7_

- [x] 2. Implement repository interfaces and in-memory implementations
    - [x] 2.1 Define repository interfaces (ILotRepository, IBidRepository)
        - Create interface/trait definitions in .NET, Go, and Rust
        - Define methods: GetLot, CreateLot, UpdateLot, GetAllLots, AddBid, GetBidsForLot, GetHighestBid
        - Write interface documentation
        - _Requirements: 7.1, 7.2, 7.3, 7.5_

    - [x] 2.2 Implement in-memory lot repository
        - Create InMemoryLotRepository class/struct
        - Use thread-safe data structures (ConcurrentDictionary in .NET, sync.Map in Go, Arc<Mutex<HashMap>> in Rust)
        - Implement all interface methods
        - Write unit tests for CRUD operations and thread-safety
        - _Requirements: 2.1, 2.2, 2.3, 2.5, 7.4_

    - [x] 2.3 Implement in-memory bid repository
        - Create InMemoryBidRepository class/struct
        - Use thread-safe data structures for storing bids per lot
        - Implement all interface methods
        - Write unit tests for bid storage and retrieval
        - _Requirements: 2.4, 7.4_

- [x] 3. Implement lock manager for per-lot locking
    - [x] 3.1 Create LockManager service
        - Implement per-lot lock storage (ConcurrentDictionary<string, SemaphoreSlim> in .NET, sync.Map with Mutex in Go, HashMap with Mutex in Rust)
        - Implement AcquireLock and ReleaseLock methods
        - Add timeout support for deadlock prevention
        - Write unit tests for lock acquisition, release, and timeout
        - _Requirements: 5.1, 5.2, 5.3, 5.6_

- [x] 4. Implement subscription service
    - [x] 4.1 Create SubscriptionService
        - Implement subscription storage (lotId -> Set<clientId>)
        - Implement Subscribe, Unsubscribe, UnsubscribeAll methods
        - Use thread-safe data structures
        - Write unit tests for subscription operations
        - _Requirements: 3.3, 3.7, 6.3, 6.4, 10.2, 10.5_

    - [x] 4.2 Implement broadcast functionality
        - Add BroadcastUpdate method to SubscriptionService
        - Implement sending LotUpdate messages to all subscribers
        - Handle broadcast failures gracefully (remove failed subscribers)
        - Write unit tests for broadcast to multiple subscribers and failure handling
        - _Requirements: 6.1, 6.2, 6.6, 6.7_

- [x] 5. Implement bid service with validation and atomic updates
    - [x] 5.1 Create BidService
        - Implement bid validation logic (lot open, bid amount > current bid)
        - Integrate with LockManager for per-lot locking
        - Integrate with repositories for state updates
        - Write unit tests for validation rules
        - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5, 4.6, 4.7, 5.4, 5.5_

    - [x] 5.2 Implement atomic bid processing
        - Acquire lock, validate bid, update lot state, store bid, release lock
        - Ensure atomic update of CurrentBid and CurrentBidder
        - Integrate with SubscriptionService for broadcasting
        - Write unit tests for atomic updates and concurrent bid handling
        - _Requirements: 4.4, 5.1, 5.2, 5.3, 5.4, 5.5, 5.6_

- [x] 6. Implement message router for dual mode support
    - [x] 6.1 Create MessageRouter service
        - Parse incoming JSON messages to extract type field
        - Route JoinLot/PlaceBid messages to auction handler
        - Route all other messages to echo handler
        - Handle invalid/malformed messages
        - Write unit tests for message routing logic
        - _Requirements: 1.4, 1.5, 1.6, 1.7, 3.6_

    - [x] 6.2 Create AuctionMessageHandler
        - Handle JoinLot messages (delegate to SubscriptionService)
        - Handle PlaceBid messages (delegate to BidService)
        - Send responses and error messages to clients
        - Write unit tests for message handling
        - _Requirements: 3.3, 3.4, 3.5, 3.6, 3.7_

- [x] 7. Integrate auction functionality into existing servers
    - [x] 7.1 Update .NET server to use MessageRouter
        - Modify HandleWebSocket to use MessageRouter
        - Preserve existing echo functionality
        - Initialize auction services (repositories, lock manager, subscription service, bid service)
        - Write integration tests for dual mode operation
        - _Requirements: 1.1, 1.2, 1.3, 1.5, 1.6, 1.7_

    - [x] 7.2 Update Go server to use MessageRouter
        - Modify connection handler to use message router
        - Preserve existing echo functionality
        - Initialize auction services
        - Write integration tests for dual mode operation
        - _Requirements: 1.1, 1.2, 1.3, 1.5, 1.6, 1.7, 8.1, 8.2, 8.3, 8.4, 8.5, 8.6_

    - [x] 7.3 Update Rust server to use MessageRouter
        - Modify handle_connection to use message router
        - Preserve existing echo functionality
        - Initialize auction services
        - Write integration tests for dual mode operation
        - _Requirements: 1.1, 1.2, 1.3, 1.5, 1.6, 1.7, 8.1, 8.2, 8.3, 8.4, 8.5, 8.6_

- [x] 8. Implement connection manager for client tracking
    - [x] 8.1 Create ConnectionManager service
        - Generate unique client IDs (UUID/GUID)
        - Associate WebSocket connections with client IDs
        - Track active connections
        - Handle connection cleanup on disconnect
        - Write unit tests for client ID generation and tracking
        - _Requirements: 10.1, 10.2, 10.4, 10.6_

    - [x] 8.2 Integrate ConnectionManager with subscription cleanup
        - Unsubscribe all lots when client disconnects
        - Remove client from connection tracking
        - Write integration tests for cleanup on disconnect
        - _Requirements: 6.5, 10.2, 10.4_

- [x] 9. Add lot initialization and management
    - [x] 9.1 Implement lot creation functionality
        - Add CreateLot method to repository
        - Add endpoint/handler for lot creation (optional, for testing)
        - Write unit tests for lot creation
        - _Requirements: 2.2, 2.3_

    - [x] 9.2 Add lot status management
        - Implement lot closing functionality
        - Update bid validation to check lot status
        - Write unit tests for status checks
        - _Requirements: 2.2, 4.1_

- [x] 10. Write comprehensive integration tests
    - [x] 10.1 Test end-to-end auction flow
        - Client connects, joins lot, places bid
        - Verify lot state updated correctly
        - Verify update broadcast to subscribers
        - Test multiple clients competing for same lot
        - _Requirements: 3.3, 3.4, 4.3, 4.4, 6.1, 6.3_

    - [x] 10.2 Test concurrent bid processing
        - Multiple clients place bids on same lot simultaneously
        - Verify only valid bids succeed
        - Verify lot state consistency
        - Verify all subscribers receive updates
        - _Requirements: 5.1, 5.2, 5.3, 5.4, 6.6_

    - [x] 10.3 Test dual mode operation
        - Client sends echo message (should echo)
        - Client sends auction message (should process auction)
        - Both modes work on same connection
        - Multiple clients using different modes
        - _Requirements: 1.2, 1.3, 1.4, 1.7_

    - [x] 10.4 Test error handling
        - Invalid message format
        - Bid on closed lot
        - Bid amount too low
        - Lot not found
        - Verify appropriate error messages returned
        - _Requirements: 3.6, 4.1, 4.2, 4.7, 9.6, 10.3_

- [x] 11. Verify cross-language consistency
    - [x] 11.1 Create consistency test scenarios
        - Define test cases that should produce identical results across languages
        - Test bid validation rules
        - Test message formats
        - Test error messages
        - Test state transitions
        - _Requirements: 8.1, 8.2, 8.3, 8.4, 8.5, 8.6, 9.7_

    - [x] 11.2 Run consistency tests across all languages
        - Execute same test scenarios in .NET, Go, and Rust
        - Verify message formats match exactly
        - Verify error messages are identical
        - Verify state transitions are identical
        - Document any discrepancies
        - _Requirements: 8.1, 8.2, 8.3, 8.4, 8.5, 8.6_

- [ ] 12. Implement bid logging and performance metrics
    - [x] 12.1 Create bid metrics data models
        - Write BidMetrics class with TotalBidsPlaced, BidsAccepted, BidsFailed fields
        - Write BidFailureReason enum (BidTooLow, LotClosed, Error)
        - Write BidDetail class with LotId, BidderId, Amount, Timestamp, Outcome, FailureReason fields
        - Add JSON serialization attributes
        - Write unit tests for data models
        - _Requirements: 12.1, 12.8, 12.11_

    - [x] 12.2 Implement BidMetricsCollector service
        - Create IBidMetricsCollector interface with RecordBidPlaced, RecordBidAccepted, RecordBidFailed, GetMetrics methods
        - Implement BidMetricsCollector with thread-safe counters and failure reason breakdown dictionary
        - Implement bid tracking with pending bids queue for correlation
        - Write unit tests for metrics collection and thread-safety
        - _Requirements: 12.1, 12.2, 12.3, 12.4, 12.5, 12.6, 12.7, 12.8_

    - [x] 12.3 Update MessageSender to record bid placements
        - Modify CreateBidMessage and bid sending methods to call BidMetricsCollector.RecordBidPlaced
        - Store bid details for correlation with responses
        - Track bid timestamp and details for metrics
        - _Requirements: 12.1, 12.2_

    - [x] 12.4 Update message receiver to correlate responses and record outcomes
        - Parse LotUpdate messages and identify accepted bids
        - Parse Error messages and identify rejected bids
        - Extract failure reason from error messages (BidTooLow, LotClosed, Error categorization)
        - Correlate responses with pending bids using lotId, bidderId, and timing
        - Call BidMetricsCollector.RecordBidAccepted or RecordBidFailed appropriately
        - Write unit tests for message parsing and correlation
        - _Requirements: 12.3, 12.4, 12.5, 12.6, 12.7, 12.11_

    - [ ] 12.5 Integrate BidMetricsCollector into benchmark client scenarios
        - Inject BidMetricsCollector into BaseScenario or appropriate service
        - Initialize BidMetricsCollector when running in auction mode
        - Ensure metrics are collected across all clients and aggregated
        - Write integration tests for bid metrics collection in auction scenarios
        - _Requirements: 12.1, 12.2, 12.12_

    - [ ] 12.6 Update BenchmarkMetrics model to include bid metrics
        - Add BidMetrics property to BenchmarkMetrics class
        - Ensure bid metrics are optional (null for echo mode)
        - Update IMetricsCollector interface to include bid metrics
        - _Requirements: 12.9, 12.12_

    - [ ] 12.7 Update JsonReportGenerator to include bid metrics in reports
        - Add bidMetrics section to JSON report when mode is Auction
        - Include totalBidsPlaced, bidsAccepted, bidsFailed fields
        - Include acceptanceRate and failureRate calculated fields
        - Include failureReasonBreakdown with counts per reason type
        - Omit bidMetrics section when mode is Echo
        - Update report generation tests to verify bid metrics inclusion
        - _Requirements: 12.9, 12.10, 12.12_

    - [ ] 12.8 Write comprehensive tests for bid metrics collection
        - Test bid metrics collector records bids placed correctly
        - Test bid metrics collector correctly identifies accepted bids
        - Test bid metrics collector correctly identifies rejected bids
        - Test failure reason categorization (BidTooLow, LotClosed, Error)
        - Test bid correlation between sent bids and received responses
        - Test metrics aggregation across multiple clients
        - Test bid metrics included in JSON report only in auction mode
        - Test bid metrics excluded from JSON report in echo mode
        - Verify failure reason breakdown sums to total failures
        - Verify total bids placed equals accepted + failed
        - _Requirements: 12.1, 12.2, 12.3, 12.4, 12.5, 12.6, 12.7, 12.8, 12.9, 12.10, 12.11, 12.12_

