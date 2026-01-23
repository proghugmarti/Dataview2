using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataView2.Core.Models.Other
{
    public class LCMS_Geometry
    {
        public int Id { get; set; }
        public float Chainage { get; set; }
        public float Time { get; set; }
        public float Roll { get; set; }
        public float Pitch { get; set; }
        public float Yaw { get; set; }
        public float VelX { get; set; }
        public float VelY { get; set; }
        public float VelZ { get; set; }
        public float Latitude { get; set; }
        public float Longitude { get; set; }
        public float Altitude { get; set; }
        public float Count { get; set; }
        public float TimeStamp { get; set; }
        public string Status { get; set; }
        public float AccX { get; set; }
        public float AccY { get; set; }
        public float AccZ { get; set; }
        public float GyrX { get; set; }
        public float GyrY { get; set; }
        public float GyrZ { get; set; }
    }
}
