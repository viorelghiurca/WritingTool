using System.Text.Json.Serialization;

namespace WritingTool.Models
{
    /// <summary>
    /// Source-generated JSON serializer context for AOT/Trimming compatibility.
    /// This eliminates IL2026 warnings and ensures JSON serialization works correctly
    /// in published/self-contained builds.
    /// </summary>
    [JsonSourceGenerationOptions(
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonSerializable(typeof(OptionsConfig))]
    [JsonSerializable(typeof(ButtonConfig))]
    [JsonSerializable(typeof(SettingsConfig))]
    [JsonSerializable(typeof(ProviderConfig))]
    [JsonSerializable(typeof(ChatMessage))]
    [JsonSerializable(typeof(OpenAIChatRequest))]
    [JsonSerializable(typeof(OpenAIChatMessage))]
    [JsonSerializable(typeof(OllamaChatRequest))]
    [JsonSerializable(typeof(GeminiChatRequest))]
    [JsonSerializable(typeof(GeminiContent))]
    [JsonSerializable(typeof(GeminiPart))]
    [JsonSerializable(typeof(GeminiSystemInstruction))]
    [JsonSerializable(typeof(GeminiGenerationConfig))]
    public partial class AppJsonContext : JsonSerializerContext
    {
    }
}
