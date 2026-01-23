using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.ServiceModel;
using ProtoBuf.Grpc;
using Google.Protobuf.WellKnownTypes;
using DataView2.Core.Models.LCMS_Data_Tables;

namespace DataView2.Core.Models.Other
{
    [DataContract]
    public class PCIRatings
    {
        [DataMember(Order = 1)]
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [DataMember(Order = 2)]
        public DateTime? Date { get; set; }
        [DataMember(Order = 3)]
        public string RatingName { get; set; }
        [DataMember(Order = 4)]
        public string RaterName { get; set; }
        [DataMember(Order = 5)]
        public string Type { get; set; }
        [DataMember(Order = 6)]
        public string Surface { get; set; }
        //Foreign Key to SampleUnitSet
        [DataMember(Order = 7)]
        [ForeignKey("SampleUnitSet")]
        public int SampleUnitSetId { get; set; }
        [DataMember(Order = 8)]
        public SampleUnit_Set? SampleUnitSet { get; set; }
        [DataMember(Order = 9)]
        public double ProgressPercentage { get; set; }
        [DataMember(Order = 10)]
        public ICollection<PCIRatingStatus>? PCIRatingStatus { get; set; }
        [DataMember(Order = 11)]
        public ICollection<PCIDefects>? PCIDefects { get; set; }
        [DataMember(Order = 12)]
        public string NetworkId { get; set; }
        [DataMember(Order = 13)]
        public string BranchId { get; set; }
        [DataMember(Order =14)]
        public int SectionId { get; set; }
    }

    [DataContract]
    public class PCIRatingStatus
    {
        [DataMember(Order = 1)]
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [DataMember(Order = 2)]
        [ForeignKey("PCIRatings")]
        public int PCIRatingId { get; set; }
        [DataMember(Order = 3)]
        public int SampleUnitId { get; set; }
        [DataMember(Order = 4)]
        public bool Status { get; set; }
        //Navigation Property
        public PCIRatings PCIRatings { get; set; }
    }


    [ServiceContract]
    public interface IPCIRatingService
    {
        [OperationContract]
        Task<IdReply> Create(PCIRatings pciRating, CallContext context = default);
        [OperationContract]
        Task<List<PCIRatings>> GetAll(Empty empty, CallContext context = default);
        [OperationContract]
        Task<IdReply> EditValue(PCIRatings request, CallContext context = default);
        [OperationContract]
        Task<IdReply> DeleteObject(PCIRatings request, CallContext context = default);
        [OperationContract]
        Task<List<PCIRatingStatus>> GetRatingStatus(IdRequest pciRatingId, CallContext context = default);
        [OperationContract]
        Task UpdateRatingStatus(PCIRatingStatus ratingStatus, CallContext context = default);
        [OperationContract]
        Task<PCIRatings> GetByName(string ratingName, CallContext context = default);
        [OperationContract]
        Task<PCIRatings> GetById(IdRequest idRequest, CallContext context = default);
    }
}
