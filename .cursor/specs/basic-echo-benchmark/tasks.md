# Implementation Plan - Basic Echo Benchmark

This document contains the implementation tasks for the Basic Echo Benchmark feature. Each task is designed to be executed by a coding agent following test-driven development principles.

## Implementation Tasks

- [x] 1. Set up project structure and core interfaces
    - Create directory structure for .NET echo server, Go echo server, Rust echo server, and benchmark client projects
    - Define core data models and interfaces for benchmark client (BenchmarkConfig, BenchmarkMetrics, LatencyPercentiles, ResourceSnapshot)
    - Create project files (.csproj for .NET, go.mod for Go, Cargo.toml for Rust)
    - _Requirements: 5.1, 5.2, 6.1, 6.2, 6.3, 6.4, 8.1, 8.2_

- [x] 2. Implement .NET 10 echo server
    - [x] 2.1 Create .NET echo server project with Kestrel and WebSocket support
        - Create ASP.NET Core minimal API project
        - Configure Kestrel for WebSocket support
        - Enable Native AOT compilation in project file
        - Write unit tests for server startup and configuration
        - _Requirements: 2.1, 2.2, 2.5, 1.9, 1.10_

    - [x] 2.2 Implement WebSocket echo handler
        - Create WebSocket middleware/handler that accepts connections
        - Implement message echo logic for text and binary messages
        - Write unit tests for message echoing (text, binary, empty, large messages)
        - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5_

    - [x] 2.3 Implement connection lifecycle management
        - Handle connection establishment and cleanup
        - Implement graceful connection close on client disconnect
        - Write unit tests for connection lifecycle
        - _Requirements: 1.6, 1.7, 1.8_

    - [x] 2.4 Add command-line configuration for port
        - Implement command-line argument parsing for port configuration
        - Write unit tests for configuration
        - _Requirements: 2.5, 1.9_

- [x] 3. Implement Go echo server
    - [x] 3.1 Create Go echo server project with Gorilla WebSocket
        - Create Go module with gorilla/websocket dependency
        - Set up HTTP server with WebSocket upgrade support
        - Write unit tests for server setup
        - _Requirements: 3.1, 3.5, 1.9, 1.10_

    - [x] 3.2 Implement WebSocket echo handler
        - Create WebSocket connection handler using Gorilla upgrader
        - Implement message echo logic for text and binary messages
        - Use goroutines for concurrent connection handling
        - Write unit tests for message echoing
        - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 3.2_

    - [x] 3.3 Implement connection lifecycle management
        - Handle connection establishment and cleanup
        - Implement graceful connection close
        - Write unit tests for connection lifecycle
        - _Requirements: 1.6, 1.7, 1.8_

    - [x] 3.4 Add command-line configuration for port
        - Implement flag parsing for port configuration
        - Write unit tests for configuration
        - _Requirements: 3.3, 1.9_

- [x] 4. Implement Rust echo server
    - [x] 4.1 Create Rust echo server project with tokio-tungstenite
        - Create Cargo project with tokio and tokio-tungstenite dependencies
        - Set up tokio runtime and WebSocket server
        - Write unit tests for server setup
        - _Requirements: 4.1, 4.5, 1.9, 1.10_

    - [x] 4.2 Implement WebSocket echo handler
        - Create async WebSocket connection handler
        - Implement message echo logic for text and binary messages
        - Use tokio::spawn for concurrent connection handling
        - Write unit tests for message echoing
        - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 4.2_

    - [x] 4.3 Implement connection lifecycle management
        - Handle connection establishment and cleanup
        - Implement graceful connection close
        - Write unit tests for connection lifecycle
        - _Requirements: 1.6, 1.7, 1.8_

    - [x] 4.4 Add command-line configuration for port
        - Implement clap or similar for port configuration
        - Write unit tests for configuration
        - _Requirements: 4.3, 1.9_

- [x] 5. Implement benchmark client core components
    - [x] 5.1 Create benchmark client project structure
        - Create .NET console application project
        - Set up project dependencies (System.Net.WebSockets, System.Text.Json, System.Diagnostics)
        - Create directory structure for components
        - _Requirements: 5.1, 5.2_

    - [x] 5.2 Implement BenchmarkConfig class
        - Create BenchmarkConfig class with all configuration properties
        - Implement configuration validation
        - Write unit tests for configuration and validation
        - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.11_

    - [x] 5.3 Implement message format and serialization
        - Create BenchmarkMessage class with MessageId, ClientId, timestamp, payload
        - Implement JSON serialization for text messages
        - Implement binary message format
        - Write unit tests for message serialization/deserialization
        - _Requirements: 5.5, 5.7, 5.11_

