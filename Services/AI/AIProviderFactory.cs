using System.Threading.Tasks;
using WritingTool.Models;

namespace WritingTool.Services.AI
{
    /// <summary>
    /// Factory to create AI providers based on settings.
    /// </summary>
    public class AIProviderFactory
    {
        private readonly SettingsService _settingsService;

        public AIProviderFactory(SettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        /// <summary>
        /// Creates an AI provider based on current settings.
        /// </summary>
        public async Task<IAIProvider> CreateProviderAsync()
        {
            var config = await _settingsService.LoadAsync();
            return CreateProvider(config);
        }

        /// <summary>
        /// Creates an AI provider from a settings config.
        /// </summary>
        public static IAIProvider CreateProvider(SettingsConfig config)
        {
            var providerName = config.Provider;

            return providerName switch
            {
                "Gemini (Recommended)" => CreateGeminiProvider(config),
                "OpenAI Compatible (For Experts)" => CreateOpenAIProvider(config),
                "Ollama (For Experts)" => CreateOllamaProvider(config),
                _ => CreateGeminiProvider(config) // Default to Gemini
            };
        }

        private static GeminiProvider CreateGeminiProvider(SettingsConfig config)
        {
            if (config.Providers.TryGetValue("Gemini (Recommended)", out var providerConfig))
            {
                return new GeminiProvider(
                    providerConfig.ApiKey ?? string.Empty,
                    providerConfig.ModelName ?? "gemini-2.0-flash"
                );
            }
            return new GeminiProvider(string.Empty, "gemini-2.0-flash");
        }

        private static OpenAIProvider CreateOpenAIProvider(SettingsConfig config)
        {
            if (config.Providers.TryGetValue("OpenAI Compatible (For Experts)", out var providerConfig))
            {
                return new OpenAIProvider(
                    providerConfig.ApiKey ?? string.Empty,
                    providerConfig.ApiBase ?? "https://api.openai.com/v1",
                    providerConfig.ModelName ?? "gpt-4o-mini",
                    providerConfig.ApiOrganisation,
                    providerConfig.ApiProject
                );
            }
            return new OpenAIProvider(string.Empty, "https://api.openai.com/v1", "gpt-4o-mini");
        }

        private static OllamaProvider CreateOllamaProvider(SettingsConfig config)
        {
            if (config.Providers.TryGetValue("Ollama (For Experts)", out var providerConfig))
            {
                return new OllamaProvider(
                    providerConfig.ApiBase ?? "http://localhost:11434",
                    providerConfig.ModelName ?? "llama3.1:8b",
                    providerConfig.KeepAlive ?? "15"
                );
            }
            return new OllamaProvider("http://localhost:11434", "llama3.1:8b", "15");
        }
    }
}
