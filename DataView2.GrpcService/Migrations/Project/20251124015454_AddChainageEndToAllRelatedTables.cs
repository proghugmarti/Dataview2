using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataView2.GrpcService.Migrations.Project
{
    /// <inheritdoc />
    public partial class AddChainageEndToAllRelatedTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "ChainageEnd",
                table: "LCMS_Water_Entrapment",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ChainageEnd",
                table: "LCMS_Texture_Processed",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ChainageEnd",
                table: "LCMS_Spalling_Raw",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ChainageEnd",
                table: "LCMS_Shove_Processed",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ChainageEnd",
                table: "LCMS_Segment",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ChainageEnd",
                table: "LCMS_Sealed_Cracks",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ChainageEnd",
                table: "LCMS_Sags_Bumps",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ChainageEnd",
                table: "LCMS_Rut_Processed",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ChainageEnd",
                table: "LCMS_Rumble_Strip",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ChainageEnd",
                table: "LCMS_Rough_Processed",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ChainageEnd",
                table: "LCMS_Ravelling_Raw",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ChainageEnd",
                table: "LCMS_Pumping_Processed",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ChainageEnd",
                table: "LCMS_Potholes_Processed",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ChainageEnd",
                table: "LCMS_PickOuts_Raw",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ChainageEnd",
                table: "LCMS_PCI",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ChainageEnd",
                table: "LCMS_Patch_Processed",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ChainageEnd",
                table: "LCMS_PASER",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ChainageEnd",
                table: "LCMS_MMO_Processed",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ChainageEnd",
                table: "LCMS_Marking_Contour",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ChainageEnd",
                table: "LCMS_Lane_Mark_Processed",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ChainageEnd",
                table: "LCMS_Grooves",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ChainageEnd",
                table: "LCMS_Geometry_Processed",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ChainageEnd",
                table: "LCMS_Curb_DropOff",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ChainageEnd",
                table: "LCMS_CrackSummary",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ChainageEnd",
                table: "LCMS_Cracking_Raw",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ChainageEnd",
                table: "LCMS_Corner_Break",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ChainageEnd",
                table: "LCMS_Concrete_Joints",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ChainageEnd",
                table: "LCMS_Bleeding",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChainageEnd",
                table: "LCMS_Water_Entrapment");

            migrationBuilder.DropColumn(
                name: "ChainageEnd",
                table: "LCMS_Texture_Processed");

            migrationBuilder.DropColumn(
                name: "ChainageEnd",
                table: "LCMS_Spalling_Raw");

            migrationBuilder.DropColumn(
                name: "ChainageEnd",
                table: "LCMS_Shove_Processed");

            migrationBuilder.DropColumn(
                name: "ChainageEnd",
                table: "LCMS_Segment");

            migrationBuilder.DropColumn(
                name: "ChainageEnd",
                table: "LCMS_Sealed_Cracks");

            migrationBuilder.DropColumn(
                name: "ChainageEnd",
                table: "LCMS_Sags_Bumps");

            migrationBuilder.DropColumn(
                name: "ChainageEnd",
                table: "LCMS_Rut_Processed");

            migrationBuilder.DropColumn(
                name: "ChainageEnd",
                table: "LCMS_Rumble_Strip");

            migrationBuilder.DropColumn(
                name: "ChainageEnd",
                table: "LCMS_Rough_Processed");

            migrationBuilder.DropColumn(
                name: "ChainageEnd",
                table: "LCMS_Ravelling_Raw");

            migrationBuilder.DropColumn(
                name: "ChainageEnd",
                table: "LCMS_Pumping_Processed");

            migrationBuilder.DropColumn(
                name: "ChainageEnd",
                table: "LCMS_Potholes_Processed");

            migrationBuilder.DropColumn(
                name: "ChainageEnd",
                table: "LCMS_PickOuts_Raw");

            migrationBuilder.DropColumn(
                name: "ChainageEnd",
                table: "LCMS_PCI");

            migrationBuilder.DropColumn(
                name: "ChainageEnd",
                table: "LCMS_Patch_Processed");

            migrationBuilder.DropColumn(
                name: "ChainageEnd",
                table: "LCMS_PASER");

            migrationBuilder.DropColumn(
                name: "ChainageEnd",
                table: "LCMS_MMO_Processed");

            migrationBuilder.DropColumn(
                name: "ChainageEnd",
                table: "LCMS_Marking_Contour");

            migrationBuilder.DropColumn(
                name: "ChainageEnd",
                table: "LCMS_Lane_Mark_Processed");

            migrationBuilder.DropColumn(
                name: "ChainageEnd",
                table: "LCMS_Grooves");

            migrationBuilder.DropColumn(
                name: "ChainageEnd",
                table: "LCMS_Geometry_Processed");

            migrationBuilder.DropColumn(
                name: "ChainageEnd",
                table: "LCMS_Curb_DropOff");

            migrationBuilder.DropColumn(
                name: "ChainageEnd",
                table: "LCMS_CrackSummary");

            migrationBuilder.DropColumn(
                name: "ChainageEnd",
                table: "LCMS_Cracking_Raw");

            migrationBuilder.DropColumn(
                name: "ChainageEnd",
                table: "LCMS_Corner_Break");

            migrationBuilder.DropColumn(
                name: "ChainageEnd",
                table: "LCMS_Concrete_Joints");

            migrationBuilder.DropColumn(
                name: "ChainageEnd",
                table: "LCMS_Bleeding");
        }
    }
}
