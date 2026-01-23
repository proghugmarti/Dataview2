using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
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
    public class LCMS_Corner_Break: IEntity
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
        public long? LRPNumStart { get; set; }
        [DataMember(Order = 6)]
        public double? LRPChainageStart { get; set; }
        [DataMember(Order = 7)]
        public string PavementType { get; set; }
        [DataMember(Order = 8)]
        public int CornerId { get; set; }
        [DataMember(Order = 9)]
        public int QuarterId { get; set; }
        [DataMember(Order = 10)]
        public double AvgDepth_mm { get; set; }
        [DataMember(Order = 11)]
        public double Area_mm2 { get; set; }
        [DataMember(Order = 12)]            
        public double BreakArea_mm2 { get; set; }
        [DataMember(Order = 13)]
        public double CNR_SpallingArea_mm2 { get; set; }
        [DataMember(Order = 14)]
        public double AreaRatio { get; set; }
        [DataMember(Order = 15)]
        public string ImageFileIndex { get; set; }
        [DataMember(Order = 16)]
        public double GPSLatitude { get; set; }
        [DataMember(Order = 17)]
        public double GPSLongitude { get; set; }
        [DataMember(Order = 18)]
        public double GPSAltitude { get; set; }
        [DataMember(Order = 19)]
        public double GPSTrackAngle { get; set; }
        [DataMember(Order = 20)]
        public string GeoJSON { get; set; }
        [DataMember(Order = 21)]
        [Column(TypeName = "boolean")]
        public bool? QCAccepted { get; set; }=false;

        [DataMember(Order = 22)]
        public double RoundedGPSLatitude { get; set; }

        [DataMember(Order = 23)]
        public double RoundedGPSLongitude { get; set; }
        [DataMember(Order = 24)]
        public int SegmentId { get; set; }
        [DataMember(Order = 25)]
        public double ChainageEnd { get; set; } = 0.0;
    }

    [DataContract]
    public class LCMS_Corner_Break_Columns
    {
        [DataMember(Order = 1)]
        [Key]
        public string Key { get; set; }
        [DataMember(Order = 2)]
        public Dictionary<string, string> FieldsToUpdate { get; set; }      
    }
    [DataContract]
    public class LCMS_Corner_Break_Columns1
    {
        [DataMember(Order = 1)]
        [Key]
        public string Key { get; set; }
    }

    [ServiceContract]
    public interface ICornerBreakService
    {
        [OperationContract]
        Task<IdReply> Create(LCMS_Corner_Break request, CallContext context = default);

        [OperationContract]
        Task<List<LCMS_Corner_Break>> GetAll(Empty empty, CallContext context = default);

        [OperationContract]
        Task<LCMS_Corner_Break> GetById(IdRequest request, CallContext context = default);
        [OperationContract]
        Task<LCMS_Corner_Break> EditValue(LCMS_Corner_Break request, CallContext context = default);
        [OperationContract]
        Task<LCMS_Corner_Break> UpdateQueryAsync(LCMS_Corner_Break_Columns request, CallContext context = default);

        [OperationContract]
        Task<IdReply> DeleteObject(LCMS_Corner_Break request,
            CallContext context = default);
        [OperationContract]
        Task<IEnumerable<LCMS_Corner_Break>> QueryAsync(string predicate);

        [OperationContract]
        Task<CountReply> GetCountAsync(string sqlQuery);
        [OperationContract]
        Task<IdReply> HasData(Empty empty, CallContext context = default);
        [OperationContract]
        Task<List<LCMS_Corner_Break>> GetWithinRange(CoordinateRangeRequest coordinateRequest);

        [OperationContract]
        Task<List<LCMS_Corner_Break>> GetBySurveyAndSegment(SurveyAndSegmentRequest request);

        [OperationContract]
        Task<List<LCMS_Corner_Break>> GetBySurvey(SurveyRequest request);

        [OperationContract]
        Task<IdReply> DeleteValue(IdRequest request, CallContext context = default);
        [OperationContract]
        Task<IdReply> HasDataBySurvey(string surveyId, CallContext context = default);

		[OperationContract]
		Task<CountReply> GetRecordCount(Empty empty, CallContext context = default);
        [OperationContract]
        Task<LCMS_Corner_Break> UpdateGenericData(string fieldsToUpdateSerialized);
    }
}
