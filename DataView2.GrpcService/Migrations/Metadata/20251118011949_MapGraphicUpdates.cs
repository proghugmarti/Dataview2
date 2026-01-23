using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataView2.GrpcService.Migrations.Metadata
{
    /// <inheritdoc />
    public partial class MapGraphicUpdates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 8,
                column: "Thickness",
                value: 3.0);

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 14,
                column: "Thickness",
                value: 10.0);

            migrationBuilder.InsertData(
                table: "MapGraphic",
                columns: new[] { "Id", "Alpha", "Blue", "Green", "LabelProperty", "Name", "Red", "SymbolType", "Thickness" },
                values: new object[] { 36, 255, 0, 0, "No Label", "Keycode", 255, "Line", 3.0 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 36);

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 8,
                column: "Thickness",
                value: 1.0);

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 14,
                column: "Thickness",
                value: 5.0);
        }
    }
}
