using System.Text.Json.Serialization;

namespace BenchmarkClient.Models;

/// <summary>
/// Tracks bid operation metrics for auction simulation benchmarks.
/// </summary>
public class BidMetrics
{
    /// <summary>
    /// Total number of bid messages sent.
    /// </summary>
    public int TotalBidsPlaced { get; set; }

    /// <summary>
    /// Number of bids accepted by the server.
    /// </summary>
    public int BidsAccepted { get; set; }

    /// <summary>
    /// Number of bids rejected by the server.
    /// </summary>
    public int BidsFailed { get; set; }

    /// <summary>
    /// Rate of accepted bids (BidsAccepted / TotalBidsPlaced).
    /// </summary>
    public double AcceptanceRate { get; set; }

    /// <summary>
    /// Rate of failed bids (BidsFailed / TotalBidsPlaced).
    /// </summary>
    public double FailureRate { get; set; }

    /// <summary>
    /// Breakdown of failures by reason type.
    /// </summary>
    public Dictionary<BidFailureReason, int> FailureReasonBreakdown { get; set; } = new();

    /// <summary>
    /// Optional detailed log of each bid for debugging purposes.
    /// </summary>
    [JsonIgnore] // Exclude from JSON reports by default to avoid large files
    public List<BidDetail> BidDetails { get; set; } = new();
}

/// <summary>
/// Categories for bid failure reasons.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BidFailureReason
{
    /// <summary>
    /// Bid amount is not greater than the current bid.
    /// </summary>
    BidTooLow,

    /// <summary>
    /// Lot is closed and not accepting bids.
    /// </summary>
    LotClosed,

    /// <summary>
    /// Other error (invalid message format, etc.).
    /// </summary>
    Error
}

/// <summary>
/// Detailed information about a single bid operation.
/// </summary>
public class BidDetail
{
    /// <summary>
    /// Lot ID the bid was placed on.
    /// </summary>
    public string LotId { get; set; } = string.Empty;

    /// <summary>
    /// ID of the bidder.
    /// </summary>
    public string BidderId { get; set; } = string.Empty;

    /// <summary>
    /// Bid amount.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// When the bid was placed.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Outcome of the bid (Accepted or Failed).
    /// </summary>
    public BidOutcome Outcome { get; set; }

    /// <summary>
    /// Failure reason if outcome is Failed, null otherwise.
    /// </summary>
    public BidFailureReason? FailureReason { get; set; }
}

/// <summary>
/// Outcome of a bid operation.
/// </summary>
public enum BidOutcome
{
    /// <summary>
    /// Bid was accepted by the server.
    /// </summary>
    Accepted,

    /// <summary>
    /// Bid was rejected by the server.
    /// </summary>
    Failed
}

