namespace AssetTagBD.Application.Common.Models;

/// <summary>Allows pipeline behaviours to inspect success without knowing T.</summary>
public interface IResultMarker
{
    bool IsSuccess { get; }
}

/// <summary>Represents a business/validation error rather than an exception.</summary>
public sealed record Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);
}

/// <summary>
/// A result type that lets handlers return expected failures without throwing.
/// Unexpected exceptions are still handled by the global exception middleware.
/// </summary>
public sealed class Result<T> : IResultMarker
{
    public bool IsSuccess { get; }

    public T? Value { get; }

    public Error Error { get; }

    public bool IsFailure => !IsSuccess;

    private Result(bool isSuccess, T? value, Error error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    public static Result<T> Success(T value) => new(true, value, Error.None);

    public static Result<T> Failure(Error error) => new(false, default, error);

    public static Result<T> Failure(string code, string message) => Failure(new Error(code, message));
}

public static class Result
{
    public static Result<T> Success<T>(T value) => Result<T>.Success(value);

    public static Result<T> Failure<T>(Error error) => Result<T>.Failure(error);

    public static Result<T> Failure<T>(string code, string message) => Result<T>.Failure(code, message);
}

/// <summary>Shared error catalogue so status-code mapping stays consistent in the API.</summary>
public static class AppErrors
{
    public static Error NotFound(string entity, object key) =>
        new("NotFound", $"{entity} with key '{key}' was not found.");

    public static Error Conflict(string message) => new("Conflict", message);

    public static Error Unauthorized(string message) => new("Unauthorized", message);

    public static Error BadRequest(string message) => new("BadRequest", message);
}
