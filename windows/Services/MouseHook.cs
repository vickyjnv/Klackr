using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Klackr.Models;

namespace Klackr.Services
{
    public class MouseHook : IDisposable
    {
        private const int WH_MOUSE_LL = 14;

        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONUP = 0x0202;
        private const int WM_RBUTTONDOWN = 0x0204;
        private const int WM_RBUTTONUP = 0x0205;
        private const int WM_MBUTTONDOWN = 0x0207;
        private const int WM_MBUTTONUP = 0x0208;

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);
        private readonly LowLevelMouseProc _proc;
        private IntPtr _hookId = IntPtr.Zero;

        public event Action<MouseButton, MouseEventType>? OnMouseEvent;

        public MouseHook()
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
                    _hookId = SetWindowsHookEx(WH_MOUSE_LL, _proc, GetModuleHandle(curModule.ModuleName), 0);
                    if (_hookId == IntPtr.Zero)
                    {
                        int error = Marshal.GetLastWin32Error();
                        System.Diagnostics.Debug.WriteLine($"Failed to install mouse hook. Error code: {error}");
                        throw new System.ComponentModel.Win32Exception(error, "Failed to install low-level mouse hook. Run as administrator if needed.");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Mouse hook installed successfully. Hook ID: {_hookId}");
                    }
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
            try
            {
                if (nCode >= 0)
                {
                    int msg = wParam.ToInt32();
                    switch (msg)
                    {
                        case WM_LBUTTONDOWN:
                            InvokeSafe(MouseButton.Left, MouseEventType.Press);
                            break;
                        case WM_LBUTTONUP:
                            InvokeSafe(MouseButton.Left, MouseEventType.Release);
                            break;
                        case WM_RBUTTONDOWN:
                            InvokeSafe(MouseButton.Right, MouseEventType.Press);
                            break;
                        case WM_RBUTTONUP:
                            InvokeSafe(MouseButton.Right, MouseEventType.Release);
                            break;
                        case WM_MBUTTONDOWN:
                            InvokeSafe(MouseButton.Middle, MouseEventType.Press);
                            break;
                        case WM_MBUTTONUP:
                            InvokeSafe(MouseButton.Middle, MouseEventType.Release);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Mouse hook callback error: {ex.Message}");
            }

            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        private void InvokeSafe(MouseButton button, MouseEventType eventType)
        {
            try
            {
                OnMouseEvent?.Invoke(button, eventType);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Mouse hook event error: {ex.Message}");
            }
        }

        public void Dispose()
        {
            Stop();
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }
}
