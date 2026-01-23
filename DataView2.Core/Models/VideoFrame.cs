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
using DataView2.Core.Models.LCMS_Data_Tables;

namespace DataView2.Core.Models
{
    [DataContract]
    public class VideoFrame
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
        public double Chainage { get; set; } 
        [DataMember(Order = 5)]
        public string ImageFileName { get; set; }
        [DataMember(Order = 6)]
        public int VideoFrameId { get; set; }
        [DataMember(Order = 7)]
        public string CameraName { get; set; }
        [DataMember(Order = 8)]
        public string CameraSerial { get; set; }

        [DataMember(Order = 9)]
        public string CameraType { get; set; }
        [DataMember(Order = 10)]
        public long CameraTime { get; set; }
        [DataMember(Order = 11)]
        public long PCTime { get; set; }    
        [DataMember(Order = 12)]
        public double GPSLatitude { get; set; }

        [DataMember(Order = 13)]
        public double GPSLongitude { get; set; }
        [DataMember(Order = 14)]
        public double GPSTrackAngle { get; set; }
        [DataMember(Order = 15)]
        public string GeoJSON { get; set; }
    }

    [ServiceContract]
    public interface IVideoFrameService
    {
        [OperationContract]
        Task<List<VideoFrame>> GetAll(Empty empty, CallContext context = default);
        [OperationContract]
        Task<List<VideoFrame>> GetByName(string cameraName, CallContext context = default);
        [OperationContract]
        Task<List<VideoFrame>> GetBySurveyId(SurveyIdRequest idRequest);
        [OperationContract]
        Task<List<string>> HasData(Empty empty, CallContext context = default);
        [OperationContract]
        Task<List<string>> HasDataBySurvey(SurveyIdRequest surveyIdrequest, CallContext context = default);

        [OperationContract]
        Task<IdReply> DeleteObject(VideoFrame request, CallContext context = default);

        [OperationContract]
        Task<CountReply> GetRecordCount(Empty empty, CallContext context = default);

        [OperationContract]
        Task<IEnumerable<VideoFrame>> QueryAsync(string predicate);
        [OperationContract]
        Task<CountReply> GetCountAsync(string sqlQuery);
        [OperationContract]
        Task<VideoFrame> EditValue(VideoFrame request, CallContext context = default);
        [OperationContract]
        Task<VideoFrame> UpdateGenericData(string fieldsToUpdateSerialized);
    }
}
