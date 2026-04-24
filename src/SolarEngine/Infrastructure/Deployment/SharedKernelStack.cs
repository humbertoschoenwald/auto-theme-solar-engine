// Copyright (c) 2026 Humberto Schoenwald.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace SolarEngine.Infrastructure.Deployment;

internal static class SharedKernelStack
{
    private const string LocalDevelopmentRegion = "local-development";

    public const string StackName = "solar-theme-shared-kernel";
    public const string StackRegion = LocalDevelopmentRegion;

    public static string QualifiedStackName => $"{StackName}-{StackRegion}";

    public static bool IsLocalDevelopment =>
        string.Equals(StackRegion, LocalDevelopmentRegion, StringComparison.Ordinal);
}
