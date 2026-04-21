using SolarEngine.Shared;

namespace SolarEngine.Features.Updates.Infrastructure;

internal sealed record PersistedInstallationMetadata
{
    private const string DefaultReleaseFlavor = "self-contained";
    private const string DefaultInstallationMode = "unknown";

    public string InstalledExecutableName { get; init; } = AppIdentity.ExecutableFileName;

    public string ReleaseFlavor { get; init; } = DefaultReleaseFlavor;

    public string InstallationMode { get; init; } = DefaultInstallationMode;

    public string? ElevatedTaskName { get; init; }
}
