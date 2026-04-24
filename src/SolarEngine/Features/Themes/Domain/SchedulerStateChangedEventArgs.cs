// Copyright (c) 2026 Humberto Schoenwald.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace SolarEngine.Features.Themes.Domain;

internal sealed class SchedulerStateChangedEventArgs : EventArgs
{
    public SchedulerStateChangedEventArgs(string tooltip)
    {
        ArgumentNullException.ThrowIfNull(tooltip);
        Tooltip = tooltip;
    }

    public string Tooltip
    {
        get;
    }
}
