using System.Runtime.InteropServices;

namespace SolarEngine.UI;

internal static partial class NativeInterop
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
    internal const int WS_VSCROLL = 0x00200000;
    internal const int WS_OVERLAPPEDWINDOW =
        WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX;
    internal const int WS_EX_CLIENTEDGE = 0x00000200;

    internal const int ES_AUTOHSCROLL = 0x0080;
    internal const int BS_DEFPUSHBUTTON = 0x0001;
    internal const int BS_AUTOCHECKBOX = 0x0003;
    internal const int CBS_DROPDOWNLIST = 0x0003;
    internal const int SS_LEFT = 0x0000;

    internal const int SW_HIDE = 0;
    internal const int SW_SHOWNORMAL = 1;
    internal const int SW_SHOW = 5;
    internal const int SW_RESTORE = 9;

    internal const uint WM_CLOSE = 0x0010;
    internal const uint WM_DESTROY = 0x0002;
    internal const uint WM_COMMAND = 0x0111;
    internal const uint WM_CONTEXTMENU = 0x007B;
    internal const uint WM_ERASEBKGND = 0x0014;
    internal const uint WM_TIMECHANGE = 0x001E;
    internal const uint WM_SETFONT = 0x0030;
    internal const uint WM_SETICON = 0x0080;
    internal const uint WM_CTLCOLOREDIT = 0x0133;
    internal const uint WM_CTLCOLORLISTBOX = 0x0134;
    internal const uint WM_CTLCOLORBTN = 0x0135;
    internal const uint WM_CTLCOLORSTATIC = 0x0138;
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
    internal const uint EM_SETPASSWORDCHAR = 0x00CC;
    internal const uint CB_ADDSTRING = 0x0143;
    internal const uint CB_GETCURSEL = 0x0147;
    internal const uint CB_RESETCONTENT = 0x014B;
    internal const uint CB_SETCURSEL = 0x014E;

    internal const int BST_UNCHECKED = 0;
    internal const int BST_CHECKED = 1;
    internal const int BN_CLICKED = 0;
    internal const int CBN_SELCHANGE = 1;
    internal const int EN_SETFOCUS = 0x0100;
    internal const int EN_KILLFOCUS = 0x0200;
    internal const int CB_ERR = -1;

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
    internal const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    internal const int DWMWCP_ROUND = 2;

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
    internal const int COLOR_WINDOW = 5;
    private const int MessageLoopFilterValue = 0;
    private const int MessageLoopExitResult = 0;
    private const int MessageLoopFailureResult = -1;
    private const long WordMask = 0xFFFF;
    private const int WordShift = 16;
    private const int BufferTerminatorLength = 1;
    private const int BufferStartIndex = 0;
    private const int FontRedrawEnabled = 1;
    private const int FirstIconIndex = 0;
    private const int SingleIconRequestCount = 1;
    private const int Windows10MajorVersion = 10;
    private const int WindowsVersionMinor = 0;
    private const int ImmersiveDarkModeBuild = 17763;
    private const int RoundedCornersBuild = 22000;
    private const int DarkModeEnabled = 1;
    private const int DarkModeDisabled = 0;
    private const int SuccessfulExtractionThreshold = 0;

    [StructLayout(LayoutKind.Sequential)]
    internal struct NativePoint
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
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

    [LibraryImport("kernel32.dll", EntryPoint = "GetModuleHandleW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    internal static partial nint GetModuleHandle(string? lpModuleName);

    [LibraryImport("user32.dll", EntryPoint = "RegisterClassExW", SetLastError = true)]
    internal static partial ushort RegisterClassEx(ref WindowClassEx windowClass);

    [LibraryImport("user32.dll", EntryPoint = "RegisterWindowMessageW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    internal static partial uint RegisterWindowMessage(string lpString);

    [LibraryImport("user32.dll", EntryPoint = "CreateWindowExW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    internal static partial nint CreateWindowEx(
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

    [LibraryImport("user32.dll", EntryPoint = "DefWindowProcW", SetLastError = true)]
    internal static partial nint DefWindowProc(nint hWnd, uint msg, nint wParam, nint lParam);

    [LibraryImport("user32.dll", EntryPoint = "ShowWindow", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool ShowWindow(nint hWnd, int nCmdShow);

    [LibraryImport("user32.dll", EntryPoint = "UpdateWindow", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool UpdateWindow(nint hWnd);

    [LibraryImport("user32.dll", EntryPoint = "DestroyWindow", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool DestroyWindow(nint hWnd);

    [LibraryImport("user32.dll", EntryPoint = "GetMessageW", SetLastError = true)]
    internal static partial int GetMessage(out NativeMessage message, nint hWnd, uint minFilter, uint maxFilter);

    [LibraryImport("user32.dll", EntryPoint = "TranslateMessage")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool TranslateMessage(ref NativeMessage message);

    [LibraryImport("user32.dll", EntryPoint = "DispatchMessageW")]
    internal static partial nint DispatchMessage(ref NativeMessage message);

    [LibraryImport("user32.dll", EntryPoint = "PostQuitMessage")]
    internal static partial void PostQuitMessage(int exitCode);

    [LibraryImport("user32.dll", EntryPoint = "PostMessageW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool PostMessage(nint hWnd, uint msg, nint wParam, nint lParam);

    [LibraryImport("user32.dll", EntryPoint = "SendMessageW", SetLastError = true)]
    internal static partial nint SendMessage(nint hWnd, uint msg, nint wParam, nint lParam);

    [LibraryImport("user32.dll", EntryPoint = "SendMessageW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    internal static partial nint SendMessage(nint hWnd, uint msg, nint wParam, string lParam);

    [LibraryImport("user32.dll", EntryPoint = "SetWindowTextW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool SetWindowText(nint hWnd, string text);

    [LibraryImport("user32.dll", EntryPoint = "GetWindowTextLengthW", SetLastError = true)]
    internal static partial int GetWindowTextLength(nint hWnd);

    [LibraryImport("user32.dll", EntryPoint = "GetWindowTextW", SetLastError = true)]
    internal static partial int GetWindowText(nint hWnd, [Out] char[] buffer, int maxCount);

    [LibraryImport("user32.dll", EntryPoint = "MessageBoxW", StringMarshalling = StringMarshalling.Utf16)]
    internal static partial int MessageBox(nint hWnd, string text, string caption, int type);

    [LibraryImport("user32.dll", EntryPoint = "SetFocus")]
    internal static partial nint SetFocus(nint hWnd);

    [LibraryImport("user32.dll", EntryPoint = "GetFocus")]
    internal static partial nint GetFocus();

    [LibraryImport("user32.dll", EntryPoint = "LoadIconW", SetLastError = true)]
    internal static partial nint LoadIcon(nint hInstance, nint iconName);

    [LibraryImport("user32.dll", EntryPoint = "SetForegroundWindow")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool SetForegroundWindow(nint hWnd);

    [LibraryImport("user32.dll", EntryPoint = "GetCursorPos")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetCursorPos(out NativePoint point);

    [LibraryImport("user32.dll", EntryPoint = "EnableWindow")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool EnableWindow(nint hWnd, [MarshalAs(UnmanagedType.Bool)] bool enable);

    [LibraryImport("user32.dll", EntryPoint = "CreatePopupMenu")]
    internal static partial nint CreatePopupMenu();

    [LibraryImport("user32.dll", EntryPoint = "AppendMenuW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool AppendMenu(nint hMenu, uint flags, nint itemId, string? itemText);

    [LibraryImport("user32.dll", EntryPoint = "TrackPopupMenuEx", SetLastError = true)]
    internal static partial int TrackPopupMenuEx(nint hMenu, uint flags, int x, int y, nint hWnd, nint reserved);

    [LibraryImport("user32.dll", EntryPoint = "DestroyMenu")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool DestroyMenu(nint hMenu);

    [LibraryImport("gdi32.dll", EntryPoint = "CreateFontW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    internal static partial nint CreateFont(
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

    [LibraryImport("gdi32.dll", EntryPoint = "DeleteObject")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool DeleteObject(nint handle);

    [LibraryImport("gdi32.dll", EntryPoint = "SetTextColor")]
    internal static partial int SetTextColor(nint hdc, int colorRef);

    [LibraryImport("gdi32.dll", EntryPoint = "SetBkColor")]
    internal static partial int SetBkColor(nint hdc, int colorRef);

    [LibraryImport("gdi32.dll", EntryPoint = "CreateSolidBrush")]
    internal static partial nint CreateSolidBrush(int colorRef);

    [LibraryImport("shell32.dll", EntryPoint = "Shell_NotifyIconW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool Shell_NotifyIcon(uint dwMessage, ref NotifyIconData data);

    [LibraryImport("shell32.dll", EntryPoint = "ExtractIconExW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    internal static partial int ExtractIconEx(
        string filePath,
        int iconIndex,
        out nint largeIcon,
        out nint smallIcon,
        int iconCount);

    [LibraryImport("dwmapi.dll", EntryPoint = "DwmSetWindowAttribute")]
    internal static partial int DwmSetWindowAttribute(nint hWnd, int attribute, ref int value, int valueSize);

    [LibraryImport("wtsapi32.dll", EntryPoint = "WTSRegisterSessionNotification")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool WTSRegisterSessionNotification(nint hWnd, int flags);

    [LibraryImport("wtsapi32.dll", EntryPoint = "WTSUnRegisterSessionNotification")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool WTSUnRegisterSessionNotification(nint hWnd);

    [LibraryImport("user32.dll", EntryPoint = "DestroyIcon")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool DestroyIcon(nint iconHandle);

    [LibraryImport("user32.dll", EntryPoint = "CopyIcon", SetLastError = true)]
    internal static partial nint CopyIcon(nint iconHandle);

    [LibraryImport("user32.dll", EntryPoint = "GetClientRect")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetClientRect(nint hWnd, out NativeRect rect);

    [LibraryImport("user32.dll", EntryPoint = "FillRect")]
    internal static partial int FillRect(nint hdc, ref NativeRect rect, nint brushHandle);

    [LibraryImport("user32.dll", EntryPoint = "InvalidateRect")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool InvalidateRect(nint hWnd, nint rect, [MarshalAs(UnmanagedType.Bool)] bool erase);

    internal static int RunMessageLoop()
    {
        while (true)
        {
            int result = GetMessage(out NativeMessage message, nint.Zero, MessageLoopFilterValue, MessageLoopFilterValue);
            if (result == MessageLoopExitResult)
            {
                return MessageLoopExitResult;
            }

            if (result == MessageLoopFailureResult)
            {
                return Marshal.GetLastWin32Error();
            }

            _ = TranslateMessage(ref message);
            _ = DispatchMessage(ref message);
        }
    }

    internal static int LoWord(nint value)
    {
        return unchecked((short)(value.ToInt64() & WordMask));
    }

    internal static int HiWord(nint value)
    {
        return unchecked((short)((value.ToInt64() >> WordShift) & WordMask));
    }

    internal static bool GetChecked(nint hWnd)
    {
        return SendMessage(hWnd, BM_GETCHECK, nint.Zero, nint.Zero) == BST_CHECKED;
    }

    internal static void SetChecked(nint hWnd, bool isChecked)
    {
        _ = SendMessage(hWnd, BM_SETCHECK, isChecked ? BST_CHECKED : BST_UNCHECKED, nint.Zero);
    }

    internal static string GetWindowString(nint hWnd)
    {
        int length = GetWindowTextLength(hWnd);
        if (length <= MessageLoopFilterValue)
        {
            return string.Empty;
        }

        char[] buffer = new char[length + BufferTerminatorLength];
        int copied = GetWindowText(hWnd, buffer, buffer.Length);
        return copied <= MessageLoopFilterValue ? string.Empty : new string(buffer, BufferStartIndex, copied);
    }

    internal static void SetPasswordCharacter(nint hWnd, char passwordCharacter)
    {
        _ = SendMessage(hWnd, EM_SETPASSWORDCHAR, passwordCharacter, nint.Zero);
        _ = InvalidateRect(hWnd, nint.Zero, erase: true);
    }

    internal static void ResetComboBoxContent(nint hWnd)
    {
        _ = SendMessage(hWnd, CB_RESETCONTENT, nint.Zero, nint.Zero);
    }

    internal static void AddComboBoxString(nint hWnd, string text)
    {
        _ = SendMessage(hWnd, CB_ADDSTRING, nint.Zero, text);
    }

    internal static void SetComboSelection(nint hWnd, int index)
    {
        _ = SendMessage(hWnd, CB_SETCURSEL, index, nint.Zero);
    }

    internal static int GetComboSelection(nint hWnd)
    {
        return unchecked((int)SendMessage(hWnd, CB_GETCURSEL, nint.Zero, nint.Zero));
    }

    internal static nint GetHandleOrZero(SafeHandle? handle)
    {
        return handle is null || handle.IsInvalid || handle.IsClosed
            ? nint.Zero
            : handle.DangerousGetHandle();
    }

    internal static void ApplyFont(nint hWnd, SafeHandle? fontHandle)
    {
        nint nativeHandle = GetHandleOrZero(fontHandle);
        if (hWnd == nint.Zero || nativeHandle == nint.Zero)
        {
            return;
        }

        _ = SendMessage(hWnd, WM_SETFONT, nativeHandle, FontRedrawEnabled);
    }

    internal static SafeGdiObjectHandle CreateFontHandle(
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
        string faceName)
    {
        return SafeGdiObjectHandle.FromHandle(
            CreateFont(
                height,
                width,
                escapement,
                orientation,
                weight,
                italic,
                underline,
                strikeOut,
                charSet,
                outPrecision,
                clipPrecision,
                quality,
                pitchAndFamily,
                faceName));
    }

    internal static SafeGdiObjectHandle CreateSolidBrushHandle(int colorRef)
    {
        return SafeGdiObjectHandle.FromHandle(CreateSolidBrush(colorRef));
    }

    internal static SafeIconHandle LoadAppIcon()
    {
        string? processPath = Environment.ProcessPath;
        if (!string.IsNullOrWhiteSpace(processPath))
        {
            int extracted = ExtractIconEx(processPath, FirstIconIndex, out nint largeIcon, out nint smallIcon, SingleIconRequestCount);
            if (extracted > SuccessfulExtractionThreshold)
            {
                if (smallIcon != nint.Zero)
                {
                    if (largeIcon != nint.Zero)
                    {
                        _ = DestroyIcon(largeIcon);
                    }

                    return SafeIconHandle.FromHandle(smallIcon);
                }

                if (largeIcon != nint.Zero)
                {
                    return SafeIconHandle.FromHandle(largeIcon);
                }
            }
        }

        nint fallbackIconHandle = LoadIcon(nint.Zero, IDI_APPLICATION);
        if (fallbackIconHandle == nint.Zero)
        {
            return SafeIconHandle.FromHandle(nint.Zero);
        }

        nint copiedIconHandle = CopyIcon(fallbackIconHandle);
        return SafeIconHandle.FromHandle(copiedIconHandle);
    }

    internal static void ApplyDwmAttributes(nint hWnd, bool useDarkMode = false)
    {
        if (hWnd == nint.Zero)
        {
            return;
        }

        if (OperatingSystem.IsWindowsVersionAtLeast(Windows10MajorVersion, WindowsVersionMinor, ImmersiveDarkModeBuild))
        {
            int darkMode = useDarkMode ? DarkModeEnabled : DarkModeDisabled;
            _ = DwmSetWindowAttribute(
                hWnd,
                DWMWA_USE_IMMERSIVE_DARK_MODE,
                ref darkMode,
                sizeof(int));
        }

        if (OperatingSystem.IsWindowsVersionAtLeast(Windows10MajorVersion, WindowsVersionMinor, RoundedCornersBuild))
        {
            int roundedCorners = DWMWCP_ROUND;
            _ = DwmSetWindowAttribute(
                hWnd,
                DWMWA_WINDOW_CORNER_PREFERENCE,
                ref roundedCorners,
                sizeof(int));
        }
    }
}
