using System;
using System.Windows.Input;
using ReactiveUI;

namespace EAFC.Configurator.ViewModels;

public class MainWindowViewModel : ReactiveObject
{
    private bool _enableNotifications;
    public bool EnableNotifications
    {
        get => _enableNotifications;
        set => this.RaiseAndSetIfChanged(ref _enableNotifications, value);
    }

    private bool _enableDiscord;
    public bool EnableDiscord
    {
        get => _enableDiscord;
        set => this.RaiseAndSetIfChanged(ref _enableDiscord, value);
    }

    private string? _discordToken;
    public string? DiscordToken
    {
        get => _discordToken;
        set => this.RaiseAndSetIfChanged(ref _discordToken, value);
    }

    private string? _guildId;
    public string? GuildId
    {
        get => _guildId;
        set => this.RaiseAndSetIfChanged(ref _guildId, value);
    }

    private string? _channelId;
    public string? ChannelId
    {
        get => _channelId;
        set => this.RaiseAndSetIfChanged(ref _channelId, value);
    }

    public ICommand SaveCommand { get; }

    public MainWindowViewModel()
    {
        SaveCommand = ReactiveCommand.Create(SaveConfiguration);
    }

    private void SaveConfiguration()
    {
        // Here you would typically save to a settings file or send to a server
        Console.WriteLine($"Configuration Saved: Notifications Enabled = {EnableNotifications}, Discord Enabled = {EnableDiscord}");
        Console.WriteLine($"Discord Token = {DiscordToken}, Guild ID = {GuildId}, Channel ID = {ChannelId}");
    }
}