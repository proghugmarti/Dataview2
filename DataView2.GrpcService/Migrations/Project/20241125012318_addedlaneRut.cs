using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataView2.GrpcService.Migrations
{
    /// <inheritdoc />
    public partial class addedlaneRut : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "LaneDepth_mm",
                table: "LCMS_Rut_Processed",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "LaneWidth_mm",
                table: "LCMS_Rut_Processed",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LwpGeoJSON",
                table: "LCMS_Rut_Processed",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RwpGeoJSON",
                table: "LCMS_Rut_Processed",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LaneDepth_mm",
                table: "LCMS_Rut_Processed");

            migrationBuilder.DropColumn(
                name: "LaneWidth_mm",
                table: "LCMS_Rut_Processed");

            migrationBuilder.DropColumn(
                name: "LwpGeoJSON",
                table: "LCMS_Rut_Processed");

            migrationBuilder.DropColumn(
                name: "RwpGeoJSON",
                table: "LCMS_Rut_Processed");
        }
    }
}
