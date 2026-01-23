using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using Google.Protobuf.WellKnownTypes;
using ProtoBuf.Grpc;
using DataView2.Core.Models.LCMS_Data_Tables;
using ProtoBuf.Grpc.Configuration;

namespace DataView2.Core.Models
{
    [DataContract]
    public class Survey
    {
        [DataMember(Order = 1)]
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [DataMember(Order = 2)]
        public string SurveyIdExternal { get; set; }
        [DataMember(Order = 3)]
        public string SurveyName { get; set; }
        [DataMember(Order = 4)]
        [Column(TypeName = "datetime")]
        public DateTime SurveyDate {  get; set; }
        [DataMember(Order = 5)]
        public string? ImageFolderPath { get; set; }
        [DataMember(Order = 6)]
        public string? VideoFolderPath { get; set; }
        [DataMember(Order = 7)]
        public string DataviewVersion { get; set; }
        [DataMember(Order = 8)]
        public double GPSLatitude { get; set; } = 0.0; // Set a default GPSLatitude

        [DataMember(Order = 9)]
        public double GPSLongitude { get; set; } = 0.0; // Set a default GPSLongitude

        [DataMember(Order = 10)]
        public int? StartFis {  get; set; }

        [DataMember(Order = 11)]
        public int? EndFis { get;set; }

        [DataMember(Order = 12)]
        public double? StartChainage { get; set; }

        [DataMember(Order = 13)]    
        public double? EndChainage { get;set; }

        [DataMember(Order = 14)]
        public int LRP {  get; set; }

        [DataMember(Order = 15)]
        public double? LRPchainage { get; set; }

        [DataMember(Order = 16)]
        public string? Operator { get; set; }
        [DataMember(Order = 17)]
        public string? Direction { get; set; }

        [DataMember (Order = 18)]
        public int? Lane { get; set; }

        public override string ToString()
        {
            return SurveyName;
        }
    }

    [DataContract]
    public class ImageFolderChangeRequest
    {
        [DataMember(Order = 1)]
        public string surveyId { get; set; }
        [DataMember(Order = 2)]
        public string ImageFolderPath { get; set; }
    }

   

    [DataContract]
    public class SurveyIdRequest
    {
        [DataMember(Order = 1)]
        public int SurveyId { get; set; }

        [DataMember(Order = 2)]
        public string? SurveyExternalId { get; set; }
        
    }

    [DataContract]

    public class SurveyDetailsResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public int SurveyId { get; set; }
        public string SurveyIdExternal { get; set; }
    }


    [ServiceContract]
    public interface ISurveyService
    {
        [OperationContract]
        Task<List<Survey>> GetAll(Empty empty, CallContext context = default);

        //[OperationContract]
        //Task<List<string>> GetAllSurveyNames(Empty empty, CallContext context = default);

        [OperationContract]
        Task<string> GetSurveyNameById(SurveyIdRequest surveyId, CallContext context = default);

        [OperationContract]
        Task<string> GetSurveyNameByExternalId(string externalSurveyId, CallContext context = default);


        [OperationContract]
        Task<Survey> EditValue(Survey request, CallContext context = default);
        
        [OperationContract]
        Task<Survey> GetById(SurveyIdRequest surveyId);
        
        [OperationContract]
        Task<string> GetSurveyIdBySurveyName(string surveyName);

        [OperationContract]
        Task<List<string>> FetchLCMSTablesBySurvey(SurveyIdRequest surveyId, CallContext context = default);

        [OperationContract]
        Task<List<string>> FetchLCMSTablesByExternalSurveyId(string surveyId, CallContext context = default);


        [OperationContract]
        Task<List<string>> FetchLCMSTables(Empty empty, CallContext context = default);

        [OperationContract]
        Task<IdReply> DeleteObject(Survey request, CallContext context = default);

        [OperationContract]
        Task<CountReply> GetRecordCount(Empty empty, CallContext context = default);

        [OperationContract]
        Task<IEnumerable<Survey>> QueryAsync(string predicate);
        [OperationContract]
        Task<CountReply> GetCountAsync(string sqlQuery);
        [OperationContract]
        Task<IdReply> Create(Survey survey, CallContext context = default);
        [OperationContract]
        Task<Survey> GetSurveyEntityByName(string surveyName);
        [OperationContract]
        Task<Survey> GetSurveyEntityByExternalId(string externalId);
        [OperationContract]
        Task<string> GetImageFolderPath(string surveyId);
        [OperationContract]
        Task<Survey> UpdateGenericData(string fieldsToUpdateSerialized);
        [OperationContract]
        Task UpdateSurveyFolderPath(ImageFolderChangeRequest request, CallContext context = default);


        [OperationContract]
        Task<Survey> CreateSegmentedSurveyFromOther(SegmentationData request, CallContext context = default);

    }
}
