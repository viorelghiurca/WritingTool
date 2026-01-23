using System.Text.Json.Serialization;

namespace WritingTool.Models
{
    /// <summary>
    /// Represents the configuration for a single action button.
    /// </summary>
    public class ButtonConfig
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("prefix")]
        public string Prefix { get; set; } = string.Empty;

        [JsonPropertyName("instruction")]
        public string Instruction { get; set; } = string.Empty;

        [JsonPropertyName("icon")]
        public string Icon { get; set; } = string.Empty;

        [JsonPropertyName("openInWindow")]
        public bool OpenInWindow { get; set; }
    }
}
