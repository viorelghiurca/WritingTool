using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using WritingTool.Models;

namespace WritingTool.Services.AI
{
    /// <summary>
    /// AI provider implementation for OpenAI-compatible APIs.
    /// </summary>
    public class OpenAIProvider : IAIProvider
    {
        private readonly string _apiKey;
        private readonly string _apiBase;
        private readonly string _modelName;
        private readonly string? _organisation;
        private readonly string? _project;
        private readonly HttpClient _httpClient;

        public string ProviderName => "OpenAI";
        public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey);

        public OpenAIProvider(string apiKey, string apiBase, string modelName, 
            string? organisation = null, string? project = null)
        {
            _apiKey = apiKey ?? string.Empty;
            _apiBase = string.IsNullOrWhiteSpace(apiBase) ? "https://api.openai.com/v1" : apiBase.TrimEnd('/');
            _modelName = string.IsNullOrWhiteSpace(modelName) ? "gpt-4o-mini" : modelName;
            _organisation = organisation;
            _project = project;
            _httpClient = new HttpClient();
        }

        public async IAsyncEnumerable<string> StreamCompletionAsync(
            List<ChatMessage> messages,
            string systemPrompt,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (!IsConfigured)
            {
                yield return "Error: OpenAI API key not configured. Please set it in Settings.";
                yield break;
            }

            var url = $"{_apiBase}/chat/completions";

            var messageList = new List<OpenAIChatMessage>();
            
            // Add system message
            if (!string.IsNullOrWhiteSpace(systemPrompt))
            {
                messageList.Add(new OpenAIChatMessage { Role = "system", Content = systemPrompt });
            }

            // Add conversation history
            foreach (var msg in messages)
            {
                messageList.Add(new OpenAIChatMessage { Role = msg.Role, Content = msg.Content });
            }

            var requestBody = new OpenAIChatRequest
            {
                Model = _modelName,
                Messages = messageList,
                Stream = true,
                Temperature = 0.7
            };

            var json = JsonSerializer.Serialize(requestBody, AppJsonContext.Default.OpenAIChatRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = content
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            
            if (!string.IsNullOrWhiteSpace(_organisation))
            {
                request.Headers.Add("OpenAI-Organization", _organisation);
            }
            if (!string.IsNullOrWhiteSpace(_project))
            {
                request.Headers.Add("OpenAI-Project", _project);
            }

            HttpResponseMessage? response = null;
            string? errorMessage = null;

            try
            {
                response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            }
            catch (Exception ex)
            {
                errorMessage = $"Error connecting to OpenAI: {ex.Message}";
            }

            if (errorMessage != null)
            {
                yield return errorMessage;
                yield break;
            }

            if (response == null)
            {
                yield return "Error: No response from OpenAI API.";
                yield break;
            }

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                yield return $"OpenAI API error: {response.StatusCode} - {error}";
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

                if (root.TryGetProperty("choices", out var choices) &&
                    choices.GetArrayLength() > 0)
                {
                    var choice = choices[0];
                    if (choice.TryGetProperty("delta", out var delta) &&
                        delta.TryGetProperty("content", out var content))
                    {
                        return content.GetString() ?? string.Empty;
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
