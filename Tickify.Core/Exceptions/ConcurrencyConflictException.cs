namespace Tickify.Core.Exceptions;

public class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException() : base("A concurrency conflict occurred while saving changes.") { }

    public ConcurrencyConflictException(string message, Exception? innerException = null)
        : base(message, innerException) { }
}
