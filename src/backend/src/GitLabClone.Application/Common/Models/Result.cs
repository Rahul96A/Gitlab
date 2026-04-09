namespace GitLabClone.Application.Common.Models;

/// <summary>
/// Operation result wrapper. Avoids throwing exceptions for expected failure paths
/// (validation errors, not-found, forbidden). Exceptions are reserved for truly
/// unexpected failures.
/// </summary>
public sealed class Result<T>
{
    public T? Value { get; }
    public bool IsSuccess { get; }
    public string? Error { get; }
    public IReadOnlyDictionary<string, string[]>? ValidationErrors { get; }

    private Result(T value) { Value = value; IsSuccess = true; }
    private Result(string error) { Error = error; IsSuccess = false; }
    private Result(IReadOnlyDictionary<string, string[]> validationErrors)
    {
        ValidationErrors = validationErrors;
        IsSuccess = false;
        Error = "Validation failed.";
    }

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(string error) => new(error);
    public static Result<T> ValidationFailure(IReadOnlyDictionary<string, string[]> errors) => new(errors);
}
