using DataView2.Core.Models;
using DataView2.GrpcService.Data;
using DataView2.GrpcService.Protos;
using DataView2.GrpcService.Services.AppDbServices;
using Grpc.Core;
using Microsoft.Data.Sqlite;
using NetTopologySuite.Algorithm;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;  // For .xlsx files
using System.Globalization;
using System.ServiceModel.Channels;
using static DataView2.Core.Helper.XMLParser;
using OfficeOpenXml;
using OfficeOpenXml.Drawing.Chart;
using System.IO;

namespace DataView2.GrpcService.Services.OtherServices
{
    public class GenerateReportService: Protos.GenerateReportService.GenerateReportServiceBase
    {
        private readonly AppDbContextProjectData _appDbContext;
        private readonly AppDbContextMetadata _metadataContext;
        private readonly ImageBandInfoService _imageBandInfoService;
        private readonly DatabasePathProvider _databasePathProvider;
        private string connString;

        public GenerateReportService(DatabasePathProvider databasePathProvider, AppDbContextProjectData appDbContext, ImageBandInfoService imageBandInfoService, AppDbContextMetadata metadataContext)
        {
            _databasePathProvider = databasePathProvider;
            string actualDatabasePath = _databasePathProvider.GetDatasetDatabasePath();


            connString = $"Data Source={actualDatabasePath};";
            _appDbContext = appDbContext;
            _imageBandInfoService = imageBandInfoService;
            _metadataContext = metadataContext;
        }
        public override async Task<GenerateReportObjResponse> GenerateReportData(GenerateReportObjRequest request, ServerCallContext context)
        {
            string message;
            switch (request.FunctionName)
            {
                case "GenerateSeoulCityReport":
                    message = await GenerateSeoulCityReport(request);
                    break;
                case "GenerateHongkongReport":
                    message = await GenerateHongkongReport(request);
                    break;
                default:
                    throw new RpcException(new Status(StatusCode.Unimplemented, $"Function '{request.FunctionName}' is not implemented"));
            }
            return new GenerateReportObjResponse
            {
                Message = message
            };
        }
        private async Task<string> GenerateSeoulCityReport(GenerateReportObjRequest request)
        {
            try
            {
                using var connection = new SqliteConnection(connString);
                await connection.OpenAsync();
                List<string> selectedSurveys;
                if (string.IsNullOrEmpty(request.SelectedSurveys))
                {
                    selectedSurveys = await GetAllSurveyIds(connection);
                }
                else
                {
                    selectedSurveys = request.SelectedSurveys.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                                            .Select(s => s.Trim())
                                            .ToList();
                }
                if (selectedSurveys.Count == 0)
                    return "No surveys selected.";
                var surveyChainages = await GetSurveyChainageRanges(connection, selectedSurveys);
                if (surveyChainages.Count == 0)
                    return "No matching surveys found.";
                string templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Report", "KOREA Report Template.xlsx");
                if (!File.Exists(templatePath))
                {
                    return $"KOREA Report Template file not found at {templatePath}";
                }
                bool includeUrban = !string.IsNullOrEmpty(request.Param1Type) && request.Param1Type.IndexOf("urban", StringComparison.OrdinalIgnoreCase) >= 0;
                bool includeMain = !string.IsNullOrEmpty(request.Param1Type) && request.Param1Type.IndexOf("main", StringComparison.OrdinalIgnoreCase) >= 0;
                bool includeMinor = !string.IsNullOrEmpty(request.Param1Type) && request.Param1Type.IndexOf("minor", StringComparison.OrdinalIgnoreCase) >= 0;
                bool includeMunicipal = !string.IsNullOrEmpty(request.Param1Type) && request.Param1Type.IndexOf("municipal", StringComparison.OrdinalIgnoreCase) >= 0;
                double spiIndex = 0.0;
                if (includeUrban)
                {
                    spiIndex = 0.8;
                }
                else if (includeMain)
                {
                    spiIndex = 0.727;
                }
                else if (includeMinor)
                {
                    spiIndex = 0.667;
                }
                const string MtqLongitudinal = "Longitudinal";
                const string MtqTransversal = "Transversal";
                const string PavementAsphalt = "Asphalt";
                const string SeverityLow = "Low";
                const string SeverityMedium = "Medium";
                const string SeverityHigh = "High";
                const double MmToMeter = 1000.0;
                const string MtqAlligator = "Alligator";
                XSSFWorkbook workbook;

                int startRow = 13; //First data line
                int currentRow3 = startRow;
                int currentRow4 = startRow;
                int currentRow5 = startRow;
                // B-series fields
                string sSurveyFileNameB1 = string.Empty;
                string sSurveyorB2 = string.Empty;
                string sRouteB3 = string.Empty;
                string sDestinationB4 = string.Empty;
                string sLaneB5 = string.Empty;
                string sStartingPointB6 = string.Empty;
                string sEndingPointB7 = string.Empty;
                string sSurveyIntervalMeterB8 = "1"; // Fix 1 meter
                string sStartTimeB9 = string.Empty;
                string sEndTimeB10 = string.Empty;

                // D-series fields
                string sSurveyTypeD1 = string.Empty;  // NO idea where come from
                string sRoadClassD2 = request.Param1Type;  // Urban,Main,Minor

                string sRouteNumberD6 = string.Empty;
                string sDestinationCodeD7 = string.Empty;
                string sRoadNameD8 = string.Empty;
                string sSurveySectionStartD9 = string.Empty;
                string sSurveySectionEndD10 = string.Empty;
                string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");


                foreach (var survey in surveyChainages)
                {
                    int id = survey.id;
                    decimal startChainage = survey.startChainage;
                    decimal endChainage = survey.endChainage;
                    string surveyIdExternal = survey.surveyIdExternal;
                    string surveyName = survey.surveyName;
                    string surveyor = survey.surveyor;
                    string surveyDate = survey.surveyDate;
                    string sDirection = survey.Direction;
                    string sLane = survey.Lane;
                    sStartingPointB6 = string.Empty;
                    sEndingPointB7 = string.Empty;
                    sStartTimeB9 = string.Empty;
                    sEndTimeB10 = string.Empty;
                    sSurveySectionStartD9 = string.Empty;
                    sSurveySectionEndD10 = string.Empty;

                    if (endChainage < startChainage)
                    {
                        return $"Invalid data for survey '{surveyIdExternal}': EndChainage ({endChainage}) is less than StartChainage ({startChainage}).";
                    }

                    using (var templateStream = new FileStream(templatePath, FileMode.Open, FileAccess.Read))
                    {
                        workbook = new XSSFWorkbook(templateStream);
                    }
                    ISheet worksheet3 = workbook.GetSheetAt(2); // zero-based index, 3rd sheet is index 2
                    ISheet worksheet4 = workbook.GetSheetAt(3); // Sheet 4
                    ISheet worksheet5 = workbook.GetSheetAt(4); // Sheet 5

                    var chainageList = Enumerable.Range((int)startChainage, (int)(endChainage - startChainage) + 1)
                                        .Select(i => (decimal)i)
                                        .ToList();
                    var endSurveyTime = await GetLastChainageTime(connection, id);

                    sSurveyFileNameB1 = surveyName;
                    sSurveyorB2 = surveyor;
                    sStartTimeB9 = surveyDate;
                    sEndTimeB10 = endSurveyTime ?? "NA";
                    sDestinationB4 = sDirection;
                    sLaneB5 = sLane;

                    var isAsphalt = await IsPavementTypeAsphaltAsync(connection, surveyIdExternal);
                    string videoFrameQuery = "SELECT Chainage, ImageFileName AS ImageFile FROM VideoFrame WHERE SurveyId = @Id;";
                    using var videoCmd = new SqliteCommand(videoFrameQuery, connection);
                    videoCmd.Parameters.AddWithValue("@Id", id);
                    var videoFrameImages = await GetVideoFilesByCommand(videoCmd);
                    string lcmsSegmentQuery = "SELECT Chainage, ImageFilePath AS ImageFile,SectionId FROM LCMS_Segment WHERE SurveyId = @SurveyId;";
                    using var lcmsCmd = new SqliteCommand(lcmsSegmentQuery, connection);
                    lcmsCmd.Parameters.AddWithValue("@SurveyId", surveyIdExternal);
                    var roadSurfaceImages = await GetImageFilesByCommand(lcmsCmd);
                    string laneDepthQuery = "SELECT Chainage, LaneDepth_mm FROM LCMS_Rut_Processed WHERE SurveyId = @SurveyId;";
                    using var laneDepthCmd = new SqliteCommand(laneDepthQuery, connection);
                    laneDepthCmd.Parameters.AddWithValue("@SurveyId", surveyIdExternal);
                    var laneDepthDict = await GetDecimalValuesByCommand(laneDepthCmd);
                    string laneRoughQuery = @"SELECT Chainage, LaneIRI, GPSLatitude, GPSLongitude, Speed 
                          FROM LCMS_Rough_Processed 
                          WHERE SurveyId = @SurveyId 
                          ORDER BY Chainage;";

                    using var laneRoughCmd = new SqliteCommand(laneRoughQuery, connection);
                    laneRoughCmd.Parameters.AddWithValue("@SurveyId", surveyIdExternal);

                    var laneIRI_Dict = new Dictionary<decimal, decimal>();
                    var gpsLatitudeDict = new Dictionary<decimal, decimal>();
                    var gpsLongitudeDict = new Dictionary<decimal, decimal>();

                    // Keep a rolling list of last 20 LaneIRI values
                    var last20IRI = new Queue<decimal>();

                    using var reader = await laneRoughCmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        if (!reader.IsDBNull(0))
                        {
                            decimal chainage = reader.GetDecimal(0);
                            decimal? laneIRI = reader.IsDBNull(1) ? (decimal?)null : reader.GetDecimal(1);
                            decimal? gpsLat = reader.IsDBNull(2) ? (decimal?)null : reader.GetDecimal(2);
                            decimal? gpsLon = reader.IsDBNull(3) ? (decimal?)null : reader.GetDecimal(3);
                            decimal? speed = reader.IsDBNull(4) ? (decimal?)null : reader.GetDecimal(4);

                            if (laneIRI.HasValue)
                            {
                                // Add current IRI to rolling queue
                                last20IRI.Enqueue(laneIRI.Value);
                                if (last20IRI.Count > 20)
                                    last20IRI.Dequeue();

                                // If speed < 20 and we have at least 20 previous points, average them
                                if (speed.HasValue && speed.Value < 20 && last20IRI.Count == 20)
                                {
                                    laneIRI_Dict[chainage] = last20IRI.Average();
                                }
                                else
                                {
                                    laneIRI_Dict[chainage] = laneIRI.Value;
                                }
                            }

                            if (gpsLat.HasValue)
                                gpsLatitudeDict[chainage] = gpsLat.Value;

                            if (gpsLon.HasValue)
                                gpsLongitudeDict[chainage] = gpsLon.Value;
                        }
                    }
                    var crackSummaryDict = await GetCrackSummaryData(connection, surveyIdExternal, startChainage, endChainage);
                    var sectionLengthDict = await GetSectionLengths(connection, surveyIdExternal, startChainage, endChainage);
                    var laneWidthDict = await GetLaneWidths(connection, surveyIdExternal, startChainage, endChainage);
                    Dictionary<decimal, double> crackRateDict = new Dictionary<decimal, double>();
                    var groupedCrackLengthSums = await GetGroupedCrackLengthSums(connection, surveyIdExternal, startChainage, endChainage);
                    var mtqListAlligator = new List<string> { "Alligator" };
                    var alligatorCrackAreas = await GetGroupedCrackAreasByMTQs(connection, surveyIdExternal, startChainage, endChainage, mtqListAlligator);
                    var mtqListOthers = new List<string> { "Unknown", "Multiple" };
                    var OthersCrackAreas = await GetGroupedCrackAreasByMTQs(connection, surveyIdExternal, startChainage, endChainage, mtqListOthers);
                    var groupedPatchAreaSums = await GetGroupedPatchAreaSums(connection, surveyIdExternal, startChainage, endChainage);
                    var groupedPotholeAreaSums = await GetGroupedPotholeAreaSums(connection, surveyIdExternal, startChainage, endChainage);
                    var bleedingAreaSums = await GetGroupedBleedingAreaSums(connection, surveyIdExternal, startChainage, endChainage);
                    crackSummaryDict = MergePatchPotholeAreasIntoCrackSummary(crackSummaryDict, groupedPatchAreaSums, groupedPotholeAreaSums, surveyIdExternal);
                    string lastVideoImage = null;
                    string lastRoadSurfaceImage = null;
                    decimal lastLaneDepthMm = 0.0M;
                    decimal lastLaneIRI = 0.0M;
                    decimal lastGpsLat = 0.0M;
                    decimal lastGpsLong = 0.0M;
                    decimal lastLaneWidth = 0.0M;
                    decimal lastSectionLength = 0.0M;
                    string lastSectionId = string.Empty;
                    // reset first row
                    currentRow3 = startRow;
                    currentRow4 = startRow;
                    currentRow5 = startRow;

                    foreach (var chainage in chainageList)
                    {

                        SetCellValue(worksheet3, currentRow3, "A", chainage);
                        if (videoFrameImages.TryGetValue(chainage, out string videoImage))
                            lastVideoImage = videoImage;
                        if (lastVideoImage != null)
                            SetCellValue(worksheet3, currentRow3, "B", lastVideoImage);
                        if (roadSurfaceImages.TryGetValue(chainage, out var info))
                        {
                            lastRoadSurfaceImage = info.fileName;
                            lastSectionId = info.sectionId;
                        }

                        if (lastRoadSurfaceImage != null)
                            SetCellValue(worksheet3, currentRow3, "C", lastRoadSurfaceImage);
                        if (laneDepthDict.TryGetValue(chainage, out decimal laneDepthMm))
                            lastLaneDepthMm = laneDepthMm;
                        SetCellValue(worksheet3, currentRow3, "D", (double)lastLaneDepthMm);
                        decimal dLaneDepthMm = lastLaneDepthMm;
                        if (laneIRI_Dict.TryGetValue(chainage, out decimal laneIRI))
                            lastLaneIRI = laneIRI;
                        SetCellValue(worksheet3, currentRow3, "E", (double)lastLaneIRI);
                        double iriValue = (double)lastLaneIRI;
                        if (gpsLatitudeDict.TryGetValue(chainage, out decimal gpsLat))
                            lastGpsLat = gpsLat;
                        SetCellValue(worksheet3, currentRow3, "F", (double)lastGpsLat);
                        if (gpsLongitudeDict.TryGetValue(chainage, out decimal gpsLong))
                            lastGpsLong = gpsLong;
                        SetCellValue(worksheet3, currentRow3, "G", (double)lastGpsLong);

                        string currentPoint = (lastGpsLat == 0 || lastGpsLong == 0) ? "(NA,NA)" : $"({lastGpsLat},{lastGpsLong})";

                        if (chainage == chainageList.First())
                        {
                            if (string.IsNullOrEmpty(sStartingPointB6))
                            {
                                sStartingPointB6 = currentPoint;
                                sSurveySectionStartD9 = lastSectionId;
                            }
                            else
                            {
                                sStartingPointB6 += $"/{currentPoint}";
                                sSurveySectionStartD9 += $"/{lastSectionId}";
                            }
                        }
                        else if (chainage == chainageList.Last())
                        {
                            if (string.IsNullOrEmpty(sEndingPointB7))
                            {
                                sEndingPointB7 = currentPoint;
                                sSurveySectionEndD10 = lastSectionId;
                            }
                            else
                            {
                                sEndingPointB7 += $"/{currentPoint}";
                                sSurveySectionEndD10 += $"/{lastSectionId}";
                            }
                        }
                        double crackArea = 0.0;
                        if (crackSummaryDict.TryGetValue((surveyIdExternal, chainage), out var crackData))
                        {
                            crackArea = (crackData.LinearLengthSum * 0.3) + crackData.ArealAreaSum;
                        }
                        SetCellValue(worksheet3, currentRow3, "H", crackArea);
                        double crackRate = 0.0;
                        decimal dLaneWidth = 0.0M;
                        decimal dSectionLength = 0.0M;
                        if (laneWidthDict.TryGetValue(chainage, out decimal laneWidth))
                            lastLaneWidth = laneWidth;
                        dLaneWidth = lastLaneWidth;
                        if (sectionLengthDict.TryGetValue(chainage, out decimal sectionLength))
                            lastSectionLength = sectionLength;
                        dSectionLength = lastSectionLength;
                        if (dLaneWidth > 0 && sectionLength > 0)
                            crackRate = (crackArea / ((double)dLaneWidth * (double)dSectionLength)) * 100.0;
                        SetCellValue(worksheet3, currentRow3, "I", crackRate);
                        crackRateDict[chainage] = crackRate;
                        double spi3Value = 10 - spiIndex * iriValue;
                        if (includeUrban)
                            SetCellValue(worksheet3, currentRow3, "J", spi3Value);
                        else if (includeMain)
                            SetCellValue(worksheet3, currentRow3, "K", spi3Value);
                        else if (includeMinor)
                            SetCellValue(worksheet3, currentRow3, "L", spi3Value);

                        double apLongLow = 0.0, apLongMed = 0.0, apLongHigh = 0.0;
                        double apTransLow = 0.0, apTransMed = 0.0, apTransHigh = 0.0;
                        if (groupedCrackLengthSums.TryGetValue((chainage, MtqLongitudinal, PavementAsphalt, SeverityLow), out double valLongLow))
                        {
                            apLongLow = valLongLow / MmToMeter;
                            SetCellValue(worksheet3, currentRow3, "M", apLongLow);
                        }
                        if (groupedCrackLengthSums.TryGetValue((chainage, MtqLongitudinal, PavementAsphalt, SeverityMedium), out double valLongMed))
                        {
                            apLongMed = valLongMed / MmToMeter;
                            SetCellValue(worksheet3, currentRow3, "N", apLongMed);
                        }
                        if (groupedCrackLengthSums.TryGetValue((chainage, MtqLongitudinal, PavementAsphalt, SeverityHigh), out double valLongHigh))
                        {
                            apLongHigh = valLongHigh / MmToMeter;
                            SetCellValue(worksheet3, currentRow3, "O", apLongHigh);
                        }
                        if (groupedCrackLengthSums.TryGetValue((chainage, MtqTransversal, PavementAsphalt, SeverityLow), out double valTransLow))
                        {
                            apTransLow = valTransLow / MmToMeter;
                            SetCellValue(worksheet3, currentRow3, "P", apTransLow);
                        }
                        if (groupedCrackLengthSums.TryGetValue((chainage, MtqTransversal, PavementAsphalt, SeverityMedium), out double valTransMed))
                        {
                            apTransMed = valTransMed / MmToMeter;
                            SetCellValue(worksheet3, currentRow3, "Q", apTransMed);
                        }
                        if (groupedCrackLengthSums.TryGetValue((chainage, MtqTransversal, PavementAsphalt, SeverityHigh), out double valTransHigh))
                        {
                            apTransHigh = valTransHigh / MmToMeter;
                            SetCellValue(worksheet3, currentRow3, "R", apTransHigh);
                        }
                        double alligatorLow = 0;
                        double alligatorMedium = 0;
                        double alligatorHigh = 0;
                        double patchLow = 0;
                        double patchMedium = 0;
                        double patchHigh = 0;
                        if (alligatorCrackAreas != null)
                        {
                            if (alligatorCrackAreas.TryGetValue((chainage, MtqAlligator, PavementAsphalt, SeverityLow), out double valLow))
                            {
                                alligatorLow = valLow;
                                SetCellValue(worksheet3, currentRow3, "V", alligatorLow);
                            }
                            if (alligatorCrackAreas.TryGetValue((chainage, MtqAlligator, PavementAsphalt, SeverityMedium), out double valMed))
                            {
                                alligatorMedium = valMed;
                                SetCellValue(worksheet3, currentRow3, "W", alligatorMedium);
                            }
                            if (alligatorCrackAreas.TryGetValue((chainage, MtqAlligator, PavementAsphalt, SeverityHigh), out double valHigh))
                            {
                                alligatorHigh = valHigh;
                                SetCellValue(worksheet3, currentRow3, "X", alligatorHigh);
                            }
                        }
                        if (groupedPatchAreaSums != null)
                        {
                            if (groupedPatchAreaSums.TryGetValue((chainage, PavementAsphalt, SeverityLow), out double valPatchLow))
                            {
                                patchLow = valPatchLow;
                                SetCellValue(worksheet3, currentRow3, "Y", patchLow);
                            }
                            if (groupedPatchAreaSums.TryGetValue((chainage, PavementAsphalt, SeverityMedium), out double valPatchMed))
                            {
                                patchMedium = valPatchMed;
                                SetCellValue(worksheet3, currentRow3, "Z", patchMedium);
                            }
                            if (groupedPatchAreaSums.TryGetValue((chainage, PavementAsphalt, SeverityHigh), out double valPatchHigh))
                            {
                                patchHigh = valPatchHigh;
                                SetCellValue(worksheet3, currentRow3, "AA", patchHigh);
                            }
                        }
                        if (groupedPotholeAreaSums != null)
                        {
                            if (groupedPotholeAreaSums.TryGetValue((chainage, PavementAsphalt, SeverityLow), out double potholeLow))
                            {
                                SetCellValue(worksheet3, currentRow3, "AB", potholeLow);
                            }
                            if (groupedPotholeAreaSums.TryGetValue((chainage, PavementAsphalt, SeverityMedium), out double potholeMedium))
                            {
                                SetCellValue(worksheet3, currentRow3, "AC", potholeMedium);
                            }
                            if (groupedPotholeAreaSums.TryGetValue((chainage, PavementAsphalt, SeverityHigh), out double potholeHigh))
                            {
                                SetCellValue(worksheet3, currentRow3, "AD", potholeHigh);
                            }
                        }
                        double bleedingLow = 0;
                        double bleedingMedium = 0;
                        double bleedingHigh = 0;
                        if (bleedingAreaSums != null)
                        {
                            if (bleedingAreaSums.TryGetValue((chainage, PavementAsphalt, SeverityLow), out double valBleedingLow))
                            {
                                bleedingLow = valBleedingLow;
                                SetCellValue(worksheet3, currentRow3, "AN", bleedingLow);
                            }
                            if (bleedingAreaSums.TryGetValue((chainage, PavementAsphalt, SeverityMedium), out double valBleedingMed))
                            {
                                bleedingMedium = valBleedingMed;
                                SetCellValue(worksheet3, currentRow3, "AO", bleedingMedium);
                            }
                            if (bleedingAreaSums.TryGetValue((chainage, PavementAsphalt, SeverityHigh), out double valBleedingHigh))
                            {
                                bleedingHigh = valBleedingHigh;
                                SetCellValue(worksheet3, currentRow3, "AP", bleedingHigh);
                            }
                        }
                        if (OthersCrackAreas != null)
                        {
                            bool hasLowValue = false, hasMedValue = false, hasHighValue = false;
                            double othersLow = 0, othersMed = 0, othersHigh = 0;
                            double multipleLow = 0, multipleMed = 0, multipleHigh = 0;
                            if (OthersCrackAreas.TryGetValue((chainage, "Unknown", PavementAsphalt, SeverityLow), out double val))
                            {
                                othersLow = val;
                                hasLowValue = true;
                            }
                            if (OthersCrackAreas.TryGetValue((chainage, "Unknown", PavementAsphalt, SeverityMedium), out val))
                            {
                                othersMed = val;
                                hasMedValue = true;
                            }
                            if (OthersCrackAreas.TryGetValue((chainage, "Unknown", PavementAsphalt, SeverityHigh), out val))
                            {
                                othersHigh = val;
                                hasHighValue = true;
                            }
                            if (OthersCrackAreas.TryGetValue((chainage, "Multiple", PavementAsphalt, SeverityLow), out val))
                            {
                                multipleLow = val;
                                hasLowValue = true;
                            }
                            if (OthersCrackAreas.TryGetValue((chainage, "Multiple", PavementAsphalt, SeverityMedium), out val))
                            {
                                multipleMed = val;
                                hasMedValue = true;
                            }
                            if (OthersCrackAreas.TryGetValue((chainage, "Multiple", PavementAsphalt, SeverityHigh), out val))
                            {
                                multipleHigh = val;
                                hasHighValue = true;
                            }
                            if (hasLowValue)
                            {
                                SetCellValue(worksheet3, currentRow3, "AQ", othersLow + multipleLow);
                            }
                            if (hasMedValue)
                            {
                                SetCellValue(worksheet3, currentRow3, "AR", othersMed + multipleMed);
                            }
                            if (hasHighValue)
                            {
                                SetCellValue(worksheet3, currentRow3, "AS", othersHigh + multipleHigh);
                            }
                        }
                        SetCellValue(worksheet3, currentRow3, "AT", dLaneWidth);
                        SetCellValue(worksheet3, currentRow3, "AU", dLaneWidth * dSectionLength);
                        // SPI2 = 10 − 0.267 × RD, RD: Rut depth (in mm)
                        SetCellValue(worksheet3, currentRow3, "AX", 10 - 0.267M * dLaneDepthMm);
                        // SPI3
                        SetCellValue(worksheet3, currentRow3, "AY", spi3Value);
                        // SPI1
                        double SPI1 = 10 - 1.667 * Math.Pow(crackRate, 0.38);
                        SetCellValue(worksheet3, currentRow3, "AZ", SPI1);
                        // HPCI calculation
                        if (isAsphalt)
                        {
                            double unitSectionLengthMeters = 100.0;
                            double crackLengthC = apLongLow + apLongMed + apLongHigh + apTransLow + apTransMed + apTransHigh;
                            double totalArea = alligatorLow + alligatorMedium + alligatorHigh + patchLow + patchMedium + patchHigh;
                            double areaPer100m = (totalArea / unitSectionLengthMeters) * 100.0;
                            double rutDepthCm = (double)dLaneDepthMm / 10;
                            double HPCI = 4.564 - 0.348 * iriValue - 0.36 * rutDepthCm - 0.01 * 5 * crackLengthC + areaPer100m;
                            SetCellValue(worksheet3, currentRow3, "BA", HPCI);
                        }
                        else
                        {
                            double patchingArea = patchLow + patchMedium + patchHigh;
                            double doubleLaneWidth = (double)dLaneWidth;
                            double C = doubleLaneWidth > 0 ? crackArea / (doubleLaneWidth * 100.0) : 0.0;
                            double P = doubleLaneWidth > 0 ? patchingArea / (doubleLaneWidth * 100.0) : 0.0;
                            double HPCI = 7.35
                                - 4.65 * Math.Log10(1 + iriValue)
                                - 1.06 * Math.Log10(10 + 2.5 * C)
                                - 0.32 * Math.Log10(10 + 2.5 * P);
                            SetCellValue(worksheet3, currentRow3, "BB", HPCI);
                        }

                        // CrackRate is already defined
                        // Rutting = lastLaneDepthMm
                        // IRI = lastLaneIRI

                        double NHPCI = 1.0 / Math.Pow(
                            0.33 +
                            (0.003 * crackRate) +
                            (0.004 * (double)lastLaneDepthMm) +
                            (0.0183 * (double)lastLaneIRI),
                            2);

                        // Write result to column BC
                        SetCellValue(worksheet3, currentRow3, "BC", NHPCI);

                        if (includeMunicipal)
                        {
                            // CrackRate is already defined in your SPI1 calculation
                            // Rutting = lastLaneDepthMm
                            // IRI = lastLaneIRI

                            double A = 10 - (1.67 * Math.Pow(crackRate, 0.47));
                            double B = 10 - (0.4 * Math.Pow((double)lastLaneDepthMm, 0.85));
                            double C = 10 - (0.87 * (double)lastLaneIRI);

                            double MPCI = 10 - Math.Pow(
                                Math.Pow(10 - A, 5) +
                                Math.Pow(10 - B, 5) +
                                Math.Pow(10 - C, 5),
                                0.2);

                            // Write result to column BD
                            SetCellValue(worksheet3, currentRow3, "BD", MPCI);
                        }

                        IRow sourceRow = worksheet3.GetRow(currentRow3-1);

                        // Sheet 4: every 10 meters
                        if ((int)chainage % 10 == 0)
                        {
                            IRow targetRow4 = worksheet4.CreateRow(currentRow4-1);
                            CopyRow(sourceRow, targetRow4);
                            currentRow4++;
                        }

                        // Sheet 5: every 20 meters
                        if ((int)chainage % 20 == 0)
                        {
                            IRow targetRow5 = worksheet5.CreateRow(currentRow5-1);
                            CopyRow(sourceRow, targetRow5);
                            currentRow5++;
                        }

                        currentRow3++;
                    }

                    // Request Korea customer to define survey ID as: Route Name_Route No._Route_OtherInfo . so extract these info from survey ID
                    ExtractRouteInfo(survey.surveyName, out sRoadNameD8, out sRouteNumberD6, out sRouteB3);

                    SetCellValue(worksheet3, 1, "B", sSurveyFileNameB1);
                    SetCellValue(worksheet3, 2, "B", sSurveyorB2);
                    SetCellValue(worksheet3, 3, "B", sRouteB3);
                    SetCellValue(worksheet3, 4, "B", sDestinationB4);
                    SetCellValue(worksheet3, 5, "B", sLaneB5);
                    SetCellValue(worksheet3, 6, "B", sStartingPointB6);
                    SetCellValue(worksheet3, 7, "B", sEndingPointB7);
                    SetCellValue(worksheet3, 8, "B", sSurveyIntervalMeterB8);
                    SetCellValue(worksheet3, 9, "B", sStartTimeB9);
                    SetCellValue(worksheet3, 10, "B", sEndTimeB10);
                    SetCellValue(worksheet3, 2, "D", sRoadClassD2);
                    SetCellValue(worksheet3, 6, "D", sRouteNumberD6);
                    SetCellValue(worksheet3, 8, "D", sRoadNameD8);
                    SetCellValue(worksheet3, 9, "D", sSurveySectionStartD9);
                    SetCellValue(worksheet3, 10, "D", sSurveySectionEndD10);

                    // Copy top 14 rows from sheet 3 to sheets 4 and 5
                    for (int rowIndex = 0; rowIndex < startRow; rowIndex++)
                    {
                        IRow row3 = worksheet3.GetRow(rowIndex);
                        if (row3 == null) continue;

                        // Create rows in sheet 4 and 5 if they don't exist
                        IRow row4 = worksheet4.GetRow(rowIndex) ?? worksheet4.CreateRow(rowIndex);
                        IRow row5 = worksheet5.GetRow(rowIndex) ?? worksheet5.CreateRow(rowIndex);

                        // Only copy column B (index 1) and column D (index 3)
                        foreach (int colIndex in new[] { 1, 3 })
                        {
                            var cell3 = row3.GetCell(colIndex);
                            if (cell3 == null) continue;

                            var cell4 = row4.GetCell(colIndex) ?? row4.CreateCell(colIndex);
                            var cell5 = row5.GetCell(colIndex) ?? row5.CreateCell(colIndex);

                            switch (cell3.CellType)
                            {
                                case CellType.String:
                                    cell4.SetCellValue(cell3.StringCellValue);
                                    cell5.SetCellValue(cell3.StringCellValue);
                                    break;
                                case CellType.Numeric:
                                    cell4.SetCellValue(cell3.NumericCellValue);
                                    cell5.SetCellValue(cell3.NumericCellValue);
                                    break;
                                case CellType.Boolean:
                                    cell4.SetCellValue(cell3.BooleanCellValue);
                                    cell5.SetCellValue(cell3.BooleanCellValue);
                                    break;
                                case CellType.Formula:
                                    cell4.SetCellFormula(cell3.CellFormula);
                                    cell5.SetCellFormula(cell3.CellFormula);
                                    break;
                                default:
                                    cell4.SetCellValue(cell3.ToString());
                                    cell5.SetCellValue(cell3.ToString());
                                    break;
                            }
                        }
                    }

                    string outputFileName = $"{surveyName}_Report_{timestamp}.xlsx";
                    string outputPath = Path.Combine(request.SavePathDirectory, outputFileName);
                    Directory.CreateDirectory(request.SavePathDirectory);
                    using (var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
                    {
                        workbook.Write(fileStream);
                    }

                    GenerateDefectChart(outputPath, startRow, currentRow3, currentRow4, currentRow5);
                    GenerateOtherDefectCharts(outputPath, startRow, currentRow3, currentRow4, currentRow5);
                }

                return $"Report generated successfully!";
            }
            catch (Exception ex)
            {
                return $"Failed to generate Seoul City report: {ex.Message}";
            }
        }

