namespace Tickify.Core.Common;

public enum ErrorType { None, Validation, NotFound, Conflict, Forbidden, Unauthorized }

public class ServiceResult
{
    public ErrorType ErrorType { get; protected init; } = ErrorType.None;
    public string? Error { get; protected init; }
    public bool IsSuccess => ErrorType == ErrorType.None;

    public static ServiceResult Success() => new();
    public static ServiceResult Fail(ErrorType type, string message) => new() { ErrorType = type, Error = message };
}

public class ServiceResult<T> : ServiceResult
{
    public T? Value { get; private init; }

    public static ServiceResult<T> Success(T value) => new() { Value = value };
    public new static ServiceResult<T> Fail(ErrorType type, string message) => new() { ErrorType = type, Error = message };
}
