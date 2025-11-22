using System.Collections.Concurrent;
using EchoServer.Models;

namespace EchoServer.Repositories;

public class InMemoryBidRepository : IBidRepository
{
    private readonly ConcurrentDictionary<string, List<Bid>> _bidsByLot = new();

    public Task AddBidAsync(Bid bid)
    {
        _bidsByLot.AddOrUpdate(
            bid.LotId,
            new List<Bid> { bid },
            (key, existingList) =>
            {
                var newList = new List<Bid>(existingList) { bid };
                return newList;
            });
        return Task.CompletedTask;
    }

    public Task<List<Bid>> GetBidsForLotAsync(string lotId)
    {
        if (_bidsByLot.TryGetValue(lotId, out var bids))
        {
            return Task.FromResult(new List<Bid>(bids));
        }
        return Task.FromResult(new List<Bid>());
    }

    public Task<Bid?> GetHighestBidAsync(string lotId)
    {
        if (!_bidsByLot.TryGetValue(lotId, out var bids) || bids.Count == 0)
        {
            return Task.FromResult<Bid?>(null);
        }

        var highestBid = bids.OrderByDescending(b => b.Amount).First();
        return Task.FromResult<Bid?>(highestBid);
    }
}

