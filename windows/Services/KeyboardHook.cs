using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Klackr.Models;

namespace Klackr.Services
{
    public class KeyboardHook : IDisposable
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        private readonly LowLevelKeyboardProc _proc;
        private IntPtr _hookId = IntPtr.Zero;

        public event Action<KeyType, KeyEvent>? OnKeyEvent;

        public KeyboardHook()
        {
            _proc = HookCallback;
            Start();
        }

        public void Start()
        {
            if (_hookId == IntPtr.Zero)
            {
                using var curProcess = Process.GetCurrentProcess();
                using var curModule = curProcess.MainModule;
                if (curModule != null)
                {
                    _hookId = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, GetModuleHandle(curModule.ModuleName), 0);
                }
            }
        }

        public void Stop()
        {
            if (_hookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookId);
                _hookId = IntPtr.Zero;
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int msg = wParam.ToInt32();
                bool isDown = msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN;
                bool isUp = msg == WM_KEYUP || msg == WM_SYSKEYUP;

                if (isDown || isUp)
                {
                    int vkCode = Marshal.ReadInt32(lParam);
                    var keyType = ClassifyKey(vkCode);
                    var keyEvent = isDown ? KeyEvent.Press : KeyEvent.Release;

                    OnKeyEvent?.Invoke(keyType, keyEvent);
                }
            }

            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        private static KeyType ClassifyKey(int vkCode)
        {
            switch (vkCode)
            {
                case 0x20: // VK_SPACE
                    return KeyType.Space;

                case 0x0D: // VK_RETURN
                    return KeyType.Enter;

                case 0x08: // VK_BACK
                case 0x2E: // VK_DELETE
                    return KeyType.Backspace;

                case 0x10: // VK_SHIFT
                case 0xA0: // VK_LSHIFT
                case 0xA1: // VK_RSHIFT
                case 0x11: // VK_CONTROL
                case 0xA2: // VK_LCONTROL
                case 0xA3: // VK_RCONTROL
                case 0x12: // VK_MENU (Alt)
                case 0xA4: // VK_LMENU
                case 0xA5: // VK_RMENU
                case 0x5B: // VK_LWIN
                case 0x5C: // VK_RWIN
                case 0x14: // VK_CAPITAL
                    return KeyType.Modifier;

                default:
                    return KeyType.Regular;
            }
        }

        public void Dispose()
        {
            Stop();
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }
}
