using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DataView2.GrpcService.Migrations.Metadata
{
    /// <inheritdoc />
    public partial class AddedMoreMapGraphic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "MapGraphic",
                columns: new[] { "Id", "Alpha", "Blue", "Green", "Name", "Red", "SymbolType", "Thickness" },
                values: new object[,]
                {
                    { 34, 128, 100, 50, "Geometry", 0, "Line", 3.0 },
                    { 35, 128, 0, 255, "Sags Bumps", 0, "Fill", 1.0 },
                    { 36, 128, 255, 0, "Water Entrapment", 0, "Fill", 1.0 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 34);

            migrationBuilder.DeleteData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 35);

            migrationBuilder.DeleteData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 36);
        }
    }
}
