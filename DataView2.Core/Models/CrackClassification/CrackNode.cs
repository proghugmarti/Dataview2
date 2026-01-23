using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataView2.Core.Models.CrackClassification
{
    public class CrackNode
    {
        public int CrackId  { get; set; }

        public virtual Crack? Crack  { get; set; }
        
        public double X { get; set; }
        public double Y { get; set; }

        public int colX { get; set; }
        public int colY { get; set; }


        public double Width { get; set; }
        public double Depth { get; set; }

        public int Type { get; set; }

    }
}
