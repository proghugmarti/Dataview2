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
    public class LCMS_Water_Entrapment : IEntity
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
        public double dLeftAverageWaterDepth { get; set; }
        [DataMember(Order = 6)]
        public double dLeftTotalWaterWidth { get; set; }
        [DataMember(Order = 7)]
        public double dRightAverageWaterDepth { get; set; }
        [DataMember(Order = 8)]
        public double dRightTotalWaterWidth { get; set; }
        [DataMember(Order = 9)]
        public double dWaterTrapDepth { get; set; }
        [DataMember(Order = 10)]
        public double dWaterTrapWidth { get; set; }
        [DataMember(Order = 11)]
        public double dCrossSection { get; set; }
        [DataMember(Order = 12)]
        public double dStraightEdgeCoordsPoint1X { get; set; }
        [DataMember(Order = 13)]
        public double dStraightEdgeCoordsPoint1Z { get; set; }
        [DataMember(Order = 14)]
        public double dStraightEdgeCoordsPoint2X { get; set; }
        [DataMember(Order = 15)]
        public double dStraightEdgeCoordsPoint2Z { get; set; }
        [DataMember(Order = 16)]
        public double dMidLineCoordsPoint1X { get; set; }
        [DataMember(Order = 17)]
        public double dMidLineCoordsPoint1Z { get; set; }
        [DataMember(Order = 18)]
        public double dMidLineCoordsPoint2X { get; set; }
        [DataMember(Order = 19)]
        public double dMidLineCoordsPoint2Z { get; set; }
        [DataMember(Order = 20)]

        public int SegmentId { get; set; }
        [DataMember(Order = 21)]
        public string GeoJSON { get; set; }
        [DataMember(Order = 22)]
        public double GPSLatitude { get; set; }
        [DataMember(Order = 23)]
        public double GPSLongitude { get; set; }
        [DataMember(Order = 24)]
        public double GPSAltitude { get; set; }
        [DataMember(Order = 25)]
        public double GPSTrackAngle { get; set; }
        [DataMember(Order = 26)]
        public string PavementType { get; set; }
        [DataMember(Order = 27)]
        public double RoundedGPSLatitude { get; set; }
        [DataMember(Order = 28)]
        public double RoundedGPSLongitude { get; set; }
        [DataMember(Order = 29)]
        public double ChainageEnd { get; set; } = 0.0;

    }

    [ServiceContract]
    public interface IWaterTrapService
    {
        [OperationContract]
        Task<IdReply> Create(LCMS_Water_Entrapment request, CallContext context = default);

        [OperationContract]
        Task<List<LCMS_Water_Entrapment>> GetAll(Empty empty, CallContext context = default);

        [OperationContract]
        Task<LCMS_Water_Entrapment> GetById(IdRequest request, CallContext context = default);
        [OperationContract]
        Task<LCMS_Water_Entrapment> EditValue(LCMS_Water_Entrapment request, CallContext context = default);
        [OperationContract]
        Task<CountReply> GetRecordCount(Empty empty, CallContext context = default);
        [OperationContract]
        Task<IdReply> DeleteObject(LCMS_Water_Entrapment request, CallContext context = default);
        [OperationContract]
        Task<IdReply> DeleteAll(Empty empty, CallContext context = default);
        [OperationContract]
        Task<List<LCMS_Water_Entrapment>> GetBySurvey(SurveyRequest request);
        [OperationContract]
        Task<IEnumerable<LCMS_Water_Entrapment>> QueryAsync(string predicate);
        [OperationContract]
        Task<CountReply> GetCountAsync(string sqlQuery);

        [OperationContract]
        Task<IdReply> HasData(Empty empty, CallContext context = default);
        [OperationContract]
        Task<LCMS_Water_Entrapment> UpdateGenericData(string fieldsToUpdateSerialized);
    }
}
