using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DataView2.Core.Models.CrackClassification.Crack;

namespace DataView2.Core.Models.CrackClassification
{
    public class CrackInMatrix
    {

        public int Id { get; set; }

        public CrackTypeEnum CrackType { get; set; }

        public List<PointInMatrix> Cells { get; set; }

        public int MinX { get { return Cells.Select(p => p.X).Min(); } }
        public int MaxX { get { return Cells.Select(p => p.X).Max(); } }
        public int MinY { get { return Cells.Select(p => p.Y).Min(); } }
        public int MaxY { get { return Cells.Select(p => p.Y).Max(); } }

        public double XDistance() { 
            return (double)(1 + MaxX - MinX);
        }

        public double YDistance()
        {
            return (double)(1 + MaxY - MinY);
        }

        public CrackInMatrix(int id) {
            Id = id; 
            Cells = new List<PointInMatrix>();
        }

        public class PointInMatrix {
            public int X,
                Y;

            public PointInMatrix(int y, int x)
            {
                Y = y; X = x;
            }
        }

        public double Length() {
            if (Cells.Count() == 0)
            {
                return 0;
            }

            return (double)Math.Max(XDistance(), YDistance());
        }


        public double Straightness() {
            if (Cells.Count() == 0) {
                return 0;
            }
            double Distance = Length();

            return Distance / (double) Cells.Count();
        }


    }
}
