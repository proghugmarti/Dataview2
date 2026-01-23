using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataView2.GrpcService.Migrations.Project
{
    /// <inheritdoc />
    public partial class addedMoreColumnsInPCIDefect : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PCIRatingName",
                table: "PCIDefects",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SampleUnitName",
                table: "PCIDefects",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SampleUnitSetName",
                table: "PCIDefects",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UnitOfMeasure",
                table: "PCIDefects",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PCIRatingName",
                table: "PCIDefects");

            migrationBuilder.DropColumn(
                name: "SampleUnitName",
                table: "PCIDefects");

            migrationBuilder.DropColumn(
                name: "SampleUnitSetName",
                table: "PCIDefects");

            migrationBuilder.DropColumn(
                name: "UnitOfMeasure",
                table: "PCIDefects");
        }
    }
}
