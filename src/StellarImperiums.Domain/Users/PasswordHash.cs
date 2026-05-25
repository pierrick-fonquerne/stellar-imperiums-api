using StellarImperiums.Domain.Common;
using StellarImperiums.Domain.Users.Exceptions;

namespace StellarImperiums.Domain.Users;

/// <summary>
/// Represents a password hash stored in PHC argon2id format. The plaintext password is never
/// part of the domain; the <see cref="ToString"/> implementation deliberately hides the value
/// to avoid accidental leaks via logging.
/// </summary>
public sealed class PasswordHash : ValueObject
{
    private const string PhcArgon2idPrefix = "$argon2id$";

    /// <summary>
    /// Maximum allowed length, aligned with the SQL VARCHAR(255) column.
    /// </summary>
    public const int MaxLength = 255;

    /// <summary>
    /// Gets the raw PHC argon2id hash string.
    /// </summary>
    public string Value { get; }

    private PasswordHash(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a new <see cref="PasswordHash"/> from a precomputed PHC argon2id hash.
    /// </summary>
    /// <param name="phcHash">The hash string in PHC argon2id format (starting with <c>$argon2id$</c>).</param>
    /// <returns>A validated <see cref="PasswordHash"/> instance.</returns>
    /// <exception cref="InvalidPasswordHashException">
    /// Thrown when the value is empty, exceeds the maximum length or is not prefixed with the expected algorithm identifier.
    /// </exception>
    public static PasswordHash Create(string phcHash)
    {
        if (string.IsNullOrWhiteSpace(phcHash))
        {
            throw new InvalidPasswordHashException("Password hash cannot be empty.");
        }

        if (phcHash.Length > MaxLength)
        {
            throw new InvalidPasswordHashException(
                $"Password hash must be at most {MaxLength} characters (got {phcHash.Length}).");
        }

        if (!phcHash.StartsWith(PhcArgon2idPrefix, StringComparison.Ordinal))
        {
            throw new InvalidPasswordHashException(
                "Password hash must be in PHC argon2id format (starting with '$argon2id$').");
        }

        return new PasswordHash(phcHash);
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    /// <summary>
    /// Returns a redacted representation to prevent accidental disclosure in logs or exceptions.
    /// </summary>
    public override string ToString() => "***";
}
