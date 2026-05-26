using System.Text.RegularExpressions;
using StellarImperiums.Domain.Common;
using StellarImperiums.Domain.Users.Exceptions;

namespace StellarImperiums.Domain.Users;

/// <summary>
/// Represents a normalized (lowercase, trimmed) email address. The format check mirrors the SQL
/// regex constraint applied on the underlying column.
/// </summary>
public sealed partial class Email : ValueObject
{
    /// <summary>
    /// Maximum allowed length, aligned with the SQL VARCHAR(100) column.
    /// </summary>
    public const int MaxLength = 100;

    [GeneratedRegex(
        @"^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        matchTimeoutMilliseconds: 200)]
    private static partial Regex FormatRegex();

    /// <summary>
    /// Gets the normalized (lowercase, trimmed) email value.
    /// </summary>
    public string Value { get; }

    private Email(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a new <see cref="Email"/> after normalizing and validating the input.
    /// </summary>
    /// <param name="value">The candidate email string.</param>
    /// <returns>A validated <see cref="Email"/> instance.</returns>
    /// <exception cref="InvalidEmailException">Thrown when the value is empty, too long or malformed.</exception>
    public static Email Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidEmailException("Email cannot be empty.");
        }

        var normalized = value.Trim().ToLowerInvariant();

        if (normalized.Length > MaxLength)
        {
            throw new InvalidEmailException(
                $"Email must be at most {MaxLength} characters (got {normalized.Length}).");
        }

        if (!FormatRegex().IsMatch(normalized))
        {
            throw new InvalidEmailException($"Email '{normalized}' is not a valid email address.");
        }

        return new Email(normalized);
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    /// <inheritdoc />
    public override string ToString() => Value;
}