- [x] 6. Implement WebSocket client connection management
    - [x] 6.1 Create ClientConnection class
        - Implement ClientConnection with WebSocket, state tracking, and metrics
        - Implement connection establishment logic
        - Write unit tests for connection creation and state management
        - _Requirements: 5.1, 5.2_

    - [x] 6.2 Implement ClientFactory for creating and managing connections
        - Create factory that spawns multiple client connections
        - Implement connection pooling and lifecycle management
        - Write unit tests for factory and connection management
        - _Requirements: 5.2, 5.8_

    - [x] 6.3 Implement connection error handling
        - Handle connection failures and retries
        - Record connection errors in metrics
        - Write unit tests for error handling
        - _Requirements: 5.8, 6.7_

- [x] 7. Implement message sending and receiving
    - [x] 7.1 Create MessageSender component
        - Implement message sending with configurable rate per client
        - Implement message pattern support (FixedRate, Burst, RampUp)
        - Write unit tests for message sending and rate limiting
        - _Requirements: 5.3, 5.4, 5.5, 5.11_

    - [x] 7.2 Implement message receiving and echo verification
        - Implement message reception and matching with sent messages
        - Verify echo response matches sent message content
        - Record message mismatches in metrics
        - Write unit tests for message receiving and verification
        - _Requirements: 5.6, 5.7, 6.8_

- [x] 8. Implement latency tracking
    - [x] 8.1 Create LatencyTracker component
        - Implement latency measurement recording (sent time, received time, latency calculation)
        - Store latency measurements efficiently (consider using arrays or lists)
        - Write unit tests for latency tracking accuracy
        - _Requirements: 5.5, 5.6, 6.2_

    - [x] 8.2 Implement latency measurement matching
        - Match received messages with sent messages using MessageId
        - Calculate round-trip latency accurately
        - Handle out-of-order message reception
        - Write unit tests for message matching and latency calculation
        - _Requirements: 5.6, 5.11_

- [x] 9. Implement statistics calculation
    - [x] 9.1 Create StatisticsCalculator component
        - Implement throughput calculation (messages per second)
        - Implement latency percentile calculation (p50, p90, p99, min, max, mean)
        - Write unit tests for throughput and percentile calculations
        - _Requirements: 6.1, 6.2, 6.11_

    - [x] 9.2 Implement metrics aggregation
        - Aggregate metrics across all clients
        - Calculate total messages sent/received
        - Calculate error rates
        - Write unit tests for metrics aggregation
        - _Requirements: 6.5, 6.6, 6.7, 6.8_

- [x] 10. Implement resource monitoring
    - [x] 10.1 Create ResourceMonitor component
        - Implement process ID resolution for server process
        - Implement CPU percentage monitoring using System.Diagnostics.Process
        - Implement memory usage monitoring (resident set size)
        - Write unit tests for resource monitoring (with mocked Process)
        - _Requirements: 6.3, 6.4, 6.10_

    - [x] 10.2 Implement resource snapshot collection
        - Collect resource snapshots at regular intervals (default: 1 second)
        - Store snapshots as time series data
        - Write unit tests for snapshot collection and timing
        - _Requirements: 6.4, 6.10_

- [x] 11. Implement metrics collection interface
    - [x] 11.1 Create IMetricsCollector interface and implementation
        - Implement RecordMessageSent, RecordMessageReceived, RecordConnectionError, RecordMessageMismatch methods
        - Implement GetMetrics method that returns aggregated BenchmarkMetrics
        - Write unit tests for metrics collection
        - _Requirements: 6.1, 6.2, 6.5, 6.6, 6.7, 6.8, 6.9_

    - [x] 11.2 Integrate metrics collection with message sending/receiving
        - Wire up metrics collection in MessageSender and message receiving logic
        - Ensure all metrics are recorded correctly
        - Write integration tests for metrics collection
        - _Requirements: 6.1, 6.2, 6.5, 6.6, 6.7, 6.8_

- [x] 12. Implement test scenarios
    - [x] 12.1 Create ScenarioRunner component
        - Implement scenario execution framework
        - Create base scenario class/interface
        - Write unit tests for scenario framework
        - _Requirements: 7.1, 7.12_

    - [x] 12.2 Implement single client scenario
        - Create SingleClientScenario that spawns one client
        - Configure default message rate and duration
        - Write unit tests for single client scenario
        - _Requirements: 7.1, 7.2, 7.3, 7.12_

    - [x] 12.3 Implement 100-client burst scenario
        - Create BurstScenario that spawns 100 clients simultaneously
        - Implement connection ramp-up within time window
        - Configure default message rate and duration
        - Write unit tests for burst scenario
        - _Requirements: 7.4, 7.5, 7.6, 7.7, 7.12_

    - [x] 12.4 Implement 1000-client steady load scenario
        - Create SteadyLoadScenario that spawns 1000 clients
        - Implement gradual connection ramp-up over time period
        - Configure default message rate and duration
        - Write unit tests for steady load scenario
        - _Requirements: 7.8, 7.9, 7.10, 7.11, 7.12_

