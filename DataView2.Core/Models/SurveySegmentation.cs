using System;
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
using Esri.ArcGISRuntime.UI;

namespace DataView2.Core.Models
{
    [DataContract]
    public class SurveySegmentation
    {
        [DataMember(Order = 1)]
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [DataMember(Order = 2)]
        public string Name { get; set; }
        [DataMember(Order = 3)]
        public string? Description { get; set; }
        [DataMember(Order = 4)]
        public string StartPoint { get; set; }

        [DataMember(Order = 5)]
        public string EndPoint { get; set; }
        [DataMember(Order = 6)]
        public int? Direction { get; set; }
        [DataMember(Order = 7)]
        public double StartChainage { get; set; }

        [DataMember(Order = 8)]
        public double EndChainage { get; set; }

        [DataMember(Order = 9)]
        public int Lane { get; set; }
    }

    [DataContract]
    public class SegmentationData
    {
        [DataMember(Order = 1)]
        public string SurveyId { get; set; }
        [DataMember(Order = 2)]
        public double SectionId { get; set; }
        [DataMember(Order = 3)]
        public string NewSurveyId { get; set; }
        [DataMember(Order = 4)]
        public double Chainage { get; set; }
        [DataMember(Order = 5)]
        public double EndChainage{ get; set; }
    }

   
    [DataContract]
    public class SegmentationTableData
    {
        [DataMember(Order = 1)]
        public string SurveyId { get; set; }

        [DataMember(Order = 2)]
        public string SurveyName { get; set; }

        [DataMember(Order = 3)]
        public int SegmentRangeStart { get; set; }

        [DataMember(Order = 4)]
        public int SegmentRangeEnd { get; set; }

        [DataMember(Order = 5)]
        public double length { get; set; }

        [DataMember(Order = 6)]
        public double StartChainage { get; set; }

        [DataMember(Order = 7)]
        public double EndChainage { get; set; }

        [DataMember(Order = 8)]
        public string Direction { get; set; }

        [DataMember(Order = 9)]
        public Survey OldSurvey{ get; set; }

        [DataMember(Order = 10)]
        public double NewStartChainage { get; set; }

    }

   

    [DataContract]
    public class CollectSegmentsToSaveRequest 
    {
        [DataMember(Order = 1)]
        public string NewSurveyName { get; private set ; }

        [DataMember(Order = 2)]
        public string OldSurveyId { get; private set; }

        [DataMember(Order = 3)]
        public List<int> SegmentsIds { get; private set; }

        // Parameterless constructor for Protobuf deserialization
        private CollectSegmentsToSaveRequest()
        {
            // Initialize properties with default values if necessary
            SegmentsIds = new List<int>();
        }

        public CollectSegmentsToSaveRequest ( string newSurveyName, string oldSurveyId, List<int> segmentsIds)
        {
            NewSurveyName = newSurveyName;
            OldSurveyId = oldSurveyId;
            SegmentsIds = segmentsIds;
        }

    }


    [ServiceContract]
    public interface ISurveySegmentationService
    {
        [OperationContract]
        Task<List<string>> HasData(Empty empty, CallContext context = default);

        [OperationContract]
        Task<SurveySegmentation> GetById(IdRequest request, CallContext context = default);

        [OperationContract]
        Task<List<SurveySegmentation>> GetAll(Empty empty, CallContext context = default);

        [OperationContract]
        Task<SurveySegmentation> GetByName(string name);

        [OperationContract]
        Task<IdReply> DeleteObject(SurveySegmentation request, CallContext context = default);

        [OperationContract]
        Task<IdReply> Create(SurveySegmentation survey, CallContext context = default);

        [OperationContract]
        Task<IdReply> UpdateSegmentation(SurveySegmentation request, CallContext context = default);

        [OperationContract]
        List<string> GetTableNamesWithColumn(string[] columnNames);

        [OperationContract]
        Task CreateSurveysAndInsertSegments(List<SegmentationTableData> surveysToSave);

        [OperationContract]
        Task ProcessSegmentationSegments(List<SegmentationData> segmentationDatas);
    }
}
