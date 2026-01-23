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
using DataView2.Core.Models.LCMS_Data_Tables;

namespace DataView2.Core.Models.Other
{
    [DataContract]
    public class SampleUnit_Set
    {
        [DataMember(Order = 1)]
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [DataMember(Order = 2)]
        public string Name { get; set; }
        [DataMember(Order = 3)]
        public string Description { get; set; }
        [DataMember(Order = 4)]
        public SampleUnitSetType Type { get; set; }
        [DataMember(Order = 5)]
        public ICollection<SampleUnit>? SampleUnits { get; set; }
    }

    public enum SampleUnitSetType
    {
        Summary,
        PCI,
        Interval
    }

    [ServiceContract]
    public interface ISampleUnitSetService
    {
        [OperationContract]
        Task<IdReply> Create(SampleUnit_Set request, CallContext context = default);
        [OperationContract]
        Task<IdReply> GetOrCreate(SampleUnit_Set sampleUnitSet);
        [OperationContract]
        Task<List<SampleUnit_Set>> GetAll(Empty empty, CallContext context = default);
        [OperationContract]
        Task<SampleUnit_Set> GetByName(string name, CallContext context = default);
        [OperationContract]
        Task<SampleUnit_Set> GetById(IdRequest request, CallContext context = default);
        [OperationContract]
        Task<IdReply> DeleteObject(SampleUnit_Set request, CallContext context = default);
        [OperationContract]
        Task<IdReply> DeleteRange(List<SampleUnit_Set> request, CallContext context = default);
    }
}
