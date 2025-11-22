using EchoServer.Models;

namespace EchoServer.Repositories;

public interface IBidRepository
{
    Task AddBidAsync(Bid bid);
    Task<List<Bid>> GetBidsForLotAsync(string lotId);
    Task<Bid?> GetHighestBidAsync(string lotId);
}

