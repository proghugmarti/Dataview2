using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataView2.Core.Models
{
    public class TableExport
    {
        [Key]
        public string RootPage { get; set; }
        public string Name { get; set; }
        public string File { get; set; }
    }
}
