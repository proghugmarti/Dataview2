using DataView2.Core.Models.ExportTemplate;
using DataView2.Core.Models.LCMS_Data_Tables;
using Google.Protobuf.WellKnownTypes;
using ProtoBuf.Grpc;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace DataView2.Core.Models.CrackClassification
{
    [DataContract]
    public class SummaryCrackClasification 
    {
        [DataMember (Order = 1)]
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [DataMember(Order = 2)]
        [Description("Ch. start(m)")]
        public double Chstart { get; set; }

        [DataMember(Order = 3)]
        [Description("Ch. end(m)")]
        public double Chend { get; set; }

        [DataMember(Order = 4)]
        [Description("Lane Width(mm)")]
        public double LaneWidth { get; set; }

        [DataMember(Order = 5)]
        [Description("Sample Area(m2)")]
        public double SampleArea { get; set; }

        [DataMember(Order = 6)]
        [Description("Long Crack Very LOW (m2)")]
        public double LongCrackVeryLOW { get; set; }

        [DataMember(Order = 7)]
        [Description("Long Crack LOW (m2)")]
        public double LongCrackLOW { get; set; }

        [DataMember(Order = 8)]
        [Description("Long Crack MED (m2)")]
        public double LongCrackMED { get; set; }

        [DataMember(Order = 9)]
        [Description("Long Crack HIGH(m2)")]
        public double LongCrackHIGH { get; set; }

        [DataMember(Order = 10)]
        [Description("Long Crack Very HIGH (m2)")]
        public double LongCrackVeryHIGH { get; set; }

        [DataMember(Order = 11)]
        [Description("Trans Crack Very LOW (m2)")]
        public double TransCrackVeryLOW { get; set; }

        [DataMember(Order = 12)]
        [Description("Trans Crack LOW (m2)")]
        public double TransCrackLOW { get; set; }

        [DataMember(Order = 13)]
        [Description("Trans Crack MED (m2)")]
        public double TransCrackMED { get; set; }

        [DataMember(Order = 14)]
        [Description("Trans Crack HIGH(m2)")]
        public double TransCrackHIGH { get; set; }

        [DataMember(Order = 15)]
        [Description("Trans Crack Very HIGH (m2)")]
        public double TransCrackVeryHIGH { get; set; }

        [DataMember(Order = 16)]
        [Description("Alligator Crack Very LOW (m2)")]
        public double AlligatorCrackVeryLOW { get; set; }

        [DataMember(Order = 17)]
        [Description("Alligator Crack LOW (m2)")]
        public double AlligatorCrackLOW { get; set; }

        [DataMember(Order = 18)]
        [Description("Alligator Crack MED (m2)")]
        public double AlligatorCrackMED { get; set; }

        [DataMember(Order = 19)]
        [Description("Alligator Crack HIGH(m2)")]
        public double AlligatorCrackHIGH { get; set; }

        [DataMember(Order = 20)]
        [Description("Alligator Crack Very HIGH (m2)")]
        public double AlligatorCrackVeryHIGH { get; set; }

        [DataMember(Order = 21)]
        [Description("Other Crack Very LOW (m2)")]
        public double OtherCrackVeryLOW { get; set; }

        [DataMember(Order = 22)]
        [Description("Other Crack LOW (m2)")]
        public double OtherCrackLOW { get; set; }

        [DataMember(Order = 23)]
        [Description("Other Crack MED (m2)")]
        public double OtherCrackMED { get; set; }

        [DataMember(Order = 24)]
        [Description("Other Crack HIGH(m2)")]
        public double OtherCrackHIGH { get; set; }
        
        [DataMember(Order = 25)]
        [Description("Other Crack Very HIGH (m2)")]
        public double OtherCrackVeryHIGH { get; set; }

        [DataMember(Order = 26)]
        [Description("Longitudinal Cracking(mm)")]
        public double LongitudinalCracking { get; set; }

        [DataMember(Order = 27)]
        [Description("Transverse Cracking(mm)")]
        public double TransverseCracking { get; set; }

        [DataMember(Order = 28)]
        [Description("Cracking in wheelpaths(m2)")]
        public double crackingWheelpaths { get; set; }

        [DataMember(Order = 29)]
        [Description("Wheelpaths Area(m2)")]
        public double WheelpathsArea { get; set; }

        [DataMember(Order = 30)]
        [Description("Square Fatigue Area(m2)")]
        public double FatigueArea { get; set; }

        [DataMember(Order = 31)]
        public string XmlFileName { get; set; }
    }

    [DataContract]
    public class XmlFilesRequest
    {
        [DataMember(Order = 1)]
        public List<string> XmlFilesPath { get; set; }
        [DataMember(Order = 2)]
        public double HorizontalOffset { get; set; }
        [DataMember(Order = 3)]
        public double VerticalOffset { get; set; }
        [DataMember(Order = 4)]
        public bool CallFromUI { get; set; } = false;
    }


    [ServiceContract]
    public interface ICrackClassification
    {
        [OperationContract]
        Task<IdReply> ReclassifyBatch(XmlFilesRequest request,
            CallContext context = default);

        [OperationContract]
        Task<IdReply> GetSummaries(Empty empty,
            CallContext context = default);

        [OperationContract]
        Task<List<SummaryCrackClasification>> GetAll(Empty empty,
            CallContext context = default);

        [OperationContract]
        Task<SummaryCrackClasification> EditValue(SummaryCrackClasification request,
            CallContext context = default);

        [OperationContract]
        Task<IdReply> DeleteObject(SummaryCrackClasification request,
            CallContext context = default);

        [OperationContract]
        Task<IEnumerable<SummaryCrackClasification>> QueryAsync(string predicate);
        [OperationContract]
        Task<CountReply> GetCountAsync(string sqlQuery);

        [OperationContract]
		Task<CountReply> GetRecordCount(Empty empty, CallContext context = default);
	}
}
