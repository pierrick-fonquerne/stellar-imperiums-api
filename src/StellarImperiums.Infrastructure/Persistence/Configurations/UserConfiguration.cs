using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StellarImperiums.Domain.Users;

namespace StellarImperiums.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping configuration for the <see cref="User"/> aggregate root.
/// </summary>
/// <remarks>
/// The configuration maps English property names to the French SQL schema defined in
/// <c>stellar-imperiums-shared/database/sql/01-schema.sql</c>: snake_case column names,
/// value object conversions, role stored as lowercase string, partial unique index on the reset token.
/// </remarks>
public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("utilisateur");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id)
            .HasColumnName("id_utilisateur")
            .UseIdentityColumn();

        builder.Property(u => u.Username)
            .HasColumnName("pseudo")
            .HasMaxLength(Username.MaxLength)
            .IsRequired()
            .HasConversion(
                username => username.Value,
                value => Username.Create(value));

        builder.Property(u => u.Email)
            .HasColumnName("mail")
            .HasMaxLength(Email.MaxLength)
            .IsRequired()
            .HasConversion(
                email => email.Value,
                value => Email.Create(value));

        builder.Property(u => u.PasswordHash)
            .HasColumnName("mot_de_passe")
            .HasMaxLength(PasswordHash.MaxLength)
            .IsRequired()
            .HasConversion(
                hash => hash.Value,
                value => PasswordHash.Create(value));

        builder.Property(u => u.Role)
            .HasColumnName("role")
            .HasMaxLength(20)
            .IsRequired()
            .HasConversion(
                role => role == UserRole.Admin ? "admin" : "joueur",
                value => value == "admin" ? UserRole.Admin : UserRole.Player);

        builder.Property(u => u.IsSuspended)
            .HasColumnName("suspendu")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(u => u.MustChangePassword)
            .HasColumnName("necessaire_a_modifier")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(u => u.RegistrationDate)
            .HasColumnName("date_inscription")
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(u => u.LastLoginAt)
            .HasColumnName("derniere_connexion");

        builder.Property(u => u.PasswordResetToken)
            .HasColumnName("reset_token")
            .HasMaxLength(255);

        builder.Property(u => u.PasswordResetTokenExpiresAt)
            .HasColumnName("reset_token_expire_le");

        builder.HasIndex(u => u.Username)
            .IsUnique()
            .HasDatabaseName("utilisateur_pseudo_key");

        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasDatabaseName("utilisateur_mail_key");

        builder.HasIndex(u => u.Role)
            .HasDatabaseName("idx_utilisateur_role");

        builder.HasIndex(u => u.RegistrationDate)
            .HasDatabaseName("idx_utilisateur_date_inscription")
            .IsDescending();

        builder.HasIndex(u => u.PasswordResetToken)
            .IsUnique()
            .HasFilter("\"reset_token\" IS NOT NULL")
            .HasDatabaseName("idx_utilisateur_reset_token");
    }
}
