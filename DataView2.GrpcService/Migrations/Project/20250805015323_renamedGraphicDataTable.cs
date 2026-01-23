using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataView2.GrpcService.Migrations.Project
{
    /// <inheritdoc />
    public partial class renamedGraphicDataTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "ShapefileGraphicData",
                newName: "DatasetGraphicData");

            migrationBuilder.DropColumn(
                name: "IsPCCPavement",
                table: "SampleUnit_Set");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                 name: "DatasetGraphicData",
                 newName: "ShapefileGraphicData");

            migrationBuilder.AddColumn<bool>(
                name: "IsPCCPavement",
                table: "SampleUnit_Set",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }
    }
}
