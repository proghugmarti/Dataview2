using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataView2.GrpcService.Migrations.Project
{
    /// <inheritdoc />
    public partial class CrackSummaryMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "maxX",
                table: "LCMS_CrackSummary",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "maxY",
                table: "LCMS_CrackSummary",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "minX",
                table: "LCMS_CrackSummary",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "minY",
                table: "LCMS_CrackSummary",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "maxX",
                table: "LCMS_CrackSummary");

            migrationBuilder.DropColumn(
                name: "maxY",
                table: "LCMS_CrackSummary");

            migrationBuilder.DropColumn(
                name: "minX",
                table: "LCMS_CrackSummary");

            migrationBuilder.DropColumn(
                name: "minY",
                table: "LCMS_CrackSummary");
        }
    }
}
