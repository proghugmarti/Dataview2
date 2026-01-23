using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataView2.GrpcService.Migrations.Project
{
    /// <inheritdoc />
    public partial class AddKeycodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Keycode",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SurveyId = table.Column<int>(type: "INTEGER", nullable: false),
                    SurveyName = table.Column<string>(type: "TEXT", nullable: false),
                    Chainage = table.Column<double>(type: "REAL", nullable: false),
                    GPSLatitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSLongitude = table.Column<double>(type: "REAL", nullable: false),
                    GPSTrackAngle = table.Column<double>(type: "REAL", nullable: false),
                    GeoJSON = table.Column<string>(type: "TEXT", nullable: false),
                    Time = table.Column<string>(type: "TEXT", nullable: false),
                    Speed = table.Column<double>(type: "REAL", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Key = table.Column<int>(type: "INTEGER", nullable: false),
                    EventKeyType = table.Column<string>(type: "TEXT", nullable: false),
                    ContinuousStatus = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Keycode", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Keycode");
        }
    }
}
