// Copyright (c) 2026 Humberto Schoenwald.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using System.Text;

namespace SolarEngine.Infrastructure.Logging;

internal sealed class StructuredLogPublisher(string logPath)
{
    internal const int MaxLogBytes = 10 * 1024;
    private const string Delimiter = " | ";
    private const string InvalidLogPathErrorMessage = "Provide a non-empty log path.";
    private const string LogDirectoryErrorMessage = "Resolve the log directory before writing telemetry.";
    private const char NewLineLineFeed = '\n';
    private const char NewLineCarriageReturn = '\r';
    private const int TimestampAndDelimiterLength = 28;
    private const int TrimmedStartIndexAdvance = 1;
    private const int CopyDestinationIndex = 0;
    private const string TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff";

    private static readonly UTF8Encoding s_utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    private readonly Lock _sync = new();
    private readonly string _logPath = !string.IsNullOrWhiteSpace(logPath)
        ? Path.GetFullPath(logPath)
        : throw new ArgumentException(InvalidLogPathErrorMessage, nameof(logPath));
    private readonly string _directoryPath = !string.IsNullOrWhiteSpace(logPath)
        ? Path.GetDirectoryName(Path.GetFullPath(logPath))
            ?? throw new DirectoryNotFoundException(LogDirectoryErrorMessage)
        : throw new ArgumentException(InvalidLogPathErrorMessage, nameof(logPath));

    public void Write(string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        string line = string.Create(
            message.Length + TimestampAndDelimiterLength,
            message,
            static (span, state) =>
            {
                DateTimeOffset now = DateTimeOffset.Now;
                _ = now.TryFormat(
                    span,
                    out int written,
                    TimestampFormat,
                    CultureInfo.InvariantCulture);
                Delimiter.AsSpan().CopyTo(span[written..]);
                written += Delimiter.Length;
                state.AsSpan().CopyTo(span[written..]);
                written += state.Length;
                span[written++] = NewLineCarriageReturn;
                span[written] = NewLineLineFeed;
            });

        lock (_sync)
        {
            _ = Directory.CreateDirectory(_directoryPath);
            File.AppendAllText(_logPath, line, s_utf8WithoutBom);
            TrimLogIfNeeded();
        }
    }

    private void TrimLogIfNeeded()
    {
        byte[] logBytes = File.ReadAllBytes(_logPath);
        if (logBytes.Length <= MaxLogBytes)
        {
            return;
        }

        int startIndex = logBytes.Length - MaxLogBytes;
        while (startIndex < logBytes.Length && logBytes[startIndex] != (byte)NewLineLineFeed)
        {
            startIndex++;
        }

        startIndex = startIndex < logBytes.Length
            ? startIndex + TrimmedStartIndexAdvance
            : logBytes.Length - MaxLogBytes;

        int remainingLength = logBytes.Length - startIndex;
        byte[] trimmedBytes = new byte[remainingLength];
        Buffer.BlockCopy(logBytes, startIndex, trimmedBytes, CopyDestinationIndex, remainingLength);
        File.WriteAllBytes(_logPath, trimmedBytes);
    }
}
