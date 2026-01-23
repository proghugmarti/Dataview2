using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataView2.GrpcService.Migrations.Project
{
    /// <inheritdoc />
    public partial class addedBoundarySummary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SampleUnitBoundaryId",
                table: "MetaTableValue");

            migrationBuilder.CreateTable(
                name: "BoundarySummary",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SurveyId = table.Column<string>(type: "TEXT", nullable: false),
                    TableName = table.Column<string>(type: "TEXT", nullable: false),
                    NumericField = table.Column<string>(type: "TEXT", nullable: false),
                    Operation = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<double>(type: "REAL", nullable: false),
                    SampleUnitBoundaryId = table.Column<int>(type: "INTEGER", nullable: false),
                    GeoJSON = table.Column<string>(type: "TEXT", nullable: false),
                    GPSLatitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSLongitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSTrackAngle = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoundarySummary", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BoundarySummary");

            migrationBuilder.AddColumn<int>(
                name: "SampleUnitBoundaryId",
                table: "MetaTableValue",
                type: "INTEGER",
                nullable: true);
        }
    }
}
