using BenchmarkClient.Interfaces;
using BenchmarkClient.Models;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text.Json;

namespace BenchmarkClient.Services;

public class MessageSender
{
    private readonly BenchmarkConfig _config;
    private readonly IBidMetricsCollector? _bidMetricsCollector;
    private int _nextMessageId = 1;
    private int _nextBidId = 1;
    private readonly Random _random = new();
    private Stopwatch? _testStopwatch;
    // Track lot subscriptions per client for auction mode (thread-safe)
    private readonly ConcurrentDictionary<int, string> _clientLotSubscriptions = new();

    public MessageSender(BenchmarkConfig config, IBidMetricsCollector? bidMetricsCollector = null)
    {
        _config = config;
        _bidMetricsCollector = bidMetricsCollector;
    }

    public async Task StartSendingAsync(
        List<ClientConnection> connections,
        Func<ClientConnection, BenchmarkMessage, Task> onMessageSent,
        CancellationToken cancellationToken)
    {
        if (connections.Count == 0)
            return;

        // Start the test stopwatch when sending begins (all clients share the same time reference)
        _testStopwatch = Stopwatch.StartNew();

        // Filter to only connected connections and log the count
        var connectedConnections = connections.Where(c => c.IsConnected).ToList();
        
        // Diagnostic: Check WebSocket states if no connections are found
        if (connectedConnections.Count == 0 && connections.Count > 0)
        {
            var states = connections
                .Select(c => c.WebSocket?.State.ToString() ?? "null")
                .GroupBy(s => s)
                .Select(g => $"{g.Key}: {g.Count()}")
                .ToList();
            Console.WriteLine($"Warning: No connected clients out of {connections.Count} total connections");
            Console.WriteLine($"WebSocket states: {string.Join(", ", states)}");
            
            // Try to find connections that might still be usable
            var potentiallyConnected = connections
                .Where(c => c.WebSocket != null && 
                           (c.WebSocket.State == WebSocketState.Open || 
                            c.WebSocket.State == WebSocketState.Connecting))
                .ToList();
            
            if (potentiallyConnected.Count > 0)
            {
                Console.WriteLine($"Found {potentiallyConnected.Count} connections with Open/Connecting state, attempting to use them");
                connectedConnections = potentiallyConnected;
            }
            else
            {
                return;
            }
        }

        if (connectedConnections.Count < connections.Count)
        {
            Console.WriteLine($"Warning: Only {connectedConnections.Count} out of {connections.Count} connections are connected");
        }

        var tasks = new List<Task>();

        // Each client runs independently in its own task
        foreach (var connection in connectedConnections)
        {
            // Capture connection for closure
            var conn = connection;
            
            var clientTask = Task.Run(async () =>
            {
                await SendMessagesForClientAsync(
                    conn,
                    onMessageSent,
                    cancellationToken);
            }, cancellationToken);
            
            tasks.Add(clientTask);
        }

        Console.WriteLine($"Starting message sending for {tasks.Count} connected clients");
        await Task.WhenAll(tasks);
    }

    private async Task SendMessagesForClientAsync(
        ClientConnection connection,
        Func<ClientConnection, BenchmarkMessage, Task> onMessageSent,
        CancellationToken cancellationToken)
    {
        if (_testStopwatch == null)
            throw new InvalidOperationException("StartSendingAsync must be called before SendMessagesForClientAsync");

        var stopwatch = _testStopwatch; // Local reference to avoid null checks
        var testDuration = _config.Duration;
        var endTime = stopwatch.Elapsed + testDuration;

        // For auction mode, first join a lot
        if (_config.Mode == BenchmarkMode.Auction)
        {
            await JoinLotForClientAsync(connection, cancellationToken);
        }

        switch (_config.Pattern)
        {
            case MessagePattern.FixedRate:
                if (_config.Mode == BenchmarkMode.Auction)
                    await SendFixedRateAuctionAsync(connection, onMessageSent, stopwatch, endTime, cancellationToken);
                else
                    await SendFixedRateAsync(connection, onMessageSent, stopwatch, endTime, cancellationToken);
                break;
            case MessagePattern.Burst:
                if (_config.Mode == BenchmarkMode.Auction)
                    await SendBurstAuctionAsync(connection, onMessageSent, stopwatch, endTime, cancellationToken);
                else
                    await SendBurstAsync(connection, onMessageSent, stopwatch, endTime, cancellationToken);
                break;
            case MessagePattern.RampUp:
                if (_config.Mode == BenchmarkMode.Auction)
                    await SendRampUpAuctionAsync(connection, onMessageSent, stopwatch, endTime, cancellationToken);
                else
                    await SendRampUpAsync(connection, onMessageSent, stopwatch, endTime, cancellationToken);
                break;
            default:
                if (_config.Mode == BenchmarkMode.Auction)
                    await SendFixedRateAuctionAsync(connection, onMessageSent, stopwatch, endTime, cancellationToken);
                else
                    await SendFixedRateAsync(connection, onMessageSent, stopwatch, endTime, cancellationToken);
                break;
        }
    }

