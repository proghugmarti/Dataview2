using static System.Collections.Specialized.BitVector32;
using System.Drawing;
using DataView2.Core.Models.LCMS_Data_Tables;
using Serilog;
using System.IO;
using OpenCvSharp;
using Size = OpenCvSharp.Size;
using DotSpatial.Data;
using System.Text.RegularExpressions;
using NetTopologySuite.Operation.OverlayNG;
using NPOI.SS.Formula.PTG;
using DataView2.Core.Helper;
using System.Data.SqlClient;
using DataView2.GrpcService.Services.AppDbServices;
using DataView2.GrpcService.Data;
using Microsoft.Data.Sqlite;
using System.Data.SQLite;
using Newtonsoft.Json.Linq;
using PointF = System.Drawing.PointF;
using Esri.ArcGISRuntime.Geometry;
using Microsoft.Extensions.Azure;
using static DataView2.Core.Helper.TableNameHelper;
using DataView2.Core.Models.Other;
using DataView2.Core.Models;
using Color = System.Drawing.Color;
using MathNet.Numerics.Statistics;
using System.Collections.Generic;

namespace DataView2.GrpcService.Services
{
    public class ImageBandInfoService
    {
        private readonly DatabasePathProvider _dbPathProvider;
        private readonly AppDbContextMetadata _appDbContextMetadata;
        private readonly AppDbContextProjectData _appDbContextProjectData;

        private string databasePath;
        public ImageBandInfoService(DatabasePathProvider dbPathProvider, AppDbContextMetadata appDbContextMetadata, AppDbContextProjectData appDbContextProjectData)
        {
            _dbPathProvider = dbPathProvider;
            _appDbContextMetadata = appDbContextMetadata;
            _appDbContextProjectData = appDbContextProjectData;
        }

