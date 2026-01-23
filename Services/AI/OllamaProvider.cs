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
    /// AI provider implementation for Ollama local API.
    /// </summary>
    public class OllamaProvider : IAIProvider
    {
        private readonly string _apiBase;
        private readonly string _modelName;
        private readonly string _keepAlive;
        private readonly HttpClient _httpClient;

        public string ProviderName => "Ollama";
        public bool IsConfigured => !string.IsNullOrWhiteSpace(_modelName);

        public OllamaProvider(string apiBase, string modelName, string keepAlive)
        {
            _apiBase = string.IsNullOrWhiteSpace(apiBase) ? "http://localhost:11434" : apiBase.TrimEnd('/');
            _modelName = string.IsNullOrWhiteSpace(modelName) ? "llama3.1:8b" : modelName;
            _keepAlive = string.IsNullOrWhiteSpace(keepAlive) ? "15m" : $"{keepAlive}m";
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(10) // Ollama can be slow for first load
            };
        }

        public async IAsyncEnumerable<string> StreamCompletionAsync(
            List<ChatMessage> messages,
            string systemPrompt,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (!IsConfigured)
            {
                yield return "Error: Ollama model not configured. Please set it in Settings.";
                yield break;
            }

            var url = $"{_apiBase}/api/chat";

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

            var requestBody = new OllamaChatRequest
            {
                Model = _modelName,
                Messages = messageList,
                Stream = true,
                KeepAlive = _keepAlive
            };

            var json = JsonSerializer.Serialize(requestBody, AppJsonContext.Default.OllamaChatRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage? response = null;
            string? errorMessage = null;

            try
            {
                response = await _httpClient.PostAsync(url, content, cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                errorMessage = $"Error connecting to Ollama at {_apiBase}: {ex.Message}. Is Ollama running?";
            }
            catch (Exception ex)
            {
                errorMessage = $"Error: {ex.Message}";
            }

            if (errorMessage != null)
            {
                yield return errorMessage;
                yield break;
            }

            if (response == null)
            {
                yield return "Error: No response from Ollama API.";
                yield break;
            }

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                yield return $"Ollama API error: {response.StatusCode} - {error}";
                yield break;
            }

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);

            // Ollama uses newline-delimited JSON
            while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(cancellationToken);
                if (string.IsNullOrEmpty(line)) continue;

                var chunk = ExtractTextFromResponse(line);
                if (!string.IsNullOrEmpty(chunk))
                {
                    yield return chunk;
                }
            }
        }

        private static string ExtractTextFromResponse(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("message", out var message) &&
                    message.TryGetProperty("content", out var content))
                {
                    return content.GetString() ?? string.Empty;
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
