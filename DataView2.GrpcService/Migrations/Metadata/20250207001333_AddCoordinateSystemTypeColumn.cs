using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataView2.GrpcService.Migrations.Metadata
{
    /// <inheritdoc />
    public partial class AddCoordinateSystemTypeColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CoordinateSystemType",
                table: "GeneralSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "GeneralSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "CoordinateSystemType",
                value: "");

            migrationBuilder.UpdateData(
                table: "GeneralSettings",
                keyColumn: "Id",
                keyValue: 2,
                column: "CoordinateSystemType",
                value: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CoordinateSystemType",
                table: "GeneralSettings");
        }
    }
}
