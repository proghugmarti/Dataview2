using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataView2.GrpcService.Migrations
{
    /// <inheritdoc />
    public partial class addedPCI : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "PCI",
                table: "LCMS_Segment",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Paser",
                table: "LCMS_Segment",
                type: "REAL",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LCMS_PCI",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SurveyId = table.Column<string>(type: "TEXT", nullable: true),
                    SegmentId = table.Column<int>(type: "INTEGER", nullable: false),
                    PavementType = table.Column<string>(type: "TEXT", nullable: true),
                    PCIValue = table.Column<double>(type: "REAL", nullable: false),
                    DeductedValues = table.Column<string>(type: "TEXT", nullable: false),
                    RatingScale = table.Column<string>(type: "TEXT", nullable: false),
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
                    table.PrimaryKey("PK_LCMS_PCI", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LCMS_PCI");

            migrationBuilder.DropColumn(
                name: "PCI",
                table: "LCMS_Segment");

            migrationBuilder.DropColumn(
                name: "Paser",
                table: "LCMS_Segment");
        }
    }
}
