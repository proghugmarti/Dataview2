using DataView2.Core.Models.LCMS_Data_Tables;
using Google.Protobuf.WellKnownTypes;
using ProtoBuf.Grpc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace DataView2.Core.Models.Other
{
    [DataContract]
    public class PCIDefects
    {
        [DataMember(Order = 1)]
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [DataMember(Order = 2)]
        [ForeignKey("PCIRatings")]
        public int PCIRatingId { get; set; }
        [DataMember(Order = 3)]
        public string PCIRatingName { get; set; }
        [DataMember(Order = 4)]
        public int SampleUnitSetId { get; set; }
        [DataMember(Order = 5)]
        public string SampleUnitSetName { get; set; }
        [DataMember(Order = 6)]
        public int SampleUnitId { get; set; }
        [DataMember(Order = 7)]
        public string SampleUnitName { get; set; }
        [DataMember(Order = 8)]
        public string DefectName { get; set; }
        [DataMember(Order = 9)]
        public double Qty { get; set; }
        [DataMember(Order = 10)]
        public string Severity { get; set; }
        [DataMember(Order = 11)]
        public string UnitOfMeasure { get; set; }
        [DataMember(Order = 12)]
        public string? GeoJSON { get; set; }

        //Navigation Property
        public PCIRatings PCIRatings { get; set; }
    }

    [DataContract]
    public class PCIDefectRequest
    {
        [DataMember(Order = 1)]
        public int PCIRatingId { get; set; }
        [DataMember(Order = 2)]
        public int SampleUnitId { get; set; }
    }

    [DataContract]
    public class PCIDefectNameResponse
    {
        [DataMember(Order = 1)]
        public string PCIRatingName { get; set; }
        [DataMember(Order = 2)]
        public List<string> PCIDefectName { get; set; }
    }

    [ServiceContract]
    public interface IPCIDefectsService
    {
        [OperationContract]
        Task<IdReply> Create(PCIDefects pciDefect, CallContext context = default);
        [OperationContract]
        Task<IdReply> CreateRange(List<PCIDefects> pciDefects, CallContext context = default);
        [OperationContract]
        Task<IdReply> DeleteObject(PCIDefects request, CallContext context = default);
        [OperationContract]
        Task<IdReply> EditValue(PCIDefects request, CallContext context = default);
        [OperationContract]
        Task<List<PCIDefects>> GetAll(Empty empty, CallContext context = default);
        [OperationContract]
        Task<List<PCIDefects>> GetByPCIRatingAndSampleUnit(PCIDefectRequest request, CallContext context = default);
        [OperationContract]
        Task<List<PCIDefectNameResponse>> HasData(Empty empty, CallContext context = default);
        [OperationContract]
        Task<List<PCIDefects>> GetByTableName(string tableName, CallContext context = default);
        [OperationContract]
        Task<PCIDefects> GetById(IdRequest id, CallContext context = default);
        [OperationContract]
        Task<List<PCIDefects>> GetByRatingName(string ratingName, CallContext context = default);
    }
}
