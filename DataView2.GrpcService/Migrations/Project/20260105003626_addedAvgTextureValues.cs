using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataView2.GrpcService.Migrations.Project
{
    /// <inheritdoc />
    public partial class addedAvgTextureValues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AvgGeoJSON",
                table: "LCMS_Texture_Processed",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "AvgMPD",
                table: "LCMS_Texture_Processed",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "AvgMTD",
                table: "LCMS_Texture_Processed",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "AvgRMS",
                table: "LCMS_Texture_Processed",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "AvgSMTD",
                table: "LCMS_Texture_Processed",
                type: "REAL",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvgGeoJSON",
                table: "LCMS_Texture_Processed");

            migrationBuilder.DropColumn(
                name: "AvgMPD",
                table: "LCMS_Texture_Processed");

            migrationBuilder.DropColumn(
                name: "AvgMTD",
                table: "LCMS_Texture_Processed");

            migrationBuilder.DropColumn(
                name: "AvgRMS",
                table: "LCMS_Texture_Processed");

            migrationBuilder.DropColumn(
                name: "AvgSMTD",
                table: "LCMS_Texture_Processed");
        }
    }
}
