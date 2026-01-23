using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DataView2.GrpcService.Migrations.Metadata
{
    /// <inheritdoc />
    public partial class addedCrackSummaryGraphics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "MapGraphic",
                columns: new[] { "Id", "Alpha", "Blue", "Green", "Name", "Red", "SymbolType", "Thickness" },
                values: new object[,]
                {
                    { 48, 255, 255, 255, "Crack Summary0", 64, "Line", 3.0 },
                    { 49, 255, 0, 192, "Crack Summary1", 0, "Line", 3.0 },
                    { 50, 255, 15, 151, "Crack Summary2", 255, "Line", 3.0 },
                    { 51, 255, 0, 0, "Crack Summary3", 255, "Line", 3.0 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 48);

            migrationBuilder.DeleteData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 49);

            migrationBuilder.DeleteData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 50);

            migrationBuilder.DeleteData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 51);
        }
    }
}
