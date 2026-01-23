using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataView2.GrpcService.Migrations.Metadata
{
    /// <inheritdoc />
    public partial class addedDefaultsInMetaTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Column10Default",
                table: "MetaTable",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Column1Default",
                table: "MetaTable",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Column2Default",
                table: "MetaTable",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Column3Default",
                table: "MetaTable",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Column4Default",
                table: "MetaTable",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Column5Default",
                table: "MetaTable",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Column6Default",
                table: "MetaTable",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Column7Default",
                table: "MetaTable",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Column8Default",
                table: "MetaTable",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Column9Default",
                table: "MetaTable",
                type: "TEXT",
                nullable: true);

            migrationBuilder.InsertData(
                table: "MapGraphic",
                columns: new[] { "Id", "Alpha", "Blue", "Green", "Name", "Red", "SymbolType", "Thickness" },
                values: new object[] { 37, 170, 35, 26, "LasPoint", 168, "Fill", 1.0 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 37);

            migrationBuilder.DropColumn(
                name: "Column10Default",
                table: "MetaTable");

            migrationBuilder.DropColumn(
                name: "Column1Default",
                table: "MetaTable");

            migrationBuilder.DropColumn(
                name: "Column2Default",
                table: "MetaTable");

            migrationBuilder.DropColumn(
                name: "Column3Default",
                table: "MetaTable");

            migrationBuilder.DropColumn(
                name: "Column4Default",
                table: "MetaTable");

            migrationBuilder.DropColumn(
                name: "Column5Default",
                table: "MetaTable");

            migrationBuilder.DropColumn(
                name: "Column6Default",
                table: "MetaTable");

            migrationBuilder.DropColumn(
                name: "Column7Default",
                table: "MetaTable");

            migrationBuilder.DropColumn(
                name: "Column8Default",
                table: "MetaTable");

            migrationBuilder.DropColumn(
                name: "Column9Default",
                table: "MetaTable");
        }
    }
}
