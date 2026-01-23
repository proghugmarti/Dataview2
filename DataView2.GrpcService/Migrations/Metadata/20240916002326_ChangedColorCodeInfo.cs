using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DataView2.GrpcService.Migrations.Metadata
{
    /// <inheritdoc />
    public partial class ChangedColorCodeInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Severity",
                table: "ColorCodeInformation",
                newName: "StringProperty");

            migrationBuilder.AddColumn<bool>(
                name: "IsStringProperty",
                table: "ColorCodeInformation",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.InsertData(
                table: "MapGraphic",
                columns: new[] { "Id", "Alpha", "Blue", "Green", "Name", "Red", "SymbolType", "Thickness" },
                values: new object[,]
                {
                    { 17, 255, 40, 253, "Dropoff", 0, "Line", 3.0 },
                    { 18, 255, 250, 125, "Curb", 225, "Line", 3.0 },
                    { 19, 170, 0, 255, "Marking Contour", 255, "Fill", 1.0 },
                    { 20, 255, 83, 22, "Sealed Crack", 191, "Line", 5.0 },
                    { 21, 255, 0, 255, "Lwp IRI", 255, "Line", 5.0 },
                    { 22, 255, 0, 255, "Rwp IRI", 255, "Line", 5.0 },
                    { 23, 255, 0, 255, "Cwp IRI", 255, "Line", 5.0 },
                    { 24, 255, 226, 43, "Lane IRI", 138, "Line", 5.0 },
                    { 25, 170, 0, 255, "Rutting", 255, "Fill", 1.0 },
                    { 26, 255, 17, 247, "MMO", 17, "FillLine", 5.0 },
                    { 27, 255, 17, 247, "Pumping", 17, "FillLine", 5.0 },
                    { 28, 170, 0, 0, "Shove", 255, "Fill", 1.0 },
                    { 29, 255, 0, 0, "Rumble Strip", 255, "FillLine", 5.0 },
                    { 30, 170, 0, 255, "Bleeding1", 255, "Fill", 1.0 },
                    { 31, 170, 0, 165, "Bleeding2", 255, "Fill", 1.0 },
                    { 32, 170, 0, 0, "Bleeding3", 255, "Fill", 1.0 },
                    { 33, 128, 255, 128, "Macro Texture", 255, "Fill", 1.0 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 24);

            migrationBuilder.DeleteData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 25);

            migrationBuilder.DeleteData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 26);

            migrationBuilder.DeleteData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 27);

            migrationBuilder.DeleteData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 28);

            migrationBuilder.DeleteData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 29);

            migrationBuilder.DeleteData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 30);

            migrationBuilder.DeleteData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 31);

            migrationBuilder.DeleteData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 32);

            migrationBuilder.DeleteData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 33);

            migrationBuilder.DropColumn(
                name: "IsStringProperty",
                table: "ColorCodeInformation");

            migrationBuilder.RenameColumn(
                name: "StringProperty",
                table: "ColorCodeInformation",
                newName: "Severity");
        }
    }
}
