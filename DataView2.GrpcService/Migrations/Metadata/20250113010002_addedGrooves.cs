using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataView2.GrpcService.Migrations.Metadata
{
    /// <inheritdoc />
    public partial class addedGrooves : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 40,
                column: "Name",
                value: "Segment Grid5");

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 45,
                columns: new[] { "Green", "Name", "Red" },
                values: new object[] { 0, "Segment Grid4", 200 });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 46,
                columns: new[] { "Green", "Name", "Red" },
                values: new object[] { 165, "LasPoints", 255 });

            migrationBuilder.InsertData(
                table: "MapGraphic",
                columns: new[] { "Id", "Alpha", "Blue", "Green", "Name", "Red", "SymbolType", "Thickness" },
                values: new object[] { 47, 170, 255, 255, "Grooves", 255, "Fill", 1.0 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 47);

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 40,
                column: "Name",
                value: "Segment Grid4");

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 45,
                columns: new[] { "Green", "Name", "Red" },
                values: new object[] { 165, "LasPoints", 255 });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 46,
                columns: new[] { "Green", "Name", "Red" },
                values: new object[] { 0, "Segment Grid3", 200 });
        }
    }
}
