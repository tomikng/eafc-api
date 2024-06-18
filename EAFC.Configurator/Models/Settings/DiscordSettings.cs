using System.Text.Json;

namespace EAFC.Configurator.Models
{
    public class DiscordSettings : IPlatformSettings
    {
        public string PlatformName => "Discord";
        public string? GuildId { get; set; }
        public string? ChannelId { get; set; }

        public void SaveSettings(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("PlatformName", PlatformName);
            writer.WriteString("GuildId", GuildId);
            writer.WriteString("ChannelId", ChannelId);
            writer.WriteEndObject();
        }

        public void LoadSettings(JsonElement settingsElement)
        {
            if (settingsElement.TryGetProperty("PlatformSettings", out var platformSettingsElement))
            {
                foreach (var platformSetting in platformSettingsElement.EnumerateArray())
                {
                    if (platformSetting.GetProperty("PlatformName").GetString() == "Discord")
                    {
                        if (platformSetting.TryGetProperty("GuildId", out var guildIdElement))
                        {
                            GuildId = guildIdElement.GetString();
                        }

                        if (platformSetting.TryGetProperty("ChannelId", out var channelIdElement))
                        {
                            ChannelId = channelIdElement.GetString();
                        }
                    }
                }
            }
        }
    }
}