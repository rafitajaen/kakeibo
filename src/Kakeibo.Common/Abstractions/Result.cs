using System.Diagnostics.CodeAnalysis;

namespace Kakeibo.Common.Abstractions;

// Discriminated union for operation results
public sealed class Result<T>
{
    private readonly T? _value;
    private readonly Error? _error;

    private Result(T value)
    {
        _value = value;
        _error = null;
        IsSuccess = true;
    }

    private Result(Error error)
    {
        _value = default;
        _error = error;
        IsSuccess = false;
    }

    [MemberNotNullWhen(true, nameof(_value), nameof(Value))]
    [MemberNotNullWhen(false, nameof(_error), nameof(Error))]
    public bool IsSuccess { get; }

    [MemberNotNullWhen(false, nameof(_value), nameof(Value))]
    [MemberNotNullWhen(true, nameof(_error), nameof(Error))]
    public bool IsFailure => !IsSuccess;

    public T Value =>
        IsSuccess ? _value : throw new InvalidOperationException("Cannot access Value on a failed result.");

    public Error Error =>
        IsFailure ? _error : throw new InvalidOperationException("Cannot access Error on a successful result.");

    public static Result<T> Success(T value) => new(value);

    public static Result<T> Failure(Error error) => new(error);

    public static implicit operator Result<T>(T value) => Success(value);

    public static implicit operator Result<T>(Error error) => Failure(error);
}
