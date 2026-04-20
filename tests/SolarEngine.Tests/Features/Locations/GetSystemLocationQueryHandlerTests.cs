using SolarEngine.Features.Locations;
using SolarEngine.Features.Locations.Domain;
using SolarEngine.Shared.Core;
using Xunit;

namespace SolarEngine.Tests.Features.Locations;

/// <summary>
/// Verifies system-location query dispatch keeps the provider contract intact.
/// </summary>
[Trait("TestLane", "Light")]
public sealed class GetSystemLocationQueryHandlerTests
{
    /// <summary>
    /// Verifies the handler returns the provider result without rewriting it.
    /// </summary>
    [Fact]
    public async Task HandleAsync_ReturnsProviderCoordinates()
    {
        FakeSystemLocationProvider provider = new(
            SystemLocationAccessState.Allowed,
            Result<GeoCoordinates>.Success(new GeoCoordinates
            {
                Latitude = 19.4326d,
                Longitude = -99.1332d
            }));
        GetSystemLocationQueryHandler handler = new(provider);

        Result<GeoCoordinates> result = await handler.HandleAsync(new GetSystemLocationQuery());

        Assert.True(result.IsSuccess);
        Assert.Equal(19.4326d, result.Value.Latitude);
        Assert.Equal(-99.1332d, result.Value.Longitude);
    }

    /// <summary>
    /// Verifies cancellation flows into the provider instead of being swallowed.
    /// </summary>
    [Fact]
    public async Task HandleAsync_PropagatesCancellationToken()
    {
        FakeSystemLocationProvider provider = new(
            SystemLocationAccessState.Allowed,
            Result<GeoCoordinates>.Failure(new Error("locations.cancelled", "Cancellation should bubble to the caller.")));
        GetSystemLocationQueryHandler handler = new(provider);
        using CancellationTokenSource cancellationTokenSource = new();

        _ = await handler.HandleAsync(new GetSystemLocationQuery(), cancellationTokenSource.Token);

        Assert.Equal(cancellationTokenSource.Token, provider.LastLocationCancellationToken);
    }

    private sealed class FakeSystemLocationProvider(
        SystemLocationAccessState accessState,
        Result<GeoCoordinates> locationResult) : ISystemLocationProvider
    {
        public CancellationToken LastLocationCancellationToken { get; private set; }

        public ValueTask<SystemLocationAccessState> GetAccessStateAsync(CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(accessState);
        }

        public ValueTask<Result<GeoCoordinates>> GetLocationAsync(CancellationToken cancellationToken = default)
        {
            LastLocationCancellationToken = cancellationToken;
            return ValueTask.FromResult(locationResult);
        }
    }
}
