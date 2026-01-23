using System.Collections.Generic;
using WritingTool.Models;

namespace WritingTool.Services
{
    /// <summary>
    /// Manages conversation history for AI chat sessions.
    /// </summary>
    public class ConversationManager
    {
        private readonly List<ChatMessage> _messages = new();
        private readonly int _maxMessages;

        /// <summary>
        /// Gets the current conversation history.
        /// </summary>
        public IReadOnlyList<ChatMessage> Messages => _messages.AsReadOnly();

        /// <summary>
        /// Gets whether there are any messages in the conversation.
        /// </summary>
        public bool HasMessages => _messages.Count > 0;

        public ConversationManager(int maxMessages = 50)
        {
            _maxMessages = maxMessages;
        }

        /// <summary>
        /// Adds a user message to the conversation.
        /// </summary>
        public void AddUserMessage(string content)
        {
            AddMessage(ChatMessage.User(content));
        }

        /// <summary>
        /// Adds an assistant message to the conversation.
        /// </summary>
        public void AddAssistantMessage(string content)
        {
            AddMessage(ChatMessage.Assistant(content));
        }

        /// <summary>
        /// Adds a message to the conversation.
        /// </summary>
        public void AddMessage(ChatMessage message)
        {
            _messages.Add(message);

            // Trim old messages if exceeding max
            while (_messages.Count > _maxMessages)
            {
                _messages.RemoveAt(0);
            }
        }

        /// <summary>
        /// Clears all messages from the conversation.
        /// </summary>
        public void Clear()
        {
            _messages.Clear();
        }

        /// <summary>
        /// Gets messages as a list for API calls.
        /// </summary>
        public List<ChatMessage> GetMessagesForApi()
        {
            return new List<ChatMessage>(_messages);
        }
    }
}
