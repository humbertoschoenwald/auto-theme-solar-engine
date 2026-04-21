namespace SolarEngine.Features.Updates.Domain;

internal readonly record struct CalVersion(int Year, int Month, int Patch) : IComparable<CalVersion>
{
    private const string TagPrefix = "v";
    private const char SegmentSeparator = '.';
    private const int PrefixLength = 1;
    private const int SegmentCount = 3;
    private const int YearIndex = 0;
    private const int MonthIndex = 1;
    private const int PatchIndex = 2;
    private const int EqualComparison = 0;

    public static bool TryParse(string? value, out CalVersion version)
    {
        version = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        ReadOnlySpan<char> span = value.AsSpan().Trim();
        if (span.StartsWith(TagPrefix, StringComparison.OrdinalIgnoreCase))
        {
            span = span[PrefixLength..];
        }

        string[] parts = span.ToString().Split(SegmentSeparator, StringSplitOptions.TrimEntries);
        if (parts.Length != SegmentCount
            || !int.TryParse(parts[YearIndex], out int year)
            || !int.TryParse(parts[MonthIndex], out int month)
            || !int.TryParse(parts[PatchIndex], out int patch))
        {
            return false;
        }

        version = new CalVersion(year, month, patch);
        return true;
    }

    public int CompareTo(CalVersion other)
    {
        int yearComparison = Year.CompareTo(other.Year);
        if (yearComparison != EqualComparison)
        {
            return yearComparison;
        }

        int monthComparison = Month.CompareTo(other.Month);
        return monthComparison != EqualComparison ? monthComparison : Patch.CompareTo(other.Patch);
    }

    public string ToTag()
    {
        return $"{TagPrefix}{Year:00}.{Month:00}.{Patch:00}";
    }

    public override string ToString()
    {
        return $"{Year:00}.{Month:00}.{Patch:00}";
    }
}
