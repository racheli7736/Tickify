namespace Tickify.Core.Exceptions;

/// <summary>A unique constraint was violated (e.g. two requests created the same e-mail or category name at once).</summary>
public class DuplicateKeyException : Exception
{
    public DuplicateKeyException(string message, Exception? innerException = null)
        : base(message, innerException) { }
}
