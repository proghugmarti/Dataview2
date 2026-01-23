using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using DataView2.Core.Models.LCMS_Data_Tables;
using ProtoBuf.Grpc;
using System.ServiceModel;
using Google.Protobuf.WellKnownTypes;

namespace DataView2.Core.Models.Other
{
    [DataContract]
    public class MapGraphicData
    {

        [DataMember(Order = 1)]
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [DataMember(Order = 2)]
        public string Name { get; set; }

        [DataMember(Order = 3)]
        public int Red { get; set; }

        [DataMember(Order = 4)]
        public int Green { get; set; }

        [DataMember(Order = 5)]
        public int Blue { get; set; }

        [DataMember(Order = 6)]
        public int Alpha { get; set; }

        [DataMember(Order = 7)]
        public double Thickness { get; set; }
        [DataMember(Order = 8)]
        public string? SymbolType { get; set; } //nullable

        [DataMember(Order = 9)]
        public string LabelProperty { get; set; } = "No Label";
    }

    [DataContract]
    public class NameRequest
    {
        [DataMember(Order = 1)]
        public string Name { get; set; }
    }

    [ServiceContract]
    public interface IMapGraphicDataService
    {
        [OperationContract]
        Task<IdReply> Create(MapGraphicData request, CallContext context = default);
        [OperationContract]
        Task<List<MapGraphicData>> GetAll(Empty empty, CallContext context = default);
        [OperationContract]
        Task<MapGraphicData> GetByName(NameRequest nameRequest, CallContext context = default);
        [OperationContract]
        Task<MapGraphicData> Edit(MapGraphicData request, CallContext callContext = default);
    }
}
