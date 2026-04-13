using System.Runtime.InteropServices;

namespace SolarEngine.UI;

internal static class NativeInterop
{
    internal delegate nint WindowProcedure(nint hWnd, uint msg, nint wParam, nint lParam);

    internal const int WS_OVERLAPPED = 0x00000000;
    internal const int WS_CAPTION = 0x00C00000;
    internal const int WS_SYSMENU = 0x00080000;
    internal const int WS_THICKFRAME = 0x00040000;
    internal const int WS_MINIMIZEBOX = 0x00020000;
    internal const int WS_MAXIMIZEBOX = 0x00010000;
    internal const int WS_VISIBLE = 0x10000000;
    internal const int WS_CHILD = 0x40000000;
    internal const int WS_CLIPCHILDREN = 0x02000000;
    internal const int WS_BORDER = 0x00800000;
    internal const int WS_TABSTOP = 0x00010000;
    internal const int WS_OVERLAPPEDWINDOW =
        WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX;
    internal const int WS_EX_CLIENTEDGE = 0x00000200;

    internal const int ES_AUTOHSCROLL = 0x0080;
    internal const int BS_DEFPUSHBUTTON = 0x0001;
    internal const int BS_AUTOCHECKBOX = 0x0003;
    internal const int SS_LEFT = 0x0000;

    internal const int SW_HIDE = 0;
    internal const int SW_SHOWNORMAL = 1;
    internal const int SW_SHOW = 5;
    internal const int SW_RESTORE = 9;

    internal const uint WM_CLOSE = 0x0010;
    internal const uint WM_DESTROY = 0x0002;
    internal const uint WM_COMMAND = 0x0111;
    internal const uint WM_CONTEXTMENU = 0x007B;
    internal const uint WM_TIMECHANGE = 0x001E;
    internal const uint WM_SETFONT = 0x0030;
    internal const uint WM_SETICON = 0x0080;
    internal const uint WM_LBUTTONUP = 0x0202;
    internal const uint WM_LBUTTONDBLCLK = 0x0203;
    internal const uint WM_RBUTTONUP = 0x0205;
    internal const uint WM_POWERBROADCAST = 0x0218;
    internal const uint WM_WTSSESSION_CHANGE = 0x02B1;
    internal const uint WM_APP = 0x8000;
    internal const uint WM_TRAYICON = WM_APP + 1;
    internal const uint WM_UPDATE_TOOLTIP = WM_APP + 2;

    internal const uint BM_GETCHECK = 0x00F0;
    internal const uint BM_SETCHECK = 0x00F1;
    internal const uint EM_SETSEL = 0x00B1;
    internal const uint EM_SETLIMITTEXT = 0x00C5;

    internal const int BST_UNCHECKED = 0;
    internal const int BST_CHECKED = 1;
    internal const int BN_CLICKED = 0;

    internal const int ICON_SMALL = 0;
    internal const int ICON_BIG = 1;

    internal const int PBT_APMRESUMEAUTOMATIC = 0x0012;
    internal const int WTS_SESSION_LOGON = 0x5;
    internal const int WTS_SESSION_UNLOCK = 0x8;
    internal const int NOTIFY_FOR_THIS_SESSION = 0;

    internal const uint MF_STRING = 0x0000;
    internal const uint MF_SEPARATOR = 0x0800;
    internal const uint TPM_LEFTALIGN = 0x0000;
    internal const uint TPM_RETURNCMD = 0x0100;
    internal const uint TPM_NONOTIFY = 0x0080;

    internal const uint NIM_ADD = 0x00;
    internal const uint NIM_MODIFY = 0x01;
    internal const uint NIM_DELETE = 0x02;
    internal const uint NIM_SETVERSION = 0x04;
    internal const uint NIF_MESSAGE = 0x01;
    internal const uint NIF_ICON = 0x02;
    internal const uint NIF_TIP = 0x04;
    internal const uint NOTIFYICON_VERSION_4 = 4;
    internal const uint NIN_SELECT = 0x0400;
    internal const uint NIN_KEYSELECT = 0x0401;

