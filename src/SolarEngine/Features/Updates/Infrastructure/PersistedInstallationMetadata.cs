using SolarEngine.Shared;

namespace SolarEngine.Features.Updates.Infrastructure;

internal sealed record PersistedInstallationMetadata
{
    public string InstalledExecutableName { get; init; } = AppIdentity.ExecutableFileName;

    public string ReleaseFlavor { get; init; } = "self-contained";

    public string InstallationMode { get; init; } = "unknown";

    public string? ElevatedTaskName { get; init; }
}
