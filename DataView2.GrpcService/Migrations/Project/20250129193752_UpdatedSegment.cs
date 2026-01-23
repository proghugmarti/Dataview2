using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataView2.GrpcService.Migrations.Project
{
    /// <inheritdoc />
    public partial class UpdatedSegment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "AverageMPD",
                table: "LCMS_Segment",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "AverageMTD",
                table: "LCMS_Segment",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "BleedingSeverity",
                table: "LCMS_Segment",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "BleedingTotalArea_m2",
                table: "LCMS_Segment",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "BumpsTotalArea_m2",
                table: "LCMS_Segment",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "CrackClassificationTotalAreaFatigueCracks",
                table: "LCMS_Segment",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "CrackClassificationTotalLengthLongCracks",
                table: "LCMS_Segment",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "CrackClassificationTotalLengthOtherCracks",
                table: "LCMS_Segment",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "CrackClassificationTotalLengthTransCracks",
                table: "LCMS_Segment",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "CrackingTotalLengthAllNodes",
                table: "LCMS_Segment",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "CrackingTotalLengthHighSevNodes",
                table: "LCMS_Segment",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "CrackingTotalLengthLowSevNodes",
                table: "LCMS_Segment",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "CrackingTotalLengthMedSevNodes",
                table: "LCMS_Segment",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "GeometryAvgCrossSlope",
                table: "LCMS_Segment",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "GeometryAvgGradient",
                table: "LCMS_Segment",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "GeometryAvgHorizontalCurvature",
                table: "LCMS_Segment",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "GeometryAvgVerticalCurvature",
                table: "LCMS_Segment",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "IRIAverage",
                table: "LCMS_Segment",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MmoCount",
                table: "LCMS_Segment",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "PatchesArea_m2",
                table: "LCMS_Segment",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "PickoutAvgPer_m2",
                table: "LCMS_Segment",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PickoutCount",
                table: "LCMS_Segment",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PotholesCount",
                table: "LCMS_Segment",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "PumpingArea_m2",
                table: "LCMS_Segment",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "RavellingSeverity",
                table: "LCMS_Segment",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "RavellingTotalArea_m2",
                table: "LCMS_Segment",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "RutAverage",
                table: "LCMS_Segment",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "SagsTotalArea_m2",
                table: "LCMS_Segment",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "SealedCrackTotalLength",
                table: "LCMS_Segment",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ShoveTotalLength",
                table: "LCMS_Segment",
                type: "REAL",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AverageMPD",
                table: "LCMS_Segment");

            migrationBuilder.DropColumn(
                name: "AverageMTD",
                table: "LCMS_Segment");

            migrationBuilder.DropColumn(
                name: "BleedingSeverity",
                table: "LCMS_Segment");

            migrationBuilder.DropColumn(
                name: "BleedingTotalArea_m2",
                table: "LCMS_Segment");

            migrationBuilder.DropColumn(
                name: "BumpsTotalArea_m2",
                table: "LCMS_Segment");

            migrationBuilder.DropColumn(
                name: "CrackClassificationTotalAreaFatigueCracks",
                table: "LCMS_Segment");

            migrationBuilder.DropColumn(
                name: "CrackClassificationTotalLengthLongCracks",
                table: "LCMS_Segment");

            migrationBuilder.DropColumn(
                name: "CrackClassificationTotalLengthOtherCracks",
                table: "LCMS_Segment");

            migrationBuilder.DropColumn(
                name: "CrackClassificationTotalLengthTransCracks",
                table: "LCMS_Segment");

            migrationBuilder.DropColumn(
                name: "CrackingTotalLengthAllNodes",
                table: "LCMS_Segment");

            migrationBuilder.DropColumn(
                name: "CrackingTotalLengthHighSevNodes",
                table: "LCMS_Segment");

            migrationBuilder.DropColumn(
                name: "CrackingTotalLengthLowSevNodes",
                table: "LCMS_Segment");

            migrationBuilder.DropColumn(
                name: "CrackingTotalLengthMedSevNodes",
                table: "LCMS_Segment");

            migrationBuilder.DropColumn(
                name: "GeometryAvgCrossSlope",
                table: "LCMS_Segment");

            migrationBuilder.DropColumn(
                name: "GeometryAvgGradient",
                table: "LCMS_Segment");

            migrationBuilder.DropColumn(
                name: "GeometryAvgHorizontalCurvature",
                table: "LCMS_Segment");

            migrationBuilder.DropColumn(
                name: "GeometryAvgVerticalCurvature",
                table: "LCMS_Segment");

            migrationBuilder.DropColumn(
                name: "IRIAverage",
                table: "LCMS_Segment");

            migrationBuilder.DropColumn(
                name: "MmoCount",
                table: "LCMS_Segment");

            migrationBuilder.DropColumn(
                name: "PatchesArea_m2",
                table: "LCMS_Segment");

            migrationBuilder.DropColumn(
                name: "PickoutAvgPer_m2",
                table: "LCMS_Segment");

            migrationBuilder.DropColumn(
                name: "PickoutCount",
                table: "LCMS_Segment");

            migrationBuilder.DropColumn(
                name: "PotholesCount",
                table: "LCMS_Segment");

            migrationBuilder.DropColumn(
                name: "PumpingArea_m2",
                table: "LCMS_Segment");

            migrationBuilder.DropColumn(
                name: "RavellingSeverity",
                table: "LCMS_Segment");

            migrationBuilder.DropColumn(
                name: "RavellingTotalArea_m2",
                table: "LCMS_Segment");

            migrationBuilder.DropColumn(
                name: "RutAverage",
                table: "LCMS_Segment");

            migrationBuilder.DropColumn(
                name: "SagsTotalArea_m2",
                table: "LCMS_Segment");

            migrationBuilder.DropColumn(
                name: "SealedCrackTotalLength",
                table: "LCMS_Segment");

            migrationBuilder.DropColumn(
                name: "ShoveTotalLength",
                table: "LCMS_Segment");
        }
    }
}
