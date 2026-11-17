using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Klackr.Services
{
    public class HotkeyManager : NativeWindow, IDisposable
    {
        private const int WM_HOTKEY = 0x0312;
        private const uint MOD_ALT = 0x0001;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_NOREPEAT = 0x4000;
        private const uint VK_K = 0x4B;
        private const int HOTKEY_ID = 9001;

        public event Action? OnHotkeyPressed;

        public HotkeyManager()
        {
            CreateHandle(new CreateParams());
            RegisterHotKey(Handle, HOTKEY_ID, MOD_ALT | MOD_CONTROL | MOD_NOREPEAT, VK_K);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID)
            {
                OnHotkeyPressed?.Invoke();
            }
            base.WndProc(ref m);
        }

        public void Dispose()
        {
            UnregisterHotKey(Handle, HOTKEY_ID);
            DestroyHandle();
        }

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    }
}
