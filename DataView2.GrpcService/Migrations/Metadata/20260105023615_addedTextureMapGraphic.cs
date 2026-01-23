using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataView2.GrpcService.Migrations.Metadata
{
    /// <inheritdoc />
    public partial class addedTextureMapGraphic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 24,
                column: "Name",
                value: "Band Texture");

            migrationBuilder.InsertData(
                table: "MapGraphic",
                columns: new[] { "Id", "Alpha", "Blue", "Green", "LabelProperty", "Name", "Red", "SymbolType", "Thickness" },
                values: new object[] { 38, 128, 0, 0, "No Label", "Average Texture", 255, "Fill", 1.0 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 38);

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 24,
                column: "Name",
                value: "Macro Texture");
        }
    }
}
