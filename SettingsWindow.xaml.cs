using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.Win32;
using System;
using System.Runtime.InteropServices;
using WinRT.Interop;
using WritingTool.Models;
using WritingTool.Services;

namespace WritingTool
{
    /// <summary>
    /// Settings window for configuring the application.
    /// </summary>
    public sealed partial class SettingsWindow : Window
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
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        private const uint WM_NCLBUTTONDOWN = 0x00A1;
        private static readonly IntPtr HT_CAPTION = new IntPtr(2);

        private const int SM_CXSCREEN = 0;
        private const int SM_CYSCREEN = 1;

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
        private readonly int _width = 420;
        private readonly int _height = 700;
        private readonly SettingsService _settingsService;
        private SettingsConfig _config = null!;

        public SettingsWindow()
        {
            InitializeComponent();
            _settingsService = new SettingsService();
            SetupWindow();
            _ = LoadSettingsAsync();
        }

        private async System.Threading.Tasks.Task LoadSettingsAsync()
        {
            _config = await _settingsService.LoadAsync();
            
            // Populate UI
            StartOnBootCheckBox.IsOn = _config.StartOnBoot;
            ShortcutTextBox.Text = _config.Shortcut;

            // Select language
            foreach (ComboBoxItem item in LanguageComboBox.Items)
            {
                if (item.Tag?.ToString() == _config.Language)
                {
                    LanguageComboBox.SelectedItem = item;
                    break;
                }
            }

            // Select theme
            foreach (ComboBoxItem item in ThemeComboBox.Items)
            {
                if (item.Tag?.ToString() == _config.Theme)
                {
                    ThemeComboBox.SelectedItem = item;
                    break;
                }
            }

            // Select provider
            foreach (ComboBoxItem item in ProviderComboBox.Items)
            {
                if (item.Tag?.ToString() == _config.Provider)
                {
                    ProviderComboBox.SelectedItem = item;
                    break;
                }
            }

            // Load provider settings
            LoadProviderSettings();
        }

        private void LoadProviderSettings()
        {
            // Gemini
            if (_config.Providers.TryGetValue("Gemini (Recommended)", out var gemini))
            {
                GeminiApiKeyBox.Text = gemini.ApiKey ?? "";
                GeminiModelBox.Text = gemini.ModelName ?? "gemini-2.0-flash";
            }

            // OpenAI
            if (_config.Providers.TryGetValue("OpenAI Compatible (For Experts)", out var openai))
            {
                OpenAIApiKeyBox.Text = openai.ApiKey ?? "";
                OpenAIBaseUrlBox.Text = openai.ApiBase ?? "https://api.openai.com/v1";
                OpenAIOrganisationBox.Text = openai.ApiOrganisation ?? "";
                OpenAIProjectBox.Text = openai.ApiProject ?? "";
                OpenAIModelBox.Text = openai.ModelName ?? "gpt-4o-mini";
            }

            // Ollama
            if (_config.Providers.TryGetValue("Ollama (For Experts)", out var ollama))
            {
                OllamaBaseUrlBox.Text = ollama.ApiBase ?? "http://localhost:11434";
                OllamaModelBox.Text = ollama.ModelName ?? "llama3.1:8b";
                OllamaKeepAliveBox.Text = ollama.KeepAlive ?? "15";
            }
        }

        private void SetupWindow()
        {
            var hWnd = WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
            _appWindow = AppWindow.GetFromWindowId(windowId);

            // Set window title and icon
            _appWindow.Title = "Settings";
            _appWindow.SetIcon("Assets\\app.ico");

            _appWindow.Resize(new Windows.Graphics.SizeInt32(_width, _height));

            // Center on screen
            CenterOnScreen();

            if (_appWindow.Presenter is OverlappedPresenter presenter)
            {
                // Keep title bar for intuitive dragging
                presenter.IsResizable = false;
                presenter.IsMaximizable = false;
                presenter.IsMinimizable = false;
            }
        }

        private void ProviderComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProviderComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                var provider = selectedItem.Tag?.ToString() ?? "";
                ProviderTitleText.Text = provider;

                // Show/hide panels based on selection
                GeminiPanel.Visibility = provider == "Gemini (Recommended)" ? Visibility.Visible : Visibility.Collapsed;
                OpenAIPanel.Visibility = provider == "OpenAI Compatible (For Experts)" ? Visibility.Visible : Visibility.Collapsed;
                OllamaPanel.Visibility = provider == "Ollama (For Experts)" ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Update config from UI
            _config.StartOnBoot = StartOnBootCheckBox.IsOn;
            _config.Shortcut = ShortcutTextBox.Text;

            // Save language
            if (LanguageComboBox.SelectedItem is ComboBoxItem langItem)
            {
                _config.Language = langItem.Tag?.ToString() ?? "en";
            }

