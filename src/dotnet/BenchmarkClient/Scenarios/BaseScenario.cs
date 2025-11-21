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
        var messageSender = new MessageSender(config);
        var latencyTracker = new LatencyTracker();
        IResourceMonitor? resourceMonitor = null;
        var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // Start resource monitoring if process ID is provided
        if (config.ServerProcessId.HasValue)
        {
            resourceMonitor = new ResourceMonitor();
            resourceMonitor.StartMonitoring(config.ServerProcessId.Value);
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
            var receiveTasks = connectedConnections.Select(c => 
                ReceiveMessagesAsync(c, metricsCollector, latencyTracker, cancellationTokenSource.Token)).ToArray();

            // Start sending messages (MessageSender will filter to connected connections)
            var sendTask = messageSender.StartSendingAsync(
                connectedConnections,
                async (conn, msg) =>
                {
                    metricsCollector.RecordMessageSent(
                        msg.SentTimestamp,
                        msg.ClientId,
                        msg.MessageId,
                        msg.Payload.Length);
                    latencyTracker.RecordSent(msg.MessageId, msg.SentTimestamp);
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

            // Get final metrics
            var metrics = metricsCollector.GetMetrics();
            
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

    private static async Task ReceiveMessagesAsync(
        ClientConnection connection,
        IMetricsCollector metricsCollector,
        LatencyTracker latencyTracker,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && connection.IsConnected)
        {
            var message = await connection.ReceiveMessageAsync(cancellationToken);
            if (message == null)
            {
                await Task.Delay(100, cancellationToken);
                continue;
            }

            var receivedTime = DateTime.UtcNow;
            var latency = receivedTime - message.SentTimestamp;
            
            metricsCollector.RecordMessageReceived(
                receivedTime,
                message.ClientId,
                message.MessageId,
                latency);
            latencyTracker.RecordReceived(message.MessageId, receivedTime);
        }
    }
}

