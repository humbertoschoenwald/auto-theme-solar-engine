// Copyright (c) 2026 Humberto Schoenwald.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SolarEngine.Features.Locations.Domain;
using SolarEngine.Features.SolarCalculations;
using SolarEngine.Features.SolarCalculations.Domain;
using SolarEngine.Shared.Core;
using Xunit;

namespace SolarEngine.Tests.Features.SolarCalculations;

/// <summary>
/// Verifies solar-schedule query dispatch preserves deterministic domain behavior.
/// </summary>
[Trait("TestLane", "Light")]
public sealed class GetSolarScheduleQueryHandlerTests
{
    private static readonly TimeZoneInfo s_baselineTimeZone = TimeZoneInfo.CreateCustomTimeZone(
        id: "TestMexicoCity",
        baseUtcOffset: TimeSpan.FromHours(-6),
        displayName: "Test Mexico City",
        standardDisplayName: "Test Mexico City");

    /// <summary>
    /// Verifies valid coordinates produce a standard schedule through the application boundary.
    /// </summary>
    [Fact]
    public async Task HandleAsyncReturnsCalculatedSchedule()
    {
        Result<GeoCoordinates> coordinatesResult = GeoCoordinates.Create(19.4326d, -99.1332d);
        Assert.True(coordinatesResult.IsSuccess);

        Result<SolarSchedule> result = await GetSolarScheduleQueryHandler.HandleAsync(
            new GetSolarScheduleQuery(
                new DateOnly(2026, 3, 29),
                coordinatesResult.Value,
                s_baselineTimeZone));

        Assert.True(result.IsSuccess);
        Assert.Equal(SolarDaylightCondition.Standard, result.Value.DaylightCondition);
        _ = Assert.NotNull(result.Value.SunriseLocal);
        _ = Assert.NotNull(result.Value.SunsetLocal);
    }

    /// <summary>
    /// Verifies cancellation is honored before any calculation work begins.
    /// </summary>
    [Fact]
    public async Task HandleAsyncThrowsWhenCancellationWasAlreadyRequested()
    {
        Result<GeoCoordinates> coordinatesResult = GeoCoordinates.Create(19.4326d, -99.1332d);
        Assert.True(coordinatesResult.IsSuccess);

        using CancellationTokenSource cancellationTokenSource = new();
        cancellationTokenSource.Cancel();

        _ = await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await GetSolarScheduleQueryHandler.HandleAsync(
                new GetSolarScheduleQuery(
                    new DateOnly(2026, 3, 29),
                    coordinatesResult.Value,
                    s_baselineTimeZone),
                cancellationTokenSource.Token));
    }
}
