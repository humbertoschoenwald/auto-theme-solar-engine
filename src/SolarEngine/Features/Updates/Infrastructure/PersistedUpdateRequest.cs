namespace SolarEngine.Features.Updates.Infrastructure;

internal sealed record PersistedUpdateRequest
{
    public int ProcessId { get; init; }

    public string DownloadedExecutablePath { get; init; } = string.Empty;

    public string InstalledExecutablePath { get; init; } = string.Empty;

    public bool StartWithWindows { get; init; }

    public bool LaunchAfterApply { get; init; } = true;
}