    /// <summary>
    /// Sends messages at a precise, constant rate per client using RateLimiter.
    /// </summary>
    private async Task SendFixedRateAsync(
        ClientConnection connection,
        Func<ClientConnection, BenchmarkMessage, Task> onMessageSent,
        Stopwatch stopwatch,
        TimeSpan endTime,
        CancellationToken cancellationToken)
    {
        var rateLimiter = new RateLimiter(_config.MessagesPerSecondPerClient);

        while (stopwatch.Elapsed < endTime && !cancellationToken.IsCancellationRequested)
        {
            if (!connection.IsConnected) break;

            // Wait for the next send time based on rate
            await rateLimiter.WaitForNextAsync(cancellationToken);

            // Create and send message
            var message = CreateMessage(connection.ClientId);
            var sent = await connection.SendMessageAsync(message, cancellationToken);
            
            if (sent)
            {
                await onMessageSent(connection, message);
            }
        }
    }

    /// <summary>
    /// Sends messages in bursts: all clients send simultaneously at regular intervals.
    /// </summary>
    private async Task SendBurstAsync(
        ClientConnection connection,
        Func<ClientConnection, BenchmarkMessage, Task> onMessageSent,
        Stopwatch stopwatch,
        TimeSpan endTime,
        CancellationToken cancellationToken)
    {
        // Calculate burst interval: if we want X msg/sec, and we send Y messages per burst,
        // then bursts should occur every (Y / X) seconds
        var messagesPerBurst = Math.Max(1, _config.MessagesPerSecondPerClient / 10); // 10 bursts per second
        var burstInterval = TimeSpan.FromSeconds(1.0 / 10); // 10 bursts per second
        
        var nextBurstTime = stopwatch.Elapsed;

        while (stopwatch.Elapsed < endTime && !cancellationToken.IsCancellationRequested)
        {
            if (!connection.IsConnected) break;

            // Wait until burst time
            var waitTime = nextBurstTime - stopwatch.Elapsed;
            if (waitTime > TimeSpan.Zero)
            {
                await Task.Delay(waitTime, cancellationToken);
            }

            // Send all messages in the burst
            for (int i = 0; i < messagesPerBurst && stopwatch.Elapsed < endTime; i++)
            {
                var message = CreateMessage(connection.ClientId);
                var sent = await connection.SendMessageAsync(message, cancellationToken);
                
                if (sent)
                {
                    await onMessageSent(connection, message);
                }
            }

            // Schedule next burst
            nextBurstTime += burstInterval;
        }
    }

    /// <summary>
    /// Gradually increases message rate from 0 to target rate over the test duration.
    /// </summary>
    private async Task SendRampUpAsync(
        ClientConnection connection,
        Func<ClientConnection, BenchmarkMessage, Task> onMessageSent,
        Stopwatch stopwatch,
        TimeSpan endTime,
        CancellationToken cancellationToken)
    {
        var startTime = stopwatch.Elapsed;
        var rampUpDuration = TimeSpan.FromSeconds(Math.Min(10, endTime.TotalSeconds * 0.3)); // Ramp up over first 30% or 10s, whichever is less
        var targetRate = _config.MessagesPerSecondPerClient;

        RateLimiter? rateLimiter = null;

        while (stopwatch.Elapsed < endTime && !cancellationToken.IsCancellationRequested)
        {
            if (!connection.IsConnected) break;

            // Calculate current rate based on elapsed time
            var elapsed = stopwatch.Elapsed - startTime;
            double currentRate;

            if (elapsed < rampUpDuration)
            {
                // Ramp up: linearly increase from 0 to target rate
                currentRate = (elapsed.TotalSeconds / rampUpDuration.TotalSeconds) * targetRate;
            }
            else
            {
                // Maintain target rate
                currentRate = targetRate;
            }

            // Update rate limiter if rate changed significantly (> 1 msg/sec difference)
            if (rateLimiter == null || Math.Abs(rateLimiter.GetActualRate() - currentRate) > 1.0)
            {
                rateLimiter = new RateLimiter(Math.Max(0.1, currentRate));
            }

            // Wait for next send time
            await rateLimiter.WaitForNextAsync(cancellationToken);

            // Create and send message
            var message = CreateMessage(connection.ClientId);
            var sent = await connection.SendMessageAsync(message, cancellationToken);
            
            if (sent)
            {
                await onMessageSent(connection, message);
            }
        }
    }

