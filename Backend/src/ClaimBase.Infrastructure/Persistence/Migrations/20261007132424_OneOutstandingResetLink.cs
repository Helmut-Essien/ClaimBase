using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClaimBase.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OneOutstandingResetLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PasswordResetTokens_UserId",
                table: "PasswordResetTokens");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_OneOutstanding",
                table: "PasswordResetTokens",
                column: "UserId",
                unique: true,
                filter: "\"UsedAt\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PasswordResetTokens_OneOutstanding",
                table: "PasswordResetTokens");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_UserId",
                table: "PasswordResetTokens",
                column: "UserId");
        }
    }
}
