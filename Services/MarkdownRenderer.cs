using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Text;

using MarkdigBlock = Markdig.Syntax.Block;

namespace WritingTool.Services
{
    /// <summary>
    /// Renders Markdown content as WinUI XAML elements.
    /// </summary>
    public static class MarkdownRenderer
    {
        private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();

        // Syntax highlighting colors (VS Code Dark+ inspired)
        private static readonly Dictionary<string, SolidColorBrush> SyntaxColors = new()
        {
            { "keyword", new SolidColorBrush(ColorHelper.FromArgb(255, 197, 134, 192)) },   // Purple/pink
            { "string", new SolidColorBrush(ColorHelper.FromArgb(255, 206, 145, 120)) },    // Orange
            { "comment", new SolidColorBrush(ColorHelper.FromArgb(255, 106, 153, 85)) },    // Green
            { "number", new SolidColorBrush(ColorHelper.FromArgb(255, 181, 206, 168)) },    // Light green
            { "function", new SolidColorBrush(ColorHelper.FromArgb(255, 220, 220, 170)) },  // Yellow
            { "type", new SolidColorBrush(ColorHelper.FromArgb(255, 78, 201, 176)) },       // Teal
            { "default", new SolidColorBrush(ColorHelper.FromArgb(255, 212, 212, 212)) }    // Light gray
        };

        // Common keywords for various languages
        private static readonly HashSet<string> Keywords = new(StringComparer.OrdinalIgnoreCase)
        {
            // C#/Java/TypeScript
            "public", "private", "protected", "internal", "static", "void", "class", "interface",
            "struct", "enum", "namespace", "using", "import", "export", "from", "return", "if",
            "else", "for", "foreach", "while", "do", "switch", "case", "break", "continue",
            "try", "catch", "finally", "throw", "new", "this", "base", "super", "null", "true",
            "false", "var", "let", "const", "async", "await", "function", "def", "lambda",
            "extends", "implements", "override", "virtual", "abstract", "sealed", "readonly",
            "get", "set", "yield", "in", "out", "ref", "params", "typeof", "instanceof",
            // Python
            "print", "self", "None", "True", "False", "and", "or", "not", "is", "pass",
            "raise", "except", "with", "as", "global", "nonlocal", "assert", "elif",
            // SQL
            "select", "from", "where", "join", "inner", "outer", "left", "right", "on",
            "group", "by", "order", "having", "insert", "update", "delete", "create", "alter",
            "drop", "table", "index", "view", "into", "values", "set", "and", "or", "not",
            "null", "like", "between", "in", "distinct", "count", "sum", "avg", "max", "min"
        };

        private static readonly HashSet<string> Types = new(StringComparer.OrdinalIgnoreCase)
        {
            "string", "int", "bool", "boolean", "float", "double", "decimal", "char", "byte",
            "short", "long", "object", "dynamic", "Task", "List", "Dictionary", "Array",
            "IEnumerable", "IList", "ICollection", "HashSet", "Queue", "Stack", "void",
            "number", "any", "unknown", "never", "undefined", "Promise", "Observable"
        };

        /// <summary>
        /// Renders Markdown text to a WinUI UIElement.
        /// </summary>
        public static UIElement Render(string markdown)
        {
            if (string.IsNullOrWhiteSpace(markdown))
            {
                return new TextBlock { Text = "" };
            }

            var document = Markdown.Parse(markdown, Pipeline);
            var container = new StackPanel { Spacing = 8 };

            foreach (var block in document)
            {
                var element = RenderBlock(block);
                if (element != null)
                {
                    container.Children.Add(element);
                }
            }

            return container;
        }

