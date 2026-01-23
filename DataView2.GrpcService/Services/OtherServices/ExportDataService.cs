using CsvHelper;
using DataView2.Core.Communication;
using DataView2.Core.Helper;
using DataView2.Core.Models;
using DataView2.Core.Models.ExportTemplate;
using DataView2.Core.Models.LCMS_Data_Tables;
using DataView2.Core.Models.Other;
using DataView2.Core.Models.QC;
using DataView2.GrpcService.Data;
using DataView2.GrpcService.Data.Projects;
using DataView2.GrpcService.Protos;
using DataView2.GrpcService.Services.AppDbServices;
using DotSpatial.Data;
using Esri.ArcGISRuntime.Mapping;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using Newtonsoft.Json.Linq;
using Serilog;
using SQLitePCL;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Globalization;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using static DataView2.Core.Helper.TableNameHelper;
using static DataView2.Core.Helper.Tools;
using Feature = NetTopologySuite.Features.Feature;
using IFeature = NetTopologySuite.Features.IFeature;

namespace DataView2.GrpcService.Services
{

    public class ExportDataService : Protos.ExportDataService.ExportDataServiceBase
    {
        private readonly AppDbContextProjectData _appDbContext;
        private readonly AppDbContextMetadataLocal _metadataContext;
        private readonly ImageBandInfoService _imageBandInfoService;
        private readonly DatabasePathProvider _databasePathProvider;

        private string connString;
        public ExportDataService(DatabasePathProvider databasePathProvider, AppDbContextProjectData appDbContext, ImageBandInfoService imageBandInfoService, AppDbContextMetadataLocal metadataContext)
        {
            _databasePathProvider = databasePathProvider;
            string actualDatabasePath = _databasePathProvider.GetDatasetDatabasePath();


            connString = $"Data Source={actualDatabasePath};";
            _appDbContext = appDbContext;
            _imageBandInfoService = imageBandInfoService;
            _metadataContext = metadataContext;
        }

        public override async Task<RecreateOverlayResponse> RecreateOverlay(RecreateOverlayRequest request, ServerCallContext context)
        {
            var response = new RecreateOverlayResponse();

            try
            {
                var segments = _appDbContext.LCMS_Segment.Where(x => x.SurveyId == request.SurveyId).ToList();
                if (segments != null && segments.Count > 0)
                {
                    var overlayResponse = await _imageBandInfoService.GenerateOverlayImages(segments, request.ImageTypes.ToList(), request.Overlays.ToList());
                    response.TotalFiles = overlayResponse.totalFiles;
                    response.FailedFiles = overlayResponse.failedFiles;
                    foreach (var msg in overlayResponse.errorMessages)
                    {
                        response.ErrorMessages.Add(msg);
                    }
                }
            }
            catch (Exception ex)
            {
                response.ErrorMessages.Add($"Unexpected error: {ex.Message}");
            }

            return response;
        }

        public override Task<ExportDataObjResponse> ExportData(ExportDataObjRequest request, ServerCallContext context)
        {
            return Task.FromResult<ExportDataObjResponse>(new ExportDataObjResponse
            {
                //ExportDataFinal exports the data as per the YUMA requirements, added function to write files with all data from all LCMS tables
                //Message = ExportDataFinal(request.SelectedSurveys, request.SavePathDirectory, request.Csv, request.Kmz, request.Shp).Result

                Message = ExportDataFinalGeneric(request).Result
            });
        }

        public List<string> GetDbTableNames()
        {
            try
            {
                var entityTypes = _appDbContext.Model.GetEntityTypes();

                // Extract table names from entity types
                var tableNames = entityTypes.Select(entityType =>
                {
                    var entityTypeName = entityType.ClrType.Name;
                    return entityTypeName;
                }).ToList();

                return tableNames;
            }
            catch (Exception ex)
            {
                Log.Error($"Error in getting tables {ex.Message}");
                return new List<string>();
            }
        }
      
        public void CreateMainKml(string directory, List<string> kmlFileNames)
        {
            string docKmlPath = Path.Combine(directory, "doc.kml");

            using (var writer = new StreamWriter(docKmlPath))
            {
                writer.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                writer.WriteLine("<kml xmlns=\"http://www.opengis.net/kml/2.2\">");
                writer.WriteLine("<Document>");
                writer.WriteLine("<name>LCMS_Defects_KMZ</name>");

                foreach (var kmlFileName in kmlFileNames)
                {
                    writer.WriteLine($"<NetworkLink>");
                    writer.WriteLine($"<name>{Path.GetFileNameWithoutExtension(kmlFileName)}</name>");
                    writer.WriteLine($"<Link>");
                    writer.WriteLine($"<href>{kmlFileName}</href>");
                    writer.WriteLine($"</Link>");
                    writer.WriteLine($"</NetworkLink>");
                }

                writer.WriteLine("</Document>");
                writer.WriteLine("</kml>");
            }
        }

