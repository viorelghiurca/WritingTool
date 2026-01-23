using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using WritingTool.Models;

namespace WritingTool.Services.AI
{
    /// <summary>
    /// AI provider implementation for Google Gemini API.
    /// </summary>
    public class GeminiProvider : IAIProvider
    {
        private readonly string _apiKey;
        private readonly string _modelName;
        private readonly HttpClient _httpClient;

        public string ProviderName => "Gemini";
        public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey);

        public GeminiProvider(string apiKey, string modelName)
        {
            _apiKey = apiKey ?? string.Empty;
            _modelName = string.IsNullOrWhiteSpace(modelName) ? "gemini-2.0-flash" : modelName;
            _httpClient = new HttpClient();
        }

        public async IAsyncEnumerable<string> StreamCompletionAsync(
            List<ChatMessage> messages,
            string systemPrompt,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (!IsConfigured)
            {
                yield return "Error: Gemini API key not configured. Please set it in Settings.";
                yield break;
            }

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_modelName}:streamGenerateContent?key={_apiKey}&alt=sse";

            var contents = new List<GeminiContent>();

            // Add conversation history
            foreach (var msg in messages)
            {
                var role = msg.Role == "assistant" ? "model" : "user";
                contents.Add(new GeminiContent
                {
                    Role = role,
                    Parts = new List<GeminiPart> { new GeminiPart { Text = msg.Content } }
                });
            }

            var requestBody = new GeminiChatRequest
            {
                Contents = contents,
                SystemInstruction = new GeminiSystemInstruction
                {
                    Parts = new List<GeminiPart> { new GeminiPart { Text = systemPrompt } }
                },
                GenerationConfig = new GeminiGenerationConfig
                {
                    Temperature = 0.7,
                    MaxOutputTokens = 8192
                }
            };

            var json = JsonSerializer.Serialize(requestBody, AppJsonContext.Default.GeminiChatRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage? response = null;
            string? errorMessage = null;

            try
            {
                response = await _httpClient.PostAsync(url, content, cancellationToken);
            }
            catch (Exception ex)
            {
                errorMessage = $"Error connecting to Gemini: {ex.Message}";
            }

            if (errorMessage != null)
            {
                yield return errorMessage;
                yield break;
            }

            if (response == null)
            {
                yield return "Error: No response from Gemini API.";
                yield break;
            }

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                yield return $"Gemini API error: {response.StatusCode} - {error}";
                yield break;
            }

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);

            while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(cancellationToken);
                if (string.IsNullOrEmpty(line)) continue;

                // SSE format: "data: {...}"
                if (line.StartsWith("data: "))
                {
                    var jsonData = line[6..];
                    if (jsonData == "[DONE]") break;

                    var chunk = ExtractTextFromResponse(jsonData);
                    if (!string.IsNullOrEmpty(chunk))
                    {
                        yield return chunk;
                    }
                }
            }
        }

        private static string ExtractTextFromResponse(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("candidates", out var candidates) &&
                    candidates.GetArrayLength() > 0)
                {
                    var candidate = candidates[0];
                    if (candidate.TryGetProperty("content", out var contentEl) &&
                        contentEl.TryGetProperty("parts", out var parts) &&
                        parts.GetArrayLength() > 0)
                    {
                        var part = parts[0];
                        if (part.TryGetProperty("text", out var text))
                        {
                            return text.GetString() ?? string.Empty;
                        }
                    }
                }
            }
            catch
            {
                // Ignore parse errors
            }
            return string.Empty;
        }
    }
}