        private void CopyRow(IRow source, IRow target)
        {
            for (int i = 0; i < source.LastCellNum; i++)
            {
                ICell sourceCell = source.GetCell(i);
                if (sourceCell == null) continue;

                ICell targetCell = target.CreateCell(i);

                switch (sourceCell.CellType)
                {
                    case CellType.String:
                        targetCell.SetCellValue(sourceCell.StringCellValue);
                        break;
                    case CellType.Numeric:
                        targetCell.SetCellValue(sourceCell.NumericCellValue);
                        break;
                    case CellType.Boolean:
                        targetCell.SetCellValue(sourceCell.BooleanCellValue);
                        break;
                    case CellType.Formula:
                        targetCell.SetCellFormula(sourceCell.CellFormula);
                        break;
                    default:
                        targetCell.SetCellValue(sourceCell.ToString());
                        break;
                }
            }
        }

        public void ExtractRouteInfo(List<string> selectedSurveys, out string sRoadNameD8, out string sRouteNumberD6, out string sRouteB3)
        {
            List<string> routeNames = new List<string>();
            List<string> routeNumbers = new List<string>();
            List<string> routes = new List<string>();

            foreach (var survey in selectedSurveys)
            {
                string routeName = "NA";
                string routeNumber = "NA";
                string route = "NA";

                var parts = survey.Split('_');

                // Route name
                if (parts.Length >= 1 && !string.IsNullOrWhiteSpace(parts[0]))
                {
                    routeName = parts[0];
                }

                // Route number
                if (parts.Length >= 2 && !string.IsNullOrWhiteSpace(parts[1]))
                {
                    routeNumber = parts[1];
                }

                // Route (e.g., "Route2")
                if (parts.Length >= 3 && !string.IsNullOrWhiteSpace(parts[2]))
                {
                    route = parts[2];
                }

                routeNames.Add(routeName);
                routeNumbers.Add(routeNumber);
                routes.Add(route);
            }

            sRoadNameD8 = string.Join("/", routeNames);
            sRouteNumberD6 = string.Join("/", routeNumbers);
            sRouteB3 = string.Join("/", routes);
        }

