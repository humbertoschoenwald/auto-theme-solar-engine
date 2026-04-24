// Copyright (c) 2026 Humberto Schoenwald.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SolarEngine.Features.Locations.Domain;

namespace SolarEngine.Features.SolarCalculations;

internal readonly record struct GetSolarScheduleQuery(
    DateOnly Date,
    GeoCoordinates Coordinates,
    TimeZoneInfo TimeZone);
