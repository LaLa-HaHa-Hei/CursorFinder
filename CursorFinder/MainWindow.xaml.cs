using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Microsoft.Win32;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.IO;

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
    private IntPtr _cursorImageHandle;
    private bool _isCustomCursorImage = false;
    private IntPtr _windowHandle = IntPtr.Zero;
    HotKeyManagement? _hotKeyManagement;
    public string? _selectedFile;

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
        _hotKeyManagement?.UnRegister();
        if (_hookID != IntPtr.Zero)
            UnhookWindowsHookEx(_hookID);
        if (UseHotKey)
            RegisterHotKey();
        if (UseMouseClick)
            _hookID = SetWindowsHookEx(WH_MOUSE_LL, HookCallback, GetModuleHandle(Process.GetCurrentProcess().MainModule.ModuleName), 0);
        if (_selectedFile != null)
        {
            try
            {
                int hotSpotX = int.Parse(HotSpotXTextBox.Text);
                int hotSpotY = int.Parse(HotSpotYTextBox.Text);
                // 把选择的png转换为cur，存到程序目录中
                using var image = Image.Load<Rgba32>(_selectedFile);
                // 获取图像数据
                var bmpData = GetBmpData(image);
                var maskData = GetMaskData(image);

                using var fs = new FileStream(@".\cursor.cur", FileMode.Create);
                using var writer = new BinaryWriter(fs);
                // CUR文件头（6字节）
                writer.Write((ushort)0);    // 保留
                writer.Write((ushort)2);    // 类型（2表示光标）
                writer.Write((ushort)1);    // 图像数量

                // 目录条目（16字节）
                writer.Write((byte)(image.Width < 256 ? image.Width : 0));    // 宽度
                writer.Write((byte)(image.Height < 256 ? image.Height : 0));   // 高度
                writer.Write((byte)0);              // 颜色数（无调色板）
                writer.Write((byte)0);              // 保留
                writer.Write((ushort)hotSpotX);    // 热点X
                writer.Write((ushort)hotSpotY);     // 热点Y
                writer.Write((uint)(bmpData.Length + maskData.Length)); // 数据大小
                writer.Write((uint)(6 + 16));       // 数据偏移（头+目录）

                // 写入BMP信息头和像素数据
                writer.Write(bmpData);
                // 写入掩码数据
                writer.Write(maskData);
                _cursorImageHandle = CursorManagement.LoadCursorImageFile(@".\cursor.cur");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存光标文件时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }
        _selectedFile = null;
        SelectedFileTextBlock.Text = "";
        MessageBox.Show("Settings saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        _windowHandle = new WindowInteropHelper(this).Handle;
        if (UseMouseClick)
            _hookID = SetWindowsHookEx(WH_MOUSE_LL, HookCallback, GetModuleHandle(Process.GetCurrentProcess().MainModule.ModuleName), 0);
        if (UseHotKey)
            RegisterHotKey();
    }

    private void RegisterHotKey()
    {
        uint modifier = HotKeyManagement.GetModifierByName(ModifierKey1)
            | HotKeyManagement.GetModifierByName(ModifierKey2)
            | HotKeyManagement.GetModifierByName(ModifierKey3);
        uint vk = HotKeyManagement.GetVirtualKeyByName(LetterKey);
        _hotKeyManagement = new(_windowHandle, 0xBFFF, modifier, vk);
        _hotKeyManagement.Register();
        _hotKeyManagement.OnHotKeyPressedEvent += () =>
        {
            if (_isCustomCursorImage)
                ShowDefaultCursor();
            else
                ShowCustomCursor();
        };
    }

    private void ChangeCursorImageButton_Click(object sender, RoutedEventArgs e)
    {
        OpenFileDialog openFileDialog = new()
        {
            Filter = "Image|*.png;",
            RestoreDirectory = true
        };
        openFileDialog.ShowDialog();
        SelectedFileTextBlock.Text = openFileDialog.FileName;
        _selectedFile = openFileDialog.FileName;
    }
    private static byte[] GetBmpData(Image<Rgba32> image)
    {
        using (var ms = new MemoryStream())
        using (var writer = new BinaryWriter(ms))
        {
            // BITMAPINFOHEADER（40字节）
            writer.Write(40);               // 结构大小
            writer.Write(image.Width);     // 宽度
            writer.Write(image.Height * 2); // 高度（图像+掩码）
            writer.Write((ushort)1);        // 颜色平面
            writer.Write((ushort)32);       // 位深度
            writer.Write(0);                // 压缩方式
            writer.Write(0);                // 图像大小
            writer.Write(0);                // 水平分辨率
            writer.Write(0);                // 垂直分辨率
            writer.Write(0);                // 使用的颜色数
            writer.Write(0);               // 重要颜色数

            // 像素数据（自下而上，BGRA格式）
            for (int y = image.Height - 1; y >= 0; y--)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    var pixel = image[x, y];
                    writer.Write(pixel.B);
                    writer.Write(pixel.G);
                    writer.Write(pixel.R);
                    writer.Write(pixel.A);
                }
            }

            return ms.ToArray();
        }
    }

    private static byte[] GetMaskData(Image<Rgba32> image)
    {
        // 掩码是1bpp位图，每行对齐到4字节
        int stride = (image.Width + 31) / 32 * 4;
        byte[] mask = new byte[stride * image.Height];

        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                var pixel = image[x, y];
                // Alpha通道小于128视为透明（掩码位设为1）
                if (pixel.A < 128)
                {
                    int pos = y * stride + x / 8;
                    mask[pos] |= (byte)(0x80 >> (x % 8));
                }
            }
        }

        return mask;
    }
}