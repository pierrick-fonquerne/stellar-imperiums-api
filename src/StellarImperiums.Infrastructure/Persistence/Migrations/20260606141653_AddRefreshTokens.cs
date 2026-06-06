using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace StellarImperiums.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRefreshTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "jeton_rafraichissement",
                columns: table => new
                {
                    id_jeton = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_utilisateur = table.Column<int>(type: "integer", nullable: false),
                    hash_jeton = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    id_famille = table.Column<Guid>(type: "uuid", nullable: false),
                    cree_le = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expire_le = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revoque_le = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    remplace_par_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_jeton_rafraichissement", x => x.id_jeton);
                    table.ForeignKey(
                        name: "fk_jeton_rafraichissement_utilisateur",
                        column: x => x.id_utilisateur,
                        principalTable: "utilisateur",
                        principalColumn: "id_utilisateur",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_jeton_rafraichissement_famille",
                table: "jeton_rafraichissement",
                column: "id_famille");

            migrationBuilder.CreateIndex(
                name: "IX_jeton_rafraichissement_id_utilisateur",
                table: "jeton_rafraichissement",
                column: "id_utilisateur");

            migrationBuilder.CreateIndex(
                name: "jeton_rafraichissement_hash_key",
                table: "jeton_rafraichissement",
                column: "hash_jeton",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "jeton_rafraichissement");
        }
    }
}
