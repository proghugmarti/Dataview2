using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataView2.GrpcService.Migrations.Project
{
    /// <inheritdoc />
    public partial class addedChainageInSummary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "ChainageEnd",
                table: "Summary",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ChainageStart",
                table: "Summary",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Chainage",
                table: "Camera360Frame",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "SurveyName",
                table: "Camera360Frame",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChainageEnd",
                table: "Summary");

            migrationBuilder.DropColumn(
                name: "ChainageStart",
                table: "Summary");

            migrationBuilder.DropColumn(
                name: "Chainage",
                table: "Camera360Frame");

            migrationBuilder.DropColumn(
                name: "SurveyName",
                table: "Camera360Frame");
        }
    }
}
