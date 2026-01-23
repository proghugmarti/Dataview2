using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace DataView2.Core.Models.Other
{
    [DataContract]
    public class PavementTypeResponse
    {
        [DataMember(Order = 1)]
        public int UserDefinedPavementType { get; set; }

        [DataMember(Order = 2)]
        public int AutoPavementTypeDetection { get; set; }
    }
}
