using System;
using System.Collections.Generic;

namespace WritingTool.Services
{
    /// <summary>
    /// Parses shortcut strings like "ctrl+space" into virtual key codes.
    /// </summary>
    public static class ShortcutParser
    {
        // Virtual key codes for modifiers
        public const int VK_CONTROL = 0x11;
        public const int VK_ALT = 0x12;     // VK_MENU
        public const int VK_SHIFT = 0x10;
        public const int VK_LWIN = 0x5B;

        // Common virtual key codes
        private static readonly Dictionary<string, int> KeyMap = new(StringComparer.OrdinalIgnoreCase)
        {
            // Letters A-Z (0x41 - 0x5A)
            ["a"] = 0x41, ["b"] = 0x42, ["c"] = 0x43, ["d"] = 0x44, ["e"] = 0x45,
            ["f"] = 0x46, ["g"] = 0x47, ["h"] = 0x48, ["i"] = 0x49, ["j"] = 0x4A,
            ["k"] = 0x4B, ["l"] = 0x4C, ["m"] = 0x4D, ["n"] = 0x4E, ["o"] = 0x4F,
            ["p"] = 0x50, ["q"] = 0x51, ["r"] = 0x52, ["s"] = 0x53, ["t"] = 0x54,
            ["u"] = 0x55, ["v"] = 0x56, ["w"] = 0x57, ["x"] = 0x58, ["y"] = 0x59,
            ["z"] = 0x5A,

            // Numbers 0-9 (0x30 - 0x39)
            ["0"] = 0x30, ["1"] = 0x31, ["2"] = 0x32, ["3"] = 0x33, ["4"] = 0x34,
            ["5"] = 0x35, ["6"] = 0x36, ["7"] = 0x37, ["8"] = 0x38, ["9"] = 0x39,

            // Function keys F1-F12 (0x70 - 0x7B)
            ["f1"] = 0x70, ["f2"] = 0x71, ["f3"] = 0x72, ["f4"] = 0x73,
            ["f5"] = 0x74, ["f6"] = 0x75, ["f7"] = 0x76, ["f8"] = 0x77,
            ["f9"] = 0x78, ["f10"] = 0x79, ["f11"] = 0x7A, ["f12"] = 0x7B,

            // Special keys
            ["space"] = 0x20,
            ["enter"] = 0x0D,
            ["return"] = 0x0D,
            ["tab"] = 0x09,
            ["escape"] = 0x1B,
            ["esc"] = 0x1B,
            ["backspace"] = 0x08,
            ["delete"] = 0x2E,
            ["del"] = 0x2E,
            ["insert"] = 0x2D,
            ["ins"] = 0x2D,
            ["home"] = 0x24,
            ["end"] = 0x23,
            ["pageup"] = 0x21,
            ["pgup"] = 0x21,
            ["pagedown"] = 0x22,
            ["pgdn"] = 0x22,

            // Arrow keys
            ["up"] = 0x26,
            ["down"] = 0x28,
            ["left"] = 0x25,
            ["right"] = 0x27,

            // Punctuation
            ["`"] = 0xC0,
            ["~"] = 0xC0,
            ["-"] = 0xBD,
            ["="] = 0xBB,
            ["["] = 0xDB,
            ["]"] = 0xDD,
            ["\\"] = 0xDC,
            [";"] = 0xBA,
            ["'"] = 0xDE,
            [","] = 0xBC,
            ["."] = 0xBE,
            ["/"] = 0xBF
        };

        private static readonly Dictionary<string, int> ModifierMap = new(StringComparer.OrdinalIgnoreCase)
        {
            ["ctrl"] = VK_CONTROL,
            ["control"] = VK_CONTROL,
            ["alt"] = VK_ALT,
            ["shift"] = VK_SHIFT,
            ["win"] = VK_LWIN,
            ["windows"] = VK_LWIN
        };

        /// <summary>
        /// Parses a shortcut string into modifiers and main key.
        /// </summary>
        /// <param name="shortcut">Shortcut string like "ctrl+space" or "alt+shift+s"</param>
        /// <returns>Tuple of modifier key codes and main key code</returns>
        public static (int[] Modifiers, int Key) Parse(string shortcut)
        {
            if (string.IsNullOrWhiteSpace(shortcut))
            {
                // Default to Ctrl+Space
                return (new[] { VK_CONTROL }, 0x20);
            }

            var parts = shortcut.ToLowerInvariant().Split('+', StringSplitOptions.RemoveEmptyEntries);
            var modifiers = new List<int>();
            int mainKey = 0x20; // Default to space

            foreach (var part in parts)
            {
                var trimmed = part.Trim();

                if (ModifierMap.TryGetValue(trimmed, out int modifierKey))
                {
                    if (!modifiers.Contains(modifierKey))
                    {
                        modifiers.Add(modifierKey);
                    }
                }
                else if (KeyMap.TryGetValue(trimmed, out int key))
                {
                    mainKey = key;
                }
            }

            // If no modifiers specified, default to Ctrl
            if (modifiers.Count == 0)
            {
                modifiers.Add(VK_CONTROL);
            }

            return (modifiers.ToArray(), mainKey);
        }

        /// <summary>
        /// Formats a shortcut for display.
        /// </summary>
        public static string Format(string shortcut)
        {
            if (string.IsNullOrWhiteSpace(shortcut))
            {
                return "Ctrl+Space";
            }

            var parts = shortcut.Split('+', StringSplitOptions.RemoveEmptyEntries);
            var formatted = new List<string>();

            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                // Capitalize first letter
                if (trimmed.Length > 0)
                {
                    formatted.Add(char.ToUpper(trimmed[0]) + trimmed[1..].ToLower());
                }
            }

            return string.Join("+", formatted);
        }
    }
}
