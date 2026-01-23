using System;
using System.Runtime.InteropServices;

namespace WritingTool.Services
{
    /// <summary>
    /// Service to register and handle global hotkeys using Win32 API.
    /// </summary>
    public class GlobalHotkeyService : IDisposable
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const uint MOD_CONTROL = 0x0002;
        private const uint VK_SPACE = 0x20;
        private const int HOTKEY_ID = 9000;

        private readonly IntPtr _hWnd;
        private bool _isRegistered;

        public event Action? HotkeyPressed;

        public GlobalHotkeyService(IntPtr windowHandle)
        {
            _hWnd = windowHandle;
        }

        public bool Register()
        {
            if (_isRegistered) return true;
            
            _isRegistered = RegisterHotKey(_hWnd, HOTKEY_ID, MOD_CONTROL, VK_SPACE);
            return _isRegistered;
        }

        public void Unregister()
        {
            if (_isRegistered)
            {
                UnregisterHotKey(_hWnd, HOTKEY_ID);
                _isRegistered = false;
            }
        }

        public void ProcessMessage(int msg, IntPtr wParam)
        {
            const int WM_HOTKEY = 0x0312;
            if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
            {
                HotkeyPressed?.Invoke();
            }
        }

        public void Dispose()
        {
            Unregister();
        }
    }
}
