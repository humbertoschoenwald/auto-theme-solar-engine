// Copyright (c) 2026 Humberto Schoenwald.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace SolarEngine.Features.SolarCalculations.Domain;

internal static class AngleMathExtensions
{
    extension(double value)
    {
        public double ToRadians()
        {
            return value * (Math.PI / 180d);
        }

        public double ToDegrees()
        {
            return value * (180d / Math.PI);
        }

        public double NormalizeDegrees()
        {
            return ((value % 360d) + 360d) % 360d;
        }

        public double NormalizeHours()
        {
            return ((value % 24d) + 24d) % 24d;
        }
    }
}
