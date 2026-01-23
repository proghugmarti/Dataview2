using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace DataView2.Core.Models.LCMS_Data_Tables
{
    public class LCMS_PickOuts_Processed
    {
        public int Id { get; set; }
        public string SurveyId { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime SurveyDate { get; set; }
        public float ChainageStart { get; set; }
        public float ChainageEnd { get; set; }
        public long LRPNumStart { get; set; }
        public float LRPChainageStart { get; set; }
        public long LRPNumEnd { get; set; }
        public float LRPChainageEnd { get; set; }
        public float TotalArea { get; set; }
        public long PickOutNum { get; set; }
        public float PickOutPerM2 { get; set; }
        public float PickOutArea { get; set; }
        public float PickOutAreaPerM2 { get; set; }
        public string ImageFileIndex { get; set; }
        public double GPSLatitude { get; set; }
        public double GPSLongitude { get; set; }
        public double GPSAltitude { get; set; }
        public int SegmentId { get; set; }
    }
}
