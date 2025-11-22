# Implementation Plan

- [x] 1. Extract shared port parsing utility
    - Create `UrlPortExtractor` service to parse WebSocket URLs, apply ws/wss defaults, and emit structured errors for invalid input.
    - Refactor existing `JsonReportGenerator` and any other callers to use the shared helper while preserving current behaviour.
    - Add focused unit tests covering the URL permutations from Requirement 3 (explicit port, implicit port, invalid formats).
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5_

- [x] 2. Introduce PID detection infrastructure
    - [x] 2.1 Define `IPidDetector` interface and register dependency injection hook (or factory wiring) so scenarios can request PID detection services.
        - Keep the interface minimal (`DetectPidByPort(int port)`), returning nullable PID plus logging hooks for diagnostics.
        - _Requirements: 1.2, 1.3, 1.5, 2.1–2.5_
    - [x] 2.2 Implement `PidDetector` service with platform-aware strategies (`netstat` on Windows, `lsof` on Linux/macOS) plus graceful fallbacks when commands are unavailable.
        - Use `RuntimeInformation` to branch per OS, shell out using `System.Diagnostics.Process`, and parse the command output defensively.
        - Ensure warnings/errors match Requirement 4 when commands fail, multiple PIDs appear, or permissions are insufficient.
        - _Requirements: 1.2, 1.4, 1.5, 2.1, 2.2, 2.3, 2.4, 2.5, 4.1, 4.2_
    - [x] 2.3 Unit-test `PidDetector` by stubbing command execution (e.g., via injectable process runner) to simulate each OS output, multi-PID responses, no matches, and command failures.
        - _Requirements: 1.2–1.5, 2.1–2.5, 4.2_

- [x] 3. Wire detection into client startup
    - [x] 3.1 Enhance `Program.ParseConfig` (or immediately after parsing) to derive the target port via `UrlPortExtractor`, call `PidDetector` when `--server-pid` is absent, and set `config.ServerProcessId` when a PID is found.
        - Respect explicit `--server-pid` overrides (Requirement 1.6) and ensure detection runs exactly once per invocation.
        - _Requirements: 1.1, 1.2, 1.3, 1.6, 3.1–3.5_
    - [x] 3.2 Update logging/resource-monitor wiring so successful detection logs info, failures log warnings, and `ResourceMonitor` starts only when a PID is available.
        - Add structured log messages mandated by Requirement 4 (success, failure, monitoring start/skip).
        - _Requirements: 1.3, 1.4, 4.1, 4.2, 4.3, 4.4_

- [x] 4. Add regression tests for end-to-end behaviour
    - Create integration tests that launch a lightweight in-process WebSocket server, run the benchmark client without `--server-pid`, and assert that resource monitoring is activated via the auto-detected PID.
    - Add a test scenario where no process listens on the requested port to confirm warnings are emitted and execution continues without monitoring.
    - Add a scenario that supplies `--server-pid` to prove automatic detection is skipped.
    - _Requirements: 1.1–1.6, 2.1–2.5, 4.1–4.4_

- [x] 5. Update CLI help and README usage examples to describe automatic PID detection and the circumstances where `--server-pid` is still needed.
    - Reflect new logging behaviour and any prerequisites (e.g., `lsof` availability or required permissions) so users can troubleshoot.
    - _Requirements: 1.4, 2.4, 2.5, 4.2_

