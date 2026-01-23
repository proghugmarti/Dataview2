using DataView2.Core.Models.LCMS_Data_Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataView2.Core.Helper
{
    public static class TableNameHelper
    {
        public static class LayerNames
        {
            public const string Segment = "Segment";
            public const string SegmentGrid = "Segment Grid";
            public const string Cracking = "Cracking";
            public const string ConcreteJoint = "Concrete Joint";
            public const string Patch = "Patch";
            public const string Pickout = "Pickout";
            public const string Potholes = "Potholes";
            public const string Ravelling = "Ravelling";
            public const string Spalling = "Spalling";
            public const string CornerBreak = "Corner Break";
            public const string FOD = "FOD";
            public const string Bleeding = "Bleeding";
            public const string CurbDropOff = "Curb DropOff";
            public const string MarkingContour = "Marking Contour";
            public const string Pumping = "Pumping";
            public const string SealedCrack = "Sealed Crack";
            public const string MMO = "MMO";
            public const string MacroTexture = "Macro Texture";
            public const string RumbleStrip = "Rumble Strip";
            public const string Roughness = "Roughness";
            public const string Rutting = "Rutting";
            public const string Shove = "Shove";
            public const string Geometry = "Geometry";
            public const string SagsBumps = "Sags Bumps";
            public const string PpfErd = "PPF ERD";
            public const string Grooves = "Grooves";
            public const string WaterEntrapment = "Water Entrapment";
            public const string PCI = "PCI";
            public const string PASER = "PASER";
            public const string CrackFaulting = "Crack Faulting";
            public const string CrackClassification = "Crack Classification";
            public const string LaneMarking = "Lane Marking";
            public const string CrackSummary = "Crack Summary";
            public const string Keycode = "Keycode";

        }

        public static readonly List<(string LayerName, string DBName, string ServiceName)> TableNameMappings = new()
        {
            //lcms layers
            ( LayerNames.Segment, "LCMS_Segment", "LCMS_Data_Services.SegmentService" ),
            ( LayerNames.ConcreteJoint, "LCMS_Concrete_Joints", "LCMS_Data_Services.ConcreteJointService"),
            ( LayerNames.CornerBreak, "LCMS_Corner_Break", "LCMS_Data_Services.CornerBreakService"),
            ( LayerNames.Spalling, "LCMS_Spalling_Raw", "LCMS_Data_Services.SpallingRawService"),
            ( LayerNames.Cracking, "LCMS_Cracking_Raw", "LCMS_Data_Services.CrackingRawService" ),
            ( LayerNames.CrackSummary, "LCMS_CrackSummary", "LCMS_Data_Services.CrackSummaryService"),
            ( LayerNames.SegmentGrid, "LCMS_Segment_Grid", "LCMS_Data_Services.SegmentGridService"),
            ( LayerNames.SealedCrack, "LCMS_Sealed_Cracks", "LCMS_Data_Services.SealedCrackService"),
            ( LayerNames.Pickout, "LCMS_PickOuts_Raw", "LCMS_Data_Services.PickOutRawService"),
            ( LayerNames.Potholes, "LCMS_Potholes_Processed", "LCMS_Data_Services.PotholesService"),
            ( LayerNames.Ravelling, "LCMS_Ravelling_Raw", "LCMS_Data_Services.RavellingRawService"),
            ( LayerNames.Bleeding, "LCMS_Bleeding", "LCMS_Data_Services.BleedingService"),
            ( LayerNames.CurbDropOff, "LCMS_Curb_DropOff", "LCMS_Data_Services.CurbDropOffService"),
            ( LayerNames.Patch, "LCMS_Patch_Processed", "LCMS_Data_Services.PatchService"),
            ( LayerNames.MarkingContour, "LCMS_Marking_Contour", "LCMS_Data_Services.MarkingContourService"),
            ( LayerNames.MMO, "LCMS_MMO_Processed", "LCMS_Data_Services.MMOService"),
            ( LayerNames.Pumping, "LCMS_Pumping_Processed", "LCMS_Data_Services.PumpingService"),
            ( LayerNames.SagsBumps, "LCMS_Sags_Bumps", "LCMS_Data_Services.SagsBumpsService"),
            ( LayerNames.Roughness, "LCMS_Rough_Processed", "LCMS_Data_Services.RoughnessService"),
            ( LayerNames.Rutting, "LCMS_Rut_Processed", "LCMS_Data_Services.RutProcessedService"),
            ( LayerNames.RumbleStrip, "LCMS_Rumble_Strip", "LCMS_Data_Services.RumbleStripService"),
            ( LayerNames.Shove, "LCMS_Shove_Processed", "LCMS_Data_Services.ShoveService"),
            ( LayerNames.MacroTexture, "LCMS_Texture_Processed", "LCMS_Data_Services.MacroTextureService"),
            ( LayerNames.Geometry, "LCMS_Geometry_Processed", "LCMS_Data_Services.GeometryService"),
            ( LayerNames.Grooves, "LCMS_Grooves", "LCMS_Data_Services.GroovesService"),
            ( LayerNames.WaterEntrapment, "LCMS_Water_Entrapment", "LCMS_Data_Services.WaterTrapService"),
            ( LayerNames.PCI, "LCMS_PCI", "LCMS_Data_Services.PCIService"),
            ( LayerNames.PASER, "LCMS_PASER", "LCMS_Data_Services.PASERService"),
            ( LayerNames.FOD, "LCMS_FOD", "LCMS_Data_Services.FODService"),
            //( LayerNames.LaneMarking, "LCMS_Lane_Mark_Processed", "LCMS_Data_Services.LaneMarkedProcessedService"), got no geojson & not shown on the map

            ( "INS Geometry", "Geometry_Processed", "OtherServices.INSGeometryService"),

            //other layers
            ( "Boundary", "Boundary", "OtherServices.BoundariesService"),
            ( "LasPoints", "LASfile", "OtherServices.LASfileService"),
            ( "LasRutting", "LAS_Rutting" , "OtherServices.LAS_RuttingService"),
            ( LayerNames.Keycode, "Keycode", "KeyCodeService"),
        };

        public static Dictionary<string, IEnumerable<string>> MultiLayerNameMappings = new()
        {
            { LayerNames.Roughness, new List<string> { MultiLayerName.LwpIRI, MultiLayerName.RwpIRI, MultiLayerName.LaneIRI, MultiLayerName.CwpIRI }.AsReadOnly() },
            { LayerNames.Rutting, new List<string>() { MultiLayerName.LeftRut, MultiLayerName.RightRut, MultiLayerName.LaneRut }.AsReadOnly() },
            { LayerNames.SegmentGrid, new List<string>() { MultiLayerName.Longitudinal, MultiLayerName.Transversal, MultiLayerName.Fatigue}.AsReadOnly() },
            { LayerNames.MacroTexture, new List<string>(){ MultiLayerName.BandTexture, MultiLayerName.AverageTexture }.AsReadOnly() }
        };

        public static class MultiLayerName
        {
            public const string LwpIRI = "Lwp IRI";
            public const string RwpIRI = "Rwp IRI";
            public const string CwpIRI = "Cwp IRI";
            public const string LaneIRI = "Lane IRI";
            public const string LeftRut = "Left Rut";
            public const string RightRut = "Right Rut";
            public const string LaneRut = "Lane Rut";
            public const string Longitudinal = "Longitudinal";
            public const string Transversal = "Transversal";
            public const string Fatigue = "Fatigue";
            public const string BandTexture = "Band Texture";
            public const string AverageTexture = "Average Texture";
        }

        public static List<string> PCIAutoCalculatedDefects = new List<string> { "Bleeding", "Ravelling", "Popouts (Pickouts)" };

        public static string GetDBTableName(string table)
        {
            var mapping = TableNameMappings.FirstOrDefault( x => x.LayerName == table );
            return mapping != default ? mapping.DBName : table;
        }

        public static string GetOriginalTableName(string dbTable)
        {
            var mapping = TableNameMappings.FirstOrDefault(t => t.DBName == dbTable);
            return mapping != default ? mapping.LayerName : dbTable;
        }

        public static List<string> GetAllLCMSTables()
        {
            return TableNameMappings.Where(t=> t.DBName.StartsWith("LCMS")).Select(t => t.LayerName).ToList();
        }

        public static List<string> GetAllLCMSOverlayIds()
        {
            var lcmsTableNames = TableNameMappings.Where(t => t.DBName.StartsWith("LCMS")).Select(t => t.LayerName).ToList();
            var multiLayerNames = new List<string>
            {
                MultiLayerName.LwpIRI, MultiLayerName.RwpIRI, MultiLayerName.LaneIRI, MultiLayerName.CwpIRI,
                MultiLayerName.LeftRut, MultiLayerName.RightRut, MultiLayerName.LaneRut,
                MultiLayerName.Longitudinal, MultiLayerName.Transversal, MultiLayerName.Fatigue
            };
            lcmsTableNames.AddRange(multiLayerNames);
            return lcmsTableNames;
        }
    }
}
