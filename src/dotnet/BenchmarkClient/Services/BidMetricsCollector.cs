using BenchmarkClient.Interfaces;
using BenchmarkClient.Models;

namespace BenchmarkClient.Services;

/// <summary>
/// Thread-safe collector for bid operation metrics during auction simulation benchmarks.
/// </summary>
public class BidMetricsCollector : IBidMetricsCollector
{
    private int _totalBidsPlaced;
    private int _bidsAccepted;
    private int _bidsFailed;
    private readonly Dictionary<BidFailureReason, int> _failureReasonBreakdown = new();
    private readonly List<BidDetail> _bidDetails = new();
    private readonly object _lock = new();

    public void RecordBidPlaced(string lotId, string bidderId, decimal amount, DateTime timestamp)
    {
        if (string.IsNullOrEmpty(lotId)) throw new ArgumentException("Lot ID cannot be null or empty.", nameof(lotId));
        if (string.IsNullOrEmpty(bidderId)) throw new ArgumentException("Bidder ID cannot be null or empty.", nameof(bidderId));

        lock (_lock)
        {
            _totalBidsPlaced++;

            // Store bid detail for correlation with responses
            _bidDetails.Add(new BidDetail
            {
                LotId = lotId,
                BidderId = bidderId,
                Amount = amount,
                Timestamp = timestamp,
                Outcome = BidOutcome.Failed, // Will be updated when we receive response
                FailureReason = null
            });
        }
    }

    public void RecordBidAccepted(string lotId, string bidderId, decimal amount, DateTime timestamp)
    {
        if (string.IsNullOrEmpty(lotId)) throw new ArgumentException("Lot ID cannot be null or empty.", nameof(lotId));
        if (string.IsNullOrEmpty(bidderId)) throw new ArgumentException("Bidder ID cannot be null or empty.", nameof(bidderId));

        lock (_lock)
        {
            _bidsAccepted++;

            // Find and update the most recent matching pending bid
            var matchingBid = FindMatchingPendingBid(lotId, bidderId, amount);
            if (matchingBid != null)
            {
                matchingBid.Outcome = BidOutcome.Accepted;
                matchingBid.FailureReason = null;
            }
        }
    }

    public void RecordBidFailed(string lotId, string bidderId, decimal amount, BidFailureReason reason, DateTime timestamp)
    {
        if (string.IsNullOrEmpty(lotId)) throw new ArgumentException("Lot ID cannot be null or empty.", nameof(lotId));
        if (string.IsNullOrEmpty(bidderId)) throw new ArgumentException("Bidder ID cannot be null or empty.", nameof(bidderId));

        lock (_lock)
        {
            _bidsFailed++;

            // Update failure reason breakdown
            if (!_failureReasonBreakdown.ContainsKey(reason))
            {
                _failureReasonBreakdown[reason] = 0;
            }
            _failureReasonBreakdown[reason]++;

            // Find and update the most recent matching pending bid
            var matchingBid = FindMatchingPendingBid(lotId, bidderId, amount);
            if (matchingBid != null)
            {
                matchingBid.Outcome = BidOutcome.Failed;
                matchingBid.FailureReason = reason;
            }
        }
    }

    public BidMetrics GetMetrics()
    {
        lock (_lock)
        {
            var totalBidsPlaced = _totalBidsPlaced;
            var bidsAccepted = _bidsAccepted;
            var bidsFailed = _bidsFailed;

            var acceptanceRate = totalBidsPlaced > 0 
                ? (double)bidsAccepted / totalBidsPlaced 
                : 0.0;
            var failureRate = totalBidsPlaced > 0 
                ? (double)bidsFailed / totalBidsPlaced 
                : 0.0;

            // Create a copy of the failure reason breakdown
            var failureBreakdown = new Dictionary<BidFailureReason, int>(_failureReasonBreakdown);

            // Create a copy of bid details
            var bidDetails = new List<BidDetail>(_bidDetails);

            return new BidMetrics
            {
                TotalBidsPlaced = totalBidsPlaced,
                BidsAccepted = bidsAccepted,
                BidsFailed = bidsFailed,
                AcceptanceRate = acceptanceRate,
                FailureRate = failureRate,
                FailureReasonBreakdown = failureBreakdown,
                BidDetails = bidDetails
            };
        }
    }

    /// <summary>
    /// Finds the most recent matching pending bid (with Failed outcome) that matches the given criteria.
    /// This helps correlate responses with sent bids.
    /// </summary>
    private BidDetail? FindMatchingPendingBid(string lotId, string bidderId, decimal amount)
    {
        // Search backwards through the list to find the most recent matching pending bid
        for (int i = _bidDetails.Count - 1; i >= 0; i--)
        {
            var bid = _bidDetails[i];
            // Match on lotId and bidderId - amount may vary but lotId and bidderId must match
            // Check for pending bids that haven't been correlated yet (still in Failed state with no reason)
            if (bid.LotId == lotId && 
                bid.BidderId == bidderId && 
                bid.Outcome == BidOutcome.Failed && 
                bid.FailureReason == null) // Not yet correlated
            {
                return bid;
            }
        }
        return null;
    }
}

