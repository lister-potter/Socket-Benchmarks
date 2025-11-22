# Requirements Document

## Introduction

The benchmark client currently requires users to manually specify the server process ID (PID) via the `--server-pid` command-line option for resource monitoring. This is inconvenient and error-prone, as users must manually identify the PID of the server process they want to monitor.

This feature will automatically detect the server PID by querying the operating system to find which process is listening on the TCP/IP port specified in the server URL. This eliminates the need for manual PID specification while maintaining the ability to override with an explicit PID if needed.

## Requirements

### Requirement 1: Automatic PID Detection

**User Story:** As a benchmark user, I want the benchmark client to automatically detect the server process ID from the server URL, so that I don't have to manually look up and specify the PID.

#### Acceptance Criteria

1. WHEN the benchmark client starts AND no `--server-pid` is provided THEN the system SHALL extract the port number from the `--server-url` parameter
2. WHEN a port number is extracted from the server URL THEN the system SHALL query the operating system to find the process ID listening on that port
3. WHEN a process is found listening on the specified port THEN the system SHALL set `config.ServerProcessId` to that process ID
4. WHEN no process is found listening on the specified port THEN the system SHALL log a warning message AND continue execution without resource monitoring
5. WHEN multiple processes are found listening on the same port THEN the system SHALL select the first process found AND log a warning about multiple matches
6. WHEN the `--server-pid` option is explicitly provided THEN the system SHALL use the provided PID AND skip automatic detection

### Requirement 2: Cross-Platform Support

**User Story:** As a benchmark user on different operating systems, I want PID detection to work on Windows, Linux, and macOS, so that the feature is usable regardless of my platform.

#### Acceptance Criteria

1. WHEN running on Windows THEN the system SHALL use `netstat -ano` or equivalent Windows APIs to find the process listening on the port
2. WHEN running on Linux THEN the system SHALL use `lsof` or `/proc/net/tcp` to find the process listening on the port
3. WHEN running on macOS THEN the system SHALL use `lsof` or equivalent macOS APIs to find the process listening on the port
4. WHEN the required system command or API is not available THEN the system SHALL log a warning message AND continue execution without resource monitoring
5. WHEN the user does not have sufficient permissions to query process information THEN the system SHALL log an appropriate error message AND continue execution without resource monitoring

### Requirement 3: URL Parsing and Port Extraction

**User Story:** As a benchmark user, I want the client to correctly extract the port from various URL formats, so that PID detection works with different server URL configurations.

#### Acceptance Criteria

1. WHEN the server URL is `ws://localhost:8080` THEN the system SHALL extract port `8080`
2. WHEN the server URL is `ws://127.0.0.1:8080` THEN the system SHALL extract port `8080`
3. WHEN the server URL is `wss://example.com:443` THEN the system SHALL extract port `443`
4. WHEN the server URL is `ws://localhost` (no port) THEN the system SHALL use default port `80` for WebSocket (ws://) or `443` for Secure WebSocket (wss://)
5. WHEN the server URL format is invalid THEN the system SHALL log an error message AND skip automatic PID detection

### Requirement 4: Logging and Diagnostics

**User Story:** As a benchmark user, I want clear feedback about PID detection, so that I understand whether resource monitoring is active and can troubleshoot issues.

#### Acceptance Criteria

1. WHEN automatic PID detection succeeds THEN the system SHALL log an informational message indicating the detected PID
2. WHEN automatic PID detection fails THEN the system SHALL log a warning message explaining why detection failed
3. WHEN resource monitoring starts with a detected PID THEN the system SHALL log that monitoring is active for the specified PID
4. WHEN resource monitoring cannot start (invalid PID) THEN the system SHALL log an error message AND continue execution without monitoring