        private static UIElement? RenderBlock(MarkdigBlock block)
        {
            return block switch
            {
                ParagraphBlock paragraph => RenderParagraph(paragraph),
                HeadingBlock heading => RenderHeading(heading),
                FencedCodeBlock fencedCode => RenderCodeBlock(fencedCode.Info ?? "", GetCodeContent(fencedCode)),
                CodeBlock code => RenderCodeBlock("", GetCodeContent(code)),
                ListBlock list => RenderList(list),
                QuoteBlock quote => RenderQuote(quote),
                ThematicBreakBlock => RenderThematicBreak(),
                _ => null
            };
        }

        private static string GetCodeContent(LeafBlock codeBlock)
        {
            var lines = codeBlock.Lines;
            return string.Join("\n", lines.Lines.Take(lines.Count).Select(l => l.Slice.ToString())).TrimEnd();
        }

        private static UIElement RenderParagraph(ParagraphBlock paragraph)
        {
            var textBlock = new RichTextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                IsTextSelectionEnabled = true
            };

            var p = new Paragraph();
            RenderInlines(paragraph.Inline, p.Inlines);
            textBlock.Blocks.Add(p);

            return textBlock;
        }

        private static UIElement RenderHeading(HeadingBlock heading)
        {
            var fontSize = heading.Level switch
            {
                1 => 24.0,
                2 => 20.0,
                3 => 18.0,
                4 => 16.0,
                5 => 14.0,
                _ => 14.0
            };

            var textBlock = new RichTextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                IsTextSelectionEnabled = true,
                FontSize = fontSize,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, heading.Level <= 2 ? 8 : 4, 0, 4)
            };

            var p = new Paragraph();
            RenderInlines(heading.Inline, p.Inlines);
            textBlock.Blocks.Add(p);

            return textBlock;
        }

        private static UIElement RenderCodeBlock(string language, string code)
        {
            var container = new Border
            {
                Background = new SolidColorBrush(ColorHelper.FromArgb(255, 30, 30, 30)),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 4, 0, 4)
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Language label (if specified)
            if (!string.IsNullOrEmpty(language))
            {
                var languageLabel = new TextBlock
                {
                    Text = language.ToUpperInvariant(),
                    FontSize = 10,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 128, 128, 128)),
                    Margin = new Thickness(0, 0, 0, 8)
                };
                Grid.SetRow(languageLabel, 0);
                grid.Children.Add(languageLabel);
            }

            // Code content with syntax highlighting
            var codeBlock = new RichTextBlock
            {
                FontFamily = new FontFamily("Cascadia Code, Consolas, Courier New, monospace"),
                FontSize = 13,
                TextWrapping = TextWrapping.Wrap,
                IsTextSelectionEnabled = true
            };

            var paragraph = new Paragraph { LineHeight = 20 };
            ApplySyntaxHighlighting(code, language, paragraph.Inlines);
            codeBlock.Blocks.Add(paragraph);

            Grid.SetRow(codeBlock, 1);
            grid.Children.Add(codeBlock);

            container.Child = grid;
            return container;
        }

        private static void ApplySyntaxHighlighting(string code, string language, InlineCollection inlines)
        {
            // Simple tokenizer for syntax highlighting
            var tokens = TokenizeCode(code, language);
            
            foreach (var token in tokens)
            {
                var run = new Run
                {
                    Text = token.Text,
                    Foreground = GetColorForTokenType(token.Type)
                };

                if (token.Type == TokenType.Keyword || token.Type == TokenType.Type)
                {
                    // No font weight change for better readability in code
                }

                inlines.Add(run);
            }
        }

        private enum TokenType { Default, Keyword, String, Comment, Number, Function, Type }

        private class Token
        {
            public string Text { get; set; } = "";
            public TokenType Type { get; set; }
        }

        private static List<Token> TokenizeCode(string code, string language)
        {
            var tokens = new List<Token>();
            var i = 0;

            while (i < code.Length)
            {
                // Check for comments
                if (i + 1 < code.Length)
                {
                    // Single-line comment
                    if ((code[i] == '/' && code[i + 1] == '/') || (code[i] == '#' && language != "csharp"))
                    {
                        var end = code.IndexOf('\n', i);
                        if (end == -1) end = code.Length;
                        tokens.Add(new Token { Text = code[i..end], Type = TokenType.Comment });
                        i = end;
                        continue;
                    }

                    // Multi-line comment
                    if (code[i] == '/' && code[i + 1] == '*')
                    {
                        var end = code.IndexOf("*/", i + 2);
                        if (end == -1) end = code.Length;
                        else end += 2;
                        tokens.Add(new Token { Text = code[i..end], Type = TokenType.Comment });
                        i = end;
                        continue;
                    }
                }

                // Check for strings
                if (code[i] == '"' || code[i] == '\'' || code[i] == '`')
                {
                    var quote = code[i];
                    var end = i + 1;
                    while (end < code.Length && (code[end] != quote || code[end - 1] == '\\'))
                    {
                        end++;
                    }
                    if (end < code.Length) end++;
                    tokens.Add(new Token { Text = code[i..end], Type = TokenType.String });
                    i = end;
                    continue;
                }

                // Check for numbers
                if (char.IsDigit(code[i]) || (code[i] == '.' && i + 1 < code.Length && char.IsDigit(code[i + 1])))
                {
                    var end = i;
                    while (end < code.Length && (char.IsDigit(code[end]) || code[end] == '.' || code[end] == 'x' || 
                           (code[end] >= 'a' && code[end] <= 'f') || (code[end] >= 'A' && code[end] <= 'F')))
                    {
                        end++;
                    }
                    tokens.Add(new Token { Text = code[i..end], Type = TokenType.Number });
                    i = end;
                    continue;
                }

                // Check for identifiers (keywords, types, functions)
                if (char.IsLetter(code[i]) || code[i] == '_')
                {
                    var end = i;
                    while (end < code.Length && (char.IsLetterOrDigit(code[end]) || code[end] == '_'))
                    {
                        end++;
                    }
                    var word = code[i..end];
                    
                    // Check if it's followed by '(' for function detection
                    var isFunction = end < code.Length && code[end] == '(';
                    
                    TokenType type;
                    if (Keywords.Contains(word))
                        type = TokenType.Keyword;
                    else if (Types.Contains(word))
                        type = TokenType.Type;
                    else if (isFunction)
                        type = TokenType.Function;
                    else
                        type = TokenType.Default;

                    tokens.Add(new Token { Text = word, Type = type });
                    i = end;
                    continue;
                }

                // Default: single character
                tokens.Add(new Token { Text = code[i].ToString(), Type = TokenType.Default });
                i++;
            }

            return tokens;
        }

        private static SolidColorBrush GetColorForTokenType(TokenType type)
        {
            return type switch
            {
                TokenType.Keyword => SyntaxColors["keyword"],
                TokenType.String => SyntaxColors["string"],
                TokenType.Comment => SyntaxColors["comment"],
                TokenType.Number => SyntaxColors["number"],
                TokenType.Function => SyntaxColors["function"],
                TokenType.Type => SyntaxColors["type"],
                _ => SyntaxColors["default"]
            };
        }

        private static UIElement RenderList(ListBlock list)
        {
            var stackPanel = new StackPanel { Spacing = 4, Margin = new Thickness(0, 4, 0, 4) };
            var index = 1;

            foreach (var item in list)
            {
                if (item is ListItemBlock listItem)
                {
                    var itemPanel = new StackPanel { Orientation = Orientation.Horizontal };

                    // Bullet or number
                    var bullet = new TextBlock
                    {
                        Text = list.IsOrdered ? $"{index}." : "•",
                        Width = list.IsOrdered ? 24 : 16,
                        Margin = new Thickness(8, 0, 8, 0),
                        FontWeight = FontWeights.Bold,
                        VerticalAlignment = VerticalAlignment.Top
                    };
                    itemPanel.Children.Add(bullet);

                    // Item content
                    var contentPanel = new StackPanel();
                    foreach (var block in listItem)
                    {
                        var element = RenderBlock(block);
                        if (element != null)
                        {
                            contentPanel.Children.Add(element);
                        }
                    }
                    itemPanel.Children.Add(contentPanel);

                    stackPanel.Children.Add(itemPanel);
                    index++;
                }
            }

            return stackPanel;
        }

        private static UIElement RenderQuote(QuoteBlock quote)
        {
            var contentPanel = new StackPanel { Spacing = 4 };

            foreach (var block in quote)
            {
                var element = RenderBlock(block);
                if (element != null)
                {
                    contentPanel.Children.Add(element);
                }
            }

            return new Border
            {
                BorderBrush = new SolidColorBrush(ColorHelper.FromArgb(255, 99, 102, 241)), // Accent color
                BorderThickness = new Thickness(3, 0, 0, 0),
                Padding = new Thickness(12, 4, 0, 4),
                Margin = new Thickness(0, 4, 0, 4),
                Background = new SolidColorBrush(ColorHelper.FromArgb(20, 99, 102, 241)),
                Child = contentPanel
            };
        }

        private static UIElement RenderThematicBreak()
        {
            return new Border
            {
                Height = 1,
                Background = new SolidColorBrush(ColorHelper.FromArgb(80, 128, 128, 128)),
                Margin = new Thickness(0, 8, 0, 8)
            };
        }

        private static void RenderInlines(ContainerInline? container, InlineCollection inlines)
        {
            if (container == null) return;

            foreach (var inline in container)
            {
                switch (inline)
                {
                    case LiteralInline literal:
                        inlines.Add(new Run { Text = literal.Content.ToString() });
                        break;

                    case EmphasisInline emphasis:
                        var emphasisSpan = new Span();
                        if (emphasis.DelimiterCount == 2)
                        {
                            emphasisSpan.FontWeight = FontWeights.Bold;
                        }
                        else
                        {
                            emphasisSpan.FontStyle = Windows.UI.Text.FontStyle.Italic;
                        }
                        RenderInlines(emphasis, emphasisSpan.Inlines);
                        inlines.Add(emphasisSpan);
                        break;

                    case CodeInline code:
                        // Add inline border effect using InlineUIContainer
                        var inlineCode = new Border
                        {
                            Background = new SolidColorBrush(ColorHelper.FromArgb(40, 128, 128, 128)),
                            CornerRadius = new CornerRadius(3),
                            Padding = new Thickness(4, 1, 4, 1),
                            Child = new TextBlock
                            {
                                Text = code.Content,
                                FontFamily = new FontFamily("Cascadia Code, Consolas, Courier New, monospace"),
                                FontSize = 12,
                                IsTextSelectionEnabled = true
                            }
                        };

                        var container2 = new InlineUIContainer { Child = inlineCode };
                        inlines.Add(container2);
                        break;

                    case LinkInline link:
                        var linkSpan = new Span
                        {
                            Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 99, 102, 241))
                        };
                        
                        if (link.IsImage)
                        {
                            // For images, just show alt text
                            var altText = link.FirstChild is LiteralInline lit ? lit.Content.ToString() : "[Image]";
                            linkSpan.Inlines.Add(new Run { Text = $"🖼 {altText}" });
                        }
                        else
                        {
                            RenderInlines(link, linkSpan.Inlines);
                        }
                        
                        inlines.Add(linkSpan);
                        break;

                    case LineBreakInline:
                        inlines.Add(new LineBreak());
                        break;

                    case HtmlInline html:
                        // Skip HTML or render as plain text
                        if (html.Tag == "<br>" || html.Tag == "<br/>" || html.Tag == "<br />")
                        {
                            inlines.Add(new LineBreak());
                        }
                        break;

                    case ContainerInline nestedContainer:
                        RenderInlines(nestedContainer, inlines);
                        break;
                }
            }
        }
    }
}
