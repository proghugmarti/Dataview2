using Google.Protobuf.WellKnownTypes;
using ProtoBuf.Grpc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace DataView2.Core.Models.Setting
{

    [DataContract]
    public class CrackClassificationConfiguration
    {
        [DataMember(Order = 1)]
        [Key]
        public int Id { get; set; }

        [DataMember(Order = 2)]
        public int MinSizeToStraight { get; set; }

        [DataMember(Order = 3)]
        public int MinSizeToAvoidMerge { get; set; }

        [DataMember(Order = 4)]
        public double Straightness { get; set; }

        [DataMember(Order = 5)]
        public double MinimumDeep { get; set; }

        [DataMember(Order = 6)]
        public bool IgnoreOutLanes { get; set; }

        [DataMember(Order = 7)]
        public string ConfigFilePath { get; set; } = "";

        [DataMember(Order = 8)]
        public double LowThreshold { get; set; }

        [DataMember(Order = 9)]
        public double LowMediumThreshold { get; set; }

        [DataMember(Order = 10)]
        public double MediumHighThreshold { get; set; }

        [DataMember(Order = 11)]
        public double HighThreshold { get; set; }
    }

    [ServiceContract]
    public interface ICrackClassificationConfiguration
    {
        [OperationContract]
        Task<CrackClassificationConfiguration> GetClassification(Empty empty, CallContext context = default);
        [OperationContract]
        Task<CrackClassificationConfiguration> EditClassification(CrackClassificationConfiguration request,
        CallContext context = default);
    }

}
