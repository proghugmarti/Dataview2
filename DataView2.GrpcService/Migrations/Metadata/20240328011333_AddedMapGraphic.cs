using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DataView2.GrpcService.Migrations.Metadata
{
    /// <inheritdoc />
    public partial class AddedMapGraphic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "GeneralSettings",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.CreateTable(
                name: "MapGraphic",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Red = table.Column<int>(type: "INTEGER", nullable: false),
                    Green = table.Column<int>(type: "INTEGER", nullable: false),
                    Blue = table.Column<int>(type: "INTEGER", nullable: false),
                    Alpha = table.Column<int>(type: "INTEGER", nullable: false),
                    Thickness = table.Column<double>(type: "REAL", nullable: false),
                    SymbolType = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MapGraphic", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "MapGraphic",
                columns: new[] { "Id", "Alpha", "Blue", "Green", "Name", "Red", "SymbolType", "Thickness" },
                values: new object[,]
                {
                    { 1, 255, 255, 255, "CrackingRaw0", 64, "Line", 1.0 },
                    { 2, 255, 0, 192, "CrackingRaw1", 0, "Line", 1.0 },
                    { 3, 255, 15, 151, "CrackingRaw2", 255, "Line", 1.0 },
                    { 4, 255, 0, 0, "CrackingRaw3", 255, "Line", 1.0 },
                    { 5, 170, 207, 159, "RavellingRaw1", 46, "Fill", 1.0 },
                    { 6, 170, 176, 105, "RavellingRaw2", 53, "Fill", 1.0 },
                    { 7, 170, 83, 0, "RavellingRaw3", 191, "Fill", 1.0 },
                    { 8, 170, 35, 26, "PickOutRaw", 168, "Fill", 1.0 },
                    { 9, 170, 110, 70, "PotholesProcessed", 44, "Fill", 1.0 },
                    { 10, 170, 150, 50, "PatchProcessed", 150, "Fill", 1.0 },
                    { 11, 170, 0, 255, "SpallingRaw", 255, "Fill", 1.0 },
                    { 12, 170, 0, 88, "CornerBreak", 255, "Fill", 1.0 },
                    { 13, 255, 225, 105, "ConcreteJoint", 65, "Line", 1.0 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MapGraphic");

            migrationBuilder.InsertData(
                table: "GeneralSettings",
                columns: new[] { "Id", "Category", "Description", "GPSLatitude", "GPSLongitude", "GeoJSON", "Name", "RoundedGPSLatitude", "RoundedGPSLongitude", "Type", "Value" },
                values: new object[] { 2, "Path", "Offline Map Path", 0.0, 0.0, "DefaultGeoJSON", "Offline Map Path", 0.0, 0.0, 2, "D:\\Khushbu_Badheka\\Dataview3\\DataView2\\DataView2.GrpcService\\offlineMap.vtpk" });
        }
    }
}
