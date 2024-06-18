using System.Collections.Generic;
using EAFC.Configurator.Models;

namespace EAFC.Configurator.ViewModels;

public class PlatformSettingsFactory
{
    private readonly Dictionary<string, IPlatformSettings> _platformSettings;

    public PlatformSettingsFactory()
    {
        _platformSettings = new Dictionary<string, IPlatformSettings>();
        RegisterPlatformSettings(new DiscordSettings());
        // Register other platform settings
    }

    public void RegisterPlatformSettings(IPlatformSettings platformSettings)
    {
        _platformSettings[platformSettings.PlatformName] = platformSettings;
    }

    public IPlatformSettings? GetPlatformSettings(string platformName)
    {
        return _platformSettings.GetValueOrDefault(platformName);
    }

    public IEnumerable<IPlatformSettings> GetAllPlatformSettings()
    {
        return _platformSettings.Values;
    }
}