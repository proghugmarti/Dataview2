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
    public class LCMS_Texture_Processed: IEntity
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
        public int TextureId { get; set; }
        [DataMember(Order = 9)]
        public double? MTDBand1 { get; set; }
        [DataMember(Order = 10)]
        public double? MTDBand2 { get; set; }
        [DataMember(Order = 11)]
        public double? MTDBand3 { get; set; }
        [DataMember(Order = 12)]
        public double? MTDBand4 { get; set; }
        [DataMember(Order = 13)]
        public double? MTDBand5 { get; set; }
        [DataMember(Order = 14)]
        public double? AvgMTD { get; set; }
        [DataMember(Order = 15)]
        public double? SMTDBand1 { get; set; }
        [DataMember(Order = 16)]
        public double? SMTDBand2 { get; set; }
        [DataMember(Order = 17)]
        public double? SMTDBand3 { get; set; }
        [DataMember(Order = 18)]
        public double? SMTDBand4 { get; set; }
        [DataMember(Order = 19)]
        public double? SMTDBand5 { get; set; }
        [DataMember(Order = 20)]
        public double? AvgSMTD { get; set; }
        [DataMember(Order = 21)]
        public double? MPDBand1 { get; set; }
        [DataMember(Order = 22)]
        public double? MPDBand2 { get; set; }
        [DataMember(Order = 23)]
        public double? MPDBand3 { get; set; }
        [DataMember(Order = 24)]
        public double? MPDBand4 { get; set; }
        [DataMember(Order = 25)]
        public double? MPDBand5 { get; set; }
        [DataMember(Order = 26)]
        public double? AvgMPD { get; set; }
        [DataMember(Order = 27)]
        public double? RMSBand1 { get; set; }
        [DataMember(Order = 28)]
        public double? RMSBand2 { get; set; }
        [DataMember(Order = 29)]
        public double? RMSBand3 { get; set; }
        [DataMember(Order = 30)]
        public double? RMSBand4 { get; set; }
        [DataMember(Order = 31)]
        public double? RMSBand5 { get; set; }
        [DataMember(Order = 32)]
        public double? AvgRMS { get; set; }
        [DataMember(Order = 33)]
        public string? ImageFileIndex { get; set; }
        [DataMember(Order = 34)]
        public double GPSLatitude { get; set; }
        [DataMember(Order = 35)]
        public double GPSLongitude { get; set; }
        [DataMember(Order = 36)]
        public double GPSAltitude { get; set; }
        [DataMember(Order = 37)]
        public double GPSTrackAngle { get; set; }
        [DataMember(Order = 38)]
        public string GeoJSON { get; set; }
        [DataMember(Order = 39)]
        public string AvgGeoJSON { get; set; }
        [DataMember(Order = 40)]
        public double RoundedGPSLatitude { get; set; }
        [DataMember(Order = 41)]
        public double RoundedGPSLongitude { get; set; }
        [DataMember(Order = 42)]
        public int SegmentId {  get; set; }
        [DataMember(Order = 43)]
        public double ChainageEnd { get; set; } = 0.0;
    }

    [ServiceContract]
    public interface IMacroTextureService
    {
        [OperationContract]
        Task<IdReply> Create(LCMS_Texture_Processed request, CallContext context = default);

        [OperationContract]
        Task<List<LCMS_Texture_Processed>> GetAll(Empty empty, CallContext context = default);

        [OperationContract]
        Task<LCMS_Texture_Processed> GetById(IdRequest request, CallContext context = default);
        [OperationContract]
        Task<LCMS_Texture_Processed> EditValue(LCMS_Texture_Processed request, CallContext context = default);
        [OperationContract]
        Task<IdReply> DeleteAll(Empty empty, CallContext context = default);
        [OperationContract]
        Task<List<LCMS_Texture_Processed>> GetBySurvey(SurveyRequest request);
        [OperationContract]
        Task<IEnumerable<LCMS_Texture_Processed>> QueryAsync(string predicate);
        [OperationContract]
        Task<CountReply> GetCountAsync(string sqlQuery);

        [OperationContract]
        Task<IdReply> HasData(Empty empty, CallContext context = default);
        [OperationContract]
        Task<IdReply> HasDataBySurvey(string surveyId, CallContext context = default);

        [OperationContract]
        Task<IdReply> DeleteObject(LCMS_Texture_Processed request, CallContext context = default);

        [OperationContract]
        Task<CountReply> GetRecordCount(Empty empty, CallContext context = default);
        [OperationContract]
        Task<LCMS_Texture_Processed> UpdateGenericData(string fieldsToUpdateSerialized);
    }
}
