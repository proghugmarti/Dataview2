using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using DataView2.Core.Models.LCMS_Data_Tables;
using Google.Protobuf.WellKnownTypes;
using ProtoBuf.Grpc;
using System.ServiceModel;

namespace DataView2.Core.Models.Other
{
    [DataContract]
    public class Summary
    {
        [DataMember(Order = 1)]
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [DataMember(Order = 2)]
        public string Name { get; set; }
        [DataMember(Order = 3)]
        public string SurveyId { get; set; }
        [DataMember(Order = 4)]
        public double ChainageStart { get; set; } = 0.0;
        [DataMember(Order = 5)]
        public double ChainageEnd { get; set; } = 0.0;
        [DataMember(Order = 6)]
        [ForeignKey("SampleUnitSet")]
        public int? SampleUnitSetId { get; set; }
        [DataMember(Order = 7)]
        [ForeignKey("SampleUnit")]
        public int? SampleUnitId { get; set; }
        [DataMember(Order = 8)]
        public double GPSLatitude { get; set; } = 0.0; 
        [DataMember(Order = 9)]
        public double GPSLongitude { get; set; } = 0.0;
        [DataMember(Order = 10)]
        public ICollection<SummaryDefect>? SummaryDefects { get; set; }

        public SampleUnit_Set? SampleUnitSet { get; set; }
        public SampleUnit? SampleUnit { get; set; }
    }

    [DataContract]
    public class SummaryDefect
    {
        [DataMember(Order = 1)]
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [DataMember(Order = 2)]
        public string TableName { get; set; }
        [DataMember(Order = 3)]
        public string NumericField { get; set; }
        [DataMember(Order = 4)]
        public string Operation { get; set; }
        [DataMember(Order = 5)]
        public double Value { get; set; }
        [DataMember(Order = 6)]
        [ForeignKey("SummaryUnit")]
        public int SummaryId { get; set; }

        //Navigation Property
        public Summary? Summary { get; set; }
    }

    //Grpc Request
    [DataContract]
    public class SummaryRequest
    {
        [DataMember(Order = 1)]
        public string SummaryName { get; set; }
        [DataMember(Order = 2)]
        public string CoordinateString { get; set; }
        [DataMember(Order = 3)]
        public string SelectedSurvey { get; set; }
        [DataMember(Order = 4)]
        public List<SummaryItem> SummaryItems { get; set; }

        [DataMember(Order = 5)]
        public int SampleUnitSetId { get; set; }
        [DataMember(Order = 6)]
        public int SampleUnitId { get; set; }
        [DataMember(Order = 7)]
        public int? SummaryId { get; set; }
        [DataMember(Order = 8)]
        public double ChainageStart { get; set; }
        [DataMember(Order = 9)]
        public double ChainageEnd { get; set; }
    }

    [DataContract]
    public class SummaryItem
    {
        [DataMember(Order = 1)]
        public string TableName { get; set; }
        [DataMember(Order = 2)]
        public string NumericField { get; set; }
        [DataMember(Order = 3)]
        public string Operation { get; set; }
        [DataMember(Order = 4)]
        public double NumericValue { get; set; } = 0.0;
        [DataMember(Order = 5)]
        public bool IsMetaTable { get; set; } = false;
    }

    [DataContract]
    public class SUSAndSUName
    {
        [DataMember(Order = 1)]
        public string SUSName { get; set; }
        [DataMember(Order = 2)]
        public string SUName { get; set; }
    }

    [ServiceContract]
    public interface ISummaryService
    {
        [OperationContract]
        Task<IdReply> CreateSummary(SummaryRequest request);
        [OperationContract]
        Task<IdReply> Create(Summary summary);
        [OperationContract]
        Task<List<Summary>> GetSummaryBySampleUnit(IdRequest request);
        [OperationContract]
        Task<List<Summary>> GetAll(Empty empty);
        [OperationContract]
        Task<List<string>> HasData(Empty empty, CallContext context = default);
        [OperationContract]
        Task<List<Summary>> GetByName(string name);
        [OperationContract]
        Task<SUSAndSUName> GetSUNamesById(IdRequest request);
        [OperationContract]
        Task<IdReply> HasSameNameSummary(string summaryName);
        [OperationContract]
        Task<IdReply> BatchRecalculateSummaries(List<SummaryRequest> summaryRequests);
        [OperationContract]
        Task<IdReply> BatchCreateSummaries(List<SummaryRequest> request);
        [OperationContract]
        Task<List<string>> GetSummaryNameBySurvey(SurveyIdRequest request);
    }
}
