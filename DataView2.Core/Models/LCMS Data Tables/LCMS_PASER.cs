using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using ProtoBuf.Grpc;
using System.ServiceModel;
using Google.Protobuf.WellKnownTypes;

namespace DataView2.Core.Models.LCMS_Data_Tables
{
    [DataContract]
    public class LCMS_PASER: IEntity
    {
        [DataMember(Order = 1)]
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [DataMember(Order = 2)]
        public string? SurveyId { get; set; }
        [DataMember(Order = 3)]
        public int SegmentId { get; set; }
        [DataMember(Order = 4)]
        public string? PavementType { get; set; }
        [DataMember(Order = 5)]
        public double PaserRating { get; set; }
        [DataMember(Order = 8)]
        public string GeoJSON { get; set; }
        [DataMember(Order = 9)]
        public double GPSLatitude { get; set; }
        [DataMember(Order = 10)]
        public double GPSLongitude { get; set; }
        [DataMember(Order = 11)]
        public double GPSAltitude { get; set; }
        [DataMember(Order = 12)]
        public double GPSTrackAngle { get; set; }
        [DataMember(Order = 13)]
        public double RoundedGPSLatitude { get; set; } = 0.0;
        [DataMember(Order = 14)]
        public double RoundedGPSLongitude { get; set; } = 0.0;
        [DataMember (Order = 15)]
        public double Chainage { get; set; }
        [DataMember(Order = 16)]
        public double ChainageEnd { get; set; } = 0.0;
    }

    [ServiceContract]
    public interface IPASERService
    {
        [OperationContract]
        Task<IdReply> Create(LCMS_PASER request, CallContext context = default);

        [OperationContract]
        Task<List<LCMS_PASER>> GetAll(Empty empty, CallContext context = default);

        [OperationContract]
        Task<LCMS_PASER> GetById(IdRequest request, CallContext context = default);

        [OperationContract]
        Task<LCMS_PASER> EditValue(LCMS_PASER request, CallContext context = default);

        [OperationContract]
        Task<IdReply> DeleteAll(Empty empty, CallContext context = default);

        [OperationContract]
        Task<List<LCMS_PASER>> GetBySurvey(SurveyRequest request);

        [OperationContract]
        Task<IEnumerable<LCMS_PASER>> QueryAsync(string predicate);

        [OperationContract]
        Task<IdReply> HasData(Empty empty, CallContext context = default);

        [OperationContract]
        Task<IdReply> HasDataBySurvey(string surveyId, CallContext context = default);
    }
}
