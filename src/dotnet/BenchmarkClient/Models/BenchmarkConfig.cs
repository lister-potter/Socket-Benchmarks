namespace BenchmarkClient.Models;

public class BenchmarkConfig
{
    public string ServerUrl { get; set; } = "ws://localhost:8080";
    public int ClientCount { get; set; } = 1;
    public int MessagesPerSecondPerClient { get; set; } = 100;
    public int MessageSizeBytes { get; set; } = 64;
    public TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(30);
    public MessagePattern Pattern { get; set; } = MessagePattern.FixedRate;
    public string ScenarioName { get; set; } = "single-client";
    public string ServerLanguage { get; set; } = "dotnet";
    public int? ServerProcessId { get; set; }
}

public enum MessagePattern
{
    FixedRate,      // Constant rate per client
    Burst,          // All clients send simultaneously
    RampUp          // Gradually increase rate
}

