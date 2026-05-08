// Copyright (c) 2026 Humberto Schoenwald.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SolarEngine.Features.Themes.Infrastructure;
using Xunit;

namespace SolarEngine.Tests.Features.Themes.Infrastructure;

/// <summary>
/// Verifies shell refresh targeting covers multi-monitor taskbar windows.
/// </summary>
[Trait("TestLane", "Light")]
public sealed class ShellThemeRefreshPlannerTests
{
    /// <summary>
    /// Verifies all taskbar instances and their child windows are refreshed.
    /// </summary>
    [Fact]
    public void RefreshShellWindowsRefreshesEveryTaskbarAndTaskbarDescendant()
    {
        nint firstPrimaryTaskbar = new(100);
        nint secondPrimaryTaskbar = new(101);
        nint secondaryTaskbar = new(200);
        nint primaryTaskbarChild = new(300);
        nint secondaryTaskbarChild = new(400);
        List<nint> refreshedWindowHandles = [];
        ShellWindowInfo[] topLevelWindows =
        [
            new(firstPrimaryTaskbar, ShellThemeRefreshPlanner.ShellTrayWindowClassName),
            new(secondPrimaryTaskbar, ShellThemeRefreshPlanner.ShellTrayWindowClassName),
            new(secondaryTaskbar, ShellThemeRefreshPlanner.ShellSecondaryTrayWindowClassName)
        ];

        ShellThemeRefreshPlanner.RefreshShellWindows(
            topLevelWindows,
            windowHandle => windowHandle == secondaryTaskbar
                ? [secondaryTaskbarChild]
                : [primaryTaskbarChild],
            refreshedWindowHandles.Add);

        Assert.Equal(
            [
                firstPrimaryTaskbar,
                primaryTaskbarChild,
                secondPrimaryTaskbar,
                secondaryTaskbar,
                secondaryTaskbarChild
            ],
            refreshedWindowHandles);
    }

    /// <summary>
    /// Verifies desktop shell windows remain root-only refresh targets.
    /// </summary>
    [Fact]
    public void RefreshShellWindowsDoesNotRefreshDesktopDescendants()
    {
        nint desktopWindow = new(500);
        nint desktopChild = new(501);
        List<nint> refreshedWindowHandles = [];
        ShellWindowInfo[] topLevelWindows =
        [
            new(desktopWindow, ShellThemeRefreshPlanner.WorkerWindowClassName)
        ];

        ShellThemeRefreshPlanner.RefreshShellWindows(
            topLevelWindows,
            _ => [desktopChild],
            refreshedWindowHandles.Add);

        Assert.Equal([desktopWindow], refreshedWindowHandles);
    }

    /// <summary>
    /// Verifies top-level app windows are notified exactly once.
    /// </summary>
    [Fact]
    public void NotifyTopLevelWindowsNotifiesEveryDistinctTopLevelWindow()
    {
        nint firstWindow = new(600);
        nint secondWindow = new(601);
        List<nint> notifiedWindowHandles = [];
        ShellWindowInfo[] topLevelWindows =
        [
            new(firstWindow, "Chrome_WidgetWin_1"),
            new(firstWindow, "Chrome_WidgetWin_1"),
            new(nint.Zero, "Ghost"),
            new(secondWindow, "ApplicationFrameWindow")
        ];

        ShellThemeRefreshPlanner.NotifyTopLevelWindows(topLevelWindows, notifiedWindowHandles.Add);

        Assert.Equal([firstWindow, secondWindow], notifiedWindowHandles);
    }
}
