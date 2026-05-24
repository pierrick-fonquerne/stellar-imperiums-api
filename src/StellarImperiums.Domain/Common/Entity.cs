namespace StellarImperiums.Domain.Common;

/// <summary>
/// Base class for domain entities identified by an integer primary key.
/// </summary>
/// <remarks>
/// Two entities are considered equal when they share the same runtime type and the same non-zero identifier.
/// Transient entities (Id = 0) are never equal to any other instance, even another transient one.
/// </remarks>
public abstract class Entity
{
    /// <summary>
    /// Gets the persistent identifier assigned by the database (0 for transient entities).
    /// </summary>
    public int Id { get; protected set; }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        if (obj is not Entity other)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (GetType() != other.GetType())
        {
            return false;
        }

        if (Id == 0 || other.Id == 0)
        {
            return false;
        }

        return Id == other.Id;
    }

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    public static bool operator ==(Entity? left, Entity? right) => Equals(left, right);

    public static bool operator !=(Entity? left, Entity? right) => !Equals(left, right);
}
