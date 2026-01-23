using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataView2.GrpcService.Migrations.Metadata
{
    /// <inheritdoc />
    public partial class RemovedLabelInColorCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LabelProperty",
                table: "ColorCodeInformation");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LabelProperty",
                table: "ColorCodeInformation",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "ColorCodeInformation",
                keyColumn: "Id",
                keyValue: 1,
                column: "LabelProperty",
                value: "No Label");

            migrationBuilder.UpdateData(
                table: "ColorCodeInformation",
                keyColumn: "Id",
                keyValue: 2,
                column: "LabelProperty",
                value: "No Label");

            migrationBuilder.UpdateData(
                table: "ColorCodeInformation",
                keyColumn: "Id",
                keyValue: 3,
                column: "LabelProperty",
                value: "No Label");

            migrationBuilder.UpdateData(
                table: "ColorCodeInformation",
                keyColumn: "Id",
                keyValue: 4,
                column: "LabelProperty",
                value: "No Label");

            migrationBuilder.UpdateData(
                table: "ColorCodeInformation",
                keyColumn: "Id",
                keyValue: 5,
                column: "LabelProperty",
                value: "No Label");

            migrationBuilder.UpdateData(
                table: "ColorCodeInformation",
                keyColumn: "Id",
                keyValue: 6,
                column: "LabelProperty",
                value: "No Label");

            migrationBuilder.UpdateData(
                table: "ColorCodeInformation",
                keyColumn: "Id",
                keyValue: 7,
                column: "LabelProperty",
                value: "No Label");

            migrationBuilder.UpdateData(
                table: "ColorCodeInformation",
                keyColumn: "Id",
                keyValue: 8,
                column: "LabelProperty",
                value: "No Label");

            migrationBuilder.UpdateData(
                table: "ColorCodeInformation",
                keyColumn: "Id",
                keyValue: 9,
                column: "LabelProperty",
                value: "No Label");

            migrationBuilder.UpdateData(
                table: "ColorCodeInformation",
                keyColumn: "Id",
                keyValue: 10,
                column: "LabelProperty",
                value: "No Label");

            migrationBuilder.UpdateData(
                table: "ColorCodeInformation",
                keyColumn: "Id",
                keyValue: 11,
                column: "LabelProperty",
                value: "No Label");

            migrationBuilder.UpdateData(
                table: "ColorCodeInformation",
                keyColumn: "Id",
                keyValue: 12,
                column: "LabelProperty",
                value: "No Label");

            migrationBuilder.UpdateData(
                table: "ColorCodeInformation",
                keyColumn: "Id",
                keyValue: 13,
                column: "LabelProperty",
                value: "No Label");

            migrationBuilder.UpdateData(
                table: "ColorCodeInformation",
                keyColumn: "Id",
                keyValue: 14,
                column: "LabelProperty",
                value: "No Label");

            migrationBuilder.UpdateData(
                table: "ColorCodeInformation",
                keyColumn: "Id",
                keyValue: 15,
                column: "LabelProperty",
                value: "No Label");

            migrationBuilder.UpdateData(
                table: "ColorCodeInformation",
                keyColumn: "Id",
                keyValue: 16,
                column: "LabelProperty",
                value: "No Label");

            migrationBuilder.UpdateData(
                table: "ColorCodeInformation",
                keyColumn: "Id",
                keyValue: 17,
                column: "LabelProperty",
                value: "No Label");

            migrationBuilder.UpdateData(
                table: "ColorCodeInformation",
                keyColumn: "Id",
                keyValue: 18,
                column: "LabelProperty",
                value: "No Label");

            migrationBuilder.UpdateData(
                table: "ColorCodeInformation",
                keyColumn: "Id",
                keyValue: 19,
                column: "LabelProperty",
                value: "No Label");

            migrationBuilder.UpdateData(
                table: "ColorCodeInformation",
                keyColumn: "Id",
                keyValue: 20,
                column: "LabelProperty",
                value: "No Label");

            migrationBuilder.UpdateData(
                table: "ColorCodeInformation",
                keyColumn: "Id",
                keyValue: 21,
                column: "LabelProperty",
                value: "No Label");

            migrationBuilder.UpdateData(
                table: "ColorCodeInformation",
                keyColumn: "Id",
                keyValue: 22,
                column: "LabelProperty",
                value: "No Label");
        }
    }
}
