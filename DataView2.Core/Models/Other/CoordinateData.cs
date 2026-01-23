using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace DataView2.Core.Models.Other
{
    public class CoordinateData
    {
        public int TableId { get; set; }
        public string Id { get; set; }
        public string File { get; set; }
        public string Table { get; set; }
        public List<Double[]> Coordinates { get; set; }
        public Dictionary<string, object> Attributes { get; set; }
    }

    [DataContract]
    public class CoordinateRangeRequest
    {
        [DataMember(Order = 1)]
        public double MinLatitude { get; set; }

        [DataMember(Order = 2)]
        public double MaxLatitude { get; set; }

        [DataMember(Order = 3)]
        public double MinLongitude { get; set; }

        [DataMember(Order = 4)]
        public double MaxLongitude { get; set; }
    }
}
