using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataView2.Core.Models.LCMS_Data_Tables
{
    public class LCMS_Ravelling_Processed
    {
        public int Id { get; set; }
        public string SurveyId { get; set; }
        public float Chainage { get; set; }
        public long LRPNumStart { get; set; }
        public float LRPChainageStart { get; set; }
        public float Ravelled_Area { get; set; }
        public float AvgRavellingIndex { get; set; }
        public float AreaLow { get; set; } // Total area of LOW ravelling, based on number of affected squares in ravelling raw
        public float AreaMedium{ get; set; } // Total area of MEDIUM ravelling, based on number of affected squares in ravelling raw
        public float AreaHigh{ get; set; } // Total area of HIGH ravelling, based on number of affected squares in ravelling raw
        public string ImageFileIndex { get; set; }
        public double GPSLatitude { get; set; }
        public double GPSLongitude { get; set; }
        public double GPSAltitude { get; set; }
        public int SegmentId { get; set; }
    }
}
