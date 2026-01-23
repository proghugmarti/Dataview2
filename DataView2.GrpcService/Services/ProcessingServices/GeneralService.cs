using DataView2.Core;
using DataView2.Core.Helper;
using DataView2.Core.Models;
using DataView2.Core.Models.CrackClassification;
using DataView2.Core.Models.LCMS_Data_Tables;
using DataView2.Core.Models.Other;
using DataView2.GrpcService.Data;
using DataView2.GrpcService.Helpers;
using DataView2.GrpcService.Protos;
using DataView2.GrpcService.Services;
using DataView2.GrpcService.Services.AppDbServices;
using DataView2.GrpcService.Services.OtherServices;
using Esri.ArcGISRuntime.Geometry;
using Grpc.Core;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using static DataView2.Core.Helper.TableNameHelper;
using static DataView2.Core.Helper.XMLParser;
using static DataView2.GrpcService.SurveySections;
using System.ServiceModel.Channels;
using DataView2.GrpcService.Services.Positioning;
using DataView2.Core.MultiInstances;
using static DataView2.Core.MultiInstances.SharedDVInstanceStore;


namespace DataView2.GrpcService.Services.ProcessingServices
{
    public class GeneralService : SurveyProcessing.SurveyProcessingBase
    {
        private readonly ILogger<GeneralService> _logger;
        private readonly AppDbContextProjectData _appDbContext;

        private readonly VideoFrameService _videoFrameService;
        private readonly XmlParsingService _xmlParsingService;
        private readonly Camera360FrameService _camera360FrameService;
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;
        private readonly GPSProcessedService _gPS_ProcessedService;
        private readonly ProcessingStateService _stateService;
        private readonly SurveyService _surveyService;
        private readonly SettingsService _settingsService;
        private readonly KeyCodeService _keyCodeService;

        private string _processingServicePath = ""; //Independent processing service.
        private int _portBase;
        private string _portWSProcess;
        private string _processPathRelease = "";
        private string _processingServiceUrl = "";
        private bool _processingServiceActive = false;
        private string _processingServiceLog = "";
        private bool _isDebug = false;

        public bool foundInvalidLicense = false;
        public List<string> detailLogViewHelpers = new List<string>();

        private ProcessingServiceManager _processingServiceClient;
        private string _databaseWSProcessing;

        public GeneralService(ILogger<GeneralService> logger,
            AppDbContextProjectData context,
            VideoFrameService videoFrameService,
            OdoDataService odoDataService,
            GPS_RawService gps_RawService,
            XmlParsingService xmlParsingService,
            Camera360FrameService camera360FrameService,
            IServiceProvider serviceProvider,
            IConfiguration configuration,
            GPSProcessedService gps_ProcessedService,
            SurveyService surveyService,
            KeyCodeService keyCodeService,
            SettingsService settingsService,
            ProcessingStateService stateService)
        {
            _appDbContext = context;
            _videoFrameService = videoFrameService;
            _xmlParsingService = xmlParsingService;
            _camera360FrameService = camera360FrameService;
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            _processingServiceClient = null;
            _isDebug = Debugger.IsAttached;
            _processingServicePath = _isDebug? _configuration.GetValue<string>("ProcessingServiceWSDebug") : _configuration.GetValue<string>("ProcessingService");
            _processingServiceUrl = _configuration.GetValue<string>("ProcessingServiceUrl");
            _processingServiceLog = _configuration.GetValue<string>("SrvcProcessingLogFile");
            _processPathRelease= _configuration.GetValue<string>("ProcessingService");
            _gPS_ProcessedService = gps_ProcessedService;
            _surveyService = surveyService;
            _stateService = stateService;
            _settingsService = settingsService;
            _keyCodeService = keyCodeService;
            _portBase = (_configuration["ProcessingServiceUrl"] is string url && Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))? uri.Port : 5001;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;

            public MEMORYSTATUSEX()
            {
                dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            }
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

