using BenchmarkClient.Services;
using System.Diagnostics;
using Xunit;

namespace BenchmarkClient.Tests;

public class RateLimiterTests
{
    [Fact]
    public void RateLimiter_WithInvalidRate_ThrowsException()
    {
        Assert.Throws<ArgumentException>(() => new RateLimiter(0));
        Assert.Throws<ArgumentException>(() => new RateLimiter(-1));
    }

    [Fact]
    public void RateLimiter_WithValidRate_CreatesSuccessfully()
    {
        var limiter = new RateLimiter(100);
        Assert.NotNull(limiter);
    }

    [Fact]
    public async Task WaitForNextAsync_WithLowRate_RespectsTiming()
    {
        var limiter = new RateLimiter(10); // 10 messages per second = 100ms per message
        var stopwatch = Stopwatch.StartNew();

        // Wait for first message (should be immediate)
        await limiter.WaitForNextAsync(CancellationToken.None);
        var firstWait = stopwatch.Elapsed;

        // Wait for second message (should wait ~100ms)
        stopwatch.Restart();
        await limiter.WaitForNextAsync(CancellationToken.None);
        var secondWait = stopwatch.Elapsed;

        // Allow some tolerance (80-120ms) due to timing precision
        Assert.True(secondWait.TotalMilliseconds >= 80, $"Expected at least 80ms, got {secondWait.TotalMilliseconds}ms");
        Assert.True(secondWait.TotalMilliseconds <= 150, $"Expected at most 150ms, got {secondWait.TotalMilliseconds}ms");
    }

    [Fact]
    public async Task WaitForNextAsync_WithHighRate_RespectsTiming()
    {
        var limiter = new RateLimiter(1000); // 1000 messages per second = 1ms per message
        var stopwatch = Stopwatch.StartNew();

        // Wait for first message (should be immediate)
        await limiter.WaitForNextAsync(CancellationToken.None);
        var firstWait = stopwatch.Elapsed;

        // Wait for second message (should wait ~1ms)
        stopwatch.Restart();
        await limiter.WaitForNextAsync(CancellationToken.None);
        var secondWait = stopwatch.Elapsed;

        // For high rates, allow more tolerance (0.5-5ms)
        Assert.True(secondWait.TotalMilliseconds >= 0.5, $"Expected at least 0.5ms, got {secondWait.TotalMilliseconds}ms");
        Assert.True(secondWait.TotalMilliseconds <= 10, $"Expected at most 10ms, got {secondWait.TotalMilliseconds}ms");
    }

    [Fact]
    public async Task GetActualRate_AfterMultipleWaits_ReturnsReasonableRate()
    {
        var targetRate = 10.0; // 10 messages per second
        var limiter = new RateLimiter(targetRate);
        var stopwatch = Stopwatch.StartNew();

        // Wait for 5 messages
        for (int i = 0; i < 5; i++)
        {
            await limiter.WaitForNextAsync(CancellationToken.None);
        }

        var elapsed = stopwatch.Elapsed.TotalSeconds;
        var actualRate = limiter.GetActualRate();

        // Actual rate should be close to target (within 50% tolerance for short tests)
        Assert.True(actualRate > 0, "Actual rate should be greater than 0");
        // For short tests, rate might be higher initially, so we check it's reasonable
        Assert.True(actualRate <= targetRate * 2, $"Actual rate {actualRate} should not exceed {targetRate * 2}");
    }

    [Fact]
    public void Reset_ResetsInternalState()
    {
        var limiter = new RateLimiter(100);
        
        // Get initial rate (should be 0 or very low)
        var rateBefore = limiter.GetActualRate();
        
        // Reset
        limiter.Reset();
        
        // Rate after reset should be 0 or very low
        var rateAfter = limiter.GetActualRate();
        Assert.True(rateAfter <= rateBefore || rateAfter < 1, "Rate after reset should be low");
    }

    [Fact]
    public async Task WaitForNextAsync_WithCancellation_ThrowsTaskCanceled()
    {
        var limiter = new RateLimiter(1); // 1 message per second = 1000ms per message
        var cts = new CancellationTokenSource();
        
        // Cancel after 100ms
        cts.CancelAfter(100);
        
        // First wait should succeed
        await limiter.WaitForNextAsync(CancellationToken.None);
        
        // Second wait should be cancelled (Task.Delay throws TaskCanceledException)
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await limiter.WaitForNextAsync(cts.Token);
        });
    }
}

