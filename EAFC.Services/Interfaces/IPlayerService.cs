using EAFC.Core.Models;

namespace EAFC.Services.Interfaces;

public interface IPlayerService
{
    Task<Pagination<Player>> GetLatestPlayersAsync(int page = 1, int pageSize = 100);
    Task AddPlayersAsync(IEnumerable<Player> players);
}