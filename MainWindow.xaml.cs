using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
    public sealed partial class MainWindow : Window
    {
        // P/Invoke for cursor position and window styling
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

        private AppWindow _appWindow = null!;
        private WindowAnimationHelper? _animationHelper;
        private readonly int _normalWidth = 300;
        private readonly int _normalHeight = 540;
        private readonly int _editWidth = 340;
        private readonly int _editHeight = 620;
        private readonly ConfigurationService _configService;
        private OptionsConfig _config = null!;
        private bool _isEditMode = false;
        private bool _isAnimating = false;
        private int _draggedIndex = -1;
        private string? _selectedText;

        public ObservableCollection<UIElement> Buttons { get; } = new();

        public MainWindow()
        {
            InitializeComponent();
            _configService = new ConfigurationService();
            SetupFixedWindow();
            _ = LoadButtonsAsync();

            // Hide window when it loses focus (click outside)
            Activated += MainWindow_Activated;
        }

        private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            if (args.WindowActivationState == WindowActivationState.Deactivated)
            {
                // Don't hide if in edit mode (user might be using dialogs)
                if (!_isEditMode)
                {
                    HideWindow();
                }
            }
        }

        private async Task LoadButtonsAsync()
        {
            _config = await _configService.LoadAsync();
            RefreshButtons();
        }

        private void RefreshButtons()
        {
            // Skip if config hasn't been loaded yet
            if (_config == null)
            {
                return;
            }

            Buttons.Clear();

            var elementsToAnimate = new List<UIElement>();

            if (_isEditMode)
            {
                // Edit mode: single column for more space
                for (int i = 0; i < _config.Buttons.Count; i++)
                {
                    var element = CreateEditModeButton(_config.Buttons[i], i);
                    element.Margin = new Thickness(0, 0, 0, 6);
                    Buttons.Add(element);
                    elementsToAnimate.Add(element);
                }
            }
            else
            {
                // Normal mode: 2 columns per row
                for (int i = 0; i < _config.Buttons.Count; i += 2)
                {
                    var rowGrid = new Grid
                    {
                        Margin = new Thickness(0, 0, 0, 8)
                    };
                    rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                    // First button in row
                    var button1 = CreateNormalButton(_config.Buttons[i], i);
                    Grid.SetColumn(button1, 0);
                    button1.Margin = new Thickness(0, 0, 4, 0);
                    rowGrid.Children.Add(button1);

                    // Second button in row (if exists)
                    if (i + 1 < _config.Buttons.Count)
                    {
                        var button2 = CreateNormalButton(_config.Buttons[i + 1], i + 1);
                        Grid.SetColumn(button2, 1);
                        button2.Margin = new Thickness(4, 0, 0, 0);
                        rowGrid.Children.Add(button2);
                    }

                    Buttons.Add(rowGrid);
                    elementsToAnimate.Add(rowGrid);
                }
            }

            // Animate staggered entrance for buttons
            if (elementsToAnimate.Count > 0)
            {
                // Small delay to let layout complete
                DispatcherQueue.TryEnqueue(() =>
                {
                    AnimationService.StaggeredEntrance(elementsToAnimate.ToArray(), 
                        staggerDelay: TimeSpan.FromMilliseconds(40),
                        duration: TimeSpan.FromMilliseconds(200));
                });
            }
        }

        private FrameworkElement CreateButtonElement(ButtonConfig config, int index)
        {
            if (_isEditMode)
            {
                return CreateEditModeButton(config, index);
            }
            else
            {
                return CreateNormalButton(config, index);
            }
        }

        private Button CreateNormalButton(ButtonConfig config, int index)
        {
            var button = new Button
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Tag = index,
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8, 8, 8, 8)
            };

            // Create content with icon and text
            var stackPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Center,
                Spacing = 4
            };

            // Try to load icon if path is specified
            if (!string.IsNullOrEmpty(config.Icon))
            {
                try
                {
                    var iconPath = System.IO.Path.Combine(AppContext.BaseDirectory, config.Icon);
                    if (System.IO.File.Exists(iconPath))
                    {
                        var image = new Image
                        {
                            Width = 20,
                            Height = 20,
                            HorizontalAlignment = HorizontalAlignment.Center
                        };
                        image.Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(iconPath));
                        stackPanel.Children.Add(image);
                    }
                }
                catch
                {
                    // Ignore icon loading errors
                }
            }

            // Add text label
            var textBlock = new TextBlock
            {
                Text = LocalizationService.GetButtonName(config.Name),
                TextTrimming = TextTrimming.CharacterEllipsis,
                HorizontalAlignment = HorizontalAlignment.Center,
                FontSize = 11
            };
            stackPanel.Children.Add(textBlock);

            button.Content = stackPanel;
            button.Click += ActionButton_Click;

            // Add hover and press animations
            button.Loaded += (s, e) =>
            {
                AnimationService.SetupHoverAnimation(button, 1.03f);
                AnimationService.SetupPressAnimation(button, 0.97f);
            };

            return button;
        }

        private Grid CreateEditModeButton(ButtonConfig config, int index)
        {
            var grid = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Drag handle
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Name
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Edit
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Delete

            // Drag handle
            var dragHandle = new FontIcon
            {
                Glyph = "\uE700",
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 0, 10, 0),
                Opacity = 0.5
            };
            Grid.SetColumn(dragHandle, 0);
            grid.Children.Add(dragHandle);

            // Button name - use localized name
            var nameBlock = new TextBlock
            {
                Text = LocalizationService.GetButtonName(config.Name),
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(0, 0, 8, 0),
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            };
            Grid.SetColumn(nameBlock, 1);
            grid.Children.Add(nameBlock);

            // Edit button
            var editBtn = new Button
            {
                Tag = index,
                Padding = new Thickness(8, 6, 8, 6),
                Margin = new Thickness(4, 0, 0, 0),
                CornerRadius = new CornerRadius(4),
                Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent)
            };
            editBtn.Content = new FontIcon { Glyph = "\uE70F", FontSize = 12 };
            editBtn.Click += EditItemButton_Click;
            Grid.SetColumn(editBtn, 2);
            grid.Children.Add(editBtn);

            // Delete button
            var deleteBtn = new Button
            {
                Tag = index,
                Padding = new Thickness(8, 6, 8, 6),
                Margin = new Thickness(4, 0, 0, 0),
                CornerRadius = new CornerRadius(4),
                Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent)
            };
            deleteBtn.Content = new FontIcon { Glyph = "\uE74D", FontSize = 12 };
            deleteBtn.Click += DeleteItemButton_Click;
            Grid.SetColumn(deleteBtn, 3);
            grid.Children.Add(deleteBtn);

            // Wrap in a border for visual grouping
            var border = new Border
            {
                Child = grid,
                BorderBrush = (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10, 8, 10, 8),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Background = (Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"],
                Tag = index,
                CanDrag = true,
                AllowDrop = true
            };

            // Drag and drop events
            border.DragStarting += Border_DragStarting;
            border.DragOver += Border_DragOver;
            border.Drop += Border_Drop;

            var wrapper = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Tag = index,
                AllowDrop = true
            };
            wrapper.Children.Add(border);
            wrapper.DragOver += Border_DragOver;
            wrapper.Drop += Border_Drop;

            // Add hover animation to the border
            border.Loaded += (s, e) =>
            {
                AnimationService.SetupHoverAnimation(border, 1.01f);
            };
            
            return wrapper;
        }

        private void Border_DragStarting(UIElement sender, DragStartingEventArgs args)
        {
            if (sender is Border border && border.Tag is int index)
            {
                _draggedIndex = index;
                args.Data.SetText(index.ToString());
                args.Data.RequestedOperation = DataPackageOperation.Move;
            }
        }

        private void Border_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Move;
            e.DragUIOverride.Caption = LocalizationService.Get("main_move_here");
            e.DragUIOverride.IsCaptionVisible = true;
            e.DragUIOverride.IsGlyphVisible = false;
        }

        private async void Border_Drop(object sender, DragEventArgs e)
        {
            int targetIndex = -1;

            if (sender is Border border && border.Tag is int idx)
            {
                targetIndex = idx;
            }
            else if (sender is Grid grid && grid.Tag is int gridIdx)
            {
                targetIndex = gridIdx;
            }

            if (_draggedIndex >= 0 && targetIndex >= 0 && _draggedIndex != targetIndex)
            {
                // Reorder the buttons
                var item = _config.Buttons[_draggedIndex];
                _config.Buttons.RemoveAt(_draggedIndex);
                
                // Adjust target index if dragging from before target
                if (_draggedIndex < targetIndex)
                {
                    targetIndex--;
                }
                
                _config.Buttons.Insert(targetIndex, item);
                
                await _configService.SaveAsync(_config);
                RefreshButtons();
            }

            _draggedIndex = -1;
        }

        private async void ActionButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int index)
            {
                var buttonConfig = _config.Buttons[index];
                
                // Get the text to process
                var textToProcess = !string.IsNullOrEmpty(_selectedText) 
                    ? _selectedText 
                    : InputBox.Text;

                if (string.IsNullOrWhiteSpace(textToProcess))
                {
                    // No text to process
                    return;
                }

                // Build the user message with prefix
                var userMessage = buttonConfig.Prefix + textToProcess;

                // Hide this window
                HideWindow();

                if (buttonConfig.OpenInWindow)
                {
                    // Open in Ask AI window with response
                    await ShowResponseInWindowAsync(userMessage, buttonConfig.Instruction);
                }
                else
                {
                    // Replace selected text with AI response
                    await ReplaceTextWithAIResponseAsync(userMessage, buttonConfig.Instruction);
                }
            }
        }

        private async Task ShowResponseInWindowAsync(string userMessage, string systemPrompt)
        {
            // Get the AskWindow from App
            if (App.Instance?.AskWindow is AskWindow askWindow)
            {
                await askWindow.ShowWithResponseAsync(userMessage, systemPrompt);
            }
        }

        private async Task ReplaceTextWithAIResponseAsync(string userMessage, string systemPrompt)
        {
            try
            {
                var settingsService = new SettingsService();
                var config = await settingsService.LoadAsync();
                var provider = AIProviderFactory.CreateProvider(config);

                var messages = new List<ChatMessage> { ChatMessage.User(userMessage) };
                var responseBuilder = new StringBuilder();

                await foreach (var chunk in provider.StreamCompletionAsync(messages, systemPrompt))
                {
                    responseBuilder.Append(chunk);
                }

                var response = responseBuilder.ToString().Trim();

                // Check for error response
                if (!response.Contains("ERROR_TEXT_INCOMPATIBLE_WITH_REQUEST"))
                {
                    // Replace the selected text with AI response
                    await TextReplacementService.ReplaceSelectedTextAsync(response);
                }
            }
            catch (Exception ex)
            {
                // Show error in a simple dialog - but we're hidden, so just log for now
                System.Diagnostics.Debug.WriteLine($"AI Error: {ex.Message}");
            }
        }

        private async void EditItemButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int index)
            {
                var config = _config.Buttons[index];
                await ShowEditDialogAsync(config, index);
            }
        }

        private async void DeleteItemButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int index)
            {
                // Ensure XamlRoot is valid before showing dialog
                if (Content?.XamlRoot == null)
                {
                    return;
                }

                try
                {
                    var dialog = new ContentDialog
                    {
                        Title = "Delete Button",
                        Content = $"Are you sure you want to delete '{_config.Buttons[index].Name}'?",
                        PrimaryButtonText = "Delete",
                        CloseButtonText = "Cancel",
                        XamlRoot = Content.XamlRoot
                    };

                    var result = await dialog.ShowAsync();
                    if (result == ContentDialogResult.Primary)
                    {
                        _config.Buttons.RemoveAt(index);
                        await _configService.SaveAsync(_config);
                        RefreshButtons();
                    }
                }
                catch (System.Runtime.InteropServices.COMException)
                {
                    // Window handle became invalid - ignore the dialog
                    System.Diagnostics.Debug.WriteLine("DeleteItemButton_Click: Invalid window handle, dialog skipped");
                }
            }
        }

        private async Task ShowEditDialogAsync(ButtonConfig config, int index)
        {
            // Ensure XamlRoot is valid before showing dialog
            if (Content?.XamlRoot == null)
            {
                return;
            }

            var nameBox = new TextBox
            {
                Header = "Button Name",
                Text = config.Name,
                Margin = new Thickness(0, 0, 0, 12)
            };

            var prefixBox = new TextBox
            {
                Header = "Prefix",
                Text = config.Prefix,
                Margin = new Thickness(0, 0, 0, 12),
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                Height = 80,
                VerticalContentAlignment = VerticalAlignment.Top
            };

            var instructionLabel = new TextBlock
            {
                Text = "Instruction",
                Margin = new Thickness(0, 0, 0, 4),
                FontSize = 12
            };

            var instructionBox = new TextBox
            {
                Text = config.Instruction,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                Height = 250,
                VerticalContentAlignment = VerticalAlignment.Top
            };

            var openInWindowCheck = new CheckBox
            {
                Content = "Open in Window",
                IsChecked = config.OpenInWindow,
                Margin = new Thickness(0, 12, 0, 0)
            };

            var panel = new StackPanel { Spacing = 4 };
            panel.Children.Add(nameBox);
            panel.Children.Add(prefixBox);
            panel.Children.Add(instructionLabel);
            panel.Children.Add(instructionBox);
            panel.Children.Add(openInWindowCheck);
            
            // Wrap in ScrollViewer for better visibility of long content
            var scrollViewer = new ScrollViewer
            {
                Content = panel,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                MaxHeight = 450,
                MinWidth = 400
            };

            try
            {
                var dialog = new ContentDialog
                {
                    Title = index >= 0 ? "Edit Button" : "Add New Button",
                    Content = scrollViewer,
                    PrimaryButtonText = "Save",
                    CloseButtonText = "Cancel",
                    XamlRoot = Content.XamlRoot
                };

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    config.Name = nameBox.Text;
                    config.Prefix = prefixBox.Text;
                    config.Instruction = instructionBox.Text;
                    config.OpenInWindow = openInWindowCheck.IsChecked ?? false;

                    if (index < 0)
                    {
                        // Adding new button
                        _config.Buttons.Add(config);
                    }

                    await _configService.SaveAsync(_config);
                    RefreshButtons();
                }
            }
            catch (System.Runtime.InteropServices.COMException)
            {
                // Window handle became invalid - ignore the dialog
                System.Diagnostics.Debug.WriteLine("ShowEditDialogAsync: Invalid window handle, dialog skipped");
            }
        }

        private async void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var newConfig = new ButtonConfig
            {
                Name = "New Button",
                Prefix = "",
                Instruction = "",
                Icon = "",
                OpenInWindow = false
            };
            await ShowEditDialogAsync(newConfig, -1);
        }

        private async void EditModeButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isAnimating) return;
            _isAnimating = true;

            _isEditMode = !_isEditMode;
            
            // Animate visibility change for Add button
            if (_isEditMode)
            {
                AddButton.Visibility = Visibility.Visible;
                AnimationService.PrepareForAnimation(AddButton);
            }
            
            // Animated resize for edit mode
            var width = _isEditMode ? _editWidth : _normalWidth;
            var height = _isEditMode ? _editHeight : _normalHeight;
            
            if (_animationHelper != null)
            {
                await _animationHelper.AnimateResizeAsync(width, height, fadeContent: true);
            }
            else
            {
                _appWindow.Resize(new Windows.Graphics.SizeInt32(width, height));
            }
            
            RefreshButtons();

            // Animate Add button entrance after resize
            if (_isEditMode)
            {
                AnimationService.SlideIn(AddButton, SlideDirection.Up);
            }
            else
            {
                AddButton.Visibility = Visibility.Collapsed;
            }

            _isAnimating = false;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            HideWindow();
        }

        private void Grid_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Escape)
            {
                HideWindow();
            }
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            var description = InputBox.Text?.Trim();
            
            // Need both a description and selected text to process
            if (string.IsNullOrEmpty(description))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(_selectedText))
            {
                return;
            }

            // Build the user message with the selected text
            var userMessage = $"Selected text:\n\n{_selectedText}\n\nRequested change: {description}";

            // System instruction for custom text editing
            var systemPrompt = @"You are a text editing assistant.
