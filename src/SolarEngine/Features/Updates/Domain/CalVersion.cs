namespace SolarEngine.Features.Updates.Domain;

internal readonly record struct CalVersion(int Year, int Month, int Patch) : IComparable<CalVersion>
{
    public static bool TryParse(string? value, out CalVersion version)
    {
        version = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        ReadOnlySpan<char> span = value.AsSpan().Trim();
        if (span.StartsWith("v", StringComparison.OrdinalIgnoreCase))
        {
            span = span[1..];
        }

        string[] parts = span.ToString().Split('.', StringSplitOptions.TrimEntries);
        if (parts.Length != 3
            || !int.TryParse(parts[0], out int year)
            || !int.TryParse(parts[1], out int month)
            || !int.TryParse(parts[2], out int patch))
        {
            return false;
        }

        version = new CalVersion(year, month, patch);
        return true;
    }

    public int CompareTo(CalVersion other)
    {
        int yearComparison = Year.CompareTo(other.Year);
        if (yearComparison != 0)
        {
            return yearComparison;
        }

        int monthComparison = Month.CompareTo(other.Month);
        return monthComparison != 0 ? monthComparison : Patch.CompareTo(other.Patch);
    }

    public string ToTag()
    {
        return $"v{Year:00}.{Month:00}.{Patch:00}";
    }

    public override string ToString()
    {
        return $"{Year:00}.{Month:00}.{Patch:00}";
    }
}
