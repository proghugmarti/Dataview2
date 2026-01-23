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
    public class LCMS_Geometry_Processed : IEntity
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
        public double? Time { get; set; }
        [DataMember(Order = 9)]
        public double? Roll { get; set; }
        [DataMember(Order = 10)]
        public double? Pitch { get; set; }
        [DataMember(Order = 11)]
        public double? Yaw { get; set; }
        [DataMember(Order = 12)]
        public double? Vel_X { get; set; }
        [DataMember(Order = 13)]
        public double? Vel_Y { get; set; }
        [DataMember(Order = 14)]
        public double? Vel_Z { get; set; }
        [DataMember(Order = 15)]
        public int? Count { get; set; }
        [DataMember(Order = 16)]
        public int? Timestamp { get; set; }
        [DataMember(Order = 17)]
        public string? Status { get; set; }
        [DataMember(Order = 18)]
        public double? Acc_X { get; set; }
        [DataMember(Order = 19)]
        public double? Acc_Y { get; set; }
        [DataMember(Order = 20)]
        public double? Acc_Z { get; set; }
        [DataMember(Order = 21)]
        public double? Gyr_X { get; set; }
        [DataMember(Order = 22)]
        public double? Gyr_Y { get; set; }
        [DataMember(Order = 23)]
        public double? Gyr_Z { get; set; }
        [DataMember(Order = 24)]
        public double Slope { get; set; }
        [DataMember(Order = 25)]
        public string StatesOfSlope{ get; set; }
        [DataMember(Order = 26)]
        public double CrossSlope { get; set; }
        [DataMember(Order = 27)]
        public string StatesOfCrossSlope{ get; set; }
        [DataMember(Order = 28)]
        public double RadiusOfCurvature { get; set; }
        [DataMember(Order = 29)]
        public string? ImageFileIndex { get; set; }
        [DataMember(Order = 30)]
        public double GPSLatitude { get ; set ; } //Start
        [DataMember(Order = 31)]
        public double GPSLongitude { get ; set ; } //Start
        [DataMember(Order = 32)]
        public double GPSAltitude { get; set; }
        [DataMember(Order = 33)]
        public double GPSTrackAngle { get; set; }
        [DataMember(Order = 34)]
        public double EndGPSLatitude { get; set; }
        [DataMember(Order = 35)]
        public double EndGPSLongitude { get; set; }
        [DataMember(Order = 36)]
        public string GeoJSON { get; set; }
        [DataMember(Order = 37)]
        public double RoundedGPSLatitude { get ; set ; }
        [DataMember(Order = 38)]
        public double RoundedGPSLongitude { get ; set ; }
        [DataMember(Order = 39)]
        public int SegmentId { get; set; }
        [DataMember(Order = 40)]
        public double ChainageEnd { get; set; } = 0.0;
    }

    [ServiceContract]
    public interface IGeometryService
    {
        [OperationContract]
        Task<IdReply> Create(LCMS_Geometry_Processed request, CallContext context = default);

        [OperationContract]
        Task<List<LCMS_Geometry_Processed>> GetAll(Empty empty, CallContext context = default);

        [OperationContract]
        Task<LCMS_Geometry_Processed> GetById(IdRequest request, CallContext context = default);
        [OperationContract]
        Task<LCMS_Geometry_Processed> EditValue(LCMS_Geometry_Processed request, CallContext context = default);
        [OperationContract]
        Task<CountReply> GetRecordCount(Empty empty, CallContext context = default);
        [OperationContract]
        Task<IdReply> DeleteObject(LCMS_Geometry_Processed request, CallContext context = default);
        [OperationContract]
        Task<IdReply> DeleteAll(Empty empty, CallContext context = default);
        [OperationContract]
        Task<List<LCMS_Geometry_Processed>> GetBySurvey(SurveyRequest request);
        [OperationContract]
        Task<IEnumerable<LCMS_Geometry_Processed>> QueryAsync(string predicate);
        [OperationContract]
        Task<CountReply> GetCountAsync(string sqlQuery);

        [OperationContract]
        Task<IdReply> HasData(Empty empty, CallContext context = default);
        [OperationContract]
        Task<IdReply> HasDataBySurvey(string surveyId, CallContext context = default);
        [OperationContract]
        Task<LCMS_Geometry_Processed> UpdateGenericData(string fieldsToUpdateSerialized);
    }
}
