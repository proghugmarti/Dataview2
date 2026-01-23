using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using ProtoBuf.Grpc;
using Google.Protobuf.WellKnownTypes;
using DataView2.Core.Models.CrackClassification;
using DataView2.Core.Models.LCMS_Data_Tables;
using System.Text.Json.Serialization;

namespace DataView2.Core.Models.Other
{
    [DataContract]
    public class Boundary
    {
        [DataMember(Order = 1)]
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [DataMember(Order = 2)]
        public string? SurveyId { get; set; }
        [DataMember(Order = 3)]
        public string? SurveyName { get; set; }
        [DataMember(Order = 4)]
        public string? BoundaryName { get; set; }
        [DataMember(Order = 5)]
        public string Coordinates { get; set; }
        [DataMember(Order = 6)]
        public string BoundariesMode { get; set; }
    }

    [ServiceContract]
    public interface IBoundariesService
    {
        [OperationContract]
        Task<IdReply> Create(Boundary request, CallContext context = default);
        [OperationContract]
        Task<List<Boundary>> GetAll(Empty empty, CallContext context = default);
        [OperationContract]
        Task<Boundary> GetById(IdRequest request, CallContext context = default);
        [OperationContract]
        Task<Boundary> GetBySurveyId(string surveyId, CallContext context = default);
        [OperationContract]
        Task<IdReply> Edit(Boundary request, CallContext context = default);
        [OperationContract]
        Task<IdReply> HasData(Empty empty, CallContext context = default);
        [OperationContract]
        Task<IEnumerable<Boundary>> QueryAsync(string predicate);
        [OperationContract]
        Task<CountReply> GetCountAsync(string sqlQuery);
        [OperationContract]
        Task<Boundary> EditValue(Boundary request,
            CallContext context = default);
        [OperationContract]
        Task<IdReply> DeleteObject(Boundary request,
            CallContext context = default);
        [OperationContract]
        Task<CountReply> GetRecordCount(Empty empty, CallContext context = default);
        [OperationContract]
        Task<Boundary> UpdateGenericData(string fieldsToUpdateSerialized);
    }

    public enum BoundariesMode
    {
        Include,
        Exclude,
    }
}
