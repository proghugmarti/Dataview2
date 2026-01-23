using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using DataView2.Core.Models.LCMS_Data_Tables;
using Google.Protobuf.WellKnownTypes;
using ProtoBuf.Grpc;

namespace DataView2.Core.Models.Other
{
    [DataContract]
    public class BoundarySummary
    {
        [DataMember(Order = 1)]
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [DataMember(Order = 2)]
        public string SurveyId { get; set; }
        [DataMember(Order = 3)]
        public string TableName { get; set; }
        [DataMember(Order = 4)]
        public string NumericField { get; set; }
        [DataMember(Order = 5)]
        public string Operation { get; set; }
        [DataMember(Order = 6)]
        public double Value { get; set; }
        [DataMember(Order = 7)]
        public int SampleUnitBoundaryId { get; set; }
        [DataMember(Order = 8)]
        public string SampleUnitBoundaryName { get; set; }
        [DataMember(Order = 9)]
        public string GeoJSON { get; set; }
        [DataMember(Order = 10)]
        public double GPSLatitude { get; set; }
        [DataMember(Order = 11)]
        public double GPSLongitude { get; set; }
        [DataMember(Order = 12)]
        public double GPSTrackAngle { get; set; }
        [DataMember(Order = 13)]
        public string BoundarySummaryName { get; set; }
        [DataMember(Order = 14)]
        public string SampleUnitSetName { get; set; }
    }
}
