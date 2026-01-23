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
    [DataContract]
    public class LCMS_Rough_Processed: IEntity
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
        public long? LRPNumber{ get; set; }
        [DataMember(Order = 6)]
        public double? LRPChainage { get; set; }
        [DataMember(Order = 7)]
        public string PavementType { get; set; }
        [DataMember(Order = 8)]
        public int RoughnessId { get; set; }
        [DataMember(Order = 9)]
        public double Speed { get; set; }
        [DataMember(Order = 10)]
        public double LwpIRI { get; set; } // Left wheel path IRI
        [DataMember(Order = 11)]
        public double RwpIRI { get; set; } // Right wheel path IRI 
        [DataMember(Order = 12)]
        public double? CwpIRI { get; set; } // Center wheel path IRI
        [DataMember(Order = 13)]
        public double LaneIRI { get; set; }
        [DataMember(Order = 14)]
        public double Naasra { get; set; }
        [DataMember(Order = 15)]
        public double LongitudinalPositionY { get; set; }
        [DataMember(Order = 16)]
        public double Interval { get; set; }
        [DataMember(Order = 17)]
        public string ImageFileIndex { get; set; }
        [DataMember(Order = 18)]
        public double GPSLatitude { get; set; } //Start
        [DataMember(Order = 19)]
        public double GPSLongitude { get; set; } //Start
        [DataMember(Order = 20)]
        public double GPSAltitude { get; set; }
        [DataMember(Order = 21)]
        public double GPSTrackAngle { get; set; }
        [DataMember(Order = 22)]
        public double EndGPSLatitude { get; set; }
        [DataMember(Order = 23)]
        public double EndGPSLongitude { get; set; }
        [DataMember(Order = 24)]
        public string GeoJSON { get; set; } // LaneIRI
        [DataMember(Order = 25)]
        public string LwpGeoJSON { get; set; }
        [DataMember(Order = 26)]
        public string RwpGeoJSON { get; set; }
        [DataMember(Order = 27)]
        public string? CwpGeoJSON { get; set; }
        [DataMember(Order = 28)]
        public double RoundedGPSLatitude { get; set; }
        [DataMember(Order = 29)]
        public double RoundedGPSLongitude { get; set; }
        [DataMember(Order = 30)]
        public int SegmentId { get; set; }
        [DataMember(Order = 31)]
        public double ChainageEnd { get; set; } = 0.0;
    }

    [DataContract]
    public class ChainagePoints
    {
        [DataMember(Order = 1)]
        public double StartChainage { get; set; }
        [DataMember(Order = 2)]
        public double EndChainage { get; set; }
    }

    [ServiceContract]
    public interface IRoughnessService
    {
        [OperationContract]
        Task<IdReply> Create(LCMS_Rough_Processed request, CallContext context = default);

        [OperationContract]
        Task<List<LCMS_Rough_Processed>> GetAll(Empty empty, CallContext context = default);

        [OperationContract]
        Task<LCMS_Rough_Processed> GetById(IdRequest request, CallContext context = default);
        
        [OperationContract]
        Task<LCMS_Rough_Processed> EditValue(LCMS_Rough_Processed request, CallContext context = default);
        
        [OperationContract]
        Task<IdReply> DeleteAll(Empty empty, CallContext context = default);

        [OperationContract]
        Task<List<LCMS_Rough_Processed>> GetBySurvey(SurveyRequest request);

        [OperationContract]
        Task<IEnumerable<LCMS_Rough_Processed>> QueryAsync(string predicate);
        [OperationContract]
        Task<CountReply> GetCountAsync(string sqlQuery);

        [OperationContract]
        Task<IdReply> HasData(Empty empty, CallContext context = default);

        [OperationContract]
        Task<IdReply> HasCwpIRI(Empty empty, CallContext ctx = default);
        [OperationContract]
        Task<IdReply> HasDataBySurvey(string surveyId, CallContext context = default);

        [OperationContract]
        Task<IdReply> DeleteObject(LCMS_Rough_Processed request, CallContext context = default);

        [OperationContract]
        Task<CountReply> GetRecordCount(Empty empty, CallContext context = default);
        [OperationContract]
        Task<LCMS_Rough_Processed> UpdateGenericData(string fieldsToUpdateSerialized);
        [OperationContract]
        Task<List<LCMS_Rough_Processed>> GetBetweenChainages(ChainagePoints request, CallContext context = default);
    }
}