        public async Task<(int totalFiles, int failedFiles, List<string> errorMessages)> GenerateOverlayImages(IEnumerable<LCMS_Segment> segments, List<string> imageTypes, List<string> overlayDefects)
        {
            var errorDetails = new List<(int Code, string FileName)>();
            try
            {
                //fetch all color code information at first
                var colorCodes = _appDbContextMetadata.ColorCodeInformation.ToList();
                var colorCodingMap = colorCodes?
                    .GroupBy(x => x.TableName)
                    .ToDictionary(g => g.Key, g => g.ToList());
                var mapGraphicByName = _appDbContextMetadata.MapGraphic
                    .ToDictionary(m => m.Name, m => m);

                if (string.IsNullOrEmpty(databasePath))
                    databasePath = _dbPathProvider.GetDatasetDatabasePath();

                // group segments by surveyId (just in case, but mostly it will be same survey)
                var groupedSegments = segments.GroupBy(s => s.SurveyId);

                foreach (var grouped in groupedSegments)
                {
                    var surveyId = grouped.Key;
                    var survey = _appDbContextProjectData.Survey.FirstOrDefault(x => x.SurveyIdExternal == surveyId);
                    if (survey == null)
                    {
                        Log.Error($"No survey found for survey Id {surveyId}. Can not proceed further.");
                        errorDetails.Add((-1, $"SurveyId={surveyId}"));
                        continue;
                    }

                    // run all segments in parallel
                    var tasks = segments.Select(segment => Task.Run(async () =>
                    {
                        try
                        {
                            // each task gets its own SQLite connection
                            using var conn = new SQLiteConnection($"Data Source={databasePath};");
                            await conn.OpenAsync();

                            var reply = await GenerateOverlayBySegment(
                                conn,
                                survey.ImageFolderPath,
                                segment,
                                overlayDefects,
                                imageTypes,
                                colorCodingMap,
                                mapGraphicByName);

                            if (reply.Id < 0)
                            {
                                lock (errorDetails) // thread-safe add
                                {
                                    errorDetails.Add((reply.Id, reply.Message));
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"Error generating overlay for segment {segment.SegmentId}: {ex.Message}");
                            lock (errorDetails)
                            {
                                errorDetails.Add((0, segment.ImageFilePath)); // 0 = unexpected error
                            }
                        }
                    }));

                    await Task.WhenAll(tasks);
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error in GenerateOverlayImagesFromProcessing" + ex.Message);
                errorDetails.Add((0, "General"));
            }

            //errorcode -1 : general , -2 : no intensity image , -3 : no range image, -4 : locked file , -5 : missing survey
            var groupedErrors = errorDetails
                .GroupBy(e => e.Code)
                .Select(g =>
                {
                    var fileCount = g.Count();
                    var fileNames = g.Select(x => x.FileName).ToList();

                    // If fewer than 10 files, show them inline; otherwise just mention the count
                    string filesSummary = fileCount <= 10
                        ? $"({string.Join(", ", fileNames)})"
                        : "See logs for the list of affected files.";

                    return g.Key switch
                    {
                        -2 => $"The required intensity images are missing for {fileCount} files. {filesSummary}",
                        -3 => $"The required range images are missing for {fileCount} files. {filesSummary}",
                        -4 => $"The files are currently in use by another process ({fileCount} files). Please close them before retrying. {filesSummary}",
                        _ => $"An unexpected error occurred ({fileCount} files). {filesSummary}"
                    };
                })
                .ToList();

            return (segments.Count(), errorDetails.Count, groupedErrors);
        }

        public async Task<IdReply> GenerateOverlayBySegment(SQLiteConnection conn, string partPath, LCMS_Segment segment, List<string> overlayDefects, List<string> imageTypes, Dictionary<string, List<ColorCodeInformation>> colorCodingMap, Dictionary<string, MapGraphicData> mapGraphicByName)
        {
            try
            {
                string origianlImagePath = segment.ImageFilePath;
                if (!Path.IsPathFullyQualified(origianlImagePath) && !string.IsNullOrEmpty(partPath))
                {
                    if (!partPath.Contains("ImageResult")) partPath = Path.Combine(partPath, "ImageResult");
                    origianlImagePath = Path.Combine(partPath, segment.ImageFilePath);
                }

                //source paths
                List<string> imagePaths = new List<string>();

                if (imageTypes.Contains("Intensity"))
                {
                    var intensityImage = origianlImagePath.Replace(".jpg", "_Intensity.jpg");
                    if (!Path.Exists(intensityImage))
                    {
                        //intensity image not found
                        Log.Error($"Intensity Image not found : {intensityImage}");
                        return new IdReply { Id = -2, Message = Path.GetFileName(intensityImage) };
                    }
                    imagePaths.Add(intensityImage);
                }

                if (imageTypes.Contains("Range"))
                {
                    var rangeImagePath = origianlImagePath.Replace(".jpg", "_Range.jpg");
                    if (!Path.Exists(rangeImagePath))
                    {
                        Log.Error($"Range Image not found : {rangeImagePath}");
                        return new IdReply { Id = -3, Message = Path.GetFileName(rangeImagePath) };
                    }
                    imagePaths.Add(rangeImagePath);
                }

                if (imagePaths.Count == 0) return new IdReply { Id = -1, Message = "No image type selected"};

                // Common segment variables
                var surveyId = segment.SurveyId;
                var segmentId = segment.SegmentId;

                var segmentLat = segment.GPSLatitude;
                var segmentLon = segment.GPSLongitude;
                double trackAngle = segment.GPSTrackAngle;
                var (minX, maxX, minY, maxY) = GetSegmentFrame(segment);

                var overlayDrawData = new Dictionary<
                    (Color color, int thickness, string symbolType), //key
                    (List<(double x, double y)> points, List<List<(double x, double y)>> polylines, List<List<(double x, double y)>> polygons)>(); //value

                foreach (var overlay in overlayDefects)
                {
                    var overlayDBName = TableNameHelper.GetDBTableName(overlay);
                    if (overlayDBName == null) continue;

                    // Get color coding rules
                    var tableColorCoding = colorCodingMap.TryGetValue(overlay, out var cc) ? cc : null;
                    bool hasColorCoding = tableColorCoding?.Any() == true;

                    int red = 255, green = 255, blue = 255, alpha = 255, thickness = 255; //default white
                    string symbolType = null;

                    //find mapGraphic
                    if (mapGraphicByName.TryGetValue(overlay, out var mg))
                    {
                        red = mg.Red;
                        green = mg.Green;
                        blue = mg.Blue;
                        alpha = mg.Alpha;
                        thickness = (int)mg.Thickness;
                        symbolType = mg.SymbolType;
                    }
                    if (overlay == LayerNames.MacroTexture) symbolType = "FillLine"; //show band only with lines

                    string query = null;
                    string property = null;

                    if (hasColorCoding)
                    {
                        property = tableColorCoding?.FirstOrDefault().Property;

                        if (overlay == LayerNames.Bleeding && property == "Severity")
                        {
                            query = $"SELECT GeoJSON, LeftSeverity, RightSeverity FROM {overlayDBName} WHERE SurveyId = {surveyId} AND SegmentId = {segmentId}";
                        }
                        else
                        {
                            if (!ColumnExists(conn, overlayDBName, property)) // if no property found, don't use color coding
                            {
                                query = $"SELECT GeoJSON FROM {overlayDBName} WHERE SurveyId = {surveyId} AND SegmentId = {segmentId}";
                                hasColorCoding = false;
                            }
                            else
                            {
                                query = $"SELECT GeoJSON, {property} FROM {overlayDBName} WHERE SurveyId = {surveyId} AND SegmentId = {segmentId}";
                            }
                        }
                    }
                    else
                    {
                        query = $"SELECT GeoJSON FROM {overlayDBName} WHERE SurveyId = {surveyId} AND SegmentId = {segmentId}";
                    }

                    using var command = conn.CreateCommand();
                    command.CommandText = query;
                    using var reader = await command.ExecuteReaderAsync();

                    while (await reader.ReadAsync())
                    {
                        string geoJson = reader["GeoJSON"].ToString();
                        var jObject = JObject.Parse(geoJson);
                        var geometry = jObject["geometry"];
                        string geoType = geometry["type"].ToString();
                        var coords = geometry?["coordinates"];
                        List<(double X, double Y)> points = new List<(double X, double Y)>();
                        List<List<(double X, double Y)>> polylines = new List<List<(double X, double Y)>>();
                        List<List<(double X, double Y)>> polygons = new List<List<(double X, double Y)>>();
                        switch (geoType)
                        {
                            case "Point":
                                points.Add((coords[0].Value<double>(), coords[1].Value<double>()));
                                break;
                            case "Polyline":
                                var line = new List<(double, double)>();
                                foreach (var coord in coords)
                                    line.Add((coord[0].Value<double>(), coord[1].Value<double>()));
                                polylines.Add(line);
                                break;
                            case "Polygon":
                            case "MultiPolygon":
                                foreach (var ring in coords)
                                {
                                    var polygon = new List<(double, double)>();
                                    foreach (var coord in ring)
                                        polygon.Add((coord[0].Value<double>(), coord[1].Value<double>()));
                                    polygons.Add(polygon);
                                }
                                break;
                        }

                        if (hasColorCoding)
                        {
                            if (overlay == LayerNames.Bleeding && property == "Severity")
                            {
                                string leftSeverity = reader["LeftSeverity"]?.ToString();
                                string rightSeverity = reader["RightSeverity"]?.ToString();

                                if (polygons.Count == 2)
                                {
                                    if (leftSeverity != "No Bleeding")
                                    {
                                        var (leftColor, leftThickness) = ResolveColor(tableColorCoding, "Severity", leftSeverity);
                                        var key = (leftColor, leftThickness, symbolType);
                                        if (!overlayDrawData.TryGetValue(key, out var bucket))
                                        {
                                            bucket = (new List<(double, double)>(), new List<List<(double, double)>>(), new List<List<(double, double)>>());
                                            overlayDrawData[key] = bucket;
                                        }
                                        bucket.polygons.Add(new List<(double, double)> (polygons[0]));
                                    }

                                    if (rightSeverity != "No Bleeding")
                                    {
                                        var (rightColor, rightThickness) = ResolveColor(tableColorCoding, "Severity", rightSeverity);
                                        var key = (rightColor, rightThickness, symbolType);
                                        if (!overlayDrawData.TryGetValue(key, out var bucket))
                                        {
                                            bucket = (new List<(double, double)>(), new List<List<(double, double)>>(), new List<List<(double, double)>>());
                                            overlayDrawData[key] = bucket;
                                        }
                                        bucket.polygons.Add(new List<(double, double)> (polygons[1]));
                                    }
                                }
                            }
                            else
                            {
                                string propertyValue = !string.IsNullOrEmpty(property) ? reader[property]?.ToString() : null;
                                if (propertyValue != null && tableColorCoding != null)
                                {
                                    var (color, colorThickness) = ResolveColor(tableColorCoding, property, propertyValue);
                                    var key = (color, colorThickness, symbolType);
                                    if (!overlayDrawData.TryGetValue(key, out var bucket))
                                    {
                                        bucket = (new List<(double, double)>(), new List<List<(double, double)>>(), new List<List<(double, double)>>());
                                        overlayDrawData[key] = bucket;
                                    }
                                    bucket.points.AddRange(points);
                                    foreach (var line in polylines)
                                        bucket.polylines.Add(new List<(double, double)>(line));
                                    foreach (var poly in polygons)
                                        bucket.polygons.Add(new List<(double, double)>(poly));
                                }
                            }
                        }
                        else
                        {
                            var color = System.Drawing.Color.FromArgb(alpha, red, green, blue);
                            var key = (color, thickness, symbolType);

                            if (!overlayDrawData.TryGetValue(key, out var bucket))
                            {
                                bucket = (new List<(double, double)>(), new List<List<(double, double)>>(), new List<List<(double, double)>>());
                                overlayDrawData[key] = bucket;
                            }
                            bucket.points.AddRange(points);
                            foreach (var line in polylines)
                                bucket.polylines.Add(new List<(double, double)>(line));
                            foreach (var poly in polygons)
                                bucket.polygons.Add(new List <(double, double)> (poly));
                        }
                    }
                }

                foreach (var imagePath in imagePaths)
                {
                    Log.Information($"Creating overlay in {imagePath}");
                    using (System.Drawing.Image original = System.Drawing.Image.FromFile(imagePath))
                    using (var image = new Bitmap(original))
                    {
                        var imageWidth = image.Width;
                        var imageHeight = image.Height;

                        foreach (var data in overlayDrawData)
                        {
                            var convertedPoints = data.Value.points
                               .Select(p => ConvertCoord(p.x, p.y, segmentLat, segmentLon, trackAngle, minX, maxX, minY, maxY, imageWidth, imageHeight))
                               .ToList();

                            var convertedPolylines = data.Value.polylines
                                .Select(line => line.Select(p => ConvertCoord(p.x, p.y, segmentLat, segmentLon, trackAngle, minX, maxX, minY, maxY, imageWidth, imageHeight)).ToList())
                                .ToList();

                            var convertedPolygons = data.Value.polygons
                                .Select(poly => poly.Select(p => ConvertCoord(p.x, p.y, segmentLat, segmentLon, trackAngle, minX, maxX, minY, maxY, imageWidth, imageHeight)).ToList())
                                .ToList();

                            await DrawGeometryInImage(image, data.Key.color, data.Key.thickness, data.Key.symbolType, convertedPoints, convertedPolylines, convertedPolygons);
                        }

                        string overlayPath = null;
                        if (imagePath.EndsWith("_Range.jpg", StringComparison.OrdinalIgnoreCase))
                        {
                            overlayPath = imagePath.Replace("_Range.jpg", "_RngOverlay.jpg");
                        }
                        else if (imagePath.EndsWith("_Intensity.jpg", StringComparison.OrdinalIgnoreCase))
                        {
                            overlayPath = imagePath.Replace("_Intensity.jpg", "_Overlay.jpg");
                        }

                        if (overlayPath != null)
                            image.Save(overlayPath, System.Drawing.Imaging.ImageFormat.Jpeg);
                    }
                }

                return new IdReply { Id = segmentId, Message = $"Overlay images successfully generated for segment {segmentId}." };
            }
            catch (Exception ex)
            {
                Log.Error($"Overlay generation failed: {ex.Message}");
                if (ex.HResult == -2147024864)
                    return new IdReply { Id = -4, Message = Path.GetFileName(segment.ImageFilePath) };
                else
                    return new IdReply { Id = -1, Message = Path.GetFileName(segment.ImageFilePath) };
            }    
        }

        private bool ColumnExists(SQLiteConnection conn, string tableName, string columnName) 
        { 
            using var cmd = conn.CreateCommand(); 
            cmd.CommandText = $"PRAGMA table_info({tableName})"; 
            using var reader = cmd.ExecuteReader(); 
            while (reader.Read()) 
            { 
                if (reader["name"].ToString().Equals(columnName, StringComparison.OrdinalIgnoreCase)) 
                    return true; 
            } 
            return false; 
        }

        private static (double minX, double maxX, double minY, double maxY) GetSegmentFrame (LCMS_Segment segment)
        {
            double minX = 0.0, maxX = 0.0, minY = 0.0, maxY = 0.0;

            var segmentGeojson = JObject.Parse(segment.GeoJSON);
            var segmentCoords = segmentGeojson["geometry"]?["coordinates"];
            if (segmentCoords != null)
            {
                var ring = segmentCoords.First();
                if (ring != null && ring.Count() >= 4)
                {
                    double lonBL = ring[0][0].Value<double>();
                    double latBL = ring[0][1].Value<double>();

                    double lonTL = ring[1][0].Value<double>();
                    double latTL = ring[1][1].Value<double>();

                    double lonTR = ring[2][0].Value<double>();
                    double latTR = ring[2][1].Value<double>();

                    double lonBR = ring[3][0].Value<double>();
                    double latBR = ring[3][1].Value<double>();

                    (double xBL, double yBL) = XYFromGPSCoordinates(latBL, lonBL, latBL, lonBL, segment.GPSTrackAngle);
                    (double xTL, double yTL) = XYFromGPSCoordinates(latBL, lonBL, latTL, lonTL, segment.GPSTrackAngle);
                    (double xTR, double yTR) = XYFromGPSCoordinates(latBL, lonBL, latTR, lonTR, segment.GPSTrackAngle);
                    (double xBR, double yBR) = XYFromGPSCoordinates(latBL, lonBL, latBR, lonBR, segment.GPSTrackAngle);

                    minX = Math.Min(Math.Min(xBL, xTL), Math.Min(xTR, xBR));
                    maxX = Math.Max(Math.Max(xBL, xTL), Math.Max(xTR, xBR));
                    minY = Math.Min(Math.Min(yBL, yTL), Math.Min(yTR, yBR));
                    maxY = Math.Max(Math.Max(yBL, yTL), Math.Max(yTR, yBR));
                }
            }
            return (minX, maxX, minY, maxY);
        }
        private PointF ConvertCoord(double lon, double lat, double segmentLat, double segmentLon, double trackAngle, double minX, double maxX, double minY, double maxY, int imageWidth, int imageHeight)
        {
            (double xRight, double yAlong) = XYFromGPSCoordinates(segmentLat, segmentLon, lat, lon, trackAngle);
            double x = (xRight - minX) / (maxX - minX) * imageWidth;
            double y = imageHeight - ((yAlong - minY) / (maxY - minY) * imageHeight);
            return new PointF((float)x, (float)y);
        }
        private (Color color, int thickness) ResolveColor(List<ColorCodeInformation> colorCodes, string property, string propertyValue)
        {
            var match = colorCodes.FirstOrDefault(cc =>
                        cc.Property == property &&
                        ((cc.IsStringProperty == true && cc.StringProperty == propertyValue) ||
                        (cc.IsStringProperty == false && double.TryParse(propertyValue, out var numVal) &&
                            numVal >= cc.MinRange && numVal <= cc.MaxRange)));
            if (match != null)
            {
                var color = GeneralHelper.ConvertHexToColor(match.HexColor);
                return (color, (int)match.Thickness);
            }
            return (System.Drawing.Color.White, 3); //default
        }
        private async Task DrawGeometryInImage(System.Drawing.Image image, Color color, int thickness, string symbolType, List<PointF> points, List<List<PointF>> polylines, List<List<PointF>> polygons)
        {
            using (Graphics graphics = Graphics.FromImage(image))
            {
                System.Drawing.Brush brush = new SolidBrush(color);
                int size = 15;
                foreach (var pt in points)
                {
                    graphics.FillEllipse(brush, pt.X - size / 2, pt.Y - size / 2, size, size);
                }

                foreach (var line in polylines)
                {
                    if (line.Count >= 2)
                    {
                        var arr = line.ToArray();
                        using (Pen pen = new Pen(brush, thickness))
                        {
                            graphics.DrawLines(pen, arr);
                        }
                    }
                }

                foreach (var polygon in polygons)
                {
                    if (polygon.Count >= 3)
                    {
                        var arr = polygon.ToArray();

                        int outerLine = 5;
                        if (symbolType != "FillLine")
                        {
                            graphics.FillPolygon(brush, arr);
                            outerLine = 1;
                        }

                        using (System.Drawing.Brush outerBrush = new SolidBrush(System.Drawing.Color.FromArgb(255, color)))
                        {
                            var pen = new Pen(outerBrush, outerLine);
                            graphics.DrawPolygon(pen, arr);
                        }
                    }
                }
            }
        }

        public static (double x, double y) XYFromGPSCoordinates(double latitudeRef, double longitudeRef, double latitudeTarget, double longitudeTarget, double trackAngleDegrees)
        {
            // 1) Project both points into Web Mercator (EPSG:3857)
            var refWgs = new MapPoint(longitudeRef, latitudeRef, SpatialReferences.Wgs84);
            var tgtWgs = new MapPoint(longitudeTarget, latitudeTarget, SpatialReferences.Wgs84);

            var refProj = (MapPoint)GeometryEngine.Project(refWgs, SpatialReferences.WebMercator);
            var tgtProj = (MapPoint)GeometryEngine.Project(tgtWgs, SpatialReferences.WebMercator);

            // 2) Compute global meter offsets (east/north)
            double dxEast = tgtProj.X - refProj.X;   // meters east
            double dyNorth = tgtProj.Y - refProj.Y;  // meters north

            // 3) Rotate into local road frame using track angle
            double theta = trackAngleDegrees * Math.PI / 180.0;

            // yAlong = forward along track, xRight = right of track
            double yAlong = dxEast * Math.Sin(theta) + dyNorth * Math.Cos(theta);
            double xRight = dxEast * Math.Cos(theta) - dyNorth * Math.Sin(theta);

            return (xRight, yAlong);
        }


        public void ExportImages(LCMS_Segment segment, string? partPath, string savePath, List<string> selectedImageLayers, Dictionary<string, bool> imageBands)
        {
            try
            { 
                //check paths
                string imagePath = segment.ImageFilePath;
                if (!Path.IsPathFullyQualified(imagePath) && !string.IsNullOrEmpty(partPath)) 
                {
                    if (!partPath.Contains("ImageResult")) partPath = Path.Combine(partPath, "ImageResult");
                    imagePath = Path.Combine(partPath, segment.ImageFilePath); 
                }

                if(!Directory.Exists(savePath))
                    Directory.CreateDirectory(savePath);

                //source paths
                List<string> imagePaths = new List<string>();

                if (selectedImageLayers.Contains("Range Images"))
                    imagePaths.Add(imagePath.Replace(".jpg", "_Range.jpg"));

                if (selectedImageLayers.Contains("Range Images (Defect Overlay)"))
                    imagePaths.Add(imagePath.Replace(".jpg", "_RngOverlay.jpg"));

                if (selectedImageLayers.Contains("Intensity Images"))
                    imagePaths.Add(imagePath.Replace(".jpg", "_Intensity.jpg"));

                if (selectedImageLayers.Contains("Intensity Images (Defect Overlay)"))
                    imagePaths.Add(imagePath.Replace(".jpg", "_Overlay.jpg"));

                // add image band info
                foreach (string imagepath in imagePaths)
                {
                    if (File.Exists(imagepath))
                    {
                        // choose save directory by image type
                        string imgSavePath = "";
                        if (imagepath.Contains("_Intensity"))
                            imgSavePath = Path.Combine(savePath, "Intensity Images");
                        else if (imagepath.Contains("_Range"))
                            imgSavePath = Path.Combine(savePath, "Range Images");
                        else if(imagepath.Contains("_RngOverlay"))
                            imgSavePath = Path.Combine(savePath, "Range Images (Defect Overlay)");
                        else if (imagepath.Contains("_Overlay"))
                            imgSavePath = Path.Combine(savePath, "Intensity Images (Defect Overlay)");

                        if (!Directory.Exists(imgSavePath))
                            Directory.CreateDirectory(imgSavePath);

                        // Gets info if the image's image band was set to true or false.
                        string folderName = Path.GetFileName(imgSavePath);
                        bool addBandInfo = false;

                        // Check if the folder name matches imageband key in the dictionary
                        if (imageBands.TryGetValue(folderName, out bool bandValue))
                        {
                            addBandInfo = bandValue;
                        }

                        byte[] imageData = File.ReadAllBytes(imagepath);

                        // Add Overlay band to each image.
                        if (addBandInfo)
                        {
                            using (MemoryStream memoryStream = new MemoryStream(imageData))
                            using (System.Drawing.Image image = System.Drawing.Image.FromStream(memoryStream))
                            {
                                using (Graphics graphics = Graphics.FromImage(image))
                                {
                                    Pen borderPen = new Pen(System.Drawing.Color.FromArgb(160, 40, 46, 77), 3);
                                    Rectangle borderRectangle = new Rectangle(0, 0, image.Width, 120);
                                    graphics.DrawRectangle(borderPen, borderRectangle);
                                    System.Drawing.Brush fillBrush = new SolidBrush(System.Drawing.Color.FromArgb(200, 40, 46, 77));
                                    graphics.FillRectangle(fillBrush, borderRectangle);

                                    System.Drawing.Font customFont = new System.Drawing.Font("Arial", 20, FontStyle.Regular);
                                    System.Drawing.Brush textBrush = new SolidBrush(System.Drawing.Color.White);

                                    StringFormat centerFormat = new StringFormat
                                    {
                                        Alignment = StringAlignment.Center,   
                                        LineAlignment = StringAlignment.Center 
                                    };

                                    string customText = $"Segment Id: {Convert.ToInt32(segment.SectionId)}     Chainage: {segment.Chainage}\n\n Lat: {Double.Round(segment.GPSLatitude, 6)}     Lon: {Double.Round(segment.GPSLongitude,6)}    Alt: {Double.Round(segment.GPSAltitude, 6)}";
                                    System.Drawing.PointF textPosition = new System.Drawing.PointF(25, 25);

                                    graphics.DrawString(customText, customFont, textBrush, borderRectangle, centerFormat);
                                }

                                //save updated image
                                using (MemoryStream ms = new MemoryStream())
                                {
                                    image.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                                    File.WriteAllBytes(Path.Combine(imgSavePath, Path.GetFileName(imagepath)), ms.ToArray());
                                }
                            }
                        }
                        else
                        {
                            File.WriteAllBytes(Path.Combine(imgSavePath, Path.GetFileName(imagepath)), imageData);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error in ExportImages : {ex.Message}");
            }
        }

        public void ExportCameraImages(List<string> imagePaths, string? partPath, string savePath)
        {
            try
            {
                if (!Directory.Exists(savePath))
                    Directory.CreateDirectory(savePath);

                //add image band info
                foreach (string imagepath in imagePaths)
                {
                    //check paths
                    string camImagePath = imagepath;
                    if (!Path.IsPathFullyQualified(imagepath) && !string.IsNullOrEmpty(partPath))
                    {
                        camImagePath = Path.Combine(partPath, imagepath);
                    }

                    if (File.Exists(camImagePath))
                    {
                        byte[] imageData = File.ReadAllBytes(camImagePath);
                        File.WriteAllBytes(Path.Combine(savePath, Path.GetFileName(camImagePath)), imageData);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error in ExportCameraImages : {ex.Message}");
            }
        }

        /// Creates videos for the selected image layers (LCMS, Camera, or 360 types).
        public void CreateVideo(int surveyId, string partPath, string imagesDirectory, string savePath, string extension, double framerate, List<string> selectedVideoLayers, string videoType)
        {
            void Process(string type, string fileExtension, string folderPath)
            {
                CreateVideoByType(type, partPath, fileExtension, surveyId, folderPath, savePath, extension, framerate);
            }

            // Goes through the selected videotype and processes those images.
            switch (videoType)
            {
                case "LCMS":
                    if (selectedVideoLayers.Contains("Range Images"))
                    {
                        string rangeDir = Path.Combine(imagesDirectory, "Range Images");
                        Process("Range", "*_Range.jpg", rangeDir);
                    }

                    if (selectedVideoLayers.Contains("Intensity Images"))
                    {
                        string intensityDir = Path.Combine(imagesDirectory, "Intensity Images");
                        Process("Intensity", "*_Intensity.jpg", intensityDir);
                    }

                    if (selectedVideoLayers.Contains("Range Images (Defect Overlay)"))
                    {
                        string rangeOverlayDir = Path.Combine(imagesDirectory, "Range Images (Defect Overlay)");
                        Process("RangeOverlay", "*_RngOverlay.jpg", rangeOverlayDir);
                    }

                    if (selectedVideoLayers.Contains("Intensity Images (Defect Overlay)"))
                    {
                        string intensityOverlayDir = Path.Combine(imagesDirectory, "Intensity Images (Defect Overlay)");
                        Process("IntensityOverlay", "*_Overlay.jpg", intensityOverlayDir);
                    }
                    break;

                case "Camera":
                    if (selectedVideoLayers.Contains("Pave Camera Images") && imagesDirectory.Contains("pave", StringComparison.OrdinalIgnoreCase))
                        Process("PaveVideo", "*.jpg", imagesDirectory);

                    if (selectedVideoLayers.Contains("Row Camera Images") && imagesDirectory.Contains("row", StringComparison.OrdinalIgnoreCase))
                        Process("RowVideo", "*.jpg", imagesDirectory);
                    break;

                case "360":
                    if (selectedVideoLayers.Contains("Panoramic Images"))
                        Process("360Video", "*.jpg", imagesDirectory);
                    break;
            }
        }

        // Does the video creation logic for a specific image set (LCMS, Camera, or 360 types).
        private void CreateVideoByType(string type, string partPath, string sourceFileExt, int surveyId, string imagesDirectory, string savePath, string extension, double framerate)
        {
            string outputVideoPath = Path.Combine(savePath, $"{Path.GetFileName(savePath)}_{type}_{DateTime.Now.ToString("ddMMyyyyHHmmss")}.{extension.ToLower()}"); // Output video file

            int? fourCC = null;
            if (extension.ToLower() == "mp4")
                fourCC = FourCC.H264;//mp4
            else if (extension.ToLower() == "avi")
                fourCC = FourCC.XVID;//avi

            try
            {
                //check paths
                string imagePath = imagesDirectory;
                if (type == "PaveVideo" || type == "RowVideo")
                {
                    var imageFilePath = Path.Combine(partPath, imagesDirectory);
                    if (imageFilePath != null && File.Exists(imageFilePath))
                    {
                        imagePath = Path.GetDirectoryName(imageFilePath);
                    }
                }
                else if (type == "360Video")
                {
                    imagePath = Path.GetDirectoryName(imagesDirectory);
                }
                if (!Path.IsPathFullyQualified(imagePath) && !string.IsNullOrEmpty(partPath)) { imagePath = Path.Combine(partPath, imagesDirectory); }

                if (!Directory.Exists(savePath))
                    Directory.CreateDirectory(savePath);

                //get the list of sorted files
                string[] imageFiles = Directory.GetFiles(imagePath, sourceFileExt)
               .Select(f => new { FileName = f, Number = int.Parse(Regex.Matches(Path.GetFileNameWithoutExtension(f), @"\d+").Last().Value) })
               .OrderBy(x => x.Number)
               .Select(x => x.FileName)
               .ToArray();

                if (imageFiles.Length == 0)
                {
                    Log.Error("Error in CreateVideo : No images found in the specified directory.");
                    return;
                }

                // Read the first image to determine resolution
                Mat firstFrame = Cv2.ImRead(imageFiles[0]);
                if (firstFrame.Empty())
                {
                    Log.Error("Error in CreateVideo : Failed to read the first image.");
                    return;
                }

                int width = firstFrame.Width;
                int height = firstFrame.Height;

                if (fourCC != null && fourCC.HasValue)
                {
                    // Create VideoWriter
                    using (VideoWriter writer = new VideoWriter(outputVideoPath, fourCC.Value, framerate, new Size(width, height)))
                    {
                        if (!writer.IsOpened())
                        {
                            Log.Error("Failed to open VideoWriter.");
                            return;
                        }

                        Log.Information("Creating video...");
                        foreach (string imageFile in imageFiles)
                        {
                            Mat frame = Cv2.ImRead(imageFile);
                            if (frame.Empty())
                            {
                                Log.Information($"Failed to read image: {imageFile}");
                                continue;
                            }

                            // Ensure the frame size matches the video's resolution
                            if (frame.Size() != new Size(width, height))
                            {
                                Cv2.Resize(frame, frame, new Size(width, height));
                            }

                            writer.Write(frame);
                        }
                    }
                }

                Log.Information($"Video successfully created: {outputVideoPath}");
            }
            catch (Exception ex)
            {
                Log.Error($"Error in CreateVideo : {ex.Message}");
            }
        }
    }
}