        public async Task SaveToCsvDynamic(List<ExpandoObject> reportData, string filename, string saveFilepath, string CoordinatesSystem)
        {
            try
            {
                string csvFolderPath = saveFilepath + $"\\CSV".Replace("\\", "/");
                string filepath = csvFolderPath + $"\\{filename}".Replace("\\", "/");
                string latitudeCoordinateTransformation = "";
                string longitudeCoordinateTransformation = "";
                bool transformCoordinates = false;

                if (!Directory.Exists(csvFolderPath))
                    Directory.CreateDirectory(csvFolderPath);
                if (!Directory.Exists(saveFilepath))
                    Directory.CreateDirectory(saveFilepath);

                using var writer = new StreamWriter(filepath);
                using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

                //Exit when not results
                if (!reportData.Any()) { /*lblMessage = "No data found to save for" + filename; StateHasChanged();*/ return; }

                string columnName = "SegmentId";
                if (reportData.All(x => ((IDictionary<string, object>)x).ContainsKey(columnName)))
                {
                    var sortedList = reportData.OrderBy(x => ((IDictionary<string, object>)x)[columnName]).ToList();
                    reportData = sortedList;
                }

                var firstResult = reportData.First();
                var headers = ((IDictionary<string, object>)firstResult).Keys.ToList();

                if (!headers.Contains("SurveyIdExternal"))
                {
                    headers.Add("SurveyIdExternal");
                }

                //Coordinates formats:
                if (!string.IsNullOrEmpty(CoordinatesSystem) && System.Enum.TryParse<cstFormats>(CoordinatesSystem.Replace("-", "_"), out var format) && format != cstFormats.WGS84)
                {
                    latitudeCoordinateTransformation = $"GPSLatitude_{CoordinatesSystem}";
                    longitudeCoordinateTransformation = $"GPSLongitude_{CoordinatesSystem}";
                    headers.Add(latitudeCoordinateTransformation);
                    headers.Add(longitudeCoordinateTransformation);
                    transformCoordinates = true;
                }

                // Write Headers
                foreach (var header in headers)
                {
                    csv.WriteField(header);
                }
                csv.NextRecord();

                // Write data
                foreach (var result in reportData)
                {
                    //Coordinates formats:
                    Core.Communication.GPSCoordinate coordinate = new Core.Communication.GPSCoordinate();
                    coordinate.Latitude = 0.0;
                    coordinate.Longitude = 0.0;
                    coordinate.Altitude = 0.0;
                    if (((IDictionary<string, object>)result).TryGetValue("GPSLatitude", out var latValue) &&
                        latValue != null && double.TryParse(latValue.ToString(), out double latitude) && !double.IsNaN(latitude))
                    {
                        coordinate.Latitude = latitude;
                    }

                    if (((IDictionary<string, object>)result).TryGetValue("GPSLongitude", out var lonValue) &&
                        lonValue != null && double.TryParse(lonValue.ToString(), out double longitude) && !double.IsNaN(longitude))
                    {
                        coordinate.Longitude = longitude;
                    }

                    if (((IDictionary<string, object>)result).TryGetValue("GPSAltitude", out var altValue) &&
                        lonValue != null && double.TryParse(lonValue.ToString(), out double altitude) && !double.IsNaN(altitude))
                    {
                        coordinate.Altitude = altitude;
                    }


                    foreach (var header in headers)
                    {
                        if (header == "SurveyDate")
                        {
                            if (((IDictionary<string, object>)result).TryGetValue(header, out var objDate))
                            {
                                string strDate = convertToUtcDateTime(objDate);
                                ((IDictionary<string, object>)result)[header] = strDate;
                                csv.WriteField(strDate);
                            }
                            else
                            {
                                csv.WriteField("");
                            }
                        }
                        else if (header == "SurveyId")
                        {
                            if (((IDictionary<string, object>)result).TryGetValue(header, out var surveyIdValue))
                            {
                                var surveyIdStr = surveyIdValue?.ToString();
                                var matchingSurvey = _appDbContext.Survey.FirstOrDefault(x => x.SurveyIdExternal == surveyIdStr);
                                var surveyName = matchingSurvey?.SurveyName ?? surveyIdStr;
                                csv.WriteField(surveyName);
                            }
                            else
                            {
                                csv.WriteField("");
                            }
                        }
                        else if (header == "SurveyIdExternal")
                        {
                            if (((IDictionary<string, object>)result).TryGetValue("SurveyId", out var surveyIdValue))
                            {
                                csv.WriteField(surveyIdValue?.ToString());
                            }
                            else
                            {
                                csv.WriteField("");
                            }
                        }
                        else
                        {
                            if (((IDictionary<string, object>)result).TryGetValue(header, out var value))
                            {
                                csv.WriteField(value);
                            }
                            else if (header != latitudeCoordinateTransformation && header != longitudeCoordinateTransformation)
                            {
                                csv.WriteField("");
                            }
                        }

                    }
                    if (transformCoordinates && System.Enum.TryParse<cstFormats>(CoordinatesSystem.Replace("-", "_"), out var formatC) && formatC != cstFormats.WGS84)
                    {
                        //coordTransformationSystem = new CoordinatesTransformationSystem();
                        //GPSCoordinate coordinatesTransformed = await CoordinatesTransformationSystem.ConvertWGS84ToLocal2DAsync(coordinate.Latitude, coordinate.Longitude, coordinate.Altitude);
                        //GPSCoordinate coordinatesTransformed = await coordTransformationSystem.TransformCoordinatesProjNet(CoordinatesSystem);

                        var point = CoordinateHelper.TransformCoordinate(coordinate.Latitude, coordinate.Longitude, 16370);

                        csv.WriteField(point.X.ToString());
                        csv.WriteField(point.Y.ToString());
                    }
                    csv.NextRecord();
                }
                //lblMessage = filename + " CSV generated"; StateHasChanged();
            }
            catch (Exception ex)
            {
                Log.Error("Failed to save CSV of " + filename + " " + ex.Message);
            }
        }
        //==========> Generate KML Files
        public void SaveToKmlDynamic(List<ExpandoObject> reportData, string filename, string saveFilepath, string tableName, HashSet<string> globalLayerStyles)
        {
            try
            {   
                string kmlPath = Path.Combine(saveFilepath, filename);

                Directory.CreateDirectory(saveFilepath);

                using var writer = new StreamWriter(kmlPath);
                writer.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                writer.WriteLine("<kml xmlns=\"http://www.opengis.net/kml/2.2\">");
                writer.WriteLine("<Document>");

                //Stored layer styles used in the KML file
                string layerName = TableNameMappings.FirstOrDefault(m => m.DBName == tableName).LayerName ?? tableName;
                var layerColorCodes = _metadataContext.ColorCodeInformation
                    .Where(m => m.TableName == layerName)
                    .ToList();

                foreach (var result in reportData)
                {
                    try
                    {
                        string colorRow = "ffff0000"; // default blue

                        var dict = (IDictionary<string, object>)result;
                        string styleId = null;
                        string layerType = null;
                        string layerTypeName = "";

                        if (dict.TryGetValue("GeoJSON", out var geoJsonObj) && !string.IsNullOrWhiteSpace(geoJsonObj?.ToString()))
                        {
                            using var jsonDoc = JsonDocument.Parse(geoJsonObj.ToString());
                            if (jsonDoc.RootElement.TryGetProperty("geometry", out var geometryElem) &&
                                geometryElem.TryGetProperty("type", out var typeElem))
                            {
                                layerType = typeElem.GetString();
                            }
                        }

                        layerTypeName = layerType switch
                        {
                            "Polyline" => "polylineStyle",
                            "Polygon" => "polygonStyle",
                            "MultiPolygon" => "polygonStyle",
                            "Point" => "pointStyle",
                            _ => "polylineStyle"
                        };

                        if (layerColorCodes.Any())
                        {
                            foreach (var cc in layerColorCodes)
                            {
                                if (!dict.TryGetValue(cc.Property, out var value) || value == null)
                                    continue;

                                if (cc.IsStringProperty)
                                {
                                    if (value.ToString().Equals(cc.StringProperty, StringComparison.OrdinalIgnoreCase))
                                    {
                                        colorRow = ConvertHexToKmlColor(cc.HexColor);
                                        styleId = BuildStyleId(layerTypeName, layerName, cc.StringProperty);
                                        break;
                                    }
                                }
                                else if (double.TryParse(value.ToString(), out double numericValue))
                                {
                                    bool inRange = cc.IsAboveFrom
                                        ? numericValue >= cc.MinRange
                                        : numericValue >= cc.MinRange && numericValue <= cc.MaxRange;

                                    if (inRange)
                                    {
                                        colorRow = ConvertHexToKmlColor(cc.HexColor);
                                        var rangeString = $"{cc.MinRange }_{cc.MaxRange}";
                                        styleId = BuildStyleId(layerTypeName, layerName, rangeString);
                                        break;
                                    }
                                }
                            }
                        }
                        else
                        {
                            //if no color code found, use MapGraphic
                            styleId = BuildStyleId(layerTypeName, layerName);

                            var mapGraphic = _metadataContext.MapGraphic.FirstOrDefault(x => x.Name == layerName);
                            if (mapGraphic != null)
                            {
                                colorRow = ConvertMapGraphicColor(
                                    mapGraphic.Red.ToString(),
                                    mapGraphic.Green.ToString(),
                                    mapGraphic.Blue.ToString(),
                                    mapGraphic.Alpha.ToString(), "kml"
                                );
                            }
                        }

                        //Handling KML Styles
                        if (!string.IsNullOrEmpty(styleId))
                        {                           
                            var fileKey = System.IO.Path.GetFileName(filename);
                            var compositeKey = $"{fileKey}:::{styleId}".ToLowerInvariant();

                            // HashSet.Add 
                            if (globalLayerStyles.Add(compositeKey))
                            {
                                if (layerTypeName == "circleStyle")
                                {     
                                    writer.WriteLine($"<Style id=\"{styleId}\">");
                                    writer.WriteLine("<LineStyle>");
                                    writer.WriteLine($"<color>{colorRow}</color>");
                                    writer.WriteLine("<width>2</width>");
                                    writer.WriteLine("</LineStyle>");
                                    writer.WriteLine("<PolyStyle>");
                                    writer.WriteLine($"<color>{colorRow}</color>");
                                    writer.WriteLine("</PolyStyle>");
                                    writer.WriteLine("</Style>");
                                }
                                else if (layerTypeName == "pointStyle")
                                {                                    
                                    //Using little fixed ratio:
                                    writer.WriteLine($"<Style id=\"{styleId}\">");
                                    writer.WriteLine("<LineStyle>");
                                    writer.WriteLine($"<color>{colorRow}</color>");
                                    writer.WriteLine("<width>2</width>");
                                    writer.WriteLine("</LineStyle>");
                                    writer.WriteLine("<PolyStyle>");
                                    writer.WriteLine($"<color>{colorRow}</color>");
                                    writer.WriteLine("</PolyStyle>");
                                    writer.WriteLine("</Style>");
                                }
                                else
                                {
                                    writer.WriteLine($"<Style id=\"{styleId}\">");
                                    writer.WriteLine("<LineStyle>");
                                    writer.WriteLine($"<color>{colorRow}</color>");
                                    writer.WriteLine("<width>2</width>");
                                    writer.WriteLine("</LineStyle>");
                                    if (layerTypeName == "polygonStyle")
                                    {
                                        writer.WriteLine("<PolyStyle>");
                                        if (tableName == "LCMS_Segment")
                                            writer.WriteLine($"<color>66D3D3D3</color>"); // Fill color with transparency 
                                        else
                                            writer.WriteLine($"<color>{colorRow}</color>"); // Fill color 
                                        writer.WriteLine("</PolyStyle>");
                                    }
                                    writer.WriteLine("</Style>");
                                }

                            }
                        }

                        writer.WriteLine("<Placemark>");
                        writer.WriteLine("<description><![CDATA[");
                        writer.WriteLine($"<p><b>Table Name:</b> {tableName}</p>");
                        foreach (var key in ((IDictionary<string, object>)result).Keys)
                        {
                            writer.WriteLine($"<p><b>{key}:</b> {((IDictionary<string, object>)result)[key]}</p>");
                        }
                        writer.WriteLine("]]></description>");

                        var geoJson = ((IDictionary<string, object>)result)["GeoJSON"]?.ToString();
                        if (!string.IsNullOrEmpty(geoJson))
                        {
                            var geoJsonElement = JsonDocument.Parse(geoJson).RootElement;
                            GenerateKmlGeometry(writer, geoJsonElement, styleId);
                        }
                        writer.WriteLine("</Placemark>");
                    }
                    catch (Exception ex) { Log.Error("Failed to save CDATA of " + tableName + " in " + ex.Message); }

                }

                writer.WriteLine("</Document>");
                writer.WriteLine("</kml>");
            }
            catch (Exception ex)
            {
                Log.Error("Failed to save KML of " + filename + " " + ex.Message);
            }
        }
        private void GenerateKmlGeometry(StreamWriter writer, JsonElement geoJsonElement, string styleId = null)
        {
            var geometry = geoJsonElement.GetProperty("geometry");
            var properties = geoJsonElement.GetProperty("properties");
            var type = geometry.GetProperty("type").GetString();
            var coordinates = geometry.GetProperty("coordinates");
            var featureType = properties.GetProperty("type").GetString();

            switch (type)
            {
                case "Point" when styleId?.Contains("circleStyle") == true:                    
                    GeneratePoinCircletKml(writer, geoJsonElement, "Circle", styleId);
                    break;

                case "Point" when styleId?.Contains("pointStyle") == true:
                    //Using little fixed ratio:
                    GeneratePoinCircletKml(writer, geoJsonElement, "Point", styleId);
                    break;

                case "Polyline":
                    if (!string.IsNullOrEmpty(styleId))
                        writer.WriteLine($"<styleUrl>#{styleId}</styleUrl>");                    
                    writer.WriteLine("<LineString>");
                    writer.WriteLine("<coordinates>");
                    foreach (var coord in coordinates.EnumerateArray())
                    {
                        writer.WriteLine($"{coord[0]},{coord[1]} ");
                    }
                    writer.WriteLine("</coordinates>");
                    writer.WriteLine("</LineString>");
                    break;

                case "Polygon":                   
                    if (!string.IsNullOrEmpty(styleId))
                        writer.WriteLine($"<styleUrl>#{styleId}</styleUrl>");
                    writer.WriteLine("<Polygon>");
                    writer.WriteLine("<outerBoundaryIs>");
                    writer.WriteLine("<LinearRing>");
                    writer.WriteLine("<coordinates>");
                    foreach (var coord in coordinates[0].EnumerateArray()) 
                    {
                        writer.WriteLine($"{coord[0]},{coord[1]} ");
                    }
                    writer.WriteLine("</coordinates>");
                    writer.WriteLine("</LinearRing>");
                    writer.WriteLine("</outerBoundaryIs>");
                    writer.WriteLine("</Polygon>");
                    break;

                case "MultiPolygon":
                    //No styles applied for now --> it is a bit complicated
                    //if (!string.IsNullOrEmpty(styleId))
                    //    writer.WriteLine($"<styleUrl>#{styleId}</styleUrl>");
                    writer.WriteLine("<MultiGeometry>");
                    if (!string.IsNullOrEmpty(styleId))
                        writer.WriteLine($"<styleUrl>#{styleId}</styleUrl>");
                    foreach (var polygon in coordinates.EnumerateArray())
                    {
                        writer.WriteLine("<Polygon>");
                        writer.WriteLine("<outerBoundaryIs>");
                        writer.WriteLine("<LinearRing>");
                        writer.WriteLine("<coordinates>");
                        foreach (var coord in polygon.EnumerateArray())
                        {
                            writer.WriteLine($"{coord[0]},{coord[1]} ");
                        }
                        writer.WriteLine("</coordinates>");
                        writer.WriteLine("</LinearRing>");
                        writer.WriteLine("</outerBoundaryIs>");
                        writer.WriteLine("</Polygon>");
                    }
                    writer.WriteLine("</MultiGeometry>");
                    break;
                default:
                    throw new NotSupportedException($"Geometry type '{type}' is not supported.");
            }
        }
        private void GeneratePoinCircletKml(StreamWriter writer, JsonElement geoJsonElement, string layer, string styleId = null)
        {
            var geometry = geoJsonElement.GetProperty("geometry");
            var properties = geoJsonElement.GetProperty("properties");
            
            var coordinates = geometry.GetProperty("coordinates");
            double centerLon = coordinates[0].GetDouble();
            double centerLat = coordinates[1].GetDouble();

            // Radio: diameter (mm) → m → ratio
            double diameterMm = layer == "Circle" ? (properties.TryGetProperty("diameter", out var diameterElem) ? diameterElem.GetDouble() : 1000.0) : 60.95;
            double radiusMeters = (diameterMm / 1000.0) / 2.0;

            // Number of vertices to approximate the circle
            int numPoints = 48;

            // Apply circle style
            if (!string.IsNullOrEmpty(styleId))
                writer.WriteLine($"<styleUrl>#{styleId}</styleUrl>");

            writer.WriteLine("<Polygon>");
            writer.WriteLine("  <outerBoundaryIs>");
            writer.WriteLine("    <LinearRing>");
            writer.WriteLine("      <coordinates>");

            double earthRadius = 6378137.0; // Earth half ratio in meters
            double latRadians = centerLat * Math.PI / 180.0;

            for (int i = 0; i <= numPoints; i++) // <= to close the circle
            {
                double angle = i * 2 * Math.PI / numPoints;
                double dx = radiusMeters * Math.Cos(angle);
                double dy = radiusMeters * Math.Sin(angle);

                double newLat = centerLat + (dy / earthRadius) * (180 / Math.PI);
                double newLon = centerLon + (dx / (earthRadius * Math.Cos(latRadians))) * (180 / Math.PI);

                writer.WriteLine($"        {newLon},{newLat},0");
            }

            writer.WriteLine("      </coordinates>");
            writer.WriteLine("    </LinearRing>");
            writer.WriteLine("  </outerBoundaryIs>");
            writer.WriteLine("</Polygon>");
        }

