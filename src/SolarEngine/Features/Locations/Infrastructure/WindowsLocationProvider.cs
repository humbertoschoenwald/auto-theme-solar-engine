using SolarEngine.Features.Locations.Domain;
using SolarEngine.Infrastructure.Logging;
using SolarEngine.Shared.Core;
using Windows.Devices.Geolocation;

namespace SolarEngine.Features.Locations.Infrastructure;

internal sealed class WindowsLocationProvider(StructuredLogPublisher logPublisher) : ISystemLocationProvider
{
    public async ValueTask<SystemLocationAccessState> GetAccessStateAsync(CancellationToken cancellationToken = default)
    {
        return await RequestAccessStateAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<Result<GeoCoordinates>> GetLocationAsync(CancellationToken cancellationToken = default)
    {
        SystemLocationAccessState accessState = await RequestAccessStateAsync(cancellationToken).ConfigureAwait(false);
        if (accessState != SystemLocationAccessState.Allowed)
        {
            return Result<GeoCoordinates>.Failure(
                new Error(
                    "locations.provider.access_denied",
                    "Preserve user-controlled privacy boundaries when native location access is unavailable."));
        }

        Geolocator geolocator = new()
        {
            DesiredAccuracyInMeters = 250
        };

        try
        {
            Geoposition position = await geolocator
                .GetGeopositionAsync(TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(5))
                .AsTask(cancellationToken)
                .ConfigureAwait(false);

            BasicGeoposition basicPosition = position.Coordinate.Point.Position;
            Result<GeoCoordinates> coordinates = GeoCoordinates.Create(basicPosition.Latitude, basicPosition.Longitude);

            if (coordinates.IsFailure)
            {
                logPublisher.Write($"Windows location returned invalid coordinates: {coordinates.Error.Description}");
            }

            return coordinates;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logPublisher.Write($"Windows location lookup failed: {exception.Message}");

            return Result<GeoCoordinates>.Failure(
                new Error(
                    "locations.provider.lookup_failed",
                    "Preserve tray responsiveness when the operating system cannot resolve coordinates."));
        }
    }

    private async ValueTask<SystemLocationAccessState> RequestAccessStateAsync(CancellationToken cancellationToken)
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041))
        {
            return SystemLocationAccessState.Unavailable;
        }

        try
        {
            GeolocationAccessStatus accessStatus = await Geolocator
                .RequestAccessAsync()
                .AsTask(cancellationToken)
                .ConfigureAwait(false);

            return accessStatus == GeolocationAccessStatus.Allowed
                ? SystemLocationAccessState.Allowed
                : SystemLocationAccessState.Denied;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logPublisher.Write($"Windows location access request failed: {exception.Message}");
            return SystemLocationAccessState.Unavailable;
        }
    }
}
