using SolarEngine.Features.SystemHost.Domain;
using SolarEngine.Features.SystemHost.Infrastructure;
using SolarEngine.Infrastructure.Logging;
using Xunit;

namespace SolarEngine.Tests.Features.SystemHost.Infrastructure;

/// <summary>
/// Verifies configuration persistence privacy and legacy migration behavior.
/// </summary>
public sealed class ConfigurationRepositoryTests
{
    /// <summary>
    /// Verifies saved coordinates are protected and reduced to configured precision.
    /// </summary>
    [Fact]
    public void Save_StoresAndLoadsReducedPrecisionCoordinates()
    {
        string directoryPath = CreateTemporaryDirectory();

        try
        {
            AppPaths appPaths = new(directoryPath);
            StructuredLogPublisher logPublisher = new(appPaths.LogPath);
            ConfigurationRepository repository = new(appPaths, logPublisher);

            repository.Save(new AppConfig
            {
                Latitude = 19.4326077123d,
                Longitude = -99.1332080000d,
                UseWindowsLocation = true,
                LocationPrecisionDecimals = 3,
                IsConfigured = true
            });

            AppConfig configuration = repository.Load();
            string persistedJson = File.ReadAllText(appPaths.ConfigPath);

            Assert.Equal(19.433d, configuration.Latitude);
            Assert.Equal(-99.133d, configuration.Longitude);
            Assert.Equal(3, configuration.LocationPrecisionDecimals);
            Assert.True(configuration.AddExtraMinuteAtSunset);
            Assert.DoesNotContain("\"Latitude\"", persistedJson, StringComparison.Ordinal);
            Assert.DoesNotContain("\"Longitude\"", persistedJson, StringComparison.Ordinal);
        }
        finally
        {
            DeleteDirectoryIfPresent(directoryPath);
        }
    }

    /// <summary>
    /// Verifies legacy plaintext coordinate files migrate to protected storage.
    /// </summary>
    [Fact]
    public void Load_RewritesLegacyPlaintextCoordinatesIntoProtectedReducedPrecisionStorage()
    {
        string directoryPath = CreateTemporaryDirectory();

        try
        {
            AppPaths appPaths = new(directoryPath);
            StructuredLogPublisher logPublisher = new(appPaths.LogPath);
            ConfigurationRepository repository = new(appPaths, logPublisher);

            File.WriteAllText(
                appPaths.ConfigPath,
                                     /*lang=json,strict*/
                                     """
                {
                  "Latitude": 19.4326077123,
                  "Longitude": -99.133208,
                  "UseWindowsLocation": true,
                  "LocationPrecisionDecimals": 2,
                  "IsConfigured": true
                }
                """);

            AppConfig configuration = repository.Load();
            string persistedJson = File.ReadAllText(appPaths.ConfigPath);

            Assert.Equal(19.43d, configuration.Latitude);
            Assert.Equal(-99.13d, configuration.Longitude);
            Assert.Contains("\"ProtectedCoordinates\"", persistedJson, StringComparison.Ordinal);
            Assert.DoesNotContain("\"Latitude\"", persistedJson, StringComparison.Ordinal);
            Assert.DoesNotContain("\"Longitude\"", persistedJson, StringComparison.Ordinal);
        }
        finally
        {
            DeleteDirectoryIfPresent(directoryPath);
        }
    }

    private static string CreateTemporaryDirectory()
    {
        string directoryPath = Path.Combine(
            Path.GetTempPath(),
            "SolarEngine.Tests",
            Path.GetRandomFileName());
        _ = Directory.CreateDirectory(directoryPath);
        return directoryPath;
    }

    private static void DeleteDirectoryIfPresent(string directoryPath)
    {
        if (Directory.Exists(directoryPath))
        {
            Directory.Delete(directoryPath, recursive: true);
        }
    }
}
