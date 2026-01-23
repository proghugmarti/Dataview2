using DataView2.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using ProtoBuf.Grpc;
using System.ServiceModel;
using DataView2.Core.Models.CrackClassification;
using DataView2.Core.Models.ExportTemplate;
using DataView2.Core.Models.Other;

namespace DataView2.Core.Models.LCMS_Data_Tables
{
    [DataContract]
    public class LCMS_Cracking_Raw : IEntity
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
        public int? CrackId { get; set; }

        [DataMember(Order = 9)]
        public int? NodeId { get; set; }

        [DataMember(Order = 10)]
        public double? NodeLength_mm { get; set; }

        [DataMember(Order = 11)]
        public double? NodeWidth_mm { get; set; }

        [DataMember(Order = 12)]
        public double? NodeDepth_mm { get; set; }

        [DataMember(Order = 13)]
        public string? Severity { get; set; }

        [DataMember(Order = 14)]
        public string? ImageFileIndex { get; set; }

        [DataMember(Order = 15)]
        public double GPSLatitude { get; set; } = 0.0; //start GPSLatitude

        [DataMember(Order = 16)]
        public double GPSLongitude { get; set; } = 0.0; //start GPSLongitude
        [DataMember(Order = 17)]
        public double GPSAltitude { get; set; } = 0.0;
        [DataMember(Order = 18)]
        public double GPSTrackAngle { get; set; }
        [DataMember(Order = 19)]
        public double EndGPSLatitude { get; set; }
        [DataMember(Order = 20)]
        public double EndGPSLongitude { get; set; }
        [DataMember(Order = 21)]
        public string GeoJSON { get; set; }
        [DataMember(Order = 22)]
        [Column(TypeName = "boolean")]
        public bool? QCAccepted { get; set; } = false;

        [DataMember(Order = 23)]
        public double RoundedGPSLatitude { get; set; }

        [DataMember(Order = 24)]
        public double RoundedGPSLongitude { get; set; }
        [DataMember(Order = 25)]
        public int SegmentId { get; set; }
        [DataMember(Order = 26)]
        public double? Faulting { get; set; }
        [DataMember(Order = 27)]
        public double ChainageEnd { get; set; } = 0.0;
    }

    [ServiceContract]
    public interface ICrackingRawService
    {
        [OperationContract]
        Task<IdReply> Create(LCMS_Cracking_Raw request, CallContext context = default);

        [OperationContract]
        Task<List<LCMS_Cracking_Raw>> GetAll(Empty empty, CallContext context = default);

        [OperationContract]
        Task<LCMS_Cracking_Raw> GetById(IdRequest request, CallContext context = default);
        [OperationContract]
        Task<LCMS_Cracking_Raw> EditValue(LCMS_Cracking_Raw request, CallContext context = default);

        [OperationContract]
        Task<IdReply> DeleteAll(Empty empty, CallContext context = default);

        [OperationContract]
        Task<IdReply> DeleteValue(IdRequest request, CallContext context = default);

        [OperationContract]
        Task<IdReply> DeleteObject(LCMS_Cracking_Raw request,
            CallContext context = default);

        [OperationContract]
        Task<IEnumerable<LCMS_Cracking_Raw>> QueryAsync(string predicate);

        [OperationContract]
        Task<CountReply> GetCountAsync(string sqlQuery);
        [OperationContract]
        Task<IdReply> HasData(Empty empty, CallContext context = default);

        [OperationContract]
        Task<List<LCMS_Cracking_Raw>> GetBySurveyAndSegment(SurveyAndSegmentRequest request);

        [OperationContract]
        Task<List<LCMS_Cracking_Raw>> GetBySurvey(SurveyRequest request);

        [OperationContract]
        Task<IEnumerable<int>> QueryIdsAsync(string predicate);
        [OperationContract]
        Task<IdReply> HasDataBySurvey(string surveyId, CallContext context = default);

		[OperationContract]
		Task<CountReply> GetRecordCount(Empty empty, CallContext context = default);
        [OperationContract]
        Task<LCMS_Cracking_Raw> UpdateGenericData(string fieldsToUpdateSerialized);
    }
}
