using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace WritingTool.Services
{
    /// <summary>
    /// Service for replacing selected text in other applications.
    /// </summary>
    public static class TextReplacementService
    {
        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [DllImport("user32.dll")]
        private static extern bool CloseClipboard();

        [DllImport("user32.dll")]
        private static extern bool EmptyClipboard();

        [DllImport("user32.dll")]
        private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

        [DllImport("user32.dll")]
        private static extern IntPtr GetClipboardData(uint uFormat);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GlobalLock(IntPtr hMem);

        [DllImport("kernel32.dll")]
        private static extern bool GlobalUnlock(IntPtr hMem);

        private const byte VK_CONTROL = 0x11;
        private const byte VK_V = 0x56;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const uint CF_UNICODETEXT = 13;
        private const uint GMEM_MOVEABLE = 0x0002;

        /// <summary>
        /// Replaces the currently selected text with the provided text.
        /// Copies text to clipboard and simulates Ctrl+V.
        /// </summary>
        public static async Task ReplaceSelectedTextAsync(string newText)
        {
            // Set clipboard content
            SetClipboardText(newText);

            // Small delay to ensure clipboard is ready
            await Task.Delay(50);

            // Simulate Ctrl+V to paste
            SimulateCtrlV();

            // Small delay after paste
            await Task.Delay(50);
        }

        private static void SetClipboardText(string text)
        {
            if (OpenClipboard(IntPtr.Zero))
            {
                EmptyClipboard();

                var bytes = (text.Length + 1) * 2; // Unicode = 2 bytes per char
                var hGlobal = GlobalAlloc(GMEM_MOVEABLE, (UIntPtr)bytes);

                if (hGlobal != IntPtr.Zero)
                {
                    var pGlobal = GlobalLock(hGlobal);
                    if (pGlobal != IntPtr.Zero)
                    {
                        Marshal.Copy(text.ToCharArray(), 0, pGlobal, text.Length);
                        // Null terminator is already there from GlobalAlloc zero-init
                        GlobalUnlock(hGlobal);
                        SetClipboardData(CF_UNICODETEXT, hGlobal);
                    }
                }

                CloseClipboard();
            }
        }

        private static void SimulateCtrlV()
        {
            // Press Ctrl
            keybd_event(VK_CONTROL, 0, 0, UIntPtr.Zero);
            // Press V
            keybd_event(VK_V, 0, 0, UIntPtr.Zero);
            // Release V
            keybd_event(VK_V, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            // Release Ctrl
            keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        }
    }
}
