using StellarImperiums.Domain.Common;
using StellarImperiums.Domain.Users.Exceptions;

namespace StellarImperiums.Domain.Users;

/// <summary>
/// Represents a user's display name. Immutable value object whose value is trimmed, case-sensitive,
/// and constrained between <see cref="MinLength"/> and <see cref="MaxLength"/> characters.
/// </summary>
public sealed class Username : ValueObject
{
    /// <summary>
    /// Minimum allowed length for a username, aligned with the SQL CHECK constraint.
    /// </summary>
    public const int MinLength = 3;

    /// <summary>
    /// Maximum allowed length for a username, aligned with the SQL VARCHAR(50) column.
    /// </summary>
    public const int MaxLength = 50;

    /// <summary>
    /// Gets the underlying string value.
    /// </summary>
    public string Value { get; }

    private Username(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a new <see cref="Username"/> after trimming and validating the input.
    /// </summary>
    /// <param name="value">The candidate username string.</param>
    /// <returns>A validated <see cref="Username"/> instance.</returns>
    /// <exception cref="InvalidUsernameException">Thrown when the value is empty or out of length bounds.</exception>
    public static Username Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidUsernameException("Username cannot be empty.");
        }

        var trimmed = value.Trim();

        if (trimmed.Length < MinLength)
        {
            throw new InvalidUsernameException(
                $"Username must be at least {MinLength} characters (got {trimmed.Length}).");
        }

        if (trimmed.Length > MaxLength)
        {
            throw new InvalidUsernameException(
                $"Username must be at most {MaxLength} characters (got {trimmed.Length}).");
        }

        return new Username(trimmed);
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    /// <inheritdoc />
    public override string ToString() => Value;
}
