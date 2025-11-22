using EchoServer.Services;
using Xunit;

namespace EchoServer.Tests.Services;

public class LockManagerTests
{
    [Fact]
    public async Task AcquireLockAsync_AcquiresLockSuccessfully()
    {
        var lockManager = new LockManager();
        var acquired = await lockManager.AcquireLockAsync("lot-123", TimeSpan.FromSeconds(1));
        Assert.True(acquired);
        lockManager.ReleaseLock("lot-123");
    }

    [Fact]
    public async Task AcquireLockAsync_WithTimeout_ReturnsFalseWhenTimeout()
    {
        var lockManager = new LockManager();
        
        // Acquire lock first
        await lockManager.AcquireLockAsync("lot-123", TimeSpan.FromSeconds(1));
        
        // Try to acquire again with short timeout (should fail)
        var acquired = await lockManager.AcquireLockAsync("lot-123", TimeSpan.FromMilliseconds(100));
        Assert.False(acquired);
        
        lockManager.ReleaseLock("lot-123");
    }

    [Fact]
    public async Task AcquireLockAsync_DifferentLots_CanBeAcquiredConcurrently()
    {
        var lockManager = new LockManager();
        
        var lock1 = await lockManager.AcquireLockAsync("lot-1", TimeSpan.FromSeconds(1));
        var lock2 = await lockManager.AcquireLockAsync("lot-2", TimeSpan.FromSeconds(1));
        
        Assert.True(lock1);
        Assert.True(lock2);
        
        lockManager.ReleaseLock("lot-1");
        lockManager.ReleaseLock("lot-2");
    }

    [Fact]
    public async Task ReleaseLock_AllowsSubsequentAcquisition()
    {
        var lockManager = new LockManager();
        
        await lockManager.AcquireLockAsync("lot-123", TimeSpan.FromSeconds(1));
        lockManager.ReleaseLock("lot-123");
        
        // Should be able to acquire again after release
        var acquired = await lockManager.AcquireLockAsync("lot-123", TimeSpan.FromSeconds(1));
        Assert.True(acquired);
        
        lockManager.ReleaseLock("lot-123");
    }
}

