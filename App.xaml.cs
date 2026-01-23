using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using H.NotifyIcon;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WritingTool.Models;
using WritingTool.Services;

namespace WritingTool
{
    /// <summary>
    /// Application with system tray support and global hotkey handling.
    /// </summary>
    public partial class App : Application
    {
        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll")]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string? lpModuleName);

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private MainWindow? _mainWindow;
        private AskWindow? _askWindow;
        private SettingsWindow? _settingsWindow;
        private AboutWindow? _aboutWindow;
        private TaskbarIcon? _trayIcon;
        private IntPtr _keyboardHook = IntPtr.Zero;
        private LowLevelKeyboardProc? _hookProc;
        private bool _isProcessingHotkey = false;

        // Configurable hotkey
        private readonly SettingsService _settingsService = new();
        private int[] _hotkeyModifiers = { ShortcutParser.VK_CONTROL };
        private int _hotkeyKey = 0x20; // Space
        private string _shortcutDisplay = "Ctrl+Space";
        private string _currentTheme = "mica";

        /// <summary>
        /// Singleton instance for accessing App from other windows.
        /// </summary>
        public static App? Instance { get; private set; }

        /// <summary>
        /// Gets the Ask AI window.
        /// </summary>
        public AskWindow? AskWindow => _askWindow;

        public App()
        {
            InitializeComponent();
            Instance = this;
        }

        protected override async void OnLaunched(LaunchActivatedEventArgs args)
        {
            // Load settings first
            await LoadSettingsAsync();

            // Create windows but don't show them yet
            _mainWindow = new MainWindow();
            _askWindow = new AskWindow();
            _settingsWindow = new SettingsWindow();
            _aboutWindow = new AboutWindow();

            // Apply theme to all windows
            ApplyThemeToAllWindows();

            // Set up tray icon
            SetupTrayIcon();

            // Apply localization to tray menu and all windows
            UpdateTrayMenuLocalization();
            NotifyLanguageChanged();

            // Set up global hotkey hook
            SetupKeyboardHook();

            // Show main window initially
            _mainWindow.Activate();
        }

        private async Task LoadSettingsAsync()
        {
            var config = await _settingsService.LoadAsync();
            ApplyShortcut(config.Shortcut);
            _currentTheme = config.Theme;
            LocalizationService.CurrentLanguage = config.Language;
        }

        private void ApplyThemeToAllWindows()
        {
            if (_mainWindow != null)
                ThemeService.ApplyTheme(_mainWindow, _currentTheme);
            if (_askWindow != null)
                ThemeService.ApplyTheme(_askWindow, _currentTheme);
            if (_settingsWindow != null)
                ThemeService.ApplyTheme(_settingsWindow, _currentTheme);
            if (_aboutWindow != null)
                ThemeService.ApplyTheme(_aboutWindow, _currentTheme);
        }

        private void ApplyShortcut(string shortcut)
        {
            var (modifiers, key) = ShortcutParser.Parse(shortcut);
            _hotkeyModifiers = modifiers;
            _hotkeyKey = key;
            _shortcutDisplay = ShortcutParser.Format(shortcut);

            // Update tray tooltip if already created
            if (_trayIcon != null)
            {
                _trayIcon.ToolTipText = $"WritingTool - {_shortcutDisplay} to activate";
            }
        }

        /// <summary>
        /// Reloads settings from disk. Called by SettingsWindow after saving.
        /// </summary>
        public async Task ReloadSettingsAsync()
        {
            await LoadSettingsAsync();
            ApplyThemeToAllWindows();
            UpdateTrayMenuLocalization();
            NotifyLanguageChanged();
        }

        private void UpdateTrayMenuLocalization()
        {
            if (_trayIcon?.ContextFlyout is MenuFlyout menu && menu.Items.Count >= 5)
            {
                if (menu.Items[0] is MenuFlyoutItem showItem)
                    showItem.Text = LocalizationService.Get("tray_show");
                if (menu.Items[1] is MenuFlyoutItem settingsItem)
                    settingsItem.Text = LocalizationService.Get("tray_settings");
                if (menu.Items[2] is MenuFlyoutItem aboutItem)
                    aboutItem.Text = LocalizationService.Get("tray_about");
                if (menu.Items[4] is MenuFlyoutItem exitItem)
                    exitItem.Text = LocalizationService.Get("tray_exit");
            }
        }

        private void NotifyLanguageChanged()
        {
            _mainWindow?.UpdateLocalization();
            _askWindow?.UpdateLocalization();
            _settingsWindow?.UpdateLocalization();
            _aboutWindow?.UpdateLocalization();
        }

