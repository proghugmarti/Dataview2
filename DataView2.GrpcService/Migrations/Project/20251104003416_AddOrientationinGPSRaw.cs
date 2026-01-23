using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataView2.GrpcService.Migrations.Project
{
    /// <inheritdoc />
    public partial class AddOrientationinGPSRaw : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Pitch",
                table: "GPS_Raw",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Roll",
                table: "GPS_Raw",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Yaw",
                table: "GPS_Raw",
                type: "REAL",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Pitch",
                table: "GPS_Raw");

            migrationBuilder.DropColumn(
                name: "Roll",
                table: "GPS_Raw");

            migrationBuilder.DropColumn(
                name: "Yaw",
                table: "GPS_Raw");
        }
    }
}
