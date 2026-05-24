namespace StellarImperiums.Domain.Common;

/// <summary>
/// Base class for exceptions raised when a domain invariant is violated.
/// </summary>
public abstract class DomainException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DomainException"/> class with the specified message.
    /// </summary>
    /// <param name="message">A description of the broken invariant.</param>
    protected DomainException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainException"/> class with the specified message and inner exception.
    /// </summary>
    /// <param name="message">A description of the broken invariant.</param>
    /// <param name="innerException">The exception that triggered this domain failure.</param>
    protected DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
