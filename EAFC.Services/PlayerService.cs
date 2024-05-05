using EAFC.Core.Models;
using EAFC.Data;
using EAFC.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EAFC.Services;

public class PlayerService(ApplicationDbContext context) : IPlayerService
{
    public async Task<DateTime?> GetLatestAddedOnDateAsync()
    {
        return await context.Players.MaxAsync(p => (DateTime?)p.AddedOn);
    }
    
    public async Task<Pagination<Player>> GetLatestPlayersAsync(int page = 1, int pageSize = 100)
    {
        var count = await context.Players.CountAsync();
        var items = await context.Players
            .OrderByDescending(p => p.AddedOn)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new Pagination<Player>(items, count, page, pageSize);
    }
    
    public async Task<Pagination<Player>> GetLatestPlayersByLatestAddOnAsync(int page = 1, int pageSize = 100)
    {
        var latestAddOn = await context.Players.MaxAsync(p => p.AddedOn);

        var count = await context.Players.Where(p => p.AddedOn == latestAddOn).CountAsync();

        var items = await context.Players
            .Where(p => p.AddedOn == latestAddOn)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new Pagination<Player>(items, count, page, pageSize);
    }

    public async Task AddPlayersAsync(IEnumerable<Player> players)
    {
        foreach (var player in players)
        {
            player.AddedOn = DateTime.SpecifyKind(player.AddedOn, DateTimeKind.Utc);

            // Check for existing player by a unique identifier, for example
            if (!context.Players.Any(p => p.Name == player.Name && p.AddedOn == player.AddedOn))
            {
                context.Players.Add(player);
            }
        }
        await context.SaveChangesAsync();
    }
}