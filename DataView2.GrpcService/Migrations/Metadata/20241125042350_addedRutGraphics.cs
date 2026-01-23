using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DataView2.GrpcService.Migrations.Metadata
{
    /// <inheritdoc />
    public partial class addedRutGraphics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 25,
                columns: new[] { "Alpha", "Blue", "Green", "Name", "Red", "SymbolType", "Thickness" },
                values: new object[] { 255, 17, 247, "MMO", 17, "FillLine", 5.0 });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 26,
                column: "Name",
                value: "Pumping");

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 27,
                columns: new[] { "Alpha", "Blue", "Green", "Name", "Red", "SymbolType", "Thickness" },
                values: new object[] { 170, 0, 0, "Shove", 255, "Fill", 1.0 });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 28,
                columns: new[] { "Alpha", "Name", "SymbolType", "Thickness" },
                values: new object[] { 255, "Rumble Strip", "FillLine", 5.0 });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 29,
                columns: new[] { "Alpha", "Green", "Name", "SymbolType", "Thickness" },
                values: new object[] { 170, 255, "Bleeding1", "Fill", 1.0 });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 30,
                columns: new[] { "Green", "Name" },
                values: new object[] { 165, "Bleeding2" });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 31,
                columns: new[] { "Green", "Name" },
                values: new object[] { 0, "Bleeding3" });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 32,
                columns: new[] { "Alpha", "Blue", "Green", "Name" },
                values: new object[] { 128, 255, 128, "Macro Texture" });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 33,
                columns: new[] { "Blue", "Green", "Name", "Red", "SymbolType", "Thickness" },
                values: new object[] { 100, 50, "Geometry", 0, "Line", 3.0 });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 34,
                columns: new[] { "Blue", "Green", "Name", "SymbolType", "Thickness" },
                values: new object[] { 0, 255, "Sags Bumps", "Fill", 1.0 });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 35,
                columns: new[] { "Blue", "Green", "Name" },
                values: new object[] { 255, 0, "Water Entrapment" });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 36,
                columns: new[] { "Alpha", "Blue", "Green", "Name", "Red" },
                values: new object[] { 170, 35, 26, "LasPoint", 168 });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 37,
                columns: new[] { "Alpha", "Blue", "Green", "Name", "Red", "SymbolType", "Thickness" },
                values: new object[] { 255, 0, 255, "Left Rut", 255, "Line", 5.0 });

            migrationBuilder.InsertData(
                table: "MapGraphic",
                columns: new[] { "Id", "Alpha", "Blue", "Green", "Name", "Red", "SymbolType", "Thickness" },
                values: new object[,]
                {
                    { 38, 255, 0, 255, "Right Rut", 255, "Line", 5.0 },
                    { 39, 255, 0, 255, "Lane Rut", 255, "Line", 5.0 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 38);

            migrationBuilder.DeleteData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 39);

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 25,
                columns: new[] { "Alpha", "Blue", "Green", "Name", "Red", "SymbolType", "Thickness" },
                values: new object[] { 170, 0, 255, "Rutting", 255, "Fill", 1.0 });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 26,
                column: "Name",
                value: "MMO");

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 27,
                columns: new[] { "Alpha", "Blue", "Green", "Name", "Red", "SymbolType", "Thickness" },
                values: new object[] { 255, 17, 247, "Pumping", 17, "FillLine", 5.0 });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 28,
                columns: new[] { "Alpha", "Name", "SymbolType", "Thickness" },
                values: new object[] { 170, "Shove", "Fill", 1.0 });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 29,
                columns: new[] { "Alpha", "Green", "Name", "SymbolType", "Thickness" },
                values: new object[] { 255, 0, "Rumble Strip", "FillLine", 5.0 });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 30,
                columns: new[] { "Green", "Name" },
                values: new object[] { 255, "Bleeding1" });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 31,
                columns: new[] { "Green", "Name" },
                values: new object[] { 165, "Bleeding2" });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 32,
                columns: new[] { "Alpha", "Blue", "Green", "Name" },
                values: new object[] { 170, 0, 0, "Bleeding3" });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 33,
                columns: new[] { "Blue", "Green", "Name", "Red", "SymbolType", "Thickness" },
                values: new object[] { 255, 128, "Macro Texture", 255, "Fill", 1.0 });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 34,
                columns: new[] { "Blue", "Green", "Name", "SymbolType", "Thickness" },
                values: new object[] { 100, 50, "Geometry", "Line", 3.0 });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 35,
                columns: new[] { "Blue", "Green", "Name" },
                values: new object[] { 0, 255, "Sags Bumps" });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 36,
                columns: new[] { "Alpha", "Blue", "Green", "Name", "Red" },
                values: new object[] { 128, 255, 0, "Water Entrapment", 0 });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 37,
                columns: new[] { "Alpha", "Blue", "Green", "Name", "Red", "SymbolType", "Thickness" },
                values: new object[] { 170, 35, 26, "LasPoint", 168, "Fill", 1.0 });
        }
    }
}
