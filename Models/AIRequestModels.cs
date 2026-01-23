using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WritingTool.Models
{
    /// <summary>
    /// Request body for OpenAI-compatible chat completions API.
    /// </summary>
    public class OpenAIChatRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("messages")]
        public List<OpenAIChatMessage> Messages { get; set; } = new();

        [JsonPropertyName("stream")]
        public bool Stream { get; set; } = true;

        [JsonPropertyName("temperature")]
        public double Temperature { get; set; } = 0.7;
    }

    /// <summary>
    /// Message format for OpenAI-compatible APIs.
    /// </summary>
    public class OpenAIChatMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request body for Ollama chat API.
    /// </summary>
    public class OllamaChatRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("messages")]
        public List<OpenAIChatMessage> Messages { get; set; } = new();

        [JsonPropertyName("stream")]
        public bool Stream { get; set; } = true;

        [JsonPropertyName("keep_alive")]
        public string KeepAlive { get; set; } = "15m";
    }

    /// <summary>
    /// Request body for Google Gemini API.
    /// </summary>
    public class GeminiChatRequest
    {
        [JsonPropertyName("contents")]
        public List<GeminiContent> Contents { get; set; } = new();

        [JsonPropertyName("systemInstruction")]
        public GeminiSystemInstruction? SystemInstruction { get; set; }

        [JsonPropertyName("generationConfig")]
        public GeminiGenerationConfig? GenerationConfig { get; set; }
    }

    /// <summary>
    /// Content object for Gemini API.
    /// </summary>
    public class GeminiContent
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("parts")]
        public List<GeminiPart> Parts { get; set; } = new();
    }

    /// <summary>
    /// Part object for Gemini API.
    /// </summary>
    public class GeminiPart
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }

    /// <summary>
    /// System instruction for Gemini API.
    /// </summary>
    public class GeminiSystemInstruction
    {
        [JsonPropertyName("parts")]
        public List<GeminiPart> Parts { get; set; } = new();
    }

    /// <summary>
    /// Generation config for Gemini API.
    /// </summary>
    public class GeminiGenerationConfig
    {
        [JsonPropertyName("temperature")]
        public double Temperature { get; set; } = 0.7;

        [JsonPropertyName("maxOutputTokens")]
        public int MaxOutputTokens { get; set; } = 8192;
    }
}
