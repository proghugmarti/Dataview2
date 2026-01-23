using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataView2.GrpcService.Migrations.Project
{
    /// <inheritdoc />
    public partial class addedPCIRatingDefect : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PCIDefect",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    PCIRatingType = table.Column<string>(type: "TEXT", nullable: false),
                    Surface = table.Column<string>(type: "TEXT", nullable: false),
                    UnitOfMeasure = table.Column<string>(type: "TEXT", nullable: false),
                    LowSeverityDescription = table.Column<string>(type: "TEXT", nullable: false),
                    MediumSeverityDescription = table.Column<string>(type: "TEXT", nullable: false),
                    HighSeverityDescription = table.Column<string>(type: "TEXT", nullable: false),
                    PotentialEffectOnPCIDeduct = table.Column<string>(type: "TEXT", nullable: false),
                    AutomaticOrManual = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PCIDefect", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PCIRating",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    Score = table.Column<double>(type: "REAL", nullable: false),
                    Surface = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PCIRating", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PCIDefect");

            migrationBuilder.DropTable(
                name: "PCIRating");
        }
    }
}
