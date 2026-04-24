// Copyright (c) 2026 Humberto Schoenwald.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Win32;
using SolarEngine.Infrastructure.Logging;
using SolarEngine.Shared;
using SolarEngine.Shared.Core;

namespace SolarEngine.Features.SystemHost.Infrastructure;

internal sealed class WindowsStartupRegistrar(StructuredLogPublisher logPublisher)
{
    private const string CurrentUserRunKeyErrorMessage = "Resolve the current-user Run key before mutating startup state.";
    private const char QuoteCharacter = '"';
    private const string QuoteString = "\"";
    private const string StartupDisabledLogMessage = "Startup registration disabled.";
    private const string StartupEnabledLogMessage = "Startup registration enabled.";
    private const string StartupLegacyRemovedLogMessage = "Legacy startup registration removed.";
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = AppIdentity.StartupValueName;
    private const string LegacyValueName = AppIdentity.LegacyStartupValueName;

    public void SetEnabled(bool enabled, string executablePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executablePath);

        using RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true)
            ?? throw new UnexpectedStateException(CurrentUserRunKeyErrorMessage);

        if (enabled)
        {
            string quotedPath = Quote(executablePath);
            object? currentValue = key.GetValue(ValueName);

            if (!string.Equals(currentValue as string, quotedPath, StringComparison.Ordinal))
            {
                key.SetValue(ValueName, quotedPath, RegistryValueKind.String);
                logPublisher.Write(StartupEnabledLogMessage);
            }

            if (key.GetValue(LegacyValueName) is not null)
            {
                key.DeleteValue(LegacyValueName, throwOnMissingValue: false);
                logPublisher.Write(StartupLegacyRemovedLogMessage);
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
            logPublisher.Write(StartupDisabledLogMessage);
        }
    }

    private static string Quote(string path)
    {
        ReadOnlySpan<char> span = path.AsSpan().Trim();
        return span is [QuoteCharacter, .., QuoteCharacter]
            ? span.ToString()
            : string.Concat(QuoteString, span, QuoteString);
    }
}
