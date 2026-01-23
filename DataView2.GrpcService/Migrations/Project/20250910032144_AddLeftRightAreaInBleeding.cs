using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataView2.GrpcService.Migrations.Project
{
    /// <inheritdoc />
    public partial class AddLeftRightAreaInBleeding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "LeftArea_m2",
                table: "LCMS_Bleeding",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "RightArea_m2",
                table: "LCMS_Bleeding",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LeftArea_m2",
                table: "LCMS_Bleeding");

            migrationBuilder.DropColumn(
                name: "RightArea_m2",
                table: "LCMS_Bleeding");
        }
    }
}
