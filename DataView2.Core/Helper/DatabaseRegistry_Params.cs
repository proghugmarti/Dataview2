using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace DataView2.Core.Helper
{
    [DataContract]
    public class DatabaseRegistry_Params
    {
        [DataMember(Order = 1)]
        public string DatasetName { get; set; }

        [DataMember(Order = 2)]
        public int ProjectId { get; set; }
    }
}
