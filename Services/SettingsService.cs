using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using WritingTool.Models;

namespace WritingTool.Services
{
    /// <summary>
    /// Service for loading and saving application settings.
    /// </summary>
    public class SettingsService
    {
        private readonly string _settingsPath;

        public SettingsService()
        {
            var appDir = AppContext.BaseDirectory;
            _settingsPath = Path.Combine(appDir, "settings.json");
        }

        public async Task<SettingsConfig> LoadAsync()
        {
            try
            {
                if (File.Exists(_settingsPath))
                {
                    var json = await File.ReadAllTextAsync(_settingsPath);
                    return JsonSerializer.Deserialize(json, AppJsonContext.Default.SettingsConfig) ?? new SettingsConfig();
                }
            }
            catch
            {
                // Return default config on error
            }

            var defaultConfig = new SettingsConfig();
            await SaveAsync(defaultConfig);
            return defaultConfig;
        }

        public async Task SaveAsync(SettingsConfig config)
        {
            var json = JsonSerializer.Serialize(config, AppJsonContext.Default.SettingsConfig);
            await File.WriteAllTextAsync(_settingsPath, json);
        }
    }
}
