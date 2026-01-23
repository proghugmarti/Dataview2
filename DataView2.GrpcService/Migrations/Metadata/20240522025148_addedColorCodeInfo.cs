using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DataView2.GrpcService.Migrations.Metadata
{
    /// <inheritdoc />
    public partial class addedColorCodeInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ColorCodeInformation",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    tableName = table.Column<string>(type: "TEXT", nullable: false),
                    property = table.Column<string>(type: "TEXT", nullable: false),
                    minRange = table.Column<double>(type: "REAL", nullable: false),
                    maxRange = table.Column<double>(type: "REAL", nullable: false),
                    hexColor = table.Column<string>(type: "TEXT", nullable: false),
                    thickness = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ColorCodeInformation", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "MapGraphic",
                columns: new[] { "Id", "Alpha", "Blue", "Green", "Name", "Red", "SymbolType", "Thickness" },
                values: new object[,]
                {
                    { 15, 50, 0, 0, "Segment", 0, "Segment", 1.0 },
                    { 16, 255, 255, 0, "HighlightedSegment", 191, "Segment", 1.0 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ColorCodeInformation");

            migrationBuilder.DeleteData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 16);
        }
    }
}
