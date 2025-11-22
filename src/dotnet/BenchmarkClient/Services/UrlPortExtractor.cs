namespace BenchmarkClient.Services;

/// <summary>
/// Utility class for extracting port numbers from WebSocket URLs.
/// Handles default ports for ws:// (80) and wss:// (443) protocols.
/// </summary>
public static class UrlPortExtractor
{
    /// <summary>
    /// Extracts the port number from a WebSocket URL.
    /// </summary>
    /// <param name="url">The WebSocket URL (ws:// or wss://)</param>
    /// <returns>The port number if successfully extracted, null if the URL is invalid</returns>
    public static int? ExtractPort(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        try
        {
            var uri = new Uri(url);
            var scheme = uri.Scheme.ToLowerInvariant();
            
            // Only accept WebSocket schemes
            if (scheme != "ws" && scheme != "wss")
            {
                return null;
            }
            
            // If port is explicitly specified, use it
            if (uri.Port > 0)
            {
                return uri.Port;
            }
            
            // Otherwise, use default ports based on scheme
            return scheme switch
            {
                "ws" => 80,
                "wss" => 443,
                _ => null
            };
        }
        catch (UriFormatException)
        {
            // Invalid URL format
            return null;
        }
    }
}

