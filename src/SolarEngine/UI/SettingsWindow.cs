// Copyright (c) 2026 Humberto Schoenwald.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Concurrent;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
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
    private const string SettingsWindowCreationDescription = "Failed to create the settings window.";
    private const string SettingsWindowClassRegistrationDescription = "Failed to register the settings window class.";
    private const string ControlCreationDescriptionFormat = "Failed to create a {0} control.";
    private const string NativeStaticClassName = "STATIC";
    private const string NativeEditClassName = "EDIT";
    private const string NativeButtonClassName = "BUTTON";
    private const string NativeComboBoxClassName = "COMBOBOX";
    private const string DefaultFontFaceName = "Segoe UI";
    private const string InvalidLongitudeCode = "InvalidLongitude";
    private const string InvalidLocationPrecisionCode = "InvalidLocationPrecision";
    private const string MissingLocationSeedCode = "MissingLocationSeed";
    private const string InvalidLocationPrecisionDescriptionFormat = "Provide a whole-number precision between {0} and {1}.";
    private const string SettingsHeaderKey = "settings.header";
    private const string SettingsSaveAndApplyKey = "settings.save_and_apply";
    private const string SettingsHomeTabKey = "settings.tab.home";
    private const string SettingsConfigurationTabKey = "settings.tab.configuration";
    private const string SettingsUpdatesTabKey = "settings.tab.updates";
    private const string SettingsLocationAccessKey = "settings.location_access";
    private const string SettingsUseWindowsLocationKey = "settings.use_windows_location";
    private const string SettingsDetectFromWindowsKey = "settings.detect_from_windows";
    private const string SettingsLatitudeKey = "settings.latitude";
    private const string SettingsLongitudeKey = "settings.longitude";
    private const string SettingsPrecisionKey = "settings.precision";
    private const string SettingsPrivacyHintKey = "settings.privacy_hint";
    private const string SettingsTodayScheduleKey = "settings.today_schedule";
    private const string SettingsLanguageKey = "settings.language";
    private const string SettingsStartWithWindowsKey = "settings.start_with_windows";
    private const string SettingsOpenInTrayKey = "settings.open_in_tray";
    private const string SettingsUseHighPriorityKey = "settings.use_high_priority";
    private const string SettingsExtraMinuteAtSunsetKey = "settings.extra_minute_at_sunset";
    private const string SettingsRuntimeStatusKey = "settings.runtime_status";
    private const string SettingsAutomaticUpdatesKey = "settings.install_updates_automatically";
    private const string SettingsCurrentVersionKey = "settings.current_version";
    private const string SettingsLatestVersionKey = "settings.latest_version";
    private const string SettingsUpdateStatusKey = "settings.update_status";
    private const string SettingsCheckUpdatesKey = "settings.check_updates";
    private const string SettingsEnglishLanguageOptionKey = "settings.language.option.english";
    private const string SettingsSpanishLanguageOptionKey = "settings.language.option.spanish";
    private const string SettingsApplyingThemeKey = "settings.operation.applying_theme";
    private const string SettingsThemeAppliedMessageKey = "settings.message.theme_applied";
    private const string SettingsDetectingLocationKey = "settings.operation.detecting_location";
    private const string SettingsCheckingUpdatesKey = "settings.operation.checking_updates";
    private const string SettingsNoUpdatesMessageKey = "settings.message.no_updates";
    private const string SettingsUpdateAvailableMessageKey = "settings.message.update_available";
    private const string SettingsAlreadyRunningKey = "settings.operation.already_running";
    private const string SettingsUpdateNotCheckedKey = "settings.update.not_checked";
    private const string SettingsUpdateCheckFailedKey = "settings.update.check_failed";
    private const string SettingsUpdateIdleKey = "settings.update.idle";
    private const string SettingsUpdateAvailableKey = "settings.update.available";
    private const string SettingsUpdateUpToDateKey = "settings.update.up_to_date";
    private const string SettingsLocationAccessAllowedKey = "settings.location_access.allowed";
    private const string SettingsLocationAccessDeniedKey = "settings.location_access.denied";
    private const string SettingsLocationAccessUnavailableKey = "settings.location_access.unavailable";
    private const string SettingsLocationAccessUnknownKey = "settings.location_access.unknown";
    private const int WindowOriginX = 220;
    private const int WindowOriginY = 160;
    private const int WindowWidth = 520;
    private const int WindowHeight = 680;
    private const int DefaultWindowStyle = 0;
    private const int NoControlId = 0;
    private const int NoExtendedStyle = 0;
    private const int NormalFontHeight = -16;
    private const int ZeroColorChannel = 0;
    private const int DarkThemeColorChannel = 24;
    private const int LightForegroundColorChannel = 244;
    private const int FullColorChannel = 255;
    private const int BusyOperationState = 1;
    private const int IdleOperationState = 0;
    private const int LanguageEnglishIndex = 0;
    private const int LanguageSpanishIndex = 1;
    private const int DefaultCoordinateMaxCharacters = 18;
    private const int PrecisionEditMaxCharacters = 1;
    private const int RegisterClassBackgroundOffset = 1;
    private const int HandledWindowMessageResult = 1;
    private const int SelectAllTextEnd = -1;
    private const ushort NoWindowClassAtom = 0;
    private const int GreenChannelShift = 8;
    private const int BlueChannelShift = 16;
    private const char HiddenCoordinateCharacter = '*';
    private const char VisibleCoordinateCharacter = '\0';

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
    private static readonly nint s_handledWindowMessageResult = nint.Zero;
    private static readonly Dictionary<nint, SettingsWindow> s_instances = [];
    private static readonly NativeInterop.WindowProcedure s_windowProcedureDelegate = WindowProcedure;
    private static readonly ControlBounds s_headerLabelBounds = new(16, 16, 240, 20);
    private static readonly ControlBounds s_applyNowButtonBounds = new(354, 12, 150, 32);
    private static readonly ControlBounds s_homeTabButtonBounds = new(16, 56, 152, 30);
    private static readonly ControlBounds s_configurationTabButtonBounds = new(184, 56, 152, 30);
    private static readonly ControlBounds s_updatesTabButtonBounds = new(352, 56, 152, 30);
    private static readonly ControlBounds s_locationAccessLabelBounds = new(16, 108, 132, 20);
    private static readonly ControlBounds s_locationAccessValueBounds = new(156, 108, 348, 20);
    private static readonly ControlBounds s_useWindowsLocationBounds = new(16, 140, 240, 20);
    private static readonly ControlBounds s_detectLocationButtonBounds = new(324, 136, 180, 28);
    private static readonly ControlBounds s_latitudeLabelBounds = new(16, 180, 132, 20);
    private static readonly ControlBounds s_latitudeEditBounds = new(156, 176, 220, 24);
    private static readonly ControlBounds s_longitudeLabelBounds = new(16, 214, 132, 20);
    private static readonly ControlBounds s_longitudeEditBounds = new(156, 210, 220, 24);
    private static readonly ControlBounds s_precisionLabelBounds = new(16, 248, 132, 20);
    private static readonly ControlBounds s_precisionEditBounds = new(156, 244, 48, 24);
    private static readonly ControlBounds s_privacyHintBounds = new(216, 248, 288, 20);
    private static readonly ControlBounds s_todayScheduleLabelBounds = new(16, 292, 160, 20);
    private static readonly ControlBounds s_todayScheduleValueBounds = new(16, 320, 488, 44);
    private static readonly ControlBounds s_languageLabelBounds = new(16, 108, 132, 20);
    private static readonly ControlBounds s_languageSelectorBounds = new(156, 104, 160, 120);
    private static readonly ControlBounds s_startWithWindowsBounds = new(16, 152, 240, 20);
    private static readonly ControlBounds s_startMinimizedBounds = new(16, 182, 240, 20);
    private static readonly ControlBounds s_highPriorityBounds = new(16, 212, 280, 20);
    private static readonly ControlBounds s_extraMinuteAtSunsetBounds = new(16, 242, 280, 20);
    private static readonly ControlBounds s_runtimeStatusLabelBounds = new(16, 286, 160, 20);
    private static readonly ControlBounds s_runtimeStatusValueBounds = new(16, 314, 488, 44);
    private static readonly ControlBounds s_automaticUpdatesBounds = new(16, 108, 280, 20);
    private static readonly ControlBounds s_currentVersionLabelBounds = new(16, 150, 132, 20);
    private static readonly ControlBounds s_currentVersionValueBounds = new(156, 150, 348, 20);
    private static readonly ControlBounds s_latestVersionLabelBounds = new(16, 182, 132, 20);
    private static readonly ControlBounds s_latestVersionValueBounds = new(156, 182, 348, 20);
    private static readonly ControlBounds s_updateStatusLabelBounds = new(16, 214, 132, 20);
    private static readonly ControlBounds s_updateStatusValueBounds = new(156, 214, 348, 44);
    private static readonly ControlBounds s_checkUpdatesButtonBounds = new(16, 274, 180, 30);
    private static readonly int s_darkBackgroundColorRef = ToColorRef(DarkThemeColorChannel, DarkThemeColorChannel, DarkThemeColorChannel);
    private static readonly int s_lightBackgroundColorRef = ToColorRef(FullColorChannel, FullColorChannel, FullColorChannel);
    private static readonly int s_darkForegroundColorRef = ToColorRef(LightForegroundColorChannel, LightForegroundColorChannel, LightForegroundColorChannel);
    private static readonly int s_lightForegroundColorRef = ToColorRef(ZeroColorChannel, ZeroColorChannel, ZeroColorChannel);
    private static readonly CompositeFormat s_controlCreationCompositeFormat = CompositeFormat.Parse(ControlCreationDescriptionFormat);
    private static readonly CompositeFormat s_invalidLocationPrecisionCompositeFormat = CompositeFormat.Parse(InvalidLocationPrecisionDescriptionFormat);
    private static ushort s_windowClassAtom;

    private readonly ApplicationLifecycleOrchestrator _applicationLifecycleOrchestrator = applicationLifecycleOrchestrator;
    private readonly AppLocalization _localization = localization;
    private readonly UpdateCoordinator _updateCoordinator = updateCoordinator;
    private readonly CoordinateInputState _coordinateInputState = new();
    private readonly ConcurrentQueue<Action> _pendingUiActions = [];
    private readonly List<nint> _homeControls = [];
    private readonly List<nint> _configurationControls = [];
    private readonly List<nint> _updatesControls = [];
    private readonly List<nint> _allControls = [];
    private bool _disposed;
    private bool _coordinateInputsVisible;
    private int _operationInProgress;
    private SettingsTab _activeTab = SettingsTab.Home;
    private string _selectedLanguageCode = AppLanguageCodes.Default;
    private nint _windowHandle;
    private SafeGdiObjectHandle? _fontHandle;
    private SafeIconHandle? _windowIconHandle;
    private SafeGdiObjectHandle? _backgroundBrushHandle;
    private int _backgroundColorRef = s_lightBackgroundColorRef;
    private int _foregroundColorRef = s_lightForegroundColorRef;
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
                return s_handledWindowMessageResult;

            case NativeInterop.WM_COMMAND:
                HandleCommand(NativeInterop.LoWord(wParam), NativeInterop.HiWord(wParam));
                return s_handledWindowMessageResult;

            case NativeInterop.WM_ERASEBKGND:
                return HandleEraseBackground(wParam);

            case NativeInterop.WM_CTLCOLOREDIT:
            case NativeInterop.WM_CTLCOLORLISTBOX:
            case NativeInterop.WM_CTLCOLORBTN:
            case NativeInterop.WM_CTLCOLORSTATIC:
                return HandleControlColor(wParam);

            case NativeInterop.WM_CLOSE:
                HideToTray();
                return s_handledWindowMessageResult;

            case NativeInterop.WM_DESTROY:
                HandleDestroy();
                return s_handledWindowMessageResult;

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
            DefaultWindowStyle,
            WindowClassName,
            WindowTitle,
            NativeInterop.WS_CAPTION
            | NativeInterop.WS_SYSMENU
            | NativeInterop.WS_MINIMIZEBOX
            | NativeInterop.WS_CLIPCHILDREN,
            WindowOriginX,
            WindowOriginY,
            WindowWidth,
            WindowHeight,
            nint.Zero,
            nint.Zero,
            NativeInterop.GetModuleHandle(null),
            nint.Zero);

        if (_windowHandle == nint.Zero)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), SettingsWindowCreationDescription);
        }

        lock (s_instancesGate)
        {
            s_instances[_windowHandle] = this;
        }

        _fontHandle = NativeInterop.CreateFontHandle(
            NormalFontHeight,
            DefaultWindowStyle,
            DefaultWindowStyle,
            DefaultWindowStyle,
            NativeInterop.FW_NORMAL,
            DefaultWindowStyle,
            DefaultWindowStyle,
            DefaultWindowStyle,
            NativeInterop.DEFAULT_CHARSET,
            NativeInterop.OUT_DEFAULT_PRECIS,
            NativeInterop.CLIP_DEFAULT_PRECIS,
            NativeInterop.CLEARTYPE_QUALITY,
            NativeInterop.DEFAULT_PITCH,
            DefaultFontFaceName);

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
        if (s_windowClassAtom != NoWindowClassAtom)
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
                hbrBackground = NativeInterop.COLOR_WINDOW + RegisterClassBackgroundOffset,
                lpszClassName = classNamePointer
            };

            s_windowClassAtom = NativeInterop.RegisterClassEx(ref windowClass);
            if (s_windowClassAtom == NoWindowClassAtom)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), SettingsWindowClassRegistrationDescription);
            }
        }
        finally
        {
            Marshal.FreeHGlobal(classNamePointer);
        }
    }

    private void CreateControls()
    {
        _headerLabelHandle = CreateLabel(string.Empty, s_headerLabelBounds);
        _applyNowButtonHandle = CreateButton(ApplyNowId, string.Empty, s_applyNowButtonBounds, true);

        _homeTabButtonHandle = CreateButton(HomeTabId, string.Empty, s_homeTabButtonBounds, false);
        _configurationTabButtonHandle = CreateButton(ConfigurationTabId, string.Empty, s_configurationTabButtonBounds, false);
        _updatesTabButtonHandle = CreateButton(UpdatesTabId, string.Empty, s_updatesTabButtonBounds, false);

        _locationAccessLabelHandle = CreateLabel(string.Empty, s_locationAccessLabelBounds, _homeControls);
        _locationAccessValueHandle = CreateLabel(string.Empty, s_locationAccessValueBounds, _homeControls);
        _useWindowsLocationHandle = CreateCheckBox(UseWindowsLocationId, string.Empty, s_useWindowsLocationBounds, _homeControls);
        _detectLocationButtonHandle = CreateButton(DetectLocationId, string.Empty, s_detectLocationButtonBounds, false, _homeControls);
        _latitudeLabelHandle = CreateLabel(string.Empty, s_latitudeLabelBounds, _homeControls);
        _latitudeEditHandle = CreateEdit(LatitudeEditId, s_latitudeEditBounds, DefaultCoordinateMaxCharacters, _homeControls);
        _longitudeLabelHandle = CreateLabel(string.Empty, s_longitudeLabelBounds, _homeControls);
        _longitudeEditHandle = CreateEdit(LongitudeEditId, s_longitudeEditBounds, DefaultCoordinateMaxCharacters, _homeControls);
        NativeInterop.SetPasswordCharacter(_latitudeEditHandle, HiddenCoordinateCharacter);
        NativeInterop.SetPasswordCharacter(_longitudeEditHandle, HiddenCoordinateCharacter);
        _precisionLabelHandle = CreateLabel(string.Empty, s_precisionLabelBounds, _homeControls);
        _precisionEditHandle = CreateEdit(PrecisionEditId, s_precisionEditBounds, PrecisionEditMaxCharacters, _homeControls);
        _privacyHintLabelHandle = CreateLabel(string.Empty, s_privacyHintBounds, _homeControls);
        _todayScheduleLabelHandle = CreateLabel(string.Empty, s_todayScheduleLabelBounds, _homeControls);
        _todayScheduleHandle = CreateLabel(string.Empty, s_todayScheduleValueBounds, _homeControls);

        _languageLabelHandle = CreateLabel(string.Empty, s_languageLabelBounds, _configurationControls);
        _languageSelectorHandle = CreateDropDownList(LanguageSelectorId, s_languageSelectorBounds, _configurationControls);
        _startWithWindowsHandle = CreateCheckBox(StartWithWindowsId, string.Empty, s_startWithWindowsBounds, _configurationControls);
        _startMinimizedHandle = CreateCheckBox(StartMinimizedId, string.Empty, s_startMinimizedBounds, _configurationControls);
        _highPriorityHandle = CreateCheckBox(HighPriorityId, string.Empty, s_highPriorityBounds, _configurationControls);
        _extraMinuteAtSunsetHandle = CreateCheckBox(ExtraMinuteAtSunsetId, string.Empty, s_extraMinuteAtSunsetBounds, _configurationControls);
        _runtimeStatusLabelHandle = CreateLabel(string.Empty, s_runtimeStatusLabelBounds, _configurationControls);
        _runtimeStatusHandle = CreateLabel(string.Empty, s_runtimeStatusValueBounds, _configurationControls);

        _automaticUpdatesHandle = CreateCheckBox(AutomaticUpdatesId, string.Empty, s_automaticUpdatesBounds, _updatesControls);
        _currentVersionLabelHandle = CreateLabel(string.Empty, s_currentVersionLabelBounds, _updatesControls);
        _currentVersionValueHandle = CreateLabel(string.Empty, s_currentVersionValueBounds, _updatesControls);
        _latestVersionLabelHandle = CreateLabel(string.Empty, s_latestVersionLabelBounds, _updatesControls);
        _latestVersionValueHandle = CreateLabel(string.Empty, s_latestVersionValueBounds, _updatesControls);
        _updateStatusLabelHandle = CreateLabel(string.Empty, s_updateStatusLabelBounds, _updatesControls);
        _updateStatusHandle = CreateLabel(string.Empty, s_updateStatusValueBounds, _updatesControls);
        _checkUpdatesButtonHandle = CreateButton(CheckUpdatesId, string.Empty, s_checkUpdatesButtonBounds, false, _updatesControls);

        SetActiveTab(SettingsTab.Home);
        ApplyLocalizedText();
    }

    private nint CreateLabel(string text, ControlBounds bounds, List<nint>? group = null)
    {
        return CreateControl(
            NativeStaticClassName,
            text,
            NativeInterop.SS_LEFT,
            NoExtendedStyle,
            bounds,
            NoControlId,
            group);
    }

    private nint CreateEdit(
        int controlId,
        ControlBounds bounds,
        int maxCharacters = DefaultCoordinateMaxCharacters,
        List<nint>? group = null)
    {
        nint editHandle = CreateControl(
            NativeEditClassName,
            string.Empty,
            NativeInterop.ES_AUTOHSCROLL | NativeInterop.WS_TABSTOP,
            NativeInterop.WS_EX_CLIENTEDGE,
            bounds,
            controlId,
            group);

        _ = NativeInterop.SendMessage(editHandle, NativeInterop.EM_SETLIMITTEXT, maxCharacters, nint.Zero);
        return editHandle;
    }

    private nint CreateCheckBox(
        int controlId,
        string text,
        ControlBounds bounds,
        List<nint>? group = null)
    {
        return CreateControl(
            NativeButtonClassName,
            text,
            NativeInterop.BS_AUTOCHECKBOX | NativeInterop.WS_TABSTOP,
            NoExtendedStyle,
            bounds,
            controlId,
            group);
    }

    private nint CreateDropDownList(
        int controlId,
        ControlBounds bounds,
        List<nint>? group = null)
    {
        return CreateControl(
            NativeComboBoxClassName,
            string.Empty,
            NativeInterop.CBS_DROPDOWNLIST | NativeInterop.WS_TABSTOP | NativeInterop.WS_VSCROLL,
            NoExtendedStyle,
            bounds,
            controlId,
            group);
    }

    private nint CreateButton(
        int controlId,
        string text,
        ControlBounds bounds,
        bool isDefault,
        List<nint>? group = null)
    {
        int style = NativeInterop.WS_TABSTOP;
        if (isDefault)
        {
            style |= NativeInterop.BS_DEFPUSHBUTTON;
        }

        return CreateControl(
            NativeButtonClassName,
            text,
            style,
            NoExtendedStyle,
            bounds,
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
                string.Format(
                    CultureInfo.InvariantCulture,
                    s_controlCreationCompositeFormat,
                    className));
        }

        NativeInterop.ApplyFont(controlHandle, _fontHandle);
        _allControls.Add(controlHandle);
        group?.Add(controlHandle);
        return controlHandle;
    }

    private void ApplyLocalizedText()
    {
        _ = NativeInterop.SetWindowText(_windowHandle, WindowTitle);
        _ = NativeInterop.SetWindowText(_headerLabelHandle, _localization[SettingsHeaderKey]);
        _ = NativeInterop.SetWindowText(_applyNowButtonHandle, _localization[SettingsSaveAndApplyKey]);
        _ = NativeInterop.SetWindowText(_homeTabButtonHandle, _localization[SettingsHomeTabKey]);
        _ = NativeInterop.SetWindowText(_configurationTabButtonHandle, _localization[SettingsConfigurationTabKey]);
        _ = NativeInterop.SetWindowText(_updatesTabButtonHandle, _localization[SettingsUpdatesTabKey]);

        _ = NativeInterop.SetWindowText(_locationAccessLabelHandle, _localization[SettingsLocationAccessKey]);
        _ = NativeInterop.SetWindowText(_useWindowsLocationHandle, _localization[SettingsUseWindowsLocationKey]);
        _ = NativeInterop.SetWindowText(_detectLocationButtonHandle, _localization[SettingsDetectFromWindowsKey]);
        _ = NativeInterop.SetWindowText(_latitudeLabelHandle, _localization[SettingsLatitudeKey]);
        _ = NativeInterop.SetWindowText(_longitudeLabelHandle, _localization[SettingsLongitudeKey]);
        _ = NativeInterop.SetWindowText(_precisionLabelHandle, _localization[SettingsPrecisionKey]);
        _ = NativeInterop.SetWindowText(_privacyHintLabelHandle, _localization[SettingsPrivacyHintKey]);
        _ = NativeInterop.SetWindowText(_todayScheduleLabelHandle, _localization[SettingsTodayScheduleKey]);

        _ = NativeInterop.SetWindowText(_languageLabelHandle, _localization[SettingsLanguageKey]);
        RefreshLanguageSelector();
        _ = NativeInterop.SetWindowText(_startWithWindowsHandle, _localization[SettingsStartWithWindowsKey]);
        _ = NativeInterop.SetWindowText(_startMinimizedHandle, _localization[SettingsOpenInTrayKey]);
        _ = NativeInterop.SetWindowText(_highPriorityHandle, _localization[SettingsUseHighPriorityKey]);
        _ = NativeInterop.SetWindowText(_extraMinuteAtSunsetHandle, _localization[SettingsExtraMinuteAtSunsetKey]);
        _ = NativeInterop.SetWindowText(_runtimeStatusLabelHandle, _localization[SettingsRuntimeStatusKey]);

        _ = NativeInterop.SetWindowText(_automaticUpdatesHandle, _localization[SettingsAutomaticUpdatesKey]);
        _ = NativeInterop.SetWindowText(_currentVersionLabelHandle, _localization[SettingsCurrentVersionKey]);
        _ = NativeInterop.SetWindowText(_latestVersionLabelHandle, _localization[SettingsLatestVersionKey]);
        _ = NativeInterop.SetWindowText(_updateStatusLabelHandle, _localization[SettingsUpdateStatusKey]);
        _ = NativeInterop.SetWindowText(_checkUpdatesButtonHandle, _localization[SettingsCheckUpdatesKey]);
    }

    private void RefreshLanguageSelector()
    {
        NativeInterop.ResetComboBoxContent(_languageSelectorHandle);
        NativeInterop.AddComboBoxString(_languageSelectorHandle, _localization[SettingsEnglishLanguageOptionKey]);
        NativeInterop.AddComboBoxString(_languageSelectorHandle, _localization[SettingsSpanishLanguageOptionKey]);
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
            ? s_darkBackgroundColorRef
            : s_lightBackgroundColorRef;
        int foregroundColorRef = themeMode == ThemeMode.Dark
            ? s_darkForegroundColorRef
            : s_lightForegroundColorRef;

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
        return HandledWindowMessageResult;
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
            ? LanguageSpanishIndex
            : LanguageEnglishIndex;
    }

    private static string GetLanguageCode(int selectedIndex)
    {
        return selectedIndex == LanguageSpanishIndex
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

        if ((controlId == LatitudeEditId || controlId == LongitudeEditId)
            && notificationCode == NativeInterop.EN_SETFOCUS)
        {
            SetCoordinateInputsVisible(areVisible: true);
            return;
        }

        if ((controlId == LatitudeEditId || controlId == LongitudeEditId)
            && notificationCode == NativeInterop.EN_KILLFOCUS)
        {
            RememberVisibleCoordinates();

            if (!CoordinateInputsHaveFocus())
            {
                SetCoordinateInputsVisible(areVisible: false);
            }

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

        if (!TryBeginOperation(_localization[SettingsApplyingThemeKey]))
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
                ShowMessage(_localization[SettingsThemeAppliedMessageKey], NativeInterop.MB_ICONINFORMATION);
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

        if (!TryBeginOperation(_localization[SettingsDetectingLocationKey]))
        {
            return;
        }

        _ = DetectLocationAsync(locationPrecisionResult.Value);
    }

    private void StartCheckForUpdates()
    {
        if (!TryBeginOperation(_localization[SettingsCheckingUpdatesKey]))
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
                    ShowMessage(_localization[SettingsNoUpdatesMessageKey], NativeInterop.MB_ICONINFORMATION);
                    return;
                }

                ShowMessage(_localization[SettingsUpdateAvailableMessageKey], NativeInterop.MB_ICONINFORMATION);
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
                _coordinateInputState.Remember(coordinates);
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
                SetCoordinateInputsVisible(areVisible: false, clearHiddenText: true);
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
        if (Interlocked.CompareExchange(ref _operationInProgress, BusyOperationState, IdleOperationState) != IdleOperationState)
        {
            ShowMessage(_localization[SettingsAlreadyRunningKey], NativeInterop.MB_ICONWARNING);
            return false;
        }

        SetBusy(isBusy: true, statusText);
        return true;
    }

    private void CompleteOperation()
    {
        _ = Interlocked.Exchange(ref _operationInProgress, IdleOperationState);
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
        _coordinateInputState.Load(configuration);

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
        SetCoordinateInputsVisible(areVisible: false, clearHiddenText: true);
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

        Result<GeoCoordinates> manualCoordinatesResult = ResolveManualCoordinates();
        return manualCoordinatesResult.IsFailure
            ? Result<AppConfig>.Failure(manualCoordinatesResult.Error)
            : (Result<AppConfig>)BuildConfiguration(
                manualCoordinatesResult.Value,
                useWindowsLocation,
                locationPrecisionDecimals);
    }

    private Result<GeoCoordinates> ResolveCoordinatesForWindowsLocation(AppConfig currentConfiguration)
    {
        return _coordinateInputState.ResolveWindowsLocationCoordinates(
            NativeInterop.GetWindowString(_latitudeEditHandle),
            NativeInterop.GetWindowString(_longitudeEditHandle),
            _coordinateInputsVisible,
            currentConfiguration);
    }

    private Result<GeoCoordinates> ResolveManualCoordinates()
    {
        return _coordinateInputState.ResolveManualCoordinates(
            NativeInterop.GetWindowString(_latitudeEditHandle),
            NativeInterop.GetWindowString(_longitudeEditHandle),
            _coordinateInputsVisible);
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
                    InvalidLocationPrecisionCode,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        s_invalidLocationPrecisionCompositeFormat,
                        CoordinatePrecisionPolicy.MinStoredDecimals,
                        CoordinatePrecisionPolicy.MaxStoredDecimals)))
            : locationPrecisionDecimals is < CoordinatePrecisionPolicy.MinStoredDecimals
            or > CoordinatePrecisionPolicy.MaxStoredDecimals
            ? Result<int>.Failure(
                Error.Validation(
                    InvalidLocationPrecisionCode,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        s_invalidLocationPrecisionCompositeFormat,
                        CoordinatePrecisionPolicy.MinStoredDecimals,
                        CoordinatePrecisionPolicy.MaxStoredDecimals)))
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
        bool isBusy = Volatile.Read(ref _operationInProgress) != IdleOperationState;
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
                ? _localization[SettingsUpdateNotCheckedKey]
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
        return !string.IsNullOrWhiteSpace(updateSnapshot.LastCheckErrorMessage)
            ? _localization[SettingsUpdateCheckFailedKey]
            : updateSnapshot.LastCheckedAtUtc is null
            ? _localization[SettingsUpdateIdleKey]
            : updateSnapshot.IsUpdateAvailable
            ? _localization[SettingsUpdateAvailableKey]
            : _localization[SettingsUpdateUpToDateKey];
    }

    private string GetLocationAccessText(SystemLocationAccessState accessState)
    {
        return accessState switch
        {
            SystemLocationAccessState.Allowed => _localization[SettingsLocationAccessAllowedKey],
            SystemLocationAccessState.Denied => _localization[SettingsLocationAccessDeniedKey],
            SystemLocationAccessState.Unavailable => _localization[SettingsLocationAccessUnavailableKey],
            SystemLocationAccessState.Unknown => _localization[SettingsLocationAccessUnknownKey],
            _ => _localization[SettingsLocationAccessUnknownKey]
        };
    }

    private void ShowValidationError(Error error)
    {
        ShowMessage(error.Description, NativeInterop.MB_ICONWARNING);
        FocusInvalidInput(error);
    }

    private bool CoordinateInputsHaveFocus()
    {
        nint focusedHandle = NativeInterop.GetFocus();
        return focusedHandle == _latitudeEditHandle || focusedHandle == _longitudeEditHandle;
    }

    private void RememberVisibleCoordinates()
    {
        if (!_coordinateInputsVisible)
        {
            return;
        }

        _coordinateInputState.RememberIfValid(
            NativeInterop.GetWindowString(_latitudeEditHandle),
            NativeInterop.GetWindowString(_longitudeEditHandle));
    }

    private void SetCoordinateInputsVisible(bool areVisible, bool clearHiddenText = false)
    {
        _coordinateInputsVisible = areVisible;

        if (_latitudeEditHandle == nint.Zero || _longitudeEditHandle == nint.Zero)
        {
            return;
        }

        char passwordCharacter = areVisible ? VisibleCoordinateCharacter : HiddenCoordinateCharacter;
        NativeInterop.SetPasswordCharacter(_latitudeEditHandle, passwordCharacter);
        NativeInterop.SetPasswordCharacter(_longitudeEditHandle, passwordCharacter);

        if (areVisible)
        {
            PopulateCoordinateInputsFromSeedIfEmpty();
            return;
        }

        if (clearHiddenText)
        {
            ClearCoordinateInputs();
        }
    }

    private void PopulateCoordinateInputsFromSeedIfEmpty()
    {
        if (!string.IsNullOrWhiteSpace(NativeInterop.GetWindowString(_latitudeEditHandle))
            || !string.IsNullOrWhiteSpace(NativeInterop.GetWindowString(_longitudeEditHandle)))
        {
            return;
        }

        int locationPrecisionDecimals = ResolveCoordinateDisplayPrecision();
        if (!_coordinateInputState.TryFormatSeed(
                locationPrecisionDecimals,
                out string latitudeText,
                out string longitudeText))
        {
            return;
        }

        _ = NativeInterop.SetWindowText(_latitudeEditHandle, latitudeText);
        _ = NativeInterop.SetWindowText(_longitudeEditHandle, longitudeText);
    }

    private void ClearCoordinateInputs()
    {
        _ = NativeInterop.SetWindowText(_latitudeEditHandle, string.Empty);
        _ = NativeInterop.SetWindowText(_longitudeEditHandle, string.Empty);
    }

    private int ResolveCoordinateDisplayPrecision()
    {
        Result<int> locationPrecisionResult = ParseLocationPrecisionFromForm();
        return locationPrecisionResult.IsSuccess
            ? locationPrecisionResult.Value
            : _applicationLifecycleOrchestrator.Config.LocationPrecisionDecimals;
    }

    private void FocusInvalidInput(Error error)
    {
        nint controlHandle = _latitudeEditHandle;

        if (string.Equals(error.Code, InvalidLongitudeCode, StringComparison.Ordinal))
        {
            controlHandle = _longitudeEditHandle;
        }
        else if (string.Equals(error.Code, InvalidLocationPrecisionCode, StringComparison.Ordinal))
        {
            controlHandle = _precisionEditHandle;
        }
        else if (string.Equals(error.Code, MissingLocationSeedCode, StringComparison.Ordinal))
        {
            controlHandle = _useWindowsLocationHandle;
        }

        _ = NativeInterop.SetFocus(controlHandle);

        if (controlHandle == _latitudeEditHandle
            || controlHandle == _longitudeEditHandle
            || controlHandle == _precisionEditHandle)
        {
            _ = NativeInterop.SendMessage(controlHandle, NativeInterop.EM_SETSEL, nint.Zero, SelectAllTextEnd);
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
        return red | (green << GreenChannelShift) | (blue << BlueChannelShift);
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
