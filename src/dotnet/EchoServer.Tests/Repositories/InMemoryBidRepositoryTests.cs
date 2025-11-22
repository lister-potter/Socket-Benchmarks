using EchoServer.Models;
using EchoServer.Repositories;
using Xunit;

namespace EchoServer.Tests.Repositories;

public class InMemoryBidRepositoryTests
{
    [Fact]
    public async Task AddBidAsync_AddsBidToRepository()
    {
        var repository = new InMemoryBidRepository();
        var bid = new Bid
        {
            BidId = "bid-123",
            LotId = "lot-456",
            BidderId = "bidder-789",
            Amount = 150.75m,
            Timestamp = DateTime.UtcNow
        };

        await repository.AddBidAsync(bid);
        var bids = await repository.GetBidsForLotAsync("lot-456");

        Assert.Single(bids);
        Assert.Equal("bid-123", bids[0].BidId);
    }

    [Fact]
    public async Task GetBidsForLotAsync_WithNoBids_ReturnsEmptyList()
    {
        var repository = new InMemoryBidRepository();
        var bids = await repository.GetBidsForLotAsync("lot-456");
        Assert.Empty(bids);
    }

    [Fact]
    public async Task GetBidsForLotAsync_WithMultipleBids_ReturnsAllBids()
    {
        var repository = new InMemoryBidRepository();
        await repository.AddBidAsync(new Bid
        {
            BidId = "bid-1",
            LotId = "lot-456",
            BidderId = "bidder-1",
            Amount = 100m,
            Timestamp = DateTime.UtcNow
        });
        await repository.AddBidAsync(new Bid
        {
            BidId = "bid-2",
            LotId = "lot-456",
            BidderId = "bidder-2",
            Amount = 150m,
            Timestamp = DateTime.UtcNow
        });

        var bids = await repository.GetBidsForLotAsync("lot-456");
        Assert.Equal(2, bids.Count);
    }

    [Fact]
    public async Task GetHighestBidAsync_ReturnsHighestBid()
    {
        var repository = new InMemoryBidRepository();
        await repository.AddBidAsync(new Bid
        {
            BidId = "bid-1",
            LotId = "lot-456",
            BidderId = "bidder-1",
            Amount = 100m,
            Timestamp = DateTime.UtcNow
        });
        await repository.AddBidAsync(new Bid
        {
            BidId = "bid-2",
            LotId = "lot-456",
            BidderId = "bidder-2",
            Amount = 150m,
            Timestamp = DateTime.UtcNow
        });
        await repository.AddBidAsync(new Bid
        {
            BidId = "bid-3",
            LotId = "lot-456",
            BidderId = "bidder-3",
            Amount = 120m,
            Timestamp = DateTime.UtcNow
        });

        var highest = await repository.GetHighestBidAsync("lot-456");
        Assert.NotNull(highest);
        Assert.Equal(150m, highest!.Amount);
        Assert.Equal("bid-2", highest.BidId);
    }

    [Fact]
    public async Task GetHighestBidAsync_WithNoBids_ReturnsNull()
    {
        var repository = new InMemoryBidRepository();
        var highest = await repository.GetHighestBidAsync("lot-456");
        Assert.Null(highest);
    }
}