        private string ConvertHexToKmlColor(string rgbaHex)
        {
            if (string.IsNullOrWhiteSpace(rgbaHex) || !rgbaHex.StartsWith("#") || rgbaHex.Length != 9)
                throw new ArgumentException("Expected format: #RRGGBBAA");

            string rr = rgbaHex.Substring(1, 2);
            string gg = rgbaHex.Substring(3, 2);
            string bb = rgbaHex.Substring(5, 2);
            string aa = rgbaHex.Substring(7, 2);

            return aa + bb + gg + rr; // KML format: AABBGGRR
        }

        private string ConvertMapGraphicColor(string redColor, string greenColor, string blueColor, string alpha, string targetFormat)
        {
            bool rOk = byte.TryParse(redColor, out byte r);
            bool gOk = byte.TryParse(greenColor, out byte g);
            bool bOk = byte.TryParse(blueColor, out byte b);
            bool aOk = byte.TryParse(alpha, out byte a);

            if (!rOk || !gOk || !bOk || !aOk)
            {
                // Fallback to white
                return targetFormat == "shp" ? "#ffffffff" : "ffffffff";
            }

            return targetFormat switch
            {
                "shp" => $"#{a:X2}{r:X2}{g:X2}{b:X2}".ToLower(),  // #AARRGGBB
                "kml" => $"{a:X2}{b:X2}{g:X2}{r:X2}".ToLower(),   // AABBGGRR
                _ => "ffffffff" // default fallback
            };
        }

        //==========> Generate Shape Files
        public void SaveToShapefile(List<ExpandoObject> data, string filename, string saveFilepath, string tableName)
        {
            try
            {
                string shapefilePath = Path.Combine(saveFilepath, filename);

                if (!Directory.Exists(saveFilepath))
                    Directory.CreateDirectory(saveFilepath);

                string layerName = TableNameMappings.FirstOrDefault(m => m.DBName == tableName).LayerName ?? tableName;
                var layerColorCodes = _metadataContext.ColorCodeInformation
                    .Where(m => m.TableName == layerName)
                    .ToList();

                var features = new List<IFeature>();
                var geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

                foreach (IDictionary<string, object> record in data)
                {
                    try
                    {
                        if (!record.ContainsKey("GeoJSON"))
                            continue;

                        string geoJson = record["GeoJSON"]?.ToString();
                        if (string.IsNullOrWhiteSpace(geoJson))
                            continue;

                        // Determine geometry
                        var geoJsonElement = JsonDocument.Parse(geoJson).RootElement;
                        var geometry = ExtractGeometryFromGeoJson(geoJson);
                        if (geometry == null)
                            continue;

                        // Determine style logic
                        string layerTypeName = "polylineStyle";
                        if (geoJsonElement.TryGetProperty("geometry", out var geomElem) &&
                            geomElem.TryGetProperty("type", out var typeElem))
                        {
                            string layerType = typeElem.GetString();
                            layerTypeName = layerType switch
                            {
                                "Polyline" => "polylineStyle",
                                "Polygon" => "polygonStyle",
                                "Point" => tableName == "LCMS_Potholes_Processed" ? "circleStyle" : "pointStyle",
                                _ => "polylineStyle"
                            };
                        }

                       

                        // Attributes
                        var attributes = new AttributesTable();
                        attributes.Add("TableName", tableName);

                        foreach (var kvp in record)
                        {
                            if (kvp.Key.Contains("Rounded")) continue;

                            var key = ShortenFieldName(kvp.Key);
                            if (!attributes.Exists(key))
                                attributes.Add(key, kvp.Value?.ToString() ?? string.Empty);
                        }

                        ////Save style metadata in DBF
                        //attributes.Add("Color", colorRow);
                        //attributes.Add("StyleId", styleId);
                        //if (tableName == "LCMS_Segment")
                        //    attributes.Add("ColorBG", "#19c8c8c8");
                        //if (layerTypeName == "Point")
                        //    attributes.Add("Size", "1.20000");

                        // Create feature
                        var feature = new Feature(geometry, attributes);
                        features.Add(feature);
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Error processing record in SaveToShapefile: " + ex.Message);
                    }
                }

                if (features.Count == 0)
                    throw new InvalidOperationException("No features to write to shapefile.");

                var firstFeature = features[0];
                var writer = new ShapefileDataWriter(shapefilePath, geometryFactory)
                {
                    Header = ShapefileDataWriter.GetHeader(firstFeature, features.Count)
                };
                writer.Write(features);

                // Create .prj
                CreatePrjFile(shapefilePath);

                //qml can't be read in google earth
                // Create .qml (QGIS style file based on "Color" field)
                //string qmlPath = Path.ChangeExtension(shapefilePath, ".qml");
                //CreateQmlFile(qmlPath, "Color", firstFeature.Geometry.OgcGeometryType);

                // --- Create project QGIS ---
                var shapefiles = new List<string>
                {
                    Path.ChangeExtension(shapefilePath, ".shp")
                };

                string qgsPath = Path.Combine(saveFilepath, "SurveyProject.qgs");
                CreateQgsProject(qgsPath, shapefiles);

            }
            catch (Exception ex)
            {
                Log.Error("Failed to save Shapefile of " + filename + " " + ex.Message);
            }
        }
       

        /// <summary>
        /// File creation .prj (WGS84).
        /// </summary>
        private void CreatePrjFile(string shapefilePath)
        {
            var prjFilePath = Path.ChangeExtension(shapefilePath, ".prj");
            const string wkt = "GEOGCS[\"WGS 84\",DATUM[\"WGS_1984\",SPHEROID[\"WGS 84\",6378137,298.257223563]],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433]]";
            File.WriteAllText(prjFilePath, wkt, Encoding.ASCII);
        }
        