        private void SetupTrayIcon()
        {
            _trayIcon = new TaskbarIcon
            {
                ToolTipText = $"WritingTool - {_shortcutDisplay} to activate"
            };
            
            // Set icon from file
            try
            {
                var iconPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico");
                if (System.IO.File.Exists(iconPath))
                {
                    _trayIcon.Icon = new System.Drawing.Icon(iconPath);
                }
            }
            catch
            {
                // Ignore icon loading errors
            }

            // Create context menu
            var contextMenu = new MenuFlyout();
            
            var showItem = new MenuFlyoutItem { Text = "Show" };
            showItem.Click += (s, e) => ShowAskWindowCentered();
            contextMenu.Items.Add(showItem);

            var settingsItem = new MenuFlyoutItem { Text = "Settings" };
            settingsItem.Click += (s, e) => ShowSettingsWindow();
            contextMenu.Items.Add(settingsItem);

            var aboutItem = new MenuFlyoutItem { Text = "About" };
            aboutItem.Click += (s, e) => ShowAboutWindow();
            contextMenu.Items.Add(aboutItem);

            contextMenu.Items.Add(new MenuFlyoutSeparator());

            var exitItem = new MenuFlyoutItem { Text = "Exit" };
            exitItem.Click += (s, e) => ExitApplication();
            contextMenu.Items.Add(exitItem);

            _trayIcon.ContextMenuMode = ContextMenuMode.SecondWindow;
            _trayIcon.ContextFlyout = contextMenu;

            // Click to show
            _trayIcon.LeftClickCommand = new RelayCommand(ShowAskWindowCentered);

            _trayIcon.ForceCreate();
        }

        private void SetupKeyboardHook()
        {
            _hookProc = HookCallback;
            using var curProcess = System.Diagnostics.Process.GetCurrentProcess();
            using var curModule = curProcess.MainModule;
            _keyboardHook = SetWindowsHookEx(WH_KEYBOARD_LL, _hookProc, 
                GetModuleHandle(curModule?.ModuleName), 0);
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN && !_isProcessingHotkey)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                
                // Check for configured hotkey
                if (vkCode == _hotkeyKey && AreModifiersPressed())
                {
                    _isProcessingHotkey = true;
                    
                    // Use dispatcher to handle on UI thread
                    _mainWindow?.DispatcherQueue.TryEnqueue(async () =>
                    {
                        await HandleHotkeyAsync();
                        _isProcessingHotkey = false;
                    });
                }
            }
            return CallNextHookEx(_keyboardHook, nCode, wParam, lParam);
        }

        private bool AreModifiersPressed()
        {
            foreach (var modifier in _hotkeyModifiers)
            {
                if ((GetAsyncKeyState(modifier) & 0x8000) == 0)
                {
                    return false;
                }
            }
            return true;
        }

        private async Task HandleHotkeyAsync()
        {
            // Small delay to let Ctrl+Space complete
            await Task.Delay(50);

            // Try to get selected text
            string? selectedText = await ClipboardService.GetSelectedTextAsync();

            if (!string.IsNullOrEmpty(selectedText))
            {
                // Text is selected - show main WritingTool window
                ShowMainWindow(selectedText);
            }
            else
            {
                // No text selected - show Ask AI window
                ShowAskWindow();
            }
        }

        private void ShowMainWindow()
        {
            ShowMainWindow(null);
        }

        private void ShowMainWindow(string? selectedText)
        {
            if (_mainWindow != null)
            {
                _mainWindow.PositionAtCursor();
                _mainWindow.SetSelectedText(selectedText);
                _mainWindow.ShowWindow();
            }
        }

        private void ShowAskWindow()
        {
            if (_askWindow != null)
            {
                _askWindow.ClearInput();
                _askWindow.Show();
            }
        }

        private void ShowAskWindowCentered()
        {
            if (_askWindow != null)
            {
                _askWindow.ClearInput();
                _askWindow.ShowCentered();
            }
        }

        private void ShowSettingsWindow()
        {
            _settingsWindow?.Show();
        }

        private void ShowAboutWindow()
        {
            _aboutWindow?.Show();
        }

        private void ExitApplication()
        {
            // Clean up
            if (_keyboardHook != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_keyboardHook);
                _keyboardHook = IntPtr.Zero;
            }

            _trayIcon?.Dispose();
            _mainWindow?.Close();
            _askWindow?.Close();
            _settingsWindow?.Close();
            _aboutWindow?.Close();
            
            Environment.Exit(0);
        }
    }
}
