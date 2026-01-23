using DataView2.Core.Helper;
using DataView2.Core.Models;
using DataView2.Core.Models.CrackClassification;
using DataView2.Core.Models.LCMS_Data_Tables;
using DataView2.Core.Models.Other;
using DataView2.Core.Models.Setting;
using DataView2.GrpcService.Data;
using DataView2.GrpcService.Helpers;
using DataView2.GrpcService.Protos;
using DataView2.GrpcService.Services.LCMS_Data_Services;
using DataView2.GrpcService.Services.OtherServices;
using DataView2.GrpcService.Services.Setting_Services;
using Esri.ArcGISRuntime.Tasks.NetworkAnalysis;
using Grpc.Core;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Maui.Storage;
using Newtonsoft.Json.Linq;
using ProtoBuf.Grpc;
using Serilog;
using System.Collections.Concurrent;
using System.Drawing;
using System.Dynamic;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using Windows.UI.Popups;
using static DataView2.Core.Helper.TableNameHelper;
using static DataView2.Core.Helper.XMLParser;
using static DataView2.Core.Models.ExportTemplate.ExportPCIToXml;
using static DataView2.GrpcService.Helpers.MTQ_Classification;
using static DataView2.GrpcService.Services.ProcessingServices.GeneralService;
using static DataView2.GrpcService.SurveySections;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static System.Collections.Specialized.BitVector32;

namespace DataView2.GrpcService.Services
{
    public class XmlParsingService
    {
        private readonly AppDbContextProjectData _appDbContext;
        private readonly SurveyService _surveyService;
        private readonly BoundariesService _boundariesService;
        private readonly CrackClassificationService _crackClassificationService;
        private readonly ImageBandInfoService _imageBandInfoService;
        private readonly SegmentService _segmentService;

        private int overallProgress;
        private gpsInfo lastSegmentCoordinate = new gpsInfo();
        //Segment Grid:
        private static bool _IsGridSegment = false;

        public int _xmlFiles;
        private int _processedxmlFiles;
        public List<string> detailLogViewHelpers = new List<string>();
        //public List<string> ProcessedSurveyIds = new List<string>();
 
        public XmlParsingService(AppDbContextProjectData context,
                                SurveyService surveyService,
                                BoundariesService boundariesService,
                                CrackClassificationService crackClassificationService,
                                ImageBandInfoService imageBandInfoService,
                                SegmentService segmentService)
        {
            _appDbContext = context;
            _surveyService = surveyService;
            _boundariesService = boundariesService;
            _crackClassificationService = crackClassificationService;
            _imageBandInfoService = imageBandInfoService;
            _segmentService = segmentService;
        }

        //gather data from the results
        ConcurrentBag<LCMS_Segment> newsegments = new ConcurrentBag<LCMS_Segment>();
        ConcurrentBag<LCMS_Segment> existingSegments = new ConcurrentBag<LCMS_Segment>();
        ConcurrentBag<LCMS_PickOuts_Raw> newpickOuts_Raws = new ConcurrentBag<LCMS_PickOuts_Raw>();
        ConcurrentBag<LCMS_Cracking_Raw> newcracking_Raws = new ConcurrentBag<LCMS_Cracking_Raw>();
        ConcurrentBag<LCMS_Ravelling_Raw> newravelling_Raws = new ConcurrentBag<LCMS_Ravelling_Raw>();
        ConcurrentBag<LCMS_Potholes_Processed> newpotholes_Processed = new ConcurrentBag<LCMS_Potholes_Processed>();
        ConcurrentBag<LCMS_Patch_Processed> newpatch_Processed = new ConcurrentBag<LCMS_Patch_Processed>();
        ConcurrentBag<LCMS_Corner_Break> newcorner_Breaks = new ConcurrentBag<LCMS_Corner_Break>();
        ConcurrentBag<LCMS_Spalling_Raw> newspalling_Raws = new ConcurrentBag<LCMS_Spalling_Raw>();
        ConcurrentBag<LCMS_Concrete_Joints> newconcrete_Joints = new ConcurrentBag<LCMS_Concrete_Joints>();
        ConcurrentBag<LCMS_Bleeding> newBleeding = new ConcurrentBag<LCMS_Bleeding>();
        ConcurrentBag<LCMS_Rut_Processed> newRut = new ConcurrentBag<LCMS_Rut_Processed>();
        ConcurrentBag<LCMS_Lane_Mark_Processed> newLaneMark = new ConcurrentBag<LCMS_Lane_Mark_Processed>();
        ConcurrentBag<LCMS_Pumping_Processed> newPumping = new ConcurrentBag<LCMS_Pumping_Processed>();
        ConcurrentBag<LCMS_Curb_DropOff> newCurbDropoff = new ConcurrentBag<LCMS_Curb_DropOff>();
        ConcurrentBag<LCMS_Sealed_Cracks> newSealedCrack = new ConcurrentBag<LCMS_Sealed_Cracks>();
        ConcurrentBag<LCMS_Water_Entrapment> newWaterTrapment = new ConcurrentBag<LCMS_Water_Entrapment>();
        ConcurrentBag<LCMS_Marking_Contour> newMarkingContour = new ConcurrentBag<LCMS_Marking_Contour>();
        ConcurrentBag<LCMS_Texture_Processed> newTexture = new ConcurrentBag<LCMS_Texture_Processed>();
        ConcurrentBag<LCMS_MMO_Processed> newMMO = new ConcurrentBag<LCMS_MMO_Processed>();
        ConcurrentBag<LCMS_Rumble_Strip> newRumbleStrip = new ConcurrentBag<LCMS_Rumble_Strip>();
        ConcurrentBag<LCMS_Rough_Processed> newRoughness = new ConcurrentBag<LCMS_Rough_Processed>();
        ConcurrentBag<LCMS_Geometry_Processed> newGeomtry = new ConcurrentBag<LCMS_Geometry_Processed>();
        ConcurrentBag<LCMS_Shove_Processed> newShove = new ConcurrentBag<LCMS_Shove_Processed>();
        ConcurrentBag<LCMS_Segment_Grid> newSegment_Grid = new ConcurrentBag<LCMS_Segment_Grid>();
        ConcurrentBag<LCMS_Grooves> newGrooves = new ConcurrentBag<LCMS_Grooves>();
        ConcurrentBag<LCMS_Sags_Bumps> newSagsBumps = new ConcurrentBag<LCMS_Sags_Bumps>();
        ConcurrentBag<LCMS_PCI> newPci = new ConcurrentBag<LCMS_PCI>();
        ConcurrentBag<LCMS_PASER> newPASER = new ConcurrentBag<LCMS_PASER>();
        ConcurrentBag<LCMS_CrackSummary> newCrackSummary = new ConcurrentBag<LCMS_CrackSummary>();


