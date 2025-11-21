using BenchmarkClient.Models;

namespace BenchmarkClient.Interfaces;

public interface IResourceMonitor
{
    void StartMonitoring(int serverProcessId);
    void StopMonitoring();
    List<ResourceSnapshot> GetSnapshots();
}

