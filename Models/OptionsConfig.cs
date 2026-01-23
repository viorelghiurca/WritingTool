using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WritingTool.Models
{
    /// <summary>
    /// Root configuration object containing all button definitions.
    /// </summary>
    public class OptionsConfig
    {
        [JsonPropertyName("buttons")]
        public List<ButtonConfig> Buttons { get; set; } = new();
    }
}
