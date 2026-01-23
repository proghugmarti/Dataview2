using System;
using System.Collections.Generic;
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
    public class LCMS_Concrete_Joints : IEntity
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
        public long? LRPNumStart { get; set; }
        [DataMember(Order = 6)]
        public double? LRPChainageStart { get; set; }
        [DataMember(Order = 7)]
        public string PavementType { get; set; }
        [DataMember(Order = 8)]
        public string JointId { get; set; }
        [DataMember(Order = 9)]
        public string JointDirection { get; set; }
        [DataMember(Order = 10)]
        public double Length_mm { get; set; }
        [DataMember(Order = 11)]
        public double AvgWidth_mm { get; set; }
        [DataMember(Order = 12)]
        public double AvgDepth_mm { get; set; }
        [DataMember(Order = 13)]
        public double FaultingAvgHeight_mm { get; set; }
        [DataMember(Order = 14)]
        public double FaultingMaxHeight_mm { get; set; }
        [DataMember(Order = 15)]
        public double FaultingMinHeight_mm { get; set; }
        [DataMember(Order = 16)]
        public double BadSealLength_mm { get; set; }
        [DataMember(Order = 17)]
        public double BadSealAvgDepth_mm { get; set; }
        [DataMember(Order = 18)]
        public double BadSealMaxDepth_mm { get; set; }
        [DataMember(Order = 19)]
        public double SpallingLength_mm { get; set; }
        [DataMember(Order = 20)]
        public double SpallingAvgDepth_mm { get; set; }
        [DataMember(Order = 21)]
        public double SpallingMaxDepth_mm { get; set; }
        [DataMember(Order = 22)]
        public double SpallingAvgWidth_mm { get; set; }
        [DataMember(Order = 23)]
        public double SpallingMaxWidth_mm { get; set; }
        [DataMember(Order = 24)]
        public double MedianPercentRng { get; set; }
        [DataMember(Order = 25)]
        public double MedianPercentInt { get; set; }
        [DataMember(Order = 26)]
        public string ImageFileIndex { get; set; }
        [DataMember(Order = 27)]
        public double GPSLatitude { get; set; } //startLatitude
        [DataMember(Order = 28)]
        public double GPSLongitude { get; set; } //startLongitude
        [DataMember(Order = 29)]
        public double GPSAltitude { get; set; } //startAltitude
        [DataMember(Order = 30)]
        public double GPSTrackAngle { get; set; }
        [DataMember(Order = 31)]
        public double EndGPSLatitude { get; set; }
        [DataMember(Order = 32)]
        public double EndGPSLongitude { get; set; }
        [DataMember(Order = 33)]
        public double EndGPSAltitude { get; set; }
        [DataMember(Order = 34)]
        public string GeoJSON { get; set; }

        [DataMember(Order = 35)]
        [Column(TypeName = "boolean")]
        public bool? QCAccepted { get; set; } = false;

        [DataMember(Order = 36)]
        public double RoundedGPSLatitude { get; set; }
        [DataMember(Order = 37)]
        public double RoundedGPSLongitude { get; set; }
        [DataMember(Order = 38)]
        public int SegmentId { get; set; }
        [DataMember(Order = 39)]
        public double AvgRngDepth_mm { get; set; }
        [DataMember(Order = 40)]
        public double StdRngDepth_mm { get; set; }
        [DataMember(Order = 41)]
        public double ChainageEnd { get; set; } = 0.0;
    }



    [ServiceContract]
    public interface IConcreteJointService
    {
        [OperationContract]
        Task<IdReply> Create(LCMS_Concrete_Joints request, CallContext context = default);

        [OperationContract]
        Task<List<LCMS_Concrete_Joints>> GetAll(Empty empty, CallContext context = default);

        [OperationContract]
        Task<LCMS_Concrete_Joints> GetById(IdRequest request, CallContext context = default);

        [OperationContract]
        Task<LCMS_Concrete_Joints> EditValue(LCMS_Concrete_Joints request, CallContext context = default);

        [OperationContract]
        Task<IdReply> DeleteObject(LCMS_Concrete_Joints request,
            CallContext context = default);

        [OperationContract]
        Task<IdReply> DeleteValue(IdRequest request, CallContext context = default);
        
        [OperationContract]
        Task<IEnumerable<LCMS_Concrete_Joints>> QueryAsync(string predicate);

        [OperationContract]
        Task<CountReply> GetCountAsync(string sqlQuery);

        [OperationContract]
        Task<IdReply> HasData(Empty empty, CallContext context = default);
        [OperationContract]
        Task<List<LCMS_Concrete_Joints>> GetWithinRange(CoordinateRangeRequest coordinateRequest);

        [OperationContract]
        Task<List<LCMS_Concrete_Joints>> GetBySurveyAndSegment(SurveyAndSegmentRequest request);

        [OperationContract]
        Task<List<LCMS_Concrete_Joints>> GetBySurvey(SurveyRequest request);

        [OperationContract]
        Task<IEnumerable<int>> QueryIdsAsync(string predicate);
        [OperationContract]
        Task<IdReply> HasDataBySurvey(string surveyId, CallContext context = default);

		[OperationContract]
		Task<CountReply> GetRecordCount(Empty empty, CallContext context = default);
        [OperationContract]
        Task<LCMS_Concrete_Joints> UpdateGenericData(string fieldsToUpdateSerialized);
    }
}
