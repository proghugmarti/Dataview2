using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataView2.GrpcService.Migrations.Project
{
    /// <inheritdoc />
    public partial class addedChainageInMetaTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Chainage",
                table: "MetaTableValue",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageFileIndex",
                table: "MetaTableValue",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "LRPNumber",
                table: "MetaTableValue",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Chainage",
                table: "MetaTableValue");

            migrationBuilder.DropColumn(
                name: "ImageFileIndex",
                table: "MetaTableValue");

            migrationBuilder.DropColumn(
                name: "LRPNumber",
                table: "MetaTableValue");
        }
    }
}
