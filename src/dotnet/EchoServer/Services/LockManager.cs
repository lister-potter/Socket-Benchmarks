using System.Collections.Concurrent;

namespace EchoServer.Services;

public interface ILockManager
{
    Task<bool> AcquireLockAsync(string lotId, TimeSpan timeout);
    void ReleaseLock(string lotId);
}

public class LockManager : ILockManager
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    public async Task<bool> AcquireLockAsync(string lotId, TimeSpan timeout)
    {
        var semaphore = _locks.GetOrAdd(lotId, _ => new SemaphoreSlim(1, 1));
        return await semaphore.WaitAsync(timeout);
    }

    public void ReleaseLock(string lotId)
    {
        if (_locks.TryGetValue(lotId, out var semaphore))
        {
            semaphore.Release();
        }
    }
}