        /// <summary>
        /// Generates a QGIS project (.qgs) that includes all shapefiles in the folder.
        /// Each layer uses its corresponding .qml style file if it exists.
        /// </summary>    
        public void CreateQgsProject(string projectPath, List<string> shapefilePaths)
        {
            if (string.IsNullOrEmpty(projectPath))
                throw new ArgumentException("Project path cannot be null or empty.", nameof(projectPath));

            if (shapefilePaths == null || shapefilePaths.Count == 0)
                throw new ArgumentException("At least one shapefile must be provided.", nameof(shapefilePaths));

            double? minX = null, minY = null, maxX = null, maxY = null;

            // Calculate the overall extent from all shapefiles
            foreach (string shpPath in shapefilePaths)
            {
                if (File.Exists(shpPath))
                {
                    using (var shpReader = new ShapefileDataReader(shpPath, new GeometryFactory()))
                    {
                        while (shpReader.Read())
                        {
                            var geom = shpReader.Geometry;
                            var envelope = geom.EnvelopeInternal;

                            if (minX == null || envelope.MinX < minX) minX = envelope.MinX;
                            if (minY == null || envelope.MinY < minY) minY = envelope.MinY;
                            if (maxX == null || envelope.MaxX > maxX) maxX = envelope.MaxX;
                            if (maxY == null || envelope.MaxY > maxY) maxY = envelope.MaxY;
                        }
                    }
                }
            }

            // Fallback extent if no data
            minX ??= 0.0;
            minY ??= 0.0;
            maxX ??= 1.0;
            maxY ??= 1.0;

            using (StreamWriter writer = new StreamWriter(projectPath))
            {
                writer.WriteLine("<!DOCTYPE qgis PUBLIC 'http://mrcc.com/qgis.dtd' 'SYSTEM'>");
                writer.WriteLine("<qgis version=\"3.28.0\" styleCategories=\"AllStyleCategories\">");
                writer.WriteLine("  <title>SurveyProject</title>");

                // CRS (WGS84)
                writer.WriteLine("  <projectCrs>");
                writer.WriteLine("    <spatialrefsys>");
                writer.WriteLine("      <wkt>GEOGCS[\"WGS 84\",DATUM[\"WGS_1984\",SPHEROID[\"WGS 84\",6378137,298.257223563]],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433]]</wkt>");
                writer.WriteLine("      <authid>EPSG:4326</authid>");
                writer.WriteLine("      <srsid>4326</srsid>");
                writer.WriteLine("    </spatialrefsys>");
                writer.WriteLine("  </projectCrs>");

                // Extent
                writer.WriteLine("  <extent>");
                writer.WriteLine($"    <xmin>{minX.Value}</xmin>");
                writer.WriteLine($"    <ymin>{minY.Value}</ymin>");
                writer.WriteLine($"    <xmax>{maxX.Value}</xmax>");
                writer.WriteLine($"    <ymax>{maxY.Value}</ymax>");
                writer.WriteLine("  </extent>");

                // MapCanvas
                writer.WriteLine("  <mapcanvas>");
                writer.WriteLine("    <units>degrees</units>");
                writer.WriteLine("    <extent>");
                writer.WriteLine($"      <xmin>{minX.Value}</xmin>");
                writer.WriteLine($"      <ymin>{minY.Value}</ymin>");
                writer.WriteLine($"      <xmax>{maxX.Value}</xmax>");
                writer.WriteLine($"      <ymax>{maxY.Value}</ymax>");
                writer.WriteLine("    </extent>");
                writer.WriteLine("  </mapcanvas>");

                // Layers
                writer.WriteLine("  <projectlayers>");
                foreach (string shpPath in shapefilePaths)
                {
                    if (!File.Exists(shpPath))
                        continue;

                    string layerName = Path.GetFileNameWithoutExtension(shpPath);
                    string qmlPath = Path.ChangeExtension(shpPath, ".qml");
                    string qmlStyle = File.Exists(qmlPath) ? $" style=\"{qmlPath}\"" : "";

                    writer.WriteLine("    <maplayer>");
                    writer.WriteLine($"      <id>{layerName}</id>");
                    writer.WriteLine($"      <datasource>{shpPath}</datasource>");
                    writer.WriteLine("      <provider>ogr</provider>");
                    writer.WriteLine("      <layername>{layerName}</layername>");
                    writer.WriteLine("      <crs>");
                    writer.WriteLine("        <spatialrefsys>");
                    writer.WriteLine("          <wkt>GEOGCS[\"WGS 84\",DATUM[\"WGS_1984\",SPHEROID[\"WGS 84\",6378137,298.257223563]],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433]]</wkt>");
                    writer.WriteLine("          <authid>EPSG:4326</authid>");
                    writer.WriteLine("        </spatialrefsys>");
                    writer.WriteLine("      </crs>");
                    writer.WriteLine($"      <renderer-v2 type=\"singleSymbol\"{qmlStyle}>");
                    writer.WriteLine("        <symbols>");
                    writer.WriteLine("          <symbol>");
                    writer.WriteLine("            <layer/>");
                    writer.WriteLine("          </symbol>");
                    writer.WriteLine("        </symbols>");
                    writer.WriteLine("      </renderer-v2>");
                    writer.WriteLine("    </maplayer>");
                }
                writer.WriteLine("  </projectlayers>");

                writer.WriteLine("</qgis>");
            }
        }
        /// <summary>
        /// Shorten 10 chars, alphanumerc and underscore.
        /// </summary>
        private string ShortenFieldName(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return "FIELD";
            var sb = new StringBuilder(10);
            foreach (var ch in key)
            {
                if (char.IsLetterOrDigit(ch) || ch == '_') sb.Append(ch);
                else sb.Append('_');
                if (sb.Length >= 10) break;
            }
            if (sb.Length == 0) sb.Append("FIELD");
            return sb.ToString();
        }        
        private string BuildStyleId(string layerTypeName, string layerName, string colorCodeInfo = null)
        {
            var cleanName = layerName.Replace(" ", "");
            return $"{layerTypeName}{cleanName}{colorCodeInfo ?? ""}";
        }
        //==========> END Shape Files

        //private string ShortenFieldName(string fieldName)
        //{
        //    //shapefile only allows field name shorter than 11 characters
        //    //try to shorten existing column names
        //    var fieldMapping = new Dictionary<string, string>
        //    {
        //        { "LRP_No_Start", "LRPStart" },
        //        { "Image_File_Index", "ImageFile" },
        //        { "CNR_SpallingArea_mm2", "SpallArea" },
        //        { "Pavement", "Pave" },
        //        { "Chainage", "Chng" },
        //        { "Confidence_Level", "ConfLevel" },
        //        { "Faulting_Avg_Height_mm", "FaultAvgHt" },
        //        { "Faulting_Max_Height_mm", "FaultMaxHt" },
        //        { "Faulting_Min_Height_mm", "FaultMinHt" },
        //        { "Bad_Seal_Length_mm", "BadS_Length" },
        //        { "Bad_Seal_Avg_Depth_mm", "BadS_AvDpth" },
        //        { "Bad_Seal_Max_Depth_mm", "BadS_MxDpth" },
        //        { "Spalling_Length_mm", "SpallLength" },
        //        { "Spalling_Avg_Depth_mm", "SpallAvgDep" },
        //        { "Spalling_Max_Depth_mm", "SpallMaxDep" },
        //        { "Spalling_Avg_Width_mm", "SpallAvgWdt" },
        //        { "Spalling_Max_Width_mm", "SpallMaxWdt" },
        //        { "StatesOfCrossSlope",  "CrossSlpSt"},
        //        { "RadiusOfCurvature", "CurvRadius" },
        //        { "DeductedValues", "DeductValues" },
        //        { "LongitudinalPositionY", "LongPosY" },
        //        { "PickoutAvgPer_m2", "PickoutAvg" },
        //        { "CrackingTotalLengthAllNodes_mm", "CrackTotLen" },
        //        { "CrackingTotalLengthLowSevNodes_mm", "CrackLowLen" },
        //        { "CrackingTotalLengthMedSevNodes_mm", "CrackMedLen" },
        //        { "CrackingTotalLengthHighSevNodes_mm", "CrackHigLen" },
        //        { "CrackClassificationTotalLengthLongCracks_mm", "CrackClLong" },
        //        { "CrackClassificationTotalLengthTransCracks_mm", "CrackClTran" },
        //        { "CrackClassificationTotalAreaFatigueCracks_m2", "CrackClFatg" },
        //        { "CrackClassificationTotalLengthOtherCracks_mm", "CrackClOthr" },
        //        { "RavellingTotalArea_m2", "RavelArea" },
        //        { "RavellingSeverity", "RavelSev" },
        //        { "PotholesCount", "PotholeCnt" },
        //        { "SealedCrackTotalLength_mm", "SealCrkLen" },
        //        { "ShoveTotalLength_mm", "ShoveLength" },
        //        { "GeometryAvgCrossSlope", "GeoAvgCrSlope" },
        //        { "GeometryAvgGradient", "GeoAvgGrad" },
        //        { "GeometryAvgHorizontalCurvature", "GeoHorCurv" },
        //        { "GeometryAvgVerticalCurvature", "GeoVerCurv" },
        //        { "BleedingTotalArea_m2", "BleedArea" },
        //        { "BleedingSeverity", "BleedSev" },
        //        { "SagsTotalArea_m2", "SagArea" },
        //        { "BumpsTotalArea_m2", "BumpArea" },

        //        { "Diameter", "Diam" },
        //        { "Intensity", "Inten" },
        //        { "Average", "Avg" },
        //        { "Latitude", "Lat" },
        //        { "Longitude", "Long" },
        //        { "Altitude", "Alt" },
        //        { "TrackAngle", "Angle" },
        //        { "Severity", "Sev" },
        //        { "Direction", "Dir" },
        //        { "Percent", "Pct" },
        //        { "Smoothness", "Smooth" },
        //        { "_mm", "" },
        //        { "_mm2", "" },
        //        { "_m2", "" },
        //        { "ALG1_", "" },
        //        { "ALG2_", "" }
        //    };

        //    if (fieldName.Length > 11)
        //    {
        //        // If direct mapping exists, use it
        //        if (fieldMapping.ContainsKey(fieldName))
        //        {
        //            fieldName = fieldMapping[fieldName];
        //        }
        //        else
        //        {
        //            // Otherwise, attempt to replace parts of the field name using the fieldMapping
        //            foreach (var map in fieldMapping)
        //            {
        //                fieldName = fieldName.Replace(map.Key, map.Value);
        //            }
        //        }
        //    }
        //    var shortenedfieldName = fieldName.Length <= 11 ? fieldName : fieldName.Substring(0, 11);
        //    return shortenedfieldName;
        //}

        private NetTopologySuite.Geometries.Geometry ExtractGeometryFromGeoJson(string geoJson)
        {
            if (string.IsNullOrEmpty(geoJson))
                throw new ArgumentException("Geo_JSON cannot be null or empty", nameof(geoJson));

            var geoJsonElement = JsonDocument.Parse(geoJson).RootElement;
            var geometry = geoJsonElement.GetProperty("geometry");
            var type = geometry.GetProperty("type").GetString();
            var coordinates = geometry.GetProperty("coordinates");

            switch (type)
            {
                case "Point":
                    return new NetTopologySuite.Geometries.Point(coordinates[0].GetDouble(), coordinates[1].GetDouble());
                case "LineString":
                case "Polyline":
                    var lineStringCoords = new List<Coordinate>();
                    foreach (var coord in coordinates.EnumerateArray())
                    {
                        lineStringCoords.Add(new Coordinate(coord[0].GetDouble(), coord[1].GetDouble()));
                    }
                    return new LineString(lineStringCoords.ToArray());
                case "Polygon":
                    var polygonCoords = new List<Coordinate>();
                    foreach (var coord in coordinates[0].EnumerateArray()) // Assuming outer boundary
                    {
                        polygonCoords.Add(new Coordinate(coord[0].GetDouble(), coord[1].GetDouble()));
                    }
                    // Ensure the first and last points are the same to form a closed ring
                    if (!polygonCoords.First().Equals2D(polygonCoords.Last()))
                    {
                        polygonCoords.Add(polygonCoords.First());
                    }
                    return new NetTopologySuite.Geometries.Polygon(new LinearRing(polygonCoords.ToArray()));
                case "MultiPolygon":
                    var polygons = new List<NetTopologySuite.Geometries.Polygon>();
                    foreach (var polygonData in coordinates.EnumerateArray())
                    {
                        // Single list for all rings, treating each top-level list as a complete polygon ring.
                        var multiPolygonCoords = new List<Coordinate>();
                        foreach (var coord in polygonData.EnumerateArray())
                        {
                            multiPolygonCoords.Add(new Coordinate(coord[0].GetDouble(), coord[1].GetDouble()));
                        }
                        // Ensure the first and last points are the same to form a closed ring.
                        if (!multiPolygonCoords.First().Equals2D(multiPolygonCoords.Last()))
                        {
                            multiPolygonCoords.Add(multiPolygonCoords.First());
                        }
                        // Create a polygon from the coordinates collected
                        var linearRing = new LinearRing(multiPolygonCoords.ToArray());
                        var polygon = new NetTopologySuite.Geometries.Polygon(linearRing);
                        polygons.Add(polygon); // Add each polygon to the MultiPolygon set.
                    }
                    return new NetTopologySuite.Geometries.MultiPolygon(polygons.ToArray());
                default:
                    throw new NotSupportedException($"Geometry type '{type}' is not supported.");
            }
        }

