using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WritingTool.Models
{
    /// <summary>
    /// Configuration model for application settings stored in settings.json.
    /// </summary>
    public class SettingsConfig
    {
        [JsonPropertyName("shortcut")]
        public string Shortcut { get; set; } = "ctrl+space";

        [JsonPropertyName("providers")]
        public Dictionary<string, ProviderConfig> Providers { get; set; } = new()
        {
            ["Gemini (Recommended)"] = new ProviderConfig(),
            ["OpenAI Compatible (For Experts)"] = new ProviderConfig(),
            ["Ollama (For Experts)"] = new ProviderConfig()
        };

        [JsonPropertyName("provider")]
        public string Provider { get; set; } = "Gemini (Recommended)";

        [JsonPropertyName("theme")]
        public string Theme { get; set; } = "mica";

        [JsonPropertyName("language")]
        public string Language { get; set; } = "en";

        [JsonPropertyName("streaming")]
        public bool Streaming { get; set; } = false;

        [JsonPropertyName("start_on_boot")]
        public bool StartOnBoot { get; set; } = false;
    }

    /// <summary>
    /// Configuration for a specific AI provider.
    /// </summary>
    public class ProviderConfig
    {
        [JsonPropertyName("api_key")]
        public string? ApiKey { get; set; }

        [JsonPropertyName("api_base")]
        public string? ApiBase { get; set; }

        [JsonPropertyName("model_name")]
        public string? ModelName { get; set; }

        [JsonPropertyName("api_organisation")]
        public string? ApiOrganisation { get; set; }

        [JsonPropertyName("api_project")]
        public string? ApiProject { get; set; }

        [JsonPropertyName("keep_alive")]
        public string? KeepAlive { get; set; }
    }
}
