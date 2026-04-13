using Microsoft.Win32;
using SolarEngine.Infrastructure.Logging;

namespace SolarEngine.Features.SystemHost.Infrastructure;

internal sealed class WindowsStartupRegistrar(StructuredLogPublisher logPublisher)
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "SolarEngine";

    public void SetEnabled(bool enabled, string executablePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executablePath);

        using RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true)
            ?? throw new InvalidOperationException("Resolve the current-user Run key before mutating startup state.");

        if (enabled)
        {
            string quotedPath = Quote(executablePath);
            object? currentValue = key.GetValue(ValueName);

            if (!string.Equals(currentValue as string, quotedPath, StringComparison.Ordinal))
            {
                key.SetValue(ValueName, quotedPath, RegistryValueKind.String);
                logPublisher.Write("Startup registration enabled.");
            }

            return;
        }

        if (key.GetValue(ValueName) is null)
        {
            return;
        }

        key.DeleteValue(ValueName, throwOnMissingValue: false);
        logPublisher.Write("Startup registration disabled.");
    }

    private static string Quote(string path)
    {
        ReadOnlySpan<char> span = path.AsSpan().Trim();
        return span is ['"', .., '"']
            ? span.ToString()
            : string.Concat("\"", span, "\"");
    }
}
