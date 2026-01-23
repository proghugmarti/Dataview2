using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DataView2.Core.Models.Other.ProcessingState;

namespace DataView2.Core.Models.Other
{
    public class ProcessingState
    {
        public ProcessingStage Stage { get; set; }
        public double TotalPercentage { get; set; }
        public double StagePercentage { get; set; }
        public string LastMessage { get; set; }
        public DateTime LastUpdate { get; set; } = DateTime.UtcNow;

    }

    public enum ProcessingStage
    {
        Unknown,
        ReadingPrerequisites,
        ProcessingFIS,
        ProcessingXML,
        ProcessingVideo
    }
}
