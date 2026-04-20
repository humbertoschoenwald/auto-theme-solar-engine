using System.Collections.Concurrent;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using SolarEngine.Features.Locations.Domain;
using SolarEngine.Features.SystemHost;
using SolarEngine.Features.SystemHost.Domain;
using SolarEngine.Features.SystemHost.Infrastructure;
using SolarEngine.Features.Themes.Domain;
using SolarEngine.Features.Updates;
using SolarEngine.Features.Updates.Domain;
using SolarEngine.Infrastructure.Localization;
using SolarEngine.Shared;
using SolarEngine.Shared.Core;

namespace SolarEngine.UI;

internal sealed class SettingsWindow(
    ApplicationLifecycleOrchestrator applicationLifecycleOrchestrator,
    AppLocalization localization,
    UpdateCoordinator updateCoordinator)
{
    private enum SettingsTab
    {
        Home,
        Configuration,
        Updates
    }

    private readonly record struct ControlBounds(int X, int Y, int Width, int Height);

    private const string WindowTitle = AppIdentity.RuntimeName;
    private const string DialogCaption = AppIdentity.RuntimeName;
    private const string WindowClassName = "SolarEngine.NativeSettingsWindow";
    private const int WindowWidth = 520;
    private const int WindowHeight = 680;

    private const int LanguageSelectorId = 100;
    private const int HomeTabId = 101;
    private const int ConfigurationTabId = 102;
    private const int UpdatesTabId = 103;
    private const int LatitudeEditId = 104;
    private const int LongitudeEditId = 105;
    private const int UseWindowsLocationId = 106;
    private const int DetectLocationId = 107;
    private const int StartWithWindowsId = 108;
    private const int StartMinimizedId = 109;
    private const int HighPriorityId = 110;
    private const int ApplyNowId = 111;
    private const int ExtraMinuteAtSunsetId = 112;
    private const int AutomaticUpdatesId = 113;
    private const int PrecisionEditId = 114;
    private const int CheckUpdatesId = 115;
    private const uint WM_PROCESS_UI_ACTIONS = NativeInterop.WM_APP + 100;

    private static readonly Lock s_instancesGate = new();
    private static readonly Dictionary<nint, SettingsWindow> s_instances = [];
    private static readonly NativeInterop.WindowProcedure s_windowProcedureDelegate = WindowProcedure;
    private static ushort s_windowClassAtom;

    private readonly ApplicationLifecycleOrchestrator _applicationLifecycleOrchestrator = applicationLifecycleOrchestrator;
    private readonly AppLocalization _localization = localization;
    private readonly UpdateCoordinator _updateCoordinator = updateCoordinator;
    private readonly ConcurrentQueue<Action> _pendingUiActions = [];
    private readonly List<nint> _homeControls = [];
    private readonly List<nint> _configurationControls = [];
    private readonly List<nint> _updatesControls = [];
    private readonly List<nint> _allControls = [];
    private bool _disposed;
    private int _operationInProgress;
    private SettingsTab _activeTab = SettingsTab.Home;
    private string _selectedLanguageCode = AppLanguageCodes.Default;
    private nint _windowHandle;
    private SafeGdiObjectHandle? _fontHandle;
    private SafeIconHandle? _windowIconHandle;
    private SafeGdiObjectHandle? _backgroundBrushHandle;
    private int _backgroundColorRef = ToColorRef(255, 255, 255);
    private int _foregroundColorRef = ToColorRef(0, 0, 0);
    private nint _headerLabelHandle;
    private nint _languageLabelHandle;
    private nint _languageSelectorHandle;
    private nint _homeTabButtonHandle;
    private nint _configurationTabButtonHandle;
    private nint _updatesTabButtonHandle;
    private nint _locationAccessLabelHandle;
    private nint _locationAccessValueHandle;
    private nint _latitudeLabelHandle;
    private nint _latitudeEditHandle;
    private nint _longitudeLabelHandle;
    private nint _longitudeEditHandle;
    private nint _precisionLabelHandle;
    private nint _precisionEditHandle;
    private nint _privacyHintLabelHandle;
    private nint _useWindowsLocationHandle;
    private nint _detectLocationButtonHandle;
    private nint _todayScheduleLabelHandle;
    private nint _startWithWindowsHandle;
    private nint _startMinimizedHandle;
    private nint _highPriorityHandle;
    private nint _extraMinuteAtSunsetHandle;
    private nint _automaticUpdatesHandle;
    private nint _todayScheduleHandle;
    private nint _runtimeStatusLabelHandle;
    private nint _runtimeStatusHandle;
    private nint _currentVersionLabelHandle;
    private nint _currentVersionValueHandle;
    private nint _latestVersionLabelHandle;
    private nint _latestVersionValueHandle;
    private nint _updateStatusLabelHandle;
    private nint _updateStatusHandle;
    private nint _checkUpdatesButtonHandle;
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
        FocusActiveTab();
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

        EnqueueUiAction(RefreshStatus);
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
        lock (s_instancesGate)
        {
            if (s_instances.TryGetValue(hWnd, out SettingsWindow? window))
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

            case NativeInterop.WM_COMMAND:
                HandleCommand(NativeInterop.LoWord(wParam), NativeInterop.HiWord(wParam));
                return 0;

            case NativeInterop.WM_ERASEBKGND:
                return HandleEraseBackground(wParam);

            case NativeInterop.WM_CTLCOLOREDIT:
            case NativeInterop.WM_CTLCOLORLISTBOX:
            case NativeInterop.WM_CTLCOLORBTN:
            case NativeInterop.WM_CTLCOLORSTATIC:
                return HandleControlColor(wParam);

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
            WindowTitle,
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

        lock (s_instancesGate)
        {
            s_instances[_windowHandle] = this;
        }

        _fontHandle = NativeInterop.CreateFontHandle(
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

        _windowIconHandle = NativeInterop.LoadAppIcon();
        nint windowIconHandle = NativeInterop.GetHandleOrZero(_windowIconHandle);
        if (windowIconHandle != nint.Zero)
        {
            _ = NativeInterop.SendMessage(_windowHandle, NativeInterop.WM_SETICON, NativeInterop.ICON_SMALL, windowIconHandle);
            _ = NativeInterop.SendMessage(_windowHandle, NativeInterop.WM_SETICON, NativeInterop.ICON_BIG, windowIconHandle);
        }

        CreateControls();
        ApplyThemePalette();
        RefreshFromModel();
    }

    private static void RegisterWindowClass()
    {
        if (s_windowClassAtom != 0)
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
                lpfnWndProc = Marshal.GetFunctionPointerForDelegate(s_windowProcedureDelegate),
                hbrBackground = NativeInterop.COLOR_WINDOW + 1,
                lpszClassName = classNamePointer
            };

            s_windowClassAtom = NativeInterop.RegisterClassEx(ref windowClass);
            if (s_windowClassAtom == 0)
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
        _headerLabelHandle = CreateLabel(string.Empty, 16, 16, 240, 20);
        _applyNowButtonHandle = CreateButton(ApplyNowId, string.Empty, 354, 12, 150, 32, true);

        _homeTabButtonHandle = CreateButton(HomeTabId, string.Empty, 16, 56, 152, 30, false);
        _configurationTabButtonHandle = CreateButton(ConfigurationTabId, string.Empty, 184, 56, 152, 30, false);
        _updatesTabButtonHandle = CreateButton(UpdatesTabId, string.Empty, 352, 56, 152, 30, false);

        _locationAccessLabelHandle = CreateLabel(string.Empty, 16, 108, 132, 20, _homeControls);
        _locationAccessValueHandle = CreateLabel(string.Empty, 156, 108, 348, 20, _homeControls);
        _useWindowsLocationHandle = CreateCheckBox(UseWindowsLocationId, string.Empty, 16, 140, 240, 20, _homeControls);
        _detectLocationButtonHandle = CreateButton(DetectLocationId, string.Empty, 324, 136, 180, 28, false, _homeControls);
        _latitudeLabelHandle = CreateLabel(string.Empty, 16, 180, 132, 20, _homeControls);
        _latitudeEditHandle = CreateEdit(LatitudeEditId, 156, 176, 220, 24, 18, _homeControls);
        _longitudeLabelHandle = CreateLabel(string.Empty, 16, 214, 132, 20, _homeControls);
        _longitudeEditHandle = CreateEdit(LongitudeEditId, 156, 210, 220, 24, 18, _homeControls);
        NativeInterop.SetPasswordCharacter(_latitudeEditHandle, '*');
        NativeInterop.SetPasswordCharacter(_longitudeEditHandle, '*');
        _precisionLabelHandle = CreateLabel(string.Empty, 16, 248, 132, 20, _homeControls);
        _precisionEditHandle = CreateEdit(PrecisionEditId, 156, 244, 48, 24, 1, _homeControls);
        _privacyHintLabelHandle = CreateLabel(string.Empty, 216, 248, 288, 20, _homeControls);
        _todayScheduleLabelHandle = CreateLabel(string.Empty, 16, 292, 160, 20, _homeControls);
        _todayScheduleHandle = CreateLabel(string.Empty, 16, 320, 488, 44, _homeControls);

        _languageLabelHandle = CreateLabel(string.Empty, 16, 108, 132, 20, _configurationControls);
        _languageSelectorHandle = CreateDropDownList(LanguageSelectorId, 156, 104, 160, 120, _configurationControls);
        _startWithWindowsHandle = CreateCheckBox(StartWithWindowsId, string.Empty, 16, 152, 240, 20, _configurationControls);
        _startMinimizedHandle = CreateCheckBox(StartMinimizedId, string.Empty, 16, 182, 240, 20, _configurationControls);
        _highPriorityHandle = CreateCheckBox(HighPriorityId, string.Empty, 16, 212, 280, 20, _configurationControls);
        _extraMinuteAtSunsetHandle = CreateCheckBox(ExtraMinuteAtSunsetId, string.Empty, 16, 242, 280, 20, _configurationControls);
        _runtimeStatusLabelHandle = CreateLabel(string.Empty, 16, 286, 160, 20, _configurationControls);
        _runtimeStatusHandle = CreateLabel(string.Empty, 16, 314, 488, 44, _configurationControls);

        _automaticUpdatesHandle = CreateCheckBox(AutomaticUpdatesId, string.Empty, 16, 108, 280, 20, _updatesControls);
        _currentVersionLabelHandle = CreateLabel(string.Empty, 16, 150, 132, 20, _updatesControls);
        _currentVersionValueHandle = CreateLabel(string.Empty, 156, 150, 348, 20, _updatesControls);
        _latestVersionLabelHandle = CreateLabel(string.Empty, 16, 182, 132, 20, _updatesControls);
        _latestVersionValueHandle = CreateLabel(string.Empty, 156, 182, 348, 20, _updatesControls);
        _updateStatusLabelHandle = CreateLabel(string.Empty, 16, 214, 132, 20, _updatesControls);
        _updateStatusHandle = CreateLabel(string.Empty, 156, 214, 348, 44, _updatesControls);
        _checkUpdatesButtonHandle = CreateButton(CheckUpdatesId, string.Empty, 16, 274, 180, 30, false, _updatesControls);

        SetActiveTab(SettingsTab.Home);
        ApplyLocalizedText();
    }

    private nint CreateLabel(string text, int x, int y, int width, int height, List<nint>? group = null)
    {
        return CreateControl(
            "STATIC",
            text,
            NativeInterop.SS_LEFT,
            0,
            new ControlBounds(x, y, width, height),
            0,
            group);
    }

    private nint CreateEdit(
        int controlId,
        int x,
        int y,
        int width,
        int height,
        int maxCharacters = 18,
        List<nint>? group = null)
    {
        nint editHandle = CreateControl(
            "EDIT",
            string.Empty,
            NativeInterop.ES_AUTOHSCROLL | NativeInterop.WS_TABSTOP,
            NativeInterop.WS_EX_CLIENTEDGE,
            new ControlBounds(x, y, width, height),
            controlId,
            group);

        _ = NativeInterop.SendMessage(editHandle, NativeInterop.EM_SETLIMITTEXT, maxCharacters, nint.Zero);
        return editHandle;
    }

    private nint CreateCheckBox(
        int controlId,
        string text,
        int x,
        int y,
        int width,
        int height,
        List<nint>? group = null)
    {
        return CreateControl(
            "BUTTON",
            text,
            NativeInterop.BS_AUTOCHECKBOX | NativeInterop.WS_TABSTOP,
            0,
            new ControlBounds(x, y, width, height),
            controlId,
            group);
    }

    private nint CreateDropDownList(
        int controlId,
        int x,
        int y,
        int width,
        int height,
        List<nint>? group = null)
    {
        return CreateControl(
            "COMBOBOX",
            string.Empty,
            NativeInterop.CBS_DROPDOWNLIST | NativeInterop.WS_TABSTOP | NativeInterop.WS_VSCROLL,
            0,
            new ControlBounds(x, y, width, height),
            controlId,
            group);
    }

    private nint CreateButton(
        int controlId,
        string text,
        int x,
        int y,
        int width,
        int height,
        bool isDefault,
        List<nint>? group = null)
    {
        int style = NativeInterop.WS_TABSTOP;
        if (isDefault)
        {
            style |= NativeInterop.BS_DEFPUSHBUTTON;
        }

        return CreateControl(
            "BUTTON",
            text,
            style,
            0,
            new ControlBounds(x, y, width, height),
            controlId,
            group);
    }

    private nint CreateControl(
        string className,
        string text,
        int style,
        int extendedStyle,
        ControlBounds bounds,
        int controlId,
        List<nint>? group = null)
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
        _allControls.Add(controlHandle);
        group?.Add(controlHandle);
        return controlHandle;
    }

    private void ApplyLocalizedText()
    {
        _ = NativeInterop.SetWindowText(_windowHandle, WindowTitle);
        _ = NativeInterop.SetWindowText(_headerLabelHandle, _localization["settings.header"]);
        _ = NativeInterop.SetWindowText(_applyNowButtonHandle, _localization["settings.save_and_apply"]);
        _ = NativeInterop.SetWindowText(_homeTabButtonHandle, _localization["settings.tab.home"]);
        _ = NativeInterop.SetWindowText(_configurationTabButtonHandle, _localization["settings.tab.configuration"]);
        _ = NativeInterop.SetWindowText(_updatesTabButtonHandle, _localization["settings.tab.updates"]);

        _ = NativeInterop.SetWindowText(_locationAccessLabelHandle, _localization["settings.location_access"]);
        _ = NativeInterop.SetWindowText(_useWindowsLocationHandle, _localization["settings.use_windows_location"]);
        _ = NativeInterop.SetWindowText(_detectLocationButtonHandle, _localization["settings.detect_from_windows"]);
        _ = NativeInterop.SetWindowText(_latitudeLabelHandle, _localization["settings.latitude"]);
        _ = NativeInterop.SetWindowText(_longitudeLabelHandle, _localization["settings.longitude"]);
        _ = NativeInterop.SetWindowText(_precisionLabelHandle, _localization["settings.precision"]);
        _ = NativeInterop.SetWindowText(_privacyHintLabelHandle, _localization["settings.privacy_hint"]);
        _ = NativeInterop.SetWindowText(_todayScheduleLabelHandle, _localization["settings.today_schedule"]);

        _ = NativeInterop.SetWindowText(_languageLabelHandle, _localization["settings.language"]);
        RefreshLanguageSelector();
        _ = NativeInterop.SetWindowText(_startWithWindowsHandle, _localization["settings.start_with_windows"]);
        _ = NativeInterop.SetWindowText(_startMinimizedHandle, _localization["settings.open_in_tray"]);
        _ = NativeInterop.SetWindowText(_highPriorityHandle, _localization["settings.use_high_priority"]);
        _ = NativeInterop.SetWindowText(_extraMinuteAtSunsetHandle, _localization["settings.extra_minute_at_sunset"]);
        _ = NativeInterop.SetWindowText(_runtimeStatusLabelHandle, _localization["settings.runtime_status"]);

        _ = NativeInterop.SetWindowText(_automaticUpdatesHandle, _localization["settings.install_updates_automatically"]);
        _ = NativeInterop.SetWindowText(_currentVersionLabelHandle, _localization["settings.current_version"]);
        _ = NativeInterop.SetWindowText(_latestVersionLabelHandle, _localization["settings.latest_version"]);
        _ = NativeInterop.SetWindowText(_updateStatusLabelHandle, _localization["settings.update_status"]);
        _ = NativeInterop.SetWindowText(_checkUpdatesButtonHandle, _localization["settings.check_updates"]);
    }

    private void RefreshLanguageSelector()
    {
        NativeInterop.ResetComboBoxContent(_languageSelectorHandle);
        NativeInterop.AddComboBoxString(_languageSelectorHandle, _localization["settings.language.option.english"]);
        NativeInterop.AddComboBoxString(_languageSelectorHandle, _localization["settings.language.option.spanish"]);
        NativeInterop.SetComboSelection(_languageSelectorHandle, GetLanguageIndex(_selectedLanguageCode));
    }

    private void SetActiveTab(SettingsTab activeTab)
    {
        _activeTab = activeTab;
        SetControlsVisible(_homeControls, activeTab == SettingsTab.Home);
        SetControlsVisible(_configurationControls, activeTab == SettingsTab.Configuration);
        SetControlsVisible(_updatesControls, activeTab == SettingsTab.Updates);

        _ = NativeInterop.EnableWindow(_homeTabButtonHandle, activeTab != SettingsTab.Home);
        _ = NativeInterop.EnableWindow(_configurationTabButtonHandle, activeTab != SettingsTab.Configuration);
        _ = NativeInterop.EnableWindow(_updatesTabButtonHandle, activeTab != SettingsTab.Updates);
    }

    private static void SetControlsVisible(IEnumerable<nint> controlHandles, bool isVisible)
    {
        foreach (nint handle in controlHandles)
        {
            _ = NativeInterop.ShowWindow(handle, isVisible ? NativeInterop.SW_SHOW : NativeInterop.SW_HIDE);
        }
    }

    private void FocusActiveTab()
    {
        nint focusHandle = _activeTab switch
        {
            SettingsTab.Home => _latitudeEditHandle,
            SettingsTab.Configuration => _languageSelectorHandle,
            SettingsTab.Updates => _checkUpdatesButtonHandle,
            _ => _latitudeEditHandle
        };

        if (focusHandle != nint.Zero)
        {
            _ = NativeInterop.SetFocus(focusHandle);
        }
    }

    private void ApplyThemePalette()
    {
        ThemeMode themeMode = _applicationLifecycleOrchestrator.GetCurrentThemeMode() ?? ThemeMode.Light;
        int backgroundColorRef = themeMode == ThemeMode.Dark
            ? ToColorRef(24, 24, 24)
            : ToColorRef(255, 255, 255);
        int foregroundColorRef = themeMode == ThemeMode.Dark
            ? ToColorRef(244, 244, 244)
            : ToColorRef(0, 0, 0);

        if (_backgroundColorRef == backgroundColorRef
            && _foregroundColorRef == foregroundColorRef
            && NativeInterop.GetHandleOrZero(_backgroundBrushHandle) != nint.Zero)
        {
            NativeInterop.ApplyDwmAttributes(_windowHandle, themeMode == ThemeMode.Dark);
            return;
        }

        _backgroundBrushHandle?.Dispose();

        _backgroundColorRef = backgroundColorRef;
        _foregroundColorRef = foregroundColorRef;
        _backgroundBrushHandle = NativeInterop.CreateSolidBrushHandle(_backgroundColorRef);

        NativeInterop.ApplyDwmAttributes(_windowHandle, themeMode == ThemeMode.Dark);
        _ = NativeInterop.InvalidateRect(_windowHandle, nint.Zero, erase: true);

        foreach (nint controlHandle in _allControls)
        {
            _ = NativeInterop.InvalidateRect(controlHandle, nint.Zero, erase: true);
        }
    }

    private nint HandleEraseBackground(nint wParam)
    {
        nint backgroundBrushHandle = NativeInterop.GetHandleOrZero(_backgroundBrushHandle);
        if (backgroundBrushHandle == nint.Zero
            || !NativeInterop.GetClientRect(_windowHandle, out NativeInterop.NativeRect rect))
        {
            return NativeInterop.DefWindowProc(_windowHandle, NativeInterop.WM_ERASEBKGND, wParam, nint.Zero);
        }

        _ = NativeInterop.FillRect(wParam, ref rect, backgroundBrushHandle);
        return 1;
    }

    private nint HandleControlColor(nint wParam)
    {
        nint backgroundBrushHandle = NativeInterop.GetHandleOrZero(_backgroundBrushHandle);
        if (backgroundBrushHandle == nint.Zero)
        {
            return nint.Zero;
        }

        _ = NativeInterop.SetTextColor(wParam, _foregroundColorRef);
        _ = NativeInterop.SetBkColor(wParam, _backgroundColorRef);
        return backgroundBrushHandle;
    }

    private void HandleLanguageSelectionChanged()
    {
        int selectedIndex = NativeInterop.GetComboSelection(_languageSelectorHandle);
        if (selectedIndex == NativeInterop.CB_ERR)
        {
            return;
        }

        _selectedLanguageCode = GetLanguageCode(selectedIndex);
        _localization.UpdateLanguage(_selectedLanguageCode);
        ApplyLocalizedText();
        RefreshStatus();
    }

    private static int GetLanguageIndex(string languageCode)
    {
        return string.Equals(languageCode, AppLanguageCodes.Spanish, StringComparison.Ordinal)
            ? 1
            : 0;
    }

    private static string GetLanguageCode(int selectedIndex)
    {
        return selectedIndex == 1
            ? AppLanguageCodes.Spanish
            : AppLanguageCodes.English;
    }

    private void HandleCommand(int controlId, int notificationCode)
    {
        if (controlId == LanguageSelectorId && notificationCode == NativeInterop.CBN_SELCHANGE)
        {
            HandleLanguageSelectionChanged();
            return;
        }

        if (notificationCode != NativeInterop.BN_CLICKED)
        {
            return;
        }

        switch (controlId)
        {
            case HomeTabId:
                SetActiveTab(SettingsTab.Home);
                FocusActiveTab();
                break;

            case ConfigurationTabId:
                SetActiveTab(SettingsTab.Configuration);
                FocusActiveTab();
                break;

            case UpdatesTabId:
                SetActiveTab(SettingsTab.Updates);
                FocusActiveTab();
                break;

            case UseWindowsLocationId:
                RefreshStatus();
                break;

            case DetectLocationId:
                StartDetectLocation();
                break;

            case CheckUpdatesId:
                StartCheckForUpdates();
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

        if (!TryBeginOperation(_localization["settings.operation.applying_theme"]))
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
                ShowMessage(_localization["settings.message.theme_applied"], NativeInterop.MB_ICONINFORMATION);
            });
        }
        catch (OperationCanceledException)
        {
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

        if (!TryBeginOperation(_localization["settings.operation.detecting_location"]))
        {
            return;
        }

        _ = DetectLocationAsync(locationPrecisionResult.Value);
    }

    private void StartCheckForUpdates()
    {
        if (!TryBeginOperation(_localization["settings.operation.checking_updates"]))
        {
            return;
        }

        _ = CheckForUpdatesAsync();
    }

    private async Task CheckForUpdatesAsync()
    {
        try
        {
            UpdateStatusSnapshot updateSnapshot = await _updateCoordinator
                .CheckForUpdatesAsync()
                .ConfigureAwait(false);

            EnqueueUiAction(() =>
            {
                RefreshStatus();

                if (!updateSnapshot.IsUpdateAvailable)
                {
                    ShowMessage(_localization["settings.message.no_updates"], NativeInterop.MB_ICONINFORMATION);
                    return;
                }

                ShowMessage(_localization["settings.message.update_available"], NativeInterop.MB_ICONINFORMATION);
            });
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            _updateCoordinator.RecordCheckFailure(exception.Message);
            EnqueueUiAction(() =>
            {
                RefreshStatus();
                ShowMessage(exception.Message, NativeInterop.MB_ICONERROR);
            });
        }
        finally
        {
            EnqueueUiAction(CompleteOperation);
        }
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
            ShowMessage(_localization["settings.operation.already_running"], NativeInterop.MB_ICONWARNING);
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
        bool locationAvailable =
            _applicationLifecycleOrchestrator.WindowsLocationAccessState == SystemLocationAccessState.Allowed;

        _ = NativeInterop.EnableWindow(_detectLocationButtonHandle, !isBusy && locationAvailable);
        _ = NativeInterop.EnableWindow(_useWindowsLocationHandle, !isBusy && locationAvailable);
        _ = NativeInterop.EnableWindow(_languageSelectorHandle, !isBusy);
        _ = NativeInterop.EnableWindow(_checkUpdatesButtonHandle, !isBusy);
        _ = NativeInterop.EnableWindow(_applyNowButtonHandle, !isBusy);

        if (isBusy && !string.IsNullOrWhiteSpace(statusText))
        {
            _ = NativeInterop.SetWindowText(_updateStatusHandle, statusText);
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
        bool hasStoredCoordinates = configuration.IsConfigured;

        _ = NativeInterop.SetWindowText(
            _latitudeEditHandle,
            hasStoredCoordinates
                ? CoordinatePrecisionPolicy.Format(
                    configuration.Latitude,
                    configuration.LocationPrecisionDecimals)
                : string.Empty);

        _ = NativeInterop.SetWindowText(
            _longitudeEditHandle,
            hasStoredCoordinates
                ? CoordinatePrecisionPolicy.Format(
                    configuration.Longitude,
                    configuration.LocationPrecisionDecimals)
                : string.Empty);

        _ = NativeInterop.SetWindowText(
            _precisionEditHandle,
            configuration.LocationPrecisionDecimals.ToString(CultureInfo.InvariantCulture));

        _selectedLanguageCode = AppLanguageCodes.Normalize(configuration.LanguageCode);
        _localization.UpdateLanguage(_selectedLanguageCode);
        ApplyLocalizedText();

        bool isWindowsLocationAvailable =
            _applicationLifecycleOrchestrator.WindowsLocationAccessState == SystemLocationAccessState.Allowed;
        NativeInterop.SetChecked(
            _useWindowsLocationHandle,
            configuration.UseWindowsLocation && isWindowsLocationAvailable);
        NativeInterop.SetChecked(_startWithWindowsHandle, configuration.StartWithWindows);
        NativeInterop.SetChecked(_startMinimizedHandle, configuration.StartMinimized);
        NativeInterop.SetChecked(_highPriorityHandle, configuration.UseHighPriority);
        NativeInterop.SetChecked(_extraMinuteAtSunsetHandle, configuration.AddExtraMinuteAtSunset);
        NativeInterop.SetChecked(_automaticUpdatesHandle, configuration.AutomaticUpdatesEnabled);
        _ = NativeInterop.EnableWindow(_useWindowsLocationHandle, isWindowsLocationAvailable);
        _ = NativeInterop.EnableWindow(_detectLocationButtonHandle, isWindowsLocationAvailable);
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
        if (!double.TryParse(
                latitudeText,
                NumberStyles.Float | NumberStyles.AllowLeadingSign,
                CultureInfo.InvariantCulture,
                out double latitude))
        {
            return Result<GeoCoordinates>.Failure(
                Error.Validation("InvalidLatitude", "Provide a valid decimal latitude."));
        }

        if (!double.TryParse(
                longitudeText,
                NumberStyles.Float | NumberStyles.AllowLeadingSign,
                CultureInfo.InvariantCulture,
                out double longitude))
        {
            return Result<GeoCoordinates>.Failure(
                Error.Validation("InvalidLongitude", "Provide a valid decimal longitude."));
        }

        return GeoCoordinates.Create(latitude, longitude);
    }

    private Result<int> ParseLocationPrecisionFromForm()
    {
        return ParseLocationPrecision(
            NativeInterop.GetWindowString(_precisionEditHandle));
    }

    private static Result<int> ParseLocationPrecision(string locationPrecisionText)
    {
        if (!int.TryParse(
                locationPrecisionText,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out int locationPrecisionDecimals))
        {
            return Result<int>.Failure(
                Error.Validation(
                    "InvalidLocationPrecision",
                    $"Provide a whole-number precision between {CoordinatePrecisionPolicy.MinStoredDecimals} and {CoordinatePrecisionPolicy.MaxStoredDecimals}."));
        }

        if (locationPrecisionDecimals is < CoordinatePrecisionPolicy.MinStoredDecimals
            or > CoordinatePrecisionPolicy.MaxStoredDecimals)
        {
            return Result<int>.Failure(
                Error.Validation(
                    "InvalidLocationPrecision",
                    $"Provide a whole-number precision between {CoordinatePrecisionPolicy.MinStoredDecimals} and {CoordinatePrecisionPolicy.MaxStoredDecimals}."));
        }

        return Result<int>.Success(locationPrecisionDecimals);
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
            AddExtraMinuteAtSunset = NativeInterop.GetChecked(_extraMinuteAtSunsetHandle),
            AutomaticUpdatesEnabled = NativeInterop.GetChecked(_automaticUpdatesHandle),
            LanguageCode = _selectedLanguageCode,
            IsConfigured = true
        };
    }

    private void RefreshStatus()
    {
        ApplyThemePalette();

        bool locationAvailable =
            _applicationLifecycleOrchestrator.WindowsLocationAccessState == SystemLocationAccessState.Allowed;
        bool isBusy = Volatile.Read(ref _operationInProgress) != 0;
        _ = NativeInterop.EnableWindow(_useWindowsLocationHandle, locationAvailable && !isBusy);
        _ = NativeInterop.EnableWindow(_detectLocationButtonHandle, locationAvailable && !isBusy);
        _ = NativeInterop.EnableWindow(_checkUpdatesButtonHandle, !isBusy);

        _ = NativeInterop.SetWindowText(
            _locationAccessValueHandle,
            GetLocationAccessText(_applicationLifecycleOrchestrator.WindowsLocationAccessState));

        _ = NativeInterop.SetWindowText(
            _todayScheduleHandle,
            _applicationLifecycleOrchestrator.GetTodayScheduleText());

        _ = NativeInterop.SetWindowText(
            _runtimeStatusHandle,
            _applicationLifecycleOrchestrator.GetStatusText());

        UpdateStatusSnapshot updateSnapshot = _updateCoordinator.GetSnapshot();
        _ = NativeInterop.SetWindowText(_currentVersionValueHandle, updateSnapshot.CurrentVersion.ToTag());
        _ = NativeInterop.SetWindowText(
            _latestVersionValueHandle,
            updateSnapshot.LastCheckedAtUtc is null
                ? _localization["settings.update.not_checked"]
                : updateSnapshot.LatestVersionTag ?? updateSnapshot.CurrentVersion.ToTag());

        if (!isBusy)
        {
            _ = NativeInterop.SetWindowText(
                _updateStatusHandle,
                GetUpdateStatusText(updateSnapshot));
        }
    }

    private string GetUpdateStatusText(UpdateStatusSnapshot updateSnapshot)
    {
        if (!string.IsNullOrWhiteSpace(updateSnapshot.LastCheckErrorMessage))
        {
            return _localization["settings.update.check_failed"];
        }

        if (updateSnapshot.LastCheckedAtUtc is null)
        {
            return _localization["settings.update.idle"];
        }

        return updateSnapshot.IsUpdateAvailable
            ? _localization["settings.update.available"]
            : _localization["settings.update.up_to_date"];
    }

    private string GetLocationAccessText(SystemLocationAccessState accessState)
    {
        return accessState switch
        {
            SystemLocationAccessState.Allowed => _localization["settings.location_access.allowed"],
            SystemLocationAccessState.Denied => _localization["settings.location_access.denied"],
            SystemLocationAccessState.Unavailable => _localization["settings.location_access.unavailable"],
            SystemLocationAccessState.Unknown => _localization["settings.location_access.unknown"],
            _ => _localization["settings.location_access.unknown"]
        };
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
            DialogCaption,
            NativeInterop.MB_OK | iconFlags);
    }

    private void HandleDestroy()
    {
        lock (s_instancesGate)
        {
            _ = s_instances.Remove(_windowHandle);
        }

        _windowHandle = nint.Zero;
        DisposeNativeResources();
    }

    private void DisposeNativeResources()
    {
        _backgroundBrushHandle?.Dispose();
        _backgroundBrushHandle = null;

        _fontHandle?.Dispose();
        _fontHandle = null;

        _windowIconHandle?.Dispose();
        _windowIconHandle = null;
        _languageSelectorHandle = nint.Zero;
        _latitudeEditHandle = nint.Zero;
        _longitudeEditHandle = nint.Zero;
        _precisionEditHandle = nint.Zero;
        _useWindowsLocationHandle = nint.Zero;
        _detectLocationButtonHandle = nint.Zero;
        _startWithWindowsHandle = nint.Zero;
        _startMinimizedHandle = nint.Zero;
        _highPriorityHandle = nint.Zero;
        _extraMinuteAtSunsetHandle = nint.Zero;
        _automaticUpdatesHandle = nint.Zero;
        _todayScheduleHandle = nint.Zero;
        _runtimeStatusHandle = nint.Zero;
        _applyNowButtonHandle = nint.Zero;
    }

    private static int ToColorRef(byte red, byte green, byte blue)
    {
        return red | (green << 8) | (blue << 16);
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
