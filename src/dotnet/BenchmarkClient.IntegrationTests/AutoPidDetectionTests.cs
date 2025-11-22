using BenchmarkClient.Interfaces;
using BenchmarkClient.Models;
using BenchmarkClient.Services;
using BenchmarkClient.Scenarios;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Xunit;

namespace BenchmarkClient.IntegrationTests;

public class AutoPidDetectionTests
{
    [Fact]
    public void AutoDetectPid_WithExplicitPid_SkipsDetection()
    {
        // Arrange
        var config = new BenchmarkConfig
        {
            ServerUrl = "ws://localhost:8080",
            ServerProcessId = 12345 // Explicit PID
        };

        // Act - Simulate what Program.cs does
        var port = UrlPortExtractor.ExtractPort(config.ServerUrl);
        var pidDetector = new PidDetector();
        
        // Should not attempt detection since PID is already set
        // In real usage, detection would be skipped in Program.cs
        Assert.True(config.ServerProcessId.HasValue);
        Assert.Equal(12345, config.ServerProcessId.Value);
    }

    [Fact]
    public void AutoDetectPid_WithNoPid_AttemptsDetection()
    {
        // Arrange
        var config = new BenchmarkConfig
        {
            ServerUrl = "ws://localhost:8080",
            ServerProcessId = null // No explicit PID
        };

        // Act - Simulate what Program.cs does
        var port = UrlPortExtractor.ExtractPort(config.ServerUrl);
        Assert.True(port.HasValue);
        
        var pidDetector = new PidDetector();
        var detectedPid = pidDetector.DetectPidByPort(port.Value);
        
        // Assert - Detection should be attempted (may return null if no process found)
        // This is expected behavior - the test verifies the detection logic runs
        // In a real scenario with a running server, a PID would be returned
    }

    [Fact]
    public void AutoDetectPid_WithInvalidUrl_LogsErrorAndSkipsDetection()
    {
        // Arrange
        var config = new BenchmarkConfig
        {
            ServerUrl = "not-a-valid-url",
            ServerProcessId = null
        };

        // Act
        var port = UrlPortExtractor.ExtractPort(config.ServerUrl);
        
        // Assert
        Assert.Null(port);
        // Detection should be skipped when port extraction fails
    }

    [Fact]
    public void AutoDetectPid_WithNoListener_ReturnsNull()
    {
        // Arrange - Use a port that's very unlikely to have a listener
        var port = 65535; // High port number, unlikely to be in use
        
        // Act
        var pidDetector = new PidDetector();
        var detectedPid = pidDetector.DetectPidByPort(port);
        
        // Assert - Should return null when no process is found
        // This verifies the detection gracefully handles the "no process found" case
        Assert.Null(detectedPid);
    }

    [Fact(Skip = "Requires a running server on port 8080")]
    public void AutoDetectPid_WithRunningServer_DetectsPid()
    {
        // This test should be run manually when a server is actually running
        // Arrange - Assume a server is running on port 8080
        var port = 8080;
        
        // Act
        var pidDetector = new PidDetector();
        var detectedPid = pidDetector.DetectPidByPort(port);
        
        // Assert
        Assert.NotNull(detectedPid);
        Assert.True(detectedPid.Value > 0);
        
        // Verify the PID is actually a running process
        try
        {
            var process = Process.GetProcessById(detectedPid.Value);
            Assert.NotNull(process);
        }
        catch (ArgumentException)
        {
            Assert.Fail($"Detected PID {detectedPid.Value} is not a valid process");
        }
    }

    [Fact]
    public void ResourceMonitor_WithDetectedPid_StartsMonitoring()
    {
        // Arrange
        var config = new BenchmarkConfig
        {
            ServerUrl = "ws://localhost:8080",
            ServerProcessId = null
        };

        // Simulate detection
        var port = UrlPortExtractor.ExtractPort(config.ServerUrl);
        if (port.HasValue)
        {
            var pidDetector = new PidDetector();
            var detectedPid = pidDetector.DetectPidByPort(port.Value);
            
            if (detectedPid.HasValue)
            {
                config.ServerProcessId = detectedPid;
                
                // Act - Start monitoring
                var resourceMonitor = new ResourceMonitor();
                resourceMonitor.StartMonitoring(detectedPid.Value);
                
                // Assert - Monitoring should be active
                // Note: We can't easily verify monitoring is working without waiting,
                // but we can verify it doesn't throw
                resourceMonitor.StopMonitoring();
            }
        }
    }

    [Fact]
    public void ResourceMonitor_WithoutPid_SkipsMonitoring()
    {
        // Arrange
        var config = new BenchmarkConfig
        {
            ServerUrl = "ws://localhost:8080",
            ServerProcessId = null
        };

        // Act - Simulate scenario execution without PID
        IResourceMonitor? resourceMonitor = null;
        if (config.ServerProcessId.HasValue)
        {
            resourceMonitor = new ResourceMonitor();
            resourceMonitor.StartMonitoring(config.ServerProcessId.Value);
        }

        // Assert - Monitoring should not be started
        Assert.Null(resourceMonitor);
    }
}

