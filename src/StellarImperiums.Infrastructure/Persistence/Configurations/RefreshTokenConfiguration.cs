using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StellarImperiums.Domain.Tokens;
using StellarImperiums.Domain.Users;

namespace StellarImperiums.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping configuration for the <see cref="RefreshToken"/> entity.
/// </summary>
/// <remarks>
/// Follows the French snake_case naming convention of the shared SQL schema. The token hash
/// is a 64-character uppercase hexadecimal SHA-256 digest with a unique index used as the
/// primary lookup path. The PostgreSQL <c>xmin</c> system column is used as the optimistic
/// concurrency token so that two concurrent rotations of the same token cannot both commit.
/// </remarks>
public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    private const int Sha256HexLength = 64;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("jeton_rafraichissement");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id)
            .HasColumnName("id_jeton")
            .UseIdentityColumn();

        builder.Property(t => t.UserId)
            .HasColumnName("id_utilisateur")
            .IsRequired();

        builder.Property(t => t.TokenHash)
            .HasColumnName("hash_jeton")
            .HasMaxLength(Sha256HexLength)
            .IsRequired();

        builder.Property(t => t.FamilyId)
            .HasColumnName("id_famille")
            .IsRequired();

        builder.Property(t => t.CreatedAt)
            .HasColumnName("cree_le")
            .IsRequired();

        builder.Property(t => t.ExpiresAt)
            .HasColumnName("expire_le")
            .IsRequired();

        builder.Property(t => t.RevokedAt)
            .HasColumnName("revoque_le");

        builder.Property(t => t.ReplacedByTokenHash)
            .HasColumnName("remplace_par_hash")
            .HasMaxLength(Sha256HexLength);

        builder.HasIndex(t => t.TokenHash)
            .IsUnique()
            .HasDatabaseName("jeton_rafraichissement_hash_key");

        builder.HasIndex(t => t.FamilyId)
            .HasDatabaseName("idx_jeton_rafraichissement_famille");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_jeton_rafraichissement_utilisateur");

        builder.Property<uint>("xmin")
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();
    }
}