        public void ExtractRouteInfo(string survey, out string sRoadNameD8, out string sRouteNumberD6, out string sRouteB3)
        {
            string routeName = "NA";
            string routeNumber = "NA";
            string route = "NA";

            var parts = survey.Split('_');

            // Route name
            if (parts.Length >= 1 && !string.IsNullOrWhiteSpace(parts[0]))
            {
                routeName = parts[0];
            }

            // Route number
            if (parts.Length >= 2 && !string.IsNullOrWhiteSpace(parts[1]))
            {
                routeNumber = parts[1];
            }

            // Route (e.g., "Route2")
            if (parts.Length >= 3 && !string.IsNullOrWhiteSpace(parts[2]))
            {
                route = parts[2];
            }

            sRoadNameD8 = routeName;
            sRouteNumberD6 = routeNumber;
            sRouteB3 = route;
        }

        // Helper method to set cell value by Excel-style column letter and 1-based row number
        private void SetCellValue(ISheet sheet, int rowNumber, string columnLetter, object value)
        {
            // Convert column letter (e.g. "A", "B", "AA") to zero-based column index
            int columnIndex = ColumnLetterToNumber(columnLetter) - 1; // zero-based
            int rowIndex = rowNumber - 1; // zero-based
            var row = sheet.GetRow(rowIndex) ?? sheet.CreateRow(rowIndex);
            var cell = row.GetCell(columnIndex) ?? row.CreateCell(columnIndex);
            if (value == null)
            {
                cell.SetCellType(CellType.Blank);
                return;
            }
            switch (value)
            {
                case string s:
                    cell.SetCellValue(s);
                    break;
                case int i:
                    cell.SetCellValue((double)i);
                    break;
                case decimal d:
                    cell.SetCellValue((double)d);
                    break;
                case double db:
                    cell.SetCellValue(db);
                    break;
                case float f:
                    cell.SetCellValue((double)f);
                    break;
                case bool b:
                    cell.SetCellValue(b);
                    break;
                default:
                    cell.SetCellValue(value.ToString());
                    break;
            }
        }
        // Convert Excel column letter to number (A=1, B=2, ..., AA=27, etc.)
        private int ColumnLetterToNumber(string columnLetter)
        {
            int sum = 0;
            foreach (char c in columnLetter.ToUpper())
            {
                sum *= 26;
                sum += (c - 'A' + 1);
            }
            return sum;
        }

