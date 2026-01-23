using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataView2.GrpcService.Migrations.Project
{
    /// <inheritdoc />
    public partial class addedNumOfSlabs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SectionId",
                table: "SampleUnit");

            migrationBuilder.DropColumn(
                name: "isPCCPavement",
                table: "SampleUnit");

            migrationBuilder.AddColumn<bool>(
                name: "IsPCCPavement",
                table: "SampleUnit_Set",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "NumOfSlabs",
                table: "SampleUnit",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SectionId",
                table: "PCIRatings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPCCPavement",
                table: "SampleUnit_Set");

            migrationBuilder.DropColumn(
                name: "NumOfSlabs",
                table: "SampleUnit");

            migrationBuilder.DropColumn(
                name: "SectionId",
                table: "PCIRatings");

            migrationBuilder.AddColumn<string>(
                name: "SectionId",
                table: "SampleUnit",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "isPCCPavement",
                table: "SampleUnit",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }
    }
}
