using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataView2.GrpcService.Migrations.Project
{
    /// <inheritdoc />
    public partial class addedLasRutting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GeoJSON",
                table: "Summary");

            migrationBuilder.CreateTable(
                name: "LAS_Rutting",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SurveyId = table.Column<string>(type: "TEXT", nullable: false),
                    SurveyDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    RutDepth_mm = table.Column<double>(type: "REAL", nullable: false),
                    RutWidth_m = table.Column<double>(type: "REAL", nullable: false),
                    GPSLatitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSLongitude = table.Column<double>(type: "REAL", nullable: false),
                    GeoJSON = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LAS_Rutting", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LAS_Rutting");

            migrationBuilder.AddColumn<string>(
                name: "GeoJSON",
                table: "Summary",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
