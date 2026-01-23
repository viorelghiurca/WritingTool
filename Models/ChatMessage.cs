using System.Text.Json.Serialization;

namespace WritingTool.Models
{
    /// <summary>
    /// Represents a message in a chat conversation.
    /// </summary>
    public class ChatMessage
    {
        /// <summary>
        /// The role of the message sender (user, assistant, system).
        /// </summary>
        [JsonPropertyName("role")]
        public string Role { get; set; } = "user";

        /// <summary>
        /// The content of the message.
        /// </summary>
        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;

        public ChatMessage() { }

        public ChatMessage(string role, string content)
        {
            Role = role;
            Content = content;
        }

        public static ChatMessage User(string content) => new("user", content);
        public static ChatMessage Assistant(string content) => new("assistant", content);
        public static ChatMessage System(string content) => new("system", content);
    }
}
