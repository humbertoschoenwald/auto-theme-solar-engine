// Copyright (c) 2026 Humberto Schoenwald.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace SolarEngine.Features.Updates.Infrastructure;

internal sealed record PersistedUpdateRequest
{
    public int ProcessId
    {
        get; init;
    }

    public string DownloadedExecutablePath { get; init; } = string.Empty;

    public string InstalledExecutablePath { get; init; } = string.Empty;

    public bool StartWithWindows
    {
        get; init;
    }
}
