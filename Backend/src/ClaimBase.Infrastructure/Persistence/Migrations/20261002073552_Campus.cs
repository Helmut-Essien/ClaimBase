using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClaimBase.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Campus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Faculties_TenantId_Name",
                table: "Faculties");

            migrationBuilder.CreateTable(
                name: "Campuses",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    TenantId = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Campuses", x => x.Id);
                    table.CheckConstraint("CK_Campuses_Name", "char_length(\"Name\") > 0");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Campuses_TenantId_Name",
                table: "Campuses",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.AddColumn<string>(
                name: "CampusId",
                table: "Faculties",
                type: "character varying(26)",
                maxLength: 26,
                nullable: true);

            // Existing faculties were created before campuses. Each university gets one campus, and its faculties move there.
            migrationBuilder.Sql(
                """
                INSERT INTO "Campuses" ("Id", "TenantId", "Name")
                SELECT CASE
                         WHEN "TenantId" = '01JB0000000000000000000001' THEN '01JB000000000000000000000B'
                         ELSE substr(md5("TenantId" || 'campus'), 1, 26)
                       END,
                       "TenantId",
                       'Main Campus'
                FROM "Faculties"
                GROUP BY "TenantId";

                UPDATE "Faculties" AS faculty
                SET "CampusId" = campus."Id"
                FROM "Campuses" AS campus
                WHERE faculty."TenantId" = campus."TenantId"
                  AND faculty."CampusId" IS NULL;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "CampusId",
                table: "Faculties",
                type: "character varying(26)",
                maxLength: 26,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(26)",
                oldMaxLength: 26,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Faculties_CampusId",
                table: "Faculties",
                column: "CampusId");

            migrationBuilder.CreateIndex(
                name: "IX_Faculties_TenantId_CampusId_Name",
                table: "Faculties",
                columns: new[] { "TenantId", "CampusId", "Name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Faculties_Campuses_CampusId",
                table: "Faculties",
                column: "CampusId",
                principalTable: "Campuses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Faculties_Campuses_CampusId",
                table: "Faculties");

            migrationBuilder.DropTable(
                name: "Campuses");

            migrationBuilder.DropIndex(
                name: "IX_Faculties_CampusId",
                table: "Faculties");

            migrationBuilder.DropIndex(
                name: "IX_Faculties_TenantId_CampusId_Name",
                table: "Faculties");

            migrationBuilder.DropColumn(
                name: "CampusId",
                table: "Faculties");

            migrationBuilder.CreateIndex(
                name: "IX_Faculties_TenantId_Name",
                table: "Faculties",
                columns: new[] { "TenantId", "Name" },
                unique: true);
        }
    }
}
