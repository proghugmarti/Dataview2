using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf.Grpc;
using System.ServiceModel;
using Google.Protobuf.WellKnownTypes;

namespace DataView2.Core.Models.Other
{
    [DataContract]
    public class ColorCodeInformation
    {
        [DataMember(Order = 1)]
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [DataMember(Order = 2)]
        public string TableName { get; set; }
        [DataMember(Order = 3)]
        public string Property { get; set; }
        [DataMember(Order = 4)]
        public double MinRange { get; set; } = 0.0;
        [DataMember(Order = 5)]
        public double MaxRange { get; set; } = 0.0;
        [DataMember(Order = 6)]
        public bool IsAboveFrom { get; set; }
        [DataMember(Order = 7)]
        public string HexColor { get; set; }
        [DataMember(Order = 8)]
        public double Thickness { get; set; } = 0.0;
        [DataMember(Order = 9)]
        public bool IsStringProperty { get; set; }
        [DataMember(Order = 10)]
        public string StringProperty { get; set; } = string.Empty;
    }
    [ServiceContract]
    public interface IColorCodeInformationService
    {
        [OperationContract]
        Task<IdReply> CreateRange(List<ColorCodeInformation> request, CallContext context = default);
        [OperationContract]
        Task<List<ColorCodeInformation>> GetByName (string tableName, CallContext context = default);
        [OperationContract]
        Task<IdReply> DeleteByTableName(string tableName, CallContext context = default);
        [OperationContract]
        Task<List<ColorCodeInformation>> GetAll(Empty empty, CallContext context = default);
    }
}
