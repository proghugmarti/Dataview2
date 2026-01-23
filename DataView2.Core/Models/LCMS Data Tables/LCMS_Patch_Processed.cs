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
    public class LCMS_Patch_Processed: IEntity
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
        public int? LRPNumStart { get; set; }
        [DataMember(Order = 6)]
        public double? LRPChainage { get; set; }
        [DataMember(Order = 7)]
        public string PavementType { get; set; }
        [DataMember(Order = 8)]
        public long PatchId { get; set; }
        [DataMember(Order = 9)]
        public double Length_mm { get; set; }
        [DataMember(Order = 10)]
        public double Width_mm { get; set; }
        [DataMember(Order = 11)]     
        public double Area_m2 { get; set; }//m^2
        [DataMember(Order = 12)]
        public string Severity { get; set; }
        [DataMember(Order = 13)]
        public double ConfidenceLevel { get; set; }
        [DataMember(Order = 14)]
        public string ImageFileIndex { get; set; }
        [DataMember(Order = 15)]
        public double GPSLatitude { get; set; }
        [DataMember(Order = 16)]
        public double GPSLongitude { get; set; }
        [DataMember(Order = 17)]
        public double GPSAltitude { get; set; }
        [DataMember(Order = 18)]
        public double GPSTrackAngle { get; set; }
        [DataMember(Order = 19)]
        public string GeoJSON { get ; set ; }
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
    public interface IPatchService
    {
        [OperationContract]
        Task<IdReply> Create(LCMS_Patch_Processed request, CallContext context = default);

        [OperationContract]
        Task<List<LCMS_Patch_Processed>> GetAll(Empty empty, CallContext context = default);
        [OperationContract]
        Task<LCMS_Patch_Processed> GetById(IdRequest request, CallContext context = default);
        [OperationContract]
        Task<LCMS_Patch_Processed> EditValue(LCMS_Patch_Processed request, CallContext context = default);

        [OperationContract]
        Task<IdReply> DeleteObject(LCMS_Patch_Processed request,
            CallContext context = default);

        [OperationContract]
        Task<IdReply> DeleteValue(IdRequest request, CallContext context = default);

        [OperationContract]
        Task<IEnumerable<LCMS_Patch_Processed>> QueryAsync(string predicate);
        [OperationContract]
        Task<CountReply> GetCountAsync(string sqlQuery);
        [OperationContract]
        Task<IdReply> HasData(Empty empty, CallContext context = default);
        [OperationContract]
        Task<List<LCMS_Patch_Processed>> GetWithinRange(CoordinateRangeRequest coordinateRequest);

        [OperationContract]
        Task<List<LCMS_Patch_Processed>> GetBySurveyAndSegment(SurveyAndSegmentRequest request);

        [OperationContract]
        Task<List<LCMS_Patch_Processed>> GetBySurvey(SurveyRequest request);

        [OperationContract]
        Task<IEnumerable<int>> QueryIdsAsync(string predicate);
        [OperationContract]
        Task<IdReply> HasDataBySurvey(string surveyId, CallContext context = default);

		[OperationContract]
		Task<CountReply> GetRecordCount(Empty empty, CallContext context = default);
        [OperationContract]
        Task<LCMS_Patch_Processed> UpdateGenericData(string fieldsToUpdateSerialized);
    }
}