    internal const int IDI_APPLICATION = 32512;

    internal const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
    internal const int DWMWA_SYSTEMBACKDROP_TYPE = 38;
    internal const int DWMWCP_ROUND = 2;
    internal const int DWMSBT_MAINWINDOW = 2;

    internal const int MB_OK = 0x0000;
    internal const int MB_ICONINFORMATION = 0x0040;
    internal const int MB_ICONWARNING = 0x0030;
    internal const int MB_ICONERROR = 0x0010;

    internal const int FW_NORMAL = 400;
    internal const int DEFAULT_CHARSET = 1;
    internal const int OUT_DEFAULT_PRECIS = 0;
    internal const int CLIP_DEFAULT_PRECIS = 0;
    internal const int CLEARTYPE_QUALITY = 5;
    internal const int DEFAULT_PITCH = 0;

    [StructLayout(LayoutKind.Sequential)]
    internal struct NativePoint
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeMessage
    {
        public nint hwnd;
        public uint message;
        public nint wParam;
        public nint lParam;
        public uint time;
        public NativePoint pt;
        public uint lPrivate;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct WindowClassEx
    {
        public uint cbSize;
        public uint style;
        public nint lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public nint hInstance;
        public nint hIcon;
        public nint hCursor;
        public nint hbrBackground;
        public nint lpszMenuName;
        public nint lpszClassName;
        public nint hIconSm;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct NotifyIconData
    {
        public int cbSize;
        public nint hWnd;
        public uint uID;
        public uint uFlags;
        public uint uCallbackMessage;
        public nint hIcon;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;

        public uint dwState;
        public uint dwStateMask;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szInfo;

        public uint uTimeoutOrVersion;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string szInfoTitle;

        public uint dwInfoFlags;
        public Guid guidItem;
        public nint hBalloonIcon;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern nint GetModuleHandle(string? lpModuleName);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern ushort RegisterClassEx(ref WindowClassEx windowClass);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern uint RegisterWindowMessage(string lpString);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern nint CreateWindowEx(
        int dwExStyle,
        string lpClassName,
        string? lpWindowName,
        int dwStyle,
        int x,
        int y,
        int nWidth,
        int nHeight,
        nint hWndParent,
        nint hMenu,
        nint hInstance,
        nint lpParam);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern nint DefWindowProc(nint hWnd, uint msg, nint wParam, nint lParam);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool ShowWindow(nint hWnd, int nCmdShow);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool UpdateWindow(nint hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool DestroyWindow(nint hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern int GetMessage(out NativeMessage message, nint hWnd, uint minFilter, uint maxFilter);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool TranslateMessage(ref NativeMessage message);

    [DllImport("user32.dll")]
    internal static extern nint DispatchMessage(ref NativeMessage message);

    [DllImport("user32.dll")]
    internal static extern void PostQuitMessage(int exitCode);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool PostMessage(nint hWnd, uint msg, nint wParam, nint lParam);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern nint SendMessage(nint hWnd, uint msg, nint wParam, nint lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetWindowText(nint hWnd, string text);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern int GetWindowTextLength(nint hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern int GetWindowText(nint hWnd, char[] buffer, int maxCount);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    internal static extern int MessageBox(nint hWnd, string text, string caption, int type);

    [DllImport("user32.dll")]
    internal static extern nint SetFocus(nint hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern nint LoadIcon(nint hInstance, nint iconName);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetForegroundWindow(nint hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetCursorPos(out NativePoint point);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool EnableWindow(nint hWnd, [MarshalAs(UnmanagedType.Bool)] bool enable);

    [DllImport("user32.dll")]
    internal static extern nint CreatePopupMenu();

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool AppendMenu(nint hMenu, uint flags, nint itemId, string? itemText);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern int TrackPopupMenuEx(nint hMenu, uint flags, int x, int y, nint hWnd, nint reserved);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool DestroyMenu(nint hMenu);

    [DllImport("gdi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern nint CreateFont(
        int height,
        int width,
        int escapement,
        int orientation,
        int weight,
        uint italic,
        uint underline,
        uint strikeOut,
        uint charSet,
        uint outPrecision,
        uint clipPrecision,
        uint quality,
        uint pitchAndFamily,
        string faceName);

    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool DeleteObject(nint handle);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool Shell_NotifyIcon(uint dwMessage, ref NotifyIconData data);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern int ExtractIconEx(
        string filePath,
        int iconIndex,
        out nint largeIcon,
        out nint smallIcon,
        int iconCount);

    [DllImport("dwmapi.dll")]
    internal static extern int DwmSetWindowAttribute(nint hWnd, int attribute, ref int value, int valueSize);

    [DllImport("wtsapi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool WTSRegisterSessionNotification(nint hWnd, int flags);

    [DllImport("wtsapi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool WTSUnRegisterSessionNotification(nint hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool DestroyIcon(nint iconHandle);

    internal static int RunMessageLoop()
    {
        while (true)
        {
            int result = GetMessage(out NativeMessage message, nint.Zero, 0, 0);
            if (result == 0)
            {
                return 0;
            }

            if (result == -1)
            {
                return Marshal.GetLastWin32Error();
            }

            _ = TranslateMessage(ref message);
            _ = DispatchMessage(ref message);
        }
    }

    internal static int LoWord(nint value)
    {
        return unchecked((short)(value.ToInt64() & 0xFFFF));
    }

    internal static int HiWord(nint value)
    {
        return unchecked((short)((value.ToInt64() >> 16) & 0xFFFF));
    }

    internal static bool GetChecked(nint hWnd)
    {
        return SendMessage(hWnd, BM_GETCHECK, nint.Zero, nint.Zero) == BST_CHECKED;
    }

    internal static void SetChecked(nint hWnd, bool isChecked)
    {
        _ = SendMessage(hWnd, BM_SETCHECK, isChecked ? BST_CHECKED : BST_UNCHECKED, nint.Zero);
    }

    internal static void ApplyFont(nint hWnd, nint fontHandle)
    {
        if (hWnd == nint.Zero || fontHandle == nint.Zero)
        {
            return;
        }

        _ = SendMessage(hWnd, WM_SETFONT, fontHandle, 1);
    }

    internal static string GetWindowString(nint hWnd)
    {
        int length = GetWindowTextLength(hWnd);
        if (length <= 0)
        {
            return string.Empty;
        }

        char[] buffer = new char[length + 1];
        int copied = GetWindowText(hWnd, buffer, buffer.Length);
        return copied <= 0 ? string.Empty : new string(buffer, 0, copied);
    }

    internal static nint LoadAppIcon(out bool ownsHandle)
    {
        string? processPath = Environment.ProcessPath;
        if (!string.IsNullOrWhiteSpace(processPath))
        {
            int extracted = ExtractIconEx(processPath, 0, out nint largeIcon, out nint smallIcon, 1);
            if (extracted > 0)
            {
                if (smallIcon != nint.Zero)
                {
                    if (largeIcon != nint.Zero)
                    {
                        _ = DestroyIcon(largeIcon);
                    }

                    ownsHandle = true;
                    return smallIcon;
                }

                if (largeIcon != nint.Zero)
                {
                    ownsHandle = true;
                    return largeIcon;
                }
            }
        }

        ownsHandle = false;
        return LoadIcon(nint.Zero, IDI_APPLICATION);
    }

    internal static void ApplyDwmAttributes(nint hWnd)
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000) || hWnd == nint.Zero)
        {
            return;
        }

        int roundedCorners = DWMWCP_ROUND;
        _ = DwmSetWindowAttribute(
            hWnd,
            DWMWA_WINDOW_CORNER_PREFERENCE,
            ref roundedCorners,
            sizeof(int));

        int backdropType = DWMSBT_MAINWINDOW;
        _ = DwmSetWindowAttribute(
            hWnd,
            DWMWA_SYSTEMBACKDROP_TYPE,
            ref backdropType,
            sizeof(int));
    }
}
