using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CareLink.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "facility",
                columns: table => new
                {
                    facility_id = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    district = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_facility", x => x.facility_id);
                });

            migrationBuilder.CreateTable(
                name: "patient",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    facility_id = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    patient_number = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    first_name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    date_of_birth = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    sex = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    phone_number = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_patient", x => x.id);
                    table.ForeignKey(
                        name: "FK_patient_facility_facility_id",
                        column: x => x.facility_id,
                        principalTable: "facility",
                        principalColumn: "facility_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "visit",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    patient_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    visit_date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    next_appointment_date = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    visit_type = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_visit", x => x.id);
                    table.CheckConstraint("CK_visit_next_appointment", "next_appointment_date IS NULL OR next_appointment_date >= visit_date");
                    table.ForeignKey(
                        name: "FK_visit_patient_patient_id",
                        column: x => x.patient_id,
                        principalTable: "patient",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_patient_facility_id",
                table: "patient",
                column: "facility_id");

            migrationBuilder.CreateIndex(
                name: "IX_patient_facility_id_patient_number",
                table: "patient",
                columns: new[] { "facility_id", "patient_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_visit_patient_id_visit_date_created_at",
                table: "visit",
                columns: new[] { "patient_id", "visit_date", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "visit");

            migrationBuilder.DropTable(
                name: "patient");

            migrationBuilder.DropTable(
                name: "facility");
        }
    }
}
