using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace WritingTool.Services
{
    /// <summary>
    /// Service to detect and retrieve selected text from other applications.
    /// </summary>
    public static class ClipboardService
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [DllImport("user32.dll")]
        private static extern bool CloseClipboard();

        [DllImport("user32.dll")]
        private static extern bool EmptyClipboard();

        [DllImport("user32.dll")]
        private static extern IntPtr GetClipboardData(uint uFormat);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GlobalLock(IntPtr hMem);

        [DllImport("kernel32.dll")]
        private static extern bool GlobalUnlock(IntPtr hMem);

        private const byte VK_CONTROL = 0x11;
        private const byte VK_C = 0x43;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const uint CF_UNICODETEXT = 13;

        /// <summary>
        /// Attempts to get selected text by simulating Ctrl+C and reading clipboard.
        /// Returns the selected text or null if nothing is selected.
        /// </summary>
        public static async Task<string?> GetSelectedTextAsync()
        {
            // Save current clipboard content
            string? originalClipboard = GetClipboardText();

            // Clear clipboard
            ClearClipboard();

            // Simulate Ctrl+C to copy selected text
            SimulateCtrlC();

            // Wait a bit for the copy to complete
            await Task.Delay(50);

            // Get the new clipboard content
            string? selectedText = GetClipboardText();

            // Restore original clipboard if we didn't get new text
            if (string.IsNullOrEmpty(selectedText) && !string.IsNullOrEmpty(originalClipboard))
            {
                SetClipboardText(originalClipboard);
            }

            return string.IsNullOrWhiteSpace(selectedText) ? null : selectedText;
        }

        private static void SimulateCtrlC()
        {
            // Press Ctrl
            keybd_event(VK_CONTROL, 0, 0, UIntPtr.Zero);
            // Press C
            keybd_event(VK_C, 0, 0, UIntPtr.Zero);
            // Release C
            keybd_event(VK_C, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            // Release Ctrl
            keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        }

        private static void ClearClipboard()
        {
            if (OpenClipboard(IntPtr.Zero))
            {
                EmptyClipboard();
                CloseClipboard();
            }
        }

        private static string? GetClipboardText()
        {
            string? result = null;
            if (OpenClipboard(IntPtr.Zero))
            {
                IntPtr hData = GetClipboardData(CF_UNICODETEXT);
                if (hData != IntPtr.Zero)
                {
                    IntPtr pData = GlobalLock(hData);
                    if (pData != IntPtr.Zero)
                    {
                        result = Marshal.PtrToStringUni(pData);
                        GlobalUnlock(hData);
                    }
                }
                CloseClipboard();
            }
            return result;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

        private const uint GMEM_MOVEABLE = 0x0002;

        private static void SetClipboardText(string text)
        {
            if (OpenClipboard(IntPtr.Zero))
            {
                EmptyClipboard();
                IntPtr hGlobal = GlobalAlloc(GMEM_MOVEABLE, (UIntPtr)((text.Length + 1) * 2));
                if (hGlobal != IntPtr.Zero)
                {
                    IntPtr pGlobal = GlobalLock(hGlobal);
                    if (pGlobal != IntPtr.Zero)
                    {
                        Marshal.Copy(text.ToCharArray(), 0, pGlobal, text.Length);
                        GlobalUnlock(hGlobal);
                        SetClipboardData(CF_UNICODETEXT, hGlobal);
                    }
                }
                CloseClipboard();
            }
        }
    }
}
