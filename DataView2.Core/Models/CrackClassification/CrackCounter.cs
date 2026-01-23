using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataView2.Core.Models.CrackClassification
{
    public class CrackCounter
    {
        public int Id { get; set; }
        public string CrackType { get; set; } = String.Empty;

        public int Count { get; set; } = 0;

    }
}
