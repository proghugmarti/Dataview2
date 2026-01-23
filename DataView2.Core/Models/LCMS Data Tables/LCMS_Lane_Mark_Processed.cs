using DataView2.Core.Models.Other;
using Google.Protobuf.WellKnownTypes;
using ProtoBuf.Grpc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace DataView2.Core.Models.LCMS_Data_Tables
{
    //Lane Mark objects
    [DataContract]
    public class LCMS_Lane_Mark_Processed : IEntity
    {
        [DataMember(Order = 1)]
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [DataMember(Order = 2)]
        public string SurveyId { get; set; }
        [DataMember(Order = 3)]
        [Column(TypeName = "datetime")]
        public DateTime SurveyDate { get; set; }
        [DataMember(Order = 4)]
        public double Chainage { get; set; }
        [DataMember(Order = 5)]
        public long? LRPNumber { get; set; }
        [DataMember(Order = 6)]
        public double? LRPChainage { get; set; }
        [DataMember(Order = 7)]
        public string PavementType { get; set; }
        [DataMember(Order = 8)]
        public string LeftType { get; set; }
        [DataMember(Order = 9)]
        public string RightType { get; set; }
        [DataMember(Order = 10)]
        public double LeftLength_mm { get; set; }
        [DataMember(Order = 11)]
        public double RightLength_mm { get; set; }
        [DataMember(Order = 12)]
        public string ImageFileIndex { get; set; }
        [DataMember(Order = 13)]
        public double GPSLatitude { get; set; }
        [DataMember(Order = 14)]
        public double GPSLongitude { get; set; }
        [DataMember(Order = 15)]
        public double GPSAltitude { get; set; }
        [DataMember(Order = 16)]
        public double GPSTrackAngle { get; set; }
        [DataMember(Order = 17)]
        public string GeoJSON { get; set; }
        [DataMember(Order = 18)]
        public double RoundedGPSLatitude { get; set; }
        [DataMember(Order = 19)]
        public double RoundedGPSLongitude { get; set; }
        [DataMember(Order = 20)]
        public int SegmentId { get; set; }
        [DataMember(Order = 21)]
        public double LaneWidth { get; set; }
        [DataMember(Order = 22)]
        public double ChainageEnd { get; set; } = 0.0;
    }

    [ServiceContract]
    public interface ILanMarkedProcessedService
    {
        [OperationContract]
        Task<IdReply> DeleteObject(LCMS_Lane_Mark_Processed request, CallContext context = default);

        [OperationContract]
        Task<IEnumerable<LCMS_Lane_Mark_Processed>> QueryAsync(string predicate);
        [OperationContract]
        Task<CountReply> GetCountAsync(string sqlQuery);

        [OperationContract]
        Task<CountReply> GetRecordCount(Empty empty, CallContext context = default);
        [OperationContract]
        Task<LCMS_Lane_Mark_Processed> EditValue(LCMS_Lane_Mark_Processed request, CallContext context = default);
        [OperationContract]
        Task<LCMS_Lane_Mark_Processed> UpdateGenericData(string fieldsToUpdateSerialized);
        [OperationContract]
        Task<IdReply> HasData(Empty empty, CallContext context = default);
        [OperationContract]
        Task<IdReply> HasDataBySurvey(string surveyId, CallContext context = default);
    }
}
