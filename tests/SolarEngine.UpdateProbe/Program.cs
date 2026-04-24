// Copyright (c) 2026 Humberto Schoenwald.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text;

namespace SolarEngine.UpdateProbe;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        string? markerPath = Environment.GetEnvironmentVariable("SOLAR_ENGINE_UPDATE_PROBE_MARKER_PATH");
        if (string.IsNullOrWhiteSpace(markerPath))
        {
            return;
        }

        string? markerDirectory = Path.GetDirectoryName(markerPath);
        if (!string.IsNullOrWhiteSpace(markerDirectory))
        {
            _ = Directory.CreateDirectory(markerDirectory);
        }

        File.WriteAllText(
            markerPath,
            DateTimeOffset.UtcNow.ToString("O"),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }
}
