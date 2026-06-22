using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StellarImperiums.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRefreshTokenCheckConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "ck_jeton_rafraichissement_dates_coherentes",
                table: "jeton_rafraichissement",
                sql: "expire_le > cree_le");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_jeton_rafraichissement_dates_coherentes",
                table: "jeton_rafraichissement");
        }
    }
}
