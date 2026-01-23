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

namespace DataView2.Core.Models.LCMS_Data_Tables
{
    [DataContract]
    public class LCMS_CrackSummary : IEntity
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
        public int CrackId { get; set; }

        [DataMember(Order = 9)]
        public double CrackLength_mm { get; set; }

        [DataMember(Order = 10)]
        public double WeightedDepth_mm { get; set; }

        [DataMember(Order = 11)]
        public double WeightedWidth_mm { get; set; }

        [DataMember(Order = 12)]
        public double Faulting { get; set; }

        [DataMember(Order = 13)]
        public string Severity { get; set; }

        [DataMember(Order = 14)]
        public string? ImageFileIndex { get; set; }

        [DataMember(Order = 15)]
        public double GPSLatitude { get; set; } = 0.0; //Start

        [DataMember(Order = 16)]
        public double GPSLongitude { get; set; } = 0.0; //Start

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
        public int SegmentId { get; set; }
        [DataMember(Order = 23)]
        public double RoundedGPSLatitude { get; set; } = 0.0;
        [DataMember(Order = 24)]
        public double RoundedGPSLongitude { get; set; } = 0.0;
        [DataMember(Order = 25)]
        public string? MTQ { get; set; }
        [DataMember(Order = 26)]
        public double minX { get; set; } = 0.0;
        [DataMember(Order = 27)]
        public double minY { get; set; } = 0.0;
        [DataMember(Order = 28)]
        public double maxX { get; set; } = 0.0;
        [DataMember(Order = 29)]
        public double maxY { get; set; } = 0.0;
        [DataMember(Order = 30)]
        public double ChainageEnd { get; set; } = 0.0;
    }

    [DataContract]
    public class CrackReference
    {
        [DataMember(Order = 1)]
        public int CrackId { get; set; }
        [DataMember(Order = 2)]
        public int SegmentId { get; set; }
        [DataMember(Order = 3)]
        public string SurveyId { get; set; }

        public override bool Equals(object obj)
        {
            return obj is CrackReference other &&
                   CrackId == other.CrackId &&
                   SegmentId == other.SegmentId &&
                   SurveyId == other.SurveyId;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(CrackId, SegmentId, SurveyId);
        }
    }

    [DataContract]
    public class CrackSummaryRefreshResult
    {
        [DataMember(Order = 1)]
        public List<LCMS_CrackSummary> Updated { get; set; } = new List<LCMS_CrackSummary>();
        [DataMember(Order = 2)]
        public List<LCMS_CrackSummary> Deleted { get; set; } = new List<LCMS_CrackSummary>();
    }

    [ServiceContract]
    public interface ICrackSummaryService
    {
        [OperationContract]
        Task<IdReply> Create(LCMS_CrackSummary request, CallContext context = default);

        [OperationContract]
        Task<List<LCMS_CrackSummary>> GetAll(Empty empty, CallContext context = default);

        [OperationContract]
        Task<LCMS_CrackSummary> GetById(IdRequest request, CallContext context = default);
        [OperationContract]
        Task<LCMS_CrackSummary> EditValue(LCMS_CrackSummary request, CallContext context = default);

        [OperationContract]
        Task<IdReply> DeleteAll(Empty empty, CallContext context = default);

        [OperationContract]
        Task<IdReply> DeleteObject(LCMS_CrackSummary request,
            CallContext context = default);

        [OperationContract]
        Task<IEnumerable<LCMS_CrackSummary>> QueryAsync(string predicate);

        [OperationContract]
        Task<IdReply> HasData(Empty empty, CallContext context = default);

        [OperationContract]
        Task<List<LCMS_CrackSummary>> GetBySurvey(SurveyRequest request);
        [OperationContract]
        Task<IdReply> HasDataBySurvey(string surveyId, CallContext context = default);
        [OperationContract]
        Task<CrackSummaryRefreshResult> RefreshCrackSummaries(List<CrackReference> crackReferences);
    }
}
