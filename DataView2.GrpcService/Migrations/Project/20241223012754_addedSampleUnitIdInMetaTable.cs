using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataView2.GrpcService.Migrations.Project
{
    /// <inheritdoc />
    public partial class addedSampleUnitIdInMetaTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SampleUnitBoundaryId",
                table: "MetaTableValue",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SampleUnitBoundaryId",
                table: "MetaTableValue");
        }
    }
}
