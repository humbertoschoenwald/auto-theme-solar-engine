// Copyright (c) 2026 Humberto Schoenwald.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace SolarEngine.Features.SolarCalculations.Domain;

internal sealed record SolarSchedule(
    DateOnly Date,
    DateTime? SunriseLocal,
    DateTime? SunsetLocal,
    SolarDaylightCondition DaylightCondition);
