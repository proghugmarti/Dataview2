using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace DataView2.Core.Models.Other
{
    public class GeoJsonProperties
    {
        public string surveyId { get; set; }
        public string surveyDescription { get; set; }
        public string surveyInstruction { get; set; }
        public double startChainage { get; set; }
        public int direction { get; set; } //(1: Increment, 0: Decrement)
        public int lane { get; set; }
        public int gpsAutoStart { get; set; } //(0: false, 1: true)
        public int? gpsAutoStartType { get; set; } //(0:Point, 1:Boundary)
        public List<string> modules { get; set; }
        public string? operatorName { get; set; }
        public string? vehicleId { get; set; }
        public double? vehicleOdoCalibration { get; set; }
        public string? acquisitionCfgFile { get; set; }
        public string? analyserCfgFile { get; set; }
        public string? completedDate { get; set; }
        public string[][]? userDefinedFields { get; set; }
        public string Status { get; set; } = "New";
    }

    public class SampleUnitGeoJsonProperties
    {
        public string sampleUnitSet { get; set; }
        public int sampleUnitSetType { get; set; }
        public string sampleUnit { get; set; }
        public int? numOfSlabs { get; set; }
    }

    public class SummaryGeoJsonProperties
    {
        public string summaryName { get; set; }
        public string sampleUnitName { get; set; }
        public string sampleUnitSetName { get; set; }
        public string surveyId { get; set; }
        public string surveyName { get; set; }
        public List<SummaryItem> summaries { get; set; }
    }

    public class SampleUnitGeoJsonObject
    {
        public string type { get; set; }
        public PolygonGeometry geometry { get; set; }
        public SampleUnitGeoJsonProperties properties { get; set; }
    }

    public class SummaryGeoJsonObject
    {
        public string type { get; set; }
        public PolygonGeometry geometry { get; set; }
        public SummaryGeoJsonProperties properties { get; set; }
    }

    public class PolygonGeometry
    {
        public string type { get; set; }
        public List<List<List<double>>> coordinates { get; set; }
    }

    public class SampleUnitFeatureCollection
    {
        public string type { get; set; }
        public List<SampleUnitGeoJsonObject> features { get; set; }
    }

    public class SummaryFeatureCollection
    {
        public string type { get; set; }
        public List<SummaryGeoJsonObject> features { get; set; }
    }

    public class GeoJsonGeometry
    {
        public string type { get; set; }

        public double[][] coordinates { get; set; }
    }

    public class GeoJsonObject
    {
        public string type { get; set; }
        public GeoJsonGeometry geometry { get; set; }
        public GeoJsonProperties properties { get; set; }
    }

    public class GeoJsonObjectCSV
    {
        public string type { get; set; }
        public string coordinates { get; set; }
        public string surveyId { get; set; }
        public string surveyDescription { get; set; }
        public string surveyInstruction { get; set; }
        public double startChainage { get; set; }
        public int direction { get; set; } //(1: Increment, 0: Decrement)
        public int lane { get; set; }
        public int gpsAutoStart { get; set; } //(0: false, 1: true)
        public int? gpsAutoStartType { get; set; } //(0:Point, 1:Boundary)
        public string modules { get; set; }
        public string? operatorName { get; set; }
        public string? vehicleId { get; set; }
        public double? vehicleOdoCalibration { get; set; }
        public string? acquisitionCfgFile { get; set; }
        public string? analyserCfgFile { get; set; }
        public string? completedDate { get; set; }
        public string? userDefinedFields { get; set; }
        public string Status { get; set; } = "New";
    }

    public class GeoJsonMetadataCsv
    {
        public GeoJsonObjectCSV Properties { get; set; }
        public string FilePath { get; set; }  // File path for each JSON file
    }

    public class GeoJsonMetadata
    {
        public GeoJsonProperties Properties { get; set; }
        public string FilePath { get; set; }  // File path for each JSON file
    }

    public class GeoJsonPropertiesSegment
    {
        public int id { get; set; }
        public string file { get; set; }
        public string type { get; set; }
    }

    public class GeometrySegment
    {
        public string type { get; set; }
        public List<List<double[]>> coordinatesSegment { get; set; }
    }

    public class GeoJsonObjectSegment
    {
        public string type { get; set; }
        public GeometrySegment geometrySegment { get; set; }
        public GeoJsonPropertiesSegment propertiesSegment { get; set; }
    }
}
