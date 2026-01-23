using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataView2.GrpcService.Migrations.Project
{
    /// <inheritdoc />
    public partial class addedPASERandCrackSummary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LCMS_CrackSummary",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SurveyId = table.Column<string>(type: "TEXT", nullable: false),
                    SurveyDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    Chainage = table.Column<double>(type: "REAL", nullable: true),
                    LRPNumStart = table.Column<long>(type: "INTEGER", nullable: true),
                    LRPChainageStart = table.Column<double>(type: "REAL", nullable: true),
                    PavementType = table.Column<string>(type: "TEXT", nullable: false),
                    CrackId = table.Column<int>(type: "INTEGER", nullable: false),
                    CrackLength_mm = table.Column<double>(type: "REAL", nullable: false),
                    WeightedDepth_mm = table.Column<double>(type: "REAL", nullable: false),
                    WeightedWidth_mm = table.Column<double>(type: "REAL", nullable: false),
                    Faulting = table.Column<double>(type: "REAL", nullable: false),
                    Severity = table.Column<string>(type: "TEXT", nullable: false),
                    ImageFileIndex = table.Column<string>(type: "TEXT", nullable: true),
                    GPSLatitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSLongitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSAltitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSTrackAngle = table.Column<double>(type: "REAL", nullable: false),
                    GeoJSON = table.Column<string>(type: "TEXT", nullable: false),
                    SegmentId = table.Column<int>(type: "INTEGER", nullable: false),
                    RoundedGPSLatitude = table.Column<double>(type: "REAL", nullable: false),
                    RoundedGPSLongitude = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LCMS_CrackSummary", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LCMS_PASER",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SurveyId = table.Column<string>(type: "TEXT", nullable: true),
                    SegmentId = table.Column<int>(type: "INTEGER", nullable: false),
                    PavementType = table.Column<string>(type: "TEXT", nullable: true),
                    PaserRating = table.Column<double>(type: "REAL", nullable: false),
                    GeoJSON = table.Column<string>(type: "TEXT", nullable: false),
                    GPSLatitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSLongitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSAltitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSTrackAngle = table.Column<double>(type: "REAL", nullable: false),
                    RoundedGPSLatitude = table.Column<double>(type: "REAL", nullable: false),
                    RoundedGPSLongitude = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LCMS_PASER", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LCMS_CrackSummary");

            migrationBuilder.DropTable(
                name: "LCMS_PASER");
        }
    }
}
