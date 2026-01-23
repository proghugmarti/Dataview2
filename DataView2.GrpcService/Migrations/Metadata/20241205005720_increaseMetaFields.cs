using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataView2.GrpcService.Migrations.Metadata
{
    /// <inheritdoc />
    public partial class increaseMetaFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Column11",
                table: "MetaTable",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Column11Default",
                table: "MetaTable",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Column11Type",
                table: "MetaTable",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Column12",
                table: "MetaTable",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Column12Default",
                table: "MetaTable",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Column12Type",
                table: "MetaTable",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Column13",
                table: "MetaTable",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Column13Default",
                table: "MetaTable",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Column13Type",
                table: "MetaTable",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Column14",
                table: "MetaTable",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Column14Default",
                table: "MetaTable",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Column14Type",
                table: "MetaTable",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Column15",
                table: "MetaTable",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Column15Default",
                table: "MetaTable",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Column15Type",
                table: "MetaTable",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Column16",
                table: "MetaTable",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Column16Default",
                table: "MetaTable",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Column16Type",
                table: "MetaTable",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Column17",
                table: "MetaTable",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Column17Default",
                table: "MetaTable",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Column17Type",
                table: "MetaTable",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Column18",
                table: "MetaTable",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Column18Default",
                table: "MetaTable",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Column18Type",
                table: "MetaTable",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Column19",
                table: "MetaTable",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Column19Default",
                table: "MetaTable",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Column19Type",
                table: "MetaTable",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Column20",
                table: "MetaTable",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Column20Default",
                table: "MetaTable",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Column20Type",
                table: "MetaTable",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Column21",
                table: "MetaTable",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Column21Default",
                table: "MetaTable",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Column21Type",
                table: "MetaTable",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Column22",
                table: "MetaTable",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Column22Default",
                table: "MetaTable",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Column22Type",
                table: "MetaTable",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Column23",
                table: "MetaTable",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Column23Default",
                table: "MetaTable",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Column23Type",
                table: "MetaTable",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Column24",
                table: "MetaTable",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Column24Default",
                table: "MetaTable",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Column24Type",
                table: "MetaTable",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Column25",
                table: "MetaTable",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Column25Default",
                table: "MetaTable",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Column25Type",
                table: "MetaTable",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Column11",
                table: "MetaTable");

            migrationBuilder.DropColumn(
                name: "Column11Default",
                table: "MetaTable");

            migrationBuilder.DropColumn(
                name: "Column11Type",
                table: "MetaTable");

            migrationBuilder.DropColumn(
                name: "Column12",
                table: "MetaTable");

            migrationBuilder.DropColumn(
                name: "Column12Default",
                table: "MetaTable");

            migrationBuilder.DropColumn(
                name: "Column12Type",
                table: "MetaTable");

            migrationBuilder.DropColumn(
                name: "Column13",
                table: "MetaTable");

            migrationBuilder.DropColumn(
                name: "Column13Default",
                table: "MetaTable");

            migrationBuilder.DropColumn(
                name: "Column13Type",
                table: "MetaTable");

            migrationBuilder.DropColumn(
                name: "Column14",
                table: "MetaTable");

            migrationBuilder.DropColumn(
                name: "Column14Default",
                table: "MetaTable");

            migrationBuilder.DropColumn(
                name: "Column14Type",
                table: "MetaTable");

            migrationBuilder.DropColumn(
                name: "Column15",
                table: "MetaTable");

            migrationBuilder.DropColumn(
                name: "Column15Default",
                table: "MetaTable");

            migrationBuilder.DropColumn(
                name: "Column15Type",
                table: "MetaTable");

            migrationBuilder.DropColumn(
                name: "Column16",
                table: "MetaTable");

            migrationBuilder.DropColumn(
                name: "Column16Default",
                table: "MetaTable");

            migrationBuilder.DropColumn(
                name: "Column16Type",
                table: "MetaTable");

            migrationBuilder.DropColumn(
                name: "Column17",
                table: "MetaTable");

            migrationBuilder.DropColumn(
                name: "Column17Default",
                table: "MetaTable");

            migrationBuilder.DropColumn(
                name: "Column17Type",
                table: "MetaTable");

            migrationBuilder.DropColumn(
                name: "Column18",
                table: "MetaTable");

            migrationBuilder.DropColumn(
                name: "Column18Default",
                table: "MetaTable");

            migrationBuilder.DropColumn(
                name: "Column18Type",
                table: "MetaTable");

            migrationBuilder.DropColumn(
                name: "Column19",
                table: "MetaTable");

            migrationBuilder.DropColumn(
                name: "Column19Default",
                table: "MetaTable");

            migrationBuilder.DropColumn(
                name: "Column19Type",
                table: "MetaTable");

            migrationBuilder.DropColumn(
                name: "Column20",
                table: "MetaTable");

            migrationBuilder.DropColumn(
                name: "Column20Default",
                table: "MetaTable");

            migrationBuilder.DropColumn(
                name: "Column20Type",
                table: "MetaTable");

            migrationBuilder.DropColumn(
                name: "Column21",
                table: "MetaTable");

            migrationBuilder.DropColumn(
                name: "Column21Default",
                table: "MetaTable");

            migrationBuilder.DropColumn(
                name: "Column21Type",
                table: "MetaTable");

            migrationBuilder.DropColumn(
                name: "Column22",
                table: "MetaTable");

            migrationBuilder.DropColumn(
                name: "Column22Default",
                table: "MetaTable");

            migrationBuilder.DropColumn(
                name: "Column22Type",
                table: "MetaTable");

            migrationBuilder.DropColumn(
                name: "Column23",
                table: "MetaTable");

            migrationBuilder.DropColumn(
                name: "Column23Default",
                table: "MetaTable");

            migrationBuilder.DropColumn(
                name: "Column23Type",
                table: "MetaTable");

            migrationBuilder.DropColumn(
                name: "Column24",
                table: "MetaTable");

            migrationBuilder.DropColumn(
                name: "Column24Default",
                table: "MetaTable");

            migrationBuilder.DropColumn(
                name: "Column24Type",
                table: "MetaTable");

            migrationBuilder.DropColumn(
                name: "Column25",
                table: "MetaTable");

            migrationBuilder.DropColumn(
                name: "Column25Default",
                table: "MetaTable");

            migrationBuilder.DropColumn(
                name: "Column25Type",
                table: "MetaTable");
        }
    }
}
