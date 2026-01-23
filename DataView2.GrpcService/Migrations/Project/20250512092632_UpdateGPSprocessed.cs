using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataView2.GrpcService.Migrations.Project
{
    /// <inheritdoc />
    public partial class UpdateGPSprocessed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AltitudeHAE",
                table: "GPS_Processed");

            migrationBuilder.DropColumn(
                name: "AltitudeMSL",
                table: "GPS_Processed");

            migrationBuilder.DropColumn(
                name: "Easting",
                table: "GPS_Processed");

            migrationBuilder.DropColumn(
                name: "GPSSource",
                table: "GPS_Processed");

            migrationBuilder.DropColumn(
                name: "GPSTime",
                table: "GPS_Processed");

            migrationBuilder.DropColumn(
                name: "HDOP",
                table: "GPS_Processed");

            migrationBuilder.DropColumn(
                name: "LRPNum",
                table: "GPS_Processed");

            migrationBuilder.DropColumn(
                name: "Northing",
                table: "GPS_Processed");

            migrationBuilder.DropColumn(
                name: "OdoTime",
                table: "GPS_Processed");

            migrationBuilder.DropColumn(
                name: "PDOP",
                table: "GPS_Processed");

            migrationBuilder.DropColumn(
                name: "Speed",
                table: "GPS_Processed");

            migrationBuilder.AlterColumn<double>(
                name: "OdoTime",
                table: "OdoData",
                type: "REAL",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<double>(
                name: "UTCTime",
                table: "GPS_Raw",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "REAL",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "Heading",
                table: "GPS_Processed",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "REAL",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OdoCount",
                table: "GPS_Processed",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SurveyId",
                table: "GPS_Processed",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "Time",
                table: "GPS_Processed",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OdoCount",
                table: "GPS_Processed");

            migrationBuilder.DropColumn(
                name: "SurveyId",
                table: "GPS_Processed");

            migrationBuilder.DropColumn(
                name: "Time",
                table: "GPS_Processed");

            migrationBuilder.AlterColumn<int>(
                name: "OdoTime",
                table: "OdoData",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "REAL");

            migrationBuilder.AlterColumn<double>(
                name: "UTCTime",
                table: "GPS_Raw",
                type: "REAL",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "REAL");

            migrationBuilder.AlterColumn<double>(
                name: "Heading",
                table: "GPS_Processed",
                type: "REAL",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "REAL");

            migrationBuilder.AddColumn<float>(
                name: "AltitudeHAE",
                table: "GPS_Processed",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "AltitudeMSL",
                table: "GPS_Processed",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Easting",
                table: "GPS_Processed",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GPSSource",
                table: "GPS_Processed",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GPSTime",
                table: "GPS_Processed",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "HDOP",
                table: "GPS_Processed",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "LRPNum",
                table: "GPS_Processed",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Northing",
                table: "GPS_Processed",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OdoTime",
                table: "GPS_Processed",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "PDOP",
                table: "GPS_Processed",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Speed",
                table: "GPS_Processed",
                type: "REAL",
                nullable: true);
        }
    }
}