        public async Task ProcessXMLFromMain(SurveyProcessingRequest request, SafeSurveyWriter<SurveyProcessingResponse> safeWriter, List<string> xmlFiles, ServerCallContext context, Action<int, string> reportProgress, double offsetX, double offsetY)
        {
            try
            {
                float availableMemory = 0;
                int chunkSize = 15;
                MEMORYSTATUSEX memoryStatus = new MEMORYSTATUSEX();
                if (GlobalMemoryStatusEx(memoryStatus))
                {
                    availableMemory = (float)(memoryStatus.ullAvailPhys / (1024.0 * 1024.0 * 1024.0)); // Available physical memory in bytes
                    Log.Warning($"Available memory: {availableMemory} GBs");
                }
                else
                {
                    Log.Warning("Failed to retrieve memory status.");
                }

                var cancellationToken = context.CancellationToken;
                var VideoPath = request.VideoPath;
                var LcmsObjects = request.ProcessingObjects.ToList();
                string DataviewVersion = request.DataviewVersion;

                DateTime startTimeDB = DateTime.Now;

                if (xmlFiles.Count > 0)
                {
                    //Store Survey Info with the first xml
                    Log.Information($"Before checking Survey Info only for first Batch");
                    var firstXml = xmlFiles.FirstOrDefault();
                    var lastXml = xmlFiles.LastOrDefault();
                    SaveSurveyInfo(firstXml, lastXml, DataviewVersion);
                    Log.Information($"After Survey Info ");

                    int batchSize = (int)Math.Round(availableMemory) != 0 ? (int)Math.Round(availableMemory) * chunkSize : chunkSize, batchCount = (int)Math.Ceiling((double)xmlFiles.Count / batchSize);
                    Log.Information($"XML File count : {xmlFiles.Count}, Batch size : {batchSize},  BatchCount : {batchCount}");
                    bool returnCoordinates = false;
                    int totalTasks = 6;

                    for (int i = 0; i < batchCount; i++)
                    {
                        int tasks = 1;
                        XmlFilesRequest xmlFileRequest = new XmlFilesRequest
                        {
                            XmlFilesPath = xmlFiles.Skip(i * batchSize).Take(batchSize).ToList(),
                            HorizontalOffset = offsetX,
                            VerticalOffset = offsetY,
                        };
                        int batchFileCount = xmlFileRequest.XmlFilesPath.Count();

                        reportProgress(ReportXmlProgress(i, batchCount, tasks, totalTasks), $"Batch {i + 1}: Starting batch");
                        tasks++;

                        //create tasks for reading xml
                        IEnumerable<LCMS_Segment> segments = Enumerable.Empty<LCMS_Segment>();
                        IEnumerable<LCMS_PickOuts_Raw> pickOuts_Raws = Enumerable.Empty<LCMS_PickOuts_Raw>();
                        IEnumerable<LCMS_Cracking_Raw> cracking_Raws = Enumerable.Empty<LCMS_Cracking_Raw>();
                        IEnumerable<LCMS_Ravelling_Raw> ravelling_Raws = Enumerable.Empty<LCMS_Ravelling_Raw>();
                        IEnumerable<LCMS_Potholes_Processed> potholes_Processed = Enumerable.Empty<LCMS_Potholes_Processed>();
                        IEnumerable<LCMS_Patch_Processed> patch_Processed = Enumerable.Empty<LCMS_Patch_Processed>();
                        IEnumerable<LCMS_Corner_Break> corner_Breaks = Enumerable.Empty<LCMS_Corner_Break>();
                        IEnumerable<LCMS_Spalling_Raw> spalling_Raws = Enumerable.Empty<LCMS_Spalling_Raw>();
                        IEnumerable<LCMS_Concrete_Joints> concrete_Joints = Enumerable.Empty<LCMS_Concrete_Joints>();
                        IEnumerable<LCMS_Rut_Processed> rut_Processed = Enumerable.Empty<LCMS_Rut_Processed>();
                        IEnumerable<LCMS_Bleeding> bleeding = Enumerable.Empty<LCMS_Bleeding>();
                        IEnumerable<LCMS_Lane_Mark_Processed> laneMark = Enumerable.Empty<LCMS_Lane_Mark_Processed>();
                        IEnumerable<LCMS_Pumping_Processed> pumping = Enumerable.Empty<LCMS_Pumping_Processed>();
                        IEnumerable<LCMS_Curb_DropOff> curb_Dropoff = Enumerable.Empty<LCMS_Curb_DropOff>();
                        IEnumerable<LCMS_Sealed_Cracks> sealedCrack = Enumerable.Empty<LCMS_Sealed_Cracks>();
                        IEnumerable<LCMS_Water_Entrapment> waterTrapment = Enumerable.Empty<LCMS_Water_Entrapment>();
                        IEnumerable<LCMS_Marking_Contour> markingContour = Enumerable.Empty<LCMS_Marking_Contour>();
                        IEnumerable<LCMS_Texture_Processed> macroTexture = Enumerable.Empty<LCMS_Texture_Processed>();
                        IEnumerable<LCMS_MMO_Processed> mmo = Enumerable.Empty<LCMS_MMO_Processed>();
                        IEnumerable<LCMS_Rumble_Strip> rumbleStrip = Enumerable.Empty<LCMS_Rumble_Strip>();
                        IEnumerable<LCMS_Geometry_Processed> geomerties = Enumerable.Empty<LCMS_Geometry_Processed>();
                        IEnumerable<LCMS_Shove_Processed> shove = Enumerable.Empty<LCMS_Shove_Processed>();
                        IEnumerable<LCMS_Grooves> grooves = Enumerable.Empty<LCMS_Grooves>();
                        IEnumerable<LCMS_Segment_Grid> segment_grid = Enumerable.Empty<LCMS_Segment_Grid>();
                        IEnumerable<LCMS_Sags_Bumps> sagsBumps = Enumerable.Empty<LCMS_Sags_Bumps>();
                        IEnumerable<LCMS_Rough_Processed> roughness = Enumerable.Empty<LCMS_Rough_Processed>();
                        IEnumerable<LCMS_PCI> pci = Enumerable.Empty<LCMS_PCI>();
                        IEnumerable<LCMS_PASER> paser = Enumerable.Empty<LCMS_PASER>();
                        IEnumerable<LCMS_CrackSummary> crackSummary = Enumerable.Empty<LCMS_CrackSummary>();

                        try
                        {
                            Log.Information($"New XML Analysis ");
                            await ParseLcmsAnalyser(xmlFileRequest, LcmsObjects, cancellationToken);
                            Log.Information($"After New XML Analysis ");
                            reportProgress(ReportXmlProgress(i, batchCount, tasks, totalTasks), $"Batch {i + 1}: XML Analysis.");
                            tasks++;
                        }
                        catch (Exception ex)
                        {
                            //failed batch files to process again when we are getting any error
                            Log.Warning($"Batch {i + 1} need to process again of {chunkSize} files : {ex.Message}");
                            return;
                        }

                        segments = newsegments;
                        pickOuts_Raws = newpickOuts_Raws;
                        cracking_Raws = newcracking_Raws;
                        ravelling_Raws = newravelling_Raws;
                        potholes_Processed = newpotholes_Processed;
                        patch_Processed = newpatch_Processed;
                        corner_Breaks = newcorner_Breaks;
                        spalling_Raws = newspalling_Raws;
                        concrete_Joints = newconcrete_Joints;
                        rut_Processed = newRut;
                        bleeding = newBleeding;
                        laneMark = newLaneMark;
                        pumping = newPumping;
                        curb_Dropoff = newCurbDropoff;
                        sealedCrack = newSealedCrack;
                        waterTrapment = newWaterTrapment;
                        markingContour = newMarkingContour;
                        macroTexture = newTexture;
                        mmo = newMMO;
                        rumbleStrip = newRumbleStrip;
                        geomerties = newGeomtry;
                        shove = newShove;
                        grooves = newGrooves;
                        segment_grid = newSegment_Grid;
                        sagsBumps = newSagsBumps;
                        roughness = newRoughness;
                        pci = newPci;
                        paser = newPASER;
                        crackSummary = newCrackSummary;

                        //overallProgress = default;
                        int completedTask = 0;

                        //database saving using raw query
                        //List<string> insertqueries = GeneralHelper.insertqueries;
                        //List<string> deletequeries = GeneralHelper.deletequeries;
                        List<Task> insertingDataQueries = new List<Task>();
                        DateTime startTimesql = DateTime.Now;

                        Log.Information($"Parsing XML Preparing Queries Batch{i + 1} ");

                        var insertingDataQueriesActions = new List<Action>
                        {
                            () => ProcessInsertQueriesAsync(segments, "LCMS_Segment", "Segments", completedTask, cancellationToken).Wait(),
                            () => ProcessInsertQueriesAsync(pickOuts_Raws, "LCMS_PickOuts_Raw", "Pickouts", completedTask, cancellationToken).Wait(),
                            () => ProcessInsertQueriesAsync(cracking_Raws, "LCMS_Cracking_Raw", "Cracking Raws", completedTask, cancellationToken).Wait(),
                            () => ProcessInsertQueriesAsync(ravelling_Raws, "LCMS_Ravelling_Raw", "Ravelling Raws", completedTask, cancellationToken).Wait(),
                            () => ProcessInsertQueriesAsync(potholes_Processed, "LCMS_Potholes_Processed", "Potholes", completedTask, cancellationToken).Wait(),
                            () => ProcessInsertQueriesAsync(patch_Processed, "LCMS_Patch_Processed", "Patches", completedTask, cancellationToken).Wait(),
                            () => ProcessInsertQueriesAsync(corner_Breaks, "LCMS_Corner_Break", "Corner Breaks", completedTask, cancellationToken).Wait(),
                            () => ProcessInsertQueriesAsync(spalling_Raws, "LCMS_Spalling_Raw", "Spalling Raws", completedTask, cancellationToken).Wait(),
                            () => ProcessInsertQueriesAsync(concrete_Joints, "LCMS_Concrete_Joints", "Concrete Joints", completedTask, cancellationToken).Wait(),
                            () => ProcessInsertQueriesAsync(rut_Processed, "LCMS_Rut_Processed", "Rutting", completedTask, cancellationToken).Wait(),
                            () => ProcessInsertQueriesAsync(bleeding, "LCMS_Bleeding", "Bleeding", completedTask, cancellationToken).Wait(),
                            () => ProcessInsertQueriesAsync(laneMark, "LCMS_Lane_Mark_Processed", "Lane Marking", completedTask, cancellationToken).Wait(),
                            () => ProcessInsertQueriesAsync(pumping, "LCMS_Pumping_Processed", "Pumping", completedTask, cancellationToken).Wait(),
                            () => ProcessInsertQueriesAsync(curb_Dropoff, "LCMS_Curb_DropOff", "Curb and DropOff", completedTask, cancellationToken).Wait(),
                            () => ProcessInsertQueriesAsync(sealedCrack, "LCMS_Sealed_Cracks", "Sealed Crack", completedTask, cancellationToken).Wait(),
                            () => ProcessInsertQueriesAsync(waterTrapment, "LCMS_Water_Entrapment", "Water Trapment", completedTask, cancellationToken).Wait(),
                            () => ProcessInsertQueriesAsync(markingContour, "LCMS_Marking_Contour", "Marking Contour", completedTask, cancellationToken).Wait(),
                            () => ProcessInsertQueriesAsync(macroTexture, "LCMS_Texture_Processed", "Macro Texture", completedTask, cancellationToken).Wait(),
                            () => ProcessInsertQueriesAsync(mmo, "LCMS_MMO_Processed", "MMO", completedTask, cancellationToken).Wait(),
                            () => ProcessInsertQueriesAsync(rumbleStrip, "LCMS_Rumble_Strip", "Rumble Strip", completedTask, cancellationToken).Wait(),
                            () => ProcessInsertQueriesAsync(shove, "LCMS_Shove_Processed", "Shove", completedTask, cancellationToken).Wait(),
                            () => ProcessInsertQueriesAsync(grooves, "LCMS_Grooves", "Groove", completedTask, cancellationToken).Wait(),
                            () => ProcessInsertQueriesAsync(roughness, "LCMS_Rough_Processed", "Roughness", completedTask, cancellationToken).Wait(),
                            () => ProcessInsertQueriesAsync(sagsBumps, "LCMS_Sags_Bumps", "Sags Bumps", completedTask, cancellationToken).Wait(),
                            () => ProcessInsertQueriesAsync(geomerties, "LCMS_Geometry_Processed", "Geometry", completedTask, cancellationToken).Wait(),
                            () => ProcessInsertQueriesAsync(segment_grid, "LCMS_Segment_Grid", "Segment Grid", completedTask, cancellationToken).Wait(),
                            () => ProcessInsertQueriesAsync(pci, "LCMS_PCI", "PCI", completedTask, cancellationToken).Wait(),
                            () => ProcessInsertQueriesAsync(paser, "LCMS_PASER", "PASER", completedTask, cancellationToken).Wait(),
                            () => ProcessInsertQueriesAsync(crackSummary, "LCMS_CrackSummary", "CrackSummary", completedTask, cancellationToken).Wait()
                        };

                        Log.Information($"Parsing XML Preparing Queries Batch{i + 1} ");
                        reportProgress(ReportXmlProgress(i, batchCount, tasks, totalTasks), $"Batch {i + 1}: Inserting data into database.");
                        tasks++;

                        Parallel.Invoke(new ParallelOptions { CancellationToken = cancellationToken }, insertingDataQueriesActions.ToArray());

                        Log.Information($"Batch{i + 1} Total Time(seconds) for SQL query creation : {Convert.ToInt64((DateTime.Now - startTimesql).TotalSeconds)}");

                        startTimesql = DateTime.Now;

                        //add exisitng segments too for segment summary/ overlay images
                        segments = newsegments.Concat(existingSegments);
                        var sortedSegments = segments.OrderBy(segment => segment.SectionId).ToList();

                        // Return coordinates for the map and chainage
                        if (!returnCoordinates)
                        {
                            // Sort segments by SectionId
                            var firstSegment = sortedSegments.FirstOrDefault();

                            if (firstSegment != null)
                            {
                                var surveyResponse = new SurveyProcessingResponse
                                {
                                    Longitude = firstSegment.GPSLongitude,
                                    Latitude = firstSegment.GPSLatitude,
                                };

                                Log.Information($"First Segment coordinate surveyResponse : Longitude  = {surveyResponse.Longitude} ... Latitude :{surveyResponse.Latitude}");

                                await safeWriter.WriteAsync(surveyResponse);
                                returnCoordinates = true;
                            }
                        }

                        if (GeneralHelper.insertqueries.Count > 0)
                        {
                            using (SqliteConnection m_dbConnection = new SqliteConnection(_appDbContext.Database.GetConnectionString()))
                            {
                                try
                                {
                                    m_dbConnection.Open();

                                    //deleting existing records
                                    foreach (string query in GeneralHelper.deletequeries)
                                    {
                                        try
                                        {
                                            using (SqliteCommand command = new SqliteCommand(query, m_dbConnection))
                                            {
                                                command.ExecuteNonQuery();
                                            }

                                        }
                                        catch (Exception ex)
                                        {
                                            Log.Error($"Error in deleting data : {ex.Message}");
                                        }
                                    }

                                    //inserting data
                                    foreach (string query in GeneralHelper.insertqueries)
                                    {
                                        try
                                        {
                                            using (SqliteCommand command = new SqliteCommand(query, m_dbConnection))
                                            {
                                                command.Parameters.AddWithValue("@NULL", DBNull.Value);
                                                command.ExecuteNonQuery();
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Log.Error($"Error in inserting data : {ex.Message} => Query : {query}");
                                        }
                                    }

                                    GeneralHelper.deletequeries.Clear();
                                    GeneralHelper.insertqueries.Clear();
                                    GC.Collect();
                                }
                                catch (Exception ex)
                                {
                                    Log.Error($"Error in inserting data : {ex.Message} ");
                                }
                            }
                        }
                        Log.Information($"Batch{i + 1} Total Time(seconds) for query execution : {(DateTime.Now - startTimesql).TotalSeconds}");

                        GeneralHelper.deletequeries.Clear();
                        GeneralHelper.insertqueries.Clear();
                        insertingDataQueries.Clear();
                        GC.Collect();
                        Log.Information($"Batch{i + 1} After Clearing objects");

                        //crack classification
                        if (LcmsObjects.Any(x => x == "Crack Classification"))
                        {
                            Log.Information($"Batch{i + 1} Started Crack Classification");
                            //List<Action> generatingCrackClassifications = new List<Action>();
                            //idgt
                            var callContext = new CallContext(new CallOptions(cancellationToken: context.CancellationToken));

                            reportProgress(ReportXmlProgress(i, batchCount, tasks, totalTasks), $"Batch {i + 1}: Processing Crack Classification.");
                            tasks++;

                            //Segment Grid:
                            await _crackClassificationService.ReclassifyBatch(xmlFileRequest, callContext);

                            Log.Information($"Batch{i + 1} After Inserting Crack Classification");

                            //GeneralHelper.deletequeries.Clear();
                            //GeneralHelper.insertqueries.Clear();
                            //generatingCrackClassifications.Clear();
                            //GC.Collect();

                            //Log.Information($"Batch{i + 1} Cleaning memory from Crack Classification");

                        }

                        _processedxmlFiles += batchFileCount;

                        if (cancellationToken.IsCancellationRequested)
                        {
                            return;
                        }

                        //segment summary
                        reportProgress(ReportXmlProgress(i, batchCount, tasks, totalTasks), $"Batch {i + 1}: Calculating segment summary.");
                        tasks++;

                        Log.Information($"Start segment summary calculation");
                        await _segmentService.CalculateSegmentSummary(segments.ToList());
                        Log.Information($"End segment summary calculation");

                        // create overlay images only from fis file processing
                        if (!request.XmlOnly)
                        {
                            reportProgress(ReportXmlProgress(i, batchCount, tasks, totalTasks), $"Batch {i + 1}: Creating Overlay images..");
                            tasks++;

                            //generating overlay images only if it is fis file processing
                            Log.Information("Start creating overlays...");

                            var imageTypes = new List<string> { "Intensity" }; //default
                            if (request.GenerateRangeOverlay) imageTypes.Add("Range");

                            var response = await _imageBandInfoService.GenerateOverlayImages(segments, imageTypes, request.SelectedOverlayModules.ToList());
                            if (response.errorMessages != null && response.failedFiles != 0)
                            {
                                var formattedErrors = string.Join(Environment.NewLine, response.errorMessages.Select(m => $"• {m}"));

                                var status = "FAIL";
                                string logDetails;
                                if (response.failedFiles == response.totalFiles)
                                {
                                    logDetails = $"Overlay creation failed for all files. Details: {formattedErrors}";
                                }
                                else
                                {
                                    //partial failure
                                    logDetails = $"Overlay creation completed with {response.failedFiles} failures out of {response.totalFiles} files. Details: {formattedErrors}";
                                }
                                var overlayLog = new DetailLogViewHelper
                                {
                                    FileName = "Overlay image creation",
                                    FileType = ".jpg",
                                    Status = status,
                                    LogDetails = logDetails
                                };
                                detailLogViewHelpers.Add(Newtonsoft.Json.JsonConvert.SerializeObject(overlayLog));
                            }
                            Log.Information("End creating overlays...");
                        }

                        ClearLocalList();
                        GC.Collect();
                    }

                    Log.Information($"Total Time(seconds) for XML operation : {(DateTime.Now - startTimeDB).TotalSeconds}");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error in ProcessXMLFromMain {ex.Message}");
            }
        }

        public async Task ParseLcmsAnalyser(XmlFilesRequest xmlFileRequest, List<string> LcmsObjects, CancellationToken cancellationToken)
        {
            DateTime start = DateTime.Now;
            try
            {
                Log.Information($"Start processing XML using parser ParseLcmsAnalyser");


                foreach (var path in xmlFileRequest.XmlFilesPath)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    DetailLogViewHelper detailLogViewHelper = new DetailLogViewHelper();
                    detailLogViewHelper.FileName = Path.GetFileNameWithoutExtension(path);
                    detailLogViewHelper.FileType = ".xml";

                    try
                    {
                        string directoryPath = Path.GetDirectoryName(path);
                        string parentDirectory = Directory.GetParent(directoryPath).FullName;
                        await ParseLcmsOneFile(path, LcmsObjects, cancellationToken, xmlFileRequest.HorizontalOffset, xmlFileRequest.VerticalOffset);

                        detailLogViewHelper.Status = "PASS";
                        detailLogViewHelper.LogDetails = "XML processed successfully.";
                        detailLogViewHelper.XMLPath = directoryPath;
                        detailLogViewHelper.ImagePath = Path.Combine(parentDirectory, "ImageResult");
                        detailLogViewHelper.SelectedLcmsObjects = string.Join(", ", LcmsObjects);
                        detailLogViewHelper.ErdPath = Path.Combine(parentDirectory, "ErdResult");
                        detailLogViewHelper.PpfPath = Path.Combine(parentDirectory, "PpfResult");
                        detailLogViewHelpers.Add(Newtonsoft.Json.JsonConvert.SerializeObject(detailLogViewHelper));
                    }
                    catch (Exception ex)
                    {

                        detailLogViewHelper.Status = "FAIL";
                        detailLogViewHelper.LogDetails = $"XML process is failed as {ex.Message}";
                        detailLogViewHelpers.Add(Newtonsoft.Json.JsonConvert.SerializeObject(detailLogViewHelper));

                        Log.Error(ex.Message);
                        throw;
                    }
                }

                Log.Information($"End processing XML using parser");
                Log.Information($"XMLParser is successfully executed within {(DateTime.Now - start).TotalSeconds} seconds.");

            }
            catch (Exception ex)
            {
                string trace = ex.StackTrace != null ? ex.StackTrace : string.Empty;
                Log.Error($"Error in parsing XML file {ex.Message} at {trace}");
                throw;
            }
        }

        public async Task ParseLcmsOneFile(string path, List<string> LcmsObjects, CancellationToken cancellationToken, double HorizontalOffset, double VerticalOffset)
        {
            string directory = Path.GetDirectoryName(Path.GetDirectoryName(path));
            string fileName = Path.GetFileNameWithoutExtension(path);

            string IRIprefix = "IRI_";
            if (fileName.StartsWith(IRIprefix))
            {
                // Remove the first 4 characters (length of "IRI_")
                fileName = fileName.Substring(IRIprefix.Length);
            }

            string imageFilePath = Path.Combine(fileName + ".jpg");

            double[] segmentcoordinate2 = new double[] { 0, 0 };
            double[] segmentcoordinate3 = new double[] { 0, 0 };
            double[] segmentcoordinate4 = new double[] { 0, 0 };
            double[] zeroPositionCoordinate = new double[] { 0, 0 };

            string surveyId = string.Empty;
            string surveyDate = string.Empty;
            string sectionId = string.Empty;

            double longitude = 0.0,
                    latitude = 0.0,
                    altitude = 0.0,
                    trackAngle = 0.0;

            double x = 0;
            double y = 0;

            double chainage = 0.0;

            XmlDocument doc = new XmlDocument();
            XmlNodeList? gpsCoordinateNodeList;
            Core.Communication.GPSCoordinate gpsCoordinate = new Core.Communication.GPSCoordinate
            {
                Longitude = longitude,
                Latitude = latitude,
                Altitude = altitude,
                TrackAngle = trackAngle
            };

            try
            {
                doc.Load(path);

                Log.Information($"Start processing ONE XML using parser for file: {path}");

                surveyId = doc.SelectSingleNode("/LcmsAnalyserResults/RoadSectionInfo/SurveyID").InnerText;
                sectionId = doc.SelectSingleNode("/LcmsAnalyserResults/RoadSectionInfo/SectionID").InnerText;
                surveyDate = doc.SelectSingleNode("/LcmsAnalyserResults/SystemData/SystemStatus/SystemTimeAndDate").InnerText;
                chainage = GetDouble(doc.SelectSingleNode("/LcmsAnalyserResults/RoadSectionInfo/DistanceBegin_m").InnerText);

                gpsCoordinateNodeList = doc.SelectNodes("/LcmsAnalyserResults/GPSInformation/GPSCoordinate");

                bool bHasValidSectionPos = false;
                XmlNode sectionPos = doc.SelectSingleNode("/LcmsAnalyserResults/SectionPosition");
                if (sectionPos != null)
                {
                    longitude = GetDoubleCoordinates(GetValueFromNode(sectionPos, "Longitude"));
                    latitude = GetDoubleCoordinates(GetValueFromNode(sectionPos, "Latitude"));
                    if (!(longitude == -1 && latitude == -1))
                    {
                        bHasValidSectionPos = true;
                    }
                }

                if (bHasValidSectionPos)
                {

                    XmlNode sectionPosition = doc.SelectSingleNode("/LcmsAnalyserResults/SectionPosition");
                    if (sectionPosition != null)
                    {
                        longitude = GetDoubleCoordinates(GetValueFromNode(sectionPosition, "Longitude"));
                        latitude = GetDoubleCoordinates(GetValueFromNode(sectionPosition, "Latitude"));
                        altitude = GetDoubleCoordinates(GetValueFromNode(sectionPosition, "Altitude"));

                        string strHeading = GetValueFromNode(sectionPosition, "Heading");
                        if (strHeading != string.Empty)
                        {
                            trackAngle = GetDoubleCoordinates(strHeading);
                        }

                        var currentGpsInfo = new gpsInfo();
                        currentGpsInfo.Longitude = longitude;
                        currentGpsInfo.Latitude = latitude;
                        currentGpsInfo.Altitude = altitude;

                        // Get trackAngle from GPSCoordinate if failed to get from SectionPosition
                        if (trackAngle == 0 && gpsCoordinateNodeList != null)
                        {
                            if (gpsCoordinateNodeList.Count > 0)
                            {
                                XmlNode firstCoordinate = gpsCoordinateNodeList[0];
                                trackAngle = GetDoubleCoordinates(GetValueFromNode(firstCoordinate, "TrackAngle"));
                            }
                        }

                        if (trackAngle == 0 && lastSegmentCoordinate.Latitude != 0)
                        {
                            trackAngle = CalculateBearing(lastSegmentCoordinate, currentGpsInfo);
                        }

                        lastSegmentCoordinate = currentGpsInfo;

                    }
                    else
                    {
                        Log.Error($"Error in Section Position: Null value");
                    }

                    if (longitude == -1 || latitude == -1)
                    {
                        Log.Error($"Error in Section Position: -1 value");
                        throw new Exception("Invalid longitude or latitude value: -1. Please check your cfg file.");
                    }
                }
                else
                {
                    if (gpsCoordinateNodeList != null && gpsCoordinateNodeList.Count > 0)
                    {
                        XmlNode firstCoordinate = gpsCoordinateNodeList[0];
                        longitude = GetDoubleCoordinates(GetValueFromNode(firstCoordinate, "Longitude"));
                        latitude = GetDoubleCoordinates(GetValueFromNode(firstCoordinate, "Latitude"));
                        altitude = GetDoubleCoordinates(GetValueFromNode(firstCoordinate, "Altitude"));
                        string sTrackAngle = GetValueFromNode(firstCoordinate, "TrackAngle");
                        if (sTrackAngle != string.Empty)
                        {
                            trackAngle = GetDoubleCoordinates(sTrackAngle);
                        }


                        var currentGpsInfo = new gpsInfo();
                        currentGpsInfo.Longitude = longitude;
                        currentGpsInfo.Latitude = latitude;
                        currentGpsInfo.Altitude = altitude;

                        if (trackAngle == 0 && lastSegmentCoordinate.Latitude != 0)
                        {
                            trackAngle = CalculateBearing(lastSegmentCoordinate, currentGpsInfo);
                        }

                        lastSegmentCoordinate = currentGpsInfo;
                    }
                }
                
                gpsCoordinate = new Core.Communication.GPSCoordinate
                {
                    Longitude = longitude,
                    Latitude = latitude,
                    Altitude = altitude,
                    TrackAngle = trackAngle
                };

                double ResolutionX = GetDouble(GetValueFromNode(doc, "/LcmsAnalyserResults/ResultImageInformation/ResolutionX"));
                double ResolutionY = GetDouble(GetValueFromNode(doc, "/LcmsAnalyserResults/ResultImageInformation/ResolutionY"));
                double ImageWidth = GetDouble(GetValueFromNode(doc, "/LcmsAnalyserResults/ResultImageInformation/ImageWidth"));
                double ImageHeight = GetDouble(GetValueFromNode(doc, "/LcmsAnalyserResults/ResultImageInformation/ImageHeight"));

                x = ResolutionX * ImageWidth;
                y = ResolutionY * ImageHeight;

                //apply initial horizontal offset considering anntena is in the middle 
                zeroPositionCoordinate = ConvertToGPSCoordinates(-(x / 2), 0, gpsCoordinate);
                gpsCoordinate.Longitude = zeroPositionCoordinate[0];
                gpsCoordinate.Latitude = zeroPositionCoordinate[1];

                if (bHasValidSectionPos)
                {
                    //apply y axis offset
                    zeroPositionCoordinate = ConvertToGPSCoordinates(0, -(y / 2), gpsCoordinate);
                    gpsCoordinate.Longitude = zeroPositionCoordinate[0];
                    gpsCoordinate.Latitude = zeroPositionCoordinate[1];
                }

                if (HorizontalOffset != 0 || VerticalOffset != 0)
                {
                    zeroPositionCoordinate = ConvertToGPSCoordinates(HorizontalOffset, VerticalOffset, gpsCoordinate);
                    gpsCoordinate.Longitude = zeroPositionCoordinate[0];
                    gpsCoordinate.Latitude = zeroPositionCoordinate[1];
                }

                segmentcoordinate2 = ConvertToGPSCoordinates(0, y, gpsCoordinate);
                segmentcoordinate3 = ConvertToGPSCoordinates(x, y, gpsCoordinate);
                segmentcoordinate4 = ConvertToGPSCoordinates(x, 0, gpsCoordinate);

            }

            catch (Exception ex)
            {
                Log.Error($"Error in parsing Serialize XML file: {path} Message: {ex.Message}");
            }

            var pavement = doc.SelectSingleNode("/LcmsAnalyserResults/PavementTypeInformation/PavementType").InnerText;
            double pciValue = 0.0;
            double? paserValue = null;

            //check if segment already exists -> only if it doesn't exist, save segment
            int segmentId = Convert.ToInt32(sectionId);
            var segmentjsonDataObject = new
            {
                type = "Feature",
                geometry = new
                {
                    type = "Polygon",
                    coordinates = new[]
                    {
                        new List<double[]>()
                        {
                            zeroPositionCoordinate, segmentcoordinate2, segmentcoordinate3, segmentcoordinate4
                        }
                    }
                },
                properties = new
                {
                    id = sectionId,
                    file = imageFilePath,
                    type = "Segment"
                }
            };

            var segmentjsonData = JsonSerializer.Serialize(segmentjsonDataObject);

            //segments
            var segment = new LCMS_Segment
            {
                SurveyId = surveyId,
                SectionId = sectionId,
                PavementType = pavement,
                ImageFilePath = imageFilePath,
                GPSLongitude = zeroPositionCoordinate[0],
                GPSLatitude = zeroPositionCoordinate[1],
                GPSAltitude = altitude,
                GPSTrackAngle = trackAngle,
                GeoJSON = segmentjsonData,
                SegmentId = Convert.ToInt32(sectionId),
                Width = x,
                Height = y,
                PCI = pciValue,
                Paser = paserValue,
                Chainage = chainage,
                ChainageEnd = chainage + (y / 1000)
            };

            var existingSegment = _appDbContext.LCMS_Segment.FirstOrDefault(x => x.SurveyId == surveyId && x.SegmentId == segmentId);

            if (existingSegment == null)
            {
                newsegments.Add(segment);
            }
            else
            {
                existingSegments.Add(existingSegment);
            }

            if (LcmsObjects.Any(x => x == LayerNames.PCI))
            {
                var pcijsonDataObject = new
                {
                    type = "Feature",
                    geometry = new
                    {
                        type = "Polygon",
                        coordinates = new[]
                        {
                                new List<double[]>()
                                {
                                    zeroPositionCoordinate, segmentcoordinate2, segmentcoordinate3, segmentcoordinate4
                                }
                            }
                    },
                    properties = new
                    {
                        id = sectionId,
                        file = imageFilePath,
                        type = "PCI"
                    }
                };

                var pcijsonData = JsonSerializer.Serialize(pcijsonDataObject);

                var pciStr = doc.SelectSingleNode("/LcmsAnalyserResults/PCI_Value")?.InnerText;
                pciValue = GetDouble(pciStr);

                string pciRating = pciValue switch
                {
                    >= 85 and <= 100 => "Good",
                    >= 70 and < 85 => "Satisfactory",
                    >= 55 and < 70 => "Fair",
                    >= 40 and < 55 => "Poor",
                    >= 25 and < 40 => "Very Poor",
                    >= 10 and < 25 => "Serious",
                    >= 0 and < 10 => "Failed",
                    _ => "Unknown" // Default case for out-of-range values
                };

                var deductedTypes = doc.SelectSingleNode("/LcmsAnalyserResults/PCI_DeductValueType")?.InnerText;
                var deductedValues = doc.SelectSingleNode("/LcmsAnalyserResults/PCI_DeductValue")?.InnerText;

                if (deductedTypes != null && deductedValues != null)
                {
                    List<string> deductTypesList = deductedTypes?.Split(' ')
                                          .Where(s => !string.IsNullOrWhiteSpace(s))
                                          .ToList()
                                          ?? new List<string>();

                    List<string> deductValuesList = deductedValues?.Split(' ')
                                                                   .Where(s => !string.IsNullOrWhiteSpace(s))
                                                                   .ToList()
                                                                   ?? new List<string>();

                    Dictionary<string, string> jsonDict = new Dictionary<string, string>();

                    if (deductTypesList.Count == deductValuesList.Count)
                    {
                        for (int i = 0; i < deductTypesList.Count; i++)
                        {
                            jsonDict.Add(deductTypesList[i], deductValuesList[i]);
                        }

                        // Convert the dictionary to a JSON string
                        string deductValueJsonString = JsonSerializer.Serialize(jsonDict);

                        var pci = new LCMS_PCI
                        {
                            SurveyId = surveyId,
                            SegmentId = segmentId,
                            DeductedValues = deductValueJsonString,
                            PCIValue = pciValue,
                            GeoJSON = pcijsonData,
                            GPSLongitude = zeroPositionCoordinate[0],
                            GPSLatitude = zeroPositionCoordinate[1],
                            GPSAltitude = altitude,
                            GPSTrackAngle = trackAngle,
                            PavementType = pavement,
                            RatingScale = pciRating,
                            Chainage = chainage,
                            ChainageEnd = chainage + (y / 1000)
                        };
                        newPci.Add(pci);

                        //Update existing segment with new pci value if it exists
                        if (existingSegment != null)
                        {
                            existingSegment.PCI = pciValue;
                            _appDbContext.SaveChanges();
                        }
                    }
                }
            }


            if (LcmsObjects.Any(x => x == LayerNames.PASER))
            {
                var paserValueStr = doc.SelectSingleNode("/LcmsAnalyserResults/PASER_Rating_Value")?.InnerText;
                paserValue = GetDouble(paserValueStr);

                //Save only if the paser has a value
                if (paserValue != -1 && paserValue != 0)
                {
                    var paserjsonDataObject = new
                    {
                        type = "Feature",
                        geometry = new
                        {
                            type = "Polygon",
                            coordinates = new[]
                        {
                                new List<double[]>()
                                {
                                    zeroPositionCoordinate, segmentcoordinate2, segmentcoordinate3, segmentcoordinate4
                                }
                            }
                        },
                        properties = new
                        {
                            id = sectionId,
                            file = imageFilePath,
                            type = "PASER"
                        }
                    };

                    var paserjsonData = JsonSerializer.Serialize(paserjsonDataObject);

                    var paser = new LCMS_PASER
                    {
                        SurveyId = surveyId,
                        SegmentId = segmentId,
                        PaserRating = paserValue.Value,
                        GeoJSON = paserjsonData,
                        GPSLongitude = zeroPositionCoordinate[0],
                        GPSLatitude = zeroPositionCoordinate[1],
                        GPSAltitude = altitude,
                        GPSTrackAngle = trackAngle,
                        PavementType = pavement,
                        Chainage = chainage,
                        ChainageEnd = chainage + (y / 1000)
                    };
                    newPASER.Add(paser);

                    //Update existing segment with new paser value if it exists
                    if (existingSegment != null)
                    {
                        existingSegment.Paser = paserValue.Value;
                        _appDbContext.SaveChanges();
                    }
                }
            }

            var distanceBegin = doc.SelectSingleNode("/LcmsAnalyserResults/RoadSectionInfo/DistanceBegin_m").InnerText;
            var distanceBeginDouble = GetDouble(distanceBegin);

            if (LcmsObjects.Any(x => x == LayerNames.Roughness))
            {
                var roughnessMeasurements = doc.SelectSingleNode("/LcmsAnalyserResults/RoughnessInformation/RoughnessMeasurements");
                if (roughnessMeasurements != null && roughnessMeasurements.HasChildNodes)
                {
                    //Roughness
                    var leftIRIList = new List<double>();
                    var rightIRIList = new List<double>();
                    var centerIRIList = new List<double>();
                    var longitudinalPositionYList = new List<double>();
                    var positionXList = new List<double>();

                    var roughnessNode = roughnessMeasurements.SelectNodes("Roughness");
                    int roughnessNodeIndex = 0;

                    foreach (XmlNode roughnessItem in roughnessNode)
                    {
                        var positionX = GetDouble(GetValueFromNode(roughnessItem, "PositionX"));
                        var iriValues = GetValueFromNode(roughnessItem, "IRI").Split(" ", StringSplitOptions.RemoveEmptyEntries).Select(double.Parse).ToArray(); ;
                        var longitudinalPositionY = GetValueFromNode(roughnessItem, "LongitudinalPositionY").Split(" ", StringSplitOptions.RemoveEmptyEntries).Select(double.Parse).ToArray();

                        if (roughnessNodeIndex == 0)
                        {
                            leftIRIList.AddRange(iriValues);
                            longitudinalPositionYList.AddRange(longitudinalPositionY);
                        }
                        else if (roughnessNodeIndex == 1)
                        {
                            rightIRIList.AddRange(iriValues);
                        }
                        else if (roughnessNodeIndex == 2)
                        {
                            centerIRIList.AddRange(iriValues);
                        }

                        positionXList.Add(positionX);

                        // Increment the node index for the next Roughness node
                        roughnessNodeIndex++;
                    }

                    var intervalNode = doc.SelectSingleNode("/LcmsAnalyserResults/ProcessingInformation/ProcessingParameters/RoughnessModule_Parameters/RoughnessModule_XMLreportingInterval_m");
                    double interval = intervalNode != null ? GetDouble(intervalNode.InnerText) : 1;

                    var speed = doc.SelectSingleNode("/LcmsAnalyserResults/RoadSectionInfo/Speed_kmh").InnerText;

                    for (int i = 0; i < leftIRIList.Count; i++)
                    {
                        var leftIRI = leftIRIList[i];
                        var rightIRI = rightIRIList[i];
                        var laneIRI = (leftIRI + rightIRI) / 2;
                        var naasra = Math.Round((26.49 * laneIRI) - 1.27, 2);
                        double? centerIRI = null;
                        if (centerIRIList.Count > i)
                        {
                            centerIRI = centerIRIList[i];
                            laneIRI = (leftIRI + rightIRI + centerIRI.Value) / 3; //Average of all IRI
                        }

                        //coordinate
                        var yStartInMeter = longitudinalPositionYList[i] - distanceBeginDouble;
                        var yEndInMeter = (i < longitudinalPositionYList.Count - 1) ? longitudinalPositionYList[i + 1] - distanceBeginDouble : yStartInMeter + interval; // Handle the last item

                        var yStart = yStartInMeter * 1000; //Convert m to mm
                        var yEnd = yEndInMeter * 1000; //Convert m to mm

                        var leftX = positionXList[0];
                        var rightX = positionXList[1];
                        var middleX = (leftX + rightX) / 2;

                        double? centerX = null;
                        double[]? centerCoordinate1 = null, centerCoordinate2 = null;
                        string centerJsonData = null;

                        //Check if Cwp IRI exists
                        if (positionXList.Count > 2)
                        {
                            centerX = positionXList[2];
                            centerCoordinate1 = ConvertToGPSCoordinates(centerX.Value, yStart, gpsCoordinate);
                            centerCoordinate2 = ConvertToGPSCoordinates(centerX.Value, yEnd, gpsCoordinate);

                            var centerJsonDataObject = new
                            {
                                type = "Feature",
                                geometry = new
                                {
                                    type = "Polyline",
                                    coordinates = new[] { centerCoordinate1, centerCoordinate2 }
                                },
                                properties = new
                                {
                                    id = i.ToString(),
                                    file = fileName,
                                    type = "Cwp IRI", //Roughness
                                    x = centerX.Value,
                                    y = (yStart + yEnd) / 2
                                }
                            };
                            centerJsonData = JsonSerializer.Serialize(centerJsonDataObject);
                        }

                        var leftCoordinate1 = ConvertToGPSCoordinates(leftX, yStart, gpsCoordinate);
                        var leftCoordinate2 = ConvertToGPSCoordinates(leftX, yEnd, gpsCoordinate);

                        var rightCoordinate1 = ConvertToGPSCoordinates(rightX, yStart, gpsCoordinate);
                        var rightCoordinate2 = ConvertToGPSCoordinates(rightX, yEnd, gpsCoordinate);

                        var laneCoordinate1 = ConvertToGPSCoordinates(middleX, yStart, gpsCoordinate);
                        var laneCoordinate2 = ConvertToGPSCoordinates(middleX, yEnd, gpsCoordinate);

                        //if (polygonCoordinates.Any() && !IsInsidePolygon(laneCoordinate1[1], laneCoordinate1[0], polygonCoordinates))
                        //{
                        //    continue;
                        //}

                        var middleY = (yStart + yEnd) / 2;

                        var leftJsonDataObject = new
                        {
                            type = "Feature",
                            geometry = new
                            {
                                type = "Polyline",
                                coordinates = new[] { leftCoordinate1, leftCoordinate2 }
                            },
                            properties = new
                            {
                                id = i.ToString(),
                                file = fileName,
                                type = "Lwp IRI", //Roughness
                                x = leftX,
                                y = middleY
                            }
                        };

                        var rightJsonDataObject = new
                        {
                            type = "Feature",
                            geometry = new
                            {
                                type = "Polyline",
                                coordinates = new[] { rightCoordinate1, rightCoordinate2 }
                            },
                            properties = new
                            {
                                id = i.ToString(),
                                file = fileName,
                                type = "Rwp IRI", //Roughness
                                x = rightX,
                                y = middleY
                            }
                        };

                        var laneJsonDataObject = new
                        {
                            type = "Feature",
                            geometry = new
                            {
                                type = "Polyline",
                                coordinates = new[] { laneCoordinate1, laneCoordinate2 }
                            },
                            properties = new
                            {
                                id = i.ToString(),
                                file = fileName,
                                type = "Lane IRI", //Roughness
                                x = middleX,
                                y = middleY
                            }
                        };

                        string leftJsonData = JsonSerializer.Serialize(leftJsonDataObject);
                        string rightJsonData = JsonSerializer.Serialize(rightJsonDataObject);
                        string laneJsonData = JsonSerializer.Serialize(laneJsonDataObject);

                        var roughness = new LCMS_Rough_Processed
                        {
                            SurveyId = surveyId,
                            SurveyDate = DateTime.Parse(surveyDate),
                            ImageFileIndex = fileName + ".jpg",
                            PavementType = pavement,
                            RoughnessId = i,
                            LwpIRI = leftIRI,
                            RwpIRI = rightIRI,
                            LaneIRI = laneIRI,
                            CwpIRI = centerIRI,
                            Naasra = naasra,
                            GPSLatitude = laneCoordinate1[1],
                            GPSLongitude = laneCoordinate1[0],
                            GPSAltitude = gpsCoordinate.Altitude,
                            GPSTrackAngle = gpsCoordinate.TrackAngle,
                            GeoJSON = laneJsonData,
                            LwpGeoJSON = leftJsonData,
                            RwpGeoJSON = rightJsonData,
                            CwpGeoJSON = centerJsonData,
                            RoundedGPSLatitude = Math.Round(gpsCoordinate.Latitude, 4),
                            RoundedGPSLongitude = Math.Round(gpsCoordinate.Longitude, 4),
                            SegmentId = Convert.ToInt32(sectionId),
                            LongitudinalPositionY = longitudinalPositionYList[i],
                            Interval = interval,
                            Speed = GetDouble(speed),
                            EndGPSLatitude = laneCoordinate2[1],
                            EndGPSLongitude = laneCoordinate2[0],
                            Chainage = yStartInMeter + chainage,
                            ChainageEnd = yEndInMeter + chainage
                        };
                        newRoughness.Add(roughness);
                    }
                }
            }

            if (LcmsObjects.Any(x => x == LayerNames.SagsBumps))
            {
                var roughnessMeasurements = doc.SelectSingleNode("/LcmsAnalyserResults/RoughnessInformation/RoughnessMeasurements");
                if (roughnessMeasurements != null && roughnessMeasurements.HasChildNodes)
                {
                    int sagBumpId = 0;
                    //Sags
                    var sags = roughnessMeasurements.SelectNodes("Sags");
                    foreach (XmlNode sagItem in sags)
                    {
                        var nbProfilesStr = doc.SelectSingleNode("/LcmsAnalyserResults/ProcessingInformation/ProcessingParameters/SagsAndBumpsModule_Parameters/SagsBumpsModule_NbrOfProfiles").InnerText;

                        var NbProfiles = GetDouble(nbProfilesStr);
                        var oneProfile = x / NbProfiles;

                        var positionX = GetDouble(GetValueFromNode(sagItem, "PositionX")); //mm
                        var nbrValues = GetDouble(GetValueFromNode(sagItem, "NbrValues"));
                        var width = oneProfile;
                        var startPosition = ConvertStringToListDouble(GetValueFromNode(sagItem, "StartPosition")); //meter
                        var endPosition = ConvertStringToListDouble(GetValueFromNode(sagItem, "EndPosition")); //meter
                        var deviations = ConvertStringToListDouble(GetValueFromNode(sagItem, "MaxDeviation"));

                        List<double> startList = startPosition.ToList();
                        List<double> endList = endPosition.ToList();
                        List<double> deviationList = deviations.ToList();

                        for (int i = 0; i < nbrValues; i++)
                            {
                            double start = startList[i];
                            double end = endList[i];
                            double deviation = deviationList[i];

                            var startY = (start - distanceBeginDouble) * 1000;
                            var endY = (end - distanceBeginDouble) * 1000;

                            var height = Math.Abs(endY - startY);
                            var area = Math.Round((width * height) / 1000000, 2);
                            var avgStartPosition = startPosition.Average();
                            var avgEndPosition = endPosition.Average();

                            var coordinate1 = ConvertToGPSCoordinates(positionX, startY, gpsCoordinate);
                            var coordinate2 = ConvertToGPSCoordinates(positionX + width, startY, gpsCoordinate);
                            var coordinate3 = ConvertToGPSCoordinates(positionX + width, endY, gpsCoordinate);
                            var coordinate4 = ConvertToGPSCoordinates(positionX, endY, gpsCoordinate);

                            //if (polygonCoordinates.Any() && !IsInsidePolygon(coordinate1[1], coordinate1[0], polygonCoordinates))
                            //{
                            //    continue;
                            //}

                            var jsonDataObject = new
                            {
                                type = "Feature",
                                geometry = new
                                {
                                    type = "Polygon",
                                    coordinates = new[]
                                    {
                                new List<double[]>()
                                {
                                    coordinate1, coordinate2, coordinate3, coordinate4
                                }
                            }
                                },
                                properties = new
                                {
                                    id = sagBumpId.ToString(),
                                    file = fileName,
                                    type = "Sags Bumps",
                                }
                            };
                            string jsonData = JsonSerializer.Serialize(jsonDataObject);

                            var sag = new LCMS_Sags_Bumps
                            {
                                SurveyId = surveyId,
                                SurveyDate = DateTime.Parse(surveyDate),
                                ImageFileIndex = fileName + ".jpg",
                                PavementType = pavement,
                                Type = "Sags",
                                SagBumpId = sagBumpId,
                                MaxDeviation = deviation,
                                Area_m2 = area,
                                GPSLatitude = coordinate1[1],
                                GPSLongitude = coordinate1[0],
                                GPSAltitude = gpsCoordinate.Altitude,
                                GPSTrackAngle = gpsCoordinate.TrackAngle,
                                GeoJSON = jsonData,
                                RoundedGPSLatitude = Math.Round(coordinate1[1], 4),
                                RoundedGPSLongitude = Math.Round(coordinate1[0], 4),
                                SegmentId = Convert.ToInt32(sectionId),
                                Chainage = avgStartPosition,
                                ChainageEnd = avgEndPosition
                            };
                            newSagsBumps.Add(sag);
                            sagBumpId++;
                        }

                    }
                    //Bumps
                    var bumps = roughnessMeasurements.SelectNodes("Bumps");
                    foreach (XmlNode bumpItem in bumps)
                    {
                        var nbProfilesStr = doc.SelectSingleNode("/LcmsAnalyserResults/ProcessingInformation/ProcessingParameters/SagsAndBumpsModule_Parameters/SagsBumpsModule_NbrOfProfiles").InnerText;

                        var NbProfiles = GetDouble(nbProfilesStr);
                        var oneProfile = x / NbProfiles;

                        var positionX = GetDouble(GetValueFromNode(bumpItem, "PositionX")); //mm
                        var nbrValues = GetDouble(GetValueFromNode(bumpItem, "NbrValues"));
                        var width = oneProfile;
                        var startPosition = ConvertStringToListDouble(GetValueFromNode(bumpItem, "StartPosition")); //meter
                        var endPosition = ConvertStringToListDouble(GetValueFromNode(bumpItem, "EndPosition")); //meter
                        var deviations = ConvertStringToListDouble(GetValueFromNode(bumpItem, "MaxDeviation"));

                        List<double> startList = startPosition.ToList();
                        List<double> endList = endPosition.ToList();
                        List<double> deviationList = deviations.ToList();

                        for (int i = 0; i < nbrValues; i++)
                        {
                            double start = startList[i];
                            double end = endList[i];
                            double deviation = deviationList[i];

                            var startY = (start - distanceBeginDouble) * 1000;
                            var endY = (end - distanceBeginDouble) * 1000;
                            var middleY = (startY + endY) / 2;
                            var height = Math.Abs(endY - startY);

                            var area = Math.Round((width * height) / 1000000, 2);
                            var avgStartPosition = startPosition.Average();
                            var avgEndPosition = endPosition.Average();

                            var coordinate1 = ConvertToGPSCoordinates(positionX, startY, gpsCoordinate);
                            var coordinate2 = ConvertToGPSCoordinates(positionX + width, startY, gpsCoordinate);
                            var coordinate3 = ConvertToGPSCoordinates(positionX + width, endY, gpsCoordinate);
                            var coordinate4 = ConvertToGPSCoordinates(positionX, endY, gpsCoordinate);

                            //if (polygonCoordinates.Any() && !IsInsidePolygon(coordinate1[1], coordinate1[0], polygonCoordinates))
                            //{
                            //    continue;
                            //}

                            var jsonDataObject = new
                            {
                                type = "Feature",
                                geometry = new
                                {
                                    type = "Polygon",
                                    coordinates = new[]
                                    {
                                        new List<double[]>()
                                        {
                                            coordinate1, coordinate2, coordinate3, coordinate4
                                        }
                                    }
                                },
                                properties = new
                                {
                                    id = sagBumpId.ToString(),
                                    file = fileName,
                                    type = "Sags Bumps",
                                    x = positionX,
                                    y = middleY
                                }
                            };
                            string jsonData = JsonSerializer.Serialize(jsonDataObject);

                            var bump = new LCMS_Sags_Bumps
                            {
                                SurveyId = surveyId,
                                SurveyDate = DateTime.Parse(surveyDate),
                                ImageFileIndex = fileName + ".jpg",
                                PavementType = pavement,
                                Type = "Bumps",
                                SagBumpId = sagBumpId,
                                MaxDeviation = deviation,
                                Area_m2 = area,
                                GPSLatitude = coordinate1[1],
                                GPSLongitude = coordinate1[0],
                                GPSAltitude = gpsCoordinate.Altitude,
                                GPSTrackAngle = gpsCoordinate.TrackAngle,
                                GeoJSON = jsonData,
                                RoundedGPSLatitude = Math.Round(coordinate1[1], 4),
                                RoundedGPSLongitude = Math.Round(coordinate1[0], 4),
                                SegmentId = Convert.ToInt32(sectionId),
                                Chainage = avgStartPosition,
                                ChainageEnd = avgEndPosition
                            };
                            newSagsBumps.Add(bump);
                            sagBumpId++;
                        }
                    }
                }

                IEnumerable<double> ConvertStringToListDouble(string value)
                {
                    return value.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(double.Parse);
                }
            }

            //pickouts
            if (LcmsObjects.Any(x => x == LayerNames.Pickout))
            {
                var pickouts = doc.SelectNodes("/LcmsAnalyserResults/PickOutInformation/PickOut");
                if (pickouts != null && pickouts.Count > 0)
                    foreach (XmlNode pickoutItem in pickouts)
                    {
                        List<double[]> coordinates = new List<double[]>();

                        var pickoutId = GetInt(GetValueFromNode(pickoutItem, "PickOutID"));
                        //Converting cm2 to mm2 
                        var area = GetDouble(GetValueFromNode(pickoutItem, "Area")) * 100;
                        var maxDepth = GetDouble(GetValueFromNode(pickoutItem, "MaximumDepth"));
                        var avgDepth = GetDouble(GetValueFromNode(pickoutItem, "AverageDepth"));

                        var boundingBox = pickoutItem.SelectNodes("BoundingBox");

                        var minX = GetDouble(GetValueFromNode(boundingBox[0], "MinX"));
                        var maxX = GetDouble(GetValueFromNode(boundingBox[0], "MaxX"));
                        var minY = GetDouble(GetValueFromNode(boundingBox[0], "MinY"));
                        var maxY = GetDouble(GetValueFromNode(boundingBox[0], "MaxY"));

                        double middleX = (minX + maxX) / 2;
                        double middleY = (minY + maxY) / 2;

                        var coordinate = ConvertToGPSCoordinates(middleX, middleY, gpsCoordinate);


                        //if (polygonCoordinates.Any() && !IsInsidePolygon(coordinate[1], coordinate[0], polygonCoordinates))
                        //{
                        //    continue;
                        //}

                        var jsonDataObject = new
                        {
                            type = "Feature",
                            geometry = new
                            {
                                type = "Point",
                                coordinates = coordinate
                            },
                            properties = new
                            {
                                id = pickoutId,
                                file = fileName,
                                type = "Pickout",
                                x = middleX,
                                y = middleY
                            }
                        };
                        string jsonData = JsonSerializer.Serialize(jsonDataObject);

                        var pickout = new LCMS_PickOuts_Raw
                        {
                            SurveyId = surveyId,
                            SurveyDate = DateTime.Parse(surveyDate),
                            PickOutId = pickoutId,
                            Area_mm2 = area,
                            MaxDepth_mm = maxDepth,
                            AvgDepth_mm = avgDepth,
                            ImageFileIndex = fileName + ".jpg",
                            PavementType = pavement,
                            GPSLatitude = coordinate[1],
                            GPSLongitude = coordinate[0],
                            GPSAltitude = gpsCoordinate.Altitude,
                            GPSTrackAngle = gpsCoordinate.TrackAngle,
                            GeoJSON = jsonData,
                            RoundedGPSLatitude = Math.Round(coordinate[1], 4),
                            RoundedGPSLongitude = Math.Round(coordinate[0], 4),
                            SegmentId = Convert.ToInt32(sectionId),
                            Chainage = (middleY/1000) + chainage,
                            ChainageEnd = (maxY / 1000) + chainage
                        };

                        newpickOuts_Raws.Add(pickout);
                    }
            }

            //crack
            if (LcmsObjects.Any(x => x == LayerNames.Cracking) || LcmsObjects.Any(x => x == LayerNames.CrackFaulting))
            {
                //Severity 
                var resultRenderList = doc.SelectNodes("/LcmsAnalyserResults/ProcessingInformation/ProcessingParameters/ResultRenderer_Parameters");
                var resultRender = resultRenderList[0];
                var low = GetDouble(GetValueFromNode(resultRender, "ResultRenderer_CrackSeverity0_MaxWidth_mm"));
                var med = GetDouble(GetValueFromNode(resultRender, "ResultRenderer_CrackSeverity1_MaxWidth_mm"));
                var high = GetDouble(GetValueFromNode(resultRender, "ResultRenderer_CrackSeverity2_MaxWidth_mm"));

                var crackType = doc.SelectSingleNode("/LcmsAnalyserResults/CrackInformation/Unit/Type");
                var MultipleCrackRegion = doc.SelectSingleNode("/LcmsAnalyserResults/CrackClassification/MultipleCracks/MultipleCrackRegion");
                var AlligatorRegion = doc.SelectSingleNode("/LcmsAnalyserResults/CrackClassification/Alligators/AlligatorRegion");
                var TransversalCrack = doc.SelectSingleNode("/LcmsAnalyserResults/CrackClassification/Transversals/TransversalCrack");

                bool isMultipleCrackRegion = MultipleCrackRegion != null ? true : false,
                    isAlligatorRegion = AlligatorRegion != null ? true : false,
                    isTransversalCrack = TransversalCrack != null ? true : false;
                LCMSBoundingBox MultipleCrackRegionBBox = null, AlligatorRegionBBox = null, TransversalCrackBBox = null;

                //if found MTQ, get all typed bounding boxes from crack classification
                if (crackType?.InnerText == "MTQ" && (isMultipleCrackRegion || isAlligatorRegion || isTransversalCrack))
                {
                    if (isMultipleCrackRegion)
                    {
                        var lCMSBoundingBox = doc.SelectSingleNode("/LcmsAnalyserResults/CrackClassification/MultipleCracks/MultipleCrackRegion/BoundingBox");
                        if (lCMSBoundingBox != null)
                            MultipleCrackRegionBBox = GetLCMSBoundingBox(lCMSBoundingBox);
                    }

                    if (isAlligatorRegion)
                    {
                        var lCMSBoundingBox = doc.SelectSingleNode("/LcmsAnalyserResults/CrackClassification/MultipleCracks/AlligatorRegion/BoundingBox");
                        if (lCMSBoundingBox != null)
                            AlligatorRegionBBox = GetLCMSBoundingBox(lCMSBoundingBox);
                    }

                    if (isTransversalCrack)
                    {
                        var lCMSBoundingBox = doc.SelectSingleNode("/LcmsAnalyserResults/CrackClassification/MultipleCracks/TransversalCrack/BoundingBox");
                        if (lCMSBoundingBox != null)
                            TransversalCrackBBox = GetLCMSBoundingBox(lCMSBoundingBox);
                    }
                }

                var cracks = doc.SelectNodes("/LcmsAnalyserResults/CrackInformation/CrackList/Crack");
                if (cracks != null & cracks?.Count > 0)
                foreach (XmlNode crackItem in cracks)
                {
                    List<(double X, double Y)> crackNodeXYList = new List<(double X, double Y)>();
                    var crackId = GetInt(GetValueFromNode(crackItem, "CrackID"));
                    double faulting = GetDouble(GetValueFromNode(crackItem, "Faulting"));

                    //converting crack length in meter to millimeter for the consistency
                    var crackLengthInMeter = crackItem.SelectSingleNode("Length").InnerText;
                    var crackLength = Double.Parse(crackLengthInMeter) * 1000;
                    double crackWeightedDepth = GetDouble(GetValueFromNode(crackItem, "WeightedDepth"));
                    double crackWeightedWidth = GetDouble(GetValueFromNode(crackItem, "WeightedWidth"));

                    var node = crackItem.SelectNodes("Node");
                    int nodeIndex = 0;
                    string severity = string.Empty;
                    double[] firstCoordinate = null;
                    double[] lastCoordinate = null;
                    XmlNode previousNode = null;
                    double firstChainage = 0; 

                    foreach (XmlNode currentNode in node)
                    {
                        if (previousNode != null)
                        {
                            double previousWidth = GetDouble(GetValueFromNode(previousNode, "Width"));
                            double previousDepth = GetDouble(GetValueFromNode(previousNode, "Depth"));
                            double previousX = GetDouble(GetValueFromNode(previousNode, "X"));
                            double previousY = GetDouble(GetValueFromNode(previousNode, "Y"));
                            double[] previousCoordinate = ConvertToGPSCoordinates(previousX, previousY, gpsCoordinate);

                            string nodeId = nodeIndex.ToString();
                            double currentX = GetDouble(GetValueFromNode(currentNode, "X"));
                            double currentY = GetDouble(GetValueFromNode(currentNode, "Y"));
                            crackNodeXYList.Add((currentX, currentY));
                            double[] coordinate = ConvertToGPSCoordinates(currentX, currentY, gpsCoordinate);

                            double middleX = (previousX + currentX) / 2;
                            double middleY = (previousY + currentY) / 2;

                            double middleY_m = middleY / 1000.0;


                            firstChainage = middleY_m + chainage; 

                            // Store the first coordinate if it's null
                            if (firstCoordinate == null)
                            {
                                firstCoordinate = previousCoordinate;
                            }

                            // Update last coordinate in every iteration
                            lastCoordinate = coordinate;

                            //if (polygonCoordinates.Any() && !IsInsidePolygon(coordinate[1], coordinate[0], polygonCoordinates))
                            //{
                            //    continue;
                            //}

                            if (previousWidth < low)
                            {
                                severity = "Very Low";
                            }
                            else if (previousWidth >= low && previousWidth <= med)
                            {
                                severity = "Low";
                            }
                            else if (previousWidth >= med && previousWidth <= high)
                            {
                                severity = "Medium";
                            }
                            else if (previousWidth > high)
                            {
                                severity = "High";
                            }

                            var jsonDataObject = new
                            {
                                type = "Feature",
                                geometry = new
                                {
                                    type = "Polyline",
                                    coordinates = new List<double[]>()
                                    {
                                        previousCoordinate, coordinate
                                    }
                                },
                                properties = new
                                {
                                    id = crackId,
                                    file = fileName,
                                    type = "Cracking",
                                    x = middleX,
                                    y = middleY
                                }
                            };

                            var nodeLength = CalculateDistance(previousX, previousY, currentX, currentY);
                            string jsonData = JsonSerializer.Serialize(jsonDataObject);

                            var crack = new LCMS_Cracking_Raw
                            {
                                SurveyId = surveyId,
                                SurveyDate = DateTime.Parse(surveyDate),
                                GPSLatitude = previousCoordinate[1],
                                GPSLongitude = previousCoordinate[0],
                                GPSAltitude = altitude,
                                GPSTrackAngle = gpsCoordinate.TrackAngle,
                                CrackId = crackId,
                                NodeId = nodeIndex,
                                NodeLength_mm = nodeLength,
                                NodeWidth_mm = previousWidth,
                                NodeDepth_mm = previousDepth,
                                Severity = severity,
                                PavementType = pavement,
                                ImageFileIndex = fileName + ".jpg",
                                GeoJSON = jsonData,
                                RoundedGPSLatitude = Math.Round(previousCoordinate[1], 4),
                                RoundedGPSLongitude = Math.Round(previousCoordinate[0], 4),
                                SegmentId = Convert.ToInt32(sectionId),
                                Faulting = LcmsObjects.Any(x => x == "Crack Faulting") ? faulting : null,
                                EndGPSLatitude = coordinate[1],
                                EndGPSLongitude = coordinate[0],
                                Chainage = firstChainage,
                                ChainageEnd = firstChainage + (nodeLength / 1000.0)
                            };
                            newcracking_Raws.Add(crack);
                            nodeIndex++;
                        }
                        previousNode = currentNode;
                    }

                    if (firstCoordinate != null && lastCoordinate != null)
                    {
                        var summaryJsonDataObject = new
                        {
                            type = "Feature",
                            geometry = new
                            {
                                type = "Polyline",
                                coordinates = new List<double[]>()
                                {
                                    firstCoordinate, lastCoordinate
                                }
                            },
                            properties = new
                            {
                                id = crackId,
                                file = fileName,
                                type = "Crack Summary"
                            }
                        };

                        string summaryJsonData = JsonSerializer.Serialize(summaryJsonDataObject);

                        string crackSeverity = null;
                        if (crackWeightedWidth < low)
                        {
                            crackSeverity = "Very Low";
                        }
                        else if (crackWeightedWidth >= low && crackWeightedWidth <= med)
                        {
                            crackSeverity = "Low";
                        }
                        else if (crackWeightedWidth >= med && crackWeightedWidth <= high)
                        {
                            crackSeverity = "Medium";
                        }
                        else if (crackWeightedWidth > high)
                        {
                            crackSeverity = "High";
                        }

                        string calculatedMTQ = string.Empty;
                        LCMSBoundingBox boundingBox = new LCMSBoundingBox();
                        if (crackNodeXYList != null && crackNodeXYList.Count > 0)
                        {
                            //check for MTQ type
                            boundingBox.MaxX = (float)crackNodeXYList.Max(c => c.X);
                            boundingBox.MaxY = (float)crackNodeXYList.Max(c => c.Y);
                            boundingBox.MinX = (float)crackNodeXYList.Min(c => c.X);
                            boundingBox.MinY = (float)crackNodeXYList.Min(c => c.Y);
                            calculatedMTQ = ClassifyCrack(
                                new LCMSBoundingBox
                                {
                                    MaxX = (float)crackNodeXYList.Max(c => c.X),
                                    MaxY = (float)crackNodeXYList.Max(c => c.Y),
                                    MinX = (float)crackNodeXYList.Min(c => c.X),
                                    MinY = (float)crackNodeXYList.Min(c => c.Y)
                                },
                                MultipleCrackRegionBBox != null ? new List<LCMSBoundingBox> { MultipleCrackRegionBBox } : new List<LCMSBoundingBox>(),
                                AlligatorRegionBBox != null ? new List<LCMSBoundingBox> { AlligatorRegionBBox } : new List<LCMSBoundingBox>(),
                                TransversalCrackBBox != null ? new List<LCMSBoundingBox> { TransversalCrackBBox } : new List<LCMSBoundingBox>());
                        }

                        var crackSummary = new LCMS_CrackSummary
                        {
                            SurveyId = surveyId,
                            SurveyDate = DateTime.Parse(surveyDate),
                            CrackId = crackId,
                            WeightedDepth_mm = crackWeightedDepth,
                            WeightedWidth_mm = crackWeightedWidth,
                            CrackLength_mm = crackLength,
                            GPSLatitude = firstCoordinate[1],
                            GPSLongitude = firstCoordinate[0],
                            GPSAltitude = altitude,
                            GPSTrackAngle = gpsCoordinate.TrackAngle,
                            Faulting = faulting,
                            PavementType = pavement,
                            ImageFileIndex = fileName + ".jpg",
                            SegmentId = Convert.ToInt32(sectionId),
                            RoundedGPSLatitude = Math.Round(firstCoordinate[1], 4),
                            RoundedGPSLongitude = Math.Round(firstCoordinate[0], 4),
                            GeoJSON = summaryJsonData,
                            Severity = crackSeverity,
                            EndGPSLatitude = lastCoordinate[1],
                            EndGPSLongitude = lastCoordinate[0],
                            MTQ = calculatedMTQ,
                            Chainage = firstChainage,
                            ChainageEnd = firstChainage + (crackLength / 1000.0),
                            maxX = boundingBox.MaxX,
                            maxY = boundingBox.MaxY,
                            minX = boundingBox.MinX,
                            minY = boundingBox.MinY
                        };
                        newCrackSummary.Add(crackSummary);
                    }
                    }
            }

            //ravelling_Raws
            if (LcmsObjects.Any(x => x == LayerNames.Ravelling))
            {
                //Severity 
                var resultRenderList = doc.SelectNodes("/LcmsAnalyserResults/ProcessingInformation/ProcessingParameters/ResultRenderer_Parameters");
                var resultRender = resultRenderList[0];
                var ravlow = GetDouble(GetValueFromNode(resultRender, "ResultRenderer_RavRate1_cm3_m2"));
                var ravmed = GetDouble(GetValueFromNode(resultRender, "ResultRenderer_RavRate2_cm3_m2"));
                var ravhigh = GetDouble(GetValueFromNode(resultRender, "ResultRenderer_RavRate3_cm3_m2"));
                var ravellingList = doc.SelectSingleNode("/LcmsAnalyserResults/RavelingInformation/ZoneReportList");
                if (ravellingList != null)
                {
                    var width = GetDouble(GetValueFromNode(ravellingList, "ZoneWidth"));
                    var height = GetDouble(GetValueFromNode(ravellingList, "ZoneHeight"));
                    var squareArea = width * height;
                    var ravellingModule = doc.SelectNodes("/LcmsAnalyserResults/ProcessingInformation/ProcessingParameters/RavelingModule_Parameters");
                    var algorithm = GetDouble(GetValueFromNode(ravellingModule[0], "RavelingModlue_RavelingCoinAlgorithmEnable"));
                    int index = 0;
                    var ravellings = ravellingList.SelectNodes("ZoneReport");

                    foreach (XmlNode ravellingItem in ravellings)
                    {
                        var squareId = index;
                        var X = GetDouble(GetValueFromNode(ravellingItem, "X"));
                        var Y = GetDouble(GetValueFromNode(ravellingItem, "Y"));
                        var RI_Area = GetDouble(GetValueFromNode(ravellingItem, "RI_Area"));
                        //Setting valuGetDouble(es to -1 when it doesn't exist
                        var ravellingIndex = -1.0;
                        var RPI = -1.0;
                        var AVC = -1.0;
                        var RI_Percent = -1.0;
                        string severity = "";
                        if (algorithm == 0)
                        {
                            ravellingIndex = GetDouble(GetValueFromNode(ravellingItem, "RI"));
                            RPI = GetDouble(GetValueFromNode(ravellingItem, "RPI"));
                            AVC = GetDouble(GetValueFromNode(ravellingItem, "AVC"));
                            severity = DetermineRavellingSeverity(ravellingIndex, ravlow, ravmed, ravhigh);
                        }
                        else if (algorithm == 1)
                        {
                            RI_Percent = GetDouble(GetValueFromNode(ravellingItem, "RI_Percent"));
                            severity = DetermineRavellingSeverity(RI_Percent, ravlow, ravmed, ravhigh);
                        }
                        if (severity == string.Empty)
                        {
                            index++;
                            continue;
                        }
                        double[] coordinate1 = ConvertToGPSCoordinates(X, Y, gpsCoordinate);
                        Core.Communication.GPSCoordinate ravelingCoordinate = new Core.Communication.GPSCoordinate
                        {
                            Longitude = coordinate1[0],
                            Latitude = coordinate1[1],
                            Altitude = altitude,
                            TrackAngle = trackAngle
                        };
                        double[] coordinate2 = ConvertToGPSCoordinates(0, height, ravelingCoordinate);
                        double[] coordinate3 = ConvertToGPSCoordinates(width, height, ravelingCoordinate);
                        double[] coordinate4 = ConvertToGPSCoordinates(width, 0, ravelingCoordinate);

                        //if (polygonCoordinates.Any() && !IsInsidePolygon(coordinate1[1], coordinate1[0], polygonCoordinates))
                        //{
                        //    continue;
                        //}

                        var jsonDataObject = new
                        {
                            type = "Feature",
                            geometry = new
                            {
                                type = "Polygon",
                                coordinates = new[]
                                {
                                new List<double[]>()
                                {
                                    coordinate1, coordinate2, coordinate3, coordinate4
                                }
                            }
                            },
                            properties = new
                            {
                                id = squareId.ToString(),
                                file = fileName,
                                type = "Ravelling",
                                x = X,
                                y = Y
                            }
                        };
                        string jsonData = JsonSerializer.Serialize(jsonDataObject);
                        var ravelling = new LCMS_Ravelling_Raw
                        {
                            SurveyId = surveyId,
                            SurveyDate = DateTime.Parse(surveyDate),
                            ImageFileIndex = fileName + ".jpg",
                            SquareId = squareId,
                            SquareArea_mm2 = squareArea,
                            Algorithm = 0,
                            ALG1_RavellingIndex = ravellingIndex,
                            ALG1_RPI = RPI,
                            ALG1_AVC = AVC,
                            ALG2_RI_Percent = RI_Percent,
                            RI_AREA_mm2 = RI_Area,
                            Severity = severity,
                            PavementType = pavement,
                            GPSLatitude = coordinate1[1],
                            GPSLongitude = coordinate1[0],
                            GPSAltitude = gpsCoordinate.Altitude,
                            GPSTrackAngle = gpsCoordinate.TrackAngle,
                            GeoJSON = jsonData,
                            RoundedGPSLatitude = Math.Round(coordinate1[1], 4),
                            RoundedGPSLongitude = Math.Round(coordinate1[0], 4),
                            SegmentId = Convert.ToInt32(sectionId),
                            Chainage = (Y / 1000 ) + chainage,
                            ChainageEnd = ((Y + height) / 1000) + chainage
                        };
                        index++;
                        newravelling_Raws.Add(ravelling);
                    }
                }
            }

            //potholes_Processed
            if (LcmsObjects.Any(x => x == LayerNames.Potholes))
            {
                var potholes = doc.SelectNodes("/LcmsAnalyserResults/PotholesInformation/Pothole");

                if (potholes != null & potholes.Count > 0)
                    foreach (XmlNode potholeItem in potholes)
                    {
                        var potholesId = GetInt(GetValueFromNode(potholeItem, "PotholeID"));
                        //Convert m2 to mm2
                        var area = Convert.ToDouble(GetValueFromNode(potholeItem, "Area")) * 1000000; // don't use get double here because of invalid value
                        var maxDepth = GetDouble(GetValueFromNode(potholeItem, "MaximumDepth"));
                        var avgDepth = GetDouble(GetValueFromNode(potholeItem, "AverageDepth"));
                        var majorDiameter = GetDouble(GetValueFromNode(potholeItem, "MajorDiameter"));
                        var minorDiameter = GetDouble(GetValueFromNode(potholeItem, "MinorDiameter"));
                        var avgIntensity = GetDouble(GetValueFromNode(potholeItem, "AverageIntensity"));
                        var severity = potholeItem.SelectSingleNode("Severity").InnerText;
                        if(severity == "Moderate")
                        {
                            severity = "Medium";
                        }
                        var boundingBox = potholeItem.SelectNodes("BoundingBox");
                        var minX = GetDouble(GetValueFromNode(boundingBox[0], "MinX"));
                        var maxX = GetDouble(GetValueFromNode(boundingBox[0], "MaxX"));
                        var minY = GetDouble(GetValueFromNode(boundingBox[0], "MinY"));
                        var maxY = GetDouble(GetValueFromNode(boundingBox[0], "MaxY"));

                        var middleX = (minX + maxX) / 2;
                        var middleY = (minY + maxY) / 2;
                        double[] centerCoordinate = ConvertToGPSCoordinates(middleX, middleY, gpsCoordinate);
                        //if (polygonCoordinates.Any() && !IsInsidePolygon(centerCoordinate[1], centerCoordinate[0], polygonCoordinates))
                        //{
                        //    continue;
                        //}

                        var jsonDataObject = new
                        {
                            type = "Feature",
                            geometry = new
                            {
                                type = "Point",
                                coordinates = centerCoordinate
                            },
                            properties = new
                            {
                                id = potholesId.ToString(),
                                file = fileName,
                                diameter = (majorDiameter + minorDiameter) / 2,
                                type = "Potholes",
                                x = middleX,
                                y = middleY
                            }
                        };
                        string jsonData = JsonSerializer.Serialize(jsonDataObject);

                        var pothole = new LCMS_Potholes_Processed
                        {
                            SurveyId = surveyId,
                            SurveyDate = DateTime.Parse(surveyDate),
                            ImageFileIndex = fileName + ".jpg",
                            PotholeId = potholesId,
                            Area_mm2 = area,
                            MaxDepth_mm = maxDepth,
                            AvgDepth_mm = avgDepth,
                            MajorDiameter_mm = majorDiameter,
                            MinorDiameter_mm = minorDiameter,
                            AvgIntensity = avgIntensity,
                            Severity = severity,
                            PavementType = pavement,
                            GPSLatitude = centerCoordinate[1],
                            GPSLongitude = centerCoordinate[0],
                            GPSAltitude = gpsCoordinate.Altitude,
                            GPSTrackAngle = gpsCoordinate.TrackAngle,
                            GeoJSON = jsonData,
                            RoundedGPSLatitude = Math.Round(centerCoordinate[1], 4),
                            RoundedGPSLongitude = Math.Round(centerCoordinate[0], 4),
                            SegmentId = Convert.ToInt32(sectionId),
                            Chainage = (middleY/1000) + chainage,
                            ChainageEnd = ((middleY + (majorDiameter / 2)) / 1000) + chainage
                        };
                        newpotholes_Processed.Add(pothole);
                    }
            }

            //patch_Processed
            if (LcmsObjects.Any(x => x == LayerNames.Patch))
            {
                var patches = doc.SelectNodes("/LcmsAnalyserResults/PatchDetectionInformation/PatchData");

                if (patches != null && patches.Count > 0)
                    foreach (XmlNode patchItem in patches)
                    {
                        List<double[]> coordinates = new List<double[]>();

                        var patchId = GetDouble(GetValueFromNode(patchItem, "PatchID"));

                        var minX = GetDouble(GetValueFromNode(patchItem, "BoundingBox/MinX"));
                        var maxX = GetDouble(GetValueFromNode(patchItem, "BoundingBox/MaxX"));
                        var minY = GetDouble(GetValueFromNode(patchItem, "BoundingBox/MinY"));
                        var maxY = GetDouble(GetValueFromNode(patchItem, "BoundingBox/MaxY"));

                        var middleX = (minX + maxX) / 2;
                        var middleY = (minY + maxY) / 2;

                        var patchlength = maxY - minY;
                        var patchwidth = maxX - minX;

                        var area = GetDouble(GetValueFromNode(patchItem, "Area"));
                        var confidenceScore = GetDouble(GetValueFromNode(patchItem, "ConfidenceScore"));
                        var severity = patchItem.SelectSingleNode("SeverityLevel").InnerText;
                        if(severity == "Moderate")
                        {
                            severity = "Medium";
                        }
                        var coordinate1 = ConvertToGPSCoordinates(minX, maxY, gpsCoordinate);
                        var coordinate2 = ConvertToGPSCoordinates(maxX, maxY, gpsCoordinate);
                        var coordinate3 = ConvertToGPSCoordinates(maxX, minY, gpsCoordinate);
                        var coordinate4 = ConvertToGPSCoordinates(minX, minY, gpsCoordinate);
                        //if (polygonCoordinates.Any() && !IsInsidePolygon(coordinate1[1], coordinate1[0], polygonCoordinates))
                        //{
                        //    continue;
                        //}

                        var jsonDataObject = new
                        {
                            type = "Feature",
                            geometry = new
                            {
                                type = "Polygon",
                                coordinates = new[]
                            {
                            new List<double[]>()
                            {
                                coordinate1, coordinate2, coordinate3, coordinate4
                            }
                        }
                            },
                            properties = new
                            {
                                id = patchId.ToString(),
                                file = fileName,
                                type = "Patch",
                                x = middleX,
                                y = middleY
                            }
                        };

                        string jsonData = JsonSerializer.Serialize(jsonDataObject);

                        var patch = new LCMS_Patch_Processed
                        {
                            SurveyId = surveyId,
                            SurveyDate = DateTime.Parse(surveyDate),
                            ImageFileIndex = fileName + ".jpg",
                            PatchId = (long)patchId,
                            Length_mm = patchlength,
                            Width_mm = patchwidth,
                            Area_m2 = area,
                            Severity = severity,
                            ConfidenceLevel = confidenceScore,
                            PavementType = pavement,
                            GPSLatitude = coordinate1[1],
                            GPSLongitude = coordinate1[0],
                            GPSAltitude = gpsCoordinate.Altitude,
                            GPSTrackAngle = gpsCoordinate.TrackAngle,
                            GeoJSON = jsonData,
                            RoundedGPSLatitude = Math.Round(coordinate1[1], 4),
                            RoundedGPSLongitude = Math.Round(coordinate1[0], 4),
                            SegmentId = Convert.ToInt32(sectionId),
                            Chainage = (middleY/1000 )+ chainage,
                            ChainageEnd = (maxY / 1000) + chainage
                        };

                        newpatch_Processed.Add(patch);
                    }
            }

            //Concrete Joint
            if (LcmsObjects.Any(x => x == LayerNames.ConcreteJoint))
            {
                //corner_Breaks
                if (!pavement.Equals("asphalt", StringComparison.OrdinalIgnoreCase))
                {
                    var spallingCorner = doc.SelectNodes("/LcmsAnalyserResults/ConcreteJointInformation/SpallingCornerList/Corner");
                    var cornerId = 0;
                    if (spallingCorner != null && spallingCorner.Count > 0)
                        foreach (XmlNode cornerItem in spallingCorner)
                        {
                            var X = GetDouble(GetValueFromNode(cornerItem, "X"));
                            var Y = GetDouble(GetValueFromNode(cornerItem, "Y"));

                            var Ymeter = Y / 1000;
                            double[] coordinate = ConvertToGPSCoordinates(X, Y, gpsCoordinate);
                            var quarter = cornerItem.SelectNodes("Quarter");

                            //if (polygonCoordinates.Any() && !IsInsidePolygon(coordinate[1], coordinate[0], polygonCoordinates))
                            //{
                            //    continue;
                            //}

                            if (quarter != null && quarter.Count > 0)
                                foreach (XmlNode quarterItem in quarter)
                                {
                                    var quarterId = GetDouble(GetValueFromNode(quarterItem, "QuarterIndex"));
                                    var avgDepth = GetDouble(GetValueFromNode(quarterItem, "AverageDepth"));
                                    var area = GetDouble(GetValueFromNode(quarterItem, "Area"));
                                    var breakArea = GetDouble(GetValueFromNode(quarterItem, "BreakArea"));
                                    var spallingArea = area - breakArea;
                                    var areaRatio = GetDouble(GetValueFromNode(quarterItem, "AreaRatio"));

                                    var jsonDataObject = new
                                    {
                                        type = "Feature",
                                        geometry = new
                                        {
                                            type = "Point",
                                            coordinates = coordinate
                                        },
                                        properties = new
                                        {
                                            id = cornerId.ToString(),
                                            file = fileName,
                                            type = "Corner Break",
                                            x = X,
                                            y = Y
                                        }
                                    }; ConcurrentBag<LCMS_Shove_Processed> newShove = new ConcurrentBag<LCMS_Shove_Processed>();
                                    string jsonData = JsonSerializer.Serialize(jsonDataObject);
                                    var cornerBreak = new LCMS_Corner_Break
                                    {
                                        SurveyId = surveyId,
                                        SurveyDate = DateTime.Parse(surveyDate),
                                        ImageFileIndex = fileName + ".jpg",
                                        CornerId = cornerId,
                                        QuarterId = (int)quarterId,
                                        AvgDepth_mm = avgDepth,
                                        Area_mm2 = area,
                                        BreakArea_mm2 = breakArea,
                                        CNR_SpallingArea_mm2 = spallingArea,
                                        AreaRatio = areaRatio,
                                        PavementType = pavement,
                                        GPSLatitude = coordinate[1],
                                        GPSLongitude = coordinate[0],
                                        GPSAltitude = gpsCoordinate.Altitude,
                                        GPSTrackAngle = gpsCoordinate.TrackAngle,
                                        GeoJSON = jsonData,
                                        RoundedGPSLatitude = Math.Round(coordinate[1], 4),
                                        RoundedGPSLongitude = Math.Round(coordinate[0], 4),
                                        SegmentId = Convert.ToInt32(sectionId),
                                        Chainage = Ymeter + chainage,
                                        ChainageEnd = (Y + Math.Sqrt(area) / 2) / 1000 + chainage
                                    };
                                    newcorner_Breaks.Add(cornerBreak);
                                }
                            cornerId++;
                        }
                }

                Log.Information($"STEP 5 ONE XML using parser for file: {path} ");


                //spalling_Raws
                if (!pavement.Equals("asphalt", StringComparison.OrdinalIgnoreCase))
                {
                    var concreteJoint = doc.SelectNodes("/LcmsAnalyserResults/ConcreteJointInformation/JointList/Joint");
                    if (concreteJoint != null && concreteJoint.Count > 0)
                    {
                        ProcessSpalling(concreteJoint, newspalling_Raws, gpsCoordinate, fileName, surveyId, surveyDate, pavement, "T", sectionId, chainage);
                    }
                    var verticalJoint = doc.SelectNodes("/LcmsAnalyserResults/ConcreteJointInformation/VerticalJointList/Joint");
                    if (verticalJoint != null && verticalJoint.Count > 0)
                    {
                        ProcessSpalling(verticalJoint, newspalling_Raws, gpsCoordinate, fileName, surveyId, surveyDate, pavement, "L", sectionId, chainage);
                    }
                }

                //concrete_Joints
                if (!pavement.Equals("asphalt", StringComparison.OrdinalIgnoreCase))
                {
                    var jointList = doc.SelectNodes("/LcmsAnalyserResults/ConcreteJointInformation/JointList/Joint");
                    var verticalJointList = doc.SelectNodes("/LcmsAnalyserResults/ConcreteJointInformation/VerticalJointList/Joint");

                    if (jointList != null && jointList.Count > 0)
                        ProcessJointListDoc(jointList, newconcrete_Joints, gpsCoordinate, fileName, surveyId, surveyDate, pavement, "T", sectionId, chainage);

                    if (verticalJointList != null && verticalJointList.Count > 0)
                        ProcessJointListDoc(verticalJointList, newconcrete_Joints, gpsCoordinate, fileName, surveyId, surveyDate, pavement, "L", sectionId, chainage);
                }
            }

            //Rutting
            if (LcmsObjects.Any(x => x == LayerNames.Rutting))
            {

                var rutMeasurement = doc.SelectNodes("/LcmsAnalyserResults/RutInformation/RutMeasurement");
                var rutData = new Dictionary<(double position, double index), dynamic>();

                if (rutMeasurement != null)
                {
                    foreach (XmlNode rutItem in rutMeasurement)
                    {
                        var position = GetDouble(GetValueFromNode(rutItem, "Position")); //y
                        var profileIndex = GetDouble(GetValueFromNode(rutItem, "ProfileIndex")); //x

                        var laneSide = rutItem.SelectSingleNode("LaneSide").InnerText;
                        var depth = GetDouble(GetValueFromNode(rutItem, "Depth"));
                        var width = GetDouble(GetValueFromNode(rutItem, "Width"));
                        var crossSection = GetDouble(GetValueFromNode(rutItem, "CrossSection"));
                        int type = GetInt(GetValueFromNode(rutItem, "Type"));
                        int method = GetInt(GetValueFromNode(rutItem, "Method"));
                        var percentDeformation = GetDouble(GetValueFromNode(rutItem, "PercentDeformation"));
                        int valid = GetInt(GetValueFromNode(rutItem, "Valid"));
                        var invalidRatioData = GetDouble(GetValueFromNode(rutItem, "InvalidRatioData"));

                        var key = (position, profileIndex);

                        if (!rutData.ContainsKey(key))
                        {
                            rutData[key] = new ExpandoObject();
                            rutData[key].LeftDepth = (double?)null;
                            rutData[key].LeftWidth = (double?)null;
                            rutData[key].LeftCrossSection = (double?)null;
                            rutData[key].LeftType = (int?)null;
                            rutData[key].LeftMethod = (int?)null;
                            rutData[key].LeftPercentDeformation = (double?)null;
                            rutData[key].LeftValid = (int?)null;
                            rutData[key].LeftInvalidRatioData = (double?)null;

                            rutData[key].RightDepth = (double?)null;
                            rutData[key].RightWidth = (double?)null;
                            rutData[key].RightCrossSection = (double?)null;
                            rutData[key].RightType = (int?)null;
                            rutData[key].RightMethod = (int?)null;
                            rutData[key].RightPercentDeformation = (double?)null;
                            rutData[key].RightValid = (int?)null;
                            rutData[key].RightInvalidRatioData = (double?)null;
                        }

                        dynamic data = rutData[key];

                        if (laneSide == "Left")
                        {
                            data.LeftDepth = depth;
                            data.LeftWidth = width;
                            data.LeftCrossSection = crossSection;
                            data.LeftType = type;
                            data.LeftMethod = method;
                            data.LeftPercentDeformation = percentDeformation;
                            data.LeftValid = valid;
                            data.LeftInvalidRatioData = invalidRatioData;
                        }
                        else if (laneSide == "Right")
                        {
                            data.RightDepth = depth;
                            data.RightWidth = width;
                            data.RightCrossSection = crossSection;
                            data.RightType = type;
                            data.RightMethod = method;
                            data.RightPercentDeformation = percentDeformation;
                            data.RightValid = valid;
                            data.RightInvalidRatioData = invalidRatioData;
                        }

                        // Update the dictionary with the modified data
                        rutData[key] = data;
                    }

                    var rutId = 1;

                    var centralBandWidthStr = doc.SelectSingleNode("/LcmsAnalyserResults/ProcessingInformation/ProcessingParameters/GeneralParam_CentralBandWidth_mm");
                    var centralBandWidth = GetDouble(centralBandWidthStr.InnerText);

                    var wheelPathWidthStr = doc.SelectSingleNode("/LcmsAnalyserResults/ProcessingInformation/ProcessingParameters/GeneralParam_WheelPathWidth_mm");
                    var wheelPathWidth = GetDouble(wheelPathWidthStr.InnerText);

                    var leftX = (x / 2) - (centralBandWidth / 2) - wheelPathWidth;
                    var rightX = (x / 2) + (centralBandWidth / 2) + wheelPathWidth;
                    var middleX = (leftX + rightX) / 2;

                    var ruttingIntervalNode = doc.SelectSingleNode("/LcmsAnalyserResults/ProcessingInformation/ProcessingParameters/RuttingModule_Parameters/RuttingModule_EvaluationInterval_m");
                    var ruttingInterval = ruttingIntervalNode != null ? GetDouble(ruttingIntervalNode.InnerText) * 1000 : 0; //convert meter to mm

                    foreach (var kvp in rutData)
                    {
                        var positionInMeter = kvp.Key.position - distanceBeginDouble;
                        var position = positionInMeter * 1000;
                        var profileIndex = kvp.Key.index; //x
                        dynamic data = kvp.Value;

                        //Coordinates for center point
                        //var coordinate = ConvertToGPSCoordinates(x / 2, position, gpsCoordinate);

                        //updated coord points to show on left and right rut
                        //using wheel path to show rut on the map
                        //double rutPath = x / 4;
                        //double[] leftCoordinate = ConvertToGPSCoordinates(rutPath, position, gpsCoordinate);

                        //var rightX = rutPath * 3;
                        //double[] rightCoordinate = ConvertToGPSCoordinates(rightX, position, gpsCoordinate);

                        double[] leftCoordinate1 = ConvertToGPSCoordinates(leftX, position, gpsCoordinate);
                        double[] leftCoordinate2 = ConvertToGPSCoordinates(leftX, position + ruttingInterval, gpsCoordinate);
                        double[] rightCoordinate1 = ConvertToGPSCoordinates(rightX, position, gpsCoordinate);
                        double[] rightCoordinate2 = ConvertToGPSCoordinates(rightX, position + ruttingInterval, gpsCoordinate);
                        double[] middleCoordinate1 = ConvertToGPSCoordinates(middleX, position, gpsCoordinate);
                        double[] middleCoordinate2 = ConvertToGPSCoordinates(middleX, position + ruttingInterval, gpsCoordinate);

                        //if (polygonCoordinates.Any() && !IsInsidePolygon(leftCoordinate1[1], leftCoordinate1[0], polygonCoordinates)
                        //    && !IsInsidePolygon(rightCoordinate1[1], rightCoordinate1[0], polygonCoordinates))
                        //{
                        //    continue;
                        //}

                        var leftJsonDataObject = new
                        {
                            type = "Feature",
                            geometry = new
                            {
                                type = "Polyline",
                                coordinates = new[] { leftCoordinate1, leftCoordinate2 }
                            },
                            properties = new
                            {
                                id = rutId,
                                file = fileName,
                                type = "Left Rut"
                            }
                        };


                        var rightJsonDataObject = new
                        {
                            type = "Feature",
                            geometry = new
                            {
                                type = "Polyline",
                                coordinates = new[] { rightCoordinate1, rightCoordinate2 }
                            },
                            properties = new
                            {
                                id = rutId,
                                file = fileName,
                                type = "Right Rut"
                            }
                        };


                        var laneJsonDataObject = new
                        {
                            type = "Feature",
                            geometry = new
                            {
                                type = "Polyline",
                                coordinates = new[] { middleCoordinate1, middleCoordinate2 }
                            },
                            properties = new
                            {
                                id = rutId,
                                file = fileName,
                                type = "Lane Rut"
                            }
                        };

                        string leftJsonData = JsonSerializer.Serialize(leftJsonDataObject);
                        string rightJsonData = JsonSerializer.Serialize(rightJsonDataObject);
                        string laneJsonData = JsonSerializer.Serialize(laneJsonDataObject);

                        var rut = new LCMS_Rut_Processed
                        {
                            SurveyId = surveyId,
                            SurveyDate = DateTime.Parse(surveyDate),
                            ImageFileIndex = fileName + ".jpg",
                            RutId = rutId,
                            LeftDepth_mm = data.LeftDepth,
                            LeftWidth_mm = data.LeftWidth,
                            LeftCrossSection = data.LeftCrossSection,
                            LeftType = data.LeftType,
                            LeftMethod = data.LeftMethod,
                            LeftValid = data.LeftValid,
                            LeftInvalidRatioData = data.LeftInvalidRatioData,
                            LeftPercentDeformation = data.LeftPercentDeformation,
                            RightDepth_mm = data.RightDepth,
                            RightWidth_mm = data.RightWidth,
                            RightCrossSection = data.RightCrossSection,
                            RightType = data.RightType,
                            RightMethod = data.RightMethod,
                            RightValid = data.RightValid,
                            RightInvalidRatioData = data.RightInvalidRatioData,
                            RightPercentDeformation = data.RightPercentDeformation,
                            LaneDepth_mm = (data.LeftDepth + data.RightDepth) / 2,
                            LaneWidth_mm = (data.LeftWidth + data.RightWidth) / 2,
                            PavementType = pavement,
                            GPSLatitude = middleCoordinate1[1],
                            GPSLongitude = middleCoordinate1[0],
                            GPSAltitude = gpsCoordinate.Altitude,
                            GPSTrackAngle = gpsCoordinate.TrackAngle,
                            GeoJSON = laneJsonData,
                            LwpGeoJSON = leftJsonData,
                            RwpGeoJSON = rightJsonData,
                            RoundedGPSLatitude = Math.Round(gpsCoordinate.Latitude, 4),
                            RoundedGPSLongitude = Math.Round(gpsCoordinate.Longitude, 4),
                            SegmentId = Convert.ToInt32(sectionId),
                            EndGPSLatitude = middleCoordinate2[1],
                            EndGPSLongitude = middleCoordinate2[0],
                            Chainage = kvp.Key.position,
                            ChainageEnd = kvp.Key.position + ruttingInterval
                        };
                        newRut.Add(rut);
                        rutId++;
                    }
                }
            }

            //Bleeding
            if (LcmsObjects.Any(x => x == LayerNames.Bleeding))
            {
                var bleedingLeftWheel = doc.SelectNodes("/LcmsAnalyserResults/BleedingInformation/LeftWheelPath/BleedingData");
                var bleedingRightWheel = doc.SelectNodes("/LcmsAnalyserResults/BleedingInformation/RightWheelPath/BleedingData");

                var leftBleedingData = new Dictionary<(double Bottom, double Top), dynamic>();
                var rightBleedingData = new Dictionary<(double Bottom, double Top), dynamic>();

                //Left wheel bleeding
                foreach (XmlNode bleedingItem in bleedingLeftWheel)
                {
                    double leftBleedingIndex = GetDouble(GetValueFromNode(bleedingItem, "BleedingIndex"));
                    double severityNumber = GetDouble(GetValueFromNode(bleedingItem, "BleedingSeverity"));
                    var leftBleedingSeverity = DetermineBleedingSeverity(severityNumber);

                    double bboxLeft = GetDouble(GetValueFromNode(bleedingItem, "BBox/Left"));
                    double bboxRight = GetDouble(GetValueFromNode(bleedingItem, "BBox/Right"));
                    double bboxBottom = GetDouble(GetValueFromNode(bleedingItem, "BBox/Bottom"));
                    double bboxTop = GetDouble(GetValueFromNode(bleedingItem, "BBox/Top"));

                    var key = (bboxBottom, bboxTop);

                    leftBleedingData[key] = new ExpandoObject();
                    leftBleedingData[key].LeftBBoxLeft = bboxLeft;
                    leftBleedingData[key].LeftBBoxRight = bboxRight;
                    leftBleedingData[key].LeftBleedingIndex = leftBleedingIndex;
                    leftBleedingData[key].LeftBleedingSeverity = leftBleedingSeverity;
                    leftBleedingData[key].LeftArea_m2 = severityNumber > 0 ? Math.Round((double)(((bboxRight - bboxLeft) * (bboxTop - bboxBottom)) / (1000 * 1000)), 4) : 0;//width * height, calculate area if severity found
                }

                //Right wheel bleeding
                foreach (XmlNode bleedingItem in bleedingRightWheel)
                {
                    double rightBleedingIndex = GetDouble(GetValueFromNode(bleedingItem, "BleedingIndex"));
                    double severityNumber = GetDouble(GetValueFromNode(bleedingItem, "BleedingSeverity"));
                    var rightBleedingSeverity = DetermineBleedingSeverity(severityNumber);

                    double bboxLeft = GetDouble(GetValueFromNode(bleedingItem, "BBox/Left"));
                    double bboxRight = GetDouble(GetValueFromNode(bleedingItem, "BBox/Right"));
                    double bboxBottom = GetDouble(GetValueFromNode(bleedingItem, "BBox/Bottom"));
                    double bboxTop = GetDouble(GetValueFromNode(bleedingItem, "BBox/Top"));

                    var key = (bboxBottom, bboxTop);

                    rightBleedingData[key] = new ExpandoObject();
                    rightBleedingData[key].RightBBoxLeft = bboxLeft;
                    rightBleedingData[key].RightBBoxRight = bboxRight;
                    rightBleedingData[key].RightBleedingIndex = rightBleedingIndex;
                    rightBleedingData[key].RightBleedingSeverity = rightBleedingSeverity;
                    rightBleedingData[key].RightArea_m2 = severityNumber > 0 ? Math.Round((double)(((bboxRight - bboxLeft) * (bboxTop - bboxBottom)) / (1000 * 1000)), 4) : 0;//width * height, calculate area if severity found
                }

                var bleedingId = 1;
                // Pair and save bleeding data
                foreach (var leftPair in leftBleedingData)
                {
                    var bottomTopKey = leftPair.Key;

                    if (rightBleedingData.TryGetValue(bottomTopKey, out var rightData))
                    {
                        var leftData = leftPair.Value;

                        //Skip if both severities are no bleeding
                        if (leftData.LeftBleedingSeverity == "No Bleeding" && rightData.RightBleedingSeverity == "No Bleeding")
                            continue;

                        // Create coordinate pairs for left and right bleeding
                        double[][] leftCoordinates = new double[][]
                        {
                        new double[] { leftData.LeftBBoxLeft, bottomTopKey.Bottom },
                        new double[] { leftData.LeftBBoxRight, bottomTopKey.Bottom },
                        new double[] { leftData.LeftBBoxRight, bottomTopKey.Top },
                        new double[] { leftData.LeftBBoxLeft, bottomTopKey.Top }
                        };

                        double[][] rightCoordinates = new double[][]
                        {
                        new double[] { rightData.RightBBoxLeft, bottomTopKey.Bottom },
                        new double[] { rightData.RightBBoxRight, bottomTopKey.Bottom },
                        new double[] { rightData.RightBBoxRight, bottomTopKey.Top },
                        new double[] { rightData.RightBBoxLeft, bottomTopKey.Top }
                        };

                        List<double[]> leftGPSCoordinates = new List<double[]>();
                        List<double[]> rightGPSCoordinates = new List<double[]>();

                        foreach (var leftCoord in leftCoordinates)
                        {
                            double leftX = leftCoord[0];
                            double leftY = leftCoord[1];
                            double[] gpsCoord = ConvertToGPSCoordinates(leftX, leftY, gpsCoordinate);
                            leftGPSCoordinates.Add(gpsCoord);
                        }

                        foreach (var rightCoord in rightCoordinates)
                        {
                            double rightX = rightCoord[0];
                            double rightY = rightCoord[1];
                            double[] gpsCoord = ConvertToGPSCoordinates(rightX, rightY, gpsCoordinate);
                            rightGPSCoordinates.Add(gpsCoord);
                        }


                        //if (polygonCoordinates.Any() && !IsInsidePolygon(leftGPSCoordinates[0][1], leftGPSCoordinates[0][0], polygonCoordinates) &&
                        //    !IsInsidePolygon(rightGPSCoordinates[0][1], rightGPSCoordinates[0][0], polygonCoordinates))
                        //{
                        //    continue;
                        //}

                        var jsonDataObject = new
                        {
                            type = "Feature",
                            geometry = new
                            {
                                type = "MultiPolygon",
                                coordinates = new[] { leftGPSCoordinates, rightGPSCoordinates }
                            },
                            properties = new
                            {
                                id = bleedingId.ToString(),
                                file = fileName,
                                type = "Bleeding"
                            }
                        };
                        string jsonData = JsonSerializer.Serialize(jsonDataObject);

                        var bleed = new LCMS_Bleeding
                        {
                            SurveyId = surveyId,
                            SurveyDate = DateTime.Parse(surveyDate),
                            PavementType = pavement,
                            BleedingId = bleedingId,
                            LeftBleedingIndex = leftData.LeftBleedingIndex,
                            LeftSeverity = leftData.LeftBleedingSeverity,
                            RightBleedingIndex = rightData.RightBleedingIndex,
                            RightSeverity = rightData.RightBleedingSeverity,
                            ImageFileIndex = fileName + ".jpg",
                            GPSLatitude = leftGPSCoordinates[0][1],
                            GPSLongitude = leftGPSCoordinates[0][0],
                            GPSRightLatitude = rightGPSCoordinates[0][1],
                            GPSRightLongitude = rightGPSCoordinates[0][0],
                            GPSAltitude = gpsCoordinate.Altitude,
                            GPSTrackAngle = gpsCoordinate.TrackAngle,
                            GeoJSON = jsonData,
                            RoundedGPSLatitude = Math.Round(leftGPSCoordinates[0][1], 4),
                            RoundedGPSLongitude = Math.Round(leftGPSCoordinates[0][0], 4),
                            SegmentId = Convert.ToInt32(sectionId),
                            Area_m2 = leftData.LeftArea_m2 + rightData.RightArea_m2, 
                            Chainage = chainage,
                            ChainageEnd = chainage + bottomTopKey.Top,
                            LeftArea_m2 = leftData.LeftArea_m2,
                            RightArea_m2 = rightData.RightArea_m2
                        };
                        newBleeding.Add(bleed);
                        bleedingId++;
                    }
                }
            }

            //No need to show it on the map
            if (LcmsObjects.Any(x => x == LayerNames.LaneMarking))
            {
                var laneMarkData = doc.SelectNodes("/LcmsAnalyserResults/LaneMarkInformation/LaneMark");

                // Initialize variables for storing the values
                double leftLength = 0.0, rightLength = 0.0;
                double leftPos = 0.0, rightPos = 0.0;
                int leftType = 0, rightType = 0;
                double laneWidth = 0.0;
                foreach (XmlNode laneMarkItem in laneMarkData)
                {
                    var laneSide = laneMarkItem.SelectSingleNode("LaneSide").InnerText;
                    var position = GetDouble(GetValueFromNode(laneMarkItem, "Position"));
                    var lengthMeter = GetDouble(GetValueFromNode(laneMarkItem, "Length"));
                    var type = GetInt(GetValueFromNode(laneMarkItem, "Type"));

                    if (laneSide == "Left")
                    {
                        leftLength = lengthMeter * 1000; //Convert to mm
                        leftType = type;
                        leftPos = position;
                    }
                    else if (laneSide == "Right")
                    {
                        rightLength = lengthMeter * 1000; //Convert to mm
                        rightType = type;
                        rightPos = position;
                    }
                }

                string leftTypeDescription = ConvertTypeToDescription(leftType);
                string rightTypeDescription = ConvertTypeToDescription(rightType);
                laneWidth = rightPos - leftPos;
                var laneMark = new LCMS_Lane_Mark_Processed
                {
                    SurveyId = surveyId,
                    SurveyDate = DateTime.Parse(surveyDate),
                    ImageFileIndex = fileName + ".jpg",
                    PavementType = pavement,
                    LeftLength_mm = leftLength,
                    LeftType = leftTypeDescription,
                    RightLength_mm = rightLength,
                    RightType = rightTypeDescription,
                    GPSLatitude = gpsCoordinate.Latitude,
                    GPSLongitude = gpsCoordinate.Longitude,
                    GPSAltitude = gpsCoordinate.Altitude,
                    GPSTrackAngle = gpsCoordinate.TrackAngle,
                    GeoJSON = string.Empty,
                    RoundedGPSLatitude = Math.Round(gpsCoordinate.Latitude, 4),
                    RoundedGPSLongitude = Math.Round(gpsCoordinate.Longitude, 4),
                    SegmentId = Convert.ToInt32(sectionId),
                    Chainage = chainage,
                    ChainageEnd = chainage + Math.Max(leftPos + (leftLength / 1000), rightPos + (rightLength / 1000)),
                    LaneWidth = laneWidth
                };
                newLaneMark.Add(laneMark);
            }

            if (LcmsObjects.Any(x => x == LayerNames.Pumping))
            {
                var pumpingData = doc.SelectNodes("/LcmsAnalyserResults/PumpingDetectionInformation/PumpingData");
                foreach (XmlNode pumpingItem in pumpingData)
                {
                    var pumpingId = GetInt(GetValueFromNode(pumpingItem, "PumpingID"));
                    var area = GetDouble(GetValueFromNode(pumpingItem, "Area"));
                    var confidenceScore = GetDouble(GetValueFromNode(pumpingItem, "ConfidenceScore"));

                    var minX = GetDouble(GetValueFromNode(pumpingItem, "BoundingBox/MinX"));
                    var maxX = GetDouble(GetValueFromNode(pumpingItem, "BoundingBox/MaxX"));
                    var minY = GetDouble(GetValueFromNode(pumpingItem, "BoundingBox/MinY"));
                    var maxY = GetDouble(GetValueFromNode(pumpingItem, "BoundingBox/MaxY"));

                    var middleX = (minX + maxX) / 2;
                    var middleY = (minY + maxY) / 2;

                    var width = maxX - minX;
                    var length = maxY - minY;

                    var coordinate1 = ConvertToGPSCoordinates(minX, maxY, gpsCoordinate);
                    var coordinate2 = ConvertToGPSCoordinates(maxX, maxY, gpsCoordinate);
                    var coordinate3 = ConvertToGPSCoordinates(maxX, minY, gpsCoordinate);
                    var coordinate4 = ConvertToGPSCoordinates(minX, minY, gpsCoordinate);

                    //if (polygonCoordinates.Any() && !IsInsidePolygon(coordinate1[1], coordinate1[0], polygonCoordinates))
                    //{
                    //    continue;
                    //}

                    var jsonDataObject = new
                    {
                        type = "Feature",
                        geometry = new
                        {
                            type = "Polygon",
                            coordinates = new[]
                        {
                            new List<double[]>()
                            {
                                coordinate1, coordinate2, coordinate3, coordinate4
                            }
                        }
                        },
                        properties = new
                        {
                            id = pumpingId.ToString(),
                            file = fileName,
                            type = "Pumping",
                            x = middleX,
                            y = middleY
                        }
                    };

                    string jsonData = JsonSerializer.Serialize(jsonDataObject);

                    var pumping = new LCMS_Pumping_Processed
                    {
                        SurveyId = surveyId,
                        SurveyDate = DateTime.Parse(surveyDate),
                        ImageFileIndex = fileName + ".jpg",
                        PumpingId = pumpingId,
                        Area_m2 = area,
                        Width_mm = width,
                        Length_mm = length,
                        ConfidenceLevel = confidenceScore,
                        PavementType = pavement,
                        GPSLatitude = coordinate1[1],
                        GPSLongitude = coordinate1[0],
                        GPSAltitude = gpsCoordinate.Altitude,
                        GPSTrackAngle = gpsCoordinate.TrackAngle,
                        GeoJSON = jsonData,
                        RoundedGPSLatitude = Math.Round(coordinate1[1], 4),
                        RoundedGPSLongitude = Math.Round(coordinate1[0], 4),
                        SegmentId = Convert.ToInt32(sectionId),
                        Chainage = (middleY / 1000)  +  chainage,
                        ChainageEnd = (maxY / 1000) + chainage

                    };
                    newPumping.Add(pumping);
                }
            }

            if (LcmsObjects.Any(x => x == LayerNames.SealedCrack))
            {
                var sealedCrackData = doc.SelectNodes("/LcmsAnalyserResults/SealedCrackInformation/SealedCrackPerimeters/SealedCrack");
                var skeletonData = doc.SelectNodes("/LcmsAnalyserResults/SealedCrackInformation/SealedCrackSkeletonList/SkeletonComponent");

                foreach (XmlNode sealedCrackItem in sealedCrackData)
                {
                    var sealedCrackId = GetInt(GetValueFromNode(sealedCrackItem, "SealedCrackID"));
                    var minY = GetDouble(GetValueFromNode(sealedCrackItem, "BoundingBox/MinY"));
                    var maxY = GetDouble(GetValueFromNode(sealedCrackItem, "BoundingBox/MaxY"));
                    var length = maxY - minY;
                    var smoothnessInside = GetDouble(GetValueFromNode(sealedCrackItem, "SmoothnessInside"));
                    var smoothnessOutside = GetDouble(GetValueFromNode(sealedCrackItem, "SmoothnessOutside"));
                    var avgIntensity = GetInt(GetValueFromNode(sealedCrackItem, "AvgIntensity"));
                    var avgIntensityOutside = GetInt(GetValueFromNode(sealedCrackItem, "AvgIntensityOutside"));
                    var crackAreaRatio = GetDouble(GetValueFromNode(sealedCrackItem, "CrackAreaRatio"));
                    var area = GetDouble(GetValueFromNode(sealedCrackItem, "Area"));
                    var avgWidth = GetDouble(GetValueFromNode(sealedCrackItem, "AvgWidth"));

                    XmlNode matchingSkeletonComponent = null;

                    foreach (XmlNode skeletonItem in skeletonData)
                    {
                        var skeletonId = GetInt(GetValueFromNode(skeletonItem, "ID"));
                        if (skeletonId == sealedCrackId)
                        {
                            matchingSkeletonComponent = skeletonItem;
                            break;
                        }
                    }
                    if (matchingSkeletonComponent == null)
                    {
                        continue; // Skip if no matching skeleton is found
                    }
                    List<double[]> coordinates = new List<double[]>();
                    var nodes = matchingSkeletonComponent.SelectNodes("Node");
                    var middleNode = nodes.Count / 2;
                    var nodeCount = 0;
                    double middleX = 0.0, middleY = 0.0;
                    foreach (XmlNode node in nodes)
                    {
                        var X = GetDouble(GetValueFromNode(node, "X"));
                        var Y = GetDouble(GetValueFromNode(node, "Y"));
                        if (nodeCount == middleNode)
                        {
                            middleX = X;
                            middleY = Y;
                        }
                        var coordinate = ConvertToGPSCoordinates(X, Y, gpsCoordinate);
                        coordinates.Add(coordinate);
                        nodeCount++;
                    }
                    //if (polygonCoordinates.Any() && !IsInsidePolygon(coordinates[0][1], coordinates[0][0], polygonCoordinates))
                    //{
                    //    continue;
                    //}

                    var jsonDataObject = new
                    {
                        type = "Feature",
                        geometry = new
                        {
                            type = "Polyline",
                            coordinates = coordinates
                        },
                        properties = new
                        {
                            id = sealedCrackId.ToString(),
                            file = fileName,
                            type = "Sealed Crack",
                            x = middleX,
                            y = middleY
                        }
                    };

                    string jsonData = JsonSerializer.Serialize(jsonDataObject);

                    var firstCoordinate = coordinates.First();
                    var lastCoordinate = coordinates.Last();

                    var sealedCrack = new LCMS_Sealed_Cracks
                    {
                        SurveyId = surveyId,
                        SurveyDate = DateTime.Parse(surveyDate),
                        ImageFileIndex = fileName + ".jpg",
                        SealedCrackId = sealedCrackId,
                        Length_mm = length,
                        SmoothnessInside = smoothnessInside,
                        SmoothnessOutside = smoothnessOutside,
                        AvgIntensity = avgIntensity,
                        AvgIntensityOutside = avgIntensityOutside,
                        CrackAreaRatio = crackAreaRatio,
                        Area_m2 = area,
                        AvgWidth_mm = avgWidth,
                        PavementType = pavement,
                        GPSLatitude = firstCoordinate[1],
                        GPSLongitude = firstCoordinate[0],
                        GPSAltitude = gpsCoordinate.Altitude,
                        GPSTrackAngle = gpsCoordinate.TrackAngle,
                        GeoJSON = jsonData,
                        RoundedGPSLatitude = Math.Round(firstCoordinate[1], 4),
                        RoundedGPSLongitude = Math.Round(firstCoordinate[0], 4),
                        SegmentId = Convert.ToInt32(sectionId),
                        EndGPSLatitude = lastCoordinate[1],
                        EndGPSLongitude = lastCoordinate[0],
                        Chainage = (minY/1000) + chainage,
                        ChainageEnd = (maxY / 1000) + chainage
                    };
                    newSealedCrack.Add(sealedCrack);
                }

            }

            if (LcmsObjects.Any(x => x == LayerNames.WaterEntrapment))
            {
                var waterTrapInfo = doc.SelectSingleNode("/LcmsAnalyserResults/WaterTrapInformation");
                if (waterTrapInfo != null && waterTrapInfo.HasChildNodes)
                {
                    var waterTrapMeasurements = waterTrapInfo.SelectNodes("WaterTrapMeasurement");

                    var curId = 0;
                    foreach (XmlNode curMeasurements in waterTrapMeasurements)
                    {
                        var position = GetDouble(GetValueFromNode(curMeasurements, "Position")) - distanceBeginDouble; //Y value
                        var profileIndex = GetDouble(GetValueFromNode(curMeasurements, "ProfileIndex")); // Don't know what to do

                        var waterTraps = curMeasurements.SelectNodes("WaterTrap");
                        var polygons = new List<List<double[]>>();

                        for (int i = 0; i < waterTraps.Count; i++)
                        {
                            var polygon = new List<double[]>();
                            var waterTrap = waterTraps[i];

                            var waterDepth = GetDouble(GetValueFromNode(waterTrap, "WaterDepth"));
                            var waterWidth = GetDouble(GetValueFromNode(waterTrap, "WaterWidth"));
                            var crossSection = GetDouble(GetValueFromNode(waterTrap, "CrossSection"));

                            var straightEdgePoint1X = GetDouble(GetValueFromNode(waterTrap, "StraightEdgeCoords/Point1/X"));
                            var straightEdgePoint1Z = GetDouble(GetValueFromNode(waterTrap, "StraightEdgeCoords/Point1/Z"));
                            var straightEdgePoint2X = GetDouble(GetValueFromNode(waterTrap, "StraightEdgeCoords/Point2/X"));
                            var straightEdgePoint2Z = GetDouble(GetValueFromNode(waterTrap, "StraightEdgeCoords/Point2/Z"));

                            var midLinePoint1X = GetDouble(GetValueFromNode(waterTrap, "MidLineCoords/Point1/X"));
                            var midLinePoint1Z = GetDouble(GetValueFromNode(waterTrap, "MidLineCoords/Point1/Z"));
                            var midLinePoint2X = GetDouble(GetValueFromNode(waterTrap, "MidLineCoords/Point2/X"));
                            var midLinePoint2Z = GetDouble(GetValueFromNode(waterTrap, "MidLineCoords/Point2/Z"));

                            var coordinate1 = ConvertToGPSCoordinates(midLinePoint1X + straightEdgePoint1X, position, gpsCoordinate); //Left Straight
                            var coordinate2 = ConvertToGPSCoordinates(midLinePoint1X + straightEdgePoint2X, position, gpsCoordinate);//Right Straight
                            var coordinate3 = ConvertToGPSCoordinates(midLinePoint1X, position, gpsCoordinate);

                            polygon.Add(coordinate1);
                            polygon.Add(coordinate2);
                            polygon.Add(coordinate3);

                            polygons.Add(polygon);


                            var jsonDataObject = new
                            {
                                type = "Feature",
                                geometry = new
                                {
                                    type = "MultiPolygon",
                                    coordinates = polygons.ToArray()
                                },
                                properties = new
                                {
                                    id = curId.ToString(),
                                    file = fileName,
                                    type = "Water Entrapment"
                                }
                            };
                            string jsonData = JsonSerializer.Serialize(jsonDataObject);

                            var waterTrapment = new LCMS_Water_Entrapment
                            {
                                SurveyId = surveyId,
                                SurveyDate = DateTime.Parse(surveyDate),
                                PavementType = pavement,
                                dLeftAverageWaterDepth = 0,
                                dLeftTotalWaterWidth = 0,
                                dRightAverageWaterDepth = 0,
                                dRightTotalWaterWidth = 0,
                                dWaterTrapDepth = waterDepth,
                                dWaterTrapWidth = waterWidth,
                                dCrossSection = crossSection,
                                dStraightEdgeCoordsPoint1X = straightEdgePoint1X,
                                dStraightEdgeCoordsPoint1Z = straightEdgePoint1Z,
                                dStraightEdgeCoordsPoint2X = straightEdgePoint2X,
                                dStraightEdgeCoordsPoint2Z = straightEdgePoint2Z,
                                dMidLineCoordsPoint1X = midLinePoint1X,
                                dMidLineCoordsPoint1Z = midLinePoint1Z,
                                dMidLineCoordsPoint2X = midLinePoint2X,
                                dMidLineCoordsPoint2Z = midLinePoint2Z,
                                GPSLatitude = polygons[0][0][1],
                                GPSLongitude = polygons[0][0][0],
                                GPSAltitude = gpsCoordinate.Altitude,
                                GPSTrackAngle = gpsCoordinate.TrackAngle,
                                GeoJSON = jsonData,
                                RoundedGPSLatitude = Math.Round(polygons[0][0][1], 4),
                                RoundedGPSLongitude = Math.Round(polygons[0][0][0], 4),
                                SegmentId = Convert.ToInt32(sectionId),
                                Chainage = chainage + position,
                                ChainageEnd = chainage + position + waterWidth
                            };
                            curId++;
                            newWaterTrapment.Add(waterTrapment);
                        }
                    }
                }

            }

            if (LcmsObjects.Any(x => x == LayerNames.MarkingContour))
            {
                var markingContourData = doc.SelectNodes("/LcmsAnalyserResults/MarkingContourInformation/MarkingContour");
                var blackMarkingContour = doc.SelectNodes("/LcmsAnalyserResults/MarkingContourInformation/BlackMarkingContour");

                //Bright Marking 
                foreach (XmlNode markingItem in markingContourData)
                {
                    var markingId = GetInt(GetValueFromNode(markingItem, "MarkingID"));
                    var area = GetDouble(GetValueFromNode(markingItem, "Area"));
                    var avgIntensity = GetDouble(GetValueFromNode(markingItem, "AvgIntensity"));

                    var perimeter = markingItem.SelectSingleNode("Perimeter");
                    if (perimeter != null && perimeter.HasChildNodes)
                    {
                        List<double[]> coordinates = new List<double[]>();
                        List<double> Ys = new List<double>();
                        var nodes = perimeter.SelectNodes("Node");

                        foreach (XmlNode node in nodes)
                        {
                            var X = GetDouble(GetValueFromNode(node, "X"));
                            var Y = GetDouble(GetValueFromNode(node, "Y"));
                            var coordinate = ConvertToGPSCoordinates(X, Y, gpsCoordinate);
                            coordinates.Add(coordinate);

                            Ys.Add(Y);

                        }

                        //if (polygonCoordinates.Any() && !IsInsidePolygon(coordinates[0][1], coordinates[0][0], polygonCoordinates))
                        //{
                        //    continue;
                        //}

                        if (coordinates.Count > 0)
                        {
                            var jsonDataObject = new
                            {
                                type = "Feature",
                                geometry = new
                                {
                                    type = "Polygon",
                                    coordinates = new[]
                                    {
                                    coordinates
                                }
                                },
                                properties = new
                                {
                                    id = markingId.ToString(),
                                    file = fileName,
                                    type = "Marking Contour"
                                }
                            };

                            string jsonData = JsonSerializer.Serialize(jsonDataObject);

                            var markingContour = new LCMS_Marking_Contour
                            {
                                SurveyId = surveyId,
                                SurveyDate = DateTime.Parse(surveyDate),
                                ImageFileIndex = fileName + ".jpg",
                                MarkingId = markingId,
                                Area_m2 = area,
                                AvgIntensity = avgIntensity,
                                Type = "Bright",
                                PavementType = pavement,
                                GPSLatitude = coordinates[0][1],
                                GPSLongitude = coordinates[0][0],
                                GPSAltitude = gpsCoordinate.Altitude,
                                GPSTrackAngle = gpsCoordinate.TrackAngle,
                                GeoJSON = jsonData,
                                RoundedGPSLatitude = Math.Round(coordinates[0][1], 4),
                                RoundedGPSLongitude = Math.Round(coordinates[0][0], 4),
                                SegmentId = Convert.ToInt32(sectionId),
                                Chainage =  Ys[0] + chainage,
                                ChainageEnd = Ys.Max() + chainage
                            };
                            newMarkingContour.Add(markingContour);
                        }
                    }
                }

                //Black Marking 
                foreach (XmlNode blackMarkingItem in blackMarkingContour)
                {
                    var markingId = GetInt(GetValueFromNode(blackMarkingItem, "BlackMarkingID"));
                    var area = GetDouble(GetValueFromNode(blackMarkingItem, "Area"));
                    var avgIntensity = GetDouble(GetValueFromNode(blackMarkingItem, "AvgIntensity"));


                    var perimeter = blackMarkingItem.SelectSingleNode("Perimeter");
                    if (perimeter != null && perimeter.HasChildNodes)
                    {
                        List<double[]> coordinates = new List<double[]>();
                        List<double> Ys = new List<double>();
                        var nodes = perimeter.SelectNodes("Node");
                        foreach (XmlNode node in nodes)
                        {
                            var X = GetDouble(GetValueFromNode(node, "X"));
                            var Y = GetDouble(GetValueFromNode(node, "Y"));

                            var coordinate = ConvertToGPSCoordinates(X, Y, gpsCoordinate);
                            coordinates.Add(coordinate);
                            Ys.Add(Y);

                        }

                        //if (polygonCoordinates.Any() && !IsInsidePolygon(coordinates[0][1], coordinates[0][0], polygonCoordinates))
                        //{
                        //    continue;
                        //}

                        if (coordinates.Count > 0)
                        {
                            var jsonDataObject = new
                            {
                                type = "Feature",
                                geometry = new
                                {
                                    type = "Polygon",
                                    coordinates = new[]
                                    {
                                    coordinates
                                }
                                },
                                properties = new
                                {
                                    id = markingId.ToString(),
                                    file = fileName,
                                    type = "Marking Contour"
                                }
                            };

                            string jsonData = JsonSerializer.Serialize(jsonDataObject);

                            var markingContour = new LCMS_Marking_Contour
                            {
                                SurveyId = surveyId,
                                SurveyDate = DateTime.Parse(surveyDate),
                                ImageFileIndex = fileName + ".jpg",
                                MarkingId = markingId,
                                Area_m2 = area,
                                AvgIntensity = avgIntensity,
                                Type = "Black",
                                PavementType = pavement,
                                GPSLatitude = coordinates[0][1],
                                GPSLongitude = coordinates[0][0],
                                GPSAltitude = gpsCoordinate.Altitude,
                                GPSTrackAngle = gpsCoordinate.TrackAngle,
                                GeoJSON = jsonData,
                                RoundedGPSLatitude = Math.Round(coordinates[0][1], 4),
                                RoundedGPSLongitude = Math.Round(coordinates[0][0], 4),
                                SegmentId = Convert.ToInt32(sectionId),
                                Chainage = Ys[0] + chainage,
                                ChainageEnd = Ys.Max() + chainage
                            };
                            newMarkingContour.Add(markingContour);
                        }
                    }
                }
            }

            if (LcmsObjects.Any(x => x == LayerNames.MMO))
            {
                var manMadeObjectData = doc.SelectSingleNode("/LcmsAnalyserResults/ManMadeObjectInformation");
                if (manMadeObjectData != null)
                {
                    foreach (XmlNode manMadeObject in manMadeObjectData.ChildNodes)
                    {
                        if (manMadeObject.Name == "DataFormat" || manMadeObject.Name == "Method" || manMadeObject.Name == "Unit")
                        {
                            continue; // Skip these general info nodes
                        }

                        var objectType = manMadeObject.Name;
                        var objectId = GetInt(GetValueFromNode(manMadeObject, $"{objectType}ID"));

                        //unit mm
                        var minX = GetDouble(GetValueFromNode(manMadeObject, "BoundingBox/MinX"));
                        var maxX = GetDouble(GetValueFromNode(manMadeObject, "BoundingBox/MaxX"));
                        var minY = GetDouble(GetValueFromNode(manMadeObject, "BoundingBox/MinY"));
                        var maxY = GetDouble(GetValueFromNode(manMadeObject, "BoundingBox/MaxY"));

                        var middleX = (minX + maxX) / 2;
                        var middleY = (minY + maxY) / 2;

                        var coordinate1 = ConvertToGPSCoordinates(minX, maxY, gpsCoordinate);
                        var coordinate2 = ConvertToGPSCoordinates(maxX, maxY, gpsCoordinate);
                        var coordinate3 = ConvertToGPSCoordinates(maxX, minY, gpsCoordinate);
                        var coordinate4 = ConvertToGPSCoordinates(minX, minY, gpsCoordinate);

                        var widthValue = GetDouble(GetValueFromNode(manMadeObject, "Width")) * 1000; //Convert to mm
                        var heightValue = GetDouble(GetValueFromNode(manMadeObject, "Height")) * 1000;

                        var width = widthValue != 0 ? widthValue : (maxX - minX);
                        var height = heightValue != 0 ? heightValue : (maxY - minY);

                        var area = GetDouble(GetValueFromNode(manMadeObject, "Area"));
                        var avgHeight = GetDouble(GetValueFromNode(manMadeObject, "AverageHeight")) * 1000;
                        var score = GetDouble(GetValueFromNode(manMadeObject, "Score"));

                        //if (polygonCoordinates.Any() && !IsInsidePolygon(coordinate1[1], coordinate1[0], polygonCoordinates))
                        //{
                        //    continue;
                        //}

                        var jsonDataObject = new
                        {
                            type = "Feature",
                            geometry = new
                            {
                                type = "Polygon",
                                coordinates = new[]
                                {
                                new List<double[]>()
                                {
                                    coordinate1, coordinate2, coordinate3, coordinate4
                                }
                            }
                            },
                            properties = new
                            {
                                id = objectId.ToString(),
                                file = fileName,
                                type = "MMO",
                                x = middleX,
                                y = middleY
                            }
                        };
                        string jsonData = JsonSerializer.Serialize(jsonDataObject);

                        var mmo = new LCMS_MMO_Processed
                        {
                            SurveyId = surveyId,
                            SurveyDate = DateTime.Parse(surveyDate),
                            ImageFileIndex = fileName + ".jpg",
                            PavementType = pavement,
                            MMOId = objectId,
                            Type = objectType,
                            Area_m2 = area,
                            Width_mm = width,
                            Height_mm = height,
                            AvgHeight_mm = avgHeight,
                            ConfidenceLevel = score,
                            GPSLatitude = coordinate1[1],
                            GPSLongitude = coordinate1[0],
                            GPSAltitude = gpsCoordinate.Altitude,
                            GPSTrackAngle = gpsCoordinate.TrackAngle,
                            GeoJSON = jsonData,
                            RoundedGPSLatitude = Math.Round(coordinate1[1], 4),
                            RoundedGPSLongitude = Math.Round(coordinate1[0], 4),
                            SegmentId = Convert.ToInt32(sectionId),
                            Chainage = middleY/1000 + chainage,
                            ChainageEnd = maxY / 1000 + chainage
                        };
                        newMMO.Add(mmo);
                    }
                }
            }

            if (LcmsObjects.Any(x => x == LayerNames.MacroTexture))
            {
                var macroTextureInfo = doc.SelectSingleNode("/LcmsAnalyserResults/MacroTextureInformation");
                if (macroTextureInfo != null && macroTextureInfo.HasChildNodes)
                {
                    var xPositions = macroTextureInfo.SelectSingleNode("Bands/BandLimitPositions").InnerText;
                    var xValues = xPositions.Split(" ", StringSplitOptions.RemoveEmptyEntries).Select(double.Parse).ToArray(); //meter

                    var algorithm = doc.SelectSingleNode("/LcmsAnalyserResults/ProcessingInformation/ProcessingParameters/MacroTextureModule_Parameters/MacroTextureModule_Algorithm").InnerText;

                    var textureMeasurements = macroTextureInfo.SelectNodes("MacroTextureMeasurement");

                    var textureId = 0;
                    foreach (XmlNode textureMeasurement in textureMeasurements)
                    {
                        var position = GetDouble(GetValueFromNode(textureMeasurement, "Position")) - distanceBeginDouble; //Y value
                        var length = GetDouble(GetValueFromNode(textureMeasurement, "Length"));

                        var mtdValues = new double?[5]; // For MTDBand1 to MTDBand5
                        var smtdValues = new double?[5]; // For SMTDBand1 to SMTDBand5
                        var mpdValues = new double?[5]; // For MPDBand1 to MPDBand5
                        var rmsValues = new double?[5]; // For RMSBand1 to RMSBand5

                        var bandReports = textureMeasurement.SelectNodes("BandReport");
                        var polygons = new List<List<double[]>>();

                        for (int i = 0; i < bandReports.Count; i++)
                        {
                            var polygon = new List<double[]>();
                            var bandReport = bandReports[i];

                            var index = GetInt(GetValueFromNode(bandReport, "BandIndex")) - 1;
                            if (algorithm == "0" || algorithm == "2")
                            {
                                mtdValues[index] = GetDouble(GetValueFromNode(bandReport, "MTD"));
                                smtdValues[index] = GetDouble(GetValueFromNode(bandReport, "SMTD"));
                            }
                            else if (algorithm == "1" || algorithm == "2")
                            {
                                mpdValues[index] = GetDouble(GetValueFromNode(bandReport, "MPD"));
                                rmsValues[index] = GetDouble(GetValueFromNode(bandReport, "RMS"));
                            }

                            double minX = xValues[i] * 1000; //meter to mm
                            double maxX = xValues[i + 1] * 1000;  //meter to mm
                            double minY = position * 1000;
                            double maxY = (position + length) * 1000;  //meter to mm

                            var coordinate1 = ConvertToGPSCoordinates(minX, maxY, gpsCoordinate);
                            var coordinate2 = ConvertToGPSCoordinates(maxX, maxY, gpsCoordinate);
                            var coordinate3 = ConvertToGPSCoordinates(maxX, minY, gpsCoordinate);
                            var coordinate4 = ConvertToGPSCoordinates(minX, minY, gpsCoordinate);

                            polygon.Add(coordinate1);
                            polygon.Add(coordinate2);
                            polygon.Add(coordinate3);
                            polygon.Add(coordinate4);

                            polygons.Add(polygon);
                        }

                        double? avgMtd = GetAverage(mtdValues); 
                        double? avgSmtd = GetAverage(smtdValues); 
                        double? avgMpd = GetAverage(mpdValues); 
                        double? avgRms = GetAverage(rmsValues);

                        var jsonDataObject = new
                        {
                            type = "Feature",
                            geometry = new
                            {
                                type = "MultiPolygon",
                                coordinates = polygons.ToArray()
                            },
                            properties = new
                            {
                                id = textureId.ToString(),
                                file = fileName,
                                type = "Band Texture"
                            }
                        };
                        string jsonData = JsonSerializer.Serialize(jsonDataObject);

                        var middlePolygon = (polygons != null && polygons.Count > 0) ? polygons[polygons.Count / 2] : new List<double[]>();
                        var avgJsonDataObject = new
                        {
                            type = "Feature",
                            geometry = new
                            {
                                type = "Polygon",
                                coordinates = new[] { middlePolygon.ToArray() }
                            },
                            properties = new
                            {
                                id = textureId.ToString(),
                                file = fileName,
                                type = "Average Texture"
                            }
                        };

                        string avgJsonData = JsonSerializer.Serialize(avgJsonDataObject);

                        var macroTexture = new LCMS_Texture_Processed
                        {
                            SurveyId = surveyId,
                            SurveyDate = DateTime.Parse(surveyDate),
                            ImageFileIndex = fileName + ".jpg",
                            PavementType = pavement,
                            TextureId = textureId,
                            MTDBand1 = mtdValues[0],
                            MTDBand2 = mtdValues[1],
                            MTDBand3 = mtdValues[2],
                            MTDBand4 = mtdValues[3],
                            MTDBand5 = mtdValues[4],
                            SMTDBand1 = smtdValues[0],
                            SMTDBand2 = smtdValues[1],
                            SMTDBand3 = smtdValues[2],
                            SMTDBand4 = smtdValues[3],
                            SMTDBand5 = smtdValues[4],
                            MPDBand1 = mpdValues[0],
                            MPDBand2 = mpdValues[1],
                            MPDBand3 = mpdValues[2],
                            MPDBand4 = mpdValues[3],
                            MPDBand5 = mpdValues[4],
                            RMSBand1 = rmsValues[0],
                            RMSBand2 = rmsValues[1],
                            RMSBand3 = rmsValues[2],
                            RMSBand4 = rmsValues[3],
                            RMSBand5 = rmsValues[4],
                            GPSLatitude = polygons[0][0][1],
                            GPSLongitude = polygons[0][0][0],
                            GPSAltitude = gpsCoordinate.Altitude,
                            GPSTrackAngle = gpsCoordinate.TrackAngle,
                            GeoJSON = jsonData,
                            RoundedGPSLatitude = Math.Round(polygons[0][0][1], 4),
                            RoundedGPSLongitude = Math.Round(polygons[0][0][0], 4),
                            SegmentId = Convert.ToInt32(sectionId),
                            Chainage = chainage,
                            ChainageEnd = chainage + position + length,
                            AvgMPD = avgMpd,
                            AvgMTD = avgMtd,
                            AvgRMS = avgRms,
                            AvgSMTD = avgSmtd,
                            AvgGeoJSON = avgJsonData
                        };
                        textureId++;
                        newTexture.Add(macroTexture);
                    }
                }
            }

            if (LcmsObjects.Any(x => x == LayerNames.CurbDropOff))
            {
                var dropoffMeasurements = doc.SelectSingleNode("/LcmsAnalyserResults/DropoffInformation/Measurements");
                var curbMeasurements = doc.SelectSingleNode("/LcmsAnalyserResults/CurbInformation/Measurements");

                if (dropoffMeasurements != null && curbMeasurements != null)
                {
                    int profileId = 0;
                    double middleX = x / 2;
                    var dropOffIntervalNode = doc.SelectSingleNode("/LcmsAnalyserResults/ProcessingInformation/ProcessingParameters/DropoffModule_Parameters/DropoffModule_EvaluationInterval_m");
                    var dropOffInterval = GetDouble(dropOffIntervalNode.InnerText) * 1000; //meter to mm
                    var curbIntervalNode = doc.SelectSingleNode("/LcmsAnalyserResults/ProcessingInformation/ProcessingParameters/CurbModule_Parameters/CurbModule_EvaluationInterval_m");
                    var curbInterval = GetDouble(curbIntervalNode.InnerText) * 1000; //meter to mm

                    // Common logic for processing measurements
                    void ProcessMeasurements(XmlNode measurementsNode, double interval, string type)
                    {
                        if (measurementsNode != null && measurementsNode.HasChildNodes)
                        {
                            var nodes = measurementsNode.SelectNodes(type + "Data");
                            List<(double PositionX, double PositionY, double Height)> leftSide = new List<(double, double, double)>();
                            List<(double PositionX, double PositionY, double Height)> rightSide = new List<(double, double, double)>();

                            foreach (XmlNode item in nodes)
                            {
                                var positionX = GetDouble(GetValueFromNode(item, "PositionX")) * 1000; // meter to mm
                                var positionY = (GetDouble(GetValueFromNode(item, "PositionY")) - distanceBeginDouble) * 1000; // meter to mm
                                var height = GetDouble(GetValueFromNode(item, "Height")); // millimeter

                                if (positionX <= middleX)
                                {
                                    leftSide.Add((positionX, positionY, height));
                                }
                                else
                                {
                                    rightSide.Add((positionX, positionY, height));
                                }
                            }

                            List<List<(double PositionX, double PositionY, double Height)>> allSides = new List<List<(double PositionX, double PositionY, double Height)>>();
                            if (leftSide.Count > 0)
                            {
                                allSides.Add(leftSide);
                            }
                            if (rightSide.Count > 0)
                            {
                                allSides.Add(rightSide);
                            }

                            foreach (var eachSide in allSides)
                            {
                                for (int i = 0; i < eachSide.Count; i++)
                                {
                                    var currentInfo = eachSide[i];
                                    double nextPositionX;
                                    double nextPositionY;

                                    if (i < eachSide.Count - 1)
                                    {
                                        nextPositionX = eachSide[i + 1].PositionX;
                                        nextPositionY = eachSide[i + 1].PositionY;
                                    }
                                    else
                                    {
                                        nextPositionX = currentInfo.PositionX;
                                        nextPositionY = currentInfo.PositionY + interval;
                                    }

                                    var currentCoordinate = ConvertToGPSCoordinates(currentInfo.PositionX, currentInfo.PositionY, gpsCoordinate);
                                    var nextCoordinate = ConvertToGPSCoordinates(nextPositionX, nextPositionY, gpsCoordinate);


                                    //if (polygonCoordinates.Any() && !IsInsidePolygon(currentCoordinate[1], currentCoordinate[0], polygonCoordinates))
                                    //{
                                    //    continue;
                                    //}

                                    var jsonDataObject = new
                                    {
                                        type = "Feature",
                                        geometry = new
                                        {
                                            type = "Polyline",
                                            coordinates = new List<double[]>()
                                        {
                                            currentCoordinate, nextCoordinate
                                        }
                                        },
                                        properties = new
                                        {
                                            id = profileId,
                                            file = fileName,
                                            type = "Curb DropOff",
                                            x = currentInfo.PositionX,
                                            y = currentInfo.PositionY
                                        }
                                    };
                                    string jsonData = JsonSerializer.Serialize(jsonDataObject);

                                    var newDropOff = new LCMS_Curb_DropOff
                                    {
                                        SurveyId = surveyId,
                                        SurveyDate = DateTime.Parse(surveyDate),
                                        ImageFileIndex = fileName + ".jpg",
                                        PavementType = pavement,
                                        Height_mm = currentInfo.Height,
                                        Type = type,
                                        ProfileId = profileId,
                                        GPSLatitude = currentCoordinate[1],
                                        GPSLongitude = currentCoordinate[0],
                                        GPSAltitude = gpsCoordinate.Altitude,
                                        GPSTrackAngle = gpsCoordinate.TrackAngle,
                                        GeoJSON = jsonData,
                                        RoundedGPSLatitude = Math.Round(currentCoordinate[1], 4),
                                        RoundedGPSLongitude = Math.Round(currentCoordinate[0], 4),
                                        SegmentId = Convert.ToInt32(sectionId),
                                        EndGPSLatitude = nextCoordinate[1],
                                        EndGPSLongitude = nextCoordinate[0],
                                        Chainage = currentInfo.PositionY / 1000 + chainage,
                                        ChainageEnd = nextPositionY / 1000 + chainage
                                    };

                                    newCurbDropoff.Add(newDropOff);
                                }
                                profileId++;
                            }
                        }
                    }

                    // Process dropoff and curb measurements
                    ProcessMeasurements(dropoffMeasurements, dropOffInterval, "Dropoff");
                    ProcessMeasurements(curbMeasurements, curbInterval, "Curb");
                }
            }

            if (LcmsObjects.Any(x => x == LayerNames.RumbleStrip))
            {
                var rumbleStripArea = doc.SelectSingleNode("/LcmsAnalyserResults/RumbleStripInformation/RumbleStripArea");
                if (rumbleStripArea != null && rumbleStripArea.HasChildNodes)
                {
                    var rumbleStripId = 0;
                    var minX = GetDouble(GetValueFromNode(rumbleStripArea, "BoundingBox/MinX"));
                    var maxX = GetDouble(GetValueFromNode(rumbleStripArea, "BoundingBox/MaxX"));
                    var minY = GetDouble(GetValueFromNode(rumbleStripArea, "BoundingBox/MinY"));
                    var maxY = GetDouble(GetValueFromNode(rumbleStripArea, "BoundingBox/MaxY"));

                    var middleX = (minX + maxX) / 2;
                    var middleY = (minY + maxY) / 2;

                    var width = maxX - minX;
                    var length = maxY - minY;
                    var area = width * length; //mm2

                    var coordinate1 = ConvertToGPSCoordinates(minX, maxY, gpsCoordinate);
                    var coordinate2 = ConvertToGPSCoordinates(maxX, maxY, gpsCoordinate);
                    var coordinate3 = ConvertToGPSCoordinates(maxX, minY, gpsCoordinate);
                    var coordinate4 = ConvertToGPSCoordinates(minX, minY, gpsCoordinate);

                    //if (polygonCoordinates.Any() && !IsInsidePolygon(coordinate1[1], coordinate1[0], polygonCoordinates))
                    //{
                    //    //skip inserting rumble strip
                    //}
                    //else
                    //{
                        var nbStrip = GetInt(GetValueFromNode(rumbleStripArea, "NbStrip"));
                        var typeDouble = GetDouble(GetValueFromNode(rumbleStripArea, "Type"));
                        string type = string.Empty;

                        if (typeDouble == 0)
                        {
                            type = "milled strip";
                        }
                        else if (typeDouble == 1)
                        {
                            type = "raised strip";
                        }
                        else
                        {
                            type = typeDouble.ToString();
                        }

                        var stripPerMeter = (length / nbStrip) / 1000; //convert mm to m

                        var strips = rumbleStripArea.SelectNodes("Strips/BoundingBox");
                        double totalDepth = 0.0;
                        double totalHeight = 0.0;

                        foreach (XmlNode strip in strips)
                        {
                            var depth = GetDouble(GetValueFromNode(strip, "Depth"));

                            //TODO - double check which height to be used <PavementRefHeight> <PavementBaseHeight> <PavementStripHeight>
                            var stripHeight = GetDouble(GetValueFromNode(strip, "PavementStripHeight"));
                            totalDepth += depth;
                            totalHeight += stripHeight;
                        }

                        var avgDepth = totalDepth / nbStrip;
                        var avgHeight = totalHeight / nbStrip;

                        var jsonDataObject = new
                        {
                            type = "Feature",
                            geometry = new
                            {
                                type = "Polygon",
                                coordinates = new[]
                                {
                                new List<double[]>()
                                {
                                    coordinate1, coordinate2, coordinate3, coordinate4
                                }
                            }
                            },
                            properties = new
                            {
                                id = rumbleStripId.ToString(),
                                file = fileName,
                                type = "Rumble Strip",
                                x = middleX,
                                y = middleY
                            }
                        };

                        string jsonData = JsonSerializer.Serialize(jsonDataObject);

                        var rumbleStrip = new LCMS_Rumble_Strip
                        {
                            SurveyId = surveyId,
                            SurveyDate = DateTime.Parse(surveyDate),
                            ImageFileIndex = fileName + ".jpg",
                            PavementType = pavement,
                            RumbleStripId = rumbleStripId,
                            Type = type,
                            Length_mm = length,
                            Area_mm2 = area,
                            NumStrip = nbStrip,
                            StripPerMeter = stripPerMeter,
                            AvgDepth_mm = avgDepth,
                            AvgHeight_mm = avgHeight,
                            GPSLatitude = coordinate1[1],
                            GPSLongitude = coordinate1[0],
                            GPSAltitude = gpsCoordinate.Altitude,
                            GPSTrackAngle = gpsCoordinate.TrackAngle,
                            GeoJSON = jsonData,
                            RoundedGPSLatitude = Math.Round(coordinate1[1], 4),
                            RoundedGPSLongitude = Math.Round(coordinate1[0], 4),
                            SegmentId = Convert.ToInt32(sectionId),
                            Chainage = (middleY/1000) + chainage,
                            ChainageEnd = (maxY / 1000) + chainage
                        };
                        newRumbleStrip.Add(rumbleStrip);
                    }

                //}
            }

            if (LcmsObjects.Any(x => x == LayerNames.Geometry)) //Geometry
            {
                var geometrySlopeMeasurement = doc.SelectNodes("/LcmsAnalyserResults/SlopeCrossSlopeInformation/SlopeMeasurement");
                var geometryCrossSlopeMeasurement = doc.SelectNodes("/LcmsAnalyserResults/SlopeCrossSlopeInformation/CrossSlopeMeasurement");
                var geometryCurvatureMeasurement = doc.SelectNodes("/LcmsAnalyserResults/SlopeCrossSlopeInformation/CurvatureMeasurement");

                List<(double, int, int, double)> slopeMeasurementData = new List<(double, int, int, double)>();
                List<(int, double, int, double, int)> crossSlopeMeasurementData = new List<(int, double, int, double, int)>();
                List<(double, double)> curvatureMeasurementData = new List<(double, double)>();
                var positionXList = new List<double>();

                //SlopeMeasurement
                foreach (XmlNode slopeItem in geometrySlopeMeasurement)
                {
                    double slopeValue = GetDouble(GetValueFromNode(slopeItem, "Slope"));
                    int slopeValidValue = GetInt(GetValueFromNode(slopeItem, "Valid"));
                    int slopeInsufficientOverlapWarning = GetInt(GetValueFromNode(slopeItem, "InsufficientOverlapWarning"));
                    double slopeSurveyPosition = GetDouble(GetValueFromNode(slopeItem, "SurveyPosition"));

                    slopeMeasurementData.Add((slopeValue, slopeValidValue, slopeInsufficientOverlapWarning, slopeSurveyPosition));
                    positionXList.Add(slopeSurveyPosition); // Cross Slope and Curvature position are same, as just use Slope position
                }

                //CrossSlopeMeasurement
                foreach (XmlNode crossSlopeItem in geometryCrossSlopeMeasurement)
                {
                    int crossSlopeNum = GetInt(GetValueFromNode(crossSlopeItem, "NbrMeasurements"));
                    double crossSlopeValue = GetDouble(GetValueFromNode(crossSlopeItem, "CrossSlope"));
                    int crossSlopeValidValue = GetInt(GetValueFromNode(crossSlopeItem, "Valid"));
                    double crossSlopeSurveyPosition = GetDouble(GetValueFromNode(crossSlopeItem, "SurveyPosition"));
                    int crossSlopeProfileIndex = GetInt(GetValueFromNode(crossSlopeItem, "ProfileIndex"));


                    crossSlopeMeasurementData.Add((crossSlopeNum, crossSlopeValue, crossSlopeValidValue, crossSlopeSurveyPosition, crossSlopeProfileIndex));
                }

                //curvatureMeasurement
                foreach (XmlNode curvatureItem in geometryCurvatureMeasurement)
                {
                    double curvatureValue = GetDouble(GetValueFromNode(curvatureItem, "Curvature"));
                    double curvatureSurveyPosition = GetDouble(GetValueFromNode(curvatureItem, "SurveyPosition"));

                    curvatureMeasurementData.Add((curvatureValue, curvatureSurveyPosition));
                }

                var intervalNode = doc.SelectSingleNode("/LcmsAnalyserResults/ProcessingInformation/ProcessingParameters/SlopeAndCrossSlopeModule_Parameters/SlopeAndCrossSlopeModule_EvaluationInterval_m");
                double interval = intervalNode != null ? GetDouble(intervalNode.InnerText) : 1;

                // Pair and save Geometry data(Slope, CrossSlope,Curvature)
                for (int i = 0; i < slopeMeasurementData.Count; i++)
                {
                    var slopeData = slopeMeasurementData[i];
                    var crossSlopeData = slopeMeasurementData[i];
                    var curvatureData = slopeMeasurementData[i];

                    //coordinate
                    var yStartInMeter = positionXList[i] - distanceBeginDouble;
                    var yEndInMeter = (i < positionXList.Count - 1) ? positionXList[i + 1] - distanceBeginDouble : yStartInMeter + interval; // Handle the last item

                    var yStart = yStartInMeter * 1000; //Convert m to mm
                    var yEnd = yEndInMeter * 1000; //Convert m to mm

                    double[]? centerCoordinate1 = null, centerCoordinate2 = null;
                    string jsonData = null;

                    if (positionXList.Count > 2)
                    {
                        centerCoordinate1 = ConvertToGPSCoordinates(x / 2, yStart, gpsCoordinate);
                        centerCoordinate2 = ConvertToGPSCoordinates(x / 2, yEnd, gpsCoordinate);

                        var jsonDataObject = new
                        {
                            type = "Feature",
                            geometry = new
                            {
                                type = "Polyline",
                                coordinates = new[] { centerCoordinate1, centerCoordinate2 }
                            },
                            properties = new
                            {
                                id = i.ToString(),
                                file = fileName,
                                type = "Geometry"
                            }
                        };
                        jsonData = JsonSerializer.Serialize(jsonDataObject);
                    }

                    var geometry = new LCMS_Geometry_Processed
                    {
                        SurveyId = surveyId,
                        SurveyDate = DateTime.Parse(surveyDate),
                        ImageFileIndex = fileName + ".jpg",
                        PavementType = pavement,
                        Slope = slopeData.Item1,
                        StatesOfSlope = slopeData.Item2.ToString(),
                        CrossSlope = crossSlopeData.Item2,
                        StatesOfCrossSlope = crossSlopeData.Item3.ToString(),
                        RadiusOfCurvature = curvatureData.Item1,
                        GPSLatitude = centerCoordinate1 != null ? centerCoordinate1[1] : gpsCoordinate.Latitude,
                        GPSLongitude = centerCoordinate1 != null ? centerCoordinate1[0] : gpsCoordinate.Longitude,
                        GPSAltitude = gpsCoordinate.Altitude,
                        GPSTrackAngle = gpsCoordinate.TrackAngle,
                        GeoJSON = jsonData,
                        RoundedGPSLatitude = Math.Round(gpsCoordinate.Latitude, 4),
                        RoundedGPSLongitude = Math.Round(gpsCoordinate.Longitude, 4),
                        SegmentId = Convert.ToInt32(sectionId),
                        EndGPSLatitude = centerCoordinate2 != null ? centerCoordinate2[1] : gpsCoordinate.Latitude,
                        EndGPSLongitude = centerCoordinate2 != null ? centerCoordinate2[0] : gpsCoordinate.Longitude,
                        Chainage = (yStartInMeter + chainage),
                        ChainageEnd = (yEndInMeter + chainage)
                    };
                    newGeomtry.Add(geometry);
                }

            }

            if (LcmsObjects.Any(x => x == LayerNames.Shove))
            {
                var shovingMeasurements = doc.SelectNodes("/LcmsAnalyserResults/ShovingInformation/ShovingMeasurement");
                if (shovingMeasurements != null)
                {
                    var shovingId = 0;
                    foreach (XmlNode shovingMeasurement in shovingMeasurements)
                    {
                        var centralBandWidthStr = doc.SelectSingleNode("/LcmsAnalyserResults/ProcessingInformation/ProcessingParameters/GeneralParam_CentralBandWidth_mm");
                        var centralBandWidth = GetDouble(centralBandWidthStr.InnerText);

                        var wheelPathWidthStr = doc.SelectSingleNode("/LcmsAnalyserResults/ProcessingInformation/ProcessingParameters/GeneralParam_WheelPathWidth_mm");
                        var wheelPathWidth = GetDouble(wheelPathWidthStr.InnerText);

                        var leftX = (x / 2) - (centralBandWidth / 2) - wheelPathWidth;
                        var rightX = (x / 2) + (centralBandWidth / 2) + wheelPathWidth;

                        var positionInMeter = GetDouble(GetValueFromNode(shovingMeasurement, "Position")) - distanceBeginDouble; //y
                        var positionY = positionInMeter * 1000;

                        var ruttingIntervalNode = doc.SelectSingleNode("/LcmsAnalyserResults/ProcessingInformation/ProcessingParameters/RuttingModule_Parameters/RuttingModule_EvaluationInterval_m");
                        if (ruttingIntervalNode != null)
                        {
                            var ruttingInterval = GetDouble(ruttingIntervalNode.InnerText) * 1000; //convert meter to mm

                            var laneSide = shovingMeasurement.SelectSingleNode("LaneSide").InnerText;
                            var shoveHeight = GetDouble(GetValueFromNode(shovingMeasurement, "ShoveHeight"));
                            var shoveWidth = GetDouble(GetValueFromNode(shovingMeasurement, "ShoveWidth"));
                            var rutDepth = GetDouble(GetValueFromNode(shovingMeasurement, "ShovingRutDepth"));
                            var rutWidth = GetDouble(GetValueFromNode(shovingMeasurement, "ShovingRutWidth"));

                            double[] coordinate1 = new double[2];
                            double[] coordinate2 = new double[2];
                            double[] coordinate3 = new double[2];
                            double[] coordinate4 = new double[2];

                            double middleX = 0.0;
                            var middleY = (positionY + ruttingInterval) / 2;

                            if (laneSide == "Left")
                            {
                                //use leftX
                                coordinate1 = ConvertToGPSCoordinates(leftX, positionY, gpsCoordinate);
                                coordinate2 = ConvertToGPSCoordinates(leftX, positionY + ruttingInterval, gpsCoordinate);
                                coordinate3 = ConvertToGPSCoordinates(leftX - shoveWidth, positionY + ruttingInterval, gpsCoordinate);
                                coordinate4 = ConvertToGPSCoordinates(leftX - shoveWidth, positionY, gpsCoordinate);
                                middleX = leftX;
                            }
                            else if (laneSide == "Right")
                            {
                                //use RightX
                                coordinate1 = ConvertToGPSCoordinates(rightX, positionY, gpsCoordinate);
                                coordinate2 = ConvertToGPSCoordinates(rightX, positionY + ruttingInterval, gpsCoordinate);
                                coordinate3 = ConvertToGPSCoordinates(rightX + shoveWidth, positionY + ruttingInterval, gpsCoordinate);
                                coordinate4 = ConvertToGPSCoordinates(rightX + shoveWidth, positionY, gpsCoordinate);
                                middleX = rightX;
                            }

                            //if (polygonCoordinates.Any() && !IsInsidePolygon(coordinate1[1], coordinate1[0], polygonCoordinates))
                            //{
                            //    continue;
                            //}

                            var jsonDataObject = new
                            {
                                type = "Feature",
                                geometry = new
                                {
                                    type = "Polygon",
                                    coordinates = new[]
                                    {
                            new List<double[]>()
                            {
                                coordinate1, coordinate2, coordinate3, coordinate4
                            }
                        }
                                },
                                properties = new
                                {
                                    id = shovingId.ToString(),
                                    file = fileName,
                                    type = "Shove",
                                    x = middleX,
                                    y = middleY
                                }
                            };
                            var intervalMeters = ruttingInterval / 1000;
                            string jsonData = JsonSerializer.Serialize(jsonDataObject);

                            var shove = new LCMS_Shove_Processed
                            {
                                SurveyId = surveyId,
                                SurveyDate = DateTime.Parse(surveyDate),
                                ImageFileIndex = fileName + ".jpg",
                                PavementType = pavement,
                                ShoveId = shovingId,
                                LaneSide = laneSide,
                                ShoveHeight_mm = shoveHeight,
                                ShoveWidth_mm = shoveWidth,
                                RutDepth_mm = rutDepth,
                                RutWidth_mm = rutWidth,
                                GPSLatitude = coordinate1[1],
                                GPSLongitude = coordinate1[0],
                                GPSAltitude = gpsCoordinate.Altitude,
                                GPSTrackAngle = gpsCoordinate.TrackAngle,
                                GeoJSON = jsonData,
                                RoundedGPSLatitude = Math.Round(coordinate1[1], 4),
                                RoundedGPSLongitude = Math.Round(coordinate1[0], 4),
                                SegmentId = Convert.ToInt32(sectionId),
                                Chainage =  chainage,
                                ChainageEnd = chainage + positionInMeter + intervalMeters
                            };
                            shovingId++;
                            newShove.Add(shove);
                        }
                    }
                }
            }

            if (LcmsObjects.Any(x => x == LayerNames.Grooves))
            {
                var grooveInformation = doc.SelectSingleNode("/LcmsAnalyserResults/PavementTypeInformation/GroovesInformation");
                if (grooveInformation != null)
                {
                    var zoneWidth = GetDouble(GetValueFromNode(grooveInformation, "ZoneReportList/ZoneWidth"));
                    var zoneHeight = GetDouble(GetValueFromNode(grooveInformation, "ZoneReportList/ZoneHeight"));
                    var zoneArea = zoneWidth * zoneHeight;

                    var zoneReports = grooveInformation.SelectNodes("ZoneReportList/ZoneReport");
                    foreach (XmlNode zoneReport in zoneReports)
                    {
                        var X = GetDouble(GetValueFromNode(zoneReport, "X"));
                        var Y = GetDouble(GetValueFromNode(zoneReport, "Y"));
                        var XMax = GetDouble(GetValueFromNode(zoneReport, "XMax"));
                        var YMax = GetDouble(GetValueFromNode(zoneReport, "YMax"));
                        var avgDepth = GetDouble(GetValueFromNode(zoneReport, "AvgDepth"));
                        var avgWidth = GetDouble(GetValueFromNode(zoneReport, "AvgWidth"));
                        var avgInterval = GetDouble(GetValueFromNode(zoneReport, "AvgInterval"));
                        var zoneId = GetDouble(GetValueFromNode(zoneReport, "ZoneID"));

                        var width = XMax - X;
                        var height = YMax - Y;

                        double[] coordinate1 = ConvertToGPSCoordinates(X, Y, gpsCoordinate);
                        Core.Communication.GPSCoordinate grooveCoordinate = new Core.Communication.GPSCoordinate
                        {
                            Longitude = coordinate1[0],
                            Latitude = coordinate1[1],
                            Altitude = altitude,
                            TrackAngle = trackAngle
                        };
                        double[] coordinate2 = ConvertToGPSCoordinates(0, height, grooveCoordinate);
                        double[] coordinate3 = ConvertToGPSCoordinates(width, height, grooveCoordinate);
                        double[] coordinate4 = ConvertToGPSCoordinates(width, 0, grooveCoordinate);

                        //if (polygonCoordinates.Any() && !IsInsidePolygon(coordinate1[1], coordinate1[0], polygonCoordinates))
                        //{
                        //    continue;
                        //}

                        var jsonDataObject = new
                        {
                            type = "Feature",
                            geometry = new
                            {
                                type = "Polygon",
                                coordinates = new[]
                                {
                                new List<double[]>()
                                {
                                    coordinate1, coordinate2, coordinate3, coordinate4
                                }
                            }
                            },
                            properties = new
                            {
                                id = zoneId.ToString(),
                                file = fileName,
                                type = "Grooves",
                                x = X,
                                y = Y
                            }
                        };
                        string jsonData = JsonSerializer.Serialize(jsonDataObject);

                        var groove = new LCMS_Grooves
                        {
                            SurveyId = surveyId,
                            SurveyDate = DateTime.Parse(surveyDate),
                            ImageFileIndex = fileName + ".jpg",
                            PavementType = pavement,
                            AvgDepth_mm = avgDepth,
                            AvgWidth_mm = avgWidth,
                            AvgInterval_mm = avgInterval,
                            ZoneArea_mm2 = zoneArea,
                            ZoneId = (int)zoneId,
                            GPSLatitude = coordinate1[1],
                            GPSLongitude = coordinate1[0],
                            GPSAltitude = gpsCoordinate.Altitude,
                            GPSTrackAngle = gpsCoordinate.TrackAngle,
                            GeoJSON = jsonData,
                            RoundedGPSLatitude = Math.Round(coordinate1[1], 4),
                            RoundedGPSLongitude = Math.Round(coordinate1[0], 4),
                            SegmentId = Convert.ToInt32(sectionId), 
                            Chainage = Y/1000 + chainage,
                            ChainageEnd = YMax / 1000 + chainage
                        };
                        newGrooves.Add(groove);
                    }
                }
            }
        }

        private LCMSBoundingBox GetLCMSBoundingBox(XmlNode xmlNode)
        {
            return new LCMSBoundingBox
            {
                MinX = GetFloat(GetValueFromNode(xmlNode, "MinX")),
                MinY = GetFloat(GetValueFromNode(xmlNode, "MinY")),
                MaxX = GetFloat(GetValueFromNode(xmlNode, "MaxX")),
                MaxY = GetFloat(GetValueFromNode(xmlNode, "MaxY"))
            };
        }

        private class LCMS_Spalling_RawWithCoordinates
        {
            public LCMS_Spalling_Raw spalling;
            public double minX;
            public double maxX;
            public double minY;
            public double maxY;
        }

        public async void ProcessSpalling(XmlNodeList spallingList, ConcurrentBag<LCMS_Spalling_Raw> spallingRawList, 
                                        Core.Communication.GPSCoordinate gpsCoordinate,
                                        string fileName, string surveyId, string surveyDate, string pavement, string idPrefix, string sectionId, double chainage)
        {
            foreach (XmlNode joint in spallingList)
            {
                var jointId = GetDouble(GetValueFromNode(joint, "JointID"));

                // average width
                var widthText = joint.SelectSingleNode("WidthMeasurements").InnerText;
                var widthStrings = widthText.Split(" ");
                double[] widthValues = widthStrings
                    .Select(double.Parse)
                    .Where(value => value != -10000.0) //-10000.0 is a fault value
                    .ToArray();
                var avgJointWidth = widthValues.Any() ? widthValues.Average() : 0;


                var spallingSegment = joint.SelectNodes("SpallingDefects/SpallingSegment");

                var spallingGroups = new Dictionary<string, List<LCMS_Spalling_RawWithCoordinates>>();

                if (spallingSegment != null && spallingSegment.Count > 0)
                {
                    foreach (XmlNode spallingItem in spallingSegment)
                    {
                        var spallingId = GetDouble(GetValueFromNode(spallingItem, "ID"));
                        var avgDepth = GetDouble(GetValueFromNode(spallingItem, "AverageDepth"));
                        var avgWidth = GetDouble(GetValueFromNode(spallingItem, "AverageWidth")) - avgJointWidth; // spalling width - joint width 
                        var length = GetDouble(GetValueFromNode(spallingItem, "Length"));

                        var startX = GetDouble(spallingItem.SelectSingleNode("Start/X").InnerText);
                        var startY = GetDouble(spallingItem.SelectSingleNode("Start/Y").InnerText);
                        var endX = GetDouble(spallingItem.SelectSingleNode("End/X").InnerText);
                        var endY = GetDouble(spallingItem.SelectSingleNode("End/Y").InnerText);

                        var middleX = (startX + endX) / 2;
                        var middleY = (startY + endY) / 2;

                        var XDifference = endX - startX;
                        var YDifference = endY - startY;

                        double[] coordinate1 = null;
                        double[] coordinate2 = null;
                        double[] coordinate3 = null;
                        double[] coordinate4 = null;
                        string direction = string.Empty;
                        double addition = 0.0;

                        if (length > 5)
                        {
                            addition = 0;
                        }
                        else
                        {
                            addition = 5;
                        }

                        if (idPrefix == "T")
                        {
                            if (XDifference < 5)
                            {
                                addition = 5;
                            }
                            coordinate1 = ConvertToGPSCoordinates(startX, startY + (avgWidth * 2.5), gpsCoordinate);
                            coordinate2 = ConvertToGPSCoordinates(startX, startY - (avgWidth * 2.5), gpsCoordinate);
                            coordinate3 = ConvertToGPSCoordinates(endX + addition, endY - (avgWidth * 2.5), gpsCoordinate);
                            coordinate4 = ConvertToGPSCoordinates(endX + addition, endY + (avgWidth * 2.5), gpsCoordinate);
                            direction = "Transversal";
                        }
                        else if (idPrefix == "L")
                        {
                            if (YDifference < 5)
                            {
                                addition = 5;
                            }
                            coordinate1 = ConvertToGPSCoordinates(startX + (avgWidth * 2.5), startY, gpsCoordinate);
                            coordinate2 = ConvertToGPSCoordinates(startX - (avgWidth * 2.5), startY, gpsCoordinate);
                            coordinate3 = ConvertToGPSCoordinates(endX - (avgWidth * 2.5), endY + addition, gpsCoordinate);
                            coordinate4 = ConvertToGPSCoordinates(endX + (avgWidth * 2.5), endY + addition, gpsCoordinate);
                            direction = "Longitudinal";
                        }

                        if (coordinate1 != null && coordinate2 != null && coordinate3 != null && coordinate4 != null)
                        {
                            //if (polygonCoordinates.Any() && !IsInsidePolygon(coordinate1[1], coordinate1[0], polygonCoordinates))
                            //{
                            //    continue;
                            //}

                            var jsonDataObject = new
                            {
                                type = "Feature",
                                geometry = new
                                {
                                    type = "Polygon",
                                    coordinates = new[]
                                    {
                                        new List<double[]>()
                                        {
                                            coordinate1, coordinate2, coordinate3, coordinate4
                                        }
                                    }
                                },
                                properties = new
                                {
                                    id = jointId.ToString(),
                                    file = fileName,
                                    type = "Spalling",
                                    x = middleX,
                                    y = middleY
                                }
                            };

                            string jsonData = JsonSerializer.Serialize(jsonDataObject);

                            var spalling = new LCMS_Spalling_Raw
                            {
                                SurveyId = surveyId,
                                SurveyDate = DateTime.Parse(surveyDate),
                                ImageFileIndex = fileName + ".jpg",
                                JointId = (int)jointId,
                                JointDirection = direction,
                                SpallingId = (int)spallingId,
                                AvgDepth_mm = avgDepth,
                                AvgWidth_mm = avgWidth,
                                Length_mm = length,
                                PavementType = pavement,
                                GPSLatitude = coordinate1[1],
                                GPSLongitude = coordinate1[0],
                                GPSAltitude = gpsCoordinate.Altitude,
                                GPSTrackAngle = gpsCoordinate.TrackAngle,
                                GeoJSON = jsonData,
                                RoundedGPSLatitude = Math.Round(coordinate1[1], 4),
                                RoundedGPSLongitude = Math.Round(coordinate1[0], 4),
                                SegmentId = Convert.ToInt32(sectionId), 
                                Chainage = middleY+ chainage,
                                ChainageEnd = endY + chainage
                            };

                            string groupKey = $"{jointId}-{avgDepth}-{avgWidth}-{length}-{sectionId}";
                            if (!spallingGroups.ContainsKey(groupKey))
                            {
                                spallingGroups[groupKey] = new List<LCMS_Spalling_RawWithCoordinates>();
                            }

                            LCMS_Spalling_RawWithCoordinates spallingcoord =
                            new LCMS_Spalling_RawWithCoordinates
                            {
                                spalling = spalling,
                                minX = startX,
                                maxX = endX,
                                minY = startY,
                                maxY = endY,
                            };

                            spallingGroups[groupKey].Add(spallingcoord);
                        }
                    }
                    /// Add to the list
                    foreach (var group in spallingGroups)
                    {
                        var firstItem = group.Value.First();

                        if (group.Value.Count > 1)
                        {
                            double minXX = firstItem.minX;
                            double maxXX = firstItem.maxX;
                            double minYY = firstItem.minY;
                            double maxYY = firstItem.maxY;

                            // Combine coordinates
                            foreach (var spallingItem1 in group.Value)
                            {
                                // Update MinX based on MinLongitude 
                                if (spallingItem1.minX < minXX)
                                {
                                    minXX = spallingItem1.minX;
                                }
                                if (spallingItem1.maxX > maxXX)
                                {
                                    maxXX = spallingItem1.maxX;
                                }
                                // Update MinY based on MinLatitude
                                if (spallingItem1.minY < minYY)
                                {
                                    minYY = spallingItem1.minY;
                                }
                                if (spallingItem1.maxY > maxYY)
                                {
                                    maxYY = spallingItem1.maxY;
                                }
                            }

                            var startX = minXX;
                            var endX = maxXX;
                            var startY = minYY;
                            var endY = maxYY;

                            var middleX = (startX + endX) / 2;
                            var middleY = (startY + endY) / 2;
                            var XDifference = endX - startX;
                            var YDifference = endY - startY;
                            double addition = 0.0;

                            double[] coordinate1 = null;
                            double[] coordinate2 = null;
                            double[] coordinate3 = null;
                            double[] coordinate4 = null;

                            string direction = string.Empty;

                            var avgWidth = firstItem.spalling.AvgWidth_mm;

                            if (idPrefix == "T")
                            {

                                if (XDifference < 5)
                                {
                                    addition = 5;
                                }

                                coordinate1 = ConvertToGPSCoordinates(startX, startY + (avgWidth * 2.5), gpsCoordinate);
                                coordinate2 = ConvertToGPSCoordinates(startX, startY - (avgWidth * 2.5), gpsCoordinate);
                                coordinate3 = ConvertToGPSCoordinates(endX + addition, endY - (avgWidth * 2.5), gpsCoordinate);
                                coordinate4 = ConvertToGPSCoordinates(endX + addition, endY + (avgWidth * 2.5), gpsCoordinate);
                                direction = "Transversal";
                            }
                            else if (idPrefix == "L")
                            {
                                if (YDifference < 5)
                                {
                                    addition = 5;
                                }

                                coordinate1 = ConvertToGPSCoordinates(startX + (avgWidth * 2.5), startY, gpsCoordinate);
                                coordinate2 = ConvertToGPSCoordinates(startX - (avgWidth * 2.5), startY, gpsCoordinate);
                                coordinate3 = ConvertToGPSCoordinates(endX - (avgWidth * 2.5), endY + addition, gpsCoordinate);
                                coordinate4 = ConvertToGPSCoordinates(endX + (avgWidth * 2.5), endY + addition, gpsCoordinate);
                                direction = "Longitudinal";
                            }

                            var jsonDataObject1 = new
                            {
                                type = "Feature",
                                geometry = new
                                {
                                    type = "Polygon",
                                    coordinates = new[]
                                    {
                                        new List<double[]>()
                                        {
                                            coordinate1, coordinate2, coordinate3, coordinate4
                                        }
                                    }
                                },
                                properties = new
                                {
                                    id = jointId.ToString(),
                                    spallingId = firstItem.spalling.SpallingId,
                                    file = fileName,
                                    type = "Spalling",
                                    x = middleX,
                                    y = middleY
                                }
                            };
                            //1

                            firstItem.spalling.GeoJSON = JsonSerializer.Serialize(jsonDataObject1);
                        }

                        spallingRawList.Add(firstItem.spalling);
                    }
                }
            }
        }

        public async void ProcessJointListDoc(XmlNodeList jointNodeList, ConcurrentBag<LCMS_Concrete_Joints> concreteJointList, 
                                    Core.Communication.GPSCoordinate gpsCoordinate,
                                    string FileName, string surveyId, string surveyDate, string pavement, string idPrefix, string sectionId, double chainage)
        {
            foreach (XmlNode jointItem in jointNodeList)
            {
                var jointId = GetDouble(GetValueFromNode(jointItem, "JointID"));
                var direction = string.Empty;
                if (idPrefix == "L")
                {
                    direction = "Longitudinal";
                }
                else
                {
                    direction = "Transversal";
                }

                var X1 = GetDouble(GetValueFromNode(jointItem, "X1"));
                var Y1 = GetDouble(GetValueFromNode(jointItem, "Y1"));
                var X2 = GetDouble(GetValueFromNode(jointItem, "X2"));
                var Y2 = GetDouble(GetValueFromNode(jointItem, "Y2"));

                double[] coordinate1 = ConvertToGPSCoordinates(X1, Y1, gpsCoordinate);
                double[] coordinate2 = ConvertToGPSCoordinates(X2, Y2, gpsCoordinate);

                //if (polygonCoordinates.Any())
                //{
                //    if (!IsInsidePolygon(coordinate1[1], coordinate1[0], polygonCoordinates) || !IsInsidePolygon(coordinate2[1], coordinate2[0], polygonCoordinates))
                //    {
                //        continue;
                //    }
                //}
                var length = GetDouble(GetValueFromNode(jointItem, "Length"));

                // average width
                var widthText = jointItem.SelectSingleNode("WidthMeasurements").InnerText;
                var widthStrings = widthText.Split(" ");
                double[] widthValues = widthStrings
                    .Select(double.Parse)
                    .Where(value => value != -10000.0) //-10000.0 is a fault value
                    .ToArray();
                var avgWidth = widthValues.Any() ? widthValues.Average() : -10000.0;

                //average depth
                var avgDepthBad = GetDouble(GetValueFromNode(jointItem, "AverageDepthBadSeal"));
                var avgDepthGood = GetDouble(GetValueFromNode(jointItem, "AverageDepthGoodSeal"));
                var totalLengthBad = GetDouble(GetValueFromNode(jointItem, "BadSealantTotalLength"));
                var totalLengthGood = length - totalLengthBad;
                var avgDepth = (avgDepthBad * totalLengthBad + avgDepthGood * totalLengthGood) / length;

                //fault heights
                var faultHeight = jointItem.SelectSingleNode("FaultMeasurements").InnerText;
                var faultHeightStrings = faultHeight.Split(" ");
                double[] heightValues = faultHeightStrings
                    .Select(double.Parse)
                    .Where(value => value != -10000.0) //-10000.0 is a fault value
                    .ToArray();
                var faultAvgHeight = heightValues.Any() ? heightValues.Average() : 0.0;
                var faultMaxHeight = heightValues.Any() ? heightValues.Max() : 0.0;
                var faultMinHeight = heightValues.Any() ? heightValues.Min() : 0.0; // Use 0.0 if there are no valid values

                //badSeal
                var badSealLength = GetDouble(GetValueFromNode(jointItem, "BadSealantTotalLength"));
                var badSealAvgDepth = GetDouble(GetValueFromNode(jointItem, "AverageDepthBadSeal"));
                var badSealMaxDepth = GetDouble(GetValueFromNode(jointItem, "MaxDepthSeal"));

                //MedianPercent
                var medianPercentRng = GetDouble(GetValueFromNode(jointItem, "MedianPercentRng"));
                var medianPercentInt = GetDouble(GetValueFromNode(jointItem, "MedianPercentInt"));

                //spalling 
                var spallingList = jointItem.SelectNodes("SpallingDefects/SpallingSegment");
                var spallingTotalLength = 0.0;
                var spallingAvgDepth = 0.0;
                var spallingMaxDepth = 0.0;
                var spallingAvgWidth = 0.0;
                var spallingMaxWidth = 0.0;

                //calculate spalling only if it exists
                if (spallingList.Count > 0)
                {
                    foreach (XmlNode spalling in spallingList)
                    {
                        var spallingLength = GetDouble(GetValueFromNode(spalling, "Length"));
                        var spallingDepth = GetDouble(GetValueFromNode(spalling, "AverageDepth"));
                        var spallingWidth = GetDouble(GetValueFromNode(spalling, "AverageWidth"));

                        spallingTotalLength += spallingLength;
                        spallingAvgDepth += spallingDepth;
                        spallingAvgWidth += spallingWidth;

                        spallingMaxDepth = Math.Max(spallingMaxDepth, spallingDepth);
                        spallingMaxWidth = Math.Max(spallingMaxWidth, spallingWidth);
                    }

                    spallingAvgDepth /= spallingList.Count;
                    spallingAvgWidth /= spallingList.Count;
                }

                //AvgRngDepth
                var avgRngDepth = GetDouble(GetValueFromNode(jointItem, "AvgRngDepth"));

                //StdRngDepth
                var stdRngDepth = GetDouble(GetValueFromNode(jointItem, "StdRngDepth"));

                var jsonDataObject = new
                {
                    type = "Feature",
                    geometry = new
                    {
                        type = "Polyline",
                        coordinates = new List<double[]>()
                            {
                                coordinate1, coordinate2
                            }
                    },
                    properties = new
                    {
                        id = $"{idPrefix}{(long)jointId}",
                        file = FileName,
                        type = "Concrete Joint"
                    }
                };

                string jsonData = JsonSerializer.Serialize(jsonDataObject);

                var concreteJoint = new LCMS_Concrete_Joints
                {
                    SurveyId = surveyId,
                    SurveyDate = DateTime.Parse(surveyDate),
                    ImageFileIndex = FileName + ".jpg",
                    JointId = $"{idPrefix}{(long)jointId}",
                    JointDirection = direction,
                    Length_mm = length,
                    AvgWidth_mm = avgWidth,
                    AvgDepth_mm = avgDepth,
                    FaultingAvgHeight_mm = faultAvgHeight,
                    FaultingMaxHeight_mm = faultMaxHeight,
                    FaultingMinHeight_mm = faultMinHeight,
                    BadSealLength_mm = badSealLength,
                    BadSealAvgDepth_mm = badSealAvgDepth,
                    BadSealMaxDepth_mm = badSealMaxDepth,
                    SpallingLength_mm = spallingTotalLength,
                    SpallingAvgDepth_mm = spallingAvgDepth,
                    SpallingMaxDepth_mm = spallingMaxDepth,
                    SpallingAvgWidth_mm = spallingAvgWidth,
                    SpallingMaxWidth_mm = spallingMaxWidth,
                    MedianPercentRng = medianPercentRng,
                    MedianPercentInt = medianPercentInt,
                    PavementType = pavement,
                    GPSLatitude = coordinate1[1],
                    GPSLongitude = coordinate1[0],
                    GPSAltitude = gpsCoordinate.Altitude,
                    GPSTrackAngle = gpsCoordinate.TrackAngle,
                    EndGPSLatitude = coordinate2[1],
                    EndGPSLongitude = coordinate2[0],
                    EndGPSAltitude = gpsCoordinate.Altitude,
                    GeoJSON = jsonData,
                    RoundedGPSLatitude = Math.Round(coordinate1[1], 4),
                    RoundedGPSLongitude = Math.Round(coordinate1[0], 4),
                    SegmentId = Convert.ToInt32(sectionId),
                    AvgRngDepth_mm = avgRngDepth,
                    StdRngDepth_mm = stdRngDepth, 
                    Chainage = Y1 + chainage,
                    ChainageEnd = Y2 + chainage
                };
                concreteJointList.Add(concreteJoint);
            }
        }
        public static string ConvertTypeToDescription(int type)
        {
            return type switch
            {
                0 => "Not Detected",
                1 => "Detected",
                2 => "Positioned from previous section",
                3 => "User Defined or Default Position",
                4 => "Positioned next to Curb/DropOff",
                _ => "Unknown Type"  // Handles any unexpected type values
            };
        }

        //check if the object is inside the Survey polygon 
        private bool IsInsidePolygon(double latitude, double longitude, List<(double, double)> polygonCoordinates)
        {
            int count = 0;
            int n = polygonCoordinates.Count;

            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                double xi = polygonCoordinates[i].Item1;
                double yi = polygonCoordinates[i].Item2;
                double xj = polygonCoordinates[j].Item1;
                double yj = polygonCoordinates[j].Item2;

                bool condition1 = (yi > latitude) != (yj > latitude);
                bool condition2 = (longitude < (xj - xi) * (latitude - yi) / (yj - yi) + xi);

                if (condition1 && condition2)
                    count++;
            }

            return (count % 2 == 1);
        }

