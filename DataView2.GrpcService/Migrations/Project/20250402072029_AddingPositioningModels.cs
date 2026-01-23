using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataView2.GrpcService.Migrations.Project
{
    /// <inheritdoc />
    public partial class AddingPositioningModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "EndChainage",
                table: "Survey",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EndFis",
                table: "Survey",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LRP",
                table: "Survey",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "LRPchainage",
                table: "Survey",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "StartChainage",
                table: "Survey",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StartFis",
                table: "Survey",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GPS_Raw",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Chainage = table.Column<double>(type: "REAL", nullable: false),
                    Latitude = table.Column<double>(type: "REAL", nullable: false),
                    Longitude = table.Column<double>(type: "REAL", nullable: false),
                    Heading = table.Column<double>(type: "REAL", nullable: false),
                    SystemTime = table.Column<double>(type: "REAL", nullable: false),
                    UTCTime = table.Column<double>(type: "REAL", nullable: true),
                    SurveyIdExternal = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GPS_Raw", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OdoData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Chainage = table.Column<double>(type: "REAL", nullable: false),
                    OdoCount = table.Column<int>(type: "INTEGER", nullable: false),
                    OdoTime = table.Column<int>(type: "INTEGER", nullable: false),
                    Speed = table.Column<double>(type: "REAL", nullable: false),
                    SystemTime = table.Column<long>(type: "INTEGER", nullable: false),
                    SurveyId = table.Column<string>(type: "TEXT", nullable: false),
                    SurveyName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OdoData", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GPS_Raw");

            migrationBuilder.DropTable(
                name: "OdoData");

            migrationBuilder.DropColumn(
                name: "EndChainage",
                table: "Survey");

            migrationBuilder.DropColumn(
                name: "EndFis",
                table: "Survey");

            migrationBuilder.DropColumn(
                name: "LRP",
                table: "Survey");

            migrationBuilder.DropColumn(
                name: "LRPchainage",
                table: "Survey");

            migrationBuilder.DropColumn(
                name: "StartChainage",
                table: "Survey");

            migrationBuilder.DropColumn(
                name: "StartFis",
                table: "Survey");
        }
    }
}
