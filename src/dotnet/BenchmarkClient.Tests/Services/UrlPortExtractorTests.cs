using BenchmarkClient.Services;
using Xunit;

namespace BenchmarkClient.Tests.Services;

public class UrlPortExtractorTests
{
    [Theory]
    [InlineData("ws://localhost:8080", 8080)]
    [InlineData("ws://127.0.0.1:8080", 8080)]
    [InlineData("wss://example.com:443", 443)]
    [InlineData("ws://localhost:3000", 3000)]
    [InlineData("wss://example.com:8443", 8443)]
    public void ExtractPort_WithExplicitPort_ReturnsPort(string url, int expectedPort)
    {
        var result = UrlPortExtractor.ExtractPort(url);
        Assert.Equal(expectedPort, result);
    }

    [Theory]
    [InlineData("ws://localhost", 80)]
    [InlineData("ws://127.0.0.1", 80)]
    [InlineData("ws://example.com", 80)]
    public void ExtractPort_WithWsSchemeAndNoPort_ReturnsDefaultPort80(string url, int expectedPort)
    {
        var result = UrlPortExtractor.ExtractPort(url);
        Assert.Equal(expectedPort, result);
    }

    [Theory]
    [InlineData("wss://localhost", 443)]
    [InlineData("wss://127.0.0.1", 443)]
    [InlineData("wss://example.com", 443)]
    public void ExtractPort_WithWssSchemeAndNoPort_ReturnsDefaultPort443(string url, int expectedPort)
    {
        var result = UrlPortExtractor.ExtractPort(url);
        Assert.Equal(expectedPort, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ExtractPort_WithNullOrEmptyUrl_ReturnsNull(string? url)
    {
        var result = UrlPortExtractor.ExtractPort(url);
        Assert.Null(result);
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("http://localhost:8080")] // Wrong scheme
    [InlineData("ftp://localhost:8080")] // Wrong scheme
    [InlineData("invalid://:8080")] // Invalid format
    public void ExtractPort_WithInvalidUrl_ReturnsNull(string url)
    {
        var result = UrlPortExtractor.ExtractPort(url);
        Assert.Null(result);
    }

    [Fact]
    public void ExtractPort_WithCaseInsensitiveScheme_HandlesCorrectly()
    {
        var result1 = UrlPortExtractor.ExtractPort("WS://localhost:8080");
        var result2 = UrlPortExtractor.ExtractPort("WSS://localhost:443");
        
        Assert.Equal(8080, result1);
        Assert.Equal(443, result2);
    }
}

