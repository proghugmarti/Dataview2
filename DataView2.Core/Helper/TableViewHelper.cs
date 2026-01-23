using DataView2.Core.Models.CrackClassification;
using DataView2.Core.Models.LCMS_Data_Tables;
using DataView2.Core.Models.Other;
using DataView2.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataView2.Core.Helper
{
    public class TableViewHelper
    {
        public static List<string> TableViewList = new List<string>
        {
            "Boundary",
            "GPS_Processed",
            "Geometry_Processed",
            "LCMS_Bleeding",
            "LCMS_Concrete_Joints",
            "LCMS_Corner_Break",
            "LCMS_Cracking_Raw",
            "LCMS_CrackSummary",
            "LCMS_Curb_DropOff",
            "LCMS_FOD",
            "LCMS_Geometry_Processed",
            "LCMS_Grooves",
            "LCMS_Lane_Mark_Processed",
            "LCMS_Marking_Contour",
            "LCMS_MMO_Processed",
            "LCMS_PASER",
            "LCMS_Patch_Processed",
            "LCMS_PCI",
            "LCMS_PickOuts_Raw",
            "LCMS_Potholes_Processed",
            "LCMS_Pumping_Processed",
            "LCMS_Ravelling_Raw",
            "LCMS_Rough_Processed",
            "LCMS_Rumble_Strip",
            "LCMS_Rut_Processed",
            "LCMS_Sags_Bumps",
            "LCMS_Sealed_Cracks",
            "LCMS_Segment",
            "LCMS_Shove_Processed",
            "LCMS_Spalling_Raw",
            "LCMS_Texture_Processed",
            "LCMS_Water_Entrapment",
            "LCMS_Segment_Grid",
            "SummaryCrackClassifications",
            "VideoFrame",
            "Camera360Frame",
            "LASfile",
            "LasRutting",
            "SampleUnit",
            "SampleUnit_Set",
            "PCIRatings",
            "PCIDefects",
            "Survey"
        };
    }
}
