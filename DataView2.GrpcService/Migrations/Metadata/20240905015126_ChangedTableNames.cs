using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataView2.GrpcService.Migrations.Metadata
{
    /// <inheritdoc />
    public partial class ChangedTableNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "GeneralSettings",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.InsertData(
                table: "GeneralSettings",
                columns: new[] { "Id", "Category", "Description", "GPSLatitude", "GPSLongitude", "GeoJSON", "Name", "RoundedGPSLatitude", "RoundedGPSLongitude", "Type", "Value" },
                values: new object[] { 2, "Networking", "URL for exporting", 0.0, 0.0, "DefaultGeoJSON", "ExportURL", 0.0, 0.0, 2, "https://dvwebservice20240808112104.azurewebsites.net" });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 1,
                column: "Name",
                value: "Cracking0");

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 2,
                column: "Name",
                value: "Cracking1");

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 3,
                column: "Name",
                value: "Cracking2");

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 4,
                column: "Name",
                value: "Cracking3");

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 5,
                column: "Name",
                value: "Ravelling1");

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 6,
                column: "Name",
                value: "Ravelling2");

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 7,
                column: "Name",
                value: "Ravelling3");

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 8,
                column: "Name",
                value: "Pickout");

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 9,
                column: "Name",
                value: "Potholes");

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 10,
                column: "Name",
                value: "Patch");

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 11,
                column: "Name",
                value: "Spalling");

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 12,
                column: "Name",
                value: "Corner Break");

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 13,
                column: "Name",
                value: "Concrete Joint");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "GeneralSettings",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.InsertData(
                table: "GeneralSettings",
                columns: new[] { "Id", "Category", "Description", "GPSLatitude", "GPSLongitude", "GeoJSON", "Name", "RoundedGPSLatitude", "RoundedGPSLongitude", "Type", "Value" },
                values: new object[] { 5, "Networking", "URL for exporting", 0.0, 0.0, "DefaultGeoJSON", "ExportURL", 0.0, 0.0, 2, "https://dvwebservice20240808112104.azurewebsites.net" });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 1,
                column: "Name",
                value: "CrackingRaw0");

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 2,
                column: "Name",
                value: "CrackingRaw1");

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 3,
                column: "Name",
                value: "CrackingRaw2");

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 4,
                column: "Name",
                value: "CrackingRaw3");

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 5,
                column: "Name",
                value: "RavellingRaw1");

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 6,
                column: "Name",
                value: "RavellingRaw2");

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 7,
                column: "Name",
                value: "RavellingRaw3");

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 8,
                column: "Name",
                value: "PickOutRaw");

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 9,
                column: "Name",
                value: "PotholesProcessed");

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 10,
                column: "Name",
                value: "PatchProcessed");

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 11,
                column: "Name",
                value: "SpallingRaw");

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 12,
                column: "Name",
                value: "CornerBreak");

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 13,
                column: "Name",
                value: "ConcreteJoint");
        }
    }
}
