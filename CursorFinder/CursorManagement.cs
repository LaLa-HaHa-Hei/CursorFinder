using System.Runtime.InteropServices;
using System.Drawing;
using System.IO;

namespace CursorFinder
{
    /// <summary>
    /// 管理光标资源
    /// </summary>
    class CursorManagement
    {
        [DllImport("user32.dll")]
        private static extern bool SetSystemCursor(IntPtr hcur, uint id);

        [DllImport("user32.dll", EntryPoint = "LoadImage")]
        public static extern IntPtr LoadImageByHandle(IntPtr hInst, IntPtr lpszName, uint uType, int cxDesired, int cyDesired, uint fuLoad);

        [DllImport("user32.dll", EntryPoint = "LoadImage")]
        public static extern IntPtr LoadImageByFile(IntPtr hInst, string lpszName, uint uType, int cxDesired, int cyDesired, uint fuLoad);

        [DllImport("user32.dll")]
        private static extern bool DestroyCursor(IntPtr hCursor);

        [DllImport("user32.dll")]
        private static extern IntPtr CopyIcon(IntPtr hIcon);

        // LoadImage()函数的uType参数
        const uint IMAGE_CURSOR = 2;    // 表示加载光标

        // LoadImage()函数的fuLoad参数
        const uint LR_SHARED = 0x00008000;  // 在共享内存中加载光标
        const uint LR_LOADFROMFILE = 0x00000010;  // 从文件加载光标

        // 存储原始光标
        private static readonly Dictionary<SystemCursorId, IntPtr> _originalCursors = [];

        /// <summary>
        /// 设置自定义光标
        /// </summary>
        /// <param name="cursorFilePath"></param>
        /// <param name="cursorId"></param>
        /// <returns></returns>
        public static bool SetCustomCursorImage(IntPtr hCursor, SystemCursorId cursorId)
        {
            // 保存原始光标
            if (!_originalCursors.ContainsKey(cursorId))
            {
                _originalCursors[cursorId] = CopyIcon(LoadImageByHandle(IntPtr.Zero, new IntPtr((int)cursorId), IMAGE_CURSOR, 0, 0, LR_SHARED));
            }
            return SetSystemCursor(CopyIcon(hCursor), (uint)cursorId);
        }

        /// <summary>
        /// 加载光标图标文件
        /// </summary>
        /// <param name="cursorFilePath"></param>
        /// <returns>返回光标图标的句柄</returns>
        public static IntPtr LoadCursorImageFile(string cursorFilePath)
        {
            string tempFilePath = @".\temp-cursor.cur";
            File.Copy(cursorFilePath, tempFilePath, overwrite: true);
            // 加载自定义光标
            int width = 0;
            int height = 0;
            try
            {
                using var icon = new Icon(tempFilePath);
                width = icon.Width;
                height = icon.Height;
            }
            catch (ArgumentException)
            { }
            IntPtr hCursor = LoadImageByFile(IntPtr.Zero, tempFilePath, IMAGE_CURSOR, width, height, LR_LOADFROMFILE);
            File.Delete(tempFilePath);
            return hCursor;
        }

        /// <summary>
        /// 销毁自定义光标
        /// </summary>
        /// <param name="hCursor"></param>
        /// <returns></returns>
        public static bool DestroyCustomCursorImage(IntPtr hCursor)
        {
            if (hCursor == IntPtr.Zero)
                return false;
            return DestroyCursor(hCursor);
        }

        /// <summary>
        /// 重置指定系统光标
        /// </summary>
        /// <returns>true则成功</returns>
        public static bool RestoreDefaultCursorImage(SystemCursorId cursorId)
        {
            if (!_originalCursors.TryGetValue(cursorId, out nint hCursor))
                return false;
            if (hCursor == IntPtr.Zero)
                return false;

            if (!SetSystemCursor(CopyIcon(hCursor), (uint)cursorId))
                return false;

            return true;
        }
    }
}
