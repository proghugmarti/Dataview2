using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataView2.GrpcService.Migrations.Project
{
    /// <inheritdoc />
    public partial class addedSampleUnitSet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SampleUnitSetId",
                table: "Boundaries",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SampleUnitSet",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SampleUnitSet", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Boundaries_SampleUnitSetId",
                table: "Boundaries",
                column: "SampleUnitSetId");

            migrationBuilder.AddForeignKey(
                name: "FK_Boundaries_SampleUnitSet_SampleUnitSetId",
                table: "Boundaries",
                column: "SampleUnitSetId",
                principalTable: "SampleUnitSet",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Boundaries_SampleUnitSet_SampleUnitSetId",
                table: "Boundaries");

            migrationBuilder.DropTable(
                name: "SampleUnitSet");

            migrationBuilder.DropIndex(
                name: "IX_Boundaries_SampleUnitSetId",
                table: "Boundaries");

            migrationBuilder.DropColumn(
                name: "SampleUnitSetId",
                table: "Boundaries");
        }
    }
}
