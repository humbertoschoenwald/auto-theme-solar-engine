// Copyright (c) 2026 Humberto Schoenwald.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace SolarEngine.Shared.Core;

internal sealed class Result : IEquatable<Result>
{
    private const string InvalidFailureErrorStateDescription = "Invalid error state for a failure result.";

    private readonly IResultVariant _variant;

    private Result(IResultVariant variant)
    {
        _variant = variant;
    }

    public bool IsSuccess => _variant.IsSuccess;

    public bool IsFailure => !IsSuccess;

    public Error Error => _variant.Error;

    public static Result Success()
    {
        return new Result(new SuccessVariant());
    }

    public static Result<T> Success<T>(T value)
    {
        return Result<T>.Success(value);
    }

    public static Result Failure(Error error)
    {
        return new Result(new FailureVariant(ValidateFailureError(error)));
    }

    public static Result<T> Failure<T>(Error error)
    {
        return Result<T>.Failure(error);
    }

    public static Result FromError(Error error)
    {
        return Failure(error);
    }

    public static implicit operator Result(Error error)
    {
        return Failure(error);
    }

    public bool Equals(Result? other)
    {
        return other is not null && EqualityComparer<IResultVariant>.Default.Equals(_variant, other._variant);
    }

    public override bool Equals(object? obj)
    {
        return obj is Result other && Equals(other);
    }

    public override int GetHashCode()
    {
        return _variant.GetHashCode();
    }

    public static bool operator ==(Result? left, Result? right)
    {
        return EqualityComparer<Result>.Default.Equals(left, right);
    }

    public static bool operator !=(Result? left, Result? right)
    {
        return !EqualityComparer<Result>.Default.Equals(left, right);
    }

    private static Error ValidateFailureError(Error error)
    {
        return error == Error.None
            ? throw new ArgumentException(InvalidFailureErrorStateDescription, nameof(error))
            : error;
    }

    private interface IResultVariant
    {
        public bool IsSuccess
        {
            get;
        }

        public Error Error
        {
            get;
        }
    }

    private sealed record SuccessVariant : IResultVariant
    {
        public bool IsSuccess => true;

        public Error Error => Error.None;
    }

    private sealed record FailureVariant(Error FailureError) : IResultVariant
    {
        public bool IsSuccess => false;

        public Error Error => FailureError;
    }
}

internal sealed class Result<T> : IEquatable<Result<T>>
{
    private const string InvalidFailureErrorStateDescription = "Invalid error state for a failure result.";
    private const string ValueAccessDescription = "Access the value only when the result is successful.";

    private readonly IResultVariant<T> _variant;

    private Result(IResultVariant<T> variant)
    {
        _variant = variant;
    }

    public bool IsSuccess => _variant.IsSuccess;

    public bool IsFailure => !IsSuccess;

    public Error Error => _variant.Error;

    public T Value => _variant.Value;

    public static Result<T> Success(T value)
    {
        return new Result<T>(new SuccessVariant(value));
    }

    public static Result<T> Failure(Error error)
    {
        return new Result<T>(new FailureVariant(ValidateFailureError(error)));
    }

    public static Result<T> FromValue(T value)
    {
        return Success(value);
    }

    public static Result<T> FromError(Error error)
    {
        return Failure(error);
    }

    public static implicit operator Result<T>(T value)
    {
        return Success(value);
    }

    public static implicit operator Result<T>(Error error)
    {
        return Failure(error);
    }

    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<Error, TResult> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);

        return IsSuccess ? onSuccess(Value) : onFailure(Error);
    }

    public bool Equals(Result<T>? other)
    {
        return other is not null && EqualityComparer<IResultVariant<T>>.Default.Equals(_variant, other._variant);
    }

    public override bool Equals(object? obj)
    {
        return obj is Result<T> other && Equals(other);
    }

    public override int GetHashCode()
    {
        return _variant.GetHashCode();
    }

    public static bool operator ==(Result<T>? left, Result<T>? right)
    {
        return EqualityComparer<Result<T>>.Default.Equals(left, right);
    }

    public static bool operator !=(Result<T>? left, Result<T>? right)
    {
        return !EqualityComparer<Result<T>>.Default.Equals(left, right);
    }

    private static Error ValidateFailureError(Error error)
    {
        return error == Error.None
            ? throw new ArgumentException(InvalidFailureErrorStateDescription, nameof(error))
            : error;
    }

    private interface IResultVariant<out TValue>
    {
        public bool IsSuccess
        {
            get;
        }

        public Error Error
        {
            get;
        }

        public TValue Value
        {
            get;
        }
    }

    private sealed record SuccessVariant(T SuccessValue) : IResultVariant<T>
    {
        public bool IsSuccess => true;

        public Error Error => Error.None;

        public T Value => SuccessValue;
    }

    private sealed record FailureVariant(Error FailureError) : IResultVariant<T>
    {
        public bool IsSuccess => false;

        public Error Error => FailureError;

        public T Value => throw new UnexpectedStateException(ValueAccessDescription);
    }
}
