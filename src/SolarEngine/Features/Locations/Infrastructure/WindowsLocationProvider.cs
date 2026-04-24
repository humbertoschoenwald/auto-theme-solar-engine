// Copyright (c) 2026 Humberto Schoenwald.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using System.Text;
using SolarEngine.Features.Locations.Domain;
using SolarEngine.Infrastructure.Logging;
using SolarEngine.Shared.Core;
using Windows.Devices.Geolocation;

namespace SolarEngine.Features.Locations.Infrastructure;

internal sealed class WindowsLocationProvider(StructuredLogPublisher logPublisher) : ISystemLocationProvider
{
    private const string AccessDeniedCode = "locations.provider.access_denied";
    private const string AccessDeniedDescription = "Preserve user-controlled privacy boundaries when native location access is unavailable.";
    private const int AccessStateWindowsMajorVersion = 10;
    private const int AccessStateWindowsMinorVersion = 0;
    private const int AccessStateWindowsBuildVersion = 19041;
    private const int DesiredAccuracyInMeters = 250;
    private const string LookupFailedCode = "locations.provider.lookup_failed";
    private const string LookupFailedDescription = "Preserve tray responsiveness when the operating system cannot resolve coordinates.";
    private const int MaximumAgeMinutes = 5;
    private const int PositionTimeoutSeconds = 10;
    private static readonly CompositeFormat s_invalidCoordinatesLogFormat =
        CompositeFormat.Parse("Windows location returned invalid coordinates: {0}");
    private static readonly CompositeFormat s_lookupFailedLogFormat =
        CompositeFormat.Parse("Windows location lookup failed: {0}");
    private static readonly CompositeFormat s_requestAccessFailedLogFormat =
        CompositeFormat.Parse("Windows location access request failed: {0}");

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
                    AccessDeniedCode,
                    AccessDeniedDescription));
        }

        Geolocator geolocator = new()
        {
            DesiredAccuracyInMeters = DesiredAccuracyInMeters
        };

        try
        {
            Geoposition position = await geolocator
                .GetGeopositionAsync(
                    TimeSpan.FromSeconds(PositionTimeoutSeconds),
                    TimeSpan.FromMinutes(MaximumAgeMinutes))
                .AsTask(cancellationToken)
                .ConfigureAwait(false);

            BasicGeoposition basicPosition = position.Coordinate.Point.Position;
            Result<GeoCoordinates> coordinates = GeoCoordinates.Create(basicPosition.Latitude, basicPosition.Longitude);

            if (coordinates.IsFailure)
            {
                logPublisher.Write(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        s_invalidCoordinatesLogFormat,
                        coordinates.Error.Description));
            }

            return coordinates;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logPublisher.Write(
                string.Format(
                    CultureInfo.InvariantCulture,
                    s_lookupFailedLogFormat,
                    exception.Message));

            return Result<GeoCoordinates>.Failure(
                new Error(
                    LookupFailedCode,
                    LookupFailedDescription));
        }
    }

    private async ValueTask<SystemLocationAccessState> RequestAccessStateAsync(CancellationToken cancellationToken)
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(
                AccessStateWindowsMajorVersion,
                AccessStateWindowsMinorVersion,
                AccessStateWindowsBuildVersion))
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
            logPublisher.Write(
                string.Format(
                    CultureInfo.InvariantCulture,
                    s_requestAccessFailedLogFormat,
                    exception.Message));
            return SystemLocationAccessState.Unavailable;
        }
    }
}
