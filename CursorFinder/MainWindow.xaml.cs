using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CursorFinder;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);
    private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    public List<string> ModifierKeys { get; set; } = ["Alt", "Ctrl", "Shift"];
    public List<string> AlphabetKeys { get; set; } = [
        "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M",
        "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z"
    ];
    public Boolean UseMouseClick { get; set; } = Settings.Default.UseMouseClick;
    public Boolean UseHotKey { get; set; } = Settings.Default.UseHotKey;
    public String ModifierKey1 { get; set; } = Settings.Default.ModifierKey1;
    public String ModifierKey2 { get; set; } = Settings.Default.ModifierKey2;
    public String ModifierKey3 { get; set; } = Settings.Default.ModifierKey3;

    public String LetterKey { get; set; } = Settings.Default.LetterKey;

    private const int WH_MOUSE_LL = 14;
    private const IntPtr WM_LBUTTONDOWN = 0x0201; // 左键按下
    private const IntPtr WM_RBUTTONDOWN = 0x0204; // 右键按下
    private const IntPtr WM_LBUTTONUP = 0x0202;   // 左键松开
    private const IntPtr WM_RBUTTONUP = 0x0205;   // 右键松开

    private IntPtr _hookID = IntPtr.Zero;
    private readonly IntPtr _cursorImageHandle;
    private bool _isCustomCursorImage = false;
    private IntPtr _windowHandle = IntPtr.Zero;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        _cursorImageHandle = CursorManagement.LoadCursorImageFile(@".\cursor.cur");
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            // 检查鼠标事件类型
            switch (wParam)
            {
                case WM_LBUTTONDOWN:
                    if (GetAsyncKeyState(0x02) != 0 && !_isCustomCursorImage) // 检查右键是否按下
                    {
                        ShowCustomCursor();
                    }
                    break;

                case WM_RBUTTONDOWN:
                    if (GetAsyncKeyState(0x01) != 0 && !_isCustomCursorImage) // 检查左键是否按下
                    {
                        ShowCustomCursor();
                    }
                    break;

                case WM_LBUTTONUP:
                    if (GetAsyncKeyState(0x02) == 0 && _isCustomCursorImage) // 检查右键是否松开
                    {
                        ShowDefaultCursor();
                    }
                    break;

                case WM_RBUTTONUP:
                    if (GetAsyncKeyState(0x01) == 0 && _isCustomCursorImage) // 检查左键是否松开
                    {
                        ShowDefaultCursor();
                    }
                    break;
            }
        }
        return CallNextHookEx(_hookID, nCode, wParam, lParam);
    }

    private void ShowCustomCursor()
    {
        CursorManagement.SetCustomCursorImage(_cursorImageHandle, SystemCursorId.OCR_NORMAL);
        CursorManagement.SetCustomCursorImage(_cursorImageHandle, SystemCursorId.OCR_IBEAM);
        CursorManagement.SetCustomCursorImage(_cursorImageHandle, SystemCursorId.OCR_HAND);
        _isCustomCursorImage = true;
    }

    private void ShowDefaultCursor()
    {
        CursorManagement.RestoreDefaultCursorImage(SystemCursorId.OCR_NORMAL);
        CursorManagement.RestoreDefaultCursorImage(SystemCursorId.OCR_IBEAM);
        CursorManagement.RestoreDefaultCursorImage(SystemCursorId.OCR_HAND);
        _isCustomCursorImage = false;
    }

    private void SaveSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        Settings.Default.UseMouseClick = UseMouseClick;
        Settings.Default.UseHotKey = UseHotKey;
        Settings.Default.ModifierKey1 = ModifierKey1;
        Settings.Default.ModifierKey2 = ModifierKey2;
        Settings.Default.ModifierKey3 = ModifierKey3;
        Settings.Default.LetterKey = LetterKey;
        Settings.Default.Save();
        MessageBox.Show("Settings saved successfully!\nRestart the application to apply", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        _windowHandle = new WindowInteropHelper(this).Handle;
        if (UseMouseClick)
        {
            _hookID = SetWindowsHookEx(WH_MOUSE_LL, HookCallback, GetModuleHandle(Process.GetCurrentProcess().MainModule.ModuleName), 0);
        }
        if (UseHotKey)
        {
            uint modifier = HotKeyManagement.GetModifierByName(ModifierKey1)
                | HotKeyManagement.GetModifierByName(ModifierKey2)
                | HotKeyManagement.GetModifierByName(ModifierKey3);
            uint vk = HotKeyManagement.GetVirtualKeyByName(LetterKey);
            HotKeyManagement hotKey = new(_windowHandle, 0xBFFF, modifier, vk);
            hotKey.Register();
            hotKey.OnHotKeyPressedEvent += () =>
            {
                if (_isCustomCursorImage)
                    ShowDefaultCursor();
                else
                    ShowCustomCursor();
            };
        }
    }

    private void HotKey_OnHotKeyPressedEvent()
    {
        throw new NotImplementedException();
    }
}