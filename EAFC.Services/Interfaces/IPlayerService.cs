using EAFC.Core.Models;

namespace EAFC.Services.Interfaces;

public interface IPlayerService
{
    Task<List<Player>> GetLatestPlayersAsync();
    Task AddPlayersAsync(IEnumerable<Player> players);
}