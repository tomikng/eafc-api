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

        private string _terminalOutput;
        public string TerminalOutput
        {
            get => _terminalOutput;
            set => this.RaiseAndSetIfChanged(ref _terminalOutput, value);
        }

        private bool _isServerRunning;
        public bool IsServerRunning
        {
            get => _isServerRunning;
            set => this.RaiseAndSetIfChanged(ref _isServerRunning, value);
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
                    DiscordGuildId = config.DiscordGuildId;
                    DiscordChannelId = config.DiscordChannelId;
                    CronExpression = config.CronExpression;
                }

                Console.WriteLine("Configuration Loaded.");
            }
        }

        private class Config
        {
            public string? DiscordGuildId { get; set; }
            public string? DiscordChannelId { get; set; }
            public string? CronExpression { get; set; }
        }
    }
}
