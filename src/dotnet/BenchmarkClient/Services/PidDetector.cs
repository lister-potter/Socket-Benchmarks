using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using BenchmarkClient.Interfaces;

namespace BenchmarkClient.Services;

/// <summary>
/// Cross-platform implementation for detecting process IDs listening on TCP ports.
/// </summary>
public class PidDetector : IPidDetector
{
    public int? DetectPidByPort(int port)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return DetectPidWindows(port);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return DetectPidUnix(port);
        }
        else
        {
            Console.WriteLine($"Warning: Unsupported operating system. Cannot detect server PID for port {port}.");
            return null;
        }
    }

    private int? DetectPidWindows(int port)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "netstat",
                    Arguments = "-ano",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                Console.WriteLine($"Warning: netstat command failed with exit code {process.ExitCode}. Cannot detect server PID for port {port}.");
                return null;
            }

            // Parse netstat output: TCP    0.0.0.0:8080           0.0.0.0:0              LISTENING       12345
            var pattern = $@"TCP\s+\S+:{port}\s+\S+\s+LISTENING\s+(\d+)";
            var match = Regex.Match(output, pattern, RegexOptions.Multiline | RegexOptions.IgnoreCase);

            if (match.Success && int.TryParse(match.Groups[1].Value, out var pid))
            {
                // Check if there are multiple matches
                var matches = Regex.Matches(output, pattern, RegexOptions.Multiline | RegexOptions.IgnoreCase);
                if (matches.Count > 1)
                {
                    Console.WriteLine($"Warning: Multiple processes found listening on port {port}. Using PID {pid}.");
                }

                return pid;
            }

            Console.WriteLine($"Warning: No process found listening on port {port}.");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Error detecting server PID for port {port}: {ex.Message}");
            return null;
        }
    }

    private int? DetectPidUnix(int port)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "lsof",
                    Arguments = $"-i :{port} -t",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            // Check if lsof is available
            if (process.ExitCode == 127 || error.Contains("command not found") || error.Contains("not found"))
            {
                Console.WriteLine($"Warning: 'lsof' command not found. Cannot detect server PID for port {port}. Resource monitoring will be disabled.");
                return null;
            }

            // Check for permission errors
            if (process.ExitCode != 0 && error.Contains("permission denied"))
            {
                Console.WriteLine($"Warning: Insufficient permissions to query process information. Cannot detect server PID for port {port}. Resource monitoring will be disabled.");
                return null;
            }

            // Parse lsof output (with -t flag, it returns only PIDs, one per line)
            var pids = output
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(line => line.Trim())
                .Where(line => int.TryParse(line, out _))
                .Select(line => int.Parse(line))
                .ToList();

            if (pids.Count == 0)
            {
                Console.WriteLine($"Warning: No process found listening on port {port}.");
                return null;
            }

            if (pids.Count > 1)
            {
                Console.WriteLine($"Warning: Multiple processes found listening on port {port}. Using PID {pids[0]}.");
            }

            return pids[0];
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Error detecting server PID for port {port}: {ex.Message}");
            return null;
        }
    }
}