The user has selected some text and described a change they want to make.
Apply the requested change to the selected text.
Output ONLY the modified text without any additional comments or explanations.
Maintain the original formatting style where appropriate.
Respond in the same language as the input text.
If the request is completely incompatible with the text (e.g., nonsensical), output ""ERROR_TEXT_INCOMPATIBLE_WITH_REQUEST"".";

            // Hide this window
            HideWindow();

            // Replace selected text with AI response
            await ReplaceTextWithAIResponseAsync(userMessage, systemPrompt);
        }

        /// <summary>
        /// Positions the window at the current cursor location.
        /// </summary>
        public void PositionAtCursor()
        {
            if (GetCursorPos(out POINT cursorPos))
            {
                var width = _isEditMode ? _editWidth : _normalWidth;
                int x = cursorPos.X - (width / 2);
                int y = cursorPos.Y - 20;
                _appWindow.Move(new Windows.Graphics.PointInt32(x, y));
            }
        }

        /// <summary>
        /// Sets the selected text from the external application.
        /// </summary>
        public void SetSelectedText(string? text)
        {
            _selectedText = text;
            // Don't put the selected text in the input field
            InputBox.Text = string.Empty;
        }

        /// <summary>
        /// Shows the window and brings it to foreground as topmost.
        /// </summary>
        public void ShowWindow()
        {
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            // Set window as topmost
            SetWindowPos(hWnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
            _appWindow.Show();
            Activate();
            InputBox.Focus(FocusState.Programmatic);

            // Animate window entrance
            _animationHelper?.AnimateWindowEntrance();
        }

        /// <summary>
        /// Hides the window without closing the application.
        /// </summary>
        public void HideWindow()
        {
            // Animate exit then hide
            _ = HideWindowWithAnimationAsync();
        }

        private async Task HideWindowWithAnimationAsync()
        {
            if (_animationHelper != null)
            {
                await _animationHelper.AnimateWindowExitAsync();
            }
            _appWindow.Hide();
        }

        private void SetupFixedWindow()
        {
            var hWnd = WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
            _appWindow = AppWindow.GetFromWindowId(windowId);

            _appWindow.Resize(new Windows.Graphics.SizeInt32(_normalWidth, _normalHeight));

            // Position window at mouse cursor
            if (GetCursorPos(out POINT cursorPos))
            {
                // Center the window on the cursor position
                int x = cursorPos.X - (_normalWidth / 2);
                int y = cursorPos.Y - 20; // Slightly below cursor for visibility
                _appWindow.Move(new Windows.Graphics.PointInt32(x, y));
            }

            if (_appWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.IsResizable = false;
                presenter.IsMaximizable = false;
                presenter.IsMinimizable = false;
                // Remove border and title bar for borderless window
                presenter.SetBorderAndTitleBar(false, false);
            }

            // Remove all window border styles via Win32
            RemoveWindowBorder(hWnd);

            _appWindow.Changed += AppWindow_Changed;

            // Initialize animation helper
            _animationHelper = new WindowAnimationHelper(_appWindow, RootGrid, DispatcherQueue);
        }

        /// <summary>
        /// Updates all UI text to current language.
        /// </summary>
        public void UpdateLocalization()
        {
            // Update placeholder text
            if (InputBox != null)
            {
                InputBox.PlaceholderText = LocalizationService.Get("main_describe_change");
            }
            
            // Update Add Button text
            if (AddButtonText != null)
            {
                AddButtonText.Text = LocalizationService.Get("main_add_button");
            }
            
            // Update tooltips
            if (EditModeButton != null)
            {
                ToolTipService.SetToolTip(EditModeButton, LocalizationService.Get("main_edit_buttons"));
            }
            if (CloseButton != null)
            {
                ToolTipService.SetToolTip(CloseButton, LocalizationService.Get("main_close"));
            }
            if (SendButton != null)
            {
                ToolTipService.SetToolTip(SendButton, LocalizationService.Get("main_send"));
            }
            
            // Refresh buttons to update their localized names
            RefreshButtons();
        }

        private static void RemoveWindowBorder(IntPtr hWnd)
        {
            // Get current window style
            int style = GetWindowLong(hWnd, GWL_STYLE);
            
            // Remove border, caption and thick frame styles
            style &= ~WS_CAPTION;
            style &= ~WS_THICKFRAME;
            style &= ~WS_BORDER;
            
            // Apply new style
            SetWindowLong(hWnd, GWL_STYLE, style);
            
            // Force window to update its frame
            SetWindowPos(hWnd, IntPtr.Zero, 0, 0, 0, 0, 
                SWP_FRAMECHANGED | SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER);
        }

        private void AppWindow_Changed(AppWindow sender, AppWindowChangedEventArgs args)
        {
            if (args.DidPresenterChange)
            {
                if (sender.Presenter is OverlappedPresenter presenter)
                {
                    if (presenter.State == OverlappedPresenterState.Maximized)
                    {
                        presenter.Restore();
                    }

                    presenter.IsResizable = false;
                    presenter.IsMaximizable = false;
                }
            }

            if (args.DidSizeChange)
            {
                var expectedWidth = _isEditMode ? _editWidth : _normalWidth;
                var expectedHeight = _isEditMode ? _editHeight : _normalHeight;
                var currentSize = sender.Size;
                if (currentSize.Width != expectedWidth || currentSize.Height != expectedHeight)
                {
                    sender.Resize(new Windows.Graphics.SizeInt32(expectedWidth, expectedHeight));
                }
            }
        }
    }
}
