using EchoServer.Models;
using EchoServer.Repositories;

namespace EchoServer.Services;

public interface IBidService
{
    Task<BidResult> PlaceBidAsync(string lotId, string bidderId, decimal amount);
}

public class BidResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Lot? UpdatedLot { get; set; }
}

public class BidService : IBidService
{
    private readonly ILotRepository _lotRepository;
    private readonly IBidRepository _bidRepository;
    private readonly ILockManager _lockManager;
    private readonly ISubscriptionService _subscriptionService;

    public BidService(
        ILotRepository lotRepository,
        IBidRepository bidRepository,
        ILockManager lockManager,
        ISubscriptionService subscriptionService)
    {
        _lotRepository = lotRepository;
        _bidRepository = bidRepository;
        _lockManager = lockManager;
        _subscriptionService = subscriptionService;
    }

    public async Task<BidResult> PlaceBidAsync(string lotId, string bidderId, decimal amount)
    {
        // Acquire lock for this lot
        var lockAcquired = await _lockManager.AcquireLockAsync(lotId, TimeSpan.FromSeconds(5));
        if (!lockAcquired)
        {
            return new BidResult
            {
                Success = false,
                ErrorMessage = "Could not acquire lock for lot"
            };
        }

        try
        {
            // Get the lot
            var lot = await _lotRepository.GetLotAsync(lotId);
            if (lot == null)
            {
                return new BidResult
                {
                    Success = false,
                    ErrorMessage = "Lot not found"
                };
            }

            // Validate bid
            if (lot.Status != LotStatus.Open)
            {
                return new BidResult
                {
                    Success = false,
                    ErrorMessage = "Lot is closed"
                };
            }

            if (amount <= lot.CurrentBid)
            {
                return new BidResult
                {
                    Success = false,
                    ErrorMessage = $"Bid amount must be greater than current bid of {lot.CurrentBid}"
                };
            }

            // Update lot state atomically
            lot.CurrentBid = amount;
            lot.CurrentBidder = bidderId;
            await _lotRepository.UpdateLotAsync(lot);

            // Store the bid
            var bid = new Bid
            {
                BidId = Guid.NewGuid().ToString(),
                LotId = lotId,
                BidderId = bidderId,
                Amount = amount,
                Timestamp = DateTime.UtcNow
            };
            await _bidRepository.AddBidAsync(bid);

            // Broadcast update to subscribers
            var updateMessage = new LotUpdateMessage
            {
                LotId = lotId,
                CurrentBid = amount,
                CurrentBidder = bidderId,
                Status = lot.Status.ToString()
            };
            await _subscriptionService.BroadcastUpdateAsync(lotId, updateMessage);

            return new BidResult
            {
                Success = true,
                UpdatedLot = lot
            };
        }
        finally
        {
            _lockManager.ReleaseLock(lotId);
        }
    }
}

