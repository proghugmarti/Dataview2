using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace DataView2.Core.Helper
{
    [DataContract]
    public class Segment_Grid_Params
    {
        [DataMember(Order = 1)]
        public string SurveyId { get; set; }

        [DataMember(Order = 2)]
        public string SectionId { get; set; }
    }
}
