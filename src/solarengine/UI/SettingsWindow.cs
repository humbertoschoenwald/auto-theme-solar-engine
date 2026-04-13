using System.Collections.Concurrent;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using SolarEngine.Features.Locations.Domain;
using SolarEngine.Features.SystemHost;
using SolarEngine.Features.SystemHost.Domain;
using SolarEngine.Features.SystemHost.Infrastructure;
using SolarEngine.Shared.Core;

namespace SolarEngine.UI;

internal sealed class SettingsWindow(ApplicationLifecycleOrchestrator applicationLifecycleOrchestrator)
{
    private readonly record struct ControlBounds(int X, int Y, int Width, int Height);

    private const string AppName = "Auto Theme Solar Engine";
    private const string WindowClassName = "SolarEngine.NativeSettingsWindow";
    private const int WindowWidth = 460;
    private const int WindowHeight = 500;

    private const int LatitudeEditId = 101;
    private const int LongitudeEditId = 102;
    private const int UseWindowsLocationId = 103;
    private const int DetectLocationId = 104;
    private const int StartWithWindowsId = 105;
    private const int StartMinimizedId = 106;
    private const int HighPriorityId = 107;
    private const int ApplyNowId = 108;
    private const int PrecisionEditId = 111;
    private const uint WM_PROCESS_UI_ACTIONS = NativeInterop.WM_APP + 100;

    private static readonly Lock InstancesGate = new();
    private static readonly Dictionary<nint, SettingsWindow> Instances = [];
    private static readonly NativeInterop.WindowProcedure WindowProcedureDelegate = WindowProcedure;
    private static ushort _windowClassAtom;

    private readonly ApplicationLifecycleOrchestrator _applicationLifecycleOrchestrator = applicationLifecycleOrchestrator;
    private readonly ConcurrentQueue<Action> _pendingUiActions = [];
    private bool _disposed;
    private int _operationInProgress;
    private nint _windowHandle;
    private nint _fontHandle;
    private nint _windowIconHandle;
    private bool _ownsWindowIconHandle;
    private nint _latitudeEditHandle;
    private nint _longitudeEditHandle;
    private nint _precisionEditHandle;
    private nint _useWindowsLocationHandle;
    private nint _detectLocationButtonHandle;
    private nint _startWithWindowsHandle;
    private nint _startMinimizedHandle;
    private nint _highPriorityHandle;
    private nint _todayScheduleHandle;
    private nint _runtimeStatusHandle;
    private nint _applyNowButtonHandle;

    public void ShowFromTray()
    {
        ThrowIfDisposed();
        EnsureCreated();

        _ = NativeInterop.ShowWindow(_windowHandle, NativeInterop.SW_RESTORE);
        _ = NativeInterop.ShowWindow(_windowHandle, NativeInterop.SW_SHOW);
        _ = NativeInterop.UpdateWindow(_windowHandle);
        RefreshFromModel();
        _ = NativeInterop.SetForegroundWindow(_windowHandle);
        _ = NativeInterop.SetFocus(_latitudeEditHandle);
    }

    public void RefreshFromModel()
    {
        if (_windowHandle == nint.Zero)
        {
            return;
        }

        LoadConfiguration();
        RefreshStatus();
    }

    public void RequestRefresh()
    {
        if (_windowHandle == nint.Zero || _disposed)
        {
            return;
        }

        EnqueueUiAction(RefreshFromModel);
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
            if (Instances.TryGetValue(hWnd, out SettingsWindow? window))
            {
                return window.HandleMessage(msg, wParam, lParam);
            }
        }

