using System.Text.RegularExpressions;
using StellarImperiums.Domain.Common;
using StellarImperiums.Domain.Utilisateurs.Exceptions;

namespace StellarImperiums.Domain.Utilisateurs;

/// <summary>
/// Represents a normalized (lowercase, trimmed) email address. The format check mirrors the SQL
/// regex constraint applied on the underlying column.
/// </summary>
public sealed partial class Email : ValueObject
{
    /// <summary>
    /// Maximum allowed length, aligned with the SQL VARCHAR(100) column.
    /// </summary>
    public const int LongueurMax = 100;

    [GeneratedRegex(
        @"^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        matchTimeoutMilliseconds: 200)]
    private static partial Regex FormatRegex();

    /// <summary>
    /// Gets the normalized (lowercase, trimmed) email value.
    /// </summary>
    public string Valeur { get; }

    private Email(string valeur)
    {
        Valeur = valeur;
    }

    /// <summary>
    /// Creates a new <see cref="Email"/> after normalizing and validating the input.
    /// </summary>
    /// <param name="valeur">The candidate email string.</param>
    /// <returns>A validated <see cref="Email"/> instance.</returns>
    /// <exception cref="EmailInvalideException">Thrown when the value is empty, too long or malformed.</exception>
    public static Email Creer(string valeur)
    {
        if (string.IsNullOrWhiteSpace(valeur))
        {
            throw new EmailInvalideException("Email cannot be empty.");
        }

        var normalized = valeur.Trim().ToLowerInvariant();

        if (normalized.Length > LongueurMax)
        {
            throw new EmailInvalideException(
                $"Email must be at most {LongueurMax} characters (got {normalized.Length}).");
        }

        if (!FormatRegex().IsMatch(normalized))
        {
            throw new EmailInvalideException($"Email '{normalized}' is not a valid email address.");
        }

        return new Email(normalized);
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Valeur;
    }

    /// <inheritdoc />
    public override string ToString() => Valeur;
}
