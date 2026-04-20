using SolarEngine.Features.Locations.Domain;
using SolarEngine.Shared.Core;
using Xunit;

namespace SolarEngine.Tests.Features.Locations;

/// <summary>
/// Verifies domain coordinate creation guards geographic and numeric invariants.
/// </summary>
[Trait("TestLane", "Light")]
public sealed class GeoCoordinatesTests
{
    /// <summary>
    /// Verifies invalid numeric values and out-of-range coordinates are rejected.
    /// </summary>
    [Theory]
    [InlineData(double.NaN, 0d)]
    [InlineData(0d, double.NaN)]
    [InlineData(double.PositiveInfinity, 0d)]
    [InlineData(0d, double.NegativeInfinity)]
    [InlineData(90.0001d, 0d)]
    [InlineData(-90.0001d, 0d)]
    [InlineData(0d, 180.0001d)]
    [InlineData(0d, -180.0001d)]
    public void Create_ReturnsFailure_ForInvalidAndOutOfBoundsCoordinates(double latitude, double longitude)
    {
        Result<GeoCoordinates> result = GeoCoordinates.Create(latitude, longitude);

        Assert.True(result.IsFailure);

        Assert.NotNull(result.Error);
        Assert.NotEqual(string.Empty, result.Error.Code);
    }

    /// <summary>
    /// Verifies valid geographic coordinates produce an immutable value object.
    /// </summary>
    [Fact]
    public void Create_ReturnsSuccess_ForValidCoordinates()
    {
        Result<GeoCoordinates> result = GeoCoordinates.Create(19.4326d, -99.1332d);

        Assert.True(result.IsSuccess);

        Assert.Equal(19.4326d, result.Value.Latitude);
        Assert.Equal(-99.1332d, result.Value.Longitude);
    }
}
