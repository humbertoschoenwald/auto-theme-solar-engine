// Copyright (c) 2026 Humberto Schoenwald.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace SolarEngine.Features.Updates.Domain;

internal sealed record InstallationMetadata
{
    public required string InstallDirectory
    {
        get; init;
    }

    public required string InstalledExecutablePath
    {
        get; init;
    }

    public required InstallationMode InstallationMode
    {
        get; init;
    }

    public required ReleaseFlavor ReleaseFlavor
    {
        get; init;
    }

    public string? ElevatedTaskName
    {
        get; init;
    }
}
