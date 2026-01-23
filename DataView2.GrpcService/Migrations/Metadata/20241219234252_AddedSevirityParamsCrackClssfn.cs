using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataView2.GrpcService.Migrations.Metadata
{
    /// <inheritdoc />
    public partial class AddedSevirityParamsCrackClssfn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "HighThreshold",
                table: "CrackClassification",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "LowMediumThreshold",
                table: "CrackClassification",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "LowThreshold",
                table: "CrackClassification",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "MediumHighThreshold",
                table: "CrackClassification",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.UpdateData(
                table: "CrackClassification",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "HighThreshold", "LowMediumThreshold", "LowThreshold", "MediumHighThreshold" },
                values: new object[] { 0.0, 0.0, 0.0, 0.0 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HighThreshold",
                table: "CrackClassification");

            migrationBuilder.DropColumn(
                name: "LowMediumThreshold",
                table: "CrackClassification");

            migrationBuilder.DropColumn(
                name: "LowThreshold",
                table: "CrackClassification");

            migrationBuilder.DropColumn(
                name: "MediumHighThreshold",
                table: "CrackClassification");
        }
    }
}
