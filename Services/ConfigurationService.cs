using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using WritingTool.Models;

namespace WritingTool.Services
{
    /// <summary>
    /// Service for loading and saving button configuration from JSON file.
    /// </summary>
    public class ConfigurationService
    {
        private readonly string _configPath;

        public ConfigurationService()
        {
            // Store config in the app's local folder
            var appFolder = AppContext.BaseDirectory;
            _configPath = Path.Combine(appFolder, "options.json");
        }

        /// <summary>
        /// Loads the options configuration from the JSON file.
        /// </summary>
        public async Task<OptionsConfig> LoadAsync()
        {
            if (!File.Exists(_configPath))
            {
                return CreateDefaultConfig();
            }

            try
            {
                var json = await File.ReadAllTextAsync(_configPath);
                var config = JsonSerializer.Deserialize(json, AppJsonContext.Default.OptionsConfig);
                
                if (config != null)
                {
                    // Normalize newlines for proper TextBox display
                    foreach (var button in config.Buttons)
                    {
                        button.Prefix = NormalizeNewlines(button.Prefix);
                        button.Instruction = NormalizeNewlines(button.Instruction);
                    }
                }
                
                return config ?? CreateDefaultConfig();
            }
            catch
            {
                return CreateDefaultConfig();
            }
        }

        /// <summary>
        /// Normalizes newlines to Windows-style line endings for proper TextBox display.
        /// Also handles literal \n sequences that weren't parsed by JSON.
        /// </summary>
        private static string NormalizeNewlines(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            
            // First, replace any literal \n (two chars: backslash, n) with placeholder
            input = input.Replace("\\n", "\n");
            
            // Normalize all line endings to Windows-style (\r\n) for proper TextBox display
            // First normalize any existing \r\n or \r, then convert all \n to \r\n
            input = input.Replace("\r\n", "\n");
            input = input.Replace("\r", "\n");
            input = input.Replace("\n", "\r\n");
            
            return input;
        }

        /// <summary>
        /// Saves the options configuration to the JSON file.
        /// </summary>
        public async Task SaveAsync(OptionsConfig config)
        {
            var json = JsonSerializer.Serialize(config, AppJsonContext.Default.OptionsConfig);
            await File.WriteAllTextAsync(_configPath, json);
        }

        private static OptionsConfig CreateDefaultConfig()
        {
            return new OptionsConfig
            {
                Buttons = new()
                {
                    new ButtonConfig
                    {
                        Name = "Proofread",
                        Prefix = "Proofread this:\n\n",
                        Instruction = "You are a grammar proofreading assistant.",
                        Icon = "icons/magnifying-glass",
                        OpenInWindow = false
                    }
                }
            };
        }
    }
}
