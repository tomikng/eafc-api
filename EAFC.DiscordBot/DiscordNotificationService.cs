using System.Text;
using Discord;
using Discord.WebSocket;
using EAFC.Core.Models;
using EAFC.Notifications;
using EAFC.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EAFC.DiscordBot;

public class DiscordNotificationService : INotificationService
{
    private readonly DiscordSocketClient _client;
    private readonly string _token;
    private readonly IServiceProvider _serviceProvider;
    private readonly ulong _guildId;
    private readonly ulong _channelId;

    public DiscordNotificationService(string token, IServiceProvider serviceProvider, IConfiguration configuration)
    {
        _client = new DiscordSocketClient();
        _client.Log += LogAsync;
        _client.SlashCommandExecuted += SlashCommandHandler;
        _token = token;
        _serviceProvider = serviceProvider;
        _guildId = ulong.Parse(configuration["DiscordGuildId"] ?? throw new InvalidDataException());
        _channelId = ulong.Parse(configuration["DiscordChannelId"] ?? throw new InvalidDataException());
    }

    public async Task InitializeAsync()
    {
        await _client.LoginAsync(TokenType.Bot, _token);
        await _client.StartAsync();
        _client.Ready += async () =>
        {
            await RegisterCommandsAsync();
        };
    }

    private async Task RegisterCommandsAsync()
    {
        var guild = _client.GetGuild(_guildId);

        var latestCommand = new SlashCommandBuilder()
            .WithName("latest")
            .WithDescription("Get latest players")
            .AddOption("page", ApplicationCommandOptionType.Integer, "Page number", isRequired: false)
            .Build();
        await guild.CreateApplicationCommandAsync(latestCommand);

        var getAllLatestPlayersCommand = new SlashCommandBuilder()
            .WithName("get-all-latest-players")
            .WithDescription("Get all latest players")
            .AddOption("page", ApplicationCommandOptionType.Integer, "Page number", isRequired: false)
            .Build();
        await guild.CreateApplicationCommandAsync(getAllLatestPlayersCommand);
    }

    private Task LogAsync(LogMessage log)
    {
        Console.WriteLine(log.ToString());
        return Task.CompletedTask;
    }

    private async Task SlashCommandHandler(SocketSlashCommand command)
    {
        await command.DeferAsync();

        using var scope = _serviceProvider.CreateScope();
        var playerService = scope.ServiceProvider.GetRequiredService<IPlayerService>();

        try
        {
            switch (command.Data.Name)
            {
                case "latest":
                    var result = await playerService.GetLatestPlayersByLatestAddOnAsync();
                    var responseLatest = FormatPlayers(result.Items);
                    await command.FollowupAsync(responseLatest);
                    break;
                default:
                    await command.FollowupAsync("Unknown command.");
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling command: {ex.Message}");
            await command.FollowupAsync("An error occurred while processing your command.");
        }
    }

    private string? FormatPlayers(List<Player> players)
    {
        if (players.Count == 0)
            // return "No new players found.";
            return null;

        var builder = new StringBuilder();
        builder.AppendLine("Latest Players:");
        foreach (var player in players)
        {
            builder.AppendLine($"**Name:** {player.Name}");
            builder.AppendLine($"**Rating:** {player.Rating}");
            builder.AppendLine($"**Position:** {player.Position}");
            builder.AppendLine($"**Added On:** {player.AddedOn:yyyy-MM-dd}");
            builder.AppendLine($"**Profile URL:** [Link]({player.ProfileUrl})");
            builder.AppendLine();
        }

        return builder.ToString();
    }
    
    public async Task SendAsync(List<Player> players)
    {
        if (await _client.GetChannelAsync(_channelId) is IMessageChannel channel)
        {
            var message = FormatPlayers(players);
            if(message != null)
                await channel.SendMessageAsync(message);
        }
        else
        {
            await Console.Error.WriteLineAsync("Failed to find specified Discord channel.");
        }
    }
}
