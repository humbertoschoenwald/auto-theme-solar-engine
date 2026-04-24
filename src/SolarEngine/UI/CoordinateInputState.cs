// Copyright (c) 2026 Humberto Schoenwald.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using SolarEngine.Features.Locations.Domain;
using SolarEngine.Features.SystemHost.Domain;
using SolarEngine.Shared.Core;

namespace SolarEngine.UI;

internal sealed class CoordinateInputState
{
    private const string MissingLocationSeedCode = "MissingLocationSeed";
    private const string MissingLocationSeedDescription = "Detect coordinates or enter manual coordinates before enabling Windows location.";
    private const string InvalidLatitudeCode = "InvalidLatitude";
    private const string InvalidLatitudeDescription = "Provide a valid decimal latitude.";
    private const string InvalidLongitudeCode = "InvalidLongitude";
    private const string InvalidLongitudeDescription = "Provide a valid decimal longitude.";

    private GeoCoordinates? _seed;

    public void Load(AppConfig configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        _seed = configuration.IsConfigured
            ? new GeoCoordinates
            {
                Latitude = configuration.Latitude,
                Longitude = configuration.Longitude
            }
            : null;
    }

    public void Remember(GeoCoordinates coordinates)
    {
        ArgumentNullException.ThrowIfNull(coordinates);
        _seed = coordinates;
    }

    public void RememberIfValid(string latitudeText, string longitudeText)
    {
        Result<GeoCoordinates> coordinatesResult = ParseCoordinates(latitudeText, longitudeText);
        if (coordinatesResult.IsSuccess)
        {
            _seed = coordinatesResult.Value;
        }
    }

    public bool TryFormatSeed(
        int locationPrecisionDecimals,
        bool inputsVisible,
        out string latitudeText,
        out string longitudeText)
    {
        if (!inputsVisible || _seed is null)
        {
            latitudeText = string.Empty;
            longitudeText = string.Empty;
            return false;
        }

        latitudeText = CoordinatePrecisionPolicy.Format(
            _seed.Latitude,
            locationPrecisionDecimals);
        longitudeText = CoordinatePrecisionPolicy.Format(
            _seed.Longitude,
            locationPrecisionDecimals);
        return true;
    }

    public Result<GeoCoordinates> ResolveManualCoordinates(
        string latitudeText,
        string longitudeText,
        bool inputsVisible)
    {
        Result<GeoCoordinates> coordinatesResult = ParseCoordinates(latitudeText, longitudeText);
        if (coordinatesResult.IsSuccess)
        {
            _seed = coordinatesResult.Value;
            return coordinatesResult;
        }

        return !inputsVisible && _seed is not null
            ? Result<GeoCoordinates>.Success(_seed)
            : coordinatesResult;
    }

    public Result<GeoCoordinates> ResolveWindowsLocationCoordinates(
        string latitudeText,
        string longitudeText,
        bool inputsVisible,
        AppConfig currentConfiguration)
    {
        ArgumentNullException.ThrowIfNull(currentConfiguration);

        Result<GeoCoordinates> coordinatesResult = ParseCoordinates(latitudeText, longitudeText);
        if (coordinatesResult.IsSuccess)
        {
            _seed = coordinatesResult.Value;
            return coordinatesResult;
        }

        bool hasVisibleInput = !string.IsNullOrWhiteSpace(latitudeText) || !string.IsNullOrWhiteSpace(longitudeText);
        if (inputsVisible && hasVisibleInput)
        {
            return coordinatesResult;
        }

        if (!inputsVisible && _seed is not null)
        {
            return Result<GeoCoordinates>.Success(_seed);
        }

        if (currentConfiguration.IsConfigured)
        {
            Result<GeoCoordinates> currentCoordinates = GeoCoordinates.Create(
                currentConfiguration.Latitude,
                currentConfiguration.Longitude);
            if (currentCoordinates.IsSuccess)
            {
                _seed = currentCoordinates.Value;
            }

            return currentCoordinates;
        }

        return hasVisibleInput
            ? coordinatesResult
            : Result<GeoCoordinates>.Failure(
                Error.Validation(
                    MissingLocationSeedCode,
                    MissingLocationSeedDescription));
    }

    internal static Result<GeoCoordinates> ParseCoordinates(string latitudeText, string longitudeText)
    {
        return !double.TryParse(
                latitudeText,
                NumberStyles.Float | NumberStyles.AllowLeadingSign,
                CultureInfo.InvariantCulture,
                out double latitude)
            ? Result<GeoCoordinates>.Failure(
                Error.Validation(InvalidLatitudeCode, InvalidLatitudeDescription))
            : !double.TryParse(
                longitudeText,
                NumberStyles.Float | NumberStyles.AllowLeadingSign,
                CultureInfo.InvariantCulture,
                out double longitude)
            ? Result<GeoCoordinates>.Failure(
                Error.Validation(InvalidLongitudeCode, InvalidLongitudeDescription))
            : GeoCoordinates.Create(latitude, longitude);
    }
}
