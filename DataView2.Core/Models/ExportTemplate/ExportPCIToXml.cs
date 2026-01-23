using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace DataView2.Core.Models.ExportTemplate
{
    public class ExportPCIToXml
    {
        [XmlRoot("pavementData")]
        public class PavementData
        {
            [XmlElement("geospatialInspectionData")]
            public List<GeospatialInspectionData> GeospatialInspectionDataList { get; set; } = new();

            // The noNamespaceSchemaLocation attribute with xsi namespace
            [XmlAttribute("noNamespaceSchemaLocation", Namespace = "http://www.w3.org/2001/XMLSchema-instance")]
            public string NoNamespaceSchemaLocation { get; set; }
            public PavementData()
            {
                // Set the value for the xsi:noNamespaceSchemaLocation attribute
                NoNamespaceSchemaLocation = "PavementInspectionDataV2.xsd";

                // Adding the xsi namespace for the XML serialization
                XmlSerializerNamespaces namespaces = new XmlSerializerNamespaces();
                namespaces.Add("xsi", "http://www.w3.org/2001/XMLSchema-instance");
            }
        }

        public class GeospatialInspectionData
        {
            [XmlAttribute("inspectionDate")]
            public string InspectionDate { get; set; }

            [XmlAttribute("units")]
            public string Units { get; set; }

            [XmlAttribute("level")]
            public string Level { get; set; }

            [XmlElement("inspectedElement")]
            public List<InspectedElement> InspectedElements { get; set; } = new();
        }

        public class InspectedElement
        {
            [XmlAttribute("inspectedElementID")]
            public int InspectedElementID { get; set; }

            [XmlAttribute("size")]
            public double Size { get; set; }

            [XmlAttribute("PID")]
            public string PID { get; set; }

            [XmlElement("centerLocation")]
            public CenterLocation CenterLocation { get; set; } = new();

            [XmlElement("inspectionData")]
            public InspectionData InspectionData { get; set; } = new();
        }
        public class CenterLocation
        {
            [XmlElement("latitude")]
            public Latitude Latitude { get; set; } = new();

            [XmlElement("longitude")]
            public Longitude Longitude { get; set; } = new();
        }

        public class Latitude
        {
            [XmlAttribute("degrees")]
            public int Degrees { get; set; }

            [XmlAttribute("minutes")]
            public int Minutes { get; set; }

            [XmlAttribute("seconds")]
            public double Seconds { get; set; }

            [XmlAttribute("northSouth")]
            public string NorthSouth { get; set; } = "N";
        }

        public class Longitude
        {
            [XmlAttribute("degrees")]
            public int Degrees { get; set; }

            [XmlAttribute("minutes")]
            public int Minutes { get; set; }

            [XmlAttribute("seconds")]
            public double Seconds { get; set; }

            [XmlAttribute("eastWest")]
            public string EastWest { get; set; } = "E";
        }

        public class InspectionData
        {
            [XmlElement("PCIDistresses")]
            public PCIDistresses PCIDistresses { get; set; } = new();
        }

        public class PCIDistresses
        {
            [XmlElement("levelDistress")]
            public List<LevelDistress> LevelDistressList { get; set; } = new();
        }

        public class LevelDistress
        {
            [XmlAttribute("distressCode")]
            public int DistressCode { get; set; }
            [XmlAttribute("quantity")]
            public double Quantity { get; set; }

            [XmlAttribute("severity")]
            public string Severity { get; set; }

            [XmlAttribute("comment")]
            public string Comment { get; set; } = "";
        }
    }
}
