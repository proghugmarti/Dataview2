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

namespace DataView2.Core.Models.Other
{
    [DataContract]
    public class SampleUnit
    {
        [DataMember(Order = 1)]
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [DataMember(Order = 2)]
        public string Name { get; set; }    
        [DataMember(Order = 3)]
        public double Area_m2 { get; set; }
        [DataMember(Order = 4)]
        public string Coordinates { get; set; }
        [DataMember(Order = 5)]
        [ForeignKey("SampleUnitSet")]
        public int SampleUnitSetId { get; set; }
        //Navigation Property
        public SampleUnit_Set SampleUnitSet { get; set; }
        [DataMember(Order = 6)]
        public int? NumOfSlabs { get; set; } //only for pcc pavement
    }
    [ServiceContract]
    public interface ISampleUnitService
    {
        [OperationContract]
        Task<List<SampleUnit>> GetAll(Empty empty, CallContext context = default);
        [OperationContract]
        Task<List<SampleUnit>> GetBySampleUnitSet(IdRequest sampleUnitSetId, CallContext context = default);
        [OperationContract]
        Task<SampleUnit> GetById(IdRequest id, CallContext context = default);
        [OperationContract]
        Task<IdReply> Create(SampleUnit request, CallContext context = default);
        [OperationContract]
        Task<IdReply> GetOrCreate(SampleUnit sampleUnit);
        [OperationContract]
        Task<IdReply> DeleteObject(SampleUnit request, CallContext context = default);
        [OperationContract]
        Task<SampleUnit> EditValue(SampleUnit request);
    }
}
