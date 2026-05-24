using StellarImperiums.Domain.Common;
using StellarImperiums.Domain.Utilisateurs.Exceptions;

namespace StellarImperiums.Domain.Utilisateurs;

/// <summary>
/// Represents a password hash stored in PHC argon2id format. The plaintext password is never
/// part of the domain; the <see cref="ToString"/> implementation deliberately hides the value
/// to avoid accidental leaks via logging.
/// </summary>
public sealed class MotDePasseHash : ValueObject
{
    private const string PrefixePhcArgon2id = "$argon2id$";

    /// <summary>
    /// Maximum allowed length, aligned with the SQL VARCHAR(255) column.
    /// </summary>
    public const int LongueurMax = 255;

    /// <summary>
    /// Gets the raw PHC argon2id hash string.
    /// </summary>
    public string Valeur { get; }

    private MotDePasseHash(string valeur)
    {
        Valeur = valeur;
    }

    /// <summary>
    /// Creates a new <see cref="MotDePasseHash"/> from a precomputed PHC argon2id hash.
    /// </summary>
    /// <param name="hashPhc">The hash string in PHC argon2id format (starting with <c>$argon2id$</c>).</param>
    /// <returns>A validated <see cref="MotDePasseHash"/> instance.</returns>
    /// <exception cref="MotDePasseHashInvalideException">
    /// Thrown when the value is empty, exceeds the maximum length or is not prefixed with the expected algorithm identifier.
    /// </exception>
    public static MotDePasseHash Creer(string hashPhc)
    {
        if (string.IsNullOrWhiteSpace(hashPhc))
        {
            throw new MotDePasseHashInvalideException("Password hash cannot be empty.");
        }

        if (hashPhc.Length > LongueurMax)
        {
            throw new MotDePasseHashInvalideException(
                $"Password hash must be at most {LongueurMax} characters (got {hashPhc.Length}).");
        }

        if (!hashPhc.StartsWith(PrefixePhcArgon2id, StringComparison.Ordinal))
        {
            throw new MotDePasseHashInvalideException(
                "Password hash must be in PHC argon2id format (starting with '$argon2id$').");
        }

        return new MotDePasseHash(hashPhc);
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Valeur;
    }

    /// <summary>
    /// Returns a redacted representation to prevent accidental disclosure in logs or exceptions.
    /// </summary>
    public override string ToString() => "***";
}
