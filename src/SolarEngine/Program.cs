// Copyright (c) 2026 Humberto Schoenwald.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace SolarEngine;

internal static class Program
{
    [STAThread]
    private static int Main()
    {
        using NativeApplication application = new();
        return application.Run();
    }
}
