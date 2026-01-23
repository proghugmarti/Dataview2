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

namespace DataView2.Core.Models
{
    [DataContract]
    public class Camera360Frame
    {

        [DataMember(Order = 1)]
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [DataMember(Order = 2)]
        public int SurveyId { get; set; }
        [DataMember(Order = 3)]
        public string SurveyName { get; set; }
        [DataMember(Order = 4)]
        public double Chainage { get; set; } = 0.0;
        [DataMember(Order = 5)]
        public string ImagePath { get; set; }
        [DataMember(Order = 6)]
        public double TimeStamp { get; set; }
        [DataMember(Order = 7)]
        public int Camera360FrameId { get; set; }
        [DataMember(Order = 8)]
        public double GPSLatitude { get; set; }

        [DataMember(Order = 9)]
        public double GPSLongitude { get; set; }
        [DataMember(Order = 10)]
        public double GPSTrackAngle { get; set; }
        [DataMember(Order = 11)]
        public string GeoJSON { get; set; }
    }

    [ServiceContract]
    public interface ICamera360FrameService
    {
        [OperationContract]
        Task<List<Camera360Frame>> GetAll(Empty empty, CallContext context = default);
        [OperationContract]
        Task<List<Camera360Frame>> GetBySurveyId(SurveyIdRequest request, CallContext context = default);

        [OperationContract]
        Task<IdReply> HasData(Empty empty, CallContext context = default);

        [OperationContract]
        Task<IdReply> DeleteObject(Camera360Frame request, CallContext context = default);

        [OperationContract]
        Task<IEnumerable<Camera360Frame>> QueryAsync(string predicate);
    }
}
