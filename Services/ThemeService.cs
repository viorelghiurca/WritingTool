using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System;
using System.Runtime.InteropServices;

namespace WritingTool.Services
{
    /// <summary>
    /// Service to manage and apply background themes to windows.
    /// </summary>
    public static class ThemeService
    {
        /// <summary>
        /// Available theme types.
        /// </summary>
        public enum ThemeType
        {
            Mica,
            MicaAlt,
            Acrylic,
            Gradient
        }

        /// <summary>
        /// Parses a theme string from settings into a ThemeType.
        /// </summary>
        public static ThemeType ParseTheme(string? theme)
        {
            return theme?.ToLowerInvariant() switch
            {
                "mica" => ThemeType.Mica,
                "micaalt" or "mica-alt" or "mica_alt" => ThemeType.MicaAlt,
                "acrylic" => ThemeType.Acrylic,
                "gradient" => ThemeType.Gradient,
                _ => ThemeType.Mica
            };
        }

        /// <summary>
        /// Applies the specified theme to a window.
        /// </summary>
        public static void ApplyTheme(Window window, ThemeType theme)
        {
            if (window == null) return;
            
            try
            {
                // Check if window has a valid content before applying theme
                if (window.Content == null)
                {
                    System.Diagnostics.Debug.WriteLine("ThemeService: Window content is null, skipping theme application");
                    return;
                }

                SystemBackdrop backdrop = theme switch
                {
                    ThemeType.Mica => new MicaBackdrop { Kind = MicaKind.Base },
                    ThemeType.MicaAlt => new MicaBackdrop { Kind = MicaKind.BaseAlt },
                    ThemeType.Acrylic => new DesktopAcrylicBackdrop(),
                    ThemeType.Gradient => new MicaBackdrop { Kind = MicaKind.Base }, // Gradient uses Mica as base
                    _ => new MicaBackdrop()
                };
                
                window.SystemBackdrop = backdrop;
            }
            catch (AccessViolationException)
            {
                // Window is in an invalid state, skip theme application
                System.Diagnostics.Debug.WriteLine("ThemeService: AccessViolationException when applying theme");
            }
            catch (COMException)
            {
                // Window handle is invalid
                System.Diagnostics.Debug.WriteLine("ThemeService: COMException when applying theme");
            }
            catch (ObjectDisposedException)
            {
                // Window has been disposed (must come before InvalidOperationException as it inherits from it)
                System.Diagnostics.Debug.WriteLine("ThemeService: ObjectDisposedException when applying theme");
            }
            catch (InvalidOperationException)
            {
                // Window not ready
                System.Diagnostics.Debug.WriteLine("ThemeService: InvalidOperationException when applying theme");
            }
            catch (Exception ex)
            {
                // Catch any other unexpected exceptions to prevent crashes
                System.Diagnostics.Debug.WriteLine($"ThemeService: Unexpected exception when applying theme: {ex.GetType().Name} - {ex.Message}");
            }
        }

        /// <summary>
        /// Applies the theme from a string setting.
        /// </summary>
        public static void ApplyTheme(Window window, string? themeSetting)
        {
            var theme = ParseTheme(themeSetting);
            ApplyTheme(window, theme);
        }
    }
}
