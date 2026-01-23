using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using Google.Protobuf.WellKnownTypes;
using ProtoBuf.Grpc;
using System.ServiceModel;
using System.Net;
using DataView2.Core.Helper;
using System.Data;

namespace DataView2.Core.Models.ExportTemplate
{
    [DataContract]
    public class OutputColumnTemplate
	{
        [DataMember(Order = 1)]
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [DataMember(Order = 2)]
        public int OutputTemplateId { get; set; }

        [DataMember(Order = 3)]
        public string Table { get; set; }

        [DataMember(Order = 4)]
        public string Column { get; set; }

        [DataMember(Order = 5)]
        public string Source { get; set; }

        [DataMember(Order = 6)]
        public bool Grouped { get; set; }

        [DataMember(Order = 7)]
        public string? GroupedBy { get; set; }

        [DataMember(Order = 8)]
        public string? Operation { get; set; }
        [DataMember(Order = 9)]
        public string? DataType { get; set; }

        [ForeignKey("OutputTemplateId")]
		public virtual OutputTemplate OutputTemplate { get; set; }

	}

    [ServiceContract]
    public interface IOutputColumnTemplateService
    {
        [OperationContract]
        Task<List<OutputColumnTemplate>> GetAll(Empty empty, 
            CallContext context = default);

        [OperationContract]
        Task<OutputColumnTemplate> EditValue(OutputColumnTemplate request,
            CallContext context = default);

        [OperationContract]
        Task<IdReply> DeleteObject(OutputColumnTemplate request,
            CallContext context = default);

        [OperationContract]
        Task<IEnumerable<OutputColumnTemplate>> QueryAsync(string predicate);

		[OperationContract]
		Task<CountReply> GetRecordCount(Empty empty, CallContext context = default);
	}
}
