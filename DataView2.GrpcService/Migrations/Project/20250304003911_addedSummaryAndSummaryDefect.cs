using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataView2.GrpcService.Migrations.Project
{
    /// <inheritdoc />
    public partial class addedSummaryAndSummaryDefect : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BoundarySummary");

            migrationBuilder.CreateTable(
                name: "Summary",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    SurveyId = table.Column<string>(type: "TEXT", nullable: false),
                    SampleUnitSetId = table.Column<int>(type: "INTEGER", nullable: false),
                    SampleUnitId = table.Column<int>(type: "INTEGER", nullable: false),
                    GeoJSON = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Summary", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Summary_SampleUnit_SampleUnitId",
                        column: x => x.SampleUnitId,
                        principalTable: "SampleUnit",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Summary_SampleUnit_Set_SampleUnitSetId",
                        column: x => x.SampleUnitSetId,
                        principalTable: "SampleUnit_Set",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SummaryDefect",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TableName = table.Column<string>(type: "TEXT", nullable: false),
                    NumericField = table.Column<string>(type: "TEXT", nullable: false),
                    Operation = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<double>(type: "REAL", nullable: false),
                    SummaryId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SummaryDefect", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SummaryDefect_Summary_SummaryId",
                        column: x => x.SummaryId,
                        principalTable: "Summary",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Summary_SampleUnitId",
                table: "Summary",
                column: "SampleUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_Summary_SampleUnitSetId",
                table: "Summary",
                column: "SampleUnitSetId");

            migrationBuilder.CreateIndex(
                name: "IX_SummaryDefect_SummaryId",
                table: "SummaryDefect",
                column: "SummaryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SummaryDefect");

            migrationBuilder.DropTable(
                name: "Summary");

            migrationBuilder.CreateTable(
                name: "BoundarySummary",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BoundarySummaryName = table.Column<string>(type: "TEXT", nullable: false),
                    GPSLatitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSLongitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSTrackAngle = table.Column<double>(type: "REAL", nullable: false),
                    GeoJSON = table.Column<string>(type: "TEXT", nullable: false),
                    NumericField = table.Column<string>(type: "TEXT", nullable: false),
                    Operation = table.Column<string>(type: "TEXT", nullable: false),
                    SampleUnitBoundaryId = table.Column<int>(type: "INTEGER", nullable: false),
                    SampleUnitBoundaryName = table.Column<string>(type: "TEXT", nullable: false),
                    SampleUnitSetName = table.Column<string>(type: "TEXT", nullable: false),
                    SurveyId = table.Column<string>(type: "TEXT", nullable: false),
                    TableName = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoundarySummary", x => x.Id);
                });
        }
    }
}
