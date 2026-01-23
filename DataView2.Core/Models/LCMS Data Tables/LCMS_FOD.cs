using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using DataView2.Core.Models.LCMS_Data_Tables;
using ProtoBuf.Grpc;
using System.ServiceModel;
using Google.Protobuf.WellKnownTypes;

namespace DataView2.Core.Models.Other
{
    [DataContract]
    public class LCMS_FOD : IEntity
    {
        [DataMember(Order = 1)]
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [DataMember(Order = 2)]
        public string FODID { get; set; }
        [DataMember(Order = 3)]
        public string SurveyId { get; set; }
        [DataMember(Order = 4)]
        public string SurveyName { get; set; }
        [DataMember(Order = 5)]
        public int SegmentId { get; set; }
        [DataMember(Order = 6)]
        public string Severity { get; set; }

        [DataMember(Order = 7)]
        public double Area { get; set; }

        [DataMember(Order = 8)]
        public double Volume { get; set; }
        
        [DataMember(Order = 9)]
        public double MaximumHeight { get; set; }

        [DataMember(Order = 10)]
        public double GPSLatitude { get; set; }
        [DataMember(Order = 11)]
        public double GPSLongitude { get; set; }
        [DataMember(Order = 12)]
        public double GPSAltitude { get; set; }
        [DataMember(Order = 13)]
        public double GPSTrackAngle { get; set; }
        [DataMember(Order = 14)]
        public string PavementType { get; set; } = "";
        [DataMember(Order = 15)]
        public double RoundedGPSLatitude { get; set; }
        [DataMember(Order = 16)]
        public double RoundedGPSLongitude { get; set; }
        [DataMember(Order = 17)]
        [Column(TypeName = "datetime")]
        public DateTime DetectionDate { get; set; }
        [DataMember(Order = 18)]
        [Column(TypeName = "datetime")]
        public DateTime? RecoveryDate { get; set; }
        [DataMember(Order = 19)]
        public string FODDescription { get; set; } = "Not defined";
        [DataMember(Order = 20)]
        public string GeoJSON { get; set; } = "DefaultGeoJSON";

        [DataMember(Order = 21)]
        public string Operator { get; set; }

        [DataMember(Order = 22)]
        public string ImageFile { get; set; }

        [DataMember(Order = 23)]
        public double AverageHeight { get; set; }

        [DataMember(Order = 24)]
        public string Status { get; set; } = "Detected";

        [DataMember(Order = 25)]
        public string Comments { get; set; }

        [DataMember(Order = 26)]
        public string ReasonNoRecovery{ get; set; }

        [DataMember(Order = 27)]
        public double FODWidth_mm { get; set; } = 0;
        [DataMember(Order = 28)]
        public double FODLength_mm { get; set; } = 0;

        [DataMember(Order = 29)]
        public double Chainage { get; set; } = 0.0;
    }

    [DataContract]
    public class FODRequest
    {
        [DataMember(Order = 1)]
        public List<string> Paths { get; set; }
        [DataMember(Order = 2)]
        public string Version { get; set; }
    }

    [ServiceContract]
    public interface IFODService
    {
        [OperationContract]
        Task<IdReply> ProcessFOD(FODRequest request, CallContext context = default);
        [OperationContract]
        Task<List<LCMS_FOD>> GetBySurveyAndSegment(SurveyAndSegmentRequest request);
        [OperationContract]
        Task<List<LCMS_FOD>> GetBySurvey(SurveyRequest request);
        [OperationContract]
        Task<IdReply> HasData(Empty empty, CallContext context = default);
        [OperationContract]
        Task<List<LCMS_FOD>> GetAll(Empty empty, CallContext context = default);
        [OperationContract]
        Task<LCMS_FOD> GetById(IdRequest request, CallContext context = default);
        [OperationContract]
        Task<IdReply> HasDataBySurvey(string surveyId, CallContext context = default);
        [OperationContract]
        Task<IdReply> DeleteObject(LCMS_FOD request, CallContext context = default);
        [OperationContract]
        Task<IEnumerable<LCMS_FOD>> QueryAsync(string predicate);
        [OperationContract]
        Task<CountReply> GetCountAsync(string sqlQuery);
        [OperationContract]
        Task<CountReply> GetRecordCount(Empty empty, CallContext context = default);
        [OperationContract]
        Task<LCMS_FOD> EditValue(LCMS_FOD request, CallContext context = default);
        [OperationContract]
        Task<LCMS_FOD> UpdateGenericData(string fieldsToUpdateSerialized);
    }
}
