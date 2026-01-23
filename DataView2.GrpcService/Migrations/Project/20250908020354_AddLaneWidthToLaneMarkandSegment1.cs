using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataView2.GrpcService.Migrations.Project
{
    /// <inheritdoc />
    public partial class AddLaneWidthToLaneMarkandSegment1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "LaneWidth",
                table: "LCMS_Segment",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "LaneWidth",
                table: "LCMS_Lane_Mark_Processed",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LaneWidth",
                table: "LCMS_Segment");

            migrationBuilder.DropColumn(
                name: "LaneWidth",
                table: "LCMS_Lane_Mark_Processed");
        }
    }
}
