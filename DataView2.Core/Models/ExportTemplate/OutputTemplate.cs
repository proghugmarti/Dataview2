using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using DataView2.Core.Models.CrackClassification;
using DataView2.Core.Models.LCMS_Data_Tables;
using Google.Protobuf.WellKnownTypes;
using ProtoBuf.Grpc;
using System.ServiceModel;
using System.Data;

namespace DataView2.Core.Models.ExportTemplate
{
    [DataContract]
    public class OutputTemplate 
	{
        [DataMember(Order = 1)]
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [DataMember(Order = 2)]
        public string Name { get; set; }

        [DataMember(Order = 3)]
        public string Format { get; set; }

        [DataMember(Order = 4)]
		[Column(TypeName = "datetime")]
		public DateTime CreatedDate { get; set; }

        [DataMember(Order = 5)]
        public bool Active { get; set; }
    }

    [ServiceContract]
    public interface IOutputTemplateService
    {
        [OperationContract]
        Task<List<OutputTemplate>> GetAll(Empty empty,
            CallContext context = default);

        [OperationContract]
        Task<OutputTemplate> EditValue(OutputTemplate request,
            CallContext context = default);

        [OperationContract]
        Task<IdReply> DeleteObject(OutputTemplate request,
            CallContext context = default);

        [OperationContract]
        Task<IEnumerable<OutputTemplate>> QueryAsync(string predicate);

		[OperationContract]
		Task<CountReply> GetRecordCount(Empty empty, CallContext context = default);
	}
}
