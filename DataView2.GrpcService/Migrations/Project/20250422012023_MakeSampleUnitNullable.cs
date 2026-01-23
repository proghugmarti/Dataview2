using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataView2.GrpcService.Migrations.Project
{
    /// <inheritdoc />
    public partial class MakeSampleUnitNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Summary_SampleUnit_SampleUnitId",
                table: "Summary");

            migrationBuilder.DropForeignKey(
                name: "FK_Summary_SampleUnit_Set_SampleUnitSetId",
                table: "Summary");

            migrationBuilder.AlterColumn<int>(
                name: "SampleUnitSetId",
                table: "Summary",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<int>(
                name: "SampleUnitId",
                table: "Summary",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddForeignKey(
                name: "FK_Summary_SampleUnit_SampleUnitId",
                table: "Summary",
                column: "SampleUnitId",
                principalTable: "SampleUnit",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Summary_SampleUnit_Set_SampleUnitSetId",
                table: "Summary",
                column: "SampleUnitSetId",
                principalTable: "SampleUnit_Set",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Summary_SampleUnit_SampleUnitId",
                table: "Summary");

            migrationBuilder.DropForeignKey(
                name: "FK_Summary_SampleUnit_Set_SampleUnitSetId",
                table: "Summary");

            migrationBuilder.AlterColumn<int>(
                name: "SampleUnitSetId",
                table: "Summary",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "SampleUnitId",
                table: "Summary",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Summary_SampleUnit_SampleUnitId",
                table: "Summary",
                column: "SampleUnitId",
                principalTable: "SampleUnit",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Summary_SampleUnit_Set_SampleUnitSetId",
                table: "Summary",
                column: "SampleUnitSetId",
                principalTable: "SampleUnit_Set",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
