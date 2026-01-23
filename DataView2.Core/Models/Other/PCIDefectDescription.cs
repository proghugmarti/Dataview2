using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using Google.Protobuf.WellKnownTypes;
using ProtoBuf.Grpc;
using System.ServiceModel;

namespace DataView2.Core.Models.Other
{
    [DataContract]
    public class PCIDefectDescription
    {
        [DataMember(Order = 1)]
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set;}
        [DataMember(Order = 2)]
        public string Name { get; set;}
        [DataMember(Order = 3)]
        public string? PCIRatingType { get; set;}
        [DataMember(Order = 4)]
        public string Surface { get; set; }
        [DataMember(Order = 5)]
        public string UnitOfMeasure { get; set; }
        [DataMember(Order = 6)]
        public string? LowSeverityDefinition { get; set; }
        [DataMember(Order = 7)]
        public string? MediumSeverityDefinition { get; set; }
        [DataMember(Order = 8)]
        public string? HighSeverityDefinition { get;set; }
        [DataMember(Order = 9)]
        public string? GeneralDefinition { get; set; }
        [DataMember(Order = 10)]
        public string PotentialEffectOnPCIDeduct { get; set; }
        [DataMember(Order = 11)]
        public string AutomaticOrManual { get; set; }   
    }
}
