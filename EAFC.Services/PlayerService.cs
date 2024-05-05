using EAFC.Core.Models;
using EAFC.Data;
using EAFC.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

public class PlayerService(ApplicationDbContext context) : IPlayerService
{
    public async Task<List<Player>> GetLatestPlayersAsync()
    {
        return await context.Players.OrderByDescending(p => p.AddedOn).Take(10).ToListAsync();
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