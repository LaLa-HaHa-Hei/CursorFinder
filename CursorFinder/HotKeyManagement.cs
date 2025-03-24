using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace CursorFinder
{
    class HotKeyManagement : IDisposable
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        [DllImport("user32.dll")]
        private static extern uint MapVirtualKey(uint uCode, uint uMapType);
        public bool IsRegistered { get; private set; } = false;

        private const int WM_HOTKEY = 0x0312;
        private const uint MOD_ALT = 0x0001;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        private const uint MOD_WIN = 0x0008;
        private readonly nint _handle;
        private readonly int _id;
        private readonly uint _fsModifiers;
        private readonly uint _vk;
        private bool _disposed = false;
        
        public event Action? OnHotKeyPressed;

        public HotKeyManagement(nint handle, int id, uint fsModifiers, uint vk)
        {
            _handle = handle;
            _id = id;
            _fsModifiers = fsModifiers;
            _vk = vk;

            HwndSource hwndSource = HwndSource.FromHwnd(_handle);
            if (hwndSource != null)
            {
                hwndSource.AddHook(WndProc);
            }
            else
            {
                throw new InvalidOperationException("无法获取窗口的 HwndSource 对象。");
            }
        }

        ~HotKeyManagement()
        {
            Dispose(false);
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this); // 阻止终结器调用
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                //if (disposing)
                //{
                //    // 释放托管资源
                //}
                // 释放非托管资源
                if (IsRegistered)
                {
                    UnregisterHotKey(_handle, _id);
                    IsRegistered = false;
                }

                _disposed = true;
            }
        }
        public bool Register()
        {
            if (!RegisterHotKey(_handle, _id, _fsModifiers, _vk))
            {
                return false;
            }
            else
            {
                IsRegistered = true;
                return true;
            }
        }
        public bool UnRegister()
        {
            if (UnregisterHotKey(_handle, _id))
            {
                IsRegistered = false;
                return true;
            }
            else
            {
                return false;
            }
        }
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY && wParam.ToInt32() == _id)
            {
                OnHotKeyPressed?.Invoke();
                handled = true;  // 标记消息已处理
            }
            else
            {
                handled = false;
            }
            return IntPtr.Zero;
        }

        public static uint GetModifierByName(string name)
        {
            name = name.ToLower();
            if (name == "alt")
                return MOD_ALT;
            else if (name == "ctrl")
                return MOD_CONTROL;
            else if (name == "shift")
                return MOD_SHIFT;
            else if (name == "win")
                return MOD_WIN;
            else
                return 0;
        }

        public static uint GetVirtualKeyByName(string name)
        {
            uint scanCode = MapVirtualKey(Convert.ToUInt32(name[0]), 0); // 将字符串的第一个字符转换为对应的扫描码
            return MapVirtualKey(scanCode, 1); // 将扫描码转换为虚拟键值
        }
    }
}
