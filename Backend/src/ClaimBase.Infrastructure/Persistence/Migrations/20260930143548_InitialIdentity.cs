using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClaimBase.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    TimeZoneId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                    table.CheckConstraint("CK_Tenants_CurrencyCode", "char_length(\"CurrencyCode\") = 3");
                    table.CheckConstraint("CK_Tenants_Name", "char_length(\"Name\") > 0");
                    table.CheckConstraint("CK_Tenants_TimeZoneId", "char_length(\"TimeZoneId\") > 0");
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    TenantId = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DepartmentId = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: true),
                    StaffId = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.CheckConstraint("CK_Users_DisplayName", "char_length(\"DisplayName\") > 0");
                    table.CheckConstraint("CK_Users_Email", "char_length(\"Email\") > 0");
                    table.CheckConstraint("CK_Users_HodDepartment", "\"Role\" <> 'HeadOfDepartment' OR \"DepartmentId\" IS NOT NULL");
                    table.CheckConstraint("CK_Users_LecturerStaff", "\"Role\" <> 'Lecturer' OR \"StaffId\" IS NOT NULL");
                    table.CheckConstraint("CK_Users_Role", "\"Role\" IN ('TenantAdmin', 'Admin', 'HeadOfDepartment', 'Finance', 'Lecturer')");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_TenantId_Email",
                table: "Users",
                columns: new[] { "TenantId", "Email" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Tenants");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
