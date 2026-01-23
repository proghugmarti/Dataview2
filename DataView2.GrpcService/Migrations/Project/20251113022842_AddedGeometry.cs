using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataView2.GrpcService.Migrations.Project
{
    /// <inheritdoc />
    public partial class AddedGeometry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "SurveyId",
                table: "Geometry_Processed",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<double>(
                name: "GPSAltitude",
                table: "Geometry_Processed",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "GPSLatitude",
                table: "Geometry_Processed",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "GPSLongitude",
                table: "Geometry_Processed",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "GPSTrackAngle",
                table: "Geometry_Processed",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "GeoJSON",
                table: "Geometry_Processed",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PavementType",
                table: "Geometry_Processed",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "RoundedGPSLatitude",
                table: "Geometry_Processed",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "RoundedGPSLongitude",
                table: "Geometry_Processed",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "SegmentId",
                table: "Geometry_Processed",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GPSAltitude",
                table: "Geometry_Processed");

            migrationBuilder.DropColumn(
                name: "GPSLatitude",
                table: "Geometry_Processed");

            migrationBuilder.DropColumn(
                name: "GPSLongitude",
                table: "Geometry_Processed");

            migrationBuilder.DropColumn(
                name: "GPSTrackAngle",
                table: "Geometry_Processed");

            migrationBuilder.DropColumn(
                name: "GeoJSON",
                table: "Geometry_Processed");

            migrationBuilder.DropColumn(
                name: "PavementType",
                table: "Geometry_Processed");

            migrationBuilder.DropColumn(
                name: "RoundedGPSLatitude",
                table: "Geometry_Processed");

            migrationBuilder.DropColumn(
                name: "RoundedGPSLongitude",
                table: "Geometry_Processed");

            migrationBuilder.DropColumn(
                name: "SegmentId",
                table: "Geometry_Processed");

            migrationBuilder.AlterColumn<int>(
                name: "SurveyId",
                table: "Geometry_Processed",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");
        }
    }
}
