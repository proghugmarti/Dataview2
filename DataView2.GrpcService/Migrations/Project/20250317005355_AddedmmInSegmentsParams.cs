using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataView2.GrpcService.Migrations.Project
{
    /// <inheritdoc />
    public partial class AddedmmInSegmentsParams : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ShoveTotalLength",
                table: "LCMS_Segment",
                newName: "ShoveTotalLength_mm");

            migrationBuilder.RenameColumn(
                name: "SealedCrackTotalLength",
                table: "LCMS_Segment",
                newName: "SealedCrackTotalLength_mm");

            migrationBuilder.RenameColumn(
                name: "CrackingTotalLengthMedSevNodes",
                table: "LCMS_Segment",
                newName: "CrackingTotalLengthMedSevNodes_mm");

            migrationBuilder.RenameColumn(
                name: "CrackingTotalLengthLowSevNodes",
                table: "LCMS_Segment",
                newName: "CrackingTotalLengthLowSevNodes_mm");

            migrationBuilder.RenameColumn(
                name: "CrackingTotalLengthHighSevNodes",
                table: "LCMS_Segment",
                newName: "CrackingTotalLengthHighSevNodes_mm");

            migrationBuilder.RenameColumn(
                name: "CrackingTotalLengthAllNodes",
                table: "LCMS_Segment",
                newName: "CrackingTotalLengthAllNodes_mm");

            migrationBuilder.RenameColumn(
                name: "CrackClassificationTotalLengthTransCracks",
                table: "LCMS_Segment",
                newName: "CrackClassificationTotalLengthTransCracks_mm");

            migrationBuilder.RenameColumn(
                name: "CrackClassificationTotalLengthOtherCracks",
                table: "LCMS_Segment",
                newName: "CrackClassificationTotalLengthOtherCracks_mm");

            migrationBuilder.RenameColumn(
                name: "CrackClassificationTotalLengthLongCracks",
                table: "LCMS_Segment",
                newName: "CrackClassificationTotalLengthLongCracks_mm");

            migrationBuilder.RenameColumn(
                name: "CrackClassificationTotalAreaFatigueCracks",
                table: "LCMS_Segment",
                newName: "CrackClassificationTotalAreaFatigueCracks_m2");

            migrationBuilder.RenameColumn(
                name: "AverageMTD",
                table: "LCMS_Segment",
                newName: "AverageMTD_mm");

            migrationBuilder.RenameColumn(
                name: "AverageMPD",
                table: "LCMS_Segment",
                newName: "AverageMPD_mm");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ShoveTotalLength_mm",
                table: "LCMS_Segment",
                newName: "ShoveTotalLength");

            migrationBuilder.RenameColumn(
                name: "SealedCrackTotalLength_mm",
                table: "LCMS_Segment",
                newName: "SealedCrackTotalLength");

            migrationBuilder.RenameColumn(
                name: "CrackingTotalLengthMedSevNodes_mm",
                table: "LCMS_Segment",
                newName: "CrackingTotalLengthMedSevNodes");

            migrationBuilder.RenameColumn(
                name: "CrackingTotalLengthLowSevNodes_mm",
                table: "LCMS_Segment",
                newName: "CrackingTotalLengthLowSevNodes");

            migrationBuilder.RenameColumn(
                name: "CrackingTotalLengthHighSevNodes_mm",
                table: "LCMS_Segment",
                newName: "CrackingTotalLengthHighSevNodes");

            migrationBuilder.RenameColumn(
                name: "CrackingTotalLengthAllNodes_mm",
                table: "LCMS_Segment",
                newName: "CrackingTotalLengthAllNodes");

            migrationBuilder.RenameColumn(
                name: "CrackClassificationTotalLengthTransCracks_mm",
                table: "LCMS_Segment",
                newName: "CrackClassificationTotalLengthTransCracks");

            migrationBuilder.RenameColumn(
                name: "CrackClassificationTotalLengthOtherCracks_mm",
                table: "LCMS_Segment",
                newName: "CrackClassificationTotalLengthOtherCracks");

            migrationBuilder.RenameColumn(
                name: "CrackClassificationTotalLengthLongCracks_mm",
                table: "LCMS_Segment",
                newName: "CrackClassificationTotalLengthLongCracks");

            migrationBuilder.RenameColumn(
                name: "CrackClassificationTotalAreaFatigueCracks_m2",
                table: "LCMS_Segment",
                newName: "CrackClassificationTotalAreaFatigueCracks");

            migrationBuilder.RenameColumn(
                name: "AverageMTD_mm",
                table: "LCMS_Segment",
                newName: "AverageMTD");

            migrationBuilder.RenameColumn(
                name: "AverageMPD_mm",
                table: "LCMS_Segment",
                newName: "AverageMPD");
        }
    }
}