    private BenchmarkMessage CreateMessage(int clientId)
    {
        var payload = new byte[_config.MessageSizeBytes];
        _random.NextBytes(payload);

        return new BenchmarkMessage
        {
            MessageId = Interlocked.Increment(ref _nextMessageId),
            ClientId = clientId,
            SentTimestamp = DateTime.UtcNow,
            Payload = payload
        };
    }

    private async Task JoinLotForClientAsync(ClientConnection connection, CancellationToken cancellationToken)
    {
        // Assign each client to a lot (round-robin across lots 1-10)
        var lotId = $"lot-{(connection.ClientId % 10) + 1}";
        _clientLotSubscriptions[connection.ClientId] = lotId;

        var joinLot = new JoinLotMessage { LotId = lotId };
        var json = joinLot.ToJson();
        var sentTimestamp = DateTime.UtcNow;
        
        await connection.SendAuctionMessageAsync(json, sentTimestamp, cancellationToken);
        
        // Wait briefly for LotUpdate response (optional, but helps ensure subscription is active)
        await Task.Delay(100, cancellationToken);
    }

    private PlaceBidMessage CreateBidMessage(int clientId)
    {
        string lotId;
        if (!_clientLotSubscriptions.TryGetValue(clientId, out var storedLotId) || string.IsNullOrEmpty(storedLotId))
        {
            lotId = $"lot-{(clientId % 10) + 1}";
            // Store it for future use
            _clientLotSubscriptions[clientId] = lotId;
        }
        else
        {
            lotId = storedLotId;
        }

        // Start with lot's starting price and increment
        var baseAmount = (clientId % 10 + 1) * 100.0m;
        var bidAmount = baseAmount + (Interlocked.Increment(ref _nextBidId) * 10.0m);

        return new PlaceBidMessage
        {
            LotId = lotId,
            BidderId = $"bidder-{clientId}",
            Amount = bidAmount
        };
    }

    private async Task SendFixedRateAuctionAsync(
        ClientConnection connection,
        Func<ClientConnection, BenchmarkMessage, Task> onMessageSent,
        Stopwatch stopwatch,
        TimeSpan endTime,
        CancellationToken cancellationToken)
    {
        var rateLimiter = new RateLimiter(_config.MessagesPerSecondPerClient);

        while (stopwatch.Elapsed < endTime && !cancellationToken.IsCancellationRequested)
        {
            if (!connection.IsConnected) break;

            await rateLimiter.WaitForNextAsync(cancellationToken);

            var bidMessage = CreateBidMessage(connection.ClientId);
            var json = bidMessage.ToJson();
            var sentTimestamp = DateTime.UtcNow;
            
            // Ensure lotId is valid before recording
            var lotId = string.IsNullOrEmpty(bidMessage.LotId) ? $"lot-{(connection.ClientId % 10) + 1}" : bidMessage.LotId;
            var bidderId = string.IsNullOrEmpty(bidMessage.BidderId) ? $"bidder-{connection.ClientId}" : bidMessage.BidderId;
            
            // Record bid placement in bid metrics collector if available
            _bidMetricsCollector?.RecordBidPlaced(lotId, bidderId, bidMessage.Amount, sentTimestamp);
            
            var sent = await connection.SendAuctionMessageAsync(json, sentTimestamp, cancellationToken);
            
            if (sent)
            {
                // Create a synthetic BenchmarkMessage for metrics tracking
                var syntheticMessage = new BenchmarkMessage
                {
                    MessageId = Interlocked.Increment(ref _nextMessageId),
                    ClientId = connection.ClientId,
                    SentTimestamp = sentTimestamp,
                    Payload = Array.Empty<byte>()
                };
                await onMessageSent(connection, syntheticMessage);
            }
        }
    }