        return NativeInterop.DefWindowProc(hWnd, msg, wParam, lParam);
    }

    private nint HandleMessage(uint msg, nint wParam, nint lParam)
    {
        switch (msg)
        {
            case WM_PROCESS_UI_ACTIONS:
                DrainUiActions();
                return 0;

            case NativeInterop.WM_COMMAND when NativeInterop.HiWord(wParam) == NativeInterop.BN_CLICKED:
                HandleCommand(NativeInterop.LoWord(wParam));
                return 0;

            case NativeInterop.WM_CLOSE:
                HideToTray();
                return 0;

            case NativeInterop.WM_DESTROY:
                HandleDestroy();
                return 0;

            default:
                return NativeInterop.DefWindowProc(_windowHandle, msg, wParam, lParam);
        }
    }

    private void EnsureCreated()
    {
        if (_windowHandle != nint.Zero)
        {
            return;
        }

        RegisterWindowClass();

        _windowHandle = NativeInterop.CreateWindowEx(
            0,
            WindowClassName,
            AppName,
            NativeInterop.WS_CAPTION
            | NativeInterop.WS_SYSMENU
            | NativeInterop.WS_MINIMIZEBOX
            | NativeInterop.WS_CLIPCHILDREN,
            220,
            160,
            WindowWidth,
            WindowHeight,
            nint.Zero,
            nint.Zero,
            NativeInterop.GetModuleHandle(null),
            nint.Zero);

        if (_windowHandle == nint.Zero)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to create the settings window.");
        }

        lock (InstancesGate)
        {
            Instances[_windowHandle] = this;
        }

        _fontHandle = NativeInterop.CreateFont(
            -16,
            0,
            0,
            0,
            NativeInterop.FW_NORMAL,
            0,
            0,
            0,
            NativeInterop.DEFAULT_CHARSET,
            NativeInterop.OUT_DEFAULT_PRECIS,
            NativeInterop.CLIP_DEFAULT_PRECIS,
            NativeInterop.CLEARTYPE_QUALITY,
            NativeInterop.DEFAULT_PITCH,
            "Segoe UI");

        _windowIconHandle = NativeInterop.LoadAppIcon(out _ownsWindowIconHandle);
        if (_windowIconHandle != nint.Zero)
        {
            _ = NativeInterop.SendMessage(_windowHandle, NativeInterop.WM_SETICON, NativeInterop.ICON_SMALL, _windowIconHandle);
            _ = NativeInterop.SendMessage(_windowHandle, NativeInterop.WM_SETICON, NativeInterop.ICON_BIG, _windowIconHandle);
        }

        CreateControls();
        NativeInterop.ApplyDwmAttributes(_windowHandle);
        RefreshFromModel();
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
                hbrBackground = NativeInterop.COLOR_WINDOW + 1,
                lpszClassName = classNamePointer
            };

            _windowClassAtom = NativeInterop.RegisterClassEx(ref windowClass);
            if (_windowClassAtom == 0)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to register the settings window class.");
            }
        }
        finally
        {
            Marshal.FreeHGlobal(classNamePointer);
        }
    }

    private void CreateControls()
    {
        _ = CreateLabel("Auto Theme Solar Engine settings", 16, 16, 400, 20);

        _ = CreateLabel("Latitude", 16, 52, 100, 20);
        _latitudeEditHandle = CreateEdit(LatitudeEditId, 140, 48, 260, 24);

        _ = CreateLabel("Longitude", 16, 86, 100, 20);
        _longitudeEditHandle = CreateEdit(LongitudeEditId, 140, 82, 260, 24);

        _ = CreateLabel("Precision", 16, 120, 100, 20);
        _precisionEditHandle = CreateEdit(PrecisionEditId, 140, 116, 40, 24, 1);
        _ = CreateLabel("2-5 decimals | lower = more private", 188, 120, 212, 20);

        _useWindowsLocationHandle = CreateCheckBox(
            UseWindowsLocationId,
            "Use Windows location",
            16,
            154,
            200,
            20);

        _detectLocationButtonHandle = CreateButton(
            DetectLocationId,
            "Detect from Windows",
            232,
            150,
            168,
            28,
            false);

        _startWithWindowsHandle = CreateCheckBox(
            StartWithWindowsId,
            "Start with Windows",
            16,
            192,
            200,
            20);

        _startMinimizedHandle = CreateCheckBox(
            StartMinimizedId,
            "Open in tray",
            16,
            220,
            200,
            20);

        _highPriorityHandle = CreateCheckBox(
            HighPriorityId,
            "Use high process priority",
            16,
            248,
            220,
            20);

        _ = CreateLabel("Today's schedule", 16, 290, 140, 20);
        _todayScheduleHandle = CreateLabel(string.Empty, 16, 314, 384, 34);

        _ = CreateLabel("Runtime status", 16, 358, 140, 20);
        _runtimeStatusHandle = CreateLabel(string.Empty, 16, 382, 384, 34);

        _applyNowButtonHandle = CreateButton(ApplyNowId, "Save and apply", 16, 430, 140, 30, true);
    }

    private nint CreateLabel(string text, int x, int y, int width, int height)
    {
        return CreateControl(
            "STATIC",
            text,
            NativeInterop.SS_LEFT,
            0,
            new ControlBounds(x, y, width, height),
            0);
    }

    private nint CreateEdit(int controlId, int x, int y, int width, int height, int maxCharacters = 18)
    {
        nint editHandle = CreateControl(
            "EDIT",
            string.Empty,
            NativeInterop.ES_AUTOHSCROLL | NativeInterop.WS_TABSTOP,
            NativeInterop.WS_EX_CLIENTEDGE,
            new ControlBounds(x, y, width, height),
            controlId);

        _ = NativeInterop.SendMessage(editHandle, NativeInterop.EM_SETLIMITTEXT, maxCharacters, nint.Zero);
        return editHandle;
    }

    private nint CreateCheckBox(int controlId, string text, int x, int y, int width, int height)
    {
        return CreateControl(
            "BUTTON",
            text,
            NativeInterop.BS_AUTOCHECKBOX | NativeInterop.WS_TABSTOP,
            0,
            new ControlBounds(x, y, width, height),
            controlId);
    }

    private nint CreateButton(
        int controlId,
        string text,
        int x,
        int y,
        int width,
        int height,
        bool isDefault)
    {
        int style = NativeInterop.WS_TABSTOP;
        if (isDefault)
        {
            style |= NativeInterop.BS_DEFPUSHBUTTON;
        }

        return CreateControl("BUTTON", text, style, 0, new ControlBounds(x, y, width, height), controlId);
    }

    private nint CreateControl(
        string className,
        string text,
        int style,
        int extendedStyle,
        ControlBounds bounds,
        int controlId)
    {
        nint controlHandle = NativeInterop.CreateWindowEx(
            extendedStyle,
            className,
            text,
            NativeInterop.WS_CHILD | NativeInterop.WS_VISIBLE | style,
            bounds.X,
            bounds.Y,
            bounds.Width,
            bounds.Height,
            _windowHandle,
            controlId,
            NativeInterop.GetModuleHandle(null),
            nint.Zero);

        if (controlHandle == nint.Zero)
        {
            throw new Win32Exception(
                Marshal.GetLastWin32Error(),
                $"Failed to create a {className} control.");
        }

        NativeInterop.ApplyFont(controlHandle, _fontHandle);
        return controlHandle;
    }

    private void HandleCommand(int controlId)
    {
        switch (controlId)
        {
            case DetectLocationId:
                StartDetectLocation();
                break;

            case ApplyNowId:
                StartApplyNow();
                break;
            default:
                break;
        }
    }

    private void StartApplyNow()
    {
        Result<AppConfig> configurationResult = ReadConfigurationFromForm();
        if (configurationResult.IsFailure)
        {
            ShowValidationError(configurationResult.Error);
            return;
        }

        if (!TryBeginOperation("Applying theme..."))
        {
            return;
        }

        _ = ApplyNowAsync(configurationResult.Value);
    }

    private async Task ApplyNowAsync(AppConfig configuration)
    {
        try
        {
            await _applicationLifecycleOrchestrator.SaveAsync(configuration).ConfigureAwait(false);
            await _applicationLifecycleOrchestrator.ApplyCurrentThemeAsync().ConfigureAwait(false);

            EnqueueUiAction(() =>
            {
                RefreshFromModel();
                ShowMessage("Theme applied.", NativeInterop.MB_ICONINFORMATION);
            });
        }
        catch (OperationCanceledException)
        {
            // Closing or disposing the app can cancel an in-flight apply after the background work already started.
        }
        catch (Exception exception)
        {
            EnqueueUiAction(() => ShowMessage(exception.Message, NativeInterop.MB_ICONERROR));
        }
        finally
        {
            EnqueueUiAction(CompleteOperation);
        }
    }

    private void StartDetectLocation()
    {
        Result<int> locationPrecisionResult = ParseLocationPrecisionFromForm();
        if (locationPrecisionResult.IsFailure)
        {
            ShowValidationError(locationPrecisionResult.Error);
            return;
        }

        if (!TryBeginOperation("Detecting location..."))
        {
            return;
        }

        _ = DetectLocationAsync(locationPrecisionResult.Value);
    }

    private async Task DetectLocationAsync(int locationPrecisionDecimals)
    {
        try
        {
            Result<GeoCoordinates> coordinatesResult =
                await _applicationLifecycleOrchestrator.DetectCoordinatesAsync().ConfigureAwait(false);

            EnqueueUiAction(() =>
            {
                if (coordinatesResult.IsFailure)
                {
                    ShowMessage(coordinatesResult.Error.Description, NativeInterop.MB_ICONWARNING);
                    return;
                }

                GeoCoordinates coordinates = CoordinatePrecisionPolicy.Reduce(
                    coordinatesResult.Value,
                    locationPrecisionDecimals);
                _ = NativeInterop.SetWindowText(
                    _latitudeEditHandle,
                    CoordinatePrecisionPolicy.Format(
                        coordinates.Latitude,
                        locationPrecisionDecimals));
                _ = NativeInterop.SetWindowText(
                    _longitudeEditHandle,
                    CoordinatePrecisionPolicy.Format(
                        coordinates.Longitude,
                        locationPrecisionDecimals));
                NativeInterop.SetChecked(_useWindowsLocationHandle, true);
                RefreshStatus();
            });
        }
        catch (OperationCanceledException)
        {
            // Location prompts can be canceled externally; the settings window should simply return to idle.
        }
        catch (Exception exception)
        {
            EnqueueUiAction(() => ShowMessage(exception.Message, NativeInterop.MB_ICONERROR));
        }
        finally
        {
            EnqueueUiAction(CompleteOperation);
        }
    }

    private bool TryBeginOperation(string statusText)
    {
        if (Interlocked.CompareExchange(ref _operationInProgress, 1, 0) != 0)
        {
            ShowMessage("Another operation is already running.", NativeInterop.MB_ICONWARNING);
            return false;
        }

        SetBusy(isBusy: true, statusText);
        return true;
    }

    private void CompleteOperation()
    {
        _ = Interlocked.Exchange(ref _operationInProgress, 0);
        SetBusy(isBusy: false, statusText: null);
    }

    private void SetBusy(bool isBusy, string? statusText)
    {
        _ = NativeInterop.EnableWindow(_detectLocationButtonHandle, !isBusy);
        _ = NativeInterop.EnableWindow(_applyNowButtonHandle, !isBusy);

        if (isBusy && !string.IsNullOrWhiteSpace(statusText))
        {
            _ = NativeInterop.SetWindowText(_runtimeStatusHandle, statusText);
            return;
        }

        RefreshStatus();
    }

    private void EnqueueUiAction(Action action)
    {
        if (_disposed || _windowHandle == nint.Zero)
        {
            return;
        }

        _pendingUiActions.Enqueue(action);
        _ = NativeInterop.PostMessage(_windowHandle, WM_PROCESS_UI_ACTIONS, nint.Zero, nint.Zero);
    }

    private void DrainUiActions()
    {
        while (_pendingUiActions.TryDequeue(out Action? action))
        {
            if (_disposed)
            {
                break;
            }

            action();
        }
    }

    private void HideToTray()
    {
        if (_windowHandle == nint.Zero)
        {
            return;
        }

        _ = NativeInterop.ShowWindow(_windowHandle, NativeInterop.SW_HIDE);
        GcPressureMonitor.ScheduleCompaction();
    }

    private void LoadConfiguration()
    {
        AppConfig configuration = _applicationLifecycleOrchestrator.Config;
        bool showStoredCoordinates = configuration.IsConfigured && !configuration.UseWindowsLocation;

        _ = NativeInterop.SetWindowText(
            _latitudeEditHandle,
            showStoredCoordinates
                ? CoordinatePrecisionPolicy.Format(
                    configuration.Latitude,
                    configuration.LocationPrecisionDecimals)
                : string.Empty);

        _ = NativeInterop.SetWindowText(
            _longitudeEditHandle,
            showStoredCoordinates
                ? CoordinatePrecisionPolicy.Format(
                    configuration.Longitude,
                    configuration.LocationPrecisionDecimals)
                : string.Empty);

        _ = NativeInterop.SetWindowText(
            _precisionEditHandle,
            configuration.LocationPrecisionDecimals.ToString(CultureInfo.InvariantCulture));
        NativeInterop.SetChecked(_useWindowsLocationHandle, configuration.UseWindowsLocation);
        NativeInterop.SetChecked(_startWithWindowsHandle, configuration.StartWithWindows);
        NativeInterop.SetChecked(_startMinimizedHandle, configuration.StartMinimized);
        NativeInterop.SetChecked(_highPriorityHandle, configuration.UseHighPriority);
    }

    private Result<AppConfig> ReadConfigurationFromForm()
    {
        bool useWindowsLocation = NativeInterop.GetChecked(_useWindowsLocationHandle);
        AppConfig currentConfiguration = _applicationLifecycleOrchestrator.Config;
        Result<int> locationPrecisionResult = ParseLocationPrecisionFromForm();
        if (locationPrecisionResult.IsFailure)
        {
            return Result<AppConfig>.Failure(locationPrecisionResult.Error);
        }

        int locationPrecisionDecimals = locationPrecisionResult.Value;

        if (useWindowsLocation)
        {
            Result<GeoCoordinates> windowsCoordinatesResult =
                ResolveCoordinatesForWindowsLocation(currentConfiguration);

            return windowsCoordinatesResult.IsFailure
                ? Result<AppConfig>.Failure(windowsCoordinatesResult.Error)
                : (Result<AppConfig>)BuildConfiguration(
                windowsCoordinatesResult.Value,
                useWindowsLocation,
                locationPrecisionDecimals);
        }

        Result<GeoCoordinates> manualCoordinatesResult = ParseCoordinatesFromText();
        return manualCoordinatesResult.IsFailure
            ? Result<AppConfig>.Failure(manualCoordinatesResult.Error)
            : (Result<AppConfig>)BuildConfiguration(
            manualCoordinatesResult.Value,
            useWindowsLocation,
            locationPrecisionDecimals);
    }

    private Result<GeoCoordinates> ResolveCoordinatesForWindowsLocation(AppConfig currentConfiguration)
    {
        string latitudeText = NativeInterop.GetWindowString(_latitudeEditHandle);
        string longitudeText = NativeInterop.GetWindowString(_longitudeEditHandle);

        return !string.IsNullOrWhiteSpace(latitudeText) || !string.IsNullOrWhiteSpace(longitudeText)
            ? ParseCoordinates(latitudeText, longitudeText)
            : currentConfiguration.IsConfigured
            ? GeoCoordinates.Create(currentConfiguration.Latitude, currentConfiguration.Longitude)
            : Result<GeoCoordinates>.Failure(
            Error.Validation(
                "MissingLocationSeed",
                "Detect coordinates or enter manual coordinates before enabling Windows location."));
    }

    private Result<GeoCoordinates> ParseCoordinatesFromText()
    {
        return ParseCoordinates(
            NativeInterop.GetWindowString(_latitudeEditHandle),
            NativeInterop.GetWindowString(_longitudeEditHandle));
    }

    private static Result<GeoCoordinates> ParseCoordinates(string latitudeText, string longitudeText)
    {
        return !double.TryParse(
                latitudeText,
                NumberStyles.Float | NumberStyles.AllowLeadingSign,
                CultureInfo.InvariantCulture,
                out double latitude)
            ? Result<GeoCoordinates>.Failure(
                Error.Validation("InvalidLatitude", "Provide a valid decimal latitude."))
            : !double.TryParse(
                longitudeText,
                NumberStyles.Float | NumberStyles.AllowLeadingSign,
                CultureInfo.InvariantCulture,
                out double longitude)
            ? Result<GeoCoordinates>.Failure(
                Error.Validation("InvalidLongitude", "Provide a valid decimal longitude."))
            : GeoCoordinates.Create(latitude, longitude);
    }

    private Result<int> ParseLocationPrecisionFromForm()
    {
        return ParseLocationPrecision(
            NativeInterop.GetWindowString(_precisionEditHandle));
    }

    private static Result<int> ParseLocationPrecision(string locationPrecisionText)
    {
        return !int.TryParse(
                locationPrecisionText,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out int locationPrecisionDecimals)
            ? Result<int>.Failure(
                Error.Validation(
                    "InvalidLocationPrecision",
                    $"Provide a whole-number precision between {CoordinatePrecisionPolicy.MinStoredDecimals} and {CoordinatePrecisionPolicy.MaxStoredDecimals}."))
            : locationPrecisionDecimals is < CoordinatePrecisionPolicy.MinStoredDecimals
            or > CoordinatePrecisionPolicy.MaxStoredDecimals
            ? Result<int>.Failure(
                Error.Validation(
                    "InvalidLocationPrecision",
                    $"Provide a whole-number precision between {CoordinatePrecisionPolicy.MinStoredDecimals} and {CoordinatePrecisionPolicy.MaxStoredDecimals}."))
            : Result<int>.Success(locationPrecisionDecimals);
    }

    private AppConfig BuildConfiguration(
        GeoCoordinates coordinates,
        bool useWindowsLocation,
        int locationPrecisionDecimals)
    {
        GeoCoordinates reducedCoordinates = CoordinatePrecisionPolicy.Reduce(
            coordinates,
            locationPrecisionDecimals);

        return new AppConfig
        {
            Latitude = reducedCoordinates.Latitude,
            Longitude = reducedCoordinates.Longitude,
            UseWindowsLocation = useWindowsLocation,
            LocationPrecisionDecimals = locationPrecisionDecimals,
            StartWithWindows = NativeInterop.GetChecked(_startWithWindowsHandle),
            StartMinimized = NativeInterop.GetChecked(_startMinimizedHandle),
            UseHighPriority = NativeInterop.GetChecked(_highPriorityHandle),
            IsConfigured = true
        };
    }

    private void RefreshStatus()
    {
        _ = NativeInterop.SetWindowText(
            _todayScheduleHandle,
            _applicationLifecycleOrchestrator.GetTodayScheduleText());

        _ = NativeInterop.SetWindowText(
            _runtimeStatusHandle,
            _applicationLifecycleOrchestrator.GetStatusText());
    }

    private void ShowValidationError(Error error)
    {
        ShowMessage(error.Description, NativeInterop.MB_ICONWARNING);
        FocusInvalidInput(error);
    }

    private void FocusInvalidInput(Error error)
    {
        nint controlHandle = _latitudeEditHandle;

        if (string.Equals(error.Code, "InvalidLongitude", StringComparison.Ordinal))
        {
            controlHandle = _longitudeEditHandle;
        }
        else if (string.Equals(error.Code, "InvalidLocationPrecision", StringComparison.Ordinal))
        {
            controlHandle = _precisionEditHandle;
        }
        else if (string.Equals(error.Code, "MissingLocationSeed", StringComparison.Ordinal))
        {
            controlHandle = _useWindowsLocationHandle;
        }

        _ = NativeInterop.SetFocus(controlHandle);

        if (controlHandle == _latitudeEditHandle
            || controlHandle == _longitudeEditHandle
            || controlHandle == _precisionEditHandle)
        {
            _ = NativeInterop.SendMessage(controlHandle, NativeInterop.EM_SETSEL, nint.Zero, -1);
        }
    }

    private void ShowMessage(string message, int iconFlags)
    {
        _ = NativeInterop.MessageBox(
            _windowHandle,
            message,
            AppName,
            NativeInterop.MB_OK | iconFlags);
    }

    private void HandleDestroy()
    {
        lock (InstancesGate)
        {
            _ = Instances.Remove(_windowHandle);
        }

        _windowHandle = nint.Zero;
        DisposeNativeResources();
    }

    private void DisposeNativeResources()
    {
        if (_fontHandle != nint.Zero)
        {
            _ = NativeInterop.DeleteObject(_fontHandle);
            _fontHandle = nint.Zero;
        }

        if (_ownsWindowIconHandle && _windowIconHandle != nint.Zero)
        {
            _ = NativeInterop.DestroyIcon(_windowIconHandle);
        }

        _windowIconHandle = nint.Zero;
        _ownsWindowIconHandle = false;
        _latitudeEditHandle = nint.Zero;
        _longitudeEditHandle = nint.Zero;
        _precisionEditHandle = nint.Zero;
        _useWindowsLocationHandle = nint.Zero;
        _detectLocationButtonHandle = nint.Zero;
        _startWithWindowsHandle = nint.Zero;
        _startMinimizedHandle = nint.Zero;
        _highPriorityHandle = nint.Zero;
        _todayScheduleHandle = nint.Zero;
        _runtimeStatusHandle = nint.Zero;
        _applyNowButtonHandle = nint.Zero;
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
