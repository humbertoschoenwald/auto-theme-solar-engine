using System.Text;
using SolarEngine.Infrastructure.Logging;
using Xunit;

namespace SolarEngine.Tests.Infrastructure.Logging;

/// <summary>
/// Verifies structured log persistence under bounded file-size pressure.
/// </summary>
public sealed class StructuredLogPublisherTests
{
    /// <summary>
    /// Verifies oversized logs retain recent entries and discard older content.
    /// </summary>
    [Fact]
    public void Write_TrimsTheLogToTheConfiguredMaximumSize()
    {
        string directoryPath = Path.Combine(
            Path.GetTempPath(),
            "SolarEngine.Tests",
            Path.GetRandomFileName());
        _ = Directory.CreateDirectory(directoryPath);

        try
        {
            string logPath = Path.Combine(directoryPath, "SolarEngine.log");
            StructuredLogPublisher publisher = new(logPath);

            for (int index = 0; index < 256; index++)
            {
                publisher.Write($"entry-{index:D3} {new string('x', 96)}");
            }

            FileInfo logFile = new(logPath);
            string logContents = File.ReadAllText(logPath, Encoding.UTF8);

            Assert.True(logFile.Length <= StructuredLogPublisher.MaxLogBytes);
            Assert.Contains("entry-255", logContents, StringComparison.Ordinal);
            Assert.DoesNotContain("entry-000", logContents, StringComparison.Ordinal);
            Assert.DoesNotContain("\0", logContents, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(directoryPath))
            {
                Directory.Delete(directoryPath, recursive: true);
            }
        }
    }
}
