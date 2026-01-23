
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using Google.Protobuf.WellKnownTypes;
using ProtoBuf.Grpc;
using System.ServiceModel;
using DataView2.Core.Models.CrackClassification;
using DataView2.Core.Models.ExportTemplate;

namespace DataView2.Core.Models.LCMS_Data_Tables
{
    [DataContract]
    public class LCMS_Bleeding : IEntity
    {
        [DataMember(Order = 1)]
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [DataMember(Order = 2)]
        public string? SurveyId { get; set; }

        [DataMember(Order = 3)]
        [Column(TypeName = "datetime")]
        public DateTime? SurveyDate { get; set; }

        [DataMember(Order = 4)]
        public long? LRPNumStart { get; set; }

        [DataMember(Order = 5)]
        public float? LRPChainageStart { get; set; }

        [DataMember(Order = 6)]
        public long? LRPNumEnd { get; set; }

        [DataMember(Order = 7)]
        public float? LRPChainageEnd { get; set; }
        [DataMember(Order = 10)]
        public string PavementType { get; set; }

        [DataMember(Order = 11)]
        public int BleedingId { get; set; }

        [DataMember(Order = 12)]
        public double? LeftBleedingIndex { get; set; }

        [DataMember(Order = 13)]
        public string? LeftSeverity { get; set; }

        [DataMember(Order = 14)]
        public double? RightBleedingIndex { get; set; }

        [DataMember(Order = 15)]
        public string? RightSeverity { get; set; }

        [DataMember(Order = 16)]
        public string? ImageFileIndex { get; set; }

        [DataMember(Order = 17)]
        public double GPSLatitude { get; set; } = 0.0; //left 

        [DataMember(Order = 18)]
        public double GPSLongitude { get; set; } = 0.0; // left
        [DataMember(Order = 19)]
        public double GPSRightLatitude { get; set; } = 0.0; //right
        [DataMember(Order = 20)]
        public double GPSRightLongitude { get; set; } = 0.0; //right

        [DataMember(Order = 21)]
        public double GPSAltitude { get; set; }
        [DataMember(Order = 22)]
        public double GPSTrackAngle { get; set; }
        [DataMember(Order = 23)]
        public string GeoJSON { get; set; }

        [DataMember(Order = 24)]
        public double RoundedGPSLatitude { get; set; }

        [DataMember(Order = 25)]
        public double RoundedGPSLongitude { get; set; }
        [DataMember(Order = 26)]
        public int SegmentId { get; set; }
        [DataMember(Order = 27)]
        public double Area_m2 { get; set; }
        [DataMember(Order = 28)]
        public double Chainage { get; set; } // Chainage of the segment where the bleeding is located

        [DataMember(Order = 29)]
        public double LeftArea_m2 { get; set; }
        [DataMember(Order = 30)]
        public double RightArea_m2 { get; set; }
        [DataMember(Order = 31)]
        public double ChainageEnd { get; set; } = 0.0;
    }


    [ServiceContract]
    public interface IBleedingService
    {
        [OperationContract]
        Task<IdReply> Create(LCMS_Bleeding request,
            CallContext context = default);

        [OperationContract]
        Task<List<LCMS_Bleeding>> GetAll(Empty empty,
            CallContext context = default);

        [OperationContract]
        Task<LCMS_Bleeding> GetById(IdRequest request, CallContext context = default);
        [OperationContract]
        Task<LCMS_Bleeding> EditValue(LCMS_Bleeding request,
            CallContext context = default);

        [OperationContract]
        Task<IdReply> DeleteAll(Empty empty, 
            CallContext context = default);

        [OperationContract]
        Task<IdReply> DeleteValue(IdRequest request,
         CallContext context = default);

        [OperationContract]
        Task<IdReply> DeleteObject(LCMS_Bleeding request,
            CallContext context = default);

        [OperationContract]
        Task<IEnumerable<LCMS_Bleeding>> QueryAsync(string predicate);
        [OperationContract]
        Task<CountReply> GetCountAsync(string sqlQuery);

        [OperationContract]
		Task<CountReply> GetRecordCount(Empty empty, CallContext context = default);
        [OperationContract]
        Task<List<LCMS_Bleeding>> GetBySurvey(SurveyRequest request);
        [OperationContract]
        Task<IdReply> HasData(Empty empty, CallContext context = default);
        [OperationContract]
        Task<IdReply> HasDataBySurvey(string surveyId, CallContext context = default);
        [OperationContract]
        Task<LCMS_Bleeding> UpdateGenericData(string fieldsToUpdateSerialized);
    }
}
