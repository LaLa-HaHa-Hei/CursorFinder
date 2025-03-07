using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CursorFinder
{
    public enum SystemCursorId : uint
    {
        OCR_NORMAL = 32512,        // 普通选择
        OCR_IBEAM = 32513,         // 文本选择
        OCR_WAIT = 32514,          // 忙碌
        OCR_CROSS = 32515,         // 精度选择
        OCR_UP = 32516,            // 备用选择
        OCR_SIZENWSE = 32642,      // 斜向调整大小光标（西北-东南）
        OCR_SIZENESW = 32643,      // 斜向调整大小光标（东北-西南）
        OCR_SIZEWE = 32644,        // 水平调整大小光标
        OCR_SIZENS = 32645,        // 垂直调整大小光标
        OCR_SIZEALL = 32646,       // 四向调整大小光标
        OCR_NO = 32648,            // 禁止光标
        OCR_HAND = 32649,          // 手型光标
        OCR_APPSTARTING = 32650    // 应用程序启动光标
    }
}
