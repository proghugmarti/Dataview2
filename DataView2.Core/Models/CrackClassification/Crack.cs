using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataView2.Core.Models.CrackClassification
{
    public class Crack
    {
        public int Id { get; set; }
        public decimal Length { get; set; }

        public decimal WeightedDepth { get; set; }
        public decimal WeightedWidth { get; set; }

        public double MinX { get; set; }
        public double MaxX { get; set; }

        public double MinY { get; set; }
        public double MaxY { get; set; }

        public int colMinX { get; set; }
        public int colMaxX { get; set; }

        public int colMinY { get; set; }
        public int colMaxY { get; set; }

        public CrackTypeEnum CrackType { get; set; }

        public enum CrackTypeEnum
        {
            Unknown,
            Transversal,
            Longitudinal,
            Alligator,
            Other
        }
    }
}
