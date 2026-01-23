using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Text;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using WinRT.Interop;
using WritingTool.Models;
using WritingTool.Services;
using WritingTool.Services.AI;

namespace WritingTool
{
    /// <summary>
    /// Chat window for asking AI questions with streaming responses.
    /// </summary>
    public sealed partial class AskWindow : Window
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private const int VK_CONTROL = 0x11;
        private const int VK_LCONTROL = 0xA2;
        private const int VK_RCONTROL = 0xA3;
        private const int VK_SHIFT = 0x10;

        private const int GWL_STYLE = -16;
        private const int WS_BORDER = 0x00800000;
        private const int WS_DLGFRAME = 0x00400000;
        private const int WS_THICKFRAME = 0x00040000;
        private const int WS_CAPTION = WS_BORDER | WS_DLGFRAME;
        private const uint SWP_FRAMECHANGED = 0x0020;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_SHOWWINDOW = 0x0040;
        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const int SM_CXSCREEN = 0;
        private const int SM_CYSCREEN = 1;

        private AppWindow _appWindow = null!;
        private WindowAnimationHelper? _animationHelper;
        private readonly int _compactWidth = 350;
        private readonly int _compactHeight = 120;
        private readonly int _expandedWidth = 450;
        private readonly int _expandedHeight = 550;

        private readonly SettingsService _settingsService = new();
        private readonly ConversationManager _conversationManager = new();
        private CancellationTokenSource? _cancellationTokenSource;
        private bool _isExpanded = false;
        private bool _isAnimating = false;
        private Border? _currentResponseContainer;
        private MessageTextHolder? _currentMessageHolder;

        /// <summary>
        /// Holds the text for a specific message, allowing proper capture in closures.
        /// </summary>
        private class MessageTextHolder
        {
            public string Text { get; set; } = "";
        }

        public AskWindow()
        {
            InitializeComponent();
            SetupWindow();

            // Hide window when it loses focus (only in compact mode)
            Activated += AskWindow_Activated;
        }

