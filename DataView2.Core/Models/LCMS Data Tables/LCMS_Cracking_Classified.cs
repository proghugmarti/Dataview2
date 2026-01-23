using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataView.Core.Models.LCMS_Data_Tables
{
    public class LCMS_Cracking_Classified
    {
        public int Id { get; set; }
        public string SurveyId { get; set; }
        public double Chainage { get; set; }
        public int LRPNumStart { get; set; }
        public int LRPChainageStart { get; set; } 
        public string CrackType { get; set; } // can be only from : longitudinal, transverse. other, fatigue 
        public int Area { get; set; }//m^2
        public int Lenght { get; set; } // m 
        public int MaxWidth { get; set; } // max from nodes 
        public int AvgWidth { get; set; }//average from nodes 
        public int MaxDepth { get; set; } // max from nodes 
        public int AvgDepth { get; set; }//average from nodes 
        public string Severity { get; set; }
        public string ImageFileIndex { get; set; }
        public double GPSLatitude { get; set; }
        public double GPSLongitude { get; set; }
        public double GPSAltitude { get; set; }
        public int SegmentId { get; set; }

    }
}
