namespace BenchmarkClient.Interfaces;

/// <summary>
/// Interface for detecting process IDs listening on TCP ports.
/// </summary>
public interface IPidDetector
{
    /// <summary>
    /// Attempts to find the process ID listening on the specified port.
    /// </summary>
    /// <param name="port">The TCP port number to query</param>
    /// <returns>The process ID if found, null otherwise</returns>
    int? DetectPidByPort(int port);
}

