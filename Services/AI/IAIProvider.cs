using System.Collections.Generic;
using System.Threading;
using WritingTool.Models;

namespace WritingTool.Services.AI
{
    /// <summary>
    /// Interface for AI providers (Gemini, OpenAI, Ollama).
    /// </summary>
    public interface IAIProvider
    {
        /// <summary>
        /// Streams completion responses from the AI model.
        /// </summary>
        /// <param name="messages">The conversation history.</param>
        /// <param name="systemPrompt">The system prompt/instruction.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Async enumerable of response chunks.</returns>
        IAsyncEnumerable<string> StreamCompletionAsync(
            List<ChatMessage> messages,
            string systemPrompt,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the provider name for display purposes.
        /// </summary>
        string ProviderName { get; }

        /// <summary>
        /// Checks if the provider is properly configured.
        /// </summary>
        bool IsConfigured { get; }
    }
}
