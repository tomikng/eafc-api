using System.Text.Json;

namespace EAFC.Configurator.Models
{
    public interface IPlatformSettings
    {
        string PlatformName { get; }
        void SaveSettings(Utf8JsonWriter jsonWriter);
        void LoadSettings(JsonElement settingsElement);
    }
}