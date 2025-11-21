using System.Diagnostics;

namespace BenchmarkClient.Services;

/// <summary>
/// Provides precise rate limiting using a monotonic clock (Stopwatch) with drift correction.
/// Ensures messages are sent at a consistent rate per client, accounting for processing time.
/// </summary>
public class RateLimiter
{
    private readonly double _messagesPerSecond;
    private readonly double _intervalMs;
    private readonly Stopwatch _stopwatch;
    private long _nextSendTick;
    private long _messageCount;

    public RateLimiter(double messagesPerSecond)
    {
        if (messagesPerSecond <= 0)
            throw new ArgumentException("Messages per second must be greater than 0", nameof(messagesPerSecond));

        _messagesPerSecond = messagesPerSecond;
        _intervalMs = 1000.0 / messagesPerSecond;
        _stopwatch = Stopwatch.StartNew();
        _nextSendTick = _stopwatch.ElapsedTicks;
        _messageCount = 0;
    }

    /// <summary>
    /// Waits until it's time to send the next message based on the configured rate.
    /// Uses drift correction to maintain precise timing even if processing takes longer than the interval.
    /// </summary>
    public async Task WaitForNextAsync(CancellationToken cancellationToken)
    {
        _messageCount++;
        
        // Calculate when the next message should be sent
        // Use ticks for precision (Stopwatch.Frequency ticks per second)
        var ticksPerMessage = (long)(Stopwatch.Frequency / _messagesPerSecond);
        _nextSendTick += ticksPerMessage;

        // Calculate how long to wait
        var currentTicks = _stopwatch.ElapsedTicks;
        var waitTicks = _nextSendTick - currentTicks;

        // If we're behind schedule (waitTicks < 0), don't wait but update nextSendTick
        // This prevents unbounded drift accumulation
        if (waitTicks < 0)
        {
            // We're behind - schedule next message immediately after this one
            // Reset to current time + one interval to prevent unbounded catch-up
            _nextSendTick = currentTicks + ticksPerMessage;
            return;
        }

        // Convert ticks to milliseconds for Task.Delay
        var waitMs = (waitTicks * 1000.0) / Stopwatch.Frequency;
        
        // Only wait if we have meaningful time (> 0.1ms)
        if (waitMs > 0.1)
        {
            // Use a more precise delay for small intervals
            if (waitMs < 1.0)
            {
                // For very small intervals, use Thread.Sleep(0) to yield CPU
                // and then spin-wait for precision
                await Task.Delay(0, cancellationToken);
                SpinWait(waitMs);
            }
            else
            {
                // For larger intervals, use Task.Delay
                await Task.Delay(TimeSpan.FromMilliseconds(waitMs), cancellationToken);
            }
        }
    }

    /// <summary>
    /// Spin-waits for the remaining time (for very small intervals < 1ms).
    /// Uses high-resolution timing to achieve sub-millisecond precision.
    /// </summary>
    private void SpinWait(double waitMs)
    {
        var targetTicks = _nextSendTick;
        
        // Spin until we reach the target time
        while (_stopwatch.ElapsedTicks < targetTicks)
        {
            Thread.SpinWait(10);
        }
    }

    /// <summary>
    /// Gets the actual achieved rate based on elapsed time and message count.
    /// Useful for reporting actual vs. target rate.
    /// </summary>
    public double GetActualRate()
    {
        var elapsedSeconds = _stopwatch.Elapsed.TotalSeconds;
        if (elapsedSeconds <= 0) return 0;
        return _messageCount / elapsedSeconds;
    }

    /// <summary>
    /// Resets the rate limiter (useful for RampUp mode).
    /// </summary>
    public void Reset()
    {
        _stopwatch.Restart();
        _nextSendTick = _stopwatch.ElapsedTicks;
        _messageCount = 0;
    }
}

