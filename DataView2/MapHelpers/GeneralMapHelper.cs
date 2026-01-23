using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using CsvHelper;
using DataView2.Core.Models.Other;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.UI;
using LiveChartsCore.Kernel;
using LiveChartsCore.SkiaSharpView.Drawing.Geometries;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static DataView2.Core.Helper.TableNameHelper;
using System.Text.Json;
using Markdig.Extensions.Tables;
using DataView2.Core.Helper;

namespace DataView2.MapHelpers
{
    public class GeneralMapHelper
    {
        public static double GetPolygonArea(List<List<double>> coordinates)
        {
            double area = 0.0;
            if (coordinates.All(innerList => innerList != null))
            {
                // Convert List<List<double>> to a collection of Points
                var points = new List<MapPoint>();

                foreach (var coordinate in coordinates)
                {
                    points.Add(new MapPoint(coordinate[0], coordinate[1], SpatialReferences.Wgs84));
                }

                // Create Polygon
                var polygon = new Polygon(points);

                // Calculate Area
                area = GeometryEngine.AreaGeodetic(polygon, AreaUnits.SquareMeters, GeodeticCurveType.Geodesic);
            }
            return area;
        }

        // Method to convert lat/lon from decimal degrees to DMS format
        public static (int degrees, int minutes, double seconds, string direction) ConvertToDMS(double decimalDegree, bool isLatitude)
        {
            // Determine direction (N/S for latitude, E/W for longitude)
            string direction;
            if (isLatitude)
                direction = decimalDegree >= 0 ? "N" : "S";
            else
                direction = decimalDegree >= 0 ? "E" : "W";

            // Convert to positive for calculation
            decimalDegree = Math.Abs(decimalDegree);

            // Extract degrees, minutes, and seconds
            int degrees = (int)decimalDegree;
            double fractionalMinutes = (decimalDegree - degrees) * 60;
            int minutes = (int)fractionalMinutes;
            double seconds = (fractionalMinutes - minutes) * 60;

            return (degrees, minutes, seconds, direction);
        }

        public static Graphic ParseSimpleGeoJson(string geoJson, Dictionary<string, object> extraAttributes = null)
        {
            var geoJsonObject = JObject.Parse(geoJson);
            var geometry = geoJsonObject["geometry"];
            var type = geometry["type"]?.ToString();
            var coordinatesArray = geometry["coordinates"];
            Dictionary<string, object> attributes = new Dictionary<string, object>();

            //properties
            var jsonProperties = geoJsonObject["properties"];
            var table = jsonProperties["type"].ToString();

            attributes.Add("Table", table);

            if (jsonProperties["diameter"] != null)
            {
                var diameter = jsonProperties["diameter"].ToString();
                attributes.Add("diameter", diameter);
            }

            if (jsonProperties["x"] != null && jsonProperties["y"] != null)
            {
                var x = jsonProperties["x"].ToString();
                var y = jsonProperties["y"].ToString();
                attributes.Add("x", x);
                attributes.Add("y", y);
            }

            if (extraAttributes != null)
            {
                foreach (var kvp in extraAttributes)
                {
                    attributes[kvp.Key] = kvp.Value;
                }
            }

            Graphic graphic = null;

            if (type == "Polygon")
            {
                var rings = new List<MapPoint>();
                foreach (var ring in coordinatesArray)
                {
                    foreach (var coord in ring)
                    {
                        var coordinate = new MapPoint(coord[0].Value<double>(), coord[1].Value<double>(),SpatialReferences.Wgs84);
                        rings.Add(coordinate);
                    }
                }
                var polygon = new Polygon(rings);
                graphic = new Graphic(polygon, attributes);

            }
            else if (type == "Polyline" || type == "LineString")
            {
                var points = coordinatesArray.Select(coord => new MapPoint(coord[0].Value<double>(), coord[1].Value<double>(), SpatialReferences.Wgs84)).ToList();
                var polyline = new Polyline(new List<MapPoint>(points));
                graphic = new Graphic(polyline, attributes);
            }
            else if (type == "Point")
            {
                var LcmsTableNames = TableNameHelper.GetAllLCMSOverlayIds();
                if (LcmsTableNames.Contains(table))
                {
                    var x = coordinatesArray[0].Value<double>();
                    var y = coordinatesArray[1].Value<double>();
                    var centerPoint = new MapPoint(x, y, SpatialReferences.Wgs84);
                    attributes["GeoType"] = "Point";
                    int diameter = 0;

                    if (table == LayerNames.Potholes)
                    {
                        if (double.TryParse(attributes["diameter"].ToString(), out double diameterValue))
                        {
                            diameter = (int)diameterValue / 2;
                        }
                    }
                    else if (table == LayerNames.Pickout)
                    {
                        diameter = 30;
                    }
                    else
                    {
                        diameter = 150;
                    }
                    var circleGeometry = GeometryEngine.BufferGeodetic(centerPoint, diameter, LinearUnits.Millimeters);
                    graphic = new Graphic(circleGeometry, attributes);
                }
                else
                {
                    var x = coordinatesArray[0].Value<double>();
                    var y = coordinatesArray[1].Value<double>();
                    var centerPoint = new MapPoint(x, y, SpatialReferences.Wgs84);
                    graphic = new Graphic(centerPoint, attributes);
                }
            }

            return graphic;
        }

        public class ListOfListDoubleConverter : DefaultTypeConverter
        {
            public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
                => JsonSerializer.Deserialize<double[][]>(text);
        }

