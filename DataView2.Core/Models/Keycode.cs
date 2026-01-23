using DataView2.Core.SurveyRecordsProto;
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
    public class Keycode
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
        public double GPSLatitude { get; set; }
        [DataMember(Order = 6)]
        public double GPSLongitude { get; set; }
        [DataMember(Order = 7)]
        public double GPSTrackAngle { get; set; }
        [DataMember(Order = 8)]
        public string GeoJSON { get; set; }
        [DataMember(Order = 9)]
        public string Time { get; set; }
        [DataMember(Order = 10)]
        public double Speed { get; set; }
        [DataMember(Order = 11)]
        public string Description { get; set; }
        [DataMember(Order = 12)]
        public int Key { get; set; }
        [DataMember(Order = 13)]
        public string EventKeyType { get; set; }
        [DataMember(Order = 14)]
        public string? ContinuousStatus { get; set; }
    }


    [DataContract]
    public class KeycodePair
    {
        [DataMember(Order = 1)]
        public Keycode StartedKeycode { get; set; }

        [DataMember(Order = 2)]
        public Keycode EndedKeycode { get; set; }

        [DataMember(Order = 3)]
        public double StartChainage { get; set; }

        [DataMember(Order = 4)]
        public double EndChainage { get; set; }
    }

    [DataContract]
    public class keycodeFromOtherSaveRequest
    {
        [DataMember(Order = 1)]
        public Keycode exampleKeycode { get; set; }
        [DataMember(Order = 2)]
        public double Latitude { get; set; }
        [DataMember(Order = 3)]
        public double Longitude { get; set; }
        [DataMember(Order = 4)]
        public string? ContinuousStatus { get; set; }
    }


    [ServiceContract]
    public interface IKeycodeService
    {
        [OperationContract]
        Task<List<Keycode>> GetAll(Empty empty, CallContext context = default);
        [OperationContract]
        Task<List<Keycode>> GetByDescription(string keycodeDescription, CallContext context = default);
        [OperationContract]
        Task<List<Keycode>> GetBySurveyId(SurveyIdRequest idRequest);
        [OperationContract]
        Task<List<Keycode>> GetBySurvey(string surveyId, CallContext context = default);

        [OperationContract]
        Task<List<Keycode>> GetBySurveyExternalIdList(List<string> listExternalIds);

        [OperationContract]
        Task<List<Keycode>> HasData(Empty empty, CallContext context = default);
       // [OperationContract]
       // Task<List<string>> HasDataBySurvey(SurveyIdRequest surveyIdrequest, CallContext context = default);

        [OperationContract]
        Task<IdReply> DeleteObject(Keycode request, CallContext context = default);

        [OperationContract]
        Task<CountReply> GetRecordCount(Empty empty, CallContext context = default);

      //  [OperationContract]
       // Task<IEnumerable<Keycode>> QueryAsync(string predicate);
        [OperationContract]
        Task<CountReply> GetCountAsync(string sqlQuery);
        [OperationContract]
        Task<Keycode> EditValue(Keycode request, CallContext context = default);
        //[OperationContract]
        // Task<Keycode> UpdateGenericData(string fieldsToUpdateSerialized);

        [OperationContract]
        Task ProcessKeycode(string filePath, SurveyIdRequest surveyIdRequest);
        [OperationContract]
        Task SaveListKeycodes(List<keycodeFromOtherSaveRequest> keycodeList);
        [OperationContract]
        Task<List<string>> GetKeycodeDescriptionsBySurvey(SurveyIdRequest request);
    }


}