        public override async Task ProcessSurvey(SurveyProcessingRequest request, IServerStreamWriter<SurveyProcessingResponse> responseStream, ServerCallContext context)
        {
            var detailLogs = new List<string>();
            var safeWriter = new SafeSurveyWriter<SurveyProcessingResponse>(responseStream);
            try
            {
                Log.Information($"ProcessSurvey => Start Survey Processing BF InitializeSurveyAsync");
                // --- Step 0: Initialize survey (Details + GPS) if files exist and work
                // ---  creates survey with surveyid as db id and sureyexternal id as the survey id too 
                var surveyInit = await InitializeSurveyAsync(request.FolderPath, request.DataviewVersion);

                var surveyRequest = new SurveyIdRequest();

                if (!surveyInit.Success)
                {
                    // Show a warning to the user, but continue processing
                    Log.Information($"ProcessSurvey => ERROR !surveyInit.Success");
                }
                else
                {
                    surveyRequest.SurveyId = surveyInit.SurveyId;
                    surveyRequest.SurveyExternalId = surveyInit.SurveyIdExternal;
                }

                //reset found invalid values to false to avoid having the value from previous processing
                foundInvalidLicense = false;

                //-- Continue processing as normal  --- 

                CreateProcessedObjectsFile(request);

                var LcmsObjects = request.ProcessingObjects.ToList();

                //Temp
                //bool processingSrvcStatus = ValidateAndRestartProcessingService(_processingServicePath);
                //_isDebug = false;

                _portWSProcess = Set_WSProcessing_Port(_processingServiceUrl);
                bool processingSrvcStatus = _isDebug ? ValidateAndRestartProcessingServiceDebugWS(_processingServicePath) : ValidateAndRestartProcessingService(_processingServicePath);  

                if (processingSrvcStatus && !string.IsNullOrEmpty(_portWSProcess))
                {
                    bool connectWS = InitializeProcessingServiceClient(_portWSProcess);
                    if (connectWS)
                    {
                        _processingServiceActive = true;

                        // -----------------------------------------------------------------------------------------

                        // 1) Prepare initial variables and check selected files
                        var (fisFileRequest, videoJsonFiles, dbFiles, videoPgrFiles) = PrepareInitialRequestData(request);
                        if (dbFiles == null && fisFileRequest == null && videoJsonFiles == null)
                        {
                            // If null, it means "Files not found" or something similar already handled.
                            return;
                        }

                        // 2) Process FIS files if needed from Windows Service:
                        if (fisFileRequest.FisFilesPath.Count > 0)
                        {
                            int batchSize = 100;
                            var response = await _settingsService.GetByName(new SettingName { name = "Batch Size" });
                            if (response != null && response.Count > 0)
                            {
                                var valueStr = response.FirstOrDefault().Value;
                                batchSize = Convert.ToInt32(valueStr);
                            }

                            await _processingServiceClient.ProcessSurveyAsync(request, batchSize, safeWriter, context.CancellationToken);

                            var multiProcessingLCMSVariables = await _processingServiceClient.GetMultiProcessingLCMSVariables(new EmptyWS());

                            foundInvalidLicense = multiProcessingLCMSVariables.FoundInvalidLicense;                            

                            detailLogViewHelpers.AddRange(multiProcessingLCMSVariables.DetailLogViewHelpers);
                        }
                        if (!_isDebug || (int.TryParse(_portWSProcess, out int port) && port > _portBase) || request.XmlOnly)
                        {
                            await StopProcessingService(_processingServicePath, _portWSProcess);
                            SharedDVInstanceStore.SetWSProcStatus("Connected", "Closed", _databaseWSProcessing);
                        }

                        // 3) Handle final steps if license was not invalid and xml only
                        if (!foundInvalidLicense || request.XmlOnly)
                        {
                            var offsetX = 0.0;
                            var offsetY = 0.0;
                            //fetch offset values
                            var horizontalOffsetResponse = await _settingsService.GetByName(new SettingName { name = "Horizontal Offset" });
                            var verticalOffsetResponse = await _settingsService.GetByName(new SettingName { name = "Vertical Offset" });

                            if (horizontalOffsetResponse.Count > 0 && verticalOffsetResponse.Count > 0)
                            {
                                var horizontalOffset = horizontalOffsetResponse.FirstOrDefault().Value;
                                var verticalOffset = verticalOffsetResponse.FirstOrDefault().Value;
                                offsetX = Convert.ToDouble(horizontalOffset);
                                offsetY = Convert.ToDouble(verticalOffset);
                            }

                            await HandleXmlsAndVideoAsync(
                                request,
                                fisFileRequest,
                                videoJsonFiles,
                                videoPgrFiles,
                                safeWriter,
                                context,
                                offsetX,
                                offsetY,
                                surveyRequest
                            );
                        }
                        else
                        {
                            if (surveyInit.Success)
                            {
                                //if fis file processing failed and no segment found in the survey, delete gps_processed
                                var segmentExists = _appDbContext.LCMS_Segment.Any(x => x.SurveyId == surveyInit.SurveyIdExternal);
                                if (segmentExists)
                                {
                                    await _gPS_ProcessedService.DeleteBySurveyID(surveyInit.SurveyId.ToString());
                                }
                            }
                        }
                       
                        // -----------------------------------------------------------------------------------------

                    }
                    else
                    {
                        await safeWriter.WriteAsync(new SurveyProcessingResponse
                        {
                            Error = "Processing Service is unable to connect."
                        });
                    }
                }
                else
                {
                    await safeWriter.WriteAsync(new SurveyProcessingResponse
                    {
                        Error = "Processing Service not available."
                    });
                }

            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message;
                Log.Error($"Error in ProcessSurvey : {ex.Message}");
               
                Match match = Regex.Match(errorMessage, @"StatusCode=\""([^\""]+)\""");
                string statusCode = match.Groups[1].Value;

                if (match.Success && statusCode.ToLower().Contains("unavailable"))
                {
                    errorMessage = "Service unavailable. Try again, or later..";
                }
                
                 if (!_isDebug || (int.TryParse(_portWSProcess, out int port) && port > _portBase))
                {
                    await StopProcessingService(_processingServicePath, _portWSProcess);
                    SharedDVInstanceStore.SetWSProcStatus("Connected", "Closed", _databaseWSProcessing);
                }

                await safeWriter.WriteAsync(new SurveyProcessingResponse
                {
                    Error = errorMessage,
                    DetailLogs = { detailLogs }
                });
            }
        }
        public override async Task SubscribeProcessingState(
         ProcessingStateRequest request,
         IServerStreamWriter<ProcessingStateResponse> responseStream,
         ServerCallContext context)
        {
            try
            {
                Log.Information("ProcessingState Service started");
                _stateService.AttachStream(responseStream, context.CancellationToken);
                while (!context.CancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(1000);
                    if (context.CancellationToken.IsCancellationRequested)
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
            }
            finally
            {
                _stateService.DetachStream();
            }
        }

        private bool InitializeProcessingServiceClient(string wsProcessPort)
        {
            Uri serviceUri = new Uri(_processingServiceUrl);
            string host = serviceUri.Host;
            if (_processingServiceClient != null)
            {
                _processingServiceClient.Dispose();
                UpdateDVInstances(wsProcessPort);
            }

            bool result = ProcessingServiceManager.TryInitialize($"http://{host}:{wsProcessPort}", out _processingServiceClient);

            if (result)
                SharedDVInstanceStore.SetWSProcStatus("Pending", "Connected", _databaseWSProcessing);

            return result;
        }

        private void UpdateDVInstances(string wsProcessPort)
        {
            SharedDVInstanceStore.SetProperties(
                propSearch: "WSProcPort",
                valueSearch: "wsProcessPort",
                valuesReplace: new[]
                {
                    new PropType { propAssign = "WSProcPort", valueAssign = "" },
                    new PropType { propAssign = "WSProcStatus",   valueAssign = "" }
                }
            );
        }

        private async Task<SurveyDetailsResult> InitializeSurveyAsync(string folderPath, string dataviewVersion)
        {
            SurveyDetailsResult surveyDetailsResult = new SurveyDetailsResult();

            try
            {
                _stateService.UpdateState(s =>
                {
                    s.Stage = ProcessingStage.ReadingPrerequisites;
                    s.StagePercentage = 0;
                    s.LastMessage = "Reading Survey Details json file...";
                });

                var parentFolderPath = Directory.GetParent(folderPath).FullName;

                var surveyDetailFile = Directory
                    .GetFiles(parentFolderPath, "Survey Details*.json", SearchOption.TopDirectoryOnly)
                    .FirstOrDefault();

                if (string.IsNullOrEmpty(surveyDetailFile))
                {
                    surveyDetailsResult.Success = false;
                    surveyDetailsResult.ErrorMessage = "InitializeSurveyAsync Survey details file not found. Video and chainage will not be processed.";
                    Log.Warning(surveyDetailsResult.ErrorMessage);
                    return surveyDetailsResult;
                }

                var survey = await _surveyService.ReadSurveyDetailsFile(surveyDetailFile, dataviewVersion);
                if (survey != null && survey.Id > 0)
                { 
                    var surveyRequest = new SurveyIdRequest
                    {
                        SurveyId = survey.Id,
                        SurveyExternalId = survey.SurveyIdExternal
                    };

                    _stateService.UpdateState(s =>
                    {
                        s.Stage = ProcessingStage.ReadingPrerequisites;
                        s.StagePercentage = 0;
                        s.LastMessage = "Processing odo and gps data...";
                    });

                    Log.Information($"InitializeSurveyAsync => Start ProcessGPS");
                    await _gPS_ProcessedService.ProcessGPS(parentFolderPath, surveyRequest);
                    Log.Information($"InitializeSurveyAsync => End ProcessGPS");

                    // Check for the Event Keys .json file
                    string[] eventKeysFiles = Directory.GetFiles(parentFolderPath, "Event Keys*.json");

                    if (eventKeysFiles.Length > 0)
                    {
                        string eventKeysFilePath = eventKeysFiles[0]; // Get the first matching file path

                        Log.Information($"InitializeSurveyAsync => Start Keycode with file: {eventKeysFilePath}");
                        await _keyCodeService.ProcessKeycode(eventKeysFilePath, surveyRequest);
                        Log.Information($"InitializeSurveyAsync => End Keycode");
                    }
                    else
                    {
                        Log.Warning($"No Event Keys .json file found in {parentFolderPath}");
                    }


                    // 07/01/2026 commented by Ivan while refining calculation process and not selected in the process screen
                    //await _gPS_ProcessedService.RunGeometryCalcFromGpsRaw(survey.Id, 1, 1000);

                    //assign survey request with returned survey information
                    surveyDetailsResult.Success = true;
                    surveyDetailsResult.SurveyId = survey.Id;
                    surveyDetailsResult.SurveyIdExternal = survey.SurveyIdExternal;
                }

                return surveyDetailsResult;

            }
            catch (Exception ex)
            {
                Log.Error($"Error initializing survey: {ex.Message}");
                surveyDetailsResult.Success = false;
                surveyDetailsResult.ErrorMessage  += $"// Error initializing survey: {ex.Message}";
                Log.Warning($"InitializeSurveyAsync => Errors: "+ surveyDetailsResult.ErrorMessage);
                return surveyDetailsResult;
            }
        }

        public bool ValidateAndRestartProcessingServiceDebugWS(string processPath)
        {
            try
            {
                if (Tools.IsProcessPortRunning(processPath, _portWSProcess))
                {
                    //SharedDVInstanceStore.SetWSProcPort(_portWSProcess.ToString(), _databaseWSProcessing);
                    Log.Information($"Debug: WSProcessing gRPC Server port: {_portWSProcess} already started.");
                    return true;
                }
                else
                    return StartWSProcessingService(processPath, _portWSProcess);
            }
            catch (Exception ex)
            {
                Log.Error($"Debug: Error starting Processing Work Service: {ex.Message}");
                return false;
            }
        }
        
        private bool StartWSProcessingService(string processPath, string port)
        {
            var logPrefix = _isDebug ? "Debug: " : string.Empty;
            try
            {
                if (!File.Exists(processPath))
                {
                    Log.Error($"Processing service executable not found: {processPath}");
                    return false;
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = processPath,
                    // FileName = "cmd.exe",Arguments = $"/k \"{_processPathRelease}\"",
                    WorkingDirectory = Path.GetDirectoryName(_processingServicePath),
                    Arguments = port,
                    CreateNoWindow = true,//false
                    UseShellExecute = false,//true
                    Environment =   {
                    ["GRPC_PORT"] = _portWSProcess
                    }
                });
                _processingServiceActive = true;
                SharedDVInstanceStore.SetWSProcPort(port, _databaseWSProcessing);
                Log.Information($"{logPrefix}Starting WSProcessing gRPC Server port: {port}");
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"{logPrefix}Error starting Processing Work Service process: {ex.Message}");
                return false;
            }
        }

        public bool ValidateAndRestartProcessingService(string processPath)
        {
           
            try
            {
                StartWSProcessingService(processPath, _portWSProcess);

            }
            catch (Exception ex)
            {
                Log.Error($"Error starting Processing Work Service: {ex.Message}");
                return false;
            }

            for (int i = 0; i < 5; i++)
            {
                if (Tools.IsProcessPortRunning(processPath, _portWSProcess))
                    return true;

                Thread.Sleep(5000);
            }

            return false;
        }
        public async Task StopProcessingService(string processPath, string port)
        {
            string processName = Path.GetFileNameWithoutExtension(processPath);
            var processes = Process.GetProcessesByName(processName);
            string message = $"There is no service for processing called: '{processName}'.";
            try
            {
                if (processes.Length == 0)
                {
                    Log.Information(message);
                    Logger.WriteLog(_processingServiceLog, message, Logger.TypeError.INFO);
                    return;
                }
                if (_isDebug && port=="5001") return;

                if (_processingServiceActive)
                {
                    if (Tools.StopProcessPortRunning(processPath, _portWSProcess))

                        _processingServiceActive = false;
                    message = $"Process '{processName}' port:{_portWSProcess} stopped successfully.";
                    Log.Information(message);
                    Logger.WriteLog(_processingServiceLog, message, Logger.TypeError.INFO);
                }
            }
            catch (Exception ex)
            {
                message = $"Error by stopping the service '{processName}': {ex.Message}";
                Log.Error(message);
                Logger.WriteLog(_processingServiceLog, message, Logger.TypeError.INFO);
            }
            await Task.CompletedTask;
        }

        public static void CreateProcessedObjectsFile(SurveyProcessingRequest request)
        {
            // Validate the request object
            if (request == null)
            {
                Console.WriteLine("SurveyProcessingRequest is null.");
                return;
            }

            // Validate the ProcessingObjects list
            if (request.ProcessingObjects == null || request.ProcessingObjects.Count == 0)
            {
                Console.WriteLine("Processing objects list is empty.");
                return;
            }

            //If fis files not found, avoid creating Processedobjects.csv for other processing like db, pgr files
            var fisFileExist = request.SelectedFiles.Any(file => file.EndsWith(".fis"));
            if (!fisFileExist) return;

            // Construct the XML result folder path using the survey's FolderPath
            string xmlFilesPath = Path.Combine(request.FolderPath, "XmlResult");

            // Ensure the directory exists
            if (!Directory.Exists(xmlFilesPath))
            {
                Directory.CreateDirectory(xmlFilesPath);
            }

            // Construct the full path to the ProcessedObjects file.
            // The file name is always "Processedobjects.csv" as specified.
            var processedObjectsFile = Path.Combine(xmlFilesPath, "Processedobjects.csv");

            // Create a HashSet to hold existing lines from the file for efficient lookup.
            HashSet<string> existingLines = new HashSet<string>();

            // If the file exists, read its lines into the HashSet.
            if (File.Exists(processedObjectsFile))
            {
                string[] lines = File.ReadAllLines(processedObjectsFile);
                foreach (string line in lines)
                {
                    string trimmed = line.Trim();
                    if (!string.IsNullOrEmpty(trimmed))
                    {
                        existingLines.Add(trimmed);
                    }
                }
            }

            // Prepare a list of new items to add (only items that are not already present).
            List<string> newItems = new List<string>();

            foreach (string item in request.ProcessingObjects)
            {
                string trimmedItem = item.Trim();
                if (!existingLines.Contains(trimmedItem))
                {
                    newItems.Add(trimmedItem);
                    existingLines.Add(trimmedItem);
                }
            }

            try
            {
                if (newItems.Count > 0)
                {
                    // If the file exists, append the new items; otherwise, create the file with the new items.
                    if (File.Exists(processedObjectsFile))
                    {
                        File.AppendAllLines(processedObjectsFile, newItems);
                        Console.WriteLine("New items added successfully to: " + processedObjectsFile);
                    }
                    else
                    {
                        File.WriteAllLines(processedObjectsFile, newItems);
                        Console.WriteLine("File created successfully at: " + processedObjectsFile);
                    }
                }
                else
                {
                    Console.WriteLine("No new items to add. File remains unchanged: " + processedObjectsFile);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error creating or updating file: " + ex.Message);
            }
        }



        /// <summary>
        /// Prepares the initial request data: sets up FisFileRequest, collects DB files,
        /// JSON files, etc. Returns (fisFileRequest, videoJsonFiles, dbFiles).
        /// If files are not found, returns (null, null, null, null, null).
        /// </summary>
        private (FisFileRequest FisFileRequest, List<string> VideoJsonFiles, List<string> DbFiles, List<string> VideoPgrFiles)
            PrepareInitialRequestData(SurveyProcessingRequest request)
        {
            var fisFileRequest = new FisFileRequest();
            var videoJsonFiles = new List<string>();
            var dbFiles = new List<string>();
            var videoPgrFiles = new List<string>();

            if (!request.SelectedFiles.Any())
            {
                // If there are no files at all, send an error response
                // (You might not have access to responseStream here directly,
                // so handle that in the caller if necessary.)
                return (null, null, null, null);
            }

            var allSelectedFiles = request.SelectedFiles.ToList();

            // Collect DB files
            dbFiles = allSelectedFiles.Where(file => file.EndsWith(".db")).ToList();

            // Collect FIS files
            var fisFiles = allSelectedFiles.Where(file => file.EndsWith(".fis")).ToList();
            fisFileRequest.FisFilesPath = fisFiles;

            // Collect JSON files (possibly for video rating)
            videoJsonFiles = allSelectedFiles.Where(file => file.EndsWith(".json")).ToList();

            //Collect PGR files
            videoPgrFiles = allSelectedFiles.Where(file => file.EndsWith(".pgr")).ToList();
            return (fisFileRequest, videoJsonFiles, dbFiles, videoPgrFiles);
        }

        /// <summary>
        /// Processes all ".db" files asynchronously, sending any necessary responses.
        /// </summary>
        //private async Task ProcessDatabaseFilesAsync(
        //    List<string> dbFiles,
        //    IServerStreamWriter<SurveyProcessingResponse> responseStream,
        //    CancellationToken cancellationToken)
        //{
        //    if (dbFiles.Count == 0) return;

        //    foreach (var dbfile in dbFiles)
        //    {
        //        try
        //        {
        //            // Process additional database file asynchronously
        //            var coordinates = await ProcessDatabaseTablesAsync(dbfile, responseStream, cancellationToken);

        //            // Check if coordinates are available
        //            if (coordinates.Count > 0)
        //            {
        //                // Use the coordinates to create and send SurveyProcessingResponse
        //                var surveyResponse = new SurveyProcessingResponse
        //                {
        //                    Type = "Coordinates",
        //                    Message = $"Data set ({dbfile}) completed",
        //                    Latitude = coordinates[0],
        //                    Longitude = coordinates.Count > 1 ? coordinates[1] : 0
        //                };

        //                Log.Information($"Coordinate surveyResponse: Longitude = {surveyResponse.Longitude}, Latitude = {surveyResponse.Latitude}");

        //                // Send the response via responseStream
        //                surveyResponse.DetailLogs.Add(
        //                    Newtonsoft.Json.JsonConvert.SerializeObject(new Core.Helper.DetailLogViewHelper
        //                    {
        //                        FileName = dbfile,
        //                        FileType = ".db",
        //                        LogDetails = surveyResponse.Message,
        //                        Status = "PASS"
        //                    })
        //                );
        //                await responseStream.WriteAsync(surveyResponse);
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            Log.Information($"Error Importing Dataset {dbfile} :  {ex.Message}");
        //            _multiProcessingLCMS.detailLogViewHelpers.Add(
        //                Newtonsoft.Json.JsonConvert.SerializeObject(new DetailLogViewHelper
        //                {
        //                    FileName = dbfile,
        //                    FileType = ".db",
        //                    LogDetails = $"Error Importing Dataset {dbfile} : {ex.Message}",
        //                    Status = "FAIL"
        //                })
        //            );

        //            var surveyResponse = new SurveyProcessingResponse
        //            {
        //                Error = $"Error Importing Dataset {dbfile} : {ex.Message}"
        //            };
        //            surveyResponse.DetailLogs.AddRange(_multiProcessingLCMS.detailLogViewHelpers);
        //            await responseStream.WriteAsync(surveyResponse);
        //            return;
        //        }
        //    }
        //}



        /// <summary>
        /// Handles the logic for FIS files, including license checks, parallel tasks, etc.
        /// Returns true if we determined that this is PCI processing (isPCI = true),
        /// or false otherwise.
        /// </summary>
        //private async Task<bool> HandleFisFilesAsync(
        //    SurveyProcessingRequest request,
        //    FisFileRequest fisFileRequest,
        //    IServerStreamWriter<SurveyProcessingResponse> responseStream,
        //    CancellationTokenSource cancellationTokenSource,
        //    ServerCallContext context)
        //{
        //    bool isPci = false;
        //    DateTime startTimeFis = DateTime.Now;

        //    try
        //    {
        //        var fisFiles = fisFileRequest.FisFilesPath.ToArray();
        //        List<Task> tasks = new List<Task>();

        //        // Check license before processing
        //        var licenseCheckResponse = await _multiProcessingLCMS.CheckLicenseBeforeProcessing(fisFiles, request.ProcessingObjects.ToList());
        //        if (!string.IsNullOrEmpty(licenseCheckResponse.Error))
        //        {
        //            await responseStream.WriteAsync(licenseCheckResponse);
        //            return false;
        //        }

        //        // Example modules
        //        var pciModules = new List<string>
        //        {
        //            LayerNames.Cracking, LayerNames.Rutting, LayerNames.Roughness,
        //            LayerNames.Potholes, LayerNames.CurbDropOff, LayerNames.Patch,
        //            LayerNames.Ravelling, LayerNames.Bleeding, "MTQ"
        //        };
        //        var LcmsObjects = request.ProcessingObjects.ToList();

        //        // Filter out Roughness, Sags Bumps, PPF ERD for the second task
        //        var filteredLcmsObjects = LcmsObjects
        //            .Where(lcms => lcms != "Roughness" && lcms != "Sags Bumps" && lcms != "PPF ERD")
        //            .ToList();

        //        var roughnessRelatedModules = LcmsObjects
        //                            .Where(lcms => lcms == "Roughness" || lcms == "Sags Bumps")
        //                            .ToList();

        //        // PCI with Roughness
        //        if (LcmsObjects.Contains("PCI with Roughness"))
        //        {
        //            isPci = true;
        //            Log.Information("Processing with Single dll for PCI");
        //            var combinedModules = pciModules.Union(LcmsObjects).ToList();

        //            var xmlPaths = await _multiProcessingLCMS.ProcessIRIandFisFilesAsync(
        //                fisFiles,
        //                request.CfgFolder,
        //                request.CfgFileName,
        //                combinedModules,
        //                responseStream,
        //                cancellationTokenSource,
        //                processType
        //            );

        //            if (xmlPaths.Any())
        //            {
        //                await _xmlParsingService.ProcessXMLFromMain(
        //                    request,
        //                    xmlPaths,
        //                    responseStream,
        //                    context,
        //                    combinedModules,
        //                    request.DataviewVersion,
        //                    true,
        //                    processType
        //                );
        //            }
        //        }
        //        else
        //        {
        //            // If Roughness is in the list, run parallel tasks
        //            if (LcmsObjects.Contains("Roughness"))
        //            {
        //                tasks.Add(Task.Run(async () =>
        //                {
        //                    var roughnessModule = new List<string> { "Roughness" };

        //                    if (LcmsObjects.Contains("Sags Bumps"))
        //                        roughnessModule.Add("Sags Bumps");

        //                    if (LcmsObjects.Contains("PPF ERD"))
        //                        roughnessModule.Add("PPF ERD");

        //                    // Process roughness first
        //                    var xmlPaths = await _multiProcessingLCMS.ProcessIRIandFisFilesAsync(
        //                        fisFiles,
        //                        request.CfgFolder,
        //                        request.CfgFileName,
        //                        roughnessModule,
        //                        responseStream,
        //                        cancellationTokenSource,
        //                        processType
        //                    );

        //                    if (xmlPaths.Any())
        //                    {
        //                        // Parsing roughness xmls
        //                        await _xmlParsingService.ProcessXMLFromMain(
        //                            request,
        //                            xmlPaths,
        //                            responseStream,
        //                            context, // context 
        //                            roughnessRelatedModules,
        //                            request.DataviewVersion,
        //                            true, processType
        //                        );
        //                    }

        //                    // Copy all generated PPF and ERD files
        //                    Log.Information("Copying PPF/ERD files at required location");
        //                    CopyPpfErdFiles(Path.Combine(request.FolderPath, "PpfResult"), "*.ppf");
        //                    CopyPpfErdFiles(Path.Combine(request.FolderPath, "ErdResult"), "*.erd");

        //                    // If LcmsObjects contains "PPF ERD", update detail logs
        //                    if (LcmsObjects.Contains("PPF ERD"))
        //                    {
        //                        if (_multiProcessingLCMS.detailLogViewHelpers != null && _multiProcessingLCMS.detailLogViewHelpers.Count > 0)
        //                        {
        //                            var ppfPath = Path.Combine(request.FolderPath, "PpfResult");
        //                            var erdPath = Path.Combine(request.FolderPath, "ErdResult");

        //                            List<string> updatedLogDetails = new List<string>();
        //                            foreach (string iriLog in _multiProcessingLCMS.detailLogViewHelpers)
        //                            {
        //                                DetailLogViewHelper logObj = Newtonsoft.Json.JsonConvert.DeserializeObject<DetailLogViewHelper>(iriLog);
        //                                if (logObj != null)
        //                                {
        //                                    logObj.PpfPath = ppfPath;
        //                                    logObj.ErdPath = erdPath;
        //                                }
        //                                updatedLogDetails.Add(Newtonsoft.Json.JsonConvert.SerializeObject(logObj));
        //                            }

        //                            _multiProcessingLCMS.detailLogViewHelpers.Clear();
        //                            _multiProcessingLCMS.detailLogViewHelpers.AddRange(updatedLogDetails);
        //                        }
        //                    }
        //                }));
        //            }


        //            if (filteredLcmsObjects.Count > 0)
        //            {
        //                tasks.Add(Task.Run(async () =>
        //                {
        //                    // Without roughness -- multiple dlls
        //                    if (LcmsObjects.Contains("PCI without Roughness"))
        //                    {
        //                        Log.Information("Processing PCI without roughness");
        //                        var pciModulesWithoutRoughness = pciModules.Where(module => module != LayerNames.Roughness).ToList();
        //                        var combinedModules = pciModulesWithoutRoughness.Union(LcmsObjects).ToList();

        //                        await _multiProcessingLCMS.ProcessFISFilesAsync(
        //                            fisFiles,
        //                            request.CfgFolder,
        //                            request.CfgFileName,
        //                            combinedModules,
        //                            responseStream,
        //                            cancellationTokenSource
        //                        );
        //                    }
        //                    else
        //                    {
        //                        // Process other modules
        //                        await _multiProcessingLCMS.ProcessFISFilesAsync(
        //                            fisFiles,
        //                            request.CfgFolder,
        //                            request.CfgFileName,
        //                            LcmsObjects,
        //                            responseStream,
        //                            cancellationTokenSource
        //                        );
        //                    }
        //                }));
        //            }

        //            // Wait for parallel tasks
        //            await Task.WhenAll(tasks);
        //        }

        //        Log.Information($"Total Time(seconds) for Fis Files Analysis: {Convert.ToInt64((DateTime.Now - startTimeFis).TotalSeconds)}");
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Information(ex.Message);
        //    }

        //    if (_multiProcessingLCMS.foundInvalidLicense)
        //    {
        //        Log.Error("Invalid license error occurred.");
        //        return false;
        //    }

        //    return isPci;
        //}

        /// <summary>
        /// Handles any final XML processing, video JSON processing, 
        /// and writes a success message at the end.
        /// </summary>
        private async Task HandleXmlsAndVideoAsync(
            SurveyProcessingRequest request,
            FisFileRequest fisFileRequest,
            List<string> videoJsonFiles,
            List<string> videoPgrFiles,
            SafeSurveyWriter<SurveyProcessingResponse> safeWriter,
            ServerCallContext context, 
            double offsetX,
            double offsetY,
            SurveyIdRequest surveyRequest)
        {
            var successSurveyResponse = new SurveyProcessingResponse();
            var xmlFiles = new List<string>();

            // 1) Retrieve or generate XML paths
            string xmlPath = Path.Combine(request.FolderPath, "XmlResult");
            if (!request.XmlOnly)
            {
                if (fisFileRequest.FisFilesPath.Count != 0 && Directory.Exists(xmlPath))
                {
                    string[] selectedFisFiles = request.SelectedFiles.ToArray();

                    if (detailLogViewHelpers.Count > 0) //after fis file processing just get successful fis files only to process xml
                    {
                        var successfulFisFiles = detailLogViewHelpers
                        .Select(json => Newtonsoft.Json.JsonConvert.DeserializeObject<DetailLogViewHelper>(json))
                        .Where(log => log.FileType == ".fis" && log.Status == "PASS")
                        .Select(log => Path.GetFileNameWithoutExtension(log.FileName))
                        .ToHashSet();

                        selectedFisFiles = selectedFisFiles
                           .Where(f => successfulFisFiles.Contains(Path.GetFileNameWithoutExtension(f)))
                           .ToArray();
                    }

                    string xmlExtension = ".xml";
                    var xmlsToRetrieve = selectedFisFiles.Select(filePath =>
                    {
                        string fileName = Path.GetFileNameWithoutExtension(filePath);
                        return $"{fileName}{xmlExtension}";
                    }).ToArray();

                    xmlFiles = Directory.GetFiles(xmlPath)
                                        .Where(filePath => xmlsToRetrieve.Contains(Path.GetFileName(filePath)))
                                        .OrderBy(x => xmlPath)
                                        .ToList();
                }
            }
            else
            {
                xmlFiles = request.SelectedFiles.OrderBy(x => xmlPath).ToList();
            }

            // 2) Process the XML data
           
            Log.Information("Start processing xml");
            await _xmlParsingService.ProcessXMLFromMain(
                request,
                safeWriter,
                xmlFiles,
                context,
                (percent, msg) =>
                {
                    _stateService.UpdateState(s =>
                    {
                        s.Stage = ProcessingStage.ProcessingXML;
                        s.StagePercentage = percent;
                        s.LastMessage = msg;
                    });
                },
                offsetX,
                offsetY
            );
            Log.Information("End processing xml");

            // 3) If video files are found, process video
            if (videoJsonFiles.Count > 0 && surveyRequest.SurveyId > 0)
            {
                await _videoFrameService.ProcessVideoRating(videoJsonFiles, request.VideoPath, surveyRequest, (percent, msg) =>
                {
                    _stateService.UpdateState(s =>
                    {
                        s.Stage = ProcessingStage.ProcessingVideo;
                        s.StagePercentage = percent;
                        s.LastMessage = msg;
                    });
                });
                successSurveyResponse.DetailLogs.AddRange(_videoFrameService.detailLogViewHelpers);
                _videoFrameService.detailLogViewHelpers.Clear();
            }

            // 360 Camera Video
            if(videoPgrFiles.Count > 0 && surveyRequest.SurveyId > 0)
            {
                await _camera360FrameService.ProcessPGRFiles(
                    videoPgrFiles,
                    surveyRequest.SurveyId, 
                    request.PgrOutputSize, 
                    request.PgrColorProcessing, 
                    request.PgrAdd6Images,
                    (percent, msg) =>
                    {
                        _stateService.UpdateState(s =>
                        {
                            s.Stage = ProcessingStage.ProcessingVideo;
                            s.StagePercentage = percent;
                            s.LastMessage = msg;
                        });
                    });
                successSurveyResponse.DetailLogs.AddRange(_camera360FrameService.detailLogViewHelpers);
                _camera360FrameService.detailLogViewHelpers.Clear();
            }

            if (request.XmlOnly)
            {
                successSurveyResponse.DetailLogs.AddRange(_xmlParsingService.detailLogViewHelpers);
                _xmlParsingService.detailLogViewHelpers.Clear();
            }
            else
            {
                successSurveyResponse.DetailLogs.AddRange(detailLogViewHelpers);
                detailLogViewHelpers.Clear();
            }

            // 4) Send final success message
            await safeWriter.WriteAsync(successSurveyResponse);
        }

        //private async Task<List<double>> MappedDataCopy<T>(string dbFilePath, IServerStreamWriter<SurveyProcessingResponse> responseStream, CancellationToken cancellationToken) where T : class
        //{
        //    try
        //    {
        //        var totalFiles = 12;
        //        var processedFiles = 0;
        //        SqliteCommand sqlGet;
        //        DataTable datatable;
        //        List<double> coordinates = new List<double>();
        //        double progress = (double)processedFiles / totalFiles * 100;

        //        await responseStream.WriteAsync(new SurveyProcessingResponse
        //        {
        //            Progress = (int)progress,
        //            CurrentStatus = typeof(T).Name
        //        });

        //        using (var sourceConn = new SqliteConnection($"Data Source={dbFilePath}"))
        //        using (var destConn = new SqliteConnection(_appDbContext.Database.GetConnectionString()))
        //        {
        //            sourceConn.Open();
        //            destConn.Open();
        //            switch (typeof(T).Name)
        //            {
        //                case "LCMS_Cracking_Raw":
        //                    sqlGet = sourceConn.CreateCommand();
        //                    sqlGet.CommandText = "SELECT * from LCMS_Cracking_Raw;";
        //                    datatable = GeneralHelper.GetDataTable(sourceConn.ConnectionString, sqlGet.CommandText);

        //                    List<LCMS_Cracking_Raw> cracking_Raws = GeneralHelper.BindList<LCMS_Cracking_Raw>(datatable);
        //                    GeneralHelper.GetCracksRawQueries(cracking_Raws.ToList());

        //                    break;

        //                case "LCMS_Bleeding":
        //                    sqlGet = sourceConn.CreateCommand();
        //                    sqlGet.CommandText = "SELECT * from LCMS_Bleeding;";
        //                    datatable = GeneralHelper.GetDataTable(sourceConn.ConnectionString, sqlGet.CommandText);

        //                    List<LCMS_Bleeding> bleedings = GeneralHelper.BindList<LCMS_Bleeding>(datatable);
        //                    GeneralHelper.GetBleedingQueries(bleedings.ToList());
        //                    break;
        //                case "LCMS_Geometry_Processed":
        //                    sqlGet = sourceConn.CreateCommand();
        //                    sqlGet.CommandText = "SELECT * from LCMS_Geometry_Processed;";
        //                    datatable = GeneralHelper.GetDataTable(sourceConn.ConnectionString, sqlGet.CommandText);

        //                    List<LCMS_Geometry_Processed> geometryProcesseds = GeneralHelper.BindList<LCMS_Geometry_Processed>(datatable);
        //                    GeneralHelper.GetGeometryProcessedQueries(geometryProcesseds.ToList());
        //                    break;
        //                case "LCMS_Concrete_Joints":
        //                    sqlGet = sourceConn.CreateCommand();
        //                    sqlGet.CommandText = "SELECT * from LCMS_Concrete_Joints;";
        //                    datatable = GeneralHelper.GetDataTable(sourceConn.ConnectionString, sqlGet.CommandText);

        //                    List<LCMS_Concrete_Joints> concrete_joints = GeneralHelper.BindList<LCMS_Concrete_Joints>(datatable);
        //                    GeneralHelper.GetConcreteJointsQueries(concrete_joints.ToList());
        //                    break;

        //                case "LCMS_Corner_Break":
        //                    sqlGet = sourceConn.CreateCommand();
        //                    sqlGet.CommandText = "SELECT * from LCMS_Corner_Break;";
        //                    datatable = GeneralHelper.GetDataTable(sourceConn.ConnectionString, sqlGet.CommandText);

        //                    List<LCMS_Corner_Break> corner_breaks = GeneralHelper.BindList<LCMS_Corner_Break>(datatable);
        //                    GeneralHelper.GetCornerBreaksQueries(corner_breaks.ToList());
        //                    break;

        //                //case "CrackClassifications":
        //                //    sqlGet = sourceConn.CreateCommand();
        //                //    sqlGet.CommandText = "SELECT * from CrackClassifications;";
        //                //    datatable = GeneralHelper.GetDataTable(sourceConn.ConnectionString, sqlGet.CommandText);

        //                //    List<Core.Helper.Crack> cracksClassifications = GeneralHelper.BindList<Core.Helper.Crack>(datatable);
        //                //    GeneralHelper.GetCracksQueries(cracksClassifications.ToList());
        //                //    break;

        //                //case "CrackClassificationNodes":
        //                //    sqlGet = sourceConn.CreateCommand();
        //                //    sqlGet.CommandText = "SELECT * from CrackClassificationNodes;";
        //                //    datatable = GeneralHelper.GetDataTable(sourceConn.ConnectionString, sqlGet.CommandText);

        //                //    List<Core.Helper.CrackNode> crackNodes = GeneralHelper.BindList<Core.Helper.CrackNode>(datatable);
        //                //    GeneralHelper.GetCrackNodesQueries(crackNodes.ToList());
        //                //    break;

        //                case "GPS_Processed":
        //                    sqlGet = sourceConn.CreateCommand();
        //                    sqlGet.CommandText = "SELECT * from GPS_Processed;";
        //                    datatable = GeneralHelper.GetDataTable(sourceConn.ConnectionString, sqlGet.CommandText);

        //                    List<GPS_Processed> gPS_Processeds = GeneralHelper.BindList<GPS_Processed>(datatable);
        //                    GeneralHelper.GetGPSProcessedQueries(gPS_Processeds.ToList());
        //                    break;

        //                case "LCMS_Patch_Processed":
        //                    sqlGet = sourceConn.CreateCommand();
        //                    sqlGet.CommandText = "SELECT * from LCMS_Patch_Processed;";
        //                    datatable = GeneralHelper.GetDataTable(sourceConn.ConnectionString, sqlGet.CommandText);

        //                    List<LCMS_Patch_Processed> patches = GeneralHelper.BindList<LCMS_Patch_Processed>(datatable);
        //                    GeneralHelper.GetPatchQueries(patches.ToList());
        //                    break;

        //                case "LCMS_PickOuts_Raw":
        //                    sqlGet = sourceConn.CreateCommand();
        //                    sqlGet.CommandText = "SELECT * from LCMS_PickOuts_Raw;";
        //                    datatable = GeneralHelper.GetDataTable(sourceConn.ConnectionString, sqlGet.CommandText);

        //                    List<LCMS_PickOuts_Raw> pickouts = GeneralHelper.BindList<LCMS_PickOuts_Raw>(datatable);
        //                    GeneralHelper.GetpickoutsQueries(pickouts.ToList());
        //                    break;

        //                case "LCMS_Potholes_Processed":
        //                    sqlGet = sourceConn.CreateCommand();
        //                    sqlGet.CommandText = "SELECT * from LCMS_Potholes_Processed;";
        //                    datatable = GeneralHelper.GetDataTable(sourceConn.ConnectionString, sqlGet.CommandText);

        //                    List<LCMS_Potholes_Processed> potholes_Processeds = GeneralHelper.BindList<LCMS_Potholes_Processed>(datatable);
        //                    GeneralHelper.GetPotholesQueries(potholes_Processeds.ToList());
        //                    break;

        //                case "LCMS_Ravelling_Raw":
        //                    sqlGet = sourceConn.CreateCommand();
        //                    sqlGet.CommandText = "SELECT * from LCMS_Ravelling_Raw;";
        //                    datatable = GeneralHelper.GetDataTable(sourceConn.ConnectionString, sqlGet.CommandText);

        //                    List<LCMS_Ravelling_Raw> ravelling_Raws = GeneralHelper.BindList<LCMS_Ravelling_Raw>(datatable);
        //                    GeneralHelper.GetRavellingQueries(ravelling_Raws.ToList());
        //                    break;

        //                case "LCMS_Segment":
        //                    sqlGet = sourceConn.CreateCommand();
        //                    sqlGet.CommandText = "SELECT * from LCMS_Segment;";
        //                    datatable = GeneralHelper.GetDataTable(sourceConn.ConnectionString, sqlGet.CommandText);

        //                    List<LCMS_Segment> segments = GeneralHelper.BindList<LCMS_Segment>(datatable);
        //                    GeneralHelper.GetSegmentsQueries(segments.ToList());
        //                    var segment = segments.FirstOrDefault();
        //                    if (segment != null)
        //                    {
        //                        double latitude = segment.GPSLatitude;
        //                        double longitude = segment.GPSLongitude;

        //                        coordinates = new List<double> { latitude, longitude };
        //                    }
        //                    break;

        //                case "LCMS_Spalling_Raw":
        //                    sqlGet = sourceConn.CreateCommand();
        //                    sqlGet.CommandText = "SELECT * from LCMS_Spalling_Raw;";
        //                    datatable = GeneralHelper.GetDataTable(sourceConn.ConnectionString, sqlGet.CommandText);

        //                    List<LCMS_Spalling_Raw> spalling_Raws = GeneralHelper.BindList<LCMS_Spalling_Raw>(datatable);
        //                    GeneralHelper.GetSpallingQueries(spalling_Raws.ToList());
        //                    break;

        //                case "SummaryCrackClasification":
        //                    sqlGet = sourceConn.CreateCommand();
        //                    sqlGet.CommandText = "SELECT * from SummaryCrackClasifications;";
        //                    datatable = GeneralHelper.GetDataTable(sourceConn.ConnectionString, sqlGet.CommandText);

        //                    List<SummaryCrackClasification> summaryCrackClasifications = GeneralHelper.BindList<SummaryCrackClasification>(datatable);
        //                    GeneralHelper.GetSummaryCrackClassificationQueries(summaryCrackClasifications.ToList());
        //                    break;

        //                case "Boundary":
        //                    sqlGet = sourceConn.CreateCommand();
        //                    sqlGet.CommandText = "SELECT * from Boundary;";
        //                    datatable = GeneralHelper.GetDataTable(sourceConn.ConnectionString, sqlGet.CommandText);

        //                    List<Boundary> boundaries = GeneralHelper.BindList<Boundary>(datatable);
        //                    GeneralHelper.GetboundriesQueries(boundaries.ToList());
        //                    break;
        //            }

        //            //deleting data to the database
        //            foreach (string query in GeneralHelper.deletequeries)
        //            {
        //                try
        //                {
        //                    sqlGet = destConn.CreateCommand();
        //                    sqlGet.CommandText = query;
        //                    sqlGet.ExecuteNonQuery();
        //                }
        //                catch (Exception ex)
        //                {
        //                    Log.Error($"Error in sql Delete : {ex.Message}");
        //                }
        //            }

        //            Log.Information($"Deleted records for {typeof(T).Name}");

        //            //inserting existing records
        //            foreach (string query in GeneralHelper.insertqueries)
        //            {
        //                try
        //                {
        //                    sqlGet = destConn.CreateCommand();
        //                    sqlGet.CommandText = query;
        //                    sqlGet.ExecuteNonQuery();
        //                }
        //                catch (Exception ex)
        //                {
        //                    Log.Error($"Error in sql Insert : {ex.Message}");
        //                }
        //            }

        //            Log.Information($"Inserted records for {typeof(T).Name}");

        //            sourceConn.Close();
        //            destConn.Close();
        //        }
        //        processedFiles++;
        //        return coordinates;
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error($"Error in MappedDataCopy : {ex.Message}");
        //        return new List<double>();
        //    }
        //}


        //public async Task<List<double>> ProcessDatabaseTablesAsync(string dbFilePath, IServerStreamWriter<SurveyProcessingResponse> responseStream, CancellationToken cancellationToken)
        //{
        //    var results = new List<List<double>>();


        //    foreach (var kvp in TableNameHelper.TableNameMappings)
        //    {
        //        // kvp.Key = "Segment"  (dictionary key)
        //        // kvp.Value = "LCMS_Segment"  (dictionary value = class name)

        //        // 1) Get the actual Type using the name from kvp.Value
        //        //    You need the full namespace + assembly name for Type.GetType to work reliably.
        //        //    For example, if LCMS_Segment is defined in MyApp.Models namespace,
        //        //    and the assembly is MyApp.dll, you might do:
        //        var typeName = $"MyApp.Models.{kvp.DBName}, MyApp";
        //        // Adjust the namespace and assembly to match your project

        //        var theType = Type.GetType(typeName);
        //        if (theType == null)
        //        {
        //            // Handle error: couldn't find the type
        //            Log.Error($"Type not found for '{typeName}'");
        //            continue;
        //        }

        //        // 2) Get the method info for MappedDataCopy<T>
        //        var methodInfo = this.GetType().GetMethod(
        //            nameof(MappedDataCopy),
        //            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public
        //        );

        //        // 3) Make the generic method with the actual Type
        //        var genericMethod = methodInfo.MakeGenericMethod(theType);

        //        // 4) Invoke it, passing the parameters (dbFilePath, responseStream, cancellationToken)
        //        var invokeResult = genericMethod.Invoke(
        //            this,
        //            new object[] { dbFilePath, responseStream, cancellationToken }
        //        );

        //        // 5) Await the returned Task<List<double>> (or whatever your return type is)
        //        var task = (Task<List<double>>)invokeResult;
        //        var resultValue = await task;

        //        // 6) Add the result to your list
        //        results.Add(resultValue);
        //    }


        //    // Check if all results are empty
        //    bool allEmpty = results.All(result => result.Count == 0);

        //    if (allEmpty)
        //    {
        //        Console.WriteLine("No coordinates in segment table found in the process of import tables .");
        //        return new List<double>(); // Return an empty list if all tables are empty

        //    }
        //    else
        //    {
        //        var coordinates = results.SelectMany(result => result).ToList();
        //        return coordinates;

        //    }
        //}

        public static double[] ConvertToGPSCoordinates(double x, double y, Core.Communication.GPSCoordinate coordinates)
        {
            double latitude = coordinates.Latitude;
            double longitude = coordinates.Longitude;
            double altitude = coordinates.Altitude;
            double trackAngle = coordinates.TrackAngle;

            if (x == 0 && y == 0)
            {
                double[] coordinate = { longitude, latitude };
                return coordinate;
            }

            double delta_X = x;
            double delta_Y = y;
            //double relativeAngle = (180 / Math.PI) * Math.Abs(Math.Atan(delta_X / delta_Y)) * (delta_X / Math.Abs(delta_X)) ;
            double relativeAngle = 180 / Math.PI * Math.Atan2(delta_X, delta_Y);
            double offset = Math.Sqrt(Math.Pow(delta_X, 2) + Math.Pow(delta_Y, 2)) / 1000;
            double heading = trackAngle + relativeAngle;
            double bearing = heading % 360.0;
            (double convLatitude, double convLongitude) = SurveyUtility.GPSPositionFromBearingAndOffset(latitude, longitude, bearing, offset);

            double[] result = { convLongitude, convLatitude };
            return result;
        }


        public static double CalculateDistance(double x1, double y1, double x2, double y2)
        {
            double distance = Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));
            return Math.Round(distance, 2);
        }



        public static double CalculateBearing(GPSCoordinate from, GPSCoordinate to)
        {
            double fromLatitude = DegreeToRadian(double.Parse(from.Latitude));
            double fromLongitude = DegreeToRadian(double.Parse(from.Longitude));
            double toLatitude = DegreeToRadian(double.Parse(to.Latitude));
            double toLongitude = DegreeToRadian(double.Parse(to.Longitude));

            double deltaLongitude = toLongitude - fromLongitude;

            double y = Math.Sin(deltaLongitude) * Math.Cos(toLatitude);
            double x = Math.Cos(fromLatitude) * Math.Sin(toLatitude) - Math.Sin(fromLatitude) * Math.Cos(toLatitude) * Math.Cos(deltaLongitude);

            double bearing = Math.Atan2(y, x);

            // Convert bearing from radians to degrees
            bearing = RadianToDegree(bearing);

            // Normalize the bearing to a compass bearing (0° to 360°)
            bearing = (bearing + 360) % 360;

            return bearing;
        }



        public static double CalculateBearing(gpsInfo from, gpsInfo to)
        {
            double fromLatitude = DegreeToRadian(from.Latitude);
            double fromLongitude = DegreeToRadian(from.Longitude);
            double toLatitude = DegreeToRadian(to.Latitude);
            double toLongitude = DegreeToRadian(to.Longitude);

            double deltaLongitude = toLongitude - fromLongitude;

            double y = Math.Sin(deltaLongitude) * Math.Cos(toLatitude);
            double x = Math.Cos(fromLatitude) * Math.Sin(toLatitude) - Math.Sin(fromLatitude) * Math.Cos(toLatitude) * Math.Cos(deltaLongitude);

            double bearing = Math.Atan2(y, x);

            // Convert bearing from radians to degrees
            bearing = RadianToDegree(bearing);

            // Normalize the bearing to a compass bearing (0° to 360°)
            bearing = (bearing + 360) % 360;

            return bearing;
        }


        private static double DegreeToRadian(double degree)
        {
            return degree * Math.PI / 180.0;
        }

        private static double RadianToDegree(double radian)
        {
            return radian * 180.0 / Math.PI;
        }

        private string Set_WSProcessing_Port(string processingServiceUrl)
        {
            try
            {
                DatabasePathProvider pathProvider = _serviceProvider.GetRequiredService<DatabasePathProvider>();
                _databaseWSProcessing = pathProvider.GetMetadataDatabasePath();

                if (string.IsNullOrWhiteSpace(processingServiceUrl))
                    return string.Empty;

                processingServiceUrl = processingServiceUrl.Trim();

                if (!Uri.TryCreate(processingServiceUrl, UriKind.Absolute, out var serviceUri))
                {
                    Console.WriteLine($"Invalid ProcessingServiceUrl: {processingServiceUrl}");
                    return string.Empty;
                }

                int originalPort = serviceUri.Port;

                //Debug mode + Default WS_Processing session Opened:
                if (_isDebug && Tools.NumProcessRunning(_processingServicePath, true) <= 1 && Tools.NumProcessRunning("DataView2.GrpcService", false) == 1) return originalPort.ToString();
                //More than 1 GrpcServces (instances) Opened => Main sesion use default 5001 port already Opened:
                else if (_isDebug && SharedDVInstanceStore.GetProperty(propSearch: "ProjectPath", valueSearch: _databaseWSProcessing, valueReturn: "MainSession") == "True") return originalPort.ToString();
                //For more than 1 Session:
                else return Tools.GetFreePort(originalPort).ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unable set WSProcessing port connection: " + ex.ToString());
                return string.Empty;
            }

        }

        //// Indicator for XML Processing Objects
        //public override async Task<ObjectsProcessedKeyReplySurvey> GetLcmsObjectIndicator(ObjectsProcessedKeyRequestSurvey request, ServerCallContext context)
        //{
        //    var response = new ObjectsProcessedKeyReplySurvey();
        //    try
        //    {
        //        var processedKeysList = await _xmlParsingService.VerifyLcmsObjectIndicator(request);
        //        foreach (var key in processedKeysList)
        //        {
        //            response.ProcesedObjectsXML.Add(key);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Information($"Error getting  XML Objects for Icons: {ex.Message}");
        //        response.ProcesedObjectsXML.Clear();
        //    }
        //    return response;
        //}

    }

}
