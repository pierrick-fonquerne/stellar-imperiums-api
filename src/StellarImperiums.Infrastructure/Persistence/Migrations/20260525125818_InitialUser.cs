using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace StellarImperiums.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "utilisateur",
                columns: table => new
                {
                    id_utilisateur = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    pseudo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    mail = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    mot_de_passe = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    suspendu = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    necessaire_a_modifier = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    date_inscription = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    derniere_connexion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    reset_token = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    reset_token_expire_le = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_utilisateur", x => x.id_utilisateur);
                });

            migrationBuilder.CreateIndex(
                name: "idx_utilisateur_date_inscription",
                table: "utilisateur",
                column: "date_inscription",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "idx_utilisateur_reset_token",
                table: "utilisateur",
                column: "reset_token",
                unique: true,
                filter: "\"reset_token\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "idx_utilisateur_role",
                table: "utilisateur",
                column: "role");

            migrationBuilder.CreateIndex(
                name: "utilisateur_mail_key",
                table: "utilisateur",
                column: "mail",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "utilisateur_pseudo_key",
                table: "utilisateur",
                column: "pseudo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "utilisateur");
        }
    }
}
