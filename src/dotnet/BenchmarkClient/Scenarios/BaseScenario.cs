using System.Diagnostics;
using System.Net.WebSockets;
using BenchmarkClient.Interfaces;
using BenchmarkClient.Models;
using BenchmarkClient.Services;

namespace BenchmarkClient.Scenarios;

public abstract class BaseScenario : IScenario
{
    public abstract string Name { get; }

    public async Task<BenchmarkMetrics> ExecuteAsync(BenchmarkConfig config, CancellationToken cancellationToken)
    {
        var metricsCollector = new MetricsCollector();
        var clientFactory = new ClientFactory();
        
        // Create bid metrics collector for auction mode
        IBidMetricsCollector? bidMetricsCollector = null;
        if (config.Mode == BenchmarkMode.Auction)
        {
            bidMetricsCollector = new BidMetricsCollector();
        }
        
        var messageSender = new MessageSender(config, bidMetricsCollector);
        var latencyTracker = new LatencyTracker();
        IResourceMonitor? resourceMonitor = null;
        var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // Start resource monitoring if process ID is provided
        if (config.ServerProcessId.HasValue)
        {
            resourceMonitor = new ResourceMonitor();
            resourceMonitor.StartMonitoring(config.ServerProcessId.Value);
            Console.WriteLine($"Resource monitoring active for server PID: {config.ServerProcessId.Value}");
        }
        else
        {
            Console.WriteLine("Resource monitoring disabled (no server PID available)");
        }

        try
        {
            // Create connections
            var connections = await CreateConnectionsAsync(clientFactory, config, cancellationToken);
            
            // Filter to only connected connections for receiving
            var connectedConnections = connections.Where(c => c.IsConnected).ToList();
            Console.WriteLine($"Created {connections.Count} connections, {connectedConnections.Count} are connected");
            
            // Diagnostic: Log WebSocket states for debugging
            if (connectedConnections.Count < connections.Count && connections.Count > 0)
            {
                var stateCounts = connections
                    .GroupBy(c => c.WebSocket?.State.ToString() ?? "null")
                    .Select(g => $"{g.Key}: {g.Count()}")
                    .ToList();
                Console.WriteLine($"WebSocket state breakdown: {string.Join(", ", stateCounts)}");
            }
            
            // Update config.ClientCount to reflect actual connected clients for accurate reporting
            config.ClientCount = connectedConnections.Count;

            // Start receiving messages for all connected connections
            // Use Task.Run to ensure they run on thread pool threads concurrently
            var receiveTasks = connectedConnections.Select(c => 
                Task.Run(() => ReceiveMessagesAsync(c, metricsCollector, latencyTracker, bidMetricsCollector, cancellationTokenSource.Token), cancellationTokenSource.Token)).ToArray();
            
            Console.WriteLine($"Started {receiveTasks.Length} receive tasks for connected clients");

            // Start sending messages (MessageSender will filter to connected connections)
            var sendTask = messageSender.StartSendingAsync(
                connectedConnections,
                async (conn, msg) =>
                {
                    var sentTimestamp = Stopwatch.GetTimestamp();
                    metricsCollector.RecordMessageSent(
                        msg.SentTimestamp,
                        msg.ClientId,
                        msg.MessageId,
                        msg.Payload.Length);
                    latencyTracker.RecordSent(msg.MessageId, sentTimestamp);
                    
                    // Track synthetic messages for auction mode latency correlation
                    if (config.Mode == BenchmarkMode.Auction)
                    {
                        TrackPendingAuctionMessage(msg.ClientId, msg.MessageId, sentTimestamp);
                    }
                },
                cancellationTokenSource.Token);

            // Wait for sending to complete, then wait a bit for remaining receives
            await sendTask;
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationTokenSource.Token);

            // Cancel receive tasks
            cancellationTokenSource.Cancel();

            // Wait for receive tasks to finish
            try
            {
                await Task.WhenAll(receiveTasks);
            }
            catch (OperationCanceledException) { }

            // Close all connections (including failed ones)
            await clientFactory.CloseAllConnectionsAsync(connections);

            // Set test end time explicitly (fixes invalid end time when no messages received)
            metricsCollector.SetTestEndTime(DateTime.UtcNow);

            // Get final metrics
            var metrics = metricsCollector.GetMetrics();
            
            // Add bid metrics if available (auction mode)
            if (bidMetricsCollector != null)
            {
                metrics.BidMetrics = bidMetricsCollector.GetMetrics();
            }
            
            // Log final connection count for debugging
            Console.WriteLine($"\nScenario completed. Final client count: {config.ClientCount}");
            Console.WriteLine($"Total connections created: {connections.Count}, Connected: {connectedConnections.Count}");
            
            // Add resource usage if available
            if (resourceMonitor != null)
            {
                resourceMonitor.StopMonitoring();
                metrics.ResourceUsage = resourceMonitor.GetSnapshots();
            }

            return metrics;
        }
        finally
        {
            resourceMonitor?.StopMonitoring();
        }
    }

    protected abstract Task<List<ClientConnection>> CreateConnectionsAsync(
        ClientFactory factory,
        BenchmarkConfig config,
        CancellationToken cancellationToken);

    // Track pending synthetic message IDs per connection for auction mode latency tracking
    // Using Stopwatch.GetTimestamp() for monotonic time
    private readonly Dictionary<int, Queue<int>> _pendingMessageIds = new();
    private readonly Dictionary<int, Queue<long>> _pendingSentTimestamps = new();
    private readonly object _pendingLock = new();

    private async Task ReceiveMessagesAsync(
        ClientConnection connection,
        IMetricsCollector metricsCollector,
        LatencyTracker latencyTracker,
        IBidMetricsCollector? bidMetricsCollector,
        CancellationToken cancellationToken)
    {

        int receiveAttempts = 0;
        int messagesReceived = 0;
        
        // Diagnostic: Log when receive task starts
        Console.WriteLine($"Receive task started for client {connection.ClientId}, WebSocket state: {connection.WebSocket?.State}, IsConnected: {connection.IsConnected}");
        
        // Ensure we start receiving even if connection state check fails
        while (!cancellationToken.IsCancellationRequested)
        {
            string? receivedJson = null;
            
            // Check if connection is still valid - but don't exit immediately
            // Give the connection a chance to receive messages even if state check fails
            if (!connection.IsConnected)
            {
                // If WebSocket is null or closed, check one more time after a delay
                await Task.Delay(50, cancellationToken);
                if (!connection.IsConnected || connection.WebSocket == null || connection.WebSocket.State != WebSocketState.Open)
                {
                    break;
                }
            }
            
            // Receive raw JSON from WebSocket
            try
            {
                // Check WebSocket state before attempting to receive
                if (connection.WebSocket == null)
                {
                    await Task.Delay(100, cancellationToken);
                    continue;
                }
                
                // Verify WebSocket is still open before receiving
                if (connection.WebSocket.State != WebSocketState.Open)
                {
                    Console.WriteLine($"Client {connection.ClientId}: WebSocket state is {connection.WebSocket.State}, exiting receive loop");
                    connection.IsConnected = false;
                    break;
                }

                receiveAttempts++;
                
                // Diagnostic: Log first few receive attempts
                if (receiveAttempts <= 3)
                {
                    Console.WriteLine($"Client {connection.ClientId}: Attempting to receive (attempt {receiveAttempts})");
                }
                
                var buffer = new byte[1024 * 64];
                var result = await connection.WebSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    cancellationToken);
                
                messagesReceived++;
                
                // Diagnostic: Log successful receive
                if (messagesReceived <= 3)
                {
                    Console.WriteLine($"Client {connection.ClientId}: Received data, MessageType: {result.MessageType}, Count: {result.Count}, EndOfMessage: {result.EndOfMessage}");
                }

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    connection.IsConnected = false;
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Text && result.Count > 0)
                {
                    // Handle complete message (EndOfMessage = true) or partial message
                    var messageBytes = new List<byte>();
                    messageBytes.AddRange(buffer.Take(result.Count));
                    
                    // If message is complete, process it
                    if (result.EndOfMessage)
                    {
                        receivedJson = System.Text.Encoding.UTF8.GetString(messageBytes.ToArray());
                        connection.MessagesReceived++;
                        
                        // Debug: Log first few messages to verify they're being received
                        if (connection.MessagesReceived <= 3)
                        {
                            Console.WriteLine($"Client {connection.ClientId}: Received message {connection.MessagesReceived}: {receivedJson.Substring(0, Math.Min(100, receivedJson.Length))}...");
                        }
                    }
                    else
                    {
                        // Partial message - continue reading until EndOfMessage
                        while (!result.EndOfMessage && connection.WebSocket?.State == WebSocketState.Open)
                        {
                            result = await connection.WebSocket.ReceiveAsync(
                                new ArraySegment<byte>(buffer),
                                cancellationToken);
                            
                            if (result.Count > 0)
                            {
                                messageBytes.AddRange(buffer.Take(result.Count));
                            }
                            
                            if (result.MessageType == WebSocketMessageType.Close)
                            {
                                connection.IsConnected = false;
                                break;
                            }
                        }
                        
                        // Now we have the complete message
                        if (result.EndOfMessage && messageBytes.Count > 0)
                        {
                            receivedJson = System.Text.Encoding.UTF8.GetString(messageBytes.ToArray());
                            connection.MessagesReceived++;
                        }
                        else
                        {
                            continue;
                        }
                    }
                }
                else if (result.MessageType == WebSocketMessageType.Binary)
                {
                    // Binary message - skip it but continue loop
                    continue;
                }
                else
                {
                    // Empty or unknown message type - skip it
                    continue;
                }
            }
            catch (OperationCanceledException)
            {
                // Cancellation requested - exit gracefully
                Console.WriteLine($"Client {connection.ClientId}: Receive loop cancelled (OperationCanceledException)");
                break;
            }
            catch (WebSocketException ex)
            {
                // WebSocket-specific error - connection is likely broken
                Console.WriteLine($"Client {connection.ClientId}: WebSocketException in receive loop: {ex.Message}");
                connection.IsConnected = false;
                break;
            }
            catch (Exception ex)
            {
                // Other exceptions - log but try to continue
                // Don't mark as disconnected unless it's a fatal error
                // Many exceptions can be transient and the connection might recover
                var wsState = connection.WebSocket?.State ?? WebSocketState.None;
                Console.WriteLine($"Client {connection.ClientId}: Exception in receive loop: {ex.GetType().Name}: {ex.Message}, WebSocket state: {wsState}");
                
                if (wsState == WebSocketState.Closed || wsState == WebSocketState.Aborted)
                {
                    connection.IsConnected = false;
                    break;
                }
                
                // For other errors, wait a bit and try again
                await Task.Delay(100, cancellationToken);
                continue;
            }

            if (string.IsNullOrEmpty(receivedJson))
            {
                // No message received - continue to next iteration
                await Task.Delay(100, cancellationToken);
                continue;
            }

            // Debug: Log that we're about to process a message - FORCE LOGGING FOR ALL MESSAGES
            Console.WriteLine($"Client {connection.ClientId}: PROCESSING message {connection.MessagesReceived}, JSON: {receivedJson.Substring(0, Math.Min(200, receivedJson.Length))}");

            var receivedTime = DateTime.UtcNow;
            var receivedTimestamp = Stopwatch.GetTimestamp();
            
            // CRITICAL FIX: Check for "type" field first to detect auction messages
            // JSON deserialization is lenient and BenchmarkMessage.FromJson will return
            // a default object even for auction messages, so we must check for "type" first
            bool isAuctionMessage = false;
            string? messageType = null;
            try
            {
                using var checkDoc = System.Text.Json.JsonDocument.Parse(receivedJson);
                if (checkDoc.RootElement.TryGetProperty("type", out var typeCheck))
                {
                    messageType = typeCheck.GetString();
                    isAuctionMessage = true;
                    Console.WriteLine($"Client {connection.ClientId}: Detected auction message type: {messageType}");
                }
            }
            catch
            {
                // If parsing fails, continue to check as BenchmarkMessage
            }
            
            // Only try BenchmarkMessage parsing if it's NOT an auction message
            if (!isAuctionMessage)
            {
                var message = BenchmarkMessage.FromJson(receivedJson);
                if (message != null && (message.MessageId > 0 || message.ClientId > 0 || message.Payload.Length > 0))
                {
                    // Echo mode message with meaningful values
                    Console.WriteLine($"Client {connection.ClientId}: Parsed as BenchmarkMessage (echo mode), MessageId={message.MessageId}, ClientId={message.ClientId}");
                    latencyTracker.RecordReceived(message.MessageId, receivedTimestamp);
                    
                    var measurements = latencyTracker.GetMeasurements();
                    var latestMeasurement = measurements.LastOrDefault(m => m.MessageId == message.MessageId);
                    var latencyMs = latestMeasurement?.LatencyMilliseconds ?? 0.0;
                    
                    if (latencyMs > 0)
                    {
                        Console.WriteLine($"Client {connection.ClientId}: Recording echo message with latency {latencyMs:F2}ms");
                        metricsCollector.RecordMessageReceived(
                            receivedTime,
                            message.ClientId,
                            message.MessageId,
                            latencyMs);
                    }
                    continue;
                }
            }
            
            // At this point, it's an auction message or an invalid message
            if (!isAuctionMessage)
            {
                Console.WriteLine($"Client {connection.ClientId}: Message does not appear to be a BenchmarkMessage or auction message, skipping");
                continue;
            }
            
            Console.WriteLine($"Client {connection.ClientId}: Processing auction message type: {messageType}");

            // Check if it's an echoed outgoing message
            bool isEchoedMessage = false;
            if (messageType == "PlaceBid" || messageType == "JoinLot")
            {
                isEchoedMessage = true;
                Console.WriteLine($"Client {connection.ClientId}: Detected echoed {messageType} message (will record for throughput but skip bid processing)");
            }
            else if (messageType == "LotUpdate" || messageType == "Error")
            {
                Console.WriteLine($"Client {connection.ClientId}: Processing server response: {messageType}, JSON: {receivedJson.Substring(0, Math.Min(150, receivedJson.Length))}");
            }

            // CRITICAL: Record ALL received messages for throughput metrics BEFORE filtering
            // This ensures totalMessagesReceived is accurate even for echoed messages
            int? messageId = null;
            long? sentTimestamp = null;
            
            // For echoed messages, don't dequeue from pending (they're not responses)
            // Only dequeue for actual server responses (LotUpdate/Error)
            if (!isEchoedMessage)
            {
                lock (_pendingLock)
                {
                    if (_pendingMessageIds.TryGetValue(connection.ClientId, out var messageIdQueue) && 
                        messageIdQueue.Count > 0 &&
                        _pendingSentTimestamps.TryGetValue(connection.ClientId, out var timestampQueue) &&
                        timestampQueue.Count > 0)
                    {
                        messageId = messageIdQueue.Dequeue();
                        sentTimestamp = timestampQueue.Dequeue();
                    }
                }
            }
            
            // ALWAYS record received messages for throughput metrics (even echoed ones)
            Console.WriteLine($"Client {connection.ClientId}: Recording received message for throughput, messageId={messageId}, sentTimestamp={sentTimestamp}, isEchoed={isEchoedMessage}");
            if (messageId.HasValue && sentTimestamp.HasValue && !isEchoedMessage)
            {
                // Calculate latency using monotonic time for actual server responses
                var elapsedTicks = receivedTimestamp - sentTimestamp.Value;
                var latencyMs = (elapsedTicks * 1000.0) / Stopwatch.Frequency;
                
                // Always record if we have valid latency (even if small)
                if (latencyMs >= 0)
                {
                    Console.WriteLine($"Client {connection.ClientId}: Recording server response with latency {latencyMs:F2}ms");
                    metricsCollector.RecordMessageReceived(
                        receivedTime,
                        connection.ClientId,
                        messageId.Value,
                        latencyMs);
                    latencyTracker.RecordReceived(messageId.Value, receivedTimestamp);
                }
                else
                {
                    // Invalid latency but still record the message
                    metricsCollector.RecordMessageReceived(
                        receivedTime,
                        connection.ClientId,
                        messageId.Value,
                        0.0);
                }
            }
            else
            {
                // Echoed message or no matching message ID - still record for throughput metrics
                // Use a placeholder message ID for unmatched/echoed messages
                Console.WriteLine($"Client {connection.ClientId}: Recording echoed/unmatched message for throughput (messageId=-1, latency=0)");
                metricsCollector.RecordMessageReceived(
                    receivedTime,
                    connection.ClientId,
                    -1, // Placeholder ID for echoed/unmatched messages
                    0.0); // No latency available for echoed/unmatched messages
            }
            
            // Skip bid metrics processing for echoed messages (they're not server responses)
            if (isEchoedMessage)
            {
                Console.WriteLine($"Client {connection.ClientId}: Skipping bid metrics processing for echoed {messageType} message");
                continue;
            }
            
            // Parse and process auction messages (LotUpdate or Error) for bid metrics
            if (bidMetricsCollector != null && !string.IsNullOrEmpty(receivedJson))
            {
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(receivedJson);
                    if (doc.RootElement.TryGetProperty("type", out var typeElement))
                    {
                        var type = typeElement.GetString();
                            
                        if (type == "LotUpdate")
                        {
                            var lotUpdate = LotUpdateMessage.FromJson(receivedJson);
                            if (lotUpdate != null)
                            {
                                // Check if this LotUpdate is for OUR bid (currentBidder matches our bidderId)
                                var ourBidderId = $"bidder-{connection.ClientId}";
                                if (!string.IsNullOrEmpty(lotUpdate.CurrentBidder) && lotUpdate.CurrentBidder == ourBidderId)
                                {
                                    // This is our bid being accepted
                                    // Record bid accepted - use currentBid as amount
                                    // The BidMetricsCollector will correlate with the pending bid
                                    bidMetricsCollector.RecordBidAccepted(
                                        lotUpdate.LotId,
                                        ourBidderId,
                                        lotUpdate.CurrentBid,
                                        receivedTime);
                                }
                                // If CurrentBidder doesn't match or is null, it's another client's bid or initial state - ignore for metrics
                            }
                        }
                        else if (type == "Error")
                        {
                            var errorMessage = ErrorMessage.FromJson(receivedJson);
                            if (errorMessage != null && !string.IsNullOrEmpty(errorMessage.Message))
                            {
                                // Categorize failure reason
                                var failureReason = CategorizeFailureReason(errorMessage.Message);
                                
                                // Extract lotId if possible from error message or use recent lot
                                var lotId = ExtractLotIdFromError(errorMessage.Message) ?? $"lot-{(connection.ClientId % 10) + 1}";
                                var bidderId = $"bidder-{connection.ClientId}";
                                
                                // Use a placeholder amount - the BidMetricsCollector will match by lotId and bidderId
                                bidMetricsCollector.RecordBidFailed(
                                    lotId,
                                    bidderId,
                                    0m, // Amount will be matched from pending bid
                                    failureReason,
                                    receivedTime);
                            }
                        }
                    }
                }
                catch
                {
                    // Log parsing errors for debugging but don't crash
                    // Console.WriteLine($"Error parsing auction message: {ex.Message}");
                }
            }
            // Continue loop to process next message
        }
    }
    
    // Called when a synthetic message is sent - track it for latency correlation
    // sentTimestamp is Stopwatch.GetTimestamp() (monotonic time)
    private void TrackPendingAuctionMessage(int clientId, int messageId, long sentTimestamp)
    {
        lock (_pendingLock)
        {
            if (!_pendingMessageIds.ContainsKey(clientId))
            {
                _pendingMessageIds[clientId] = new Queue<int>();
                _pendingSentTimestamps[clientId] = new Queue<long>();
            }
            
            _pendingMessageIds[clientId].Enqueue(messageId);
            _pendingSentTimestamps[clientId].Enqueue(sentTimestamp);
        }
    }

    private static BidFailureReason CategorizeFailureReason(string errorMessage)
    {
        if (string.IsNullOrEmpty(errorMessage))
            return BidFailureReason.Error;

        var message = errorMessage.ToLowerInvariant();

        // Check for "BidTooLow" - bid amount must be greater than current bid
        if (message.Contains("greater than") || message.Contains("greater than current bid") || message.Contains("bid amount"))
        {
            return BidFailureReason.BidTooLow;
        }

        // Check for "LotClosed" - lot is closed
        if (message.Contains("closed") || message.Contains("lot is closed"))
        {
            return BidFailureReason.LotClosed;
        }

        // Default to Error for other cases
        return BidFailureReason.Error;
    }

    private static string? ExtractLotIdFromError(string errorMessage)
    {
        // Try to extract lot ID from error message if present
        // This is a best-effort extraction - may return null if not found
        var match = System.Text.RegularExpressions.Regex.Match(errorMessage, @"lot-\d+");
        return match.Success ? match.Value : null;
    }
}

