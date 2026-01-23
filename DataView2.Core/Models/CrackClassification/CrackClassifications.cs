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

namespace DataView2.Core.Models.CrackClassification
{
    [DataContract]
    public class CrackClassifications
    {
        [DataMember(Order = 1)]
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [DataMember(Order = 2)]
        public string SurveyId { get; set; }

        [DataMember(Order = 3)]
        public int SegmentId { get; set; }
        
        [DataMember(Order = 4)]
        public double CrackId { get; set; }

        [DataMember(Order = 5)]
        public double Length { get; set; }

        [DataMember(Order = 6)]
        public double WeightedDepth { get; set;}

        [DataMember(Order = 7)]
        public double WeightedWidth { get; set; }

        [DataMember(Order = 8)]
        public double Chainage { get; set; }
    }

    [ServiceContract]
    public interface ICrackClassificationsService
    {
        [OperationContract]
        Task<List<CrackClassifications>> GetAll(Empty empty,
            CallContext context = default);

        [OperationContract]
        Task<CrackClassifications> EditValue(CrackClassifications request,
            CallContext context = default);

        [OperationContract]
        Task<IdReply> DeleteObject(CrackClassifications request,
            CallContext context = default);

        [OperationContract]
        Task<IEnumerable<CrackClassifications>> QueryAsync(string predicate);
        [OperationContract]
        Task<CountReply> GetCountAsync(string sqlQuery);

        [OperationContract]
		Task<CountReply> GetRecordCount(Empty empty, CallContext context = default);
        [OperationContract]
        Task<CrackClassifications> UpdateGenericData(string fieldsToUpdateSerialized);
    }
}
