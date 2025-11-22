using BenchmarkClient.Models;

namespace BenchmarkClient.Interfaces;

/// <summary>
/// Interface for collecting bid operation metrics during auction simulation benchmarks.
/// </summary>
public interface IBidMetricsCollector
{
    /// <summary>
    /// Records that a bid was placed (message sent).
    /// </summary>
    /// <param name="lotId">Lot ID the bid was placed on.</param>
    /// <param name="bidderId">ID of the bidder.</param>
    /// <param name="amount">Bid amount.</param>
    /// <param name="timestamp">When the bid was placed.</param>
    void RecordBidPlaced(string lotId, string bidderId, decimal amount, DateTime timestamp);

    /// <summary>
    /// Records that a bid was accepted by the server.
    /// </summary>
    /// <param name="lotId">Lot ID the bid was placed on.</param>
    /// <param name="bidderId">ID of the bidder.</param>
    /// <param name="amount">Bid amount.</param>
    /// <param name="timestamp">When the acceptance was received.</param>
    void RecordBidAccepted(string lotId, string bidderId, decimal amount, DateTime timestamp);

    /// <summary>
    /// Records that a bid was rejected by the server.
    /// </summary>
    /// <param name="lotId">Lot ID the bid was placed on.</param>
    /// <param name="bidderId">ID of the bidder.</param>
    /// <param name="amount">Bid amount.</param>
    /// <param name="reason">Reason for the failure.</param>
    /// <param name="timestamp">When the rejection was received.</param>
    void RecordBidFailed(string lotId, string bidderId, decimal amount, BidFailureReason reason, DateTime timestamp);

    /// <summary>
    /// Gets the current bid metrics.
    /// </summary>
    /// <returns>BidMetrics containing all collected bid statistics.</returns>
    BidMetrics GetMetrics();
}

