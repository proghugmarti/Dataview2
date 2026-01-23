using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DataView2.GrpcService.Migrations.Metadata
{
    /// <inheritdoc />
    public partial class ChangeInGraphicAndColorCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 36);

            migrationBuilder.DeleteData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 37);

            migrationBuilder.DeleteData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 38);

            migrationBuilder.DeleteData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 39);

            migrationBuilder.DeleteData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 40);

            migrationBuilder.DeleteData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 41);

            migrationBuilder.DeleteData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 42);

            migrationBuilder.DeleteData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 43);

            migrationBuilder.DeleteData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 44);

            migrationBuilder.DeleteData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 45);

            migrationBuilder.DeleteData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 46);

            migrationBuilder.DeleteData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 47);

            migrationBuilder.DeleteData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 48);

            migrationBuilder.DeleteData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 49);

            migrationBuilder.DeleteData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 50);

            migrationBuilder.DeleteData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 51);

            migrationBuilder.InsertData(
                table: "ColorCodeInformation",
                columns: new[] { "Id", "HexColor", "IsAboveFrom", "IsStringProperty", "LabelProperty", "MaxRange", "MinRange", "Property", "StringProperty", "TableName", "Thickness" },
                values: new object[,]
                {
                    { 1, "#E17DFAFF", false, true, "No Label", 0.0, 0.0, "Type", "Curb", "Curb DropOff", 3.0 },
                    { 2, "#00FD28FF", false, true, "No Label", 0.0, 0.0, "Type", "Dropoff", "Curb DropOff", 3.0 },
                    { 3, "#40FFFFFF", false, true, "No Label", 0.0, 0.0, "Severity", "Very Low", "Cracking", 3.0 },
                    { 4, "#00C000FF", false, true, "No Label", 0.0, 0.0, "Severity", "Low", "Cracking", 3.0 },
                    { 5, "#FF970FFF", false, true, "No Label", 0.0, 0.0, "Severity", "Medium", "Cracking", 3.0 },
                    { 6, "#FF0000FF", false, true, "No Label", 0.0, 0.0, "Severity", "High", "Cracking", 3.0 },
                    { 7, "#40FFFFFF", false, true, "No Label", 0.0, 0.0, "Severity", "Very Low", "Crack Summary", 3.0 },
                    { 8, "#00C000FF", false, true, "No Label", 0.0, 0.0, "Severity", "Low", "Crack Summary", 3.0 },
                    { 9, "#FF970FFF", false, true, "No Label", 0.0, 0.0, "Severity", "Medium", "Crack Summary", 3.0 },
                    { 10, "#FF0000FF", false, true, "No Label", 0.0, 0.0, "Severity", "High", "Crack Summary", 3.0 },
                    { 11, "#2E9FCFAA", false, true, "No Label", 0.0, 0.0, "Severity", "Low", "Ravelling", 3.0 },
                    { 12, "#3569B0AA", false, true, "No Label", 0.0, 0.0, "Severity", "Medium", "Ravelling", 3.0 },
                    { 13, "#BF0053AA", false, true, "No Label", 0.0, 0.0, "Severity", "High", "Ravelling", 3.0 },
                    { 14, "#FFFF00AA", false, true, "No Label", 0.0, 0.0, "Severity", "Low", "Bleeding", 3.0 },
                    { 15, "#FFA500AA", false, true, "No Label", 0.0, 0.0, "Severity", "Medium", "Bleeding", 3.0 },
                    { 16, "#FF0000AA", false, true, "No Label", 0.0, 0.0, "Severity", "High", "Bleeding", 3.0 },
                    { 17, "#00FF00AA", false, true, "No Label", 0.0, 0.0, "Severity", "Very Low", "Segment Grid", 3.0 },
                    { 18, "#FFFF00AA", false, true, "No Label", 0.0, 0.0, "Severity", "Low", "Segment Grid", 3.0 },
                    { 19, "#FFA500AA", false, true, "No Label", 0.0, 0.0, "Severity", "Medium", "Segment Grid", 3.0 },
                    { 20, "#FF0000AA", false, true, "No Label", 0.0, 0.0, "Severity", "High", "Segment Grid", 3.0 },
                    { 21, "#C80000AA", false, true, "No Label", 0.0, 0.0, "Severity", "Very High", "Segment Grid", 3.0 },
                    { 22, "#000000AA", false, true, "No Label", 0.0, 0.0, "Severity", "None", "Segment Grid", 3.0 }
                });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Name", "Red" },
                values: new object[] { "Cracking", 255 });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Alpha", "Green", "Name", "SymbolType", "Thickness" },
                values: new object[] { 170, 0, "Ravelling", "Fill", 1.0 });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Alpha", "Blue", "Green", "Name", "Red", "SymbolType", "Thickness" },
                values: new object[] { 170, 35, 26, "Pickout", 168, "Fill", 1.0 });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Alpha", "Blue", "Green", "Name", "Red", "SymbolType", "Thickness" },
                values: new object[] { 170, 110, 70, "Potholes", 44, "Fill", 1.0 });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "Blue", "Green", "Name", "Red" },
                values: new object[] { 150, 50, "Patch", 150 });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "Blue", "Green", "Name", "Red" },
                values: new object[] { 0, 255, "Spalling", 255 });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "Blue", "Green", "Name", "Red" },
                values: new object[] { 0, 88, "Corner Break", 255 });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "Alpha", "Blue", "Green", "Name", "Red", "SymbolType" },
                values: new object[] { 255, 225, 105, "Concrete Joint", 65, "Line" });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "Alpha", "Blue", "Green", "Name", "Red" },
                values: new object[] { 255, 255, 0, "Boundaries", 255 });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "Alpha", "Blue", "Green", "Name", "Red", "SymbolType" },
                values: new object[] { 50, 0, 0, "Segment", 0, "Segment" });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "Alpha", "Blue", "Green", "Name", "Red", "SymbolType" },
                values: new object[] { 255, 255, 0, "HighlightedSegment", 191, "Segment" });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 12,
                columns: new[] { "Alpha", "Green", "Name", "SymbolType", "Thickness" },
                values: new object[] { 255, 0, "Curb DropOff", "Line", 3.0 });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 13,
                columns: new[] { "Alpha", "Blue", "Green", "Name", "Red", "SymbolType" },
                values: new object[] { 170, 0, 255, "Marking Contour", 255, "Fill" });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 14,
                columns: new[] { "Blue", "Green", "Name", "Red", "SymbolType", "Thickness" },
                values: new object[] { 83, 22, "Sealed Crack", 191, "Line", 5.0 });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 15,
                columns: new[] { "Alpha", "Green", "Name", "Red", "SymbolType", "Thickness" },
                values: new object[] { 255, 255, "Lwp IRI", 255, "Line", 5.0 });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 16,
                columns: new[] { "Blue", "Green", "Name", "Red", "SymbolType", "Thickness" },
                values: new object[] { 0, 255, "Rwp IRI", 255, "Line", 5.0 });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 17,
                columns: new[] { "Blue", "Green", "Name", "Red", "Thickness" },
                values: new object[] { 0, 255, "Cwp IRI", 255, 5.0 });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 18,
                columns: new[] { "Blue", "Green", "Name", "Red", "Thickness" },
                values: new object[] { 226, 43, "Lane IRI", 138, 5.0 });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 19,
                columns: new[] { "Alpha", "Blue", "Green", "Name", "Red", "SymbolType", "Thickness" },
                values: new object[] { 255, 17, 247, "MMO", 17, "FillLine", 5.0 });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 20,
                columns: new[] { "Blue", "Green", "Name", "Red", "SymbolType" },
                values: new object[] { 17, 247, "Pumping", 17, "FillLine" });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 21,
                columns: new[] { "Alpha", "Green", "Name", "SymbolType", "Thickness" },
                values: new object[] { 170, 0, "Shove", "Fill", 1.0 });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 22,
                columns: new[] { "Green", "Name", "SymbolType" },
                values: new object[] { 0, "Rumble Strip", "FillLine" });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 23,
                columns: new[] { "Alpha", "Blue", "Name", "SymbolType", "Thickness" },
                values: new object[] { 170, 255, "Bleeding", "Fill", 1.0 });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 24,
                columns: new[] { "Alpha", "Blue", "Green", "Name", "Red", "SymbolType", "Thickness" },
                values: new object[] { 128, 255, 128, "Macro Texture", 255, "Fill", 1.0 });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 25,
                columns: new[] { "Alpha", "Blue", "Green", "Name", "Red", "SymbolType", "Thickness" },
                values: new object[] { 128, 100, 50, "Geometry", 0, "Line", 3.0 });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 26,
                columns: new[] { "Alpha", "Blue", "Green", "Name", "Red", "SymbolType", "Thickness" },
                values: new object[] { 128, 0, 255, "Sags Bumps", 0, "Fill", 1.0 });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 27,
                columns: new[] { "Alpha", "Blue", "Name", "Red" },
                values: new object[] { 128, 255, "Water Entrapment", 0 });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 28,
                columns: new[] { "Alpha", "Blue", "Green", "Name", "Red", "SymbolType", "Thickness" },
                values: new object[] { 170, 35, 26, "LasPoint", 168, "Fill", 1.0 });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 29,
                columns: new[] { "Alpha", "Name", "SymbolType", "Thickness" },
                values: new object[] { 255, "Left Rut", "Line", 5.0 });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 30,
                columns: new[] { "Alpha", "Green", "Name", "SymbolType", "Thickness" },
                values: new object[] { 255, 255, "Right Rut", "Line", 5.0 });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 31,
                columns: new[] { "Alpha", "Green", "Name", "SymbolType", "Thickness" },
                values: new object[] { 255, 255, "Lane Rut", "Line", 5.0 });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 32,
                columns: new[] { "Alpha", "Blue", "Green", "Name", "Red" },
                values: new object[] { 170, 0, 0, "Segment Grid", 0 });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 33,
                columns: new[] { "Alpha", "Blue", "Green", "Name", "Red", "SymbolType", "Thickness" },
                values: new object[] { 170, 0, 165, "LasPoints", 255, "Fill", 1.0 });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 34,
                columns: new[] { "Alpha", "Blue", "Name", "Red" },
                values: new object[] { 170, 255, "Grooves", 255 });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 35,
                columns: new[] { "Alpha", "Green", "Name", "Red", "SymbolType", "Thickness" },
                values: new object[] { 255, 255, "Crack Summary", 255, "Line", 3.0 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ColorCodeInformation",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "ColorCodeInformation",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "ColorCodeInformation",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "ColorCodeInformation",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "ColorCodeInformation",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "ColorCodeInformation",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "ColorCodeInformation",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "ColorCodeInformation",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "ColorCodeInformation",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "ColorCodeInformation",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "ColorCodeInformation",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "ColorCodeInformation",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "ColorCodeInformation",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "ColorCodeInformation",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "ColorCodeInformation",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "ColorCodeInformation",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "ColorCodeInformation",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "ColorCodeInformation",
                keyColumn: "Id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "ColorCodeInformation",
                keyColumn: "Id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "ColorCodeInformation",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "ColorCodeInformation",
                keyColumn: "Id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "ColorCodeInformation",
                keyColumn: "Id",
                keyValue: 22);

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Name", "Red" },
                values: new object[] { "Cracking0", 64 });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Alpha", "Green", "Name", "SymbolType", "Thickness" },
                values: new object[] { 255, 192, "Cracking1", "Line", 3.0 });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Alpha", "Blue", "Green", "Name", "Red", "SymbolType", "Thickness" },
                values: new object[] { 255, 15, 151, "Cracking2", 255, "Line", 3.0 });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Alpha", "Blue", "Green", "Name", "Red", "SymbolType", "Thickness" },
                values: new object[] { 255, 0, 0, "Cracking3", 255, "Line", 3.0 });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "Blue", "Green", "Name", "Red" },
                values: new object[] { 207, 159, "Ravelling1", 46 });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "Blue", "Green", "Name", "Red" },
                values: new object[] { 176, 105, "Ravelling2", 53 });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "Blue", "Green", "Name", "Red" },
                values: new object[] { 83, 0, "Ravelling3", 191 });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "Alpha", "Blue", "Green", "Name", "Red", "SymbolType" },
                values: new object[] { 170, 35, 26, "Pickout", 168, "Fill" });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "Alpha", "Blue", "Green", "Name", "Red" },
                values: new object[] { 170, 110, 70, "Potholes", 44 });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "Alpha", "Blue", "Green", "Name", "Red", "SymbolType" },
                values: new object[] { 170, 150, 50, "Patch", 150, "Fill" });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "Alpha", "Blue", "Green", "Name", "Red", "SymbolType" },
                values: new object[] { 170, 0, 255, "Spalling", 255, "Fill" });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 12,
                columns: new[] { "Alpha", "Green", "Name", "SymbolType", "Thickness" },
                values: new object[] { 170, 88, "Corner Break", "Fill", 1.0 });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 13,
                columns: new[] { "Alpha", "Blue", "Green", "Name", "Red", "SymbolType" },
                values: new object[] { 255, 225, 105, "Concrete Joint", 65, "Line" });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 14,
                columns: new[] { "Blue", "Green", "Name", "Red", "SymbolType", "Thickness" },
                values: new object[] { 255, 0, "Boundaries", 255, "Fill", 1.0 });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 15,
                columns: new[] { "Alpha", "Green", "Name", "Red", "SymbolType", "Thickness" },
                values: new object[] { 50, 0, "Segment", 0, "Segment", 1.0 });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 16,
                columns: new[] { "Blue", "Green", "Name", "Red", "SymbolType", "Thickness" },
                values: new object[] { 255, 0, "HighlightedSegment", 191, "Segment", 1.0 });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 17,
                columns: new[] { "Blue", "Green", "Name", "Red", "Thickness" },
                values: new object[] { 40, 253, "Dropoff", 0, 3.0 });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 18,
                columns: new[] { "Blue", "Green", "Name", "Red", "Thickness" },
                values: new object[] { 250, 125, "Curb", 225, 3.0 });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 19,
                columns: new[] { "Alpha", "Blue", "Green", "Name", "Red", "SymbolType", "Thickness" },
                values: new object[] { 170, 0, 255, "Marking Contour", 255, "Fill", 1.0 });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 20,
                columns: new[] { "Blue", "Green", "Name", "Red", "SymbolType" },
                values: new object[] { 83, 22, "Sealed Crack", 191, "Line" });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 21,
                columns: new[] { "Alpha", "Green", "Name", "SymbolType", "Thickness" },
                values: new object[] { 255, 255, "Lwp IRI", "Line", 5.0 });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 22,
                columns: new[] { "Green", "Name", "SymbolType" },
                values: new object[] { 255, "Rwp IRI", "Line" });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 23,
                columns: new[] { "Alpha", "Blue", "Name", "SymbolType", "Thickness" },
                values: new object[] { 255, 0, "Cwp IRI", "Line", 5.0 });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 24,
                columns: new[] { "Alpha", "Blue", "Green", "Name", "Red", "SymbolType", "Thickness" },
                values: new object[] { 255, 226, 43, "Lane IRI", 138, "Line", 5.0 });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 25,
                columns: new[] { "Alpha", "Blue", "Green", "Name", "Red", "SymbolType", "Thickness" },
                values: new object[] { 255, 17, 247, "MMO", 17, "FillLine", 5.0 });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 26,
                columns: new[] { "Alpha", "Blue", "Green", "Name", "Red", "SymbolType", "Thickness" },
                values: new object[] { 255, 17, 247, "Pumping", 17, "FillLine", 5.0 });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 27,
                columns: new[] { "Alpha", "Blue", "Name", "Red" },
                values: new object[] { 170, 0, "Shove", 255 });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 28,
                columns: new[] { "Alpha", "Blue", "Green", "Name", "Red", "SymbolType", "Thickness" },
                values: new object[] { 255, 0, 0, "Rumble Strip", 255, "FillLine", 5.0 });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 29,
                columns: new[] { "Alpha", "Name", "SymbolType", "Thickness" },
                values: new object[] { 170, "Bleeding1", "Fill", 1.0 });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 30,
                columns: new[] { "Alpha", "Green", "Name", "SymbolType", "Thickness" },
                values: new object[] { 170, 165, "Bleeding2", "Fill", 1.0 });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 31,
                columns: new[] { "Alpha", "Green", "Name", "SymbolType", "Thickness" },
                values: new object[] { 170, 0, "Bleeding3", "Fill", 1.0 });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 32,
                columns: new[] { "Alpha", "Blue", "Green", "Name", "Red" },
                values: new object[] { 128, 255, 128, "Macro Texture", 255 });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 33,
                columns: new[] { "Alpha", "Blue", "Green", "Name", "Red", "SymbolType", "Thickness" },
                values: new object[] { 128, 100, 50, "Geometry", 0, "Line", 3.0 });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 34,
                columns: new[] { "Alpha", "Blue", "Name", "Red" },
                values: new object[] { 128, 0, "Sags Bumps", 0 });

            migrationBuilder.UpdateData(
                table: "MapGraphic",
                keyColumn: "Id",
                keyValue: 35,
                columns: new[] { "Alpha", "Green", "Name", "Red", "SymbolType", "Thickness" },
                values: new object[] { 128, 0, "Water Entrapment", 0, "Fill", 1.0 });

            migrationBuilder.InsertData(
                table: "MapGraphic",
                columns: new[] { "Id", "Alpha", "Blue", "Green", "LabelProperty", "Name", "Red", "SymbolType", "Thickness" },
                values: new object[,]
                {
                    { 36, 170, 35, 26, "No Label", "LasPoint", 168, "Fill", 1.0 },
                    { 37, 255, 0, 255, "No Label", "Left Rut", 255, "Line", 5.0 },
                    { 38, 255, 0, 255, "No Label", "Right Rut", 255, "Line", 5.0 },
                    { 39, 255, 0, 255, "No Label", "Lane Rut", 255, "Line", 5.0 },
                    { 40, 170, 0, 0, "No Label", "Segment Grid5", 0, "Fill", 1.0 },
                    { 41, 170, 0, 255, "No Label", "Segment Grid0", 0, "Fill", 1.0 },
                    { 42, 170, 0, 255, "No Label", "Segment Grid1", 255, "Fill", 1.0 },
                    { 43, 170, 0, 165, "No Label", "Segment Grid2", 255, "Fill", 1.0 },
                    { 44, 170, 0, 0, "No Label", "Segment Grid3", 255, "Fill", 1.0 },
                    { 45, 170, 0, 0, "No Label", "Segment Grid4", 200, "Fill", 1.0 },
                    { 46, 170, 0, 165, "No Label", "LasPoints", 255, "Fill", 1.0 },
                    { 47, 170, 255, 255, "No Label", "Grooves", 255, "Fill", 1.0 },
                    { 48, 255, 255, 255, "No Label", "Crack Summary0", 64, "Line", 3.0 },
                    { 49, 255, 0, 192, "No Label", "Crack Summary1", 0, "Line", 3.0 },
                    { 50, 255, 15, 151, "No Label", "Crack Summary2", 255, "Line", 3.0 },
                    { 51, 255, 0, 0, "No Label", "Crack Summary3", 255, "Line", 3.0 }
                });
        }
    }
}
