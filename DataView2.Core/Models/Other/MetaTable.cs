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
    public class MetaTable
    {
        [DataMember(Order = 1)]
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [DataMember(Order = 2)]
        public string TableName { get; set; }
        [DataMember(Order = 3)]
        public string GeoType { get; set; }
        [DataMember (Order = 4)]
        public string? Icon { get; set; }
        [DataMember (Order = 5)]
        public int? IconSize { get; set; }

        // Columns 1-25
        [DataMember(Order = 6)]
        public string? Column1 { get; set; }
        [DataMember(Order = 7)]
        public string? Column2 { get; set; }
        [DataMember(Order = 8)]
        public string? Column3 { get; set; }
        [DataMember(Order = 9)]
        public string? Column4 { get; set; }
        [DataMember(Order = 10)]
        public string? Column5 { get; set; }
        [DataMember(Order = 11)]
        public string? Column6 { get; set; }
        [DataMember(Order = 12)]
        public string? Column7 { get; set; }
        [DataMember(Order = 13)]
        public string? Column8 { get; set; }
        [DataMember(Order = 14)]
        public string? Column9 { get; set; }
        [DataMember(Order = 15)]
        public string? Column10 { get; set; }
        [DataMember(Order = 16)]
        public string? Column11 { get; set; }
        [DataMember(Order = 17)]
        public string? Column12 { get; set; }
        [DataMember(Order = 18)]
        public string? Column13 { get; set; }
        [DataMember(Order = 19)]
        public string? Column14 { get; set; }
        [DataMember(Order = 20)]
        public string? Column15 { get; set; }
        [DataMember(Order = 21)]
        public string? Column16 { get; set; }
        [DataMember(Order = 22)]
        public string? Column17 { get; set; }
        [DataMember(Order = 23)]
        public string? Column18 { get; set; }
        [DataMember(Order = 24)]
        public string? Column19 { get; set; }
        [DataMember(Order = 25)]
        public string? Column20 { get; set; }
        [DataMember(Order = 26)]
        public string? Column21 { get; set; }
        [DataMember(Order = 27)]
        public string? Column22 { get; set; }
        [DataMember(Order = 28)]
        public string? Column23 { get; set; }
        [DataMember(Order = 29)]
        public string? Column24 { get; set; }
        [DataMember(Order = 30)]
        public string? Column25 { get; set; }

        // Column Types 1-25
        [DataMember(Order = 31)]
        public string? Column1Type { get; set; }
        [DataMember(Order = 32)]
        public string? Column2Type { get; set; }
        [DataMember(Order = 33)]
        public string? Column3Type { get; set; }
        [DataMember(Order = 34)]
        public string? Column4Type { get; set; }
        [DataMember(Order = 35)]
        public string? Column5Type { get; set; }
        [DataMember(Order = 36)]
        public string? Column6Type { get; set; }
        [DataMember(Order = 37)]
        public string? Column7Type { get; set; }
        [DataMember(Order = 38)]
        public string? Column8Type { get; set; }
        [DataMember(Order = 39)]
        public string? Column9Type { get; set; }
        [DataMember(Order = 40)]
        public string? Column10Type { get; set; }
        [DataMember(Order = 41)]
        public string? Column11Type { get; set; }
        [DataMember(Order = 42)]
        public string? Column12Type { get; set; }
        [DataMember(Order = 43)]
        public string? Column13Type { get; set; }
        [DataMember(Order = 44)]
        public string? Column14Type { get; set; }
        [DataMember(Order = 45)]
        public string? Column15Type { get; set; }
        [DataMember(Order = 46)]
        public string? Column16Type { get; set; }
        [DataMember(Order = 47)]
        public string? Column17Type { get; set; }
        [DataMember(Order = 48)]
        public string? Column18Type { get; set; }
        [DataMember(Order = 49)]
        public string? Column19Type { get; set; }
        [DataMember(Order = 50)]
        public string? Column20Type { get; set; }
        [DataMember(Order = 51)]
        public string? Column21Type { get; set; }
        [DataMember(Order = 52)]
        public string? Column22Type { get; set; }
        [DataMember(Order = 53)]
        public string? Column23Type { get; set; }
        [DataMember(Order = 54)]
        public string? Column24Type { get; set; }
        [DataMember(Order = 55)]
        public string? Column25Type { get; set; }

        // Column Defaults 1-25
        [DataMember(Order = 56)]
        public string? Column1Default { get; set; }
        [DataMember(Order = 57)]
        public string? Column2Default { get; set; }
        [DataMember(Order = 58)]
        public string? Column3Default { get; set; }
        [DataMember(Order = 59)]
        public string? Column4Default { get; set; }
        [DataMember(Order = 60)]
        public string? Column5Default { get; set; }
        [DataMember(Order = 61)]
        public string? Column6Default { get; set; }
        [DataMember(Order = 62)]
        public string? Column7Default { get; set; }
        [DataMember(Order = 63)]
        public string? Column8Default { get; set; }
        [DataMember(Order = 64)]
        public string? Column9Default { get; set; }
        [DataMember(Order = 65)]
        public string? Column10Default { get; set; }
        [DataMember(Order = 66)]
        public string? Column11Default { get; set; }
        [DataMember(Order = 67)]
        public string? Column12Default { get; set; }
        [DataMember(Order = 68)]
        public string? Column13Default { get; set; }
        [DataMember(Order = 69)]
        public string? Column14Default { get; set; }
        [DataMember(Order = 70)]
        public string? Column15Default { get; set; }
        [DataMember(Order = 71)]
        public string? Column16Default { get; set; }
        [DataMember(Order = 72)]
        public string? Column17Default { get; set; }
        [DataMember(Order = 73)]
        public string? Column18Default { get; set; }
        [DataMember(Order = 74)]
        public string? Column19Default { get; set; }
        [DataMember(Order = 75)]
        public string? Column20Default { get; set; }
        [DataMember(Order = 76)]
        public string? Column21Default { get; set; }
        [DataMember(Order = 77)]
        public string? Column22Default { get; set; }
        [DataMember(Order = 78)]
        public string? Column23Default { get; set; }
        [DataMember(Order = 79)]
        public string? Column24Default { get; set; }
        [DataMember(Order = 80)]
        public string? Column25Default { get; set; }
    }

    public enum ColumnType
    {
        Text,
        Number,
        Dropdown,
        Date,
        Measurement
    }
}
