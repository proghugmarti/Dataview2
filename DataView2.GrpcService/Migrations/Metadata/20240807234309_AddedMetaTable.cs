using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataView2.GrpcService.Migrations.Metadata
{
    /// <inheritdoc />
    public partial class AddedMetaTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MetaTable",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TableName = table.Column<string>(type: "TEXT", nullable: false),
                    GeoType = table.Column<string>(type: "TEXT", nullable: false),
                    Icon = table.Column<string>(type: "TEXT", nullable: true),
                    Column1 = table.Column<string>(type: "TEXT", nullable: true),
                    Column2 = table.Column<string>(type: "TEXT", nullable: true),
                    Column3 = table.Column<string>(type: "TEXT", nullable: true),
                    Column4 = table.Column<string>(type: "TEXT", nullable: true),
                    Column5 = table.Column<string>(type: "TEXT", nullable: true),
                    Column6 = table.Column<string>(type: "TEXT", nullable: true),
                    Column7 = table.Column<string>(type: "TEXT", nullable: true),
                    Column8 = table.Column<string>(type: "TEXT", nullable: true),
                    Column9 = table.Column<string>(type: "TEXT", nullable: true),
                    Column10 = table.Column<string>(type: "TEXT", nullable: true),
                    Column1Type = table.Column<string>(type: "TEXT", nullable: true),
                    Column2Type = table.Column<string>(type: "TEXT", nullable: true),
                    Column3Type = table.Column<string>(type: "TEXT", nullable: true),
                    Column4Type = table.Column<string>(type: "TEXT", nullable: true),
                    Column5Type = table.Column<string>(type: "TEXT", nullable: true),
                    Column6Type = table.Column<string>(type: "TEXT", nullable: true),
                    Column7Type = table.Column<string>(type: "TEXT", nullable: true),
                    Column8Type = table.Column<string>(type: "TEXT", nullable: true),
                    Column9Type = table.Column<string>(type: "TEXT", nullable: true),
                    Column10Type = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetaTable", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 1,
                column: "Thickness",
                value: 3.0);

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 2,
                column: "Thickness",
                value: 3.0);

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 3,
                column: "Thickness",
                value: 3.0);

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 4,
                column: "Thickness",
                value: 3.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MetaTable");

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 1,
                column: "Thickness",
                value: 1.0);

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 2,
                column: "Thickness",
                value: 1.0);

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 3,
                column: "Thickness",
                value: 1.0);

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 4,
                column: "Thickness",
                value: 1.0);
        }
    }
}
