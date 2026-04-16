using Microsoft.Win32;
using SolarEngine.Infrastructure.Logging;
using SolarEngine.Shared;
using SolarEngine.Shared.Core;

namespace SolarEngine.Features.SystemHost.Infrastructure;

internal sealed class WindowsStartupRegistrar(StructuredLogPublisher logPublisher)
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = AppIdentity.RuntimeName;
    private const string LegacyValueName = AppIdentity.LegacyRuntimeName;

    public void SetEnabled(bool enabled, string executablePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executablePath);

        using RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true)
            ?? throw new UnexpectedStateException("Resolve the current-user Run key before mutating startup state.");

        if (enabled)
        {
            string quotedPath = Quote(executablePath);
            object? currentValue = key.GetValue(ValueName);

            if (!string.Equals(currentValue as string, quotedPath, StringComparison.Ordinal))
            {
                key.SetValue(ValueName, quotedPath, RegistryValueKind.String);
                logPublisher.Write("Startup registration enabled.");
            }

            if (key.GetValue(LegacyValueName) is not null)
            {
                key.DeleteValue(LegacyValueName, throwOnMissingValue: false);
                logPublisher.Write("Legacy startup registration removed.");
            }

            return;
        }

        bool removedValue = false;

        if (key.GetValue(ValueName) is not null)
        {
            key.DeleteValue(ValueName, throwOnMissingValue: false);
            removedValue = true;
        }

        if (key.GetValue(LegacyValueName) is not null)
        {
            key.DeleteValue(LegacyValueName, throwOnMissingValue: false);
            removedValue = true;
        }

        if (removedValue)
        {
            logPublisher.Write("Startup registration disabled.");
        }
    }

    private static string Quote(string path)
    {
        ReadOnlySpan<char> span = path.AsSpan().Trim();
        return span is ['"', .., '"']
            ? span.ToString()
            : string.Concat("\"", span, "\"");
    }
}
