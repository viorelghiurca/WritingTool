using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using System;
using System.Runtime.InteropServices;
using WinRT.Interop;
using WritingTool.Services;

namespace WritingTool
{
    /// <summary>
    /// About window displaying application and developer information.
    /// </summary>
    public sealed partial class AboutWindow : Window
    {
        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SM_CXSCREEN = 0;
        private const int SM_CYSCREEN = 1;
        private const int SW_HIDE = 0;

        public AboutWindow()
        {
            InitializeComponent();
            ConfigureWindow();
        }

        private void ConfigureWindow()
        {
            var hWnd = WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);

            // Set window size
            int width = 420;
            int height = 620;
            appWindow.Resize(new Windows.Graphics.SizeInt32(width, height));

            // Center on screen
            int screenWidth = GetSystemMetrics(SM_CXSCREEN);
            int screenHeight = GetSystemMetrics(SM_CYSCREEN);
            int x = (screenWidth - width) / 2;
            int y = (screenHeight - height) / 2;
            appWindow.Move(new Windows.Graphics.PointInt32(x, y));

            // Set title bar
            if (appWindow.TitleBar != null)
            {
                appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
                appWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
                appWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
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
                e.Handled = true;
            }
        }

        /// <summary>
        /// Shows the window centered on screen.
        /// </summary>
        public void Show()
        {
            Activate();
        }

        /// <summary>
        /// Hides the window without closing it.
        /// </summary>
        public void Hide()
        {
            var hWnd = WindowNative.GetWindowHandle(this);
            ShowWindow(hWnd, SW_HIDE);
        }

        /// <summary>
        /// Updates all UI text to current language.
        /// </summary>
        public void UpdateLocalization()
        {
            // Header
            AppNameText.Text = LocalizationService.Get("app_name");
            AppSubtitleText.Text = LocalizationService.Get("app_subtitle");
            
            // Version card
            VersionLabelText.Text = LocalizationService.Get("about_version");
            StableText.Text = LocalizationService.Get("about_stable");
            
            // Developer card
            DeveloperRoleText.Text = LocalizationService.Get("about_developer");
            CertificationText.Text = LocalizationService.Get("about_certification");
        }
    }
}
