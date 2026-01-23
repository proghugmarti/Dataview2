using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DataView2.GrpcService.Migrations.Metadata
{
    /// <inheritdoc />
    public partial class AddedIndexOfSurveyIdAndSegId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CrackClassification",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MinSizeToStraight = table.Column<int>(type: "INTEGER", nullable: false),
                    MinSizeToAvoidMerge = table.Column<int>(type: "INTEGER", nullable: false),
                    Straightness = table.Column<double>(type: "REAL", nullable: false),
                    MinimumDeep = table.Column<double>(type: "REAL", nullable: false),
                    IgnoreOutLanes = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrackClassification", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DatabaseRegistry",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Path = table.Column<string>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAtActionResult = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtActionResult = table.Column<DateTime>(type: "TEXT", nullable: false),
                    GPSLatitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSLongitude = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatabaseRegistry", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GeneralSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false),
                    Category = table.Column<string>(type: "TEXT", nullable: false),
                    GeoJSON = table.Column<string>(type: "TEXT", nullable: false),
                    GPSLatitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSLongitude = table.Column<double>(type: "REAL", nullable: false),
                    RoundedGPSLatitude = table.Column<double>(type: "REAL", nullable: false),
                    RoundedGPSLongitude = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeneralSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProjectRegistries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAtActionResult = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtActionResult = table.Column<DateTime>(type: "TEXT", nullable: false),
                    GeoJSON = table.Column<string>(type: "TEXT", nullable: false),
                    GPSLatitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSLongitude = table.Column<double>(type: "REAL", nullable: false),
                    RoundedGPSLatitude = table.Column<double>(type: "REAL", nullable: false),
                    RoundedGPSLongitude = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectRegistries", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "CrackClassification",
                columns: new[] { "Id", "IgnoreOutLanes", "MinSizeToAvoidMerge", "MinSizeToStraight", "MinimumDeep", "Straightness" },
                values: new object[] { 1, true, 6, 4, 0.0, 0.69999999999999996 });

            migrationBuilder.InsertData(
                table: "GeneralSettings",
                columns: new[] { "Id", "Category", "Description", "GPSLatitude", "GPSLongitude", "GeoJSON", "Name", "RoundedGPSLatitude", "RoundedGPSLongitude", "Type", "Value" },
                values: new object[,]
                {
                    { 1, "NetWorking", "DataView IP Address", 0.0, 0.0, "DefaultGeoJSON", "IP Address", 0.0, 0.0, 1, "0.0.0.1" },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CrackClassification");

            migrationBuilder.DropTable(
                name: "DatabaseRegistry");

            migrationBuilder.DropTable(
                name: "GeneralSettings");

            migrationBuilder.DropTable(
                name: "ProjectRegistries");
        }
    }
}