            // Save theme
            if (ThemeComboBox.SelectedItem is ComboBoxItem themeItem)
            {
                _config.Theme = themeItem.Tag?.ToString() ?? "mica";
            }

            if (ProviderComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                _config.Provider = selectedItem.Tag?.ToString() ?? "Gemini (Recommended)";
            }

            // Update Gemini settings
            if (!_config.Providers.ContainsKey("Gemini (Recommended)"))
                _config.Providers["Gemini (Recommended)"] = new ProviderConfig();
            _config.Providers["Gemini (Recommended)"].ApiKey = GeminiApiKeyBox.Text;
            _config.Providers["Gemini (Recommended)"].ModelName = GeminiModelBox.Text;

            // Update OpenAI settings
            if (!_config.Providers.ContainsKey("OpenAI Compatible (For Experts)"))
                _config.Providers["OpenAI Compatible (For Experts)"] = new ProviderConfig();
            _config.Providers["OpenAI Compatible (For Experts)"].ApiKey = OpenAIApiKeyBox.Text;
            _config.Providers["OpenAI Compatible (For Experts)"].ApiBase = OpenAIBaseUrlBox.Text;
            _config.Providers["OpenAI Compatible (For Experts)"].ApiOrganisation = OpenAIOrganisationBox.Text;
            _config.Providers["OpenAI Compatible (For Experts)"].ApiProject = OpenAIProjectBox.Text;
            _config.Providers["OpenAI Compatible (For Experts)"].ModelName = OpenAIModelBox.Text;

            // Update Ollama settings
            if (!_config.Providers.ContainsKey("Ollama (For Experts)"))
                _config.Providers["Ollama (For Experts)"] = new ProviderConfig();
            _config.Providers["Ollama (For Experts)"].ApiBase = OllamaBaseUrlBox.Text;
            _config.Providers["Ollama (For Experts)"].ModelName = OllamaModelBox.Text;
            _config.Providers["Ollama (For Experts)"].KeepAlive = OllamaKeepAliveBox.Text;

            // Handle Start on Boot
            SetStartOnBoot(_config.StartOnBoot);

            await _settingsService.SaveAsync(_config);

            // Reload settings in the app to apply new shortcut
            if (App.Instance != null)
            {
                await App.Instance.ReloadSettingsAsync();
            }

            Hide();
        }

        private void SetStartOnBoot(bool enable)
        {
            const string appName = "WritingTool";
            var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;

            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                if (key != null)
                {
                    if (enable && exePath != null)
                    {
                        key.SetValue(appName, $"\"{exePath}\"");
                    }
                    else
                    {
                        key.DeleteValue(appName, false);
                    }
                }
            }
            catch
            {
                // Ignore registry errors
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        private void Grid_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Escape)
            {
                Hide();
            }
        }

        public void Hide()
        {
            _appWindow.Hide();
        }

        private void CenterOnScreen()
        {
            int screenWidth = GetSystemMetrics(SM_CXSCREEN);
            int screenHeight = GetSystemMetrics(SM_CYSCREEN);
            int x = (screenWidth - _width) / 2;
            int y = (screenHeight - _height) / 2;
            _appWindow.Move(new Windows.Graphics.PointInt32(x, y));
        }

        public void Show()
        {
            CenterOnScreen();
            var hWnd = WindowNative.GetWindowHandle(this);
            SetWindowPos(hWnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
            _appWindow.Show();
            Activate();
        }

        /// <summary>
        /// Updates all UI text to current language.
        /// </summary>
        public void UpdateLocalization()
        {
            // Header
            SettingsTitleText.Text = LocalizationService.Get("settings_title");
            SettingsSubtitleText.Text = LocalizationService.Get("settings_subtitle");
            
            // General section
            GeneralHeaderText.Text = LocalizationService.Get("settings_general");
            LanguageLabelText.Text = LocalizationService.Get("settings_language");
            StartOnBootCheckBox.Header = LocalizationService.Get("settings_start_on_boot");
            StartOnBootCheckBox.OnContent = LocalizationService.Get("settings_enabled");
            StartOnBootCheckBox.OffContent = LocalizationService.Get("settings_disabled");
            ShortcutLabelText.Text = LocalizationService.Get("settings_shortcut");
            ThemeLabelText.Text = LocalizationService.Get("settings_theme");
            
            // Provider section
            ProviderLabelText.Text = LocalizationService.Get("settings_provider");
            GeminiApiKeyLabel.Text = LocalizationService.Get("settings_api_key");
            GeminiModelLabel.Text = LocalizationService.Get("settings_model");
            GeminiApiKeyLink.Content = LocalizationService.Get("settings_get_api_key");
            
            // Save button
            SaveButtonText.Text = LocalizationService.Get("settings_save");
        }
    }
}
