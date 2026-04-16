namespace SolarEngine.Shared.Core;

internal readonly struct Result : IEquatable<Result>
{
    private readonly Error? _error;

    private Result(bool isSuccess, Error error)
    {
        if ((isSuccess && error != Error.None) || (!isSuccess && error == Error.None))
        {
            throw new ArgumentException("Invalid error state for the given success status.", nameof(error));
        }

        IsSuccess = isSuccess;
        _error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public Error Error => _error ?? Error.None;

    public static Result Success()
    {
        return new(true, Error.None);
    }

    public static Result<T> Success<T>(T value)
    {
        return new(value);
    }

    public static Result Failure(Error error)
    {
        return new(false, error);
    }

    public static Result<T> Failure<T>(Error error)
    {
        return new(error);
    }

    public static Result FromError(Error error)
    {
        return Failure(error);
    }

    public static implicit operator Result(Error error)
    {
        return Failure(error);
    }

    public bool Equals(Result other)
    {
        return IsSuccess == other.IsSuccess
            && EqualityComparer<Error>.Default.Equals(Error, other.Error);
    }

    public override bool Equals(object? obj)
    {
        return obj is Result other && Equals(other);
    }

    public override int GetHashCode()
    {
        HashCode hash = new();
        hash.Add(IsSuccess);
        hash.Add(Error, EqualityComparer<Error>.Default);
        return hash.ToHashCode();
    }

    public static bool operator ==(Result left, Result right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Result left, Result right)
    {
        return !left.Equals(right);
    }
}

internal readonly struct Result<T> : IEquatable<Result<T>>
{
    private readonly T? _value;
    private readonly Error? _error;

    internal Result(T value)
    {
        _value = value;
        _error = Error.None;
        IsSuccess = true;
    }

    internal Result(Error error)
    {
        if (error == Error.None)
        {
            throw new ArgumentException("Invalid error state for a failure result.", nameof(error));
        }

        _value = default;
        _error = error;
        IsSuccess = false;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public Error Error => _error ?? Error.None;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Access the value only when the result is successful.");

    public static Result<T> Success(T value)
    {
        return new(value);
    }

    public static Result<T> Failure(Error error)
    {
        return new(error);
    }

    public static Result<T> FromValue(T value)
    {
        return new(value);
    }

    public static Result<T> FromError(Error error)
    {
        return new(error);
    }

    public static implicit operator Result<T>(T value)
    {
        return new(value);
    }

    public static implicit operator Result<T>(Error error)
    {
        return new(error);
    }

    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<Error, TResult> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);

        return IsSuccess ? onSuccess(_value!) : onFailure(Error);
    }

    public bool Equals(Result<T> other)
    {
        return IsSuccess == other.IsSuccess && (IsSuccess
            ? EqualityComparer<T>.Default.Equals(_value, other._value)
            : EqualityComparer<Error>.Default.Equals(Error, other.Error));
    }

    public override bool Equals(object? obj)
    {
        return obj is Result<T> other && Equals(other);
    }

    public override int GetHashCode()
    {
        HashCode hash = new();
        hash.Add(IsSuccess);

        if (IsSuccess)
        {
            hash.Add(_value);
        }
        else
        {
            hash.Add(Error, EqualityComparer<Error>.Default);
        }

        return hash.ToHashCode();
    }

    public static bool operator ==(Result<T> left, Result<T> right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Result<T> left, Result<T> right)
    {
        return !left.Equals(right);
    }
}
