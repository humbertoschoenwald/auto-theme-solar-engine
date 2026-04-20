using System.Globalization;
using System.Text;

namespace SolarEngine.Infrastructure.Logging;

internal sealed class StructuredLogPublisher(string logPath)
{
    internal const int MaxLogBytes = 10 * 1024;
    private const int TimestampAndDelimiterLength = 28;

    private static readonly UTF8Encoding s_utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    private readonly Lock _sync = new();
    private readonly string _logPath = !string.IsNullOrWhiteSpace(logPath)
        ? Path.GetFullPath(logPath)
        : throw new ArgumentException("Provide a non-empty log path.", nameof(logPath));
    private readonly string _directoryPath = !string.IsNullOrWhiteSpace(logPath)
        ? Path.GetDirectoryName(Path.GetFullPath(logPath))
            ?? throw new DirectoryNotFoundException("Resolve the log directory before writing telemetry.")
        : throw new ArgumentException("Provide a non-empty log path.", nameof(logPath));

    public void Write(string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        string line = string.Create(
            message.Length + TimestampAndDelimiterLength,
            message,
            static (span, state) =>
            {
                DateTimeOffset now = DateTimeOffset.Now;
                _ = now.TryFormat(span, out int written, "yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
                " | ".AsSpan().CopyTo(span[written..]);
                written += 3;
                state.AsSpan().CopyTo(span[written..]);
                written += state.Length;
                span[written++] = '\r';
                span[written] = '\n';
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
        while (startIndex < logBytes.Length && logBytes[startIndex] != (byte)'\n')
        {
            startIndex++;
        }

        startIndex = startIndex < logBytes.Length
            ? startIndex + 1
            : logBytes.Length - MaxLogBytes;

        int remainingLength = logBytes.Length - startIndex;
        byte[] trimmedBytes = new byte[remainingLength];
        Buffer.BlockCopy(logBytes, startIndex, trimmedBytes, 0, remainingLength);
        File.WriteAllBytes(_logPath, trimmedBytes);
    }
}
