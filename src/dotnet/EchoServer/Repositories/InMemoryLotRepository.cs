using System.Collections.Concurrent;
using EchoServer.Models;

namespace EchoServer.Repositories;

public class InMemoryLotRepository : ILotRepository
{
    private readonly ConcurrentDictionary<string, Lot> _lots = new();

    public Task<Lot?> GetLotAsync(string lotId)
    {
        _lots.TryGetValue(lotId, out var lot);
        return Task.FromResult(lot);
    }

    public Task<Lot> CreateLotAsync(string lotId, string auctionId, decimal startingPrice)
    {
        var lot = new Lot
        {
            LotId = lotId,
            AuctionId = auctionId,
            StartingPrice = startingPrice,
            CurrentBid = startingPrice,
            CurrentBidder = null,
            Status = LotStatus.Open
        };

        _lots.TryAdd(lotId, lot);
        return Task.FromResult(lot);
    }

    public Task UpdateLotAsync(Lot lot)
    {
        _lots.AddOrUpdate(lot.LotId, lot, (key, oldValue) => lot);
        return Task.CompletedTask;
    }

    public Task<List<Lot>> GetAllLotsAsync()
    {
        return Task.FromResult(_lots.Values.ToList());
    }
}

