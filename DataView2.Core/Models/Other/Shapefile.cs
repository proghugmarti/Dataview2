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

namespace DataView2.Core.Models.Other
{
    [DataContract]
    public class Shapefile
    {
        [DataMember(Order = 1)]
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [DataMember(Order = 2)]
        public string ShapefileName { get; set; }
        [DataMember(Order = 3)]
        public string Coordinates { get; set; } 
        [DataMember(Order = 4)]
        public string Attributes { get; set; } //json
        [DataMember(Order = 5)]
        public string ShapeType { get; set; }
    }

    [DataContract]
    public class ShapefileRequest
    {
        [DataMember(Order = 1)]
        public string ShapefileName { get; set; }
        [DataMember(Order = 2)]
        public string FilePath { get; set; }
    }

    [ServiceContract]
    public interface IShapefileService
    {
        [OperationContract]
        Task<IdReply> ProcessShapefiles(List<ShapefileRequest> request, CallContext context = default);
        [OperationContract]
        Task<List<Shapefile>> GetAll(Empty empty, CallContext context = default);
        [OperationContract]
        Task<List<string>> HasData(Empty empty, CallContext context = default);
        [OperationContract]
        Task<IdReply> DeleteByName(string name, CallContext context = default);
        [OperationContract]
        Task<List<Shapefile>> GetByName(string name, CallContext context = default);
    }
}