        private void AskWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            if (args.WindowActivationState == WindowActivationState.Deactivated && !_isExpanded)
            {
                Hide();
            }
        }

        private void SetupWindow()
        {
            var hWnd = WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
            _appWindow = AppWindow.GetFromWindowId(windowId);

            // Set window title and icon
            _appWindow.Title = "Ask AI";
            _appWindow.SetIcon("Assets\\app.ico");

            _appWindow.Resize(new Windows.Graphics.SizeInt32(_compactWidth, _compactHeight));

            if (GetCursorPos(out POINT cursorPos))
            {
                int x = cursorPos.X - (_compactWidth / 2);
                int y = cursorPos.Y - 30;
                _appWindow.Move(new Windows.Graphics.PointInt32(x, y));
            }

            // Extend content into title bar for borderless look in compact mode
            _appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            
            if (_appWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.IsResizable = false;
                presenter.IsMaximizable = false;
                presenter.IsMinimizable = false;
                presenter.SetBorderAndTitleBar(false, false);
            }

            RemoveWindowBorder(hWnd);

            // Initialize animation helper
            _animationHelper = new WindowAnimationHelper(_appWindow, RootGrid, DispatcherQueue);
        }

        private static void RemoveWindowBorder(IntPtr hWnd)
        {
            int style = GetWindowLong(hWnd, GWL_STYLE);
            style &= ~WS_CAPTION;
            style &= ~WS_THICKFRAME;
            style &= ~WS_BORDER;
            SetWindowLong(hWnd, GWL_STYLE, style);
            SetWindowPos(hWnd, IntPtr.Zero, 0, 0, 0, 0,
                SWP_FRAMECHANGED | SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER);
        }

        private static void RestoreWindowBorder(IntPtr hWnd)
        {
            int style = GetWindowLong(hWnd, GWL_STYLE);
            style |= WS_CAPTION;
            style |= WS_BORDER;
            SetWindowLong(hWnd, GWL_STYLE, style);
            SetWindowPos(hWnd, IntPtr.Zero, 0, 0, 0, 0,
                SWP_FRAMECHANGED | SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER);
        }

        private void ShowWindowTitleBar(bool show)
        {
            var hWnd = WindowNative.GetWindowHandle(this);
            
            if (show)
            {
                // Restore Win32 window styles first
                RestoreWindowBorder(hWnd);
                
                // Don't extend content into title bar - show system title bar
                _appWindow.TitleBar.ExtendsContentIntoTitleBar = false;
                
                if (_appWindow.Presenter is OverlappedPresenter presenter)
                {
                    // Show Windows title bar and border
                    presenter.SetBorderAndTitleBar(true, true);
                    presenter.IsResizable = false;
                    presenter.IsMaximizable = false;
                    presenter.IsMinimizable = true;
                }
            }
            else
            {
                // Extend content into title bar area (no system title bar)
                _appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
                
                if (_appWindow.Presenter is OverlappedPresenter presenter)
                {
                    // Hide Windows title bar and border
                    presenter.SetBorderAndTitleBar(false, false);
                    presenter.IsResizable = false;
                    presenter.IsMaximizable = false;
                    presenter.IsMinimizable = false;
                }
                
                // Also remove window border styles for clean look
                RemoveWindowBorder(hWnd);
            }
        }

        public void PositionAtCursor()
        {
            if (GetCursorPos(out POINT cursorPos))
            {
                var width = _isExpanded ? _expandedWidth : _compactWidth;
                int x = cursorPos.X - (width / 2);
                int y = cursorPos.Y - 30;
                _appWindow.Move(new Windows.Graphics.PointInt32(x, y));
            }
        }

        private void CenterOnScreen()
        {
            int screenWidth = GetSystemMetrics(SM_CXSCREEN);
            int screenHeight = GetSystemMetrics(SM_CYSCREEN);
            var width = _isExpanded ? _expandedWidth : _compactWidth;
            var height = _isExpanded ? _expandedHeight : _compactHeight;
            int x = (screenWidth - width) / 2;
            int y = (screenHeight - height) / 2;
            _appWindow.Move(new Windows.Graphics.PointInt32(x, y));
        }

        public void FocusInput()
        {
            InputBox.Focus(FocusState.Programmatic);
        }

        public void ClearInput()
        {
            InputBox.Text = string.Empty;
        }

        private async void ExpandWindow()
        {
            if (!_isExpanded && !_isAnimating)
            {
                _isAnimating = true;
                _isExpanded = true;
                
                // Hide custom header first since Windows title bar will be visible
                AnimationService.FadeOut(HeaderRow, TimeSpan.FromMilliseconds(100), withScale: false, onComplete: () =>
                {
                    DispatcherQueue.TryEnqueue(() => HeaderRow.Visibility = Visibility.Collapsed);
                });
                
                // Show Windows title bar when expanded
                ShowWindowTitleBar(true);
                
                // Calculate center position for expanded window
                int screenWidth = GetSystemMetrics(SM_CXSCREEN);
                int screenHeight = GetSystemMetrics(SM_CYSCREEN);
                int targetX = (screenWidth - _expandedWidth) / 2;
                int targetY = (screenHeight - _expandedHeight) / 2;
                
                // Animated resize and move to center
                if (_animationHelper != null)
                {
                    await _animationHelper.AnimateResizeAndMoveAsync(_expandedWidth, _expandedHeight, targetX, targetY, fadeContent: true);
                }
                else
                {
                    _appWindow.Resize(new Windows.Graphics.SizeInt32(_expandedWidth, _expandedHeight));
                    CenterOnScreen();
                }
                
                // Show response area with animation
                ResponseScrollViewer.Visibility = Visibility.Visible;
                AnimationService.FadeIn(ResponseScrollViewer, TimeSpan.FromMilliseconds(200), withScale: true);
                
                NewChatButton.Visibility = Visibility.Visible;
                AnimationService.FadeIn(NewChatButton, TimeSpan.FromMilliseconds(150));
                
                _isAnimating = false;
            }
        }

        private async void CollapseWindow()
        {
            if (_isAnimating) return;
            _isAnimating = true;
            _isExpanded = false;
            
            // Fade out response area
            AnimationService.FadeOut(ResponseScrollViewer, TimeSpan.FromMilliseconds(100));
            AnimationService.FadeOut(NewChatButton, TimeSpan.FromMilliseconds(100));
            
            await Task.Delay(100);
            
            // Hide Windows title bar when compact
            ShowWindowTitleBar(false);
            
            // Animated resize
            if (_animationHelper != null)
            {
                await _animationHelper.AnimateResizeAsync(_compactWidth, _compactHeight, fadeContent: true);
            }
            else
            {
                _appWindow.Resize(new Windows.Graphics.SizeInt32(_compactWidth, _compactHeight));
            }
            
            ResponseScrollViewer.Visibility = Visibility.Collapsed;
            NewChatButton.Visibility = Visibility.Collapsed;
            MessagesPanel.Children.Clear();
            _conversationManager.Clear();
            
            // Show custom header when compact with animation
            HeaderRow.Visibility = Visibility.Visible;
            AnimationService.FadeIn(HeaderRow, TimeSpan.FromMilliseconds(150));
            
            _isAnimating = false;
        }

        private void Grid_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Escape)
            {
                _cancellationTokenSource?.Cancel();
                Hide();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            _cancellationTokenSource?.Cancel();
            Hide();
        }

        private void NewChatButton_Click(object sender, RoutedEventArgs e)
        {
            CollapseWindow();
            ClearInput();
            FocusInput();
        }

        private void InputBox_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
        {
            // Ctrl+Enter to send (Enter alone creates new line for markdown)
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                // Check all possible Control key states using Win32 API
                bool ctrlPressed = (GetAsyncKeyState(VK_CONTROL) & 0x8000) != 0 ||
                                   (GetAsyncKeyState(VK_LCONTROL) & 0x8000) != 0 ||
                                   (GetAsyncKeyState(VK_RCONTROL) & 0x8000) != 0;
                
                if (ctrlPressed)
                {
                    e.Handled = true;
                    _ = SubmitQueryAsync();
                }
            }
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            _ = SubmitQueryAsync();
        }

        private async Task SubmitQueryAsync()
        {
            var text = InputBox.Text?.Trim();
            if (string.IsNullOrEmpty(text)) return;

            // Clear input immediately
            InputBox.Text = string.Empty;

            // Expand window for response
            ExpandWindow();

            // Add user message to UI
            AddMessageToUI("You", text, isUser: true);

            // Add to conversation history
            _conversationManager.AddUserMessage(text);

            // Show loading
            LoadingPanel.Visibility = Visibility.Visible;
            SendButton.IsEnabled = false;

            // Create response container for markdown with its own text holder
            _currentMessageHolder = new MessageTextHolder();
            _currentResponseContainer = CreateResponseContainer();
            var holder = _currentMessageHolder; // Capture for closure
            MessagesPanel.Children.Add(CreateAIMessageContainer(_currentResponseContainer, () => holder.Text));

            // Stream response
            await StreamResponseAsync();

            // Hide loading
            LoadingPanel.Visibility = Visibility.Collapsed;
            SendButton.IsEnabled = true;

            // Scroll to bottom
            ScrollToBottom();
        }

        private void ScrollToBottom()
        {
            // Update layout first to ensure ScrollableHeight is correct
            ResponseScrollViewer.UpdateLayout();
            ResponseScrollViewer.ChangeView(null, ResponseScrollViewer.ScrollableHeight, null, true);
        }

        private async Task StreamResponseAsync()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            var responseBuilder = new StringBuilder();
            var lastUpdateTime = DateTime.Now;
            const int updateIntervalMs = 100; // Throttle markdown rendering
            var messageHolder = _currentMessageHolder!; // Capture for this message

            try
            {
                var config = await _settingsService.LoadAsync();
                var provider = AIProviderFactory.CreateProvider(config);

                var systemPrompt = "You are a helpful AI assistant. Be concise and helpful. Use markdown formatting when appropriate.";

                await foreach (var chunk in provider.StreamCompletionAsync(
                    _conversationManager.GetMessagesForApi(),
                    systemPrompt,
                    _cancellationTokenSource.Token))
                {
                    responseBuilder.Append(chunk);
                    messageHolder.Text = responseBuilder.ToString();
                    
                    // Throttle UI updates for better performance
                    var now = DateTime.Now;
                    if ((now - lastUpdateTime).TotalMilliseconds >= updateIntervalMs)
                    {
                        lastUpdateTime = now;
                        var textToRender = messageHolder.Text;
                        DispatcherQueue.TryEnqueue(() =>
                        {
                            UpdateResponseMarkdown(textToRender);
                            ScrollToBottom();
                        });
                    }
                }

                // Add complete response to conversation
                _conversationManager.AddAssistantMessage(responseBuilder.ToString());
            }
            catch (OperationCanceledException)
            {
                // User cancelled
                responseBuilder.Append("\n\n[Cancelled]");
                messageHolder.Text = responseBuilder.ToString();
            }
            catch (Exception ex)
            {
                responseBuilder.Append($"\n\nError: {ex.Message}");
                messageHolder.Text = responseBuilder.ToString();
            }

            // Final UI update with complete markdown
            var finalText = messageHolder.Text;
            DispatcherQueue.TryEnqueue(() =>
            {
                UpdateResponseMarkdown(finalText);
                ScrollToBottom();
            });
        }

        private void UpdateResponseMarkdown(string markdown)
        {
            if (_currentResponseContainer == null) return;

            try
            {
                var rendered = MarkdownRenderer.Render(markdown);
                _currentResponseContainer.Child = rendered;
            }
            catch
            {
                // Fallback to plain text if rendering fails
                _currentResponseContainer.Child = new TextBlock
                {
                    Text = markdown,
                    TextWrapping = TextWrapping.Wrap,
                    IsTextSelectionEnabled = true
                };
            }
        }

        private void AddMessageToUI(string sender, string content, bool isUser)
        {
            UIElement contentElement;
            try
            {
                // Render markdown for both user and AI messages
                contentElement = MarkdownRenderer.Render(content);
            }
            catch
            {
                // Fallback to plain text if rendering fails
                contentElement = new TextBlock
                {
                    Text = content,
                    TextWrapping = TextWrapping.Wrap,
                    IsTextSelectionEnabled = true
                };
            }
            MessagesPanel.Children.Add(CreateMessageContainer(sender, contentElement, isUser));
        }

        private Border CreateMessageContainer(string sender, UIElement content, bool isUser)
        {
            var headerText = new TextBlock
            {
                Text = sender,
                FontWeight = FontWeights.SemiBold,
                FontSize = 12,
                Opacity = 0.7,
                Margin = new Thickness(0, 0, 0, 4)
            };

            var stack = new StackPanel();
            stack.Children.Add(headerText);
            stack.Children.Add(content);

            var border = new Border
            {
                Child = stack,
                Background = isUser 
                    ? (Brush)Application.Current.Resources["CardBackgroundFillColorSecondaryBrush"]
                    : (Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"],
                BorderBrush = (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 8, 12, 8),
                HorizontalAlignment = isUser ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                MaxWidth = 380
            };

            // Animate message entrance
            border.Loaded += (s, e) =>
            {
                var direction = isUser ? SlideDirection.Right : SlideDirection.Left;
                AnimationService.SlideIn(border, direction, TimeSpan.FromMilliseconds(200));
            };

            return border;
        }

        private Border CreateResponseContainer()
        {
            return new Border();
        }

        private Border CreateAIMessageContainer(Border contentContainer, Func<string> getResponseText)
        {
            // Header row with AI label and copy button
            var headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var headerText = new TextBlock
            {
                Text = "AI",
                FontWeight = FontWeights.SemiBold,
                FontSize = 12,
                Opacity = 0.7,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(headerText, 0);
            headerGrid.Children.Add(headerText);

            // Copy button
            var copyButton = new Button
            {
                Content = new FontIcon 
                { 
                    Glyph = "\uE8C8", // Copy icon
                    FontSize = 12 
                },
                Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
                BorderThickness = new Thickness(0),
                Padding = new Thickness(6, 4, 6, 4),
                CornerRadius = new CornerRadius(4),
                Opacity = 0.6,
                VerticalAlignment = VerticalAlignment.Center
            };
            ToolTipService.SetToolTip(copyButton, LocalizationService.Get("ask_copy_clipboard"));
            
            // Copy button copies this specific message
            copyButton.Click += async (s, e) =>
            {
                var text = getResponseText();
                if (string.IsNullOrEmpty(text)) return;

                var dataPackage = new DataPackage();
                dataPackage.SetText(text);
                Clipboard.SetContent(dataPackage);

                // Visual feedback - change icon temporarily
                var icon = (FontIcon)copyButton.Content;
                icon.Glyph = "\uE73E"; // Checkmark
                copyButton.Opacity = 1.0;
                
                await Task.Delay(1500);
                
                icon.Glyph = "\uE8C8"; // Back to copy icon
                copyButton.Opacity = 0.6;
            };
            
            Grid.SetColumn(copyButton, 1);
            headerGrid.Children.Add(copyButton);

            headerGrid.Margin = new Thickness(0, 0, 0, 4);

            var stack = new StackPanel();
            stack.Children.Add(headerGrid);
            stack.Children.Add(contentContainer);

            var border = new Border
            {
                Child = stack,
                Background = (Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"],
                BorderBrush = (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 8, 12, 8),
                HorizontalAlignment = HorizontalAlignment.Left,
                MaxWidth = 400
            };

            // Animate AI message entrance
            border.Loaded += (s, e) =>
            {
                AnimationService.SlideIn(border, SlideDirection.Left, TimeSpan.FromMilliseconds(200));
            };

            return border;
        }

        public void Hide()
        {
            _ = HideWithAnimationAsync();
        }

        private async Task HideWithAnimationAsync()
        {
            if (_animationHelper != null)
            {
                await _animationHelper.AnimateWindowExitAsync();
            }
            _appWindow.Hide();
        }

        public void Show()
        {
            PositionAtCursor();
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            SetWindowPos(hWnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
            _appWindow.Show();
            Activate();
            FocusInput();

            // Animate entrance
            _animationHelper?.AnimateWindowEntrance();
        }

        public void ShowCentered()
        {
            CenterOnScreen();
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            SetWindowPos(hWnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
            _appWindow.Show();
            Activate();
            FocusInput();

            // Animate entrance
            _animationHelper?.AnimateWindowEntrance();
        }

        /// <summary>
        /// Updates all UI text to current language.
        /// </summary>
        public void UpdateLocalization()
        {
            AskTitleText.Text = LocalizationService.Get("ask_title");
            InputBox.PlaceholderText = LocalizationService.Get("ask_placeholder");
            ThinkingText.Text = LocalizationService.Get("ask_thinking");
            
            // Update tooltips
            ToolTipService.SetToolTip(NewChatButton, LocalizationService.Get("ask_new_chat"));
            ToolTipService.SetToolTip(CloseButton, LocalizationService.Get("ask_close"));
            ToolTipService.SetToolTip(SendButton, LocalizationService.Get("ask_send"));
        }

        /// <summary>
        /// Shows the window with an initial response (for button actions that open in window).
        /// </summary>
        public async Task ShowWithResponseAsync(string userMessage, string systemPrompt)
        {
            Show();
            ExpandWindow();

            // Add user message
            AddMessageToUI("You", userMessage, isUser: true);
            _conversationManager.AddUserMessage(userMessage);

            // Show loading
            LoadingPanel.Visibility = Visibility.Visible;
            SendButton.IsEnabled = false;

            // Create response container for markdown with its own text holder
            _currentMessageHolder = new MessageTextHolder();
            _currentResponseContainer = CreateResponseContainer();
            var holder = _currentMessageHolder; // Capture for closure
            MessagesPanel.Children.Add(CreateAIMessageContainer(_currentResponseContainer, () => holder.Text));

            // Stream with custom system prompt
            _cancellationTokenSource = new CancellationTokenSource();
            var responseBuilder = new StringBuilder();
            var lastUpdateTime = DateTime.Now;
            const int updateIntervalMs = 100;

            try
            {
                var config = await _settingsService.LoadAsync();
                var provider = AIProviderFactory.CreateProvider(config);

                await foreach (var chunk in provider.StreamCompletionAsync(
                    _conversationManager.GetMessagesForApi(),
                    systemPrompt,
                    _cancellationTokenSource.Token))
                {
                    responseBuilder.Append(chunk);
                    holder.Text = responseBuilder.ToString();
                    
                    var now = DateTime.Now;
                    if ((now - lastUpdateTime).TotalMilliseconds >= updateIntervalMs)
                    {
                        lastUpdateTime = now;
                        var textToRender = holder.Text;
                        DispatcherQueue.TryEnqueue(() =>
                        {
                            UpdateResponseMarkdown(textToRender);
                            ScrollToBottom();
                        });
                    }
                }

                _conversationManager.AddAssistantMessage(responseBuilder.ToString());
            }
            catch (OperationCanceledException)
            {
                responseBuilder.Append("\n\n[Cancelled]");
                holder.Text = responseBuilder.ToString();
            }
            catch (Exception ex)
            {
                responseBuilder.Append($"\n\nError: {ex.Message}");
                holder.Text = responseBuilder.ToString();
            }

            var finalText = holder.Text;
            DispatcherQueue.TryEnqueue(() =>
            {
                UpdateResponseMarkdown(finalText);
                LoadingPanel.Visibility = Visibility.Collapsed;
                SendButton.IsEnabled = true;
                ScrollToBottom();
            });
        }
    }
}
