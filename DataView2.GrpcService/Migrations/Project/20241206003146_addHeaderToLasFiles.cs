using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataView2.GrpcService.Migrations.Project
{
    /// <inheritdoc />
    public partial class addHeaderToLasFiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "MaxX",
                table: "LASfile",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "MaxY",
                table: "LASfile",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "MaxZ",
                table: "LASfile",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "MinX",
                table: "LASfile",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "MinY",
                table: "LASfile",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "MinZ",
                table: "LASfile",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<uint>(
                name: "NumberOfPointRecords",
                table: "LASfile",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<byte>(
                name: "PointDataFormatId",
                table: "LASfile",
                type: "INTEGER",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<ushort>(
                name: "PointDataRecordLength",
                table: "LASfile",
                type: "INTEGER",
                nullable: false,
                defaultValue: (ushort)0);

            migrationBuilder.AddColumn<string>(
                name: "SurveyId",
                table: "LASfile",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxX",
                table: "LASfile");

            migrationBuilder.DropColumn(
                name: "MaxY",
                table: "LASfile");

            migrationBuilder.DropColumn(
                name: "MaxZ",
                table: "LASfile");

            migrationBuilder.DropColumn(
                name: "MinX",
                table: "LASfile");

            migrationBuilder.DropColumn(
                name: "MinY",
                table: "LASfile");

            migrationBuilder.DropColumn(
                name: "MinZ",
                table: "LASfile");

            migrationBuilder.DropColumn(
                name: "NumberOfPointRecords",
                table: "LASfile");

            migrationBuilder.DropColumn(
                name: "PointDataFormatId",
                table: "LASfile");

            migrationBuilder.DropColumn(
                name: "PointDataRecordLength",
                table: "LASfile");

            migrationBuilder.DropColumn(
                name: "SurveyId",
                table: "LASfile");
        }
    }
}