        private async Task<bool> IsPavementTypeAsphaltAsync(SqliteConnection connection, string surveyIdExternal)
        {
            string query = @"
        SELECT PavementType
        FROM LCMS_Segment
        WHERE SurveyId = @SurveyId
        LIMIT 1;";
            using var cmd = new SqliteCommand(query, connection);
            cmd.Parameters.AddWithValue("@SurveyId", surveyIdExternal);
            var result = await cmd.ExecuteScalarAsync();
            if (result != null && result != DBNull.Value)
            {
                string pavementType = result.ToString();
                return string.Equals(pavementType, "Asphalt", StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                // No record found, return false by default or adjust as needed
                return false;
            }
        }

        private async Task<List<string>> GetAllSurveyIds(SqliteConnection connection)
        {
            var surveyIds = new List<string>();
            string query = "SELECT SurveyIdExternal FROM Survey WHERE SurveyIdExternal IS NOT NULL AND SurveyIdExternal != '';";
            using var cmd = new SqliteCommand(query, connection);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                string surveyId = reader.GetString(0);
                if (!string.IsNullOrWhiteSpace(surveyId))
                {
                    surveyIds.Add(surveyId);
                }
            }
            return surveyIds;
        }

        // Reads StartChainage and EndChainage from Survey table
        private async Task<List<(int id, decimal startChainage, decimal endChainage, string surveyIdExternal, string surveyName, 
            string surveyor, string surveyDate,string Direction,string Lane)>> GetSurveyChainageRanges(SqliteConnection connection, List<string> SurveyNames)
        {
            var ranges = new List<(int, decimal, decimal, string, string, string,string, string, string)>();
            if (SurveyNames == null || SurveyNames.Count == 0)
                return ranges;
            // Prepare query with parameters for IN clause
            string paramNames = string.Join(",", SurveyNames.Select((_, i) => $"@SurveyName{i}"));
            string query = $"SELECT Id, StartChainage, EndChainage, SurveyIdExternal,SurveyName,Operator,SurveyDate,Direction,Lane FROM Survey WHERE SurveyName IN ({paramNames});";
            using var cmd = new SqliteCommand(query, connection);
            for (int i = 0; i < SurveyNames.Count; i++)
            {
                cmd.Parameters.AddWithValue($"@SurveyName{i}", SurveyNames[i]);
            }
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                int id = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                decimal startChainage = reader.IsDBNull(1) ? 0 : reader.GetDecimal(1);
                decimal endChainage = reader.IsDBNull(2) ? 0 : reader.GetDecimal(2);
                string surveyIdExternal = reader.IsDBNull(3) ? null : reader.GetString(3);
                string surveyName = reader.IsDBNull(4) ? null : reader.GetString(4);
                string surveyor = reader.IsDBNull(5) ? null : reader.GetString(5);
                string surveyDate = reader.IsDBNull(6) ? null : reader.GetString(6);
                string sDirection = reader.IsDBNull(7) ? null : reader.GetString(7);
                string sLane = reader.IsDBNull(8) ? "-" : reader.GetInt32(8).ToString();

                if (!string.IsNullOrEmpty(surveyIdExternal))
                {
                    ranges.Add((id, startChainage, endChainage, surveyIdExternal, surveyName, surveyor, surveyDate, sDirection,sLane));
                }
            }
            return ranges;
        }

