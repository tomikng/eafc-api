using EAFC.Core.Models;

namespace EAFC.Services.Interfaces;

public interface IPlayerDataCrawler
{
    Task<List<Player>> FetchNewlyAddedPlayersAsync();
}