using SolarEngine.Features.Locations.Domain;
using SolarEngine.Features.SystemHost.Domain;
using SolarEngine.Shared.Core;
using SolarEngine.UI;
using Xunit;

namespace SolarEngine.Tests.UI;

/// <summary>
/// Verifies coordinate seed handling keeps masked UI text from breaking saves.
/// </summary>
[Trait("TestLane", "Light")]
public sealed class CoordinateInputStateTests
{
    /// <summary>
    /// Verifies a Windows-detected seed survives masked coordinate text during save.
    /// </summary>
    [Fact]
    public void ResolveWindowsLocationCoordinates_UsesDetectedSeedWhenInputsAreHidden()
    {
        CoordinateInputState state = new();
        GeoCoordinates detectedCoordinates = new()
        {
            Latitude = 19.4326d,
            Longitude = -99.1332d
        };

        state.Remember(detectedCoordinates);

        Result<GeoCoordinates> result = state.ResolveWindowsLocationCoordinates(
            "***",
            "***",
            inputsVisible: false,
            new AppConfig());

        Assert.True(result.IsSuccess);
        Assert.Equal(detectedCoordinates.Latitude, result.Value.Latitude);
        Assert.Equal(detectedCoordinates.Longitude, result.Value.Longitude);
    }

    /// <summary>
    /// Verifies visible manual edits update the remembered seed once they parse successfully.
    /// </summary>
    [Fact]
    public void RememberIfValid_UpdatesSeedFromVisibleManualCoordinates()
    {
        CoordinateInputState state = new();

        state.RememberIfValid("19.500", "-99.100");

        Result<GeoCoordinates> result = state.ResolveManualCoordinates(
            "***",
            "***",
            inputsVisible: false);

        Assert.True(result.IsSuccess);
        Assert.Equal(19.5d, result.Value.Latitude);
        Assert.Equal(-99.1d, result.Value.Longitude);
    }

    /// <summary>
    /// Verifies hidden coordinate inputs can be repopulated from the remembered seed without exposing raw stored values.
    /// </summary>
    [Fact]
    public void TryFormatSeed_ReturnsFormattedCoordinatesFromRememberedSeed()
    {
        CoordinateInputState state = new();
        state.Remember(new GeoCoordinates
        {
            Latitude = 19.4326d,
            Longitude = -99.1332d
        });

        bool hasSeed = state.TryFormatSeed(
            3,
            out string latitudeText,
            out string longitudeText);

        Assert.True(hasSeed);
        Assert.Equal("19.433", latitudeText);
        Assert.Equal("-99.133", longitudeText);
    }
}
