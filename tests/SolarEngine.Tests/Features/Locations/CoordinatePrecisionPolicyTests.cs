using SolarEngine.Features.Locations.Domain;
using Xunit;

namespace SolarEngine.Tests.Features.Locations;

/// <summary>
/// Verifies coordinate precision policy boundaries and persisted coordinate reduction.
/// </summary>
[Trait("TestLane", "Light")]
public sealed class CoordinatePrecisionPolicyTests
{
    /// <summary>
    /// Verifies persisted precision stays inside the supported storage range.
    /// </summary>
    [Theory]
    [InlineData(-1, CoordinatePrecisionPolicy.DefaultStoredDecimals)]
    [InlineData(0, CoordinatePrecisionPolicy.DefaultStoredDecimals)]
    [InlineData(1, CoordinatePrecisionPolicy.MinStoredDecimals)]
    [InlineData(3, 3)]
    [InlineData(9, CoordinatePrecisionPolicy.MaxStoredDecimals)]
    public void NormalizeDecimals_ClampsIntoAllowedRange(int input, int expected)
    {
        int result = CoordinatePrecisionPolicy.NormalizeDecimals(input);

        Assert.Equal(expected, result);
    }

    /// <summary>
    /// Verifies coordinate persistence rounds both axes with the configured precision.
    /// </summary>
    [Fact]
    public void Reduce_RoundsCoordinatesToConfiguredPrecision()
    {
        GeoCoordinates coordinates = new()
        {
            Latitude = 19.4326077123d,
            Longitude = -99.1332080000d
        };

        GeoCoordinates reduced = CoordinatePrecisionPolicy.Reduce(coordinates, 3);

        Assert.Equal(19.433d, reduced.Latitude);
        Assert.Equal(-99.133d, reduced.Longitude);
    }
}
