using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataView2.Core.Models.Other
{
    public class Video_Processed
    {
        public int Id { get; set; }
        public float Chainage { get; set; }
        public long LRPOffsetEnd { get; set; }
        public long LRPNum { get; set; }
        public float Frame { get; set; }
        public float GPSTime { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float Heading { get; set; }
        public float Pitch { get; set; }
        public float Roll { get; set; }
    }
}
