using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Input;
using ReactiveUI;
using IPlatformSettings = EAFC.Configurator.Models.IPlatformSettings;

namespace EAFC.Configurator.ViewModels
{
    public class MainWindowViewModel : ReactiveObject
    {
        private readonly string _configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
        private readonly PlatformSettingsFactory _platformSettingsFactory;

        public ObservableCollection<IPlatformSettings> PlatformSettings { get; }

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

        private string? _cronExpression;
        public string? CronExpression
        {
            get => _cronExpression;
            set => this.RaiseAndSetIfChanged(ref _cronExpression, value);
        }

        public ICommand SaveCommand { get; }

        public MainWindowViewModel()
        {
            _platformSettingsFactory = new PlatformSettingsFactory();
            PlatformSettings = new ObservableCollection<IPlatformSettings>(_platformSettingsFactory.GetAllPlatformSettings());
            SaveCommand = ReactiveCommand.Create(SaveConfiguration);
            LoadConfiguration();
        }

        private void SaveConfiguration()
        {
            var config = new
            {
                EnableNotifications,
                AllowedPlatforms = AllowedPlatforms.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries),
                CronExpression,
                PlatformSettings = PlatformSettings.Select(ps =>
                {
                    using (var stream = new MemoryStream())
                    {
                        using (var writer = new Utf8JsonWriter(stream))
                        {
                            ps.SaveSettings(writer);
                        }
                        return JsonDocument.Parse(stream.ToArray()).RootElement.Clone();
                    }
                }).ToArray()
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
                var configElement = JsonDocument.Parse(configJson).RootElement;

                if (configElement.TryGetProperty("EnableNotifications", out var enableNotificationsElement))
                {
                    EnableNotifications = enableNotificationsElement.GetBoolean();
                }

                if (configElement.TryGetProperty("AllowedPlatforms", out var allowedPlatformsElement))
                {
                    AllowedPlatforms = string.Join(",", allowedPlatformsElement.EnumerateArray().Select(x => x.GetString()));
                }

                if (configElement.TryGetProperty("CronExpression", out var cronExpressionElement))
                {
                    CronExpression = cronExpressionElement.GetString();
                }

                foreach (var platformSettings in PlatformSettings)
                {
                    platformSettings.LoadSettings(configElement);
                }

                Console.WriteLine("Configuration Loaded.");
            }
        }
    }
}
