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
