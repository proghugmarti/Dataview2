using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataView2.GrpcService.Migrations
{
    /// <inheritdoc />
    public partial class addedBoundaryName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_LCMS_Spalling_Raws",
                table: "LCMS_Spalling_Raws");

            migrationBuilder.DropPrimaryKey(
                name: "PK_LCMS_PickOuts",
                table: "LCMS_PickOuts");

            migrationBuilder.RenameTable(
                name: "LCMS_Spalling_Raws",
                newName: "LCMS_Spalling_Raw");

            migrationBuilder.RenameTable(
                name: "LCMS_PickOuts",
                newName: "LCMS_PickOuts_Raw");

            migrationBuilder.RenameColumn(
                name: "Chaingage",
                table: "LCMS_Marking_Contour",
                newName: "Chainage");

            migrationBuilder.AlterColumn<string>(
                name: "SurveyName",
                table: "Boundaries",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "SurveyId",
                table: "Boundaries",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddColumn<string>(
                name: "BoundaryName",
                table: "Boundaries",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_LCMS_Spalling_Raw",
                table: "LCMS_Spalling_Raw",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_LCMS_PickOuts_Raw",
                table: "LCMS_PickOuts_Raw",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_LCMS_Spalling_Raw",
                table: "LCMS_Spalling_Raw");

            migrationBuilder.DropPrimaryKey(
                name: "PK_LCMS_PickOuts_Raw",
                table: "LCMS_PickOuts_Raw");

            migrationBuilder.DropColumn(
                name: "BoundaryName",
                table: "Boundaries");

            migrationBuilder.RenameTable(
                name: "LCMS_Spalling_Raw",
                newName: "LCMS_Spalling_Raws");

            migrationBuilder.RenameTable(
                name: "LCMS_PickOuts_Raw",
                newName: "LCMS_PickOuts");

            migrationBuilder.RenameColumn(
                name: "Chainage",
                table: "LCMS_Marking_Contour",
                newName: "Chaingage");

            migrationBuilder.AlterColumn<string>(
                name: "SurveyName",
                table: "Boundaries",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SurveyId",
                table: "Boundaries",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_LCMS_Spalling_Raws",
                table: "LCMS_Spalling_Raws",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_LCMS_PickOuts",
                table: "LCMS_PickOuts",
                column: "Id");
        }
    }
}
