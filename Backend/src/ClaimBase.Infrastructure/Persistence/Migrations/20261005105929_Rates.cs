using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClaimBase.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Rates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TeachingRates",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    TenantId = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    PositionTitleId = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    QualificationId = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeachingRates", x => x.Id);
                    table.CheckConstraint("CK_TeachingRates_Amount", "\"Amount\" >= 0");
                    table.CheckConstraint("CK_TeachingRates_Range", "\"EffectiveTo\" IS NULL OR \"EffectiveTo\" > \"EffectiveFrom\"");
                    table.ForeignKey(
                        name: "FK_TeachingRates_PositionTitles_PositionTitleId",
                        column: x => x.PositionTitleId,
                        principalTable: "PositionTitles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeachingRates_Qualifications_QualificationId",
                        column: x => x.QualificationId,
                        principalTable: "Qualifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TransportRates",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    TenantId = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransportRates", x => x.Id);
                    table.CheckConstraint("CK_TransportRates_Amount", "\"Amount\" >= 0");
                    table.CheckConstraint("CK_TransportRates_Range", "\"EffectiveTo\" IS NULL OR \"EffectiveTo\" > \"EffectiveFrom\"");
                });

            migrationBuilder.CreateIndex(
                name: "IX_TeachingRates_PositionTitleId",
                table: "TeachingRates",
                column: "PositionTitleId");

            migrationBuilder.CreateIndex(
                name: "IX_TeachingRates_QualificationId",
                table: "TeachingRates",
                column: "QualificationId");

            migrationBuilder.CreateIndex(
                name: "IX_TeachingRates_TenantId_PositionTitleId_QualificationId_Effe~",
                table: "TeachingRates",
                columns: new[] { "TenantId", "PositionTitleId", "QualificationId", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_TransportRates_TenantId_EffectiveFrom",
                table: "TransportRates",
                columns: new[] { "TenantId", "EffectiveFrom" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TeachingRates");

            migrationBuilder.DropTable(
                name: "TransportRates");
        }
    }
}
