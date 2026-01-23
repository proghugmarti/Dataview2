using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataView2.GrpcService.Migrations.Project
{
    /// <inheritdoc />
    public partial class UpdateSurveyIdForPositioning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OdoDetails");

            migrationBuilder.DropColumn(
                name: "SurveyIdExternal",
                table: "GPS_Raw");

            migrationBuilder.AlterColumn<int>(
                name: "SurveyId",
                table: "VideoFrame",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddColumn<string>(
                name: "Operator",
                table: "Survey",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "SurveyId",
                table: "OdoData",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddColumn<int>(
                name: "SurveyId",
                table: "GPS_Raw",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "SurveyId",
                table: "GPS_Processed",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<int>(
                name: "SurveyId",
                table: "Camera360Frame",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Operator",
                table: "Survey");

            migrationBuilder.DropColumn(
                name: "SurveyId",
                table: "GPS_Raw");

            migrationBuilder.AlterColumn<string>(
                name: "SurveyId",
                table: "VideoFrame",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<string>(
                name: "SurveyId",
                table: "OdoData",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<string>(
                name: "SurveyIdExternal",
                table: "GPS_Raw",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "SurveyId",
                table: "GPS_Processed",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<string>(
                name: "SurveyId",
                table: "Camera360Frame",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.CreateTable(
                name: "OdoDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Chainage = table.Column<double>(type: "REAL", nullable: false),
                    NmeaLine = table.Column<string>(type: "TEXT", nullable: false),
                    Speed = table.Column<double>(type: "REAL", nullable: false),
                    SurveyIdExternal = table.Column<string>(type: "TEXT", nullable: false),
                    SurveyName = table.Column<string>(type: "TEXT", nullable: false),
                    Time = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OdoDetails", x => x.Id);
                });
        }
    }
}
