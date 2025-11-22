using EchoServer.Models;
using EchoServer.Repositories;
using Xunit;

namespace EchoServer.Tests.Repositories;

public class InMemoryLotRepositoryTests
{
    [Fact]
    public async Task GetLotAsync_WithNonExistentLot_ReturnsNull()
    {
        var repository = new InMemoryLotRepository();
        var lot = await repository.GetLotAsync("non-existent");
        Assert.Null(lot);
    }

    [Fact]
    public async Task CreateLotAsync_CreatesLotWithCorrectValues()
    {
        var repository = new InMemoryLotRepository();
        var lot = await repository.CreateLotAsync("lot-123", "auction-456", 100.50m);

        Assert.Equal("lot-123", lot.LotId);
        Assert.Equal("auction-456", lot.AuctionId);
        Assert.Equal(100.50m, lot.StartingPrice);
        Assert.Equal(100.50m, lot.CurrentBid);
        Assert.Null(lot.CurrentBidder);
        Assert.Equal(LotStatus.Open, lot.Status);
    }

    [Fact]
    public async Task GetLotAsync_AfterCreate_ReturnsCreatedLot()
    {
        var repository = new InMemoryLotRepository();
        var created = await repository.CreateLotAsync("lot-123", "auction-456", 100.50m);
        var retrieved = await repository.GetLotAsync("lot-123");

        Assert.NotNull(retrieved);
        Assert.Equal(created.LotId, retrieved!.LotId);
        Assert.Equal(created.AuctionId, retrieved.AuctionId);
    }

    [Fact]
    public async Task UpdateLotAsync_UpdatesExistingLot()
    {
        var repository = new InMemoryLotRepository();
        var lot = await repository.CreateLotAsync("lot-123", "auction-456", 100.50m);
        
        lot.CurrentBid = 150.75m;
        lot.CurrentBidder = "bidder-789";
        await repository.UpdateLotAsync(lot);

        var updated = await repository.GetLotAsync("lot-123");
        Assert.NotNull(updated);
        Assert.Equal(150.75m, updated!.CurrentBid);
        Assert.Equal("bidder-789", updated.CurrentBidder);
    }

    [Fact]
    public async Task GetAllLotsAsync_ReturnsAllLots()
    {
        var repository = new InMemoryLotRepository();
        await repository.CreateLotAsync("lot-1", "auction-1", 100m);
        await repository.CreateLotAsync("lot-2", "auction-2", 200m);

        var allLots = await repository.GetAllLotsAsync();
        Assert.Equal(2, allLots.Count);
    }
}

