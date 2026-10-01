using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClaimBase.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AcademicCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Faculties",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    TenantId = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Faculties", x => x.Id);
                    table.CheckConstraint("CK_Faculties_Name", "char_length(\"Name\") > 0");
                });

            migrationBuilder.CreateTable(
                name: "PositionTitles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    TenantId = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PositionTitles", x => x.Id);
                    table.CheckConstraint("CK_PositionTitles_Name", "char_length(\"Name\") > 0");
                });

            migrationBuilder.CreateTable(
                name: "Qualifications",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    TenantId = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Qualifications", x => x.Id);
                    table.CheckConstraint("CK_Qualifications_Name", "char_length(\"Name\") > 0");
                });

            migrationBuilder.CreateTable(
                name: "Semesters",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    TenantId = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Semesters", x => x.Id);
                    table.CheckConstraint("CK_Semesters_Dates", "\"EndDate\" >= \"StartDate\"");
                    table.CheckConstraint("CK_Semesters_Name", "char_length(\"Name\") > 0");
                    table.CheckConstraint("CK_Semesters_Status", "\"Status\" IN ('Draft', 'Open', 'Closed')");
                });

            migrationBuilder.CreateTable(
                name: "Staff",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    TenantId = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    StaffNumber = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    EmploymentType = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    BiometricId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Staff", x => x.Id);
                    table.CheckConstraint("CK_Staff_DisplayName", "char_length(\"DisplayName\") > 0");
                    table.CheckConstraint("CK_Staff_EmploymentType", "\"EmploymentType\" IN ('PartTime', 'FullTime')");
                    table.CheckConstraint("CK_Staff_StaffNumber", "char_length(\"StaffNumber\") > 0");
                });

            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    TenantId = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    FacultyId = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.Id);
                    table.CheckConstraint("CK_Departments_Name", "char_length(\"Name\") > 0");
                    table.ForeignKey(
                        name: "FK_Departments_Faculties_FacultyId",
                        column: x => x.FacultyId,
                        principalTable: "Faculties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Courses",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    TenantId = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    QualificationId = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Courses", x => x.Id);
                    table.CheckConstraint("CK_Courses_Code", "char_length(\"Code\") > 0");
                    table.CheckConstraint("CK_Courses_Name", "char_length(\"Name\") > 0");
                    table.ForeignKey(
                        name: "FK_Courses_Qualifications_QualificationId",
                        column: x => x.QualificationId,
                        principalTable: "Qualifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StaffPositions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    TenantId = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    StaffId = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    PositionTitleId = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffPositions", x => x.Id);
                    table.CheckConstraint("CK_StaffPositions_Range", "\"EffectiveTo\" IS NULL OR \"EffectiveTo\" > \"EffectiveFrom\"");
                    table.ForeignKey(
                        name: "FK_StaffPositions_PositionTitles_PositionTitleId",
                        column: x => x.PositionTitleId,
                        principalTable: "PositionTitles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StaffPositions_Staff_StaffId",
                        column: x => x.StaffId,
                        principalTable: "Staff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StaffDepartments",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    TenantId = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    StaffId = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    DepartmentId = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffDepartments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StaffDepartments_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StaffDepartments_Staff_StaffId",
                        column: x => x.StaffId,
                        principalTable: "Staff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_DepartmentId",
                table: "Users",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_StaffId",
                table: "Users",
                column: "StaffId");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_QualificationId",
                table: "Courses",
                column: "QualificationId");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_TenantId_Code",
                table: "Courses",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Departments_FacultyId",
                table: "Departments",
                column: "FacultyId");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_TenantId_FacultyId_Name",
                table: "Departments",
                columns: new[] { "TenantId", "FacultyId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Faculties_TenantId_Name",
                table: "Faculties",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PositionTitles_TenantId_Name",
                table: "PositionTitles",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Qualifications_TenantId_Name",
                table: "Qualifications",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Semesters_TenantId_Status",
                table: "Semesters",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Staff_TenantId_BiometricId",
                table: "Staff",
                columns: new[] { "TenantId", "BiometricId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Staff_TenantId_StaffNumber",
                table: "Staff",
                columns: new[] { "TenantId", "StaffNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StaffDepartments_DepartmentId",
                table: "StaffDepartments",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffDepartments_StaffId",
                table: "StaffDepartments",
                column: "StaffId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffDepartments_TenantId_DepartmentId",
                table: "StaffDepartments",
                columns: new[] { "TenantId", "DepartmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_StaffDepartments_TenantId_StaffId_DepartmentId",
                table: "StaffDepartments",
                columns: new[] { "TenantId", "StaffId", "DepartmentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StaffPositions_PositionTitleId",
                table: "StaffPositions",
                column: "PositionTitleId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffPositions_StaffId",
                table: "StaffPositions",
                column: "StaffId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffPositions_TenantId_StaffId_EffectiveFrom",
                table: "StaffPositions",
                columns: new[] { "TenantId", "StaffId", "EffectiveFrom" });

            // Slice 1 seed stored these ids before the parent rows existed. Insert them before the foreign keys.
            migrationBuilder.Sql(
                """
                INSERT INTO "Faculties" ("Id", "TenantId", "Name")
                SELECT '01JB0000000000000000000009', '01JB0000000000000000000001', 'Development Faculty'
                WHERE EXISTS (
                    SELECT 1 FROM "Users"
                    WHERE "DepartmentId" = '01JB0000000000000000000007'
                       OR "StaffId" = '01JB0000000000000000000008')
                  AND NOT EXISTS (SELECT 1 FROM "Faculties" WHERE "Id" = '01JB0000000000000000000009');

                INSERT INTO "Departments" ("Id", "TenantId", "FacultyId", "Name")
                SELECT '01JB0000000000000000000007', '01JB0000000000000000000001', '01JB0000000000000000000009', 'Development Department'
                WHERE EXISTS (SELECT 1 FROM "Users" WHERE "DepartmentId" = '01JB0000000000000000000007')
                  AND NOT EXISTS (SELECT 1 FROM "Departments" WHERE "Id" = '01JB0000000000000000000007');

                INSERT INTO "Staff" ("Id", "TenantId", "StaffNumber", "DisplayName", "Email", "EmploymentType", "BiometricId")
                SELECT '01JB0000000000000000000008', '01JB0000000000000000000001', 'DEV-LEC', 'Dev Lecturer', 'lecturer@claimbase.test', 'PartTime', NULL
                WHERE EXISTS (SELECT 1 FROM "Users" WHERE "StaffId" = '01JB0000000000000000000008')
                  AND NOT EXISTS (SELECT 1 FROM "Staff" WHERE "Id" = '01JB0000000000000000000008');

                INSERT INTO "StaffDepartments" ("Id", "TenantId", "StaffId", "DepartmentId")
                SELECT '01JB000000000000000000000A', '01JB0000000000000000000001', '01JB0000000000000000000008', '01JB0000000000000000000007'
                WHERE EXISTS (SELECT 1 FROM "Staff" WHERE "Id" = '01JB0000000000000000000008')
                  AND EXISTS (SELECT 1 FROM "Departments" WHERE "Id" = '01JB0000000000000000000007')
                  AND NOT EXISTS (SELECT 1 FROM "StaffDepartments" WHERE "Id" = '01JB000000000000000000000A');
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Departments_DepartmentId",
                table: "Users",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Staff_StaffId",
                table: "Users",
                column: "StaffId",
                principalTable: "Staff",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Departments_DepartmentId",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Staff_StaffId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "Courses");

            migrationBuilder.DropTable(
                name: "Semesters");

            migrationBuilder.DropTable(
                name: "StaffDepartments");

            migrationBuilder.DropTable(
                name: "StaffPositions");

            migrationBuilder.DropTable(
                name: "Qualifications");

            migrationBuilder.DropTable(
                name: "Departments");

            migrationBuilder.DropTable(
                name: "PositionTitles");

            migrationBuilder.DropTable(
                name: "Staff");

            migrationBuilder.DropTable(
                name: "Faculties");

            migrationBuilder.DropIndex(
                name: "IX_Users_DepartmentId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_StaffId",
                table: "Users");
        }
    }
}
