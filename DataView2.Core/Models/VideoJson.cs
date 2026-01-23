using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataView2.Core.Models
{
    public class VideoJson
    {
        public string CameraName { get; set; } = string.Empty;
        public string CameraSerial { get; set; } = string.Empty;
        public string CameraType { get; set; } = string.Empty;
        public double CameraOffset { get; set; }
        public string SurveyUid { get; set; } = string.Empty;
        public List<SurveyVideoLocationTriggerRecord> SurveyVideoLocationTriggerRecordList { get; set; } = new List<SurveyVideoLocationTriggerRecord>();
    }
    

    public class SurveyVideoLocationTriggerRecord
    {
        public double Longitude { get; set; }
        public double Latitude { get; set; }
        public double Chainage { get; set; }
        public double HeadingAngle { get; set; }
        public long ImageNumber { get; set; }
        public long CameraTime { get; set; }
        public long PCTime { get; set; }
        public string ImageFilePath { get; set; } = string.Empty;
    }
}
