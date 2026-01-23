using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataView2.GrpcService.Migrations.Project
{
    /// <inheritdoc />
    public partial class addedPCIValue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PCIDefect");

            migrationBuilder.AddColumn<int>(
                name: "SampleUnitSetId",
                table: "PCIRating",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "PCIValue",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PCIRatingId = table.Column<int>(type: "INTEGER", nullable: false),
                    PCIDefectId = table.Column<int>(type: "INTEGER", nullable: false),
                    PCIDefectName = table.Column<string>(type: "TEXT", nullable: false),
                    SampleUnitId = table.Column<int>(type: "INTEGER", nullable: false),
                    SeverityValue = table.Column<string>(type: "TEXT", nullable: true),
                    NumberValue = table.Column<double>(type: "REAL", nullable: true)
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

            migrationBuilder.AddForeignKey(
                name: "FK_PCIRating_SampleUnitSet_SampleUnitSetId",
                table: "PCIRating",
                column: "SampleUnitSetId",
                principalTable: "SampleUnitSet",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PCIRating_SampleUnitSet_SampleUnitSetId",
                table: "PCIRating");

            migrationBuilder.DropTable(
                name: "PCIValue");

            migrationBuilder.DropIndex(
                name: "IX_PCIRating_SampleUnitSetId",
                table: "PCIRating");

            migrationBuilder.DropColumn(
                name: "SampleUnitSetId",
                table: "PCIRating");

            migrationBuilder.AddColumn<double>(
                name: "Score",
                table: "PCIRating",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.CreateTable(
                name: "PCIDefect",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AutomaticOrManual = table.Column<string>(type: "TEXT", nullable: false),
                    HighSeverityDescription = table.Column<string>(type: "TEXT", nullable: false),
                    LowSeverityDescription = table.Column<string>(type: "TEXT", nullable: false),
                    MediumSeverityDescription = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    PCIRatingType = table.Column<string>(type: "TEXT", nullable: false),
                    PotentialEffectOnPCIDeduct = table.Column<string>(type: "TEXT", nullable: false),
                    Surface = table.Column<string>(type: "TEXT", nullable: false),
                    UnitOfMeasure = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PCIDefect", x => x.Id);
                });
        }
    }
}
