using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StellarImperiums.Domain.Utilisateurs;

namespace StellarImperiums.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping configuration for the <see cref="Utilisateur"/> aggregate root.
/// </summary>
/// <remarks>
/// The configuration matches the SQL schema defined in <c>stellar-imperiums-shared/database/sql/01-schema.sql</c>:
/// snake_case column names, value object conversions, role stored as lowercase string, and the partial
/// unique index on <c>reset_token</c>.
/// </remarks>
public sealed class UtilisateurConfiguration : IEntityTypeConfiguration<Utilisateur>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Utilisateur> builder)
    {
        builder.ToTable("utilisateur");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id)
            .HasColumnName("id_utilisateur")
            .UseIdentityColumn();

        builder.Property(u => u.Pseudo)
            .HasColumnName("pseudo")
            .HasMaxLength(Pseudo.LongueurMax)
            .IsRequired()
            .HasConversion(
                pseudo => pseudo.Valeur,
                value => Pseudo.Creer(value));

        builder.Property(u => u.Mail)
            .HasColumnName("mail")
            .HasMaxLength(Email.LongueurMax)
            .IsRequired()
            .HasConversion(
                mail => mail.Valeur,
                value => Email.Creer(value));

        builder.Property(u => u.MotDePasse)
            .HasColumnName("mot_de_passe")
            .HasMaxLength(MotDePasseHash.LongueurMax)
            .IsRequired()
            .HasConversion(
                hash => hash.Valeur,
                value => MotDePasseHash.Creer(value));

        builder.Property(u => u.Role)
            .HasColumnName("role")
            .HasMaxLength(20)
            .IsRequired()
            .HasConversion(
                role => role == RoleUtilisateur.Admin ? "admin" : "joueur",
                value => value == "admin" ? RoleUtilisateur.Admin : RoleUtilisateur.Joueur);

        builder.Property(u => u.Suspendu)
            .HasColumnName("suspendu")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(u => u.NecessaireAModifier)
            .HasColumnName("necessaire_a_modifier")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(u => u.DateInscription)
            .HasColumnName("date_inscription")
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(u => u.DerniereConnexion)
            .HasColumnName("derniere_connexion");

        builder.Property(u => u.ResetToken)
            .HasColumnName("reset_token")
            .HasMaxLength(255);

        builder.Property(u => u.ResetTokenExpireLe)
            .HasColumnName("reset_token_expire_le");

        builder.HasIndex(u => u.Pseudo)
            .IsUnique()
            .HasDatabaseName("utilisateur_pseudo_key");

        builder.HasIndex(u => u.Mail)
            .IsUnique()
            .HasDatabaseName("utilisateur_mail_key");

        builder.HasIndex(u => u.Role)
            .HasDatabaseName("idx_utilisateur_role");

        builder.HasIndex(u => u.DateInscription)
            .HasDatabaseName("idx_utilisateur_date_inscription")
            .IsDescending();

        builder.HasIndex(u => u.ResetToken)
            .IsUnique()
            .HasFilter("\"reset_token\" IS NOT NULL")
            .HasDatabaseName("idx_utilisateur_reset_token");
    }
}
