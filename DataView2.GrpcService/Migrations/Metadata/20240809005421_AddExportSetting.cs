using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataView2.GrpcService.Migrations.Metadata
{
    /// <inheritdoc />
    public partial class AddExportSetting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "GeneralSettings",
                columns: new[] { "Id", "Category", "Description", "GPSLatitude", "GPSLongitude", "GeoJSON", "Name", "RoundedGPSLatitude", "RoundedGPSLongitude", "Type", "Value" },
                values: new object[] { 5, "Networking", "URL for exporting", 0.0, 0.0, "DefaultGeoJSON", "ExportURL", 0.0, 0.0, 2, "https://dvwebservice20240808112104.azurewebsites.net" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "GeneralSettings",
                keyColumn: "Id",
                keyValue: 5);
        }
    }
}
