using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataView2.GrpcService.Migrations
{
    /// <inheritdoc />
    public partial class UpdColLCMSSegmentGrid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
          migrationBuilder.DropColumn(
          name: "SegmentGridID",
          table: "LCMS_Segment_Grid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
           migrationBuilder.AddColumn<string>(
           name: "SegmentGridID",
           table: "LCMS_Segment_Grid",
           type: "TEXT",
           nullable: false,
           defaultValue: "");
        }
    }
}
