using System.Diagnostics;
using BenchmarkClient.Interfaces;
using BenchmarkClient.Models;

namespace BenchmarkClient.Services;

public class ResourceMonitor : IResourceMonitor
{
    private readonly List<ResourceSnapshot> _snapshots = new();
    private readonly object _lock = new();
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _monitoringTask;
    private int _processId;

    public void StartMonitoring(int serverProcessId)
    {
        _processId = serverProcessId;
        _cancellationTokenSource = new CancellationTokenSource();
        _monitoringTask = Task.Run(() => MonitorLoop(_cancellationTokenSource.Token));
    }

    public void StopMonitoring()
    {
        _cancellationTokenSource?.Cancel();
        try
        {
            _monitoringTask?.Wait(TimeSpan.FromSeconds(5));
        }
        catch (AggregateException ex) when (ex.InnerException is TaskCanceledException)
        {
            // Task was canceled, which is expected when stopping monitoring
            // This is normal and not an error
        }
        catch (TaskCanceledException)
        {
            // Task was canceled, which is expected when stopping monitoring
            // This is normal and not an error
        }
    }

    public List<ResourceSnapshot> GetSnapshots()
    {
        lock (_lock)
        {
            return new List<ResourceSnapshot>(_snapshots);
        }
    }

    private async Task MonitorLoop(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var snapshot = CollectSnapshot();
                if (snapshot != null)
                {
                    lock (_lock)
                    {
                        _snapshots.Add(snapshot);
                    }
                }
            }
            catch
            {
                // Ignore errors
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Cancellation requested, exit the loop
                break;
            }
        }
    }

    private ResourceSnapshot? CollectSnapshot()
    {
        try
        {
            var process = Process.GetProcessById(_processId);
            process.Refresh();

            var cpuPercent = 0.0;
            try
            {
                var startTime = process.StartTime;
                var totalProcessorTime = process.TotalProcessorTime;
                var currentTime = DateTime.UtcNow;
                var elapsedTime = currentTime - startTime;
                if (elapsedTime.TotalMilliseconds > 0)
                {
                    cpuPercent = (totalProcessorTime.TotalMilliseconds / elapsedTime.TotalMilliseconds) * 100.0;
                }
            }
            catch
            {
                // CPU calculation may fail on some systems
            }

            return new ResourceSnapshot
            {
                Timestamp = DateTime.UtcNow,
                CpuPercent = cpuPercent,
                MemoryBytes = process.WorkingSet64
            };
        }
        catch
        {
            return null;
        }
    }
}