        public class ListStringJsonConverter : DefaultTypeConverter
        {
            public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
                => JsonSerializer.Deserialize<List<string>>(text);
        }

        public class ListOfListStringConverter : DefaultTypeConverter
        {
            public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
                => JsonSerializer.Deserialize<string[][]>(text);
        }

        public sealed class GeometryMap : ClassMap<GeoJsonGeometry>
        {
            public GeometryMap()
            {
                Map(m => m.type).Name("type");
                Map(m => m.coordinates).Name("coordinates").TypeConverter<ListOfListDoubleConverter>();
            }
        }

        public sealed class GeoJsonPropertiesMap : ClassMap<GeoJsonProperties>
        {
            public GeoJsonPropertiesMap()
            {
                Map(m => m.surveyId).Name("surveyId");
                Map(m => m.surveyDescription).Name("surveyDescription");
                Map(m => m.surveyInstruction).Name("surveyInstruction");
                Map(m => m.startChainage).Name("startChainage");
                Map(m => m.direction).Name("direction");
                Map(m => m.lane).Name("lane");
                Map(m => m.gpsAutoStart).Name("gpsAutoStart");
                Map(m => m.gpsAutoStartType).Name("gpsAutoStartType");
                Map(m => m.modules).Name("modules").TypeConverter<ListStringJsonConverter>();
                Map(m => m.operatorName).Name("operatorName");
                Map(m => m.vehicleId).Name("vehicleId");
                Map(m => m.vehicleOdoCalibration).Name("vehicleOdoCalibration");
                Map(m => m.acquisitionCfgFile).Name("acquisitionCfgFile");
                Map(m => m.analyserCfgFile).Name("analyserCfgFile");
                Map(m => m.completedDate).Name("completedDate");
                Map(m => m.userDefinedFields).Name("userDefinedFields").TypeConverter<ListOfListStringConverter>();
                Map(m => m.Status).Name("Status");
            }
        }

        public sealed class GeoJsonObjectMap : ClassMap<GeoJsonObject>
        {
            public GeoJsonObjectMap()
            {
                Map(m => m.type).Name("type");
                References<GeometryMap>(m => m.geometry);
                References<GeoJsonPropertiesMap>(m => m.properties);
            }
        }

        public static Graphic GetClosestGraphicFromOverlay(GraphicsOverlay overlay, MapPoint clickedPoint, double distanceThresholdKm, Func<Graphic, bool> filter = null)
        {
            if (overlay == null || !overlay.IsVisible)
                return null;

            // Project the clicked point just one time.
            var projectedClickedPoint = (MapPoint)GeometryEngine.Project(clickedPoint, SpatialReferences.Wgs84);

            Graphic closestGraphic = null;
            double closestDistance = double.MaxValue;

            foreach (var graphic in overlay.Graphics)
            {
                if (filter != null && !filter(graphic))
                    continue;

                var projectedGeometry = GeometryEngine.Project(graphic.Geometry, SpatialReferences.Wgs84);
                MapPoint comparisonPoint = null;

                if (projectedGeometry is MapPoint graphicPoint)
                {
                    comparisonPoint = graphicPoint;
                }
                else if (projectedGeometry is Polygon polygon)
                {
                    comparisonPoint = GeometryEngine.LabelPoint(polygon);
                    //comparisonPoint = GeometryEngine.NearestCoordinate(polygon, projectedClickedPoint).Coordinate;
                }

                if (comparisonPoint != null)
                {
                    double distance = GeometryEngine.DistanceGeodetic(
                        projectedClickedPoint, comparisonPoint,
                        LinearUnits.Kilometers, null, GeodeticCurveType.Geodesic).Distance;

                    if (distance <= distanceThresholdKm && distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestGraphic = graphic;
                    }

                }
            }

            return closestGraphic;
        }

        public static List<Graphic> GetClosestGraphicsFromOverlay(GraphicsOverlay overlay, MapPoint clickedPoint, double distanceThresholdKm, Func<Graphic, bool> filter = null)
        {
            if (overlay == null || !overlay.IsVisible)
                return new List<Graphic>(); // Return an empty list if overlay is null or not visible

            var projectedClickedPoint = (MapPoint)GeometryEngine.Project(clickedPoint, SpatialReferences.Wgs84);
            List<Graphic> closestGraphics = new List<Graphic>();

            foreach (var graphic in overlay.Graphics)
            {
                if (filter != null && !filter(graphic))
                    continue;

                var projectedGeometry = GeometryEngine.Project(graphic.Geometry, SpatialReferences.Wgs84);
                MapPoint comparisonPoint = null;

                if (projectedGeometry is MapPoint graphicPoint)
                {
                    comparisonPoint = graphicPoint;
                }
                else if (projectedGeometry is Polygon polygon)
                {
                    comparisonPoint = GeometryEngine.LabelPoint(polygon);
                }

                if (comparisonPoint != null)
                {
                    double distance = GeometryEngine.DistanceGeodetic(
                        projectedClickedPoint, comparisonPoint,
                        LinearUnits.Kilometers, null, GeodeticCurveType.Geodesic).Distance;

                    if (distance <= distanceThresholdKm)
                    {
                        closestGraphics.Add(graphic); // Add to the list if within the threshold
                    }
                }
            }

            return closestGraphics; // Return the list of closest graphics
        }
    }
}
