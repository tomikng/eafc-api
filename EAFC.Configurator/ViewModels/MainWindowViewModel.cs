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
        private Process? _serverProcess;

        public string AppDescription => "This application configures and manages notifications for multiple platforms and allows manual or scheduled data crawling.";

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
        public ICommand StartCrawlingCommand { get; }
        public ICommand ToggleServerCommand { get; }

        public MainWindowViewModel()
        {
            SaveCommand = ReactiveCommand.Create(SaveConfiguration);
            StartCrawlingCommand = ReactiveCommand.Create(StartCrawling);
            ToggleServerCommand = ReactiveCommand.Create(ToggleServer);
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

        private void StartCrawling()
        {
            string projectPath = GetAbsolutePath("../../../EAFC.Api/EAFC.Api.csproj");
            StartProcess("dotnet", $"run --project \"{projectPath}\" --crawl");
        }

        private void ToggleServer()
        {
            if (IsServerRunning)
            {
                StopServer();
            }
            else
            {
                StartServer();
            }
        }

        private async void StartServer()
        {
            if (IsServerRunning) return;

            ShowCurrentDirectory();
            var projectPath = GetAbsolutePath("../../../../EAFC.Api/EAFC.Api.csproj");

            _serverProcess = StartProcess("dotnet", $"run --project \"{projectPath}\"");
            IsServerRunning = true;

            await Task.Run(() => _serverProcess.WaitForExit());
            IsServerRunning = false;
        }

        private void StopServer()
        {
            if (_serverProcess == null || _serverProcess.HasExited) return;
            _serverProcess.Kill();
            _serverProcess.WaitForExit();
            _serverProcess = null;
            IsServerRunning = false;
        }

        private string GetAbsolutePath(string relativePath)
        {
            return Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, relativePath));
        }

        private Process StartProcess(string fileName, string arguments)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.OutputDataReceived += (sender, e) => AppendToTerminalOutput(e.Data);
            process.ErrorDataReceived += (sender, e) => AppendToTerminalOutput(e.Data);

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            return process;
        }

        private void ShowCurrentDirectory()
        {
            var currentDirectory = Environment.CurrentDirectory;
            AppendToTerminalOutput("Current Directory: " + currentDirectory);
        }

        private void AppendToTerminalOutput(string? output)
        {
            if (output != null)
            {
                TerminalOutput += $"{output}{Environment.NewLine}";
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
