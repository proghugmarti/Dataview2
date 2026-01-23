using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataView2.GrpcService.Migrations.Metadata
{
    /// <inheritdoc />
    public partial class removedGeoJSONInProject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "GeoJSON",
                table: "ProjectRegistries",
                newName: "FolderPath");

            migrationBuilder.AddColumn<string>(
                name: "DBPath",
                table: "ProjectRegistries",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DBPath",
                table: "ProjectRegistries");

            migrationBuilder.RenameColumn(
                name: "FolderPath",
                table: "ProjectRegistries",
                newName: "GeoJSON");
        }
    }
}
