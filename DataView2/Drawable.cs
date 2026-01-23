using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataView2
{
    internal class Drawable : IDrawable
    {
        public PointF start;
        public PointF end;
        public float width;
        public float height;
        private float canvasWidth;
        private float canvasHeight;
        private float aspectRatio = 1; // Default to 1 (square)

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            canvas.StrokeColor = Colors.Black;
            canvas.StrokeSize = 2;
            //float size = Math.Min(width, height);

            //// Ensure the square stays within bounds
            //float maxSize = Math.Min(canvasWidth, canvasHeight);
            //size = Math.Min(size, maxSize);

            //// Adjust the width and height to maintain the aspect ratio
            //width = size;
            //height = size;

            float adjustedHeight = width / aspectRatio;

            // Ensure the rectangle stays within the canvas bounds
            width = Math.Min(width, canvasWidth - start.X);
            height = Math.Min(adjustedHeight, canvasHeight - start.Y);

            // Draw the square
            canvas.DrawRectangle(start.X, start.Y, width, height);
        }

        public void UpdateRectangle(PointF startPoint, PointF endPoint)
        {
            start = startPoint;
            end = endPoint;

            width = end.X - start.X;
            height = end.Y - start.Y;
        }

        public void UpdateCanvasSize(float width, float height, float imageAspectRatio)
        {
            canvasWidth = width;
            canvasHeight = height;
            aspectRatio = imageAspectRatio;
        }

        public bool Clear()
        {
            // Reset the state of the drawable
            start = PointF.Zero;
            end = PointF.Zero;
            width = 0;
            height = 0;
            return true;
        }
    }
}
