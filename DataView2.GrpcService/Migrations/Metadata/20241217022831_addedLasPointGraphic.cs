using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataView2.GrpcService.Migrations.Metadata
{
    /// <inheritdoc />
    public partial class addedLasPointGraphic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "MapGraphic",
                columns: new[] { "Id", "Alpha", "Blue", "Green", "Name", "Red", "SymbolType", "Thickness" },
                values: new object[] { 45, 170, 0, 165, "LasPoints", 255, "Fill", 1.0 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 45);
        }
    }
}
