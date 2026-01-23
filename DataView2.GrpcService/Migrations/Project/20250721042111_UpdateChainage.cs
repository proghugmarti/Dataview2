using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataView2.GrpcService.Migrations.Project
{
    /// <inheritdoc />
    public partial class UpdateChainage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChainageEnd",
                table: "LCMS_Rumble_Strip");

            migrationBuilder.DropColumn(
                name: "ChainageStart",
                table: "LCMS_Rumble_Strip");

            migrationBuilder.DropColumn(
                name: "ChainageEnd",
                table: "LCMS_Bleeding");

            migrationBuilder.DropColumn(
                name: "ChainageStart",
                table: "LCMS_Bleeding");

            migrationBuilder.AddColumn<double>(
                name: "Chainage",
                table: "XMLObject",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AlterColumn<double>(
                name: "Chainage",
                table: "MetaTableValue",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "REAL",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "Chainage",
                table: "LCMS_Water_Entrapment",
                type: "REAL",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<double>(
                name: "Chainage",
                table: "LCMS_Texture_Processed",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "REAL",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "Chainage",
                table: "LCMS_Spalling_Raw",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "REAL",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "Chainage",
                table: "LCMS_Shove_Processed",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "REAL",
                oldNullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Chainage",
                table: "LCMS_Segment_Grid",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AlterColumn<double>(
                name: "Chainage",
                table: "LCMS_Sealed_Cracks",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "REAL",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "Chainage",
                table: "LCMS_Sags_Bumps",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "REAL",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "Chainage",
                table: "LCMS_Rut_Processed",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "REAL",
                oldNullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Chainage",
                table: "LCMS_Rumble_Strip",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AlterColumn<double>(
                name: "Chainage",
                table: "LCMS_Rough_Processed",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "REAL",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "Chainage",
                table: "LCMS_Ravelling_Raw",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "REAL",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "Chainage",
                table: "LCMS_Pumping_Processed",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "REAL",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "Chainage",
                table: "LCMS_Potholes_Processed",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "REAL",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "Chainage",
                table: "LCMS_PickOuts_Raw",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "REAL",
                oldNullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Chainage",
                table: "LCMS_PCI",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AlterColumn<double>(
                name: "Chainage",
                table: "LCMS_Patch_Processed",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "REAL",
                oldNullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Chainage",
                table: "LCMS_PASER",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AlterColumn<double>(
                name: "Chainage",
                table: "LCMS_MMO_Processed",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "REAL",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "Chainage",
                table: "LCMS_Marking_Contour",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "REAL",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "Chainage",
                table: "LCMS_Lane_Mark_Processed",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "REAL",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "Chainage",
                table: "LCMS_Grooves",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "REAL",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "Chainage",
                table: "LCMS_Geometry_Processed",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Chainage",
                table: "LCMS_FOD",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AlterColumn<double>(
                name: "Chainage",
                table: "LCMS_Curb_DropOff",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "REAL",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "Chainage",
                table: "LCMS_CrackSummary",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "REAL",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "Chainage",
                table: "LCMS_Cracking_Raw",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "REAL",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "Chainage",
                table: "LCMS_Corner_Break",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "REAL",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "Chainage",
                table: "LCMS_Concrete_Joints",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "REAL",
                oldNullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Chainage",
                table: "LCMS_Bleeding",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Chainage",
                table: "XMLObject");

            migrationBuilder.DropColumn(
                name: "Chainage",
                table: "LCMS_Segment_Grid");

            migrationBuilder.DropColumn(
                name: "Chainage",
                table: "LCMS_Rumble_Strip");

            migrationBuilder.DropColumn(
                name: "Chainage",
                table: "LCMS_PCI");

            migrationBuilder.DropColumn(
                name: "Chainage",
                table: "LCMS_PASER");

            migrationBuilder.DropColumn(
                name: "Chainage",
                table: "LCMS_FOD");

            migrationBuilder.DropColumn(
                name: "Chainage",
                table: "LCMS_Bleeding");

            migrationBuilder.AlterColumn<double>(
                name: "Chainage",
                table: "MetaTableValue",
                type: "REAL",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "REAL");

            migrationBuilder.AlterColumn<int>(
                name: "Chainage",
                table: "LCMS_Water_Entrapment",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "REAL");

            migrationBuilder.AlterColumn<double>(
                name: "Chainage",
                table: "LCMS_Texture_Processed",
                type: "REAL",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "REAL");

            migrationBuilder.AlterColumn<double>(
                name: "Chainage",
                table: "LCMS_Spalling_Raw",
                type: "REAL",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "REAL");

            migrationBuilder.AlterColumn<double>(
                name: "Chainage",
                table: "LCMS_Shove_Processed",
                type: "REAL",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "REAL");

            migrationBuilder.AlterColumn<double>(
                name: "Chainage",
                table: "LCMS_Sealed_Cracks",
                type: "REAL",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "REAL");

            migrationBuilder.AlterColumn<double>(
                name: "Chainage",
                table: "LCMS_Sags_Bumps",
                type: "REAL",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "REAL");

            migrationBuilder.AlterColumn<double>(
                name: "Chainage",
                table: "LCMS_Rut_Processed",
                type: "REAL",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "REAL");

            migrationBuilder.AddColumn<double>(
                name: "ChainageEnd",
                table: "LCMS_Rumble_Strip",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ChainageStart",
                table: "LCMS_Rumble_Strip",
                type: "REAL",
                nullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "Chainage",
                table: "LCMS_Rough_Processed",
                type: "REAL",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "REAL");

            migrationBuilder.AlterColumn<double>(
                name: "Chainage",
                table: "LCMS_Ravelling_Raw",
                type: "REAL",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "REAL");

            migrationBuilder.AlterColumn<double>(
                name: "Chainage",
                table: "LCMS_Pumping_Processed",
                type: "REAL",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "REAL");

            migrationBuilder.AlterColumn<double>(
                name: "Chainage",
                table: "LCMS_Potholes_Processed",
                type: "REAL",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "REAL");

            migrationBuilder.AlterColumn<double>(
                name: "Chainage",
                table: "LCMS_PickOuts_Raw",
                type: "REAL",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "REAL");

            migrationBuilder.AlterColumn<double>(
                name: "Chainage",
                table: "LCMS_Patch_Processed",
                type: "REAL",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "REAL");

            migrationBuilder.AlterColumn<double>(
                name: "Chainage",
                table: "LCMS_MMO_Processed",
                type: "REAL",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "REAL");

            migrationBuilder.AlterColumn<double>(
                name: "Chainage",
                table: "LCMS_Marking_Contour",
                type: "REAL",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "REAL");

            migrationBuilder.AlterColumn<double>(
                name: "Chainage",
                table: "LCMS_Lane_Mark_Processed",
                type: "REAL",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "REAL");

            migrationBuilder.AlterColumn<double>(
                name: "Chainage",
                table: "LCMS_Grooves",
                type: "REAL",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "REAL");

            migrationBuilder.AlterColumn<int>(
                name: "Chainage",
                table: "LCMS_Geometry_Processed",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "REAL");

            migrationBuilder.AlterColumn<double>(
                name: "Chainage",
                table: "LCMS_Curb_DropOff",
                type: "REAL",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "REAL");

            migrationBuilder.AlterColumn<double>(
                name: "Chainage",
                table: "LCMS_CrackSummary",
                type: "REAL",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "REAL");

            migrationBuilder.AlterColumn<double>(
                name: "Chainage",
                table: "LCMS_Cracking_Raw",
                type: "REAL",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "REAL");

            migrationBuilder.AlterColumn<double>(
                name: "Chainage",
                table: "LCMS_Corner_Break",
                type: "REAL",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "REAL");

            migrationBuilder.AlterColumn<double>(
                name: "Chainage",
                table: "LCMS_Concrete_Joints",
                type: "REAL",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "REAL");

            migrationBuilder.AddColumn<float>(
                name: "ChainageEnd",
                table: "LCMS_Bleeding",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "ChainageStart",
                table: "LCMS_Bleeding",
                type: "REAL",
                nullable: true);
        }
    }
}
