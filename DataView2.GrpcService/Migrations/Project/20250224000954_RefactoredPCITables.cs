using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataView2.GrpcService.Migrations.Project
{
    /// <inheritdoc />
    public partial class RefactoredPCITables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PCIValue");

            migrationBuilder.DropTable(
                name: "Boundaries");

            migrationBuilder.DropTable(
                name: "PCIRating");

            migrationBuilder.DropTable(
                name: "SampleUnitSet");

            migrationBuilder.CreateTable(
                name: "Boundary",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SurveyId = table.Column<string>(type: "TEXT", nullable: true),
                    SurveyName = table.Column<string>(type: "TEXT", nullable: true),
                    BoundaryName = table.Column<string>(type: "TEXT", nullable: true),
                    Coordinates = table.Column<string>(type: "TEXT", nullable: false),
                    BoundariesMode = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Boundary", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SampleUnit_Set",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SampleUnit_Set", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PCIRatings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RatingName = table.Column<string>(type: "TEXT", nullable: false),
                    RaterName = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    Surface = table.Column<string>(type: "TEXT", nullable: false),
                    SampleUnitSetId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProgressPercentage = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PCIRatings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PCIRatings_SampleUnit_Set_SampleUnitSetId",
                        column: x => x.SampleUnitSetId,
                        principalTable: "SampleUnit_Set",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SampleUnit",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Area_m2 = table.Column<double>(type: "REAL", nullable: false),
                    Coordinates = table.Column<string>(type: "TEXT", nullable: false),
                    SectionId = table.Column<string>(type: "TEXT", nullable: false),
                    SampleUnitSetId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SampleUnit", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SampleUnit_SampleUnit_Set_SampleUnitSetId",
                        column: x => x.SampleUnitSetId,
                        principalTable: "SampleUnit_Set",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PCIDefects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PCIRatingId = table.Column<int>(type: "INTEGER", nullable: false),
                    SampleUnitSetId = table.Column<int>(type: "INTEGER", nullable: false),
                    SampleUnitId = table.Column<int>(type: "INTEGER", nullable: false),
                    DefectName = table.Column<string>(type: "TEXT", nullable: false),
                    Qty = table.Column<double>(type: "REAL", nullable: false),
                    Severity = table.Column<string>(type: "TEXT", nullable: false),
                    GeoJSON = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PCIDefects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PCIDefects_PCIRatings_PCIRatingId",
                        column: x => x.PCIRatingId,
                        principalTable: "PCIRatings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PCIRatingStatus",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PCIRatingId = table.Column<int>(type: "INTEGER", nullable: false),
                    SampleUnitId = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PCIRatingStatus", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PCIRatingStatus_PCIRatings_PCIRatingId",
                        column: x => x.PCIRatingId,
                        principalTable: "PCIRatings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PCIDefects_PCIRatingId",
                table: "PCIDefects",
                column: "PCIRatingId");

            migrationBuilder.CreateIndex(
                name: "IX_PCIRatings_SampleUnitSetId",
                table: "PCIRatings",
                column: "SampleUnitSetId");

            migrationBuilder.CreateIndex(
                name: "IX_PCIRatingStatus_PCIRatingId",
                table: "PCIRatingStatus",
                column: "PCIRatingId");

            migrationBuilder.CreateIndex(
                name: "IX_SampleUnit_SampleUnitSetId",
                table: "SampleUnit",
                column: "SampleUnitSetId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Boundary");

            migrationBuilder.DropTable(
                name: "PCIDefects");

            migrationBuilder.DropTable(
                name: "PCIRatingStatus");

            migrationBuilder.DropTable(
                name: "SampleUnit");

            migrationBuilder.DropTable(
                name: "PCIRatings");

            migrationBuilder.DropTable(
                name: "SampleUnit_Set");

            migrationBuilder.CreateTable(
                name: "SampleUnitSet",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SampleUnitSet", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Boundaries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SampleUnitSetId = table.Column<int>(type: "INTEGER", nullable: true),
                    BoundariesMode = table.Column<string>(type: "TEXT", nullable: false),
                    BoundaryName = table.Column<string>(type: "TEXT", nullable: true),
                    Coordinates = table.Column<string>(type: "TEXT", nullable: false),
                    SurveyId = table.Column<string>(type: "TEXT", nullable: true),
                    SurveyName = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Boundaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Boundaries_SampleUnitSet_SampleUnitSetId",
                        column: x => x.SampleUnitSetId,
                        principalTable: "SampleUnitSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PCIRating",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SampleUnitSetId = table.Column<int>(type: "INTEGER", nullable: false),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Surface = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PCIRating", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PCIRating_SampleUnitSet_SampleUnitSetId",
                        column: x => x.SampleUnitSetId,
                        principalTable: "SampleUnitSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PCIValue",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PCIRatingId = table.Column<int>(type: "INTEGER", nullable: false),
                    SampleUnitId = table.Column<int>(type: "INTEGER", nullable: false),
                    NumberValue = table.Column<double>(type: "REAL", nullable: true),
                    PCIDefectId = table.Column<int>(type: "INTEGER", nullable: false),
                    PCIDefectName = table.Column<string>(type: "TEXT", nullable: false),
                    SeverityValue = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PCIValue", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PCIValue_Boundaries_SampleUnitId",
                        column: x => x.SampleUnitId,
                        principalTable: "Boundaries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PCIValue_PCIRating_PCIRatingId",
                        column: x => x.PCIRatingId,
                        principalTable: "PCIRating",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Boundaries_SampleUnitSetId",
                table: "Boundaries",
                column: "SampleUnitSetId");

            migrationBuilder.CreateIndex(
                name: "IX_PCIRating_SampleUnitSetId",
                table: "PCIRating",
                column: "SampleUnitSetId");

            migrationBuilder.CreateIndex(
                name: "IX_PCIValue_PCIRatingId",
                table: "PCIValue",
                column: "PCIRatingId");

            migrationBuilder.CreateIndex(
                name: "IX_PCIValue_SampleUnitId",
                table: "PCIValue",
                column: "SampleUnitId");
        }
    }
}
