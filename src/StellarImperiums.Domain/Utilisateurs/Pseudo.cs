using StellarImperiums.Domain.Common;
using StellarImperiums.Domain.Utilisateurs.Exceptions;

namespace StellarImperiums.Domain.Utilisateurs;

/// <summary>
/// Represents a user's pseudo (display name). Immutable value object whose value is trimmed,
/// case-sensitive and constrained between <see cref="LongueurMin"/> and <see cref="LongueurMax"/> characters.
/// </summary>
public sealed class Pseudo : ValueObject
{
    /// <summary>
    /// Minimum allowed length for a pseudo, aligned with the SQL CHECK constraint.
    /// </summary>
    public const int LongueurMin = 3;

    /// <summary>
    /// Maximum allowed length for a pseudo, aligned with the SQL VARCHAR(50) column.
    /// </summary>
    public const int LongueurMax = 50;

    /// <summary>
    /// Gets the underlying string value.
    /// </summary>
    public string Valeur { get; }

    private Pseudo(string valeur)
    {
        Valeur = valeur;
    }

    /// <summary>
    /// Creates a new <see cref="Pseudo"/> after trimming and validating the input.
    /// </summary>
    /// <param name="valeur">The candidate pseudo string.</param>
    /// <returns>A validated <see cref="Pseudo"/> instance.</returns>
    /// <exception cref="PseudoInvalideException">Thrown when the value is empty or out of length bounds.</exception>
    public static Pseudo Creer(string valeur)
    {
        if (string.IsNullOrWhiteSpace(valeur))
        {
            throw new PseudoInvalideException("Pseudo cannot be empty.");
        }

        var trimmed = valeur.Trim();

        if (trimmed.Length < LongueurMin)
        {
            throw new PseudoInvalideException(
                $"Pseudo must be at least {LongueurMin} characters (got {trimmed.Length}).");
        }

        if (trimmed.Length > LongueurMax)
        {
            throw new PseudoInvalideException(
                $"Pseudo must be at most {LongueurMax} characters (got {trimmed.Length}).");
        }

        return new Pseudo(trimmed);
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Valeur;
    }

    /// <inheritdoc />
    public override string ToString() => Valeur;
}
