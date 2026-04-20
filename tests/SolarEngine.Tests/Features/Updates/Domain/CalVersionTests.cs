using SolarEngine.Features.Updates.Domain;
using Xunit;

namespace SolarEngine.Tests.Features.Updates.Domain;

/// <summary>
/// Verifies CalVer parsing, formatting, and ordering across release tags.
/// </summary>
[Trait("TestLane", "Light")]
public sealed class CalVersionTests
{
    /// <summary>
    /// Verifies prefixed tags parse into structured version parts.
    /// </summary>
    [Fact]
    public void TryParse_ParsesTaggedCalVer()
    {
        bool wasParsed = CalVersion.TryParse(" v26.04.04 ", out CalVersion version);

        Assert.True(wasParsed);
        Assert.Equal(26, version.Year);
        Assert.Equal(4, version.Month);
        Assert.Equal(4, version.Patch);
    }

    /// <summary>
    /// Verifies malformed values are rejected instead of producing partial versions.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("26.04")]
    [InlineData("v26.04.x")]
    [InlineData("release-26.04.04")]
    public void TryParse_ReturnsFalse_ForMalformedValues(string value)
    {
        bool wasParsed = CalVersion.TryParse(value, out CalVersion version);

        Assert.False(wasParsed);
        Assert.Equal(default, version);
    }

    /// <summary>
    /// Verifies release ordering compares year, month, and patch in sequence.
    /// </summary>
    [Fact]
    public void CompareTo_OrdersByYearThenMonthThenPatch()
    {
        CalVersion older = new(26, 4, 3);
        CalVersion newer = new(26, 4, 4);
        CalVersion nextMonth = new(26, 5, 0);

        Assert.True(older.CompareTo(newer) < 0);
        Assert.True(nextMonth.CompareTo(newer) > 0);
    }

    /// <summary>
    /// Verifies string formatting keeps the repository tag shape stable.
    /// </summary>
    [Fact]
    public void Formatting_UsesRepositoryCalVerShape()
    {
        CalVersion version = new(26, 4, 4);

        Assert.Equal("26.04.04", version.ToString());
        Assert.Equal("v26.04.04", version.ToTag());
    }
}
