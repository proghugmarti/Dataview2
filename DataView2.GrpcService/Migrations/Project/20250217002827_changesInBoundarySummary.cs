using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataView2.GrpcService.Migrations.Project
{
    /// <inheritdoc />
    public partial class changesInBoundarySummary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BoundarySummaryName",
                table: "BoundarySummary",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SampleUnitBoundaryName",
                table: "BoundarySummary",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SampleUnitSetName",
                table: "BoundarySummary",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BoundarySummaryName",
                table: "BoundarySummary");

            migrationBuilder.DropColumn(
                name: "SampleUnitBoundaryName",
                table: "BoundarySummary");

            migrationBuilder.DropColumn(
                name: "SampleUnitSetName",
                table: "BoundarySummary");
        }
    }
}
