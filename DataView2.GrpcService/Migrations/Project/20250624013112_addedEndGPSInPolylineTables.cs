using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataView2.GrpcService.Migrations.Project
{
    /// <inheritdoc />
    public partial class addedEndGPSInPolylineTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Direction",
                table: "SurveySegmentation",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "EndChainage",
                table: "SurveySegmentation",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "StartChainage",
                table: "SurveySegmentation",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AlterColumn<string>(
                name: "ImageFileIndex",
                table: "MetaTableValue",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddColumn<double>(
                name: "Chainage",
                table: "LCMS_Segment",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "EndGPSLatitude",
                table: "LCMS_Sealed_Cracks",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "EndGPSLongitude",
                table: "LCMS_Sealed_Cracks",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "EndGPSLatitude",
                table: "LCMS_Rut_Processed",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "EndGPSLongitude",
                table: "LCMS_Rut_Processed",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "EndGPSLatitude",
                table: "LCMS_Rough_Processed",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "EndGPSLongitude",
                table: "LCMS_Rough_Processed",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "EndGPSLatitude",
                table: "LCMS_Geometry_Processed",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "EndGPSLongitude",
                table: "LCMS_Geometry_Processed",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "EndGPSLatitude",
                table: "LCMS_Curb_DropOff",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "EndGPSLongitude",
                table: "LCMS_Curb_DropOff",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "EndGPSLatitude",
                table: "LCMS_CrackSummary",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "EndGPSLongitude",
                table: "LCMS_CrackSummary",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "EndGPSLatitude",
                table: "LCMS_Cracking_Raw",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "EndGPSLongitude",
                table: "LCMS_Cracking_Raw",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Direction",
                table: "SurveySegmentation");

            migrationBuilder.DropColumn(
                name: "EndChainage",
                table: "SurveySegmentation");

            migrationBuilder.DropColumn(
                name: "StartChainage",
                table: "SurveySegmentation");

            migrationBuilder.DropColumn(
                name: "Chainage",
                table: "LCMS_Segment");

            migrationBuilder.DropColumn(
                name: "EndGPSLatitude",
                table: "LCMS_Sealed_Cracks");

            migrationBuilder.DropColumn(
                name: "EndGPSLongitude",
                table: "LCMS_Sealed_Cracks");

            migrationBuilder.DropColumn(
                name: "EndGPSLatitude",
                table: "LCMS_Rut_Processed");

            migrationBuilder.DropColumn(
                name: "EndGPSLongitude",
                table: "LCMS_Rut_Processed");

            migrationBuilder.DropColumn(
                name: "EndGPSLatitude",
                table: "LCMS_Rough_Processed");

            migrationBuilder.DropColumn(
                name: "EndGPSLongitude",
                table: "LCMS_Rough_Processed");

            migrationBuilder.DropColumn(
                name: "EndGPSLatitude",
                table: "LCMS_Geometry_Processed");

            migrationBuilder.DropColumn(
                name: "EndGPSLongitude",
                table: "LCMS_Geometry_Processed");

            migrationBuilder.DropColumn(
                name: "EndGPSLatitude",
                table: "LCMS_Curb_DropOff");

            migrationBuilder.DropColumn(
                name: "EndGPSLongitude",
                table: "LCMS_Curb_DropOff");

            migrationBuilder.DropColumn(
                name: "EndGPSLatitude",
                table: "LCMS_CrackSummary");

            migrationBuilder.DropColumn(
                name: "EndGPSLongitude",
                table: "LCMS_CrackSummary");

            migrationBuilder.DropColumn(
                name: "EndGPSLatitude",
                table: "LCMS_Cracking_Raw");

            migrationBuilder.DropColumn(
                name: "EndGPSLongitude",
                table: "LCMS_Cracking_Raw");

            migrationBuilder.AlterColumn<string>(
                name: "ImageFileIndex",
                table: "MetaTableValue",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);
        }
    }
}
