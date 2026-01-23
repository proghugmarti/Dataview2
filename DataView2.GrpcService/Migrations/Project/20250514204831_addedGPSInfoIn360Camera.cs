using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataView2.GrpcService.Migrations.Project
{
    /// <inheritdoc />
    public partial class addedGPSInfoIn360Camera : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Camera360FrameId",
                table: "Camera360Frame",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "GPSLatitude",
                table: "Camera360Frame",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "GPSLongitude",
                table: "Camera360Frame",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "GPSTrackAngle",
                table: "Camera360Frame",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "GeoJSON",
                table: "Camera360Frame",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Camera360FrameId",
                table: "Camera360Frame");

            migrationBuilder.DropColumn(
                name: "GPSLatitude",
                table: "Camera360Frame");

            migrationBuilder.DropColumn(
                name: "GPSLongitude",
                table: "Camera360Frame");

            migrationBuilder.DropColumn(
                name: "GPSTrackAngle",
                table: "Camera360Frame");

            migrationBuilder.DropColumn(
                name: "GeoJSON",
                table: "Camera360Frame");
        }
    }
}
