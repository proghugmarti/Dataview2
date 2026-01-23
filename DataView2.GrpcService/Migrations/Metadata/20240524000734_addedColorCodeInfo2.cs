using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataView2.GrpcService.Migrations.Metadata
{
    /// <inheritdoc />
    public partial class addedColorCodeInfo2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "thickness",
                table: "ColorCodeInformation",
                newName: "Thickness");

            migrationBuilder.RenameColumn(
                name: "tableName",
                table: "ColorCodeInformation",
                newName: "TableName");

            migrationBuilder.RenameColumn(
                name: "property",
                table: "ColorCodeInformation",
                newName: "Property");

            migrationBuilder.RenameColumn(
                name: "minRange",
                table: "ColorCodeInformation",
                newName: "MinRange");

            migrationBuilder.RenameColumn(
                name: "maxRange",
                table: "ColorCodeInformation",
                newName: "MaxRange");

            migrationBuilder.RenameColumn(
                name: "hexColor",
                table: "ColorCodeInformation",
                newName: "HexColor");

            migrationBuilder.AddColumn<bool>(
                name: "IsAboveFrom",
                table: "ColorCodeInformation",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Severity",
                table: "ColorCodeInformation",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAboveFrom",
                table: "ColorCodeInformation");

            migrationBuilder.DropColumn(
                name: "Severity",
                table: "ColorCodeInformation");

            migrationBuilder.RenameColumn(
                name: "Thickness",
                table: "ColorCodeInformation",
                newName: "thickness");

            migrationBuilder.RenameColumn(
                name: "TableName",
                table: "ColorCodeInformation",
                newName: "tableName");

            migrationBuilder.RenameColumn(
                name: "Property",
                table: "ColorCodeInformation",
                newName: "property");

            migrationBuilder.RenameColumn(
                name: "MinRange",
                table: "ColorCodeInformation",
                newName: "minRange");

            migrationBuilder.RenameColumn(
                name: "MaxRange",
                table: "ColorCodeInformation",
                newName: "maxRange");

            migrationBuilder.RenameColumn(
                name: "HexColor",
                table: "ColorCodeInformation",
                newName: "hexColor");
        }
    }
}
