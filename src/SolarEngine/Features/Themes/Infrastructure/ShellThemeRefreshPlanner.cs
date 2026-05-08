// Copyright (c) 2026 Humberto Schoenwald.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace SolarEngine.Features.Themes.Infrastructure;

internal static class ShellThemeRefreshPlanner
{
    internal const string ShellTrayWindowClassName = "Shell_TrayWnd";
    internal const string ShellSecondaryTrayWindowClassName = "Shell_SecondaryTrayWnd";
    internal const string ProgramManagerWindowClassName = "Program";
    internal const string WorkerWindowClassName = "WorkerW";

    internal static void RefreshShellWindows(
        IEnumerable<ShellWindowInfo> topLevelWindows,
        Func<nint, IEnumerable<nint>> enumerateDescendantWindowHandles,
        Action<nint> refreshWindow)
    {
        ArgumentNullException.ThrowIfNull(topLevelWindows);
        ArgumentNullException.ThrowIfNull(enumerateDescendantWindowHandles);
        ArgumentNullException.ThrowIfNull(refreshWindow);

        HashSet<nint> refreshedWindowHandles = [];

        foreach (ShellWindowInfo window in topLevelWindows)
        {
            if (!ShouldRefreshWindowClass(window.ClassName))
            {
                continue;
            }

            RefreshWindowOnce(window.WindowHandle, refreshedWindowHandles, refreshWindow);

            if (!ShouldRefreshDescendants(window.ClassName))
            {
                continue;
            }

            foreach (nint descendantWindowHandle in enumerateDescendantWindowHandles(window.WindowHandle))
            {
                RefreshWindowOnce(descendantWindowHandle, refreshedWindowHandles, refreshWindow);
            }
        }
    }

    private static bool ShouldRefreshWindowClass(string className)
    {
        return className is ShellTrayWindowClassName
            or ShellSecondaryTrayWindowClassName
            or ProgramManagerWindowClassName
            or WorkerWindowClassName;
    }

    private static bool ShouldRefreshDescendants(string className)
    {
        return className is ShellTrayWindowClassName or ShellSecondaryTrayWindowClassName;
    }

    private static void RefreshWindowOnce(
        nint windowHandle,
        HashSet<nint> refreshedWindowHandles,
        Action<nint> refreshWindow)
    {
        if (windowHandle == nint.Zero || !refreshedWindowHandles.Add(windowHandle))
        {
            return;
        }

        refreshWindow(windowHandle);
    }
}
