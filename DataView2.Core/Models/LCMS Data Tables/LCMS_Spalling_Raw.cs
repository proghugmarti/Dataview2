using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using DataView2.Core.Models.CrackClassification;
using Google.Protobuf.WellKnownTypes;
using ProtoBuf.Grpc;
using System.ServiceModel;
using DataView2.Core.Models.ExportTemplate;
using DataView2.Core.Models.Other;

namespace DataView2.Core.Models.LCMS_Data_Tables
{
    [DataContract]
    public class LCMS_Spalling_Raw: IEntity
    {
        [DataMember(Order = 1)]
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [DataMember(Order = 2)]
        public string SurveyId { get; set; }
        [Column(TypeName = "datetime")]
        [DataMember(Order = 3)]
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
        public int JointId { get; set; }
        [DataMember(Order = 9)]
        public string JointDirection { get; set; }
        [DataMember(Order = 10)]
        public int SpallingId { get; set; }
        [DataMember(Order = 11)]
        public double AvgDepth_mm { get; set; }
        [DataMember(Order = 12)]
        public double AvgWidth_mm { get; set; }
        [DataMember(Order = 13)]
        public double Length_mm { get; set; }
        [DataMember(Order = 14)]
        public string? ImageFileIndex { get; set; }
        [DataMember(Order = 15)]
        public double GPSLatitude { get; set; }
        [DataMember(Order = 16)]
        public double GPSLongitude { get; set; }
        [DataMember(Order = 17)]
        public double GPSAltitude { get; set; }
        [DataMember(Order = 18)]
        public double GPSTrackAngle { get; set; }
        [DataMember(Order = 19)]
        public string GeoJSON { get; set; }
        [DataMember(Order = 20)]
        [Column(TypeName = "boolean")]
        public bool? QCAccepted { get; set; } = false;

        [DataMember(Order = 21)]
        public double RoundedGPSLatitude { get; set; }

        [DataMember(Order = 22)]
        public double RoundedGPSLongitude { get; set; }
        [DataMember(Order = 23)]
        public int SegmentId { get; set; }
        [DataMember(Order = 24)]
        public double ChainageEnd { get; set; } = 0.0;
    }


    [ServiceContract]
    public interface ISpallingService
    {
        [OperationContract]
        Task<IdReply> Create(LCMS_Spalling_Raw request, CallContext context = default);

        [OperationContract]
        Task<List<LCMS_Spalling_Raw>> GetAll(Empty empty, CallContext context = default);
        [OperationContract]
        Task<LCMS_Spalling_Raw> GetById(IdRequest request, CallContext context = default);
        [OperationContract]
        Task<LCMS_Spalling_Raw> EditValue(LCMS_Spalling_Raw request, CallContext context = default);
        [OperationContract]
        Task<IdReply> DeleteObject(LCMS_Spalling_Raw request,
            CallContext context = default);

        [OperationContract]
        Task<IdReply> DeleteValue(IdRequest request, CallContext context = default);
        [OperationContract]
        Task<IEnumerable<LCMS_Spalling_Raw>> QueryAsync(string predicate);
        [OperationContract]
        Task<CountReply> GetCountAsync(string sqlQuery);
        [OperationContract]
        Task<IdReply> HasData(Empty empty, CallContext context = default);
        [OperationContract]
        Task<List<LCMS_Spalling_Raw>> GetWithinRange(CoordinateRangeRequest coordinateRequest);

        [OperationContract]
        Task<List<LCMS_Spalling_Raw>> GetBySurveyAndSegment(SurveyAndSegmentRequest request);

        [OperationContract]
        Task<List<LCMS_Spalling_Raw>> GetBySurvey(SurveyRequest request);

        [OperationContract]
        Task<IEnumerable<int>> QueryIdsAsync(string predicate);
        [OperationContract]
        Task<IdReply> HasDataBySurvey(string surveyId, CallContext context = default);

		[OperationContract]
		Task<CountReply> GetRecordCount(Empty empty, CallContext context = default);
        [OperationContract]
        Task<LCMS_Spalling_Raw> UpdateGenericData(string fieldsToUpdateSerialized);
    }
}
