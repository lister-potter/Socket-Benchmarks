using EchoServer.Models;

namespace EchoServer.Repositories;

public interface ILotRepository
{
    Task<Lot?> GetLotAsync(string lotId);
    Task<Lot> CreateLotAsync(string lotId, string auctionId, decimal startingPrice);
    Task UpdateLotAsync(Lot lot);
    Task<List<Lot>> GetAllLotsAsync();
}