        private async Task<string?> GetLastChainageTime(SqliteConnection connection, int surveyId)
        {
            string query = @"
        SELECT strftime('%Y-%m-%d %H:%M:%f', Time, 'unixepoch') AS FormattedTime
        FROM GPS_Processed
        WHERE SurveyId = @Id
        ORDER BY Chainage DESC
        LIMIT 1;
    ";

            using var cmd = new SqliteCommand(query, connection);
            cmd.Parameters.AddWithValue("@Id", surveyId);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync() && !reader.IsDBNull(0))
            {
                return reader.GetString(0); // FormattedTime as string
            }

            return null;
        }

        // Generic method to execute any query returning Chainage and ImageFile (filename or filepath)
        private async Task<Dictionary<decimal, (string fileName, string sectionId)>> GetImageFilesByCommand(SqliteCommand cmd)
        {
            var dict = new Dictionary<decimal, (string fileName, string sectionId)>();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                if (!reader.IsDBNull(0) && !reader.IsDBNull(1) && !reader.IsDBNull(2))
                {
                    var chainage = reader.GetDecimal(0);
                    var fileName = reader.GetString(1);
                    var sectionId = reader.GetString(2);
                    dict[chainage] = (fileName, sectionId);
                }
            }
            return dict;
        }

        private async Task<Dictionary<decimal, string>> GetVideoFilesByCommand(SqliteCommand cmd)
        {
            var dict = new Dictionary<decimal, string>();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                if (!reader.IsDBNull(0) && !reader.IsDBNull(1))
                {
                    var chainage = reader.GetDecimal(0);
                    var fileName = reader.GetString(1);
                    dict[chainage] = fileName;
                }
            }
            return dict;
        }

        private async Task<Dictionary<decimal, decimal>> GetDecimalValuesByCommand(SqliteCommand cmd)
        {
            var dict = new Dictionary<decimal, decimal>();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                if (!reader.IsDBNull(0) && !reader.IsDBNull(1))
                {
                    var chainage = reader.GetDecimal(0);
                    var value = reader.GetDecimal(1);
                    dict[chainage] = value;
                }
            }
            return dict;
        }

        private async Task<Dictionary<(string SurveyId, decimal Chainage), (double LinearLengthSum, double ArealAreaSum)>> GetCrackSummaryData(SqliteConnection connection, string surveyIdExternal, decimal startChainage, decimal endChainage)
        {
            var result = new Dictionary<(string, decimal), (double, double)>();
            string query = @"
        SELECT 
            SurveyId, 
            Chainage,
            MTQ,
            CrackLength_mm,
            minX, minY, maxX, maxY
        FROM LCMS_CrackSummary 
        WHERE SurveyId = @SurveyId 
          AND Chainage >= @StartChainage 
          AND Chainage <= @EndChainage;";
            using var cmd = new SqliteCommand(query, connection);
            cmd.Parameters.AddWithValue("@SurveyId", surveyIdExternal);
            cmd.Parameters.AddWithValue("@StartChainage", startChainage);
            cmd.Parameters.AddWithValue("@EndChainage", endChainage);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                string surveyId = reader.IsDBNull(0) ? null : reader.GetString(0);
                decimal chainage = reader.IsDBNull(1) ? 0 : reader.GetDecimal(1);
                decimal truncatedChainage = Math.Floor(chainage);
                var key = (surveyId, truncatedChainage);
                string mtq = reader.IsDBNull(2) ? null : reader.GetString(2);
                double crackLength = reader.IsDBNull(3) ? 0.0 : reader.GetDouble(3);
                double minX = reader.IsDBNull(4) ? 0.0 : reader.GetDouble(4);
                double minY = reader.IsDBNull(5) ? 0.0 : reader.GetDouble(5);
                double maxX = reader.IsDBNull(6) ? 0.0 : reader.GetDouble(6);
                double maxY = reader.IsDBNull(7) ? 0.0 : reader.GetDouble(7);

                // Initialize sums if not exists
                if (!result.ContainsKey(key))
                {
                    result[key] = (0.0, 0.0);
                }
                if (!string.IsNullOrEmpty(mtq))
                {
                    if (mtq.Equals("Transversal", StringComparison.OrdinalIgnoreCase) || mtq.Equals("Longitudinal", StringComparison.OrdinalIgnoreCase))
                    {
                        // Linear crack length sum
                        result[key] = (result[key].Item1 + crackLength, result[key].Item2);
                    }
                    else if (mtq.Equals("Alligator", StringComparison.OrdinalIgnoreCase))
                    {
                        // Calculate area in square meters; divide by 1,000,000 to convert mm² to m²
                        double area = ((maxY - minY) * (maxX - minX)) / 1000000.0;
                        if (area < 0) area = 0; // just in case
                        result[key] = (result[key].Item1, result[key].Item2 + area);
                    }
                }
            }
            return result;
        }

        private async Task<Dictionary<(decimal Chainage, string MTQ, string PavementType, string Severity), double>> GetGroupedCrackAreasByMTQs(
    SqliteConnection connection,
    string surveyIdExternal,
    decimal startChainage,
    decimal endChainage,
    IEnumerable<string> mtqConditions)  // Accept multiple MTQ values
        {
            var result = new Dictionary<(decimal, string, string, string), double>();
            // Build parameter placeholders for the IN clause
            var mtqParams = mtqConditions.Select((value, index) => $"@mtqParam{index}").ToList();
            string inClause = string.Join(",", mtqParams);
            string query = $@"
    SELECT 
        Chainage,
        MTQ,
        PavementType,
        Severity,
        minX,
        minY,
        maxX,
        maxY
    FROM LCMS_CrackSummary
    WHERE SurveyId = @SurveyId
      AND Chainage >= @StartChainage
      AND Chainage <= @EndChainage
      AND MTQ IN ({inClause});";
            using var cmd = new SqliteCommand(query, connection);
            cmd.Parameters.AddWithValue("@SurveyId", surveyIdExternal);
            cmd.Parameters.AddWithValue("@StartChainage", startChainage);
            cmd.Parameters.AddWithValue("@EndChainage", endChainage);
            // Add MTQ parameters
            int i = 0;
            foreach (var mtq in mtqConditions)
            {
                cmd.Parameters.AddWithValue(mtqParams[i], mtq);
                i++;
            }
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                if (reader.IsDBNull(0) || reader.IsDBNull(1) || reader.IsDBNull(2) || reader.IsDBNull(3)
                    || reader.IsDBNull(4) || reader.IsDBNull(5) || reader.IsDBNull(6) || reader.IsDBNull(7))
                    continue;
                decimal chainage = reader.GetDecimal(0);
                decimal truncatedChainage = Math.Floor(chainage);
                string mtq = reader.GetString(1);
                string pavementType = reader.GetString(2);
                string severity = reader.GetString(3);
                double minX = reader.GetDouble(4);
                double minY = reader.GetDouble(5);
                double maxX = reader.GetDouble(6);
                double maxY = reader.GetDouble(7);
                double area = ((maxY - minY) * (maxX - minX)) / 1000000.0; // mm² to m²
                if (area < 0)
                    area = 0;
                var key = (truncatedChainage, mtq, pavementType, severity);
                if (result.ContainsKey(key))
                    result[key] += area;
                else
                    result[key] = area;
            }
            return result;
        }

        private async Task<Dictionary<(decimal Chainage, string MTQ, string PavementType, string Severity), double>> GetGroupedCrackLengthSums(
    SqliteConnection connection, string surveyIdExternal, decimal startChainage, decimal endChainage)
        {
            var result = new Dictionary<(decimal, string, string, string), double>();
            string query = @"
        SELECT 
            Chainage,
            MTQ,
            PavementType,
            Severity,
            SUM(CrackLength_mm) AS TotalCrackLength
        FROM LCMS_CrackSummary
        WHERE SurveyId = @SurveyId
          AND Chainage >= @StartChainage
          AND Chainage <= @EndChainage
        GROUP BY Chainage, MTQ, PavementType, Severity;";
            using var cmd = new SqliteCommand(query, connection);
            cmd.Parameters.AddWithValue("@SurveyId", surveyIdExternal);
            cmd.Parameters.AddWithValue("@StartChainage", startChainage);
            cmd.Parameters.AddWithValue("@EndChainage", endChainage);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                if (reader.IsDBNull(0) || reader.IsDBNull(1) || reader.IsDBNull(2) || reader.IsDBNull(3) || reader.IsDBNull(4))
                    continue;
                decimal chainage = reader.GetDecimal(0);
                decimal truncatedChainage = Math.Floor(chainage);
                string mtq = reader.GetString(1);
                string pavementType = reader.GetString(2);
                string severity = reader.GetString(3);
                double totalCrackLength = reader.GetDouble(4);
                var key = (truncatedChainage, mtq, pavementType, severity);
                result[key] = totalCrackLength;
            }
            return result;
        }

        private async Task<Dictionary<(decimal Chainage, string PavementType, string Severity), double>> GetGroupedPatchAreaSums(
    SqliteConnection connection, string surveyIdExternal, decimal startChainage, decimal endChainage)
        {
            var result = new Dictionary<(decimal, string, string), double>();
            string query = @"
        SELECT 
            Chainage,
            PavementType,
            Severity,
            SUM(Area_m2) AS TotalArea
        FROM LCMS_Patch_Processed
        WHERE SurveyId = @SurveyId
          AND Chainage >= @StartChainage
          AND Chainage <= @EndChainage
        GROUP BY Chainage, PavementType, Severity;";
            using var cmd = new SqliteCommand(query, connection);
            cmd.Parameters.AddWithValue("@SurveyId", surveyIdExternal);
            cmd.Parameters.AddWithValue("@StartChainage", startChainage);
            cmd.Parameters.AddWithValue("@EndChainage", endChainage);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                if (reader.IsDBNull(0) || reader.IsDBNull(1) || reader.IsDBNull(2) || reader.IsDBNull(3))
                    continue;
                decimal chainage = reader.GetDecimal(0);
                decimal truncatedChainage = Math.Floor(chainage);
                string pavementType = reader.GetString(1);
                string severity = reader.GetString(2);
                double totalArea = reader.GetDouble(3);
                var key = (truncatedChainage, pavementType, severity);
                result[key] = totalArea;
            }
            return result;
        }

        private async Task<Dictionary<(decimal Chainage, string PavementType, string Severity), double>> GetGroupedBleedingAreaSums(
    SqliteConnection connection, string surveyIdExternal, decimal startChainage, decimal endChainage)
        {
            var result = new Dictionary<(decimal, string, string), double>();
            string query = @"
        SELECT 
            Chainage,
            PavementType,
            LeftSeverity,
            LeftArea_m2,
            RightSeverity,
            RightArea_m2
        FROM LCMS_Bleeding
        WHERE SurveyId = @SurveyId
          AND Chainage >= @StartChainage
          AND Chainage <= @EndChainage;";
            using var cmd = new SqliteCommand(query, connection);
            cmd.Parameters.AddWithValue("@SurveyId", surveyIdExternal);
            cmd.Parameters.AddWithValue("@StartChainage", startChainage);
            cmd.Parameters.AddWithValue("@EndChainage", endChainage);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                if (reader.IsDBNull(0) || reader.IsDBNull(1))
                    continue;
                decimal chainage = reader.GetDecimal(0);
                decimal truncatedChainage = Math.Floor(chainage);
                string pavementType = reader.GetString(1);
                // Process Left severity and area
                string leftSeverity = reader.IsDBNull(2) ? null : reader.GetString(2);
                double leftArea = reader.IsDBNull(3) ? 0.0 : reader.GetDouble(3);
                if (!string.IsNullOrEmpty(leftSeverity) && leftArea > 0)
                {
                    var key = (truncatedChainage, pavementType, leftSeverity);
                    if (result.ContainsKey(key))
                        result[key] += leftArea;
                    else
                        result[key] = leftArea;
                }
                // Process Right severity and area
                string rightSeverity = reader.IsDBNull(4) ? null : reader.GetString(4);
                double rightArea = reader.IsDBNull(5) ? 0.0 : reader.GetDouble(5);
                if (!string.IsNullOrEmpty(rightSeverity) && rightArea > 0)
                {
                    var key = (truncatedChainage, pavementType, rightSeverity);
                    if (result.ContainsKey(key))
                        result[key] += rightArea;
                    else
                        result[key] = rightArea;
                }
            }
            return result;
        }

        private async Task<Dictionary<(decimal Chainage, string PavementType, string Severity), double>> GetGroupedPotholeAreaSums(
    SqliteConnection connection, string surveyIdExternal, decimal startChainage, decimal endChainage)
        {
            const double Mm2ToM2 = 1_000_000.0;

            var result = new Dictionary<(decimal, string, string), double>();
            string query = @"
        SELECT
            Chainage,
            PavementType,
            Severity,
            SUM(Area_mm2) AS TotalArea
        FROM LCMS_Potholes_Processed
        WHERE SurveyId = @SurveyId
          AND Chainage >= @StartChainage
          AND Chainage <= @EndChainage
        GROUP BY Chainage, PavementType, Severity;";
            using var cmd = new SqliteCommand(query, connection);
            cmd.Parameters.AddWithValue("@SurveyId", surveyIdExternal);
            cmd.Parameters.AddWithValue("@StartChainage", startChainage);
            cmd.Parameters.AddWithValue("@EndChainage", endChainage);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                if (reader.IsDBNull(0) || reader.IsDBNull(1) || reader.IsDBNull(2) || reader.IsDBNull(3))
                    continue;
                decimal chainage = reader.GetDecimal(0);
                decimal truncatedChainage = Math.Floor(chainage);
                string pavementType = reader.GetString(1);
                string severity = reader.GetString(2);
                double totalArea = reader.GetDouble(3) / Mm2ToM2; // Convert mm² to m²;
                var key = (truncatedChainage, pavementType, severity);
                result[key] = totalArea;
            }
            return result;
        }
        private Dictionary<(string SurveyId, decimal Chainage), (double LinearLengthSum, double ArealAreaSum)> MergePatchPotholeAreasIntoCrackSummary(
    Dictionary<(string SurveyId, decimal Chainage), (double LinearLengthSum, double ArealAreaSum)> crackSummary,
    Dictionary<(decimal Chainage, string PavementType, string Severity), double> patchAreas,
    Dictionary<(decimal Chainage, string PavementType, string Severity), double> potholeAreas,
    string surveyIdExternal)
        {
            // Sum patch and pothole areas by truncated chainage
            var areaSumsByChainage = new Dictionary<decimal, double>();
            void AddAreaSum(decimal chainage, double area)
            {
                if (areaSumsByChainage.ContainsKey(chainage))
                    areaSumsByChainage[chainage] += area;
                else
                    areaSumsByChainage[chainage] = area;
            }
            foreach (var kvp in patchAreas)
            {
                AddAreaSum(kvp.Key.Chainage, kvp.Value);
            }
            foreach (var kvp in potholeAreas)
            {
                AddAreaSum(kvp.Key.Chainage, kvp.Value);
            }
            // Add patch + pothole area sums to crackSummary where keys exist
            foreach (var key in crackSummary.Keys.ToList())
            {
                decimal chainage = key.Chainage;
                if (areaSumsByChainage.TryGetValue(chainage, out double extraArea))
                {
                    var current = crackSummary[key];
                    crackSummary[key] = (current.LinearLengthSum, current.ArealAreaSum + extraArea);
                    areaSumsByChainage.Remove(chainage);
                }
            }
            // Add any remaining chainages from patch/pothole areas into crackSummary if missing
            foreach (var kvp in areaSumsByChainage)
            {
                var combinedKey = (surveyIdExternal, kvp.Key);
                if (!crackSummary.ContainsKey(combinedKey))
                {
                    crackSummary[combinedKey] = (0.0, kvp.Value); // linear length = 0, area = patch+pothole sum
                }
            }
            return crackSummary;
        }
        private async Task<Dictionary<decimal, decimal>> GetSectionLengths(SqliteConnection connection, string surveyIdExternal, decimal startChainage, decimal endChainage)
        {
            var dict = new Dictionary<decimal, decimal>();
            string query = @"
        SELECT Chainage, Height 
        FROM LCMS_segment 
        WHERE SurveyId = @SurveyId 
          AND Chainage >= @StartChainage 
          AND Chainage <= @EndChainage;";
            using var cmd = new SqliteCommand(query, connection);
            cmd.Parameters.AddWithValue("@SurveyId", surveyIdExternal);
            cmd.Parameters.AddWithValue("@StartChainage", startChainage);
            cmd.Parameters.AddWithValue("@EndChainage", endChainage);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                if (!reader.IsDBNull(0) && !reader.IsDBNull(1))
                {
                    var chainage = reader.GetDecimal(0);
                    var height = reader.GetDecimal(1) / 1000m;   // mm->m
                    dict[chainage] = height;
                }
            }
            return dict;
        }
        private async Task<Dictionary<decimal, decimal>> GetLaneWidths(SqliteConnection connection, string surveyIdExternal, decimal startChainage, decimal endChainage)
        {
            var dict = new Dictionary<decimal, decimal>();
            string query = @"
        SELECT Chainage, LaneWidth
        FROM LCMS_Lane_Mark_Processed
        WHERE SurveyId = @SurveyId
          AND Chainage >= @StartChainage
          AND Chainage <= @EndChainage;";
            using var cmd = new SqliteCommand(query, connection);
            cmd.Parameters.AddWithValue("@SurveyId", surveyIdExternal);
            cmd.Parameters.AddWithValue("@StartChainage", startChainage);
            cmd.Parameters.AddWithValue("@EndChainage", endChainage);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                if (!reader.IsDBNull(0) && !reader.IsDBNull(1))
                {
                    var chainage = reader.GetDecimal(0);
                    var laneWidth = reader.GetDecimal(1) / 1000m;  // mm->m
                    dict[chainage] = laneWidth;
                }
            }
            return dict;
        }

        public void GenerateDefectChart(string filePath, int startRow, int currentRow3, int currentRow4, int currentRow5)
        {
            ExcelPackage.License.SetNonCommercialPersonal("My Name");

            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                var workbook = package.Workbook;

                // Chart 1: 1-meter interval
                AddDefectChart(workbook, workbook.Worksheets[2], startRow, currentRow3, "Main Defects Chart-1M");

                // Chart 2: 10-meter interval
                AddDefectChart(workbook, workbook.Worksheets[3], startRow, currentRow4, "Main Defects Chart-10M");

                // Chart 3: 20-meter interval
                AddDefectChart(workbook, workbook.Worksheets[4], startRow, currentRow5, "Main Defects Chart-20M");
                package.Save(); // Overwrites the original file
            }
        }

        private void AddDefectChart(ExcelWorkbook workbook, ExcelWorksheet sourceSheet, int startRow, int endRow, string chartSheetName)
        {
            var chartSheet = workbook.Worksheets.Add(chartSheetName);

            var chart = chartSheet.Drawings.AddChart(chartSheetName + "-Chart", eChartType.XYScatter);
            chart.Title.Text = "ROMDAS Data Chart";
            chart.SetPosition(1, 0, 1, 0);
            chart.SetSize(900, 500);

            // Chainage is column A (1)
            // Cracking is column H (8)
            // Rutting is column D (4)
            // IRI is column E (5)
            // Pothole L/M/H: AB (28), AC (29), AD (30)
            // Patch L/M/H: Y (25), Z (26), AA (27)

            chart.Series.Add(sourceSheet.Cells[startRow, 8, endRow, 8], sourceSheet.Cells[startRow, 1, endRow, 1]).Header = "Cracking";
            chart.Series.Add(sourceSheet.Cells[startRow, 4, endRow, 4], sourceSheet.Cells[startRow, 1, endRow, 1]).Header = "Rutting";
            chart.Series.Add(sourceSheet.Cells[startRow, 5, endRow, 5], sourceSheet.Cells[startRow, 1, endRow, 1]).Header = "IRI";

            chart.Series.Add(sourceSheet.Cells[startRow, 28, endRow, 28], sourceSheet.Cells[startRow, 1, endRow, 1]).Header = "Pothole L";
            chart.Series.Add(sourceSheet.Cells[startRow, 29, endRow, 29], sourceSheet.Cells[startRow, 1, endRow, 1]).Header = "Pothole M";
            chart.Series.Add(sourceSheet.Cells[startRow, 30, endRow, 30], sourceSheet.Cells[startRow, 1, endRow, 1]).Header = "Pothole H";

            chart.Series.Add(sourceSheet.Cells[startRow, 25, endRow, 25], sourceSheet.Cells[startRow, 1, endRow, 1]).Header = "Patch L";
            chart.Series.Add(sourceSheet.Cells[startRow, 26, endRow, 26], sourceSheet.Cells[startRow, 1, endRow, 1]).Header = "Patch M";
            chart.Series.Add(sourceSheet.Cells[startRow, 27, endRow, 27], sourceSheet.Cells[startRow, 1, endRow, 1]).Header = "Patch H";
        }

        public void GenerateOtherDefectCharts(string filePath, int startRow, int currentRow3, int currentRow4, int currentRow5)
        {
            ExcelPackage.License.SetNonCommercialPersonal("My Name");

            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                var workbook = package.Workbook;

                // Chart 1: 1-meter interval
                AddOtherDefectChart(workbook, workbook.Worksheets[2], startRow, currentRow3, "Other Defects Chart-1M");

                // Chart 2: 10-meter interval
                AddOtherDefectChart(workbook, workbook.Worksheets[3], startRow, currentRow4, "Other Defects Chart-10M");

                // Chart 3: 20-meter interval
                AddOtherDefectChart(workbook, workbook.Worksheets[4], startRow, currentRow5, "Other Defects Chart-20M");

                package.Save(); // Overwrites the original file
            }
        }

        private void AddOtherDefectChart(ExcelWorkbook workbook, ExcelWorksheet sourceSheet, int startRow, int endRow, string chartSheetName)
        {
            var chartSheet = workbook.Worksheets.Add(chartSheetName);

            var chart = chartSheet.Drawings.AddChart(chartSheetName + "-Chart", eChartType.XYScatter);
            chart.Title.Text = "Other Defects by Chainage";
            chart.SetPosition(1, 0, 1, 0);
            chart.SetSize(900, 500);

            // Chainage: Column A (1)
            // NHPCI: Column BC (55)
            // MPCI: Column BD (56)
            // HPCI: BA (53) if isAsphalt, else BB (54)
            // Bleeding L/M/H: AN (40), AO (41), AP (42)

            chart.Series.Add(sourceSheet.Cells[startRow, 55, endRow, 55], sourceSheet.Cells[startRow, 1, endRow, 1]).Header = "NHPCI";
            chart.Series.Add(sourceSheet.Cells[startRow, 56, endRow, 56], sourceSheet.Cells[startRow, 1, endRow, 1]).Header = "MPCI";

            chart.Series.Add(sourceSheet.Cells[startRow, 53, endRow, 53], sourceSheet.Cells[startRow, 1, endRow, 1]).Header = "HPCI (Asphalt)";
            chart.Series.Add(sourceSheet.Cells[startRow, 54, endRow, 54], sourceSheet.Cells[startRow, 1, endRow, 1]).Header = "HPCI (Concrete)";

            chart.Series.Add(sourceSheet.Cells[startRow, 40, endRow, 40], sourceSheet.Cells[startRow, 1, endRow, 1]).Header = "Bleeding L";
            chart.Series.Add(sourceSheet.Cells[startRow, 41, endRow, 41], sourceSheet.Cells[startRow, 1, endRow, 1]).Header = "Bleeding M";
            chart.Series.Add(sourceSheet.Cells[startRow, 42, endRow, 42], sourceSheet.Cells[startRow, 1, endRow, 1]).Header = "Bleeding H";
        }

        private Task<string> GenerateHongkongReport(GenerateReportObjRequest request)
        {
            // Implement the actual report generation logic here.
            throw new NotImplementedException();
        }
    }
}