- [x] 13. Implement JSON report generation
    - [x] 13.1 Create IReportGenerator interface and JSON implementation
        - Define JSON report schema structure
        - Implement JSON serialization using System.Text.Json
        - Write unit tests for JSON schema validity
        - _Requirements: 8.1, 8.10, 8.11_

    - [x] 13.2 Implement metadata section generation
        - Generate metadata with server language, version, scenario name, timestamps, duration
        - Write unit tests for metadata generation
        - _Requirements: 8.2, 8.8_

    - [x] 13.3 Implement client and server configuration sections
        - Generate clientConfig and serverConfig sections
        - Write unit tests for configuration sections
        - _Requirements: 8.7, 8.8_

    - [x] 13.4 Implement throughput metrics section
        - Generate throughput section with total messages, messages per second
        - Write unit tests for throughput section
        - _Requirements: 8.3_

    - [x] 13.5 Implement latency metrics section
        - Generate latency section with all percentiles (p50, p90, p99, min, max, mean)
        - Ensure units are in milliseconds
        - Write unit tests for latency section
        - _Requirements: 8.4_

    - [x] 13.6 Implement error metrics section
        - Generate errors section with connection errors, message mismatches, error rate
        - Write unit tests for error section
        - _Requirements: 8.5_

    - [x] 13.7 Implement resource usage section
        - Generate resourceUsage section with CPU and memory time series
        - Format timestamps as ISO8601
        - Write unit tests for resource usage section
        - _Requirements: 8.6_

    - [x] 13.8 Implement report file writing
        - Generate filename with scenario name, server language, and timestamp
        - Write JSON report to file system
        - Write unit tests for file writing
        - _Requirements: 8.12_

- [x] 14. Integrate all components into benchmark client application
    - [x] 14.1 Create main application entry point
        - Implement command-line interface for benchmark client
        - Parse command-line arguments for server URL, scenario selection, configuration
        - Write integration tests for CLI
        - _Requirements: 5.1, 7.1, 7.2, 7.3_

    - [x] 14.2 Wire up scenario execution flow
        - Integrate ScenarioRunner with ClientFactory, MessageSender, MetricsCollector, ResourceMonitor, ReportGenerator
        - Implement end-to-end benchmark execution
        - Write integration tests for full benchmark flow
        - _Requirements: 7.12, 7.13_

    - [x] 14.3 Add error handling and logging
        - Implement error handling throughout the application
        - Add logging for benchmark progress and errors
        - Write tests for error handling
        - _Requirements: 5.8, 5.9_

- [x] 15. Create integration tests
    - [x] 15.1 Create integration test project
        - Set up test project for integration tests
        - Create test fixtures for starting/stopping echo servers
        - _Requirements: All integration test requirements_

    - [x] 15.2 Implement client-server integration tests
        - Test single client echo (text and binary messages)
        - Test multiple concurrent clients
        - Test message rate limiting
        - Test connection lifecycle
        - _Requirements: 1.1, 1.2, 1.3, 1.4, 5.1, 5.2, 5.3_

    - [x] 15.3 Implement end-to-end scenario tests
        - Test single client scenario execution
        - Test 100-client burst scenario
        - Test 1000-client steady load scenario (may be skipped in CI due to resource requirements)
        - Verify metrics collection accuracy
        - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5, 7.6, 7.7, 7.8, 7.9, 7.10, 7.11, 7.12, 7.13_

    - [x] 15.4 Implement cross-language compatibility tests
        - Test .NET client against .NET server
        - Test .NET client against Go server
        - Test .NET client against Rust server
        - Verify consistent behavior and metrics across all servers
        - _Requirements: 1.1, 1.2, 1.3, 1.4, 5.1, 5.2, 5.3, 8.1_

- [x] 16. Create unit tests for echo servers
    - [x] 16.1 Create unit tests for .NET echo server
        - Test connection acceptance and WebSocket upgrade
        - Test text and binary message echoing
        - Test multiple concurrent connections
        - Test error handling (malformed frames, connection close)
        - Test connection cleanup
        - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7, 1.8_

    - [x] 16.2 Create unit tests for Go echo server
        - Test connection acceptance and WebSocket upgrade
        - Test text and binary message echoing
        - Test concurrent connection handling with goroutines
        - Test error handling
        - Test connection cleanup
        - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7, 1.8, 3.2_

    - [x] 16.3 Create unit tests for Rust echo server
        - Test connection acceptance and WebSocket upgrade
        - Test text and binary message echoing
        - Test concurrent connection handling with tokio
        - Test error handling
        - Test connection cleanup
        - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7, 1.8, 4.2_

- [x] 17. Documentation and build configuration
    - [x] 17.1 Create README for each server implementation
        - Document build instructions
        - Document command-line arguments
        - Document usage examples
        - _Requirements: 2.5, 3.3, 4.3_

    - [x] 17.2 Create README for benchmark client
        - Document installation and build
        - Document command-line usage
        - Document scenario configuration
        - Document JSON report format
        - _Requirements: 5.1, 7.1, 8.1_

    - [x] 17.3 Configure build scripts/CI
        - Create build scripts for each server (if needed)
        - Ensure Native AOT is enabled for .NET server
        - Ensure static linking for Go and Rust servers
        - _Requirements: 2.1, 3.5, 4.5_

