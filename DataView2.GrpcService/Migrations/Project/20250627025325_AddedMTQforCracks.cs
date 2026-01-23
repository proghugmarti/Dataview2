using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataView2.GrpcService.Migrations.Project
{
    /// <inheritdoc />
    public partial class AddedMTQforCracks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MTQ",
                table: "LCMS_CrackSummary",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MTQ",
                table: "LCMS_Cracking_Raw",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MTQ",
                table: "LCMS_CrackSummary");

            migrationBuilder.DropColumn(
                name: "MTQ",
                table: "LCMS_Cracking_Raw");
        }
    }
}