        public static DateTime convertToDateTime(object date)
        {
            return Convert.ToDateTime(date);
        }

        public static string convertToDateTimeString(object date)
        {
            return Convert.ToDateTime(date).ToString();
        }

        public static string convertToUtcDateTime(object date)
        {
            DateTime dateTime = TimeZoneInfo.ConvertTimeToUtc(Convert.ToDateTime(date));
            string strIsoDate = dateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            return strIsoDate;
        }

        public async Task<string> ExportDataFinalGeneric(ExportDataObjRequest request)
        {
            try
            {
                DateTime dateTimeStart = DateTime.Now;
                bool dataExported = false;

                // Directory to store individual KML files before packaging
                string kmlTempDirectory = Path.Combine(Path.GetTempPath(), "KML_Temp");

                // Directory to store shapefiles before zipping
                string shapefileTempDirectory = Path.Combine(Path.GetTempPath(), "Shapefile_Temp");

                //segments to save images if required
                List<LCMS_Segment> segments = new List<LCMS_Segment>();

                //videoframes to get camera images
                List<VideoFrame> videoFrames = new List<VideoFrame>();

                //camera360frames to get 360 camera images
                List<Camera360Frame> camera360Frames = new List<Camera360Frame>();

                //list of surveyId and image file paths
                List<VideoDataHelper> videoDataHelpers = new List<VideoDataHelper>();

                //list of surveys
                List<Survey> surveys = new List<Survey>();

                //survey id list
                List<string>? selectedSurveys = !string.IsNullOrEmpty(request.SelectedSurveys) ? request.SelectedSurveys.Split(',').ToList() : null;

                // LCMS Images
                bool intensityImages = request.ImageLayers.Contains("Intensity Images");
                bool rangeImages = request.ImageLayers.Contains("Range Images");
                bool intensityOverlay = request.ImageLayers.Contains("Intensity Images (Defect Overlay)");
                bool rangeOverlay = request.ImageLayers.Contains("Range Images (Defect Overlay)");

                // Camera Images
                bool panoramicImgs = request.CameraLayers != null && request.CameraLayers.Contains("Panoramic Images"); // For 360 videos
                bool separateSensorImgs = request.CameraLayers != null && request.CameraLayers.Contains("Separate Sensor Images");

                // Image Bands
                Dictionary<string, bool> imageBands = new Dictionary<string, bool>();
                if (request.ImageLayers != null)
                {
                    foreach (var layer in request.ImageLayers)
                    {
                        // Get the overlay flag for this layer, default to false if not present
                        bool overlay = request.ImgBands != null && request.ImgBands.TryGetValue(layer, out var isOverlay) && isOverlay;

                        imageBands[layer] = overlay;
                    }
                }

                // Selected Videos
                List<string> selectedVideoLayers = new List<string>();

                if (request.VideoLayers != null && request.VideoLayers.Count > 0)
                {
                    selectedVideoLayers = request.VideoLayers.ToList();

                }


                using (var connection = new SqliteConnection(connString))
                {
                    connection.Open();

                    var globalLayerStyles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    //getting all LCMS tables
                    DataTable orgTables = new DataTable();
                    using (SqliteCommand cmd = new SqliteCommand("SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%' and name like 'LCMS_%' or name like 'Video%' or name='Survey' or name='Camera360Frame';", connection))
                    {
                        using (SqliteDataReader rdr = cmd.ExecuteReader())
                        {
                            orgTables.Load(rdr);
                        }
                    }

                    List<string> orgTablesList = new List<string>();
                    foreach (DataRow row in orgTables.Rows)
                    {
                        orgTablesList.Add(row[0]?.ToString());
                    }

                    // Retrieve surveys from Survey table
                    if (selectedSurveys != null && selectedSurveys.Count > 0)
                    {
                        surveys = _appDbContext.Survey.Where(s => selectedSurveys.Contains(s.SurveyIdExternal)).ToList();
                    }

                    #region Metadata Export
                    if (request.MetaLayers.Any())
                    {
                        foreach (var metaLayer in request.MetaLayers)
                        {
                            var existingMetaTable = _metadataContext.MetaTable.FirstOrDefault(x => x.TableName == metaLayer);
                            if (existingMetaTable != null && existingMetaTable.Id > 0)
                            {
                                var tableValues = _appDbContext.MetaTableValue.Where(x => x.TableId == existingMetaTable.Id && selectedSurveys.Any(s => s.Equals(x.SurveyId))).ToList();
                                if (tableValues.Any())
                                {
                                    var responses = tableValues.Select(tableValue =>
                                    {
                                        var attributes = new List<KeyValueField>();
                                        if (!existingMetaTable.TableName.StartsWith("IRI") && !existingMetaTable.TableName.Contains("Meter Section"))
                                        {
                                            attributes.Add(new KeyValueField { Key = "Id", Type = "int", Value = tableValue.Id.ToString() });
                                        }
                                        for (int i = 1; i <= 25; i++) // Adjust the loop limit based on the maximum number of columns
                                        {
                                            var headerProperty = typeof(MetaTable).GetProperty($"Column{i}");
                                            var typeProperty = typeof(MetaTable).GetProperty($"Column{i}Type");
                                            var valueProperty = typeof(MetaTableValue).GetProperty($"StrValue{i}");
                                            var decimalValueProperty = typeof(MetaTableValue).GetProperty($"DecValue{i}");

                                            if (headerProperty != null && typeProperty != null)
                                            {
                                                var header = (string)headerProperty.GetValue(existingMetaTable);
                                                var headerType = (string)typeProperty.GetValue(existingMetaTable);

                                                if (header != null)
                                                {
                                                    string value = null;
                                                    if (headerType == "Text" || headerType == "Date" || headerType == "Dropdown")
                                                    {
                                                        if (valueProperty != null)
                                                        {
                                                            value = (string)valueProperty.GetValue(tableValue);
                                                        }
                                                    }
                                                    else if (headerType == "Number" || headerType == "Measurement")
                                                    {
                                                        if (decimalValueProperty != null)
                                                        {
                                                            var decimalValue = (decimal?)decimalValueProperty.GetValue(tableValue);
                                                            value = decimalValue.HasValue ? decimalValue.Value.ToString() : null;
                                                        }
                                                    }

                                                    attributes.Add(new KeyValueField
                                                    {
                                                        Key = header,
                                                        Value = value,
                                                        Type = headerType
                                                    });
                                                }
                                            }
                                        }

                                        return new MetaTableResponse
                                        {
                                            TableId = existingMetaTable.Id,
                                            TableName = existingMetaTable.TableName,
                                            Icon = string.Empty,
                                            IconSize = 0,
                                            GeoJSON = tableValue.GeoJSON,
                                            SurveyId = tableValue.SurveyId,
                                            SegmentId = tableValue.SegmentId,
                                            GPSTrackAngle = tableValue.GPSTrackAngle,
                                            GPSLatitude = tableValue.GPSLatitude,
                                            GPSLongitude = tableValue.GPSLongitude,
                                            Attributes = attributes,
                                            LRPNumber = tableValue.LRPNumber,
                                            Chainage = tableValue.Chainage,
                                            ImageFileIndex = tableValue.ImageFileIndex
                                        };
                                    }).ToList();

                                    //Recalculate IRI treated different - should combine the multiple rows into one
                                    //if (existingMetaTable.TableName.StartsWith("IRI") && existingMetaTable.TableName.Contains("Meter Section"))
                                    //{
                                    //    responses = responses
                                    //       .GroupBy(r => r.SegmentId)
                                    //       .Select(group =>
                                    //       {
                                    //           var first = group.First();
                                    //           var combinedAttributes = new Dictionary<string, KeyValueField>();

                                    //           foreach (var item in group)
                                    //           {
                                    //               foreach (var attr in item.Attributes)
                                    //               {
                                    //                   if (attr.Key == "Id") continue; //don't save Id as it is combined.

                                    //                   if (!string.IsNullOrEmpty(attr.Value) && !combinedAttributes.ContainsKey(attr.Key))
                                    //                   {
                                    //                       combinedAttributes[attr.Key] = attr;
                                    //                   }
                                    //               }
                                    //           }

                                    //           return new MetaTableResponse
                                    //           {
                                    //               TableId = first.TableId,
                                    //               TableName = first.TableName,
                                    //               Icon = first.Icon,
                                    //               IconSize = first.IconSize,
                                    //               GeoJSON = first.GeoJSON,
                                    //               SurveyId = first.SurveyId,
                                    //               SegmentId = first.SegmentId,
                                    //               GPSTrackAngle = first.GPSTrackAngle,
                                    //               GPSLatitude = first.GPSLatitude,
                                    //               GPSLongitude = first.GPSLongitude,
                                    //               Chainage = first.Chainage,
                                    //               ImageFileIndex = first.ImageFileIndex,
                                    //               LRPNumber = first.LRPNumber,
                                    //               Attributes = combinedAttributes.Values.ToList()
                                    //           };
                                    //       })
                                    //       .ToList();
                                    //}

                                    var list = new List<ExpandoObject>();
                                    if (responses != null && responses.Any())
                                    {
                                        foreach (var value in responses)
                                        {
                                            //attributes
                                            var attributes = ConvertKeyValueFieldsToDictionary(value.Attributes);

                                            // Use reflection to dynamically add all properties of MetaTableResponse to attributes
                                            foreach (var prop in typeof(MetaTableResponse).GetProperties())
                                            {
                                                var propValue = prop.GetValue(value);
                                                if (propValue != null)
                                                {
                                                    if (prop.Name != "TableId" && prop.Name != "SegmentId" && prop.Name != "Attributes" && !prop.Name.Contains("Icon"))
                                                    {
                                                        attributes[prop.Name] = propValue.ToString();
                                                    }
                                                }
                                            }

                                            if (value.TableName.StartsWith("IRI") && value.TableName.Contains("Meter Section"))
                                            {
                                                attributes["IRI SectionId"] = value.SegmentId.ToString();
                                            }
                                            else
                                            {
                                                //if (existingMetaTable.TableName != TableNameHelper.LASRutting)
                                                //{
                                                //    attributes["SegmentId"] = value.SegmentId.ToString();
                                               // }
                                            }

                                            list.Add(ToExpando(attributes));
                                        }
                                    }

                                    if (list.Count > 0)
                                    {
                                        //save to csv
                                        if (request.Csv)
                                        {
                                            var fileName = request.DatasetName + "_" + existingMetaTable.TableName + "_" + DateTime.Now.ToFileTime() + ".csv";
                                            await SaveToCsvDynamic(list, fileName, request.SavePathDirectory, request.CoordinatesSystem);
                                        }

                                        // save to kml
                                        if (request.Kmz)
                                        {
                                            var fileName = request.DatasetName + "_" + existingMetaTable.TableName + "_" + DateTime.Now.ToFileTime() + ".kml";
                                            SaveToKmlDynamic(list, fileName, kmlTempDirectory, existingMetaTable.TableName, globalLayerStyles);
                                        }

                                        if (request.Shp)
                                        {
                                            var fileName = request.DatasetName + "_" + existingMetaTable.TableName + "_" + DateTime.Now.ToFileTime();
                                            SaveToShapefile(list, fileName, shapefileTempDirectory, existingMetaTable.TableName);
                                        }

                                        dataExported = true;
                                        list.Clear();
                                        GC.Collect();
                                    }
                                }
                            }
                        }
                    }

                    #endregion
                    #region Summary Export
                    if (request.SummaryLayers.Any())
                    {
                        foreach (var summaryLayer in request.SummaryLayers)
                        {
                            var matchingSummaryList = _appDbContext.Summary.Where(x => x.Name == summaryLayer).ToList();
                            var list = new List<ExpandoObject>();

                            foreach (var summary in matchingSummaryList)
                            {
                                var summaryDefects = _appDbContext.SummaryDefect.Where(x => x.SummaryId == summary.Id);
                                if (summaryDefects != null && summaryDefects.Count() > 0)
                                {
                                    var attributes = new Dictionary<string, object>();
                                    // Add Summary properties
                                    foreach (var prop in typeof(Summary).GetProperties())
                                    {
                                        if (prop.Name is "SummaryDefects" or "SampleUnitSet" or "SampleUnit")
                                            continue;

                                        if (prop.Name == "SampleUnitId")
                                        {
                                            var targetSampleUnit = _appDbContext.SampleUnit.FirstOrDefault(x => x.Id == summary.SampleUnitId);
                                            if (targetSampleUnit != null && targetSampleUnit.Coordinates != null)
                                            {
                                                var rawCoords = JArray.Parse(targetSampleUnit.Coordinates);

                                                // Wrap inside another array for GeoJSON Polygon
                                                var polygonCoords = new JArray { rawCoords };

                                                var geoJson = GeneralHelper.CreateNewGeoJson(GeneralHelper.GeoType.Polygon, polygonCoords, null, null, summary.Name);
                                                if (geoJson != null)
                                                {
                                                    attributes["GeoJSON"] = geoJson;
                                                }
                                            }
                                        }
                                        else if (prop.Name == "SampleUnitSetId")
                                        {
                                            var targetSet = _appDbContext.SampleUnit_Set.FirstOrDefault(x => x.Id == summary.SampleUnitSetId);
                                            if (targetSet != null && targetSet.Name != null)
                                            {
                                                attributes["SampleUnitSet"] = targetSet.Name;
                                                attributes["SummaryType"] = targetSet.Type.ToString();
                                            }
                                        }
                                        else
                                        {
                                            var value = prop.GetValue(summary);
                                            if (value != null)
                                            {
                                                attributes[prop.Name] = value;
                                            }
                                        }
                                    }

                                    // Add SummaryDefects
                                    foreach (var defect in summaryDefects)
                                    {
                                        var key = $"{defect.TableName} {defect.Operation} ({defect.NumericField})";
                                        if (!attributes.ContainsKey(key))
                                        {
                                            attributes[key] = defect.Value;
                                        }
                                    }

                                    if (attributes.Count() > 0)
                                    {
                                        list.Add(ToExpando(attributes));
                                    }
                                }
                            }

                            if (list.Count > 0)
                            {
                                if (request.Csv)
                                {
                                    var fileName = request.DatasetName + "_" + summaryLayer + "_" + DateTime.Now.ToFileTime() + ".csv";
                                    await SaveToCsvDynamic(list, fileName, request.SavePathDirectory, request.CoordinatesSystem);
                                }

                                if (request.Kmz)
                                {
                                    var fileName = request.DatasetName + "_" + summaryLayer + "_" + DateTime.Now.ToFileTime() + ".kml";
                                    SaveToKmlDynamic(list, fileName, kmlTempDirectory, summaryLayer, globalLayerStyles);
                                }

                                if (request.Shp)
                                {
                                    var fileName = request.DatasetName + "_" + summaryLayer + "_" + DateTime.Now.ToFileTime();
                                    SaveToShapefile(list, fileName, shapefileTempDirectory, summaryLayer);
                                }

                                dataExported = true;
                                list.Clear();
                                GC.Collect();
                            }
                        }
                    }
                    #endregion

                    if (request.LCMSLayers.Count > 0 && orgTablesList.Count > 0)
                    {
                        foreach (string tablename in orgTablesList)
                        {
                            bool isLCMSLayer = tablename.StartsWith("LCMS", StringComparison.OrdinalIgnoreCase);
                            bool shouldExport = !isLCMSLayer || request.LCMSLayers.Contains(tablename);

                            if (!shouldExport)
                                continue;

                            string whereClause = string.Empty, query = string.Empty;

                            if (selectedSurveys != null && selectedSurveys.Count > 0)
                                whereClause = @" WHERE SurveyId in (" + string.Join(',', selectedSurveys.Select(id => $"'{id}'")) + ");";

                            if (tablename == "VideoFrame" || tablename == "Camera360Frame")
                            {
                                var surveyIds = surveys.Select(s => s.Id).ToList();
                                if (surveyIds.Any())
                                {
                                    whereClause = " WHERE SurveyId IN (" + string.Join(',', surveyIds) + ");";
                                }
                            }
                            query = "SELECT * from " + tablename + whereClause;

                            if (tablename == "Survey")
                                query = query.Replace("SurveyId", "SurveyIdExternal");

                            string fileName = tablename;

                            if (fileName == "LCMS_Segment_Grid")
                                fileName = "LCMS Crack Classification";

                            fileName = fileName.Replace("_", " ");


                            var list = new List<ExpandoObject>();
                            using (var cmd = new SqliteCommand(query, connection))
                            using (var rdr = cmd.ExecuteReader())
                            {
                                if (rdr.HasRows)
                                {
                                    while (rdr.Read())
                                    {
                                        var expandoObject = new ExpandoObject();
                                        for (var i = 1; i < rdr.FieldCount; i++)
                                        {
                                            if (rdr[i] != null)
                                            {
                                                if (rdr.GetName(i) == "SurveyDate" && tablename == "Survey")
                                                {
                                                    string surveyDateString = DateTime.Parse((string)rdr[i]).ToString("yyyy-MM-ddTHH:mm:ss");
                                                    ((IDictionary<string, object>)expandoObject).Add(rdr.GetName(i), surveyDateString);
                                                }
                                                else
                                                {
                                                    //handled survey data for expando object
                                                    if (tablename == "Survey")
                                                    {
                                                        //check for int values
                                                        string[] surveyParams = { "EndChainage", "EndFis", "LRP", "LRPchainage", "StartChainage", "StartFis" };
                                                        if (surveyParams.Contains(rdr.GetName(i)))
                                                            ((IDictionary<string, object>)expandoObject).Add(rdr.GetName(i), string.IsNullOrEmpty(rdr[i].ToString()) ? 0 : rdr[i]);
                                                        else
                                                            ((IDictionary<string, object>)expandoObject).Add(rdr.GetName(i), rdr[i]);
                                                    }
                                                    else
                                                    {
                                                        ((IDictionary<string, object>)expandoObject).Add(rdr.GetName(i), rdr[i]);
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                object value = rdr.IsDBNull(i) ? null : rdr[i];  // Ensure null for missing values
                                                ((IDictionary<string, object>)expandoObject).Add(rdr.GetName(i), value);
                                            }
                                        }
                                        list.Add(expandoObject);
                                    }

                                    if (list.Count > 0)
                                    {
                                        //save to csv
                                        if (request.Csv && tablename.StartsWith("LCMS"))
                                        {
                                            await SaveToCsvDynamic(list, request.DatasetName + "_" + fileName + ".csv", request.SavePathDirectory, request.CoordinatesSystem);
                                        }

                                        // save to kml
                                        if (request.Kmz && tablename.StartsWith("LCMS"))
                                        {
                                            SaveToKmlDynamic(list, request.DatasetName + "_" + fileName + ".kml", kmlTempDirectory, tablename, globalLayerStyles);
                                        }

                                        if (request.Shp && tablename.StartsWith("LCMS"))
                                        {
                                            SaveToShapefile(list, request.DatasetName + "_" + fileName, shapefileTempDirectory, tablename);
                                        }

                                        if ((request.ImageLayers.Any()) && tablename == "LCMS_Segment")
                                        {
                                            foreach (var expando in list)
                                            {
                                                //get image paths from database
                                                string json = JsonSerializer.Serialize(expando);

                                                // Deserialize to a strongly-typed object

                                                json = json.Replace(":{}", ":null");
                                                LCMS_Segment segment = JsonSerializer.Deserialize<LCMS_Segment>(json);

                                                segments.Add(segment);
                                            }

                                        }
                                        else
                                        {
                                            Console.WriteLine("Condition not met for LCMS_Segment processing.");
                                        }


                                        if ((request.PaveImg || request.RowImg) && tablename == "VideoFrame")
                                        {
                                            foreach (var expando in list)
                                            {
                                                string json = JsonSerializer.Serialize(expando);
                                                VideoFrame videoFrame = JsonSerializer.Deserialize<VideoFrame>(json);
                                                videoFrames.Add(videoFrame);
                                            }
                                        }

                                        if (request.CameraLayers.Any() && tablename == "Camera360Frame")
                                        {
                                            foreach (var expando in list)
                                            {
                                                string json = JsonSerializer.Serialize(expando);
                                                Camera360Frame camera360Frame = JsonSerializer.Deserialize<Camera360Frame>(json);
                                                camera360Frames.Add(camera360Frame);
                                            }
                                        }
                                        //if ((request.Overlayimg || request.Rangeimg || request.Videoframes) && tablename == "Survey" && list.Count > 0)
                                        //{
                                        //    foreach (var expando in list)
                                        //    {
                                        //        string json = JsonSerializer.Serialize(expando);
                                        //        Survey survey = JsonSerializer.Deserialize<Survey>(json.Replace("{}", "\"\""));
                                        //        surveys.Add(survey);
                                        //    }
                                        //}
                                        //else
                                        //{
                                        //    Console.WriteLine("Condition not met for Survey processing.");
                                        //}

                                        dataExported = true;
                                        list.Clear();
                                        GC.Collect();
                                    }
                                }
                            }
                        }
                    }
                    if (request.INSGeometryLayer.Count > 0)
                    {
                        string tablename = "Geometry_Processed";
                        string whereClause = string.Empty, query = string.Empty;

                        if (selectedSurveys != null && selectedSurveys.Count > 0)
                        {
                            var surveyFilter = string.Join(',', selectedSurveys.Select(id => $"'{id}'"));

                            query = @"SELECT " + tablename + @".* 
                              FROM " + tablename + @" 
                              INNER JOIN Survey 
                                  ON Survey.Id = " + tablename + @".SurveyId 
                              WHERE Survey.SurveyIdExternal IN (" + surveyFilter + @");";
                        }

                        string fileName = tablename;

                        var list = new List<ExpandoObject>();
                        using (var cmd = new SqliteCommand(query, connection))
                        using (var rdr = cmd.ExecuteReader())
                        {
                            if (rdr.HasRows)
                            {
                                while (rdr.Read())
                                {
                                    var expandoObject = new ExpandoObject();
                                    for (var i = 1; i < rdr.FieldCount; i++)
                                    {
                                        if (rdr[i] != null)
                                        {
                                            ((IDictionary<string, object>)expandoObject).Add(rdr.GetName(i), rdr[i]);
                                        }
                                        else
                                        {
                                            object value = rdr.IsDBNull(i) ? null : rdr[i];  // Ensure null for missing values
                                            ((IDictionary<string, object>)expandoObject).Add(rdr.GetName(i), value);
                                        }
                                    }
                                    list.Add(expandoObject);
                                }

                                if (list.Count > 0)
                                {
                                    //save to csv
                                    if (request.Csv)
                                    {
                                        await SaveToCsvDynamic(list, request.DatasetName + "_" + fileName + ".csv", request.SavePathDirectory, request.CoordinatesSystem);
                                    }

                                    // save to kml
                                    if (request.Kmz)
                                    {
                                        SaveToKmlDynamic(list, request.DatasetName + "_" + fileName + ".kml", kmlTempDirectory, tablename, globalLayerStyles);
                                    }

                                    if (request.Shp)
                                    {
                                        SaveToShapefile(list, request.DatasetName + "_" + fileName, shapefileTempDirectory, tablename);
                                    }

                                    dataExported = true;
                                    list.Clear();
                                    GC.Collect();
                                }
                            }
                        }

                    }

                    if (request.KeycodeLayers.Count > 0)
                    {
                        string tablename = "Keycode";
                        string whereClause = string.Empty, query = string.Empty;
                        if (selectedSurveys != null && selectedSurveys.Count > 0)
                        {
                            var surveyFilter = string.Join(',', selectedSurveys.Select(id => $"'{id}'"));
                            var layerFilter = string.Join(',', request.KeycodeLayers.Select(l => $"'{l}'"));

                            query = @"SELECT " + tablename + @".* 
                               FROM " + tablename + @" 
                               INNER JOIN Survey 
                                   ON Survey.Id  = " + tablename + @".SurveyId 
                               WHERE Survey.SurveyIdExternal IN (" + surveyFilter + @")
                               AND Keycode.Description IN (" + layerFilter + @")";
                        }
                        string fileName = tablename;

                        var list = new List<ExpandoObject>();
                        using (var cmd = new SqliteCommand(query, connection))
                        using (var rdr = cmd.ExecuteReader())
                        {
                            if (rdr.HasRows)
                            {
                                while (rdr.Read())
                                {
                                    var expandoObject = new ExpandoObject();
                                    for (var i = 0; i < rdr.FieldCount; i++) // Start from 0 to include all fields
                                    {
                                        // Check for DBNull and set to null, if necessary
                                        object value = rdr.IsDBNull(i) ? null : rdr[i];

                                        // Add the value to the ExpandoObject
                                        ((IDictionary<string, object>)expandoObject).Add(rdr.GetName(i), value);
                                    }
                                    list.Add(expandoObject);
                                }

                                if (list.Count > 0)
                                {
                                    //save to csv
                                    if (request.Csv)
                                    {
                                        await SaveToCsvDynamic(list, request.DatasetName + "_" + fileName + ".csv", request.SavePathDirectory, request.CoordinatesSystem);
                                    }

                                    // TO DO: Add geojson data to keycode and make kmz + shp exportable.

                                    //// save to kml
                                    //if (request.Kmz)
                                    //{
                                    //    SaveToKmlDynamic(list, request.DatasetName + "_" + fileName + ".kml", kmlTempDirectory, tablename, globalLayerStyles);
                                    //}

                                    //if (request.Shp)
                                    //{
                                    //    SaveToShapefile(list, request.DatasetName + "_" + fileName, shapefileTempDirectory, tablename);
                                    //}

                                    dataExported = true;
                                    list.Clear();
                                    GC.Collect();
                                }
                            }
                        }
                    }
                
                    if( request.LasLayers.Count > 0)
                    {
                        string tablename = "LAS_Rutting";
                        string whereClause = string.Empty, query = string.Empty;
                        if (selectedSurveys != null && selectedSurveys.Count > 0)
                        {
                            var surveyFilter = string.Join(',', selectedSurveys.Select(id => $"'{id}'"));

                            query = @"SELECT " + tablename + @".* 
                               FROM " + tablename + @" 
                               INNER JOIN Survey 
                                   ON Survey.SurveyIdExternal  = " + tablename + @".SurveyId 
                               WHERE Survey.SurveyIdExternal IN (" + surveyFilter + @");";
                        }

                        string fileName = tablename;

                        var list = new List<ExpandoObject>();
                        using (var cmd = new SqliteCommand(query, connection))
                        using (var rdr = cmd.ExecuteReader())
                        {
                            if (rdr.HasRows)
                            {
                                while (rdr.Read())
                                {
                                    var expandoObject = new ExpandoObject();
                                    for (var i = 0; i < rdr.FieldCount; i++) // Start from 0 to include all fields
                                    {
                                        // Check for DBNull and set to null, if necessary
                                        object value = rdr.IsDBNull(i) ? null : rdr[i];

                                        // Add the value to the ExpandoObject
                                        ((IDictionary<string, object>)expandoObject).Add(rdr.GetName(i), value);
                                    }
                                    list.Add(expandoObject);
                                }

                                if (list.Count > 0)
                                {
                                    //save to csv
                                    if (request.Csv)
                                    {
                                        await SaveToCsvDynamic(list, request.DatasetName + "_" + fileName + ".csv", request.SavePathDirectory, request.CoordinatesSystem);
                                    }

                                    // save to kml
                                    if (request.Kmz)
                                    {
                                        SaveToKmlDynamic(list, request.DatasetName + "_" + fileName + ".kml", kmlTempDirectory, tablename, globalLayerStyles);
                                    }

                                    if (request.Shp)
                                    {
                                        SaveToShapefile(list, request.DatasetName + "_" + fileName, shapefileTempDirectory, tablename);
                                    }

                                    dataExported = true;
                                    list.Clear();
                                    GC.Collect();
                                }
                            }
                        }

                    }
                }

                if (!dataExported && !request.PCI)
                {
                    return "No data found to export.";
                }

                if (!Directory.Exists(request.SavePathDirectory))
                {
                    Directory.CreateDirectory(request.SavePathDirectory);
                }

                if (request.Kmz && Directory.Exists(kmlTempDirectory))
                {
                    if (!Directory.Exists(request.SavePathDirectory))
                        Directory.CreateDirectory(request.SavePathDirectory);

                    string kmzFilePath = Path.Combine(request.SavePathDirectory, request.DatasetName + "_" + "Export_KMZfiles" + DateTime.Now.ToFileTime() + ".kmz");
                    // Collect all KML file names
                    var kmlFiles = Directory.GetFiles(kmlTempDirectory, "*.kml").Select(Path.GetFileName).ToList();

                    // Create the main KML file that references other KML files
                    CreateMainKml(kmlTempDirectory, kmlFiles);

                    using (var zip = ZipFile.Open(kmzFilePath, ZipArchiveMode.Create))
                    {
                        foreach (var filePath in Directory.GetFiles(kmlTempDirectory, "*.kml"))
                        {
                            zip.CreateEntryFromFile(filePath, Path.GetFileName(filePath));
                        }
                    }

                    // Clean up the temporary KML files
                    Directory.Delete(kmlTempDirectory, true);
                }

                if (request.Shp && Directory.Exists(shapefileTempDirectory))
                {
                    if (!Directory.Exists(request.SavePathDirectory))
                        Directory.CreateDirectory(request.SavePathDirectory);

                        string shapefileZipPath = Path.Combine(request.SavePathDirectory, request.DatasetName + "_" + "Export_Shapefiles_" + DateTime.Now.ToFileTime() + ".zip");

                        // Collect all shapefile component files
                        using (var zip = ZipFile.Open(shapefileZipPath, ZipArchiveMode.Create))
                        {
                            foreach (var filePath in Directory.GetFiles(shapefileTempDirectory))
                            {
                                zip.CreateEntryFromFile(filePath, Path.GetFileName(filePath));
                            }
                        }

                        // Clean up the temporary shapefile components
                        Directory.Delete(shapefileTempDirectory, true);
                }

                //calculate framerate
                double framerate = 1;
                if (request.Createvideo)
                {
                    if (request.Timeunit == "ms")
                    {
                        framerate = (double)1000 / request.Delayperframe;
                    }
                    else
                    {
                        framerate = (double)1 / request.Delayperframe;
                    }
                }

                // LCMS images / video export
                if (request.ImageLayers.Any())
                {
                    foreach (LCMS_Segment segment in segments)
                    {
                        Survey survey = surveys.FirstOrDefault(s => s.SurveyIdExternal == segment.SurveyId);

                        List<string> selectedImageLayers = request.ImageLayers.ToList();

                        string exportImagePath = Path.Combine(request.SavePathDirectory, $"LCMS Images/{survey?.SurveyName}"); // Save image path to create videos with overlay band

                        _imageBandInfoService.ExportImages(segment, survey?.ImageFolderPath, exportImagePath, selectedImageLayers, imageBands);


                        if (int.TryParse(survey.SurveyIdExternal, out int extId) && !videoDataHelpers.Any(v => v.SurveyId == extId))
                        {
                            videoDataHelpers.Add(new VideoDataHelper
                            {
                                SurveyId = extId,
                                ImageFilePath = exportImagePath,
                                PartPath = survey.ImageFolderPath,
                                SurveyName = survey.SurveyName
                            });
                        }
                    }

                    if (request.Createvideo)
                    {
                        string videoType = "LCMS";
                        foreach (var videoData in videoDataHelpers)
                        {
                            _imageBandInfoService.CreateVideo(videoData.SurveyId, videoData.PartPath, videoData.ImageFilePath, Path.Combine(request.SavePathDirectory, $"LCMS Video/{videoData.SurveyName}"), request.Videoext, framerate, selectedVideoLayers, videoType);
                        }
                        videoDataHelpers.Clear();
                    }
                }

                // Pave camera images export
                if (request.PaveImg || request.RowImg)
                {
                    int  oldSurveyId = 0;
                    string oldCameraName = "";

                    // Filters video frames based on what was selected.
                    var filteredFrames = videoFrames.Where(f => (f.CameraName.ToLower().Contains("pave") && request.PaveImg) || (f.CameraName.ToLower().Contains("row") && request.RowImg)).ToList();

                    foreach (var frame in filteredFrames)
                    {
                        Survey survey = surveys.FirstOrDefault(s => s.Id == frame.SurveyId);

                        // Only images that match the same survey + camera type
                        var selectedImages = filteredFrames.Where(v => v.SurveyId == frame.SurveyId && v.CameraName == frame.CameraName).Select(v => v.ImageFileName).ToList();

                        var savePath = Path.Combine(request.SavePathDirectory, $"Camera Images/{survey?.SurveyName}/{frame.CameraName} ({frame.CameraSerial})");

                        //call only once for each new survey as all images are copied on first call
                        if (oldSurveyId != frame.SurveyId || oldCameraName != frame.CameraName)
                        {
                            _imageBandInfoService.ExportCameraImages(selectedImages, survey?.VideoFolderPath, savePath);
                            oldSurveyId = frame.SurveyId;
                            oldCameraName = frame.CameraName;
                        }

                        if (!videoDataHelpers.Any(v => v.SurveyId == frame.SurveyId))
                            videoDataHelpers.Add(new VideoDataHelper { SurveyId = frame.SurveyId, ImageFilePath = frame.ImageFileName, PartPath = survey?.VideoFolderPath, SurveyName = survey?.SurveyName });
                    }

                    if (request.Createvideo)
                    {
                        foreach (var videoData in videoDataHelpers)
                        {
                            string videoType = "Camera";
                            _imageBandInfoService.CreateVideo(videoData.SurveyId, videoData.PartPath, videoData.ImageFilePath, Path.Combine(request.SavePathDirectory, $"Camera Video/{videoData.SurveyName}"), request.Videoext, framerate, selectedVideoLayers, videoType);
                        }
                        videoDataHelpers.Clear();
                    }
                }


                // 360 camera Images / Video Export
                if (request.CameraLayers.Any())
                {
                    int oldSurveyId = 0;
                    var sensorSurveysProcessed = new HashSet<int>();

                    foreach (var frame in camera360Frames)
                    {
                        Survey survey = surveys.FirstOrDefault(s => s.Id == frame.SurveyId);
                        // Base export path
                        string basePath = Path.Combine(request.SavePathDirectory, $"Camera Images/{survey.SurveyName}/360 Camera");

                        // Exports Panoramic Images
                        if (panoramicImgs)
                        {
                            string savePath = Path.Combine(basePath, "Panoramic Images");
                            var selectedImages = camera360Frames.Select(v => v.ImagePath).ToList();

                            //call only once for each new survey as all images are copied on first call
                            if (oldSurveyId != frame.SurveyId)
                            {
                                _imageBandInfoService.ExportCameraImages(selectedImages, survey?.VideoFolderPath, Path.Combine(request.SavePathDirectory, savePath));
                                oldSurveyId = frame.SurveyId;
                            }

                            if (!videoDataHelpers.Any(v => v.SurveyId == frame.SurveyId))
                                videoDataHelpers.Add(new VideoDataHelper { SurveyId = frame.SurveyId, ImageFilePath = frame.ImagePath, PartPath = survey?.VideoFolderPath, SurveyName = survey?.SurveyName });
                        }
                        // Exports Separate sensor images.
                        if (separateSensorImgs && !sensorSurveysProcessed.Contains(survey.Id))
                        {
                            ExportSensorImages(frame.ImagePath, request.SavePathDirectory, survey.SurveyName);
                            sensorSurveysProcessed.Add(survey.Id);
                        }
                    }

                    if (request.Createvideo)
                    {
                        foreach (var videoData in videoDataHelpers)
                        {
                            string videoType = "360";
                            _imageBandInfoService.CreateVideo(videoData.SurveyId, videoData.PartPath, videoData.ImageFilePath, Path.Combine(request.SavePathDirectory, $"Camera Video/{videoData.SurveyName}"), request.Videoext, framerate, selectedVideoLayers, videoType);
                        }
                        videoDataHelpers.Clear();
                    }
                }
                
                if (!Directory.GetFiles(request.SavePathDirectory).Any() && !Directory.GetDirectories(request.SavePathDirectory).Any() && !request.PCI)
                {
                    Directory.Delete(request.SavePathDirectory);
                    return "No data found to export.";
                }

                    segments.Clear();

                return $"Data exported successfully";
            }
            catch (Exception ex)
            {
                if (ex.Message.ToLower().Contains("OutOfMemoryException".ToLower()))
                    return $"Export Failed due to unavailability of resources, please try again after restarting the application.";
                else
                    return $"Export Failed : {ex.Message}";
            }
        }

        public void ExportSensorImages(string imagePath, string savePathDirectory, string surveyName)
        {
            string sensorPath = Path.Combine(Path.GetDirectoryName(imagePath), "IndividualCameras");

            if (Directory.Exists(sensorPath))
            {
                var sensorImages = Directory.GetFiles(sensorPath, "*.jpg", SearchOption.TopDirectoryOnly).ToList();

                if (sensorImages.Count > 0)
                {
                    string exportPath = Path.Combine(savePathDirectory,
                        $"Camera Images/{surveyName}/360 Camera/Separate Sensor Images");

                    _imageBandInfoService.ExportCameraImages(sensorImages, null, exportPath);
                }
            }
        }
        private IEnumerable<GroupResult> GroupAndSort(List<ExpandoObject> list, string[] groupByProperties, string[] sortProperties, bool ascending)
        {
            // Perform Grouping
            var grouped = list
                .GroupBy(expando => string.Join("|", groupByProperties.Select(prop => ((IDictionary<string, object>)expando).ContainsKey(prop) ? ((IDictionary<string, object>)expando)[prop]?.ToString() : "NULL")))
                .Select(g => new GroupResult
                {
                    Key = g.Key,
                    Items = OrderByMultiple(g, sortProperties, ascending).ToList()
                });

            return grouped;
        }

        public class GroupResult
        {
            public string Key { get; set; }
            public List<ExpandoObject> Items { get; set; }
        }

        public IEnumerable<ExpandoObject> OrderByMultiple(IEnumerable<ExpandoObject> source, string[] properties, bool ascending = true)
        {
            IOrderedEnumerable<ExpandoObject> orderedQuery = null;
            foreach (var prop in properties)
            {
                orderedQuery = orderedQuery == null
                    ? (ascending
                        ? source.OrderBy(e => ((IDictionary<string, object>)e)[prop])
                        : source.OrderByDescending(e => ((IDictionary<string, object>)e)[prop]))
                    : (ascending
                        ? orderedQuery.ThenBy(e => ((IDictionary<string, object>)e)[prop])
                        : orderedQuery.ThenByDescending(e => ((IDictionary<string, object>)e)[prop]));
            }

            return orderedQuery ?? source;
        }

        private ExpandoObject ToExpando(Dictionary<string, object> dictionary)
        {
            var expando = new ExpandoObject();
            var expandoDict = (IDictionary<string, object>)expando;

            foreach (var kvp in dictionary)
            {
                expandoDict[kvp.Key] = kvp.Value;
            }

            return expando;
        }

        private Dictionary<string, object> ConvertKeyValueFieldsToDictionary(List<KeyValueField> keyValueFields)
        {
            var dictionary = new Dictionary<string, object>();

            foreach (var field in keyValueFields)
            {
                if (field.Type == ColumnType.Number.ToString() || field.Type == ColumnType.Measurement.ToString())
                {
                    if (double.TryParse(field.Value, out var decimalValue))
                    {
                        dictionary[field.Key] = decimalValue;
                    }
                    else
                    {
                        dictionary[field.Key] = null;
                    }
                }
                else if (field.Type == ColumnType.Text.ToString() || field.Type == ColumnType.Dropdown.ToString() || field.Type == ColumnType.Date.ToString())
                {
                    dictionary[field.Key] = field.Value;
                }
                else if (field.Type == "int" && int.TryParse(field.Value, out var intValue))
                {
                    dictionary[field.Key] = intValue;
                }
            }

            return dictionary;
        }

        public class ExportDatabase
        {
            public string Table { get; set; }
            public string Column { get; set; }
            public string SourceTable { get; set; }
            public string SourceColumn { get; set; }
            public string Grouped { get; set; }
            public string GroupedBy { get; set; }
            public string Operation { get; set; }
            public string DataType { get; set; }
        }

        public class ExportQueries
        {
            public string SourceTable { get; set; }
            public string TableName { get; set; }
            public string CreateTable { get; set; }
        }

        public class VideoDataHelper
        {
            public int SurveyId { get; set; }
            public string SurveyName { get; set; }
            public string ImageFilePath { get; set; }
            public string? PartPath { get; set; }
        }
    }
}
