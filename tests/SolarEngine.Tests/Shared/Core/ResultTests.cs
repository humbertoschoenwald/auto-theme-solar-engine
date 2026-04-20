using SolarEngine.Shared.Core;
using Xunit;

namespace SolarEngine.Tests.Shared.Core;

/// <summary>
/// Verifies result primitives preserve explicit success and failure contracts.
/// </summary>
[Trait("TestLane", "Light")]
public sealed class ResultTests
{
    /// <summary>
    /// Verifies success results expose values and no error payload.
    /// </summary>
    [Fact]
    public void SuccessResult_ExposesValueAndNoError()
    {
        Result<int> result = Result<int>.Success(42);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(42, result.Value);
        Assert.Equal(Error.None, result.Error);
    }

    /// <summary>
    /// Verifies failure results block value access and preserve the failure payload.
    /// </summary>
    [Fact]
    public void FailureResult_ThrowsOnValueAccess()
    {
        Error error = Error.Validation("config.invalid", "Configuration was rejected.");
        Result<int> result = Result<int>.Failure(error);

        UnexpectedStateException exception = Assert.Throws<UnexpectedStateException>(() => _ = result.Value);
        Assert.Equal(error, result.Error);
        Assert.Contains("Access the value only when the result is successful.", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies non-generic results reject impossible success and failure state combinations.
    /// </summary>
    [Fact]
    public void Failure_RejectsErrorNone()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() => Result<int>.Failure(Error.None));

        Assert.Equal("error", exception.ParamName);
    }

    /// <summary>
    /// Verifies non-generic result factories and operators preserve equality semantics.
    /// </summary>
    [Fact]
    public void NonGenericResult_FactoriesAndOperatorsPreserveContracts()
    {
        Error error = Error.Conflict("theme.conflict", "Theme application conflicted with another state.");
        Result success = Result.Success();
        Result failure = Result.Failure(error);
        Result fromError = Result.FromError(error);
        Result implicitFailure = error;

        Assert.True(success.IsSuccess);
        Assert.True(failure.IsFailure);
        Assert.Equal(error, failure.Error);
        Assert.Equal(failure, fromError);
        Assert.Equal(failure, implicitFailure);
        Assert.True(failure == fromError);
        Assert.True(failure != success);
        Assert.Equal(failure.GetHashCode(), fromError.GetHashCode());
        Assert.True(failure.Equals((object)fromError));
    }

    /// <summary>
    /// Verifies non-generic failures reject the sentinel no-error value.
    /// </summary>
    [Fact]
    public void NonGenericResult_RejectsErrorNone()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() => Result.Failure(Error.None));

        Assert.Equal("error", exception.ParamName);
    }

    /// <summary>
    /// Verifies generic factories, conversions, and equality keep success and failure states stable.
    /// </summary>
    [Fact]
    public void GenericResult_FactoriesAndConversionsPreserveState()
    {
        Error error = Error.NotFound("schedule.missing", "No schedule was available.");
        Result<int> fromValue = Result<int>.FromValue(9);
        Result<int> implicitValue = 9;
        Result<int> fromError = Result<int>.FromError(error);
        Result<int> implicitError = error;

        Assert.Equal(fromValue, implicitValue);
        Assert.Equal(fromError, implicitError);
        Assert.True(fromValue == implicitValue);
        Assert.True(fromError == implicitError);
        Assert.True(fromValue != fromError);
        Assert.Equal(fromValue.GetHashCode(), implicitValue.GetHashCode());
        Assert.Equal(fromError.GetHashCode(), implicitError.GetHashCode());
        Assert.True(fromValue.Equals((object)implicitValue));
        Assert.True(fromError.Equals((object)implicitError));
    }

    /// <summary>
    /// Verifies Match dispatches to the branch implied by the result state.
    /// </summary>
    [Fact]
    public void Match_InvokesTheCorrectBranch()
    {
        Error error = Error.NotFound("locations.missing", "Coordinates are missing.");
        Result<int> failure = Result<int>.Failure(error);
        Result<int> success = Result<int>.Success(7);

        string failureText = failure.Match(
            static value => $"value:{value}",
            static currentError => currentError.Code);
        string successText = success.Match(
            static value => $"value:{value}",
            static currentError => currentError.Code);

        Assert.Equal("locations.missing", failureText);
        Assert.Equal("value:7", successText);
    }

    /// <summary>
    /// Verifies Match rejects null delegates instead of silently choosing a branch.
    /// </summary>
    [Fact]
    public void Match_RejectsNullDelegates()
    {
        Result<int> result = Result<int>.Success(1);

        _ = Assert.Throws<ArgumentNullException>(() => result.Match<string>(null!, static error => error.Code));
        _ = Assert.Throws<ArgumentNullException>(() => result.Match(static value => value.ToString(), null!));
    }

    /// <summary>
    /// Verifies error values trim their inputs and render empty for Error.None.
    /// </summary>
    [Fact]
    public void Error_TrimsFieldsAndFormatsExpectedText()
    {
        Error error = new(" config.invalid ", " Configuration was rejected. ");

        Assert.Equal("config.invalid", error.Code);
        Assert.Equal("Configuration was rejected.", error.Description);
        Assert.True(error.IsSome);
        Assert.Equal("Configuration was rejected.", error.ToString());
        Assert.Equal(string.Empty, Error.None.ToString());
    }

    /// <summary>
    /// Verifies named error factories preserve the same trimmed payload contract.
    /// </summary>
    [Fact]
    public void ErrorFactories_PreserveTrimmedPayloads()
    {
        Error validation = Error.Validation(" validation ", " Validation error. ");
        Error failure = Error.Failure(" failure ", " Failure error. ");
        Error notFound = Error.NotFound(" missing ", " Not found. ");
        Error conflict = Error.Conflict(" conflict ", " Conflict. ");
        Error unexpected = Error.Unexpected(" unexpected ", " Unexpected. ");

        Assert.Equal("validation", validation.Code);
        Assert.Equal("Validation error.", validation.Description);
        Assert.Equal("failure", failure.Code);
        Assert.Equal("missing", notFound.Code);
        Assert.Equal("conflict", conflict.Code);
        Assert.Equal("unexpected", unexpected.Code);
    }

    /// <summary>
    /// Verifies the repository exception keeps both message and inner exception.
    /// </summary>
    [Fact]
    public void UnexpectedStateException_PreservesMessageAndInnerException()
    {
        ArgumentException innerException = new("Inner.");
        UnexpectedStateException exception = new("Outer.", innerException);

        Assert.Equal("Outer.", exception.Message);
        Assert.Same(innerException, exception.InnerException);
    }
}
