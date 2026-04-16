namespace SolarEngine.Features.Themes.Domain;

internal sealed class SchedulerStateChangedEventArgs : EventArgs
{
    public SchedulerStateChangedEventArgs(string tooltip)
    {
        ArgumentNullException.ThrowIfNull(tooltip);
        Tooltip = tooltip;
    }

    public string Tooltip { get; }
}
