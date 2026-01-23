using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataView2.GrpcService.Migrations.Project
{
    /// <inheritdoc />
    public partial class AddedParamsSummaryCrackClssification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "AlligatorCrackVeryHIGH",
                table: "SummaryCrackClasifications",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "AlligatorCrackVeryLOW",
                table: "SummaryCrackClasifications",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "LongCrackVeryHIGH",
                table: "SummaryCrackClasifications",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "LongCrackVeryLOW",
                table: "SummaryCrackClasifications",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "OtherCrackVeryHIGH",
                table: "SummaryCrackClasifications",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "OtherCrackVeryLOW",
                table: "SummaryCrackClasifications",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "TransCrackVeryHIGH",
                table: "SummaryCrackClasifications",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "TransCrackVeryLOW",
                table: "SummaryCrackClasifications",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AlligatorCrackVeryHIGH",
                table: "SummaryCrackClasifications");

            migrationBuilder.DropColumn(
                name: "AlligatorCrackVeryLOW",
                table: "SummaryCrackClasifications");

            migrationBuilder.DropColumn(
                name: "LongCrackVeryHIGH",
                table: "SummaryCrackClasifications");

            migrationBuilder.DropColumn(
                name: "LongCrackVeryLOW",
                table: "SummaryCrackClasifications");

            migrationBuilder.DropColumn(
                name: "OtherCrackVeryHIGH",
                table: "SummaryCrackClasifications");

            migrationBuilder.DropColumn(
                name: "OtherCrackVeryLOW",
                table: "SummaryCrackClasifications");

            migrationBuilder.DropColumn(
                name: "TransCrackVeryHIGH",
                table: "SummaryCrackClasifications");

            migrationBuilder.DropColumn(
                name: "TransCrackVeryLOW",
                table: "SummaryCrackClasifications");
        }
    }
}
