using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using Google.Protobuf.WellKnownTypes;
using ProtoBuf.Grpc;
using DataView2.Core.Models.CrackClassification;
using DataView2.Core.Models.ExportTemplate;
using DataView2.Core.Models.Other;

namespace DataView2.Core.Models.LCMS_Data_Tables
{
    [DataContract]
    public class LCMS_Ravelling_Raw : IEntity
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
        public int SquareId { get; set; }   // starting bottom left to top right, only ravelling sqares included
        [DataMember(Order = 9)]
        public double SquareArea_mm2 { get; set; } // should always be 250mm x 250mm  
        [DataMember(Order = 10)]
        public int Algorithm { get; set; }
        [DataMember(Order = 11)]
        public double ALG1_RavellingIndex { get; set; } // 0-300
        [DataMember(Order = 12)]
        public double? ALG1_RPI { get; set; }  
        [DataMember(Order = 13)]
        public double? ALG1_AVC { get; set; }
        [DataMember(Order = 14)]
        public double? ALG2_RI_Percent { get; set; }
        [DataMember(Order = 15)]
        public double? RI_AREA_mm2 { get; set; }
        [DataMember(Order = 16)]
        public string? Severity { get; set; }
        [DataMember(Order = 17)]
        public string? ImageFileIndex { get; set; }
        [DataMember(Order = 18)]
        public double GPSLatitude { get; set; } = 0.0; // Set a default GPSLatitude
        [DataMember(Order = 19)]
        public double GPSLongitude { get; set; } = 0.0; // Set a default GPSLongitude
        [DataMember(Order = 20)]
        public double GPSAltitude { get; set; }
        [DataMember(Order = 21)]
        public double GPSTrackAngle { get; set; }
        [DataMember(Order = 22)]
        public string GeoJSON { get; set; }
        [DataMember(Order = 23)]
        [Column(TypeName = "boolean")]
        public bool? QCAccepted { get; set; } = false;
        [DataMember(Order = 24)]
        public double RoundedGPSLatitude { get; set; }

        [DataMember(Order = 25)]
        public double RoundedGPSLongitude { get; set; }
        [DataMember(Order = 26)]
        public int SegmentId { get; set; }
        [DataMember(Order = 27)]
        public double ChainageEnd { get; set; } = 0.0;
    }

    [ServiceContract]
    public interface IRavellingService
    {
        [OperationContract]
        Task<IdReply> Create(LCMS_Ravelling_Raw request, CallContext context = default);

        [OperationContract]
        Task<List<LCMS_Ravelling_Raw>> GetAll(Empty empty, CallContext context = default);
        [OperationContract]
        Task<LCMS_Ravelling_Raw> GetById(IdRequest request, CallContext context = default);
        [OperationContract]
        Task<LCMS_Ravelling_Raw> EditValue(LCMS_Ravelling_Raw request, CallContext context = default);

        [OperationContract]
        Task<IdReply> DeleteObject(LCMS_Ravelling_Raw request,
            CallContext context = default);

        [OperationContract]
        Task<IdReply> DeleteValue(IdRequest request, CallContext context = default);
        [OperationContract]
        Task<IEnumerable<LCMS_Ravelling_Raw>> QueryAsync(string predicate);
        [OperationContract]
        Task<CountReply> GetCountAsync(string sqlQuery);
        [OperationContract]
        Task<IdReply> HasData(Empty empty, CallContext context = default);
        [OperationContract]
        Task<List<LCMS_Ravelling_Raw>> GetWithinRange(CoordinateRangeRequest coordinateRequest);

        [OperationContract]
        Task<List<LCMS_Ravelling_Raw>> GetBySurveyAndSegment(SurveyAndSegmentRequest request);

        [OperationContract]
        Task<List<LCMS_Ravelling_Raw>> GetBySurvey(SurveyRequest request);

        [OperationContract]
        Task<IEnumerable<int>> QueryIdsAsync(string predicate);
        [OperationContract]
        Task<IdReply> HasDataBySurvey(string surveyId, CallContext context = default);

		[OperationContract]
		Task<CountReply> GetRecordCount(Empty empty, CallContext context = default);
        [OperationContract]
        Task<LCMS_Ravelling_Raw> UpdateGenericData(string fieldsToUpdateSerialized);
    }
}