    private async Task SendBurstAuctionAsync(
        ClientConnection connection,
        Func<ClientConnection, BenchmarkMessage, Task> onMessageSent,
        Stopwatch stopwatch,
        TimeSpan endTime,
        CancellationToken cancellationToken)
    {
        var messagesPerBurst = Math.Max(1, _config.MessagesPerSecondPerClient / 10);
        var burstInterval = TimeSpan.FromSeconds(1.0 / 10);
        var nextBurstTime = stopwatch.Elapsed;

        while (stopwatch.Elapsed < endTime && !cancellationToken.IsCancellationRequested)
        {
            if (!connection.IsConnected) break;

            var waitTime = nextBurstTime - stopwatch.Elapsed;
            if (waitTime > TimeSpan.Zero)
            {
                await Task.Delay(waitTime, cancellationToken);
            }

            for (int i = 0; i < messagesPerBurst && stopwatch.Elapsed < endTime; i++)
            {
                var bidMessage = CreateBidMessage(connection.ClientId);
                var json = bidMessage.ToJson();
                var sentTimestamp = DateTime.UtcNow;
                
                // Record bid placement in bid metrics collector if available
                _bidMetricsCollector?.RecordBidPlaced(bidMessage.LotId, bidMessage.BidderId, bidMessage.Amount, sentTimestamp);
                
                var sent = await connection.SendAuctionMessageAsync(json, sentTimestamp, cancellationToken);
                
                if (sent)
                {
                    var syntheticMessage = new BenchmarkMessage
                    {
                        MessageId = Interlocked.Increment(ref _nextMessageId),
                        ClientId = connection.ClientId,
                        SentTimestamp = sentTimestamp,
                        Payload = Array.Empty<byte>()
                    };
                    await onMessageSent(connection, syntheticMessage);
                }
            }

            nextBurstTime += burstInterval;
        }
    }

    private async Task SendRampUpAuctionAsync(
        ClientConnection connection,
        Func<ClientConnection, BenchmarkMessage, Task> onMessageSent,
        Stopwatch stopwatch,
        TimeSpan endTime,
        CancellationToken cancellationToken)
    {
        var startTime = stopwatch.Elapsed;
        var rampUpDuration = TimeSpan.FromSeconds(Math.Min(10, endTime.TotalSeconds * 0.3));
        var targetRate = _config.MessagesPerSecondPerClient;
        RateLimiter? rateLimiter = null;

        while (stopwatch.Elapsed < endTime && !cancellationToken.IsCancellationRequested)
        {
            if (!connection.IsConnected) break;

            var elapsed = stopwatch.Elapsed - startTime;
            double currentRate;

            if (elapsed < rampUpDuration)
            {
                currentRate = (elapsed.TotalSeconds / rampUpDuration.TotalSeconds) * targetRate;
            }
            else
            {
                currentRate = targetRate;
            }

            if (rateLimiter == null || Math.Abs(rateLimiter.GetActualRate() - currentRate) > 1.0)
            {
                rateLimiter = new RateLimiter(Math.Max(0.1, currentRate));
            }

            await rateLimiter.WaitForNextAsync(cancellationToken);

            var bidMessage = CreateBidMessage(connection.ClientId);
            var json = bidMessage.ToJson();
            var sentTimestamp = DateTime.UtcNow;
            
            // Ensure lotId is valid before recording
            var lotId = string.IsNullOrEmpty(bidMessage.LotId) ? $"lot-{(connection.ClientId % 10) + 1}" : bidMessage.LotId;
            var bidderId = string.IsNullOrEmpty(bidMessage.BidderId) ? $"bidder-{connection.ClientId}" : bidMessage.BidderId;
            
            // Record bid placement in bid metrics collector if available
            _bidMetricsCollector?.RecordBidPlaced(lotId, bidderId, bidMessage.Amount, sentTimestamp);
            
            var sent = await connection.SendAuctionMessageAsync(json, sentTimestamp, cancellationToken);
            
            if (sent)
            {
                var syntheticMessage = new BenchmarkMessage
                {
                    MessageId = Interlocked.Increment(ref _nextMessageId),
                    ClientId = connection.ClientId,
                    SentTimestamp = sentTimestamp,
                    Payload = Array.Empty<byte>()
                };
                await onMessageSent(connection, syntheticMessage);
            }
        }
    }
}

