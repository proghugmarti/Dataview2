using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using DataView2.Core.Models.ExportTemplate;
using Google.Protobuf.WellKnownTypes;
using ProtoBuf.Grpc;
using System.ServiceModel;
using DataView2.Core.Models.LCMS_Data_Tables;

namespace DataView2.Core.Models.QC
{
    [DataContract]
    public class QCFilter
    {
        [DataMember(Order = 1)]
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [DataMember(Order = 2)]
        public string FilterName { get; set; }

        [DataMember(Order = 3)]
        public string FilterJson { get; set; }
    }

    [ServiceContract]
    public interface IQCFilterService
    {
        [OperationContract]
        Task<IdReply> Create(QCFilter request, CallContext context = default);       

        [OperationContract]
        Task<QCFilter> GetById(IdRequest request, CallContext context = default);

        [OperationContract]
        Task<List<QCFilter>> GetAll(Empty empty,
            CallContext context = default);

        [OperationContract]
        Task<QCFilter> EditValue(QCFilter request,
            CallContext context = default);

        [OperationContract]
        Task<IdReply> DeleteObject(QCFilter request,
            CallContext context = default);

        [OperationContract]
        Task<IEnumerable<QCFilter>> QueryAsync(string predicate);
    }
}
