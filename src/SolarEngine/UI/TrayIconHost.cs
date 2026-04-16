using System.ComponentModel;
using System.Runtime.InteropServices;
using SolarEngine.Infrastructure.Localization;
using SolarEngine.Shared;

namespace SolarEngine.UI;

internal sealed class TrayIconHost(string appName, AppLocalization localization)
{
    private const string WindowClassName = "SolarEngine.TrayHostWindow";
    private const int OpenCommandId = 1001;
    private const int ApplyNowCommandId = 1002;
    private const int RecalculateCommandId = 1003;
    private const int ExitCommandId = 1004;
    private const uint TrayIconId = 1;

    private static readonly Lock InstancesGate = new();
    private static readonly uint TaskbarCreatedMessage = NativeInterop.RegisterWindowMessage("TaskbarCreated");
    private static readonly Dictionary<nint, TrayIconHost> Instances = [];
    private static readonly NativeInterop.WindowProcedure WindowProcedureDelegate = WindowProcedure;
    private static ushort _windowClassAtom;

    private readonly string _appName = appName;
    private readonly AppLocalization _localization = localization;
    private readonly Lock _tooltipGate = new();
    private bool _disposed;
    private nint _windowHandle;
    private nint _menuHandle;
    private nint _iconHandle;
    private bool _ownsIconHandle;
    private string _currentTooltip = appName;
    private string? _pendingTooltip;
    private NativeInterop.NotifyIconData _notifyIconData;

    public event Action? OpenRequested;
    public event Action? ApplyNowRequested;
    public event Action? RecalculateTodayRequested;
    public event Action? ExitRequested;
    public event Action? PowerResumed;
    public event Action? SessionActivated;
    public event Action? ClockChanged;

    public void Create()
    {
        ThrowIfDisposed();

        try
        {
            RegisterWindowClass();

            _windowHandle = NativeInterop.CreateWindowEx(
                0,
                WindowClassName,
                _appName,
                0,
                0,
                0,
                0,
                0,
                nint.Zero,
                nint.Zero,
                NativeInterop.GetModuleHandle(null),
                nint.Zero);

            if (_windowHandle == nint.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to create the tray host window.");
            }

            lock (InstancesGate)
            {
                Instances[_windowHandle] = this;
            }

            _menuHandle = CreateMenuHandle();
            _iconHandle = NativeInterop.LoadAppIcon(out _ownsIconHandle);
            CreateTrayIcon();

            _ = NativeInterop.WTSRegisterSessionNotification(
                _windowHandle,
                NativeInterop.NOTIFY_FOR_THIS_SESSION);
        }
        catch
        {
            Close();
            throw;
        }
    }

    public void SetTooltip(string tooltip)
    {
        string normalizedTooltip = NormalizeTooltip(tooltip);

        lock (_tooltipGate)
        {
            _pendingTooltip = normalizedTooltip;
        }

        if (_windowHandle != nint.Zero)
        {
            _ = NativeInterop.PostMessage(
                _windowHandle,
                NativeInterop.WM_UPDATE_TOOLTIP,
                nint.Zero,
                nint.Zero);
        }
    }

