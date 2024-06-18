using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows.Input;
using System.Threading.Tasks;
using ReactiveUI;

namespace EAFC.Configurator.ViewModels
{
    public class MainWindowViewModel : ReactiveObject
    {
        private readonly string _configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");

        private bool _enableNotifications;

        public bool EnableNotifications
        {
            get => _enableNotifications;
            set => this.RaiseAndSetIfChanged(ref _enableNotifications, value);
        }

        private string _allowedPlatforms;

        public string AllowedPlatforms
        {
            get => _allowedPlatforms;
            set => this.RaiseAndSetIfChanged(ref _allowedPlatforms, value);
        }

        private string? _discordGuildId;

        public string? DiscordGuildId
        {
            get => _discordGuildId;
            set => this.RaiseAndSetIfChanged(ref _discordGuildId, value);
        }

        private string? _discordChannelId;

        public string? DiscordChannelId
        {
            get => _discordChannelId;
            set => this.RaiseAndSetIfChanged(ref _discordChannelId, value);
        }

        private string? _cronExpression;

        public string? CronExpression
        {
            get => _cronExpression;
            set => this.RaiseAndSetIfChanged(ref _cronExpression, value);
        }

        public ICommand SaveCommand { get; }

        public MainWindowViewModel()
        {
            SaveCommand = ReactiveCommand.Create(SaveConfiguration);
            LoadConfiguration();
        }

        private void SaveConfiguration()
        {
            var config = new
            {
                EnableNotifications,
                AllowedPlatforms = AllowedPlatforms.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries),
                DiscordGuildId,
                DiscordChannelId,
                CronExpression
            };

            var configJson = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_configFilePath, configJson);

            Console.WriteLine("Configuration Saved.");
        }

        private void LoadConfiguration()
        {
            if (File.Exists(_configFilePath))
            {
                var configJson = File.ReadAllText(_configFilePath);
                var config = JsonSerializer.Deserialize<Config>(configJson);

                if (config != null)
                {
                    EnableNotifications = config.EnableNotifications;
                    AllowedPlatforms = string.Join(",", config.AllowedPlatforms);
                    DiscordGuildId = config.DiscordGuildId;
                    DiscordChannelId = config.DiscordChannelId;
                    CronExpression = config.CronExpression;
                }

                Console.WriteLine("Configuration Loaded.");
            }
        }

        private class Config
        {
            public bool EnableNotifications { get; set; }
            public string[] AllowedPlatforms { get; set; } = Array.Empty<string>();
            public string? DiscordGuildId { get; set; }
            public string? DiscordChannelId { get; set; }
            public string? CronExpression { get; set; }
        }

    }
}
