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
    public class LCMS_Grooves : IEntity
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
        public long? LRPNumber { get; set; }
        [DataMember(Order = 6)]
        public double? LRPChainage { get; set; }
        [DataMember(Order = 7)]
        public string PavementType { get; set; }
        [DataMember(Order = 8)]
        public int ZoneId { get; set; }
        [DataMember(Order = 9)]
        public double ZoneArea_mm2 { get; set; }
        [DataMember(Order = 10)]
        public double AvgDepth_mm { get; set; }
        [DataMember(Order = 11)]
        public double AvgWidth_mm { get; set; }
        [DataMember(Order = 12)]
        public double AvgInterval_mm { get; set; }
        [DataMember(Order = 13)]
        public string ImageFileIndex { get; set; }
        [DataMember(Order = 14)]
        public double GPSLatitude { get; set; }
        [DataMember(Order = 15)]
        public double GPSLongitude { get; set; }
        [DataMember(Order = 16)]
        public double GPSAltitude { get; set; }
        [DataMember(Order = 17)]
        public double GPSTrackAngle { get; set; }
        [DataMember(Order = 18)]
        public string GeoJSON { get; set; }
        [DataMember(Order = 19)]
        public double RoundedGPSLatitude { get; set; }
        [DataMember(Order = 20)]
        public double RoundedGPSLongitude { get; set; }
        [DataMember(Order = 21)]
        public int SegmentId { get; set; }
        [DataMember(Order = 22)]
        public double ChainageEnd { get; set; } = 0.0;
    }

    [ServiceContract]
    public interface IGroovesService
    {
        [OperationContract]
        Task<IdReply> Create(LCMS_Grooves request, CallContext context = default);

        [OperationContract]
        Task<List<LCMS_Grooves>> GetAll(Empty empty, CallContext context = default);

        [OperationContract]
        Task<LCMS_Grooves> GetById(IdRequest request, CallContext context = default);
        [OperationContract]
        Task<LCMS_Grooves> EditValue(LCMS_Grooves request, CallContext context = default);
        [OperationContract]
        Task<IdReply> DeleteAll(Empty empty, CallContext context = default);
        [OperationContract]
        Task<List<LCMS_Grooves>> GetBySurvey(SurveyRequest request);
        [OperationContract]
        Task<IEnumerable<LCMS_Grooves>> QueryAsync(string predicate);
        [OperationContract]
        Task<CountReply> GetCountAsync(string sqlQuery);

        [OperationContract]
        Task<IdReply> HasData(Empty empty, CallContext context = default);
        [OperationContract]
        Task<IdReply> HasDataBySurvey(string surveyId, CallContext context = default);
        [OperationContract]
        Task<IdReply> DeleteObject(LCMS_Grooves request, CallContext context = default);

        [OperationContract]
        Task<CountReply> GetRecordCount(Empty empty, CallContext context = default);
        [OperationContract]
        Task<LCMS_Grooves> UpdateGenericData(string fieldsToUpdateSerialized);
    }
}