    public void Close()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_windowHandle != nint.Zero)
        {
            _ = NativeInterop.DestroyWindow(_windowHandle);
        }
        else
        {
            DisposeNativeResources();
        }
    }

    private static nint WindowProcedure(nint hWnd, uint msg, nint wParam, nint lParam)
    {
        lock (InstancesGate)
        {
            if (Instances.TryGetValue(hWnd, out TrayIconHost? host))
            {
                return host.HandleMessage(msg, wParam, lParam);
            }
        }

        return NativeInterop.DefWindowProc(hWnd, msg, wParam, lParam);
    }

    private nint HandleMessage(uint msg, nint wParam, nint lParam)
    {
        if (msg == TaskbarCreatedMessage)
        {
            TryRecreateTrayIcon();
            return 0;
        }

        switch (msg)
        {
            case NativeInterop.WM_COMMAND:
                ExecuteCommand(NativeInterop.LoWord(wParam));
                return 0;

            case NativeInterop.WM_TRAYICON:
                HandleTrayIconMessage((uint)NativeInterop.LoWord(lParam));
                return 0;

            case NativeInterop.WM_UPDATE_TOOLTIP:
                ApplyPendingTooltip();
                return 0;

            case NativeInterop.WM_POWERBROADCAST when wParam.ToInt32() == NativeInterop.PBT_APMRESUMEAUTOMATIC:
                PowerResumed?.Invoke();
                return 0;

            case NativeInterop.WM_TIMECHANGE:
                ClockChanged?.Invoke();
                return 0;

            case NativeInterop.WM_WTSSESSION_CHANGE
                when wParam.ToInt32() is NativeInterop.WTS_SESSION_LOGON or NativeInterop.WTS_SESSION_UNLOCK:
                SessionActivated?.Invoke();
                return 0;

            case NativeInterop.WM_CLOSE:
                _ = NativeInterop.DestroyWindow(_windowHandle);
                return 0;

            case NativeInterop.WM_DESTROY:
                HandleDestroy();
                return 0;

            default:
                return NativeInterop.DefWindowProc(_windowHandle, msg, wParam, lParam);
        }
    }

    private static void RegisterWindowClass()
    {
        if (_windowClassAtom != 0)
        {
            return;
        }

        nint classNamePointer = Marshal.StringToHGlobalUni(WindowClassName);

        try
        {
            NativeInterop.WindowClassEx windowClass = new()
            {
                cbSize = (uint)Marshal.SizeOf<NativeInterop.WindowClassEx>(),
                hInstance = NativeInterop.GetModuleHandle(null),
                lpfnWndProc = Marshal.GetFunctionPointerForDelegate(WindowProcedureDelegate),
                lpszClassName = classNamePointer
            };

            _windowClassAtom = NativeInterop.RegisterClassEx(ref windowClass);
            if (_windowClassAtom == 0)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to register the tray host window class.");
            }
        }
        finally
        {
            Marshal.FreeHGlobal(classNamePointer);
        }
    }

    private nint CreateMenuHandle()
    {
        nint menuHandle = NativeInterop.CreatePopupMenu();
        if (menuHandle == nint.Zero)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to create the tray menu.");
        }

        if (!NativeInterop.AppendMenu(menuHandle, NativeInterop.MF_STRING, OpenCommandId, _localization["tray.open_settings"])
            || !NativeInterop.AppendMenu(menuHandle, NativeInterop.MF_STRING, ApplyNowCommandId, _localization["tray.apply_now"])
            || !NativeInterop.AppendMenu(menuHandle, NativeInterop.MF_STRING, RecalculateCommandId, _localization["tray.recalculate_today"])
            || !NativeInterop.AppendMenu(menuHandle, NativeInterop.MF_SEPARATOR, nint.Zero, null)
            || !NativeInterop.AppendMenu(menuHandle, NativeInterop.MF_STRING, ExitCommandId, _localization["tray.exit"]))
        {
            _ = NativeInterop.DestroyMenu(menuHandle);
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to populate the tray menu.");
        }

        return menuHandle;
    }

    private void CreateTrayIcon()
    {
        _notifyIconData = new NativeInterop.NotifyIconData
        {
            cbSize = Marshal.SizeOf<NativeInterop.NotifyIconData>(),
            hWnd = _windowHandle,
            uID = TrayIconId,
            uFlags = NativeInterop.NIF_MESSAGE | NativeInterop.NIF_ICON | NativeInterop.NIF_TIP,
            uCallbackMessage = NativeInterop.WM_TRAYICON,
            hIcon = _iconHandle,
            szTip = NormalizeTooltip(_currentTooltip),
            szInfo = string.Empty,
            szInfoTitle = string.Empty
        };

        if (!NativeInterop.Shell_NotifyIcon(NativeInterop.NIM_ADD, ref _notifyIconData))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to create the tray icon.");
        }

        _notifyIconData.uTimeoutOrVersion = NativeInterop.NOTIFYICON_VERSION_4;
        _ = NativeInterop.Shell_NotifyIcon(NativeInterop.NIM_SETVERSION, ref _notifyIconData);
    }

    private void TryRecreateTrayIcon()
    {
        if (_windowHandle == nint.Zero)
        {
            return;
        }

        _notifyIconData = new NativeInterop.NotifyIconData
        {
            cbSize = Marshal.SizeOf<NativeInterop.NotifyIconData>(),
            hWnd = _windowHandle,
            uID = TrayIconId,
            uFlags = NativeInterop.NIF_MESSAGE | NativeInterop.NIF_ICON | NativeInterop.NIF_TIP,
            uCallbackMessage = NativeInterop.WM_TRAYICON,
            hIcon = _iconHandle,
            szTip = NormalizeTooltip(_currentTooltip),
            szInfo = string.Empty,
            szInfoTitle = string.Empty
        };

        if (!NativeInterop.Shell_NotifyIcon(NativeInterop.NIM_ADD, ref _notifyIconData))
        {
            return;
        }

        _notifyIconData.uTimeoutOrVersion = NativeInterop.NOTIFYICON_VERSION_4;
        _ = NativeInterop.Shell_NotifyIcon(NativeInterop.NIM_SETVERSION, ref _notifyIconData);
    }

    private void HandleTrayIconMessage(uint message)
    {
        switch (message)
        {
            case NativeInterop.WM_LBUTTONUP:
            case NativeInterop.WM_LBUTTONDBLCLK:
            case NativeInterop.NIN_SELECT:
            case NativeInterop.NIN_KEYSELECT:
                OpenRequested?.Invoke();
                break;

            case NativeInterop.WM_CONTEXTMENU:
            case NativeInterop.WM_RBUTTONUP:
                ShowContextMenu();
                break;
            default:
                break;
        }
    }

    private void ShowContextMenu()
    {
        RebuildMenu();

        _ = NativeInterop.SetForegroundWindow(_windowHandle);

        if (!NativeInterop.GetCursorPos(out NativeInterop.NativePoint cursor))
        {
            return;
        }

        int command = NativeInterop.TrackPopupMenuEx(
            _menuHandle,
            NativeInterop.TPM_LEFTALIGN | NativeInterop.TPM_RETURNCMD | NativeInterop.TPM_NONOTIFY,
            cursor.X,
            cursor.Y,
            _windowHandle,
            nint.Zero);

        if (command != 0)
        {
            ExecuteCommand(command);
        }
    }

    private void RebuildMenu()
    {
        if (_menuHandle != nint.Zero)
        {
            _ = NativeInterop.DestroyMenu(_menuHandle);
        }

        _menuHandle = CreateMenuHandle();
    }

    private void ExecuteCommand(int commandId)
    {
        switch (commandId)
        {
            case OpenCommandId:
                OpenRequested?.Invoke();
                break;

            case ApplyNowCommandId:
                ApplyNowRequested?.Invoke();
                break;

            case RecalculateCommandId:
                RecalculateTodayRequested?.Invoke();
                break;

            case ExitCommandId:
                ExitRequested?.Invoke();
                break;
            default:
                break;
        }
    }

    private void ApplyPendingTooltip()
    {
        string? tooltip;

        lock (_tooltipGate)
        {
            tooltip = _pendingTooltip;
            _pendingTooltip = null;
        }

        if (string.IsNullOrWhiteSpace(tooltip))
        {
            return;
        }

        _currentTooltip = tooltip;
        _notifyIconData.uFlags = NativeInterop.NIF_TIP;
        _notifyIconData.szTip = tooltip;
        _ = NativeInterop.Shell_NotifyIcon(NativeInterop.NIM_MODIFY, ref _notifyIconData);
    }

    private void HandleDestroy()
    {
        _ = NativeInterop.WTSUnRegisterSessionNotification(_windowHandle);

        if (_notifyIconData.hWnd != nint.Zero)
        {
            _ = NativeInterop.Shell_NotifyIcon(NativeInterop.NIM_DELETE, ref _notifyIconData);
            _notifyIconData = default;
        }

        lock (InstancesGate)
        {
            _ = Instances.Remove(_windowHandle);
        }

        _windowHandle = nint.Zero;
        DisposeNativeResources();
        NativeInterop.PostQuitMessage(0);
    }

    private void DisposeNativeResources()
    {
        if (_menuHandle != nint.Zero)
        {
            _ = NativeInterop.DestroyMenu(_menuHandle);
            _menuHandle = nint.Zero;
        }

        if (_ownsIconHandle && _iconHandle != nint.Zero)
        {
            _ = NativeInterop.DestroyIcon(_iconHandle);
        }

        _iconHandle = nint.Zero;
        _ownsIconHandle = false;
    }

    private static string NormalizeTooltip(string tooltip)
    {
        string normalized = string.IsNullOrWhiteSpace(tooltip) ? AppIdentity.RuntimeName : tooltip.Trim();
        return normalized.Length <= 127 ? normalized : normalized[..127];
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
