namespace SolarEngine.Features.Updates.Domain;

internal sealed record InstallationMetadata
{
    public required string InstallDirectory { get; init; }

    public required string InstalledExecutablePath { get; init; }

    public required InstallationMode InstallationMode { get; init; }

    public required ReleaseFlavor ReleaseFlavor { get; init; }

    public string? ElevatedTaskName { get; init; }
}
