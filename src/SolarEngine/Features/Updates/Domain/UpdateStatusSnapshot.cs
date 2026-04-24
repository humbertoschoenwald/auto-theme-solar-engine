// Copyright (c) 2026 Humberto Schoenwald.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace SolarEngine.Features.Updates.Domain;

internal sealed record UpdateStatusSnapshot
{
    public required CalVersion CurrentVersion
    {
        get; init;
    }

    public required ReleaseFlavor ReleaseFlavor
    {
        get; init;
    }

    public required InstallationMode InstallationMode
    {
        get; init;
    }

    public string? LatestVersionTag
    {
        get; init;
    }

    public bool IsUpdateAvailable
    {
        get; init;
    }

    public string? PendingAssetName
    {
        get; init;
    }

    public DateTimeOffset? LastCheckedAtUtc
    {
        get; init;
    }

    public string? LastCheckErrorMessage
    {
        get; init;
    }
}
