namespace SolarEngine.Shared.Core;

internal readonly record struct Error
{
    public static readonly Error None = new(string.Empty, string.Empty);

    public string Code { get; } = string.Empty;

    public string Description { get; } = string.Empty;

    public bool IsNone => string.IsNullOrEmpty(Code);

    public bool IsSome => !IsNone;

    public Error(string code, string description)
    {
        Code = code?.Trim() ?? string.Empty;
        Description = description?.Trim() ?? string.Empty;
    }

    public static Error Validation(string code, string description)
    {
        return new(code, description);
    }

    public static Error Failure(string code, string description)
    {
        return new(code, description);
    }

    public static Error NotFound(string code, string description)
    {
        return new(code, description);
    }

    public static Error Conflict(string code, string description)
    {
        return new(code, description);
    }

    public static Error Unexpected(string code, string description)
    {
        return new(code, description);
    }

    public override string ToString()
    {
        return IsNone ? string.Empty : Description;
    }
}
