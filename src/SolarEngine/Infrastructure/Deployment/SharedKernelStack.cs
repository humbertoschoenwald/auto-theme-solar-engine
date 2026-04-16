namespace SolarEngine.Infrastructure.Deployment;

internal static class SharedKernelStack
{
    public const string StackName = "solar-theme-shared-kernel";
    public const string StackRegion = "local-development";

    public static string QualifiedStackName => $"{StackName}-{StackRegion}";

    public static bool IsLocalDevelopment =>
        string.Equals(StackRegion, "local-development", StringComparison.Ordinal);
}
