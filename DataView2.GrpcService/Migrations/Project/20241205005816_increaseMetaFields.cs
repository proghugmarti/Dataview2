using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataView2.GrpcService.Migrations.Project
{
    /// <inheritdoc />
    public partial class increaseMetaFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DecValue11",
                table: "MetaTableValue",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DecValue12",
                table: "MetaTableValue",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DecValue13",
                table: "MetaTableValue",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DecValue14",
                table: "MetaTableValue",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DecValue15",
                table: "MetaTableValue",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DecValue16",
                table: "MetaTableValue",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DecValue17",
                table: "MetaTableValue",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DecValue18",
                table: "MetaTableValue",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DecValue19",
                table: "MetaTableValue",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DecValue20",
                table: "MetaTableValue",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DecValue21",
                table: "MetaTableValue",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DecValue22",
                table: "MetaTableValue",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DecValue23",
                table: "MetaTableValue",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DecValue24",
                table: "MetaTableValue",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DecValue25",
                table: "MetaTableValue",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StrValue11",
                table: "MetaTableValue",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StrValue12",
                table: "MetaTableValue",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StrValue13",
                table: "MetaTableValue",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StrValue14",
                table: "MetaTableValue",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StrValue15",
                table: "MetaTableValue",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StrValue16",
                table: "MetaTableValue",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StrValue17",
                table: "MetaTableValue",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StrValue18",
                table: "MetaTableValue",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StrValue19",
                table: "MetaTableValue",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StrValue20",
                table: "MetaTableValue",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StrValue21",
                table: "MetaTableValue",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StrValue22",
                table: "MetaTableValue",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StrValue23",
                table: "MetaTableValue",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StrValue24",
                table: "MetaTableValue",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StrValue25",
                table: "MetaTableValue",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DecValue11",
                table: "MetaTableValue");

            migrationBuilder.DropColumn(
                name: "DecValue12",
                table: "MetaTableValue");

            migrationBuilder.DropColumn(
                name: "DecValue13",
                table: "MetaTableValue");

            migrationBuilder.DropColumn(
                name: "DecValue14",
                table: "MetaTableValue");

            migrationBuilder.DropColumn(
                name: "DecValue15",
                table: "MetaTableValue");

            migrationBuilder.DropColumn(
                name: "DecValue16",
                table: "MetaTableValue");

            migrationBuilder.DropColumn(
                name: "DecValue17",
                table: "MetaTableValue");

            migrationBuilder.DropColumn(
                name: "DecValue18",
                table: "MetaTableValue");

            migrationBuilder.DropColumn(
                name: "DecValue19",
                table: "MetaTableValue");

            migrationBuilder.DropColumn(
                name: "DecValue20",
                table: "MetaTableValue");

            migrationBuilder.DropColumn(
                name: "DecValue21",
                table: "MetaTableValue");

            migrationBuilder.DropColumn(
                name: "DecValue22",
                table: "MetaTableValue");

            migrationBuilder.DropColumn(
                name: "DecValue23",
                table: "MetaTableValue");

            migrationBuilder.DropColumn(
                name: "DecValue24",
                table: "MetaTableValue");

            migrationBuilder.DropColumn(
                name: "DecValue25",
                table: "MetaTableValue");

            migrationBuilder.DropColumn(
                name: "StrValue11",
                table: "MetaTableValue");

            migrationBuilder.DropColumn(
                name: "StrValue12",
                table: "MetaTableValue");

            migrationBuilder.DropColumn(
                name: "StrValue13",
                table: "MetaTableValue");

            migrationBuilder.DropColumn(
                name: "StrValue14",
                table: "MetaTableValue");

            migrationBuilder.DropColumn(
                name: "StrValue15",
                table: "MetaTableValue");

            migrationBuilder.DropColumn(
                name: "StrValue16",
                table: "MetaTableValue");

            migrationBuilder.DropColumn(
                name: "StrValue17",
                table: "MetaTableValue");

            migrationBuilder.DropColumn(
                name: "StrValue18",
                table: "MetaTableValue");

            migrationBuilder.DropColumn(
                name: "StrValue19",
                table: "MetaTableValue");

            migrationBuilder.DropColumn(
                name: "StrValue20",
                table: "MetaTableValue");

            migrationBuilder.DropColumn(
                name: "StrValue21",
                table: "MetaTableValue");

            migrationBuilder.DropColumn(
                name: "StrValue22",
                table: "MetaTableValue");

            migrationBuilder.DropColumn(
                name: "StrValue23",
                table: "MetaTableValue");

            migrationBuilder.DropColumn(
                name: "StrValue24",
                table: "MetaTableValue");

            migrationBuilder.DropColumn(
                name: "StrValue25",
                table: "MetaTableValue");
        }
    }
}