        private (string, List<(double, double)>) ReadPolygonCoordinatesFromJson(string filePath)
        {
            string surveyId = "";
            List<(double, double)> coordinates = new List<(double, double)>();
            if (string.IsNullOrEmpty(filePath))
            {
                Log.Information("No boundaries selected, Processing all without filtering");
                return (surveyId, coordinates); // Returning an empty tuple
            }
            try
            {
                string jsonContent = File.ReadAllText(filePath);
                JObject jsonObject = JObject.Parse(jsonContent);

                surveyId = jsonObject["properties"]["surveyId"].ToString();

                // Assuming the coordinates are under "geometry" -> "coordinates"
                JArray coordinatesArray = jsonObject["geometry"]["coordinates"] as JArray;

                if (coordinatesArray != null)
                {
                    foreach (JArray pointArray in coordinatesArray)
                    {
                        double latitude = (double)pointArray[1];
                        double longitude = (double)pointArray[0];
                        coordinates.Add((longitude, latitude));
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle the exception appropriately
                Log.Error($"Error reading polygon coordinates from JSON: {ex.Message}");
            }

            return (surveyId, coordinates);
        }
        private async void SaveSurveyInfo(string firstXml, string lastXml, string dataviewVersion)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(firstXml);

            string surveyIdExternal = doc.SelectSingleNode("/LcmsAnalyserResults/SurveyInfo/SurveyID").InnerText;
            string surveyPath = Directory.GetParent(Directory.GetParent(firstXml).FullName).FullName + Path.DirectorySeparatorChar;
            string surveyDate = doc.SelectSingleNode("/LcmsAnalyserResults/SystemData/SystemStatus/SystemTimeAndDate").InnerText;
            string sectionFileName = doc.SelectSingleNode("/LcmsAnalyserResults/ProcessingInformation/RoadSectionFileName").InnerText;
            var fileName = ExtractSurveyName(Path.GetFileNameWithoutExtension(sectionFileName));

            //Coordinates
            double longitude = 0.0;
            double latitude = 0.0;

            var gpsCoordinateNodeList = doc.SelectNodes("/LcmsAnalyserResults/GPSInformation/GPSCoordinate");
            if (gpsCoordinateNodeList != null)
            {
                if (gpsCoordinateNodeList != null && gpsCoordinateNodeList.Count > 0)
                {
                    XmlNode firstCoordinate = gpsCoordinateNodeList[0];
                    longitude = GetDoubleCoordinates(GetValueFromNode(firstCoordinate, "Longitude"));
                    latitude = GetDoubleCoordinates(GetValueFromNode(firstCoordinate, "Latitude"));
                }
            }

            //Start Chainage
            var chainage = GetDouble(doc.SelectSingleNode("/LcmsAnalyserResults/RoadSectionInfo/DistanceBegin_m").InnerText);

            //End Chainage
            XmlDocument doc2 = new XmlDocument(); 
            doc2.Load(lastXml);
            var endChainage = GetDouble(doc2.SelectSingleNode("/LcmsAnalyserResults/RoadSectionInfo/DistanceEnd_m").InnerText);

            // Log the latitude and longitude before saving
            Log.Information($"Saving Survey Info: SurveyIdExternal={surveyIdExternal}, Latitude={latitude}, Longitude={longitude}");

            var surveyInfo = new Survey
            {
                SurveyIdExternal = surveyIdExternal,
                SurveyName = fileName,
                SurveyDate = DateTime.Parse(surveyDate),
                ImageFolderPath = surveyPath,
                DataviewVersion = dataviewVersion,
                GPSLatitude = latitude,
                GPSLongitude = longitude,
                StartChainage = Math.Round(chainage, MidpointRounding.AwayFromZero),
                EndChainage = Math.Round(endChainage, MidpointRounding.AwayFromZero)
            };

            //save survey info
            var response = await _surveyService.Create(surveyInfo);

            Log.Information($"{response.Message}");
        }

        private string ExtractSurveyName(string surveyName)
        {
            // Remove file extension
            surveyName = Path.GetFileNameWithoutExtension(surveyName);

            // Remove "LCMS_" prefix if it exists
            if (surveyName.StartsWith("LCMS_"))
            {
                surveyName = surveyName.Substring(5);
            }

            // Split by underscores
            var parts = surveyName.Split('_');

            // Reconstruct until and including the 12-digit timestamp (e.g., 202505131817)
            var resultParts = new List<string>();

            foreach (var part in parts)
            {
                resultParts.Add(part);

                if (Regex.IsMatch(part, @"^\d{12}$"))
                {
                    break;
                }
            }

            // Join parts with underscore
            return string.Join("_", resultParts);
        }

        public string DetermineRavellingSeverity(double ravellingIndex, double lowThreshold, double medThreshold, double highThreshold)
        {
            if (ravellingIndex > lowThreshold && ravellingIndex < medThreshold)
            {
                return "Low";
            }
            else if (ravellingIndex > medThreshold && ravellingIndex < highThreshold)
            {
                return "Medium";
            }
            else if (ravellingIndex > highThreshold)
            {
                return "High";
            }
            else
            {
                return string.Empty;
            }
        }

        public string DetermineBleedingSeverity(double severityNumber)
        {
            string severity;
            switch (severityNumber)
            {
                case 0:
                    severity = "No Bleeding";
                    break;
                case 1:
                    severity = "Low";
                    break;
                case 2:
                    severity = "Medium";
                    break;
                case 3:
                    severity = "High";
                    break;
                default:
                    severity = string.Empty;
                    break;
            }

            return severity;
        }

        private int ReportXmlProgress(int batchIndex, int batchCount, int taskIndex, int totalTasks)
        {
            double batchPortion = 100.0 / batchCount;
            double completedBatches = batchIndex * batchPortion;
            double currentBatchProgress = (taskIndex / (double)totalTasks) * batchPortion;

            int percent = (int)(completedBatches + currentBatchProgress);
            return percent;
        }

        private async Task ProcessInsertQueriesAsync<T>(IEnumerable<T> data, string tableName, string taskDescription, int completedTask, CancellationToken cancellationToken)
        {
            if (data != null && data.Any())
            {
                try
                {
                    Log.Information($"Started {taskDescription}");
                    GeneralHelper.GetInsertQueries(data.ToList(), tableName);
                    Log.Information($"Completed {taskDescription}");
                }
                catch (Exception ex)
                {
                    Log.Error($"Error in generating INSERT queries for {taskDescription} : {ex.Message}");
                }
            }
        }
        public string GetValueFromNode(XmlNode doc, string fieldName)
        {
            string result = string.Empty;

            XmlNode? xmlselNode = null;
            try
            {
                if (doc != null)
                {
                    xmlselNode = doc.SelectSingleNode(fieldName);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error in GetValueFromNode {ex.Message}");
                return result;
            }

            if (xmlselNode != null)
            {
                result = xmlselNode.InnerText;
            }

            return result;
        }

        public double GetDoubleCoordinates(string param)
        {
            try
            {
                if (double.TryParse(param, out double result))
                {
                    // Check if the result is NaN
                    if (double.IsNaN(result))
                    {
                        return -1;  // or another default value of your choice
                    }
                    return result;
                }

                return -1;
            }
            catch (Exception ex)
            {
                return -1;
            }
        }

        public double GetDouble(string param)
        {
            try
            {
                if (double.TryParse(param, out double result))
                {
                    // Check if the result is NaN
                    if (double.IsNaN(result))
                    {
                        return -1;  // or another default value of your choice
                    }
                    return Math.Round(result, 2);
                }

                return 0;
            }
            catch (Exception ex)
            {
                return 0;
            }
        }
        public int GetInt(string param)
        {
            try
            {
                return Convert.ToInt32(param);
            }
            catch (Exception ex)
            {
                return 0;
            }
        }

        public float GetFloat(string param)
        {
            try
            {
                if (float.TryParse(param, out float result))
                {
                    // Check if the result is NaN
                    if (float.IsNaN(result))
                    {
                        return -1;  // or another default value of your choice
                    }
                    return result;
                }

                return 0;
            }
            catch (Exception ex)
            {
                return 0;
            }
        }

        private double? GetAverage(double?[] values)
        {
            var nonNulls = values.Where(v => v.HasValue).Select(v => v.Value).ToList(); 
            if (nonNulls.Count == 0) return 
                    null;
            return nonNulls.Average(); 
        }
        private void ClearLocalList()
        {
            newsegments.Clear();
            existingSegments.Clear();
            newpickOuts_Raws.Clear();
            newcracking_Raws.Clear();
            newravelling_Raws.Clear();
            newpatch_Processed.Clear();
            newpotholes_Processed.Clear();
            newcorner_Breaks.Clear();
            newspalling_Raws.Clear();
            newconcrete_Joints.Clear();
            newBleeding.Clear();
            newRut.Clear();
            newLaneMark.Clear();
            newPumping.Clear();
            newCurbDropoff.Clear();
            newSealedCrack.Clear();
            newWaterTrapment.Clear();
            newMarkingContour.Clear();
            newTexture.Clear();
            newMMO.Clear();
            newRumbleStrip.Clear();
            newRoughness.Clear();
            newGeomtry.Clear();
            newShove.Clear();
            newSegment_Grid.Clear();
            newGrooves.Clear();
            newSagsBumps.Clear();
            newPci.Clear();
        }

        //// Indicator for XML Processing Objects
        //public async Task<List<string>> VerifyLcmsObjectIndicator(ObjectsProcessedKeyRequestSurvey request)
        //{
        //    List<string> result = new List<string>();

        //    try
        //    {
        //        var LcmsObjectsKey = request.ProcesedObjectsKeyXML;
        //        string fileXML = request.FileXML;
        //        XDocument xmlDoc = XDocument.Load(fileXML);

               
        //        var normalizedLcmsObjectsKey = LcmsObjectsKey.ToDictionary(
        //            kvp => kvp.Key.ToLowerInvariant().Replace(" ", ""),
        //            kvp => kvp.Value.ToLowerInvariant().Replace(" ", "")
        //        );

        //        foreach (var kvp in normalizedLcmsObjectsKey)
        //        {                    
        //            var originalKey = LcmsObjectsKey.FirstOrDefault(
        //                original => original.Key.ToLowerInvariant().Replace(" ", "") == kvp.Key
        //            ).Key;

        //            if (kvp.Value.IndexOf(",") < 0)
        //            {
        //                var nodes = xmlDoc.Descendants()
        //                    .Where(x => x.Name.LocalName.ToLowerInvariant().Replace(" ", "") == kvp.Value);

        //                if (nodes.Any() && originalKey != null)
        //                {
        //                    result.Add(originalKey); 
        //                }
        //            }
        //            else
        //            {
        //                List<string> keys = kvp.Value.Split(",").Select(k => k.Trim()).ToList();
        //                foreach (string key in keys)
        //                {
        //                    var nodes = xmlDoc.Descendants()
        //                        .Where(x => x.Name.LocalName.ToLowerInvariant().Replace(" ", "") == key);

        //                    if (nodes.Any() && originalKey != null)
        //                    {
        //                        result.Add(originalKey); 
        //                        break; 
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Information($"Error mapping LCMS Objects in XML files for Icons: {ex.Message}");
        //    }

        //    return await Task.FromResult(result);
        //}

    }
}
