using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataView2.Core.Models
{
    public class Report
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string FunctionName { get; set; }
        public List<string>? Param1Type { get; set; }
        public string Param2Type { get; set; }
        public string Param3Type { get; set; }
    }
}
