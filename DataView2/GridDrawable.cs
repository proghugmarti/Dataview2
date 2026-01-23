using DataView2.Core.Models.LCMS_Data_Tables;
using DataView2.Core.Models.Other;
using DataView2.Engines;
using DataView2.States;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Maui.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static MudBlazor.Colors;

namespace DataView2
{
    public class GridDrawable : IDrawable
    {
        private int matrixWidth;
        private int matrixHeight;
        private List<LCMS_Segment_Grid> segmentGridOutputs;
        private bool isGridVisible;

        public GridDrawable(int matrixWidth, int matrixHeight, List<LCMS_Segment_Grid> segmentGrids)
        {
            this.matrixWidth = matrixWidth;
            this.matrixHeight = matrixHeight;
            this.segmentGridOutputs = segmentGrids;
        }
        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            canvas.SaveState();
            canvas.StrokeColor = Colors.Transparent; // Colors.Red;
            canvas.StrokeSize = 1;

            float cellWidth = dirtyRect.Width / matrixWidth;
            float cellHeight = dirtyRect.Height / matrixHeight;

            // Call DrawGrid to draw de grid
            DrawGrid(canvas, matrixWidth, matrixHeight, cellWidth, cellHeight, dirtyRect);

            canvas.RestoreState();
        }

        void DrawGrid(ICanvas canvas, int columns, int rows, float cellWidth, float cellHeight, RectF dirtyRect)
        {
            // Drawing vertical lines
            for (int i = 0; i <= columns; i++)
            {
                float x = i * cellWidth; // X line Positión 
                canvas.DrawLine(x, 0, x, dirtyRect.Height); // Vertical Line
            }

            // Drawing horizontal lines
            for (int j = 0; j <= rows; j++)
            {
                float y = j * cellHeight; // Y line Positión 
                canvas.DrawLine(0, y, dirtyRect.Width, y); // Horizontal Line
            }

            //apply updated color codes to segment grids
            #region Color coding
            Color veryLowColor = new Color(0, 255, 0, 0.25f), lowColor = new Color(255, 255, 0, 0.25f), medColor = new Color(255, 165, 0, 0.5f), highColor = new Color(200, 0, 0, 0.5f), veryHighColor = new Color(255, 0, 0, 0.5f);

            #endregion

            foreach (var segment in segmentGridOutputs)
            {
                // Verify that the column and row are within the range and that the severity and type of crack conditions are met
                if (segment.Column >= 0 && segment.Row >= 0 && segment.Column < columns && segment.Row < rows &&
                    (segment.Severity == "High" || segment.Severity == "Very High") && segment.CrackType != "Unknown" && segment.CrackType != "WheelPath" && segment.CrackType != "Offroad" && segment.CrackType != "Other")
                {
                    float highlightX = segment.Column * cellWidth;
                    float highlightY = segment.Row * cellHeight;
                    if (segment.Severity == "High")
                        canvas.FillColor = highColor;//new Color(255, 0, 0, 0.5f); //Red
                    else
                        canvas.FillColor = veryHighColor;//new Color(255, 0, 0, 0.5f); //Red

                    //canvas.FillColor = new Color(0, 0, 0, 0.5f); //Black  (1, 1, 0, 0.5f); // Amarillo
                    canvas.FillRectangle(highlightX, highlightY, cellWidth, cellHeight);
                }
                if (segment.Column >= 0 && segment.Row >= 0 && segment.Column < columns && segment.Row < rows &&
                   segment.Severity == "Medium" && segment.CrackType != "Unknown" && segment.CrackType != "WheelPath")
                {
                    float highlightX = segment.Column * cellWidth;
                    float highlightY = segment.Row * cellHeight;
                    canvas.FillColor = medColor;// new Color(0.5f, 0.0f, 0.5f, 0.5f);  //Violet
                    canvas.FillRectangle(highlightX, highlightY, cellWidth, cellHeight);
                }
                if (segment.Column >= 0 && segment.Row >= 0 && segment.Column < columns && segment.Row < rows &&
                  (segment.Severity == "Very Low" || segment.Severity == "Low") && segment.CrackType != "Unknown" && segment.CrackType != "WheelPath")
                {
                    float highlightX = segment.Column * cellWidth;
                    float highlightY = segment.Row * cellHeight;

                    if (segment.Severity == "Low")
                        canvas.FillColor = lowColor;//new Color(0, 1, 0, 0.25f); //Green
                    else
                        canvas.FillColor = veryLowColor;//new Color(0, 1, 0, 0.25f); //Green

                    //canvas.FillColor = new Color(0, 1, 0, 0.25f); //Green
                    canvas.FillRectangle(highlightX, highlightY, cellWidth, cellHeight);
                }
                if (segment.Column >= 0 && segment.Row >= 0 && segment.Column < columns && segment.Row < rows &&
                  (segment.Severity == "0" || segment.Severity == "None") && segment.CrackType != "Offroad" && segment.CrackType != "Unknown" && segment.CrackType != "WheelPath" && segment.CrackType != "Other")
                {
                    float highlightX = segment.Column * cellWidth;
                    float highlightY = segment.Row * cellHeight;
                    canvas.FillColor = new Color(1, 1, 1, 0.25f); //White
                    canvas.FillRectangle(highlightX, highlightY, cellWidth, cellHeight);
                }

            }
        }
    }
}
