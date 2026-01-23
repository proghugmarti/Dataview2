using System.Runtime.InteropServices;
using Grpc.Core;
using System.Reflection;
using DataView2.Core.Models;
using DataView2.Packages.Lcms;
using DataView2.Core.Protos;
using DataView2.Core.Helper;
using static DataView2.Core.Helper.TableNameHelper;
using DataView2.Core.Communication;
using DataView2.Core;
using ProtoBuf.Grpc;
using Serilog;
using DataView2.WS.Processing.Protos;
using DataView2.Core.Models.Other;

namespace DataView2.WS.Processing.Services
{

    public class RoadInspectService : IRoadInspectService
    {

        int intensityImageType = 0x00000001; //intensity
        int rangeImageType = 0x00000002; //range
        uint stringLength = 0;
        sbyte[] pcOptions;
        private LcmsAnalyserLib lcmsAnalyserLib;
        int _ProcessUnit = 0;

        private readonly ILogger<RoadInspectService> _logger;

        public RoadInspectService(int analyserInstance, ILogger<RoadInspectService> logger)
        {
            _logger = logger;
            _ProcessUnit = analyserInstance;
            pcOptions = new sbyte[4096];

            _logger.LogInformation($"Start RoadInspectService: (RI1SE2B) :{analyserInstance}");

            try
            {
                string baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location); ;
                string TargetInstanceFolder = Path.Combine(baseDir, $"LcmsDll{analyserInstance}");
                string originalDllFolder = Path.Combine(baseDir, $"LcmsDll");

                CopyDirectory(originalDllFolder, TargetInstanceFolder);

                lcmsAnalyserLib = new LcmsAnalyserLib(analyserInstance);
            }
            catch (Exception ex)
            {
                _logger.LogError($"WSProcessing An error occurred while initializing the RoadInspectService. --> {ex.Message}");
                throw; // Optional: Re-throw the exception if you want the error to propagate further.
            }
        }
        static void CopyDirectory(string sourceDir, string destinationDir)
        {
            // Check if destination directory already exists
            if (Directory.Exists(destinationDir))
            {
                return;
            }
            // Create destination directory if it doesn't exist
            Directory.CreateDirectory(destinationDir);

            // Copy all files
            foreach (string filePath in Directory.GetFiles(sourceDir))
            {
                string fileName = Path.GetFileName(filePath);
                string destFilePath = Path.Combine(destinationDir, fileName);
                File.Copy(filePath, destFilePath, overwrite: true);
            }

            // Recursively copy subdirectories
            foreach (string subDir in Directory.GetDirectories(sourceDir))
            {
                string subDirName = Path.GetFileName(subDir);
                string destSubDir = Path.Combine(destinationDir, subDirName);
                CopyDirectory(subDir, destSubDir);
            }
        }

        public uint CheckLicenseModules(string path, List<string> LcmsObjects)
        {
            try
            {
                var openFile = lcmsAnalyserLib.LcmsOpenRoadSection(path);

                if (openFile == (int)LcmsAnalyserLib.ErrorCodes.LCMS_ANALYSER_NO_ERROR)
                {
                    sLcmsLicenseInfo lcmsLicenseInfo = new sLcmsLicenseInfo();
                    var getLicenseInfo = lcmsAnalyserLib.LcmsGetLicenseInfo(ref lcmsLicenseInfo);
                    if (getLicenseInfo == (int)LcmsAnalyserLib.ErrorCodes.LCMS_ANALYSER_NO_ERROR)
                    {
                        var availableModules = lcmsLicenseInfo.uiProcessModuleBitField;
                        uint selectedModules = GetModules(LcmsObjects);

                        uint unavailableModules = selectedModules & ~availableModules; // Modules that are selected but NOT available
                        uint enabledModules = selectedModules & availableModules; // Modules that are selected and available

                        if (unavailableModules != 0)
                        {
                            lcmsAnalyserLib.LcmsCloseRoadSection();
                            return unavailableModules;
                        }
                        else
                        {
                            _logger.LogInformation("All selected modules are available in the license.");
                            lcmsAnalyserLib.LcmsCloseRoadSection();
                            return 0; // No unavailable modules
                        }
                    }
                    else
                    {
                        return getLicenseInfo;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error occurred while License modules: {ex.Message}");
            }

            // In case of error or any issue, return a value indicating no valid modules (e.g., max uint)
            return uint.MaxValue;
        }


        //public async Task<(int resultCode, List<string> xmlPaths, List<string> processResults)> ProcessIRIAndFisFiles(
        //    string[] fisFiles,
        //    string cfgFolder,
        //    string cfgFileName,
        //    List<string> LcmsObjects,
        //    CancellationTokenSource cancellationTokenSource,
        //    IServerStreamWriter<SurveyWSProcessingResponse> surveyProcessingResponse)
        //{
        //    ByteValuePtr xmlResultBytes = new ByteValuePtr(new byte[4096]);
        //    nint xmlResultPtr = xmlResultBytes.Ref;

        //    try
        //    {
        //        List<string> xmlFiles = new List<string>();
        //        List<string> processResults = new List<string>();
        //        _logger.LogInformation($"1. Processing IRI in Unit {_ProcessUnit}");
        //        lcmsAnalyserLib.LcmsSetConfigFileName(cfgFolder, cfgFileName);

        //        bool paramsSet = SetConfigParams(LcmsObjects);
        //        if (!paramsSet)
        //        {
        //            _logger.LogError($"Processing IRI: Error in Params. not proceeding IRI further");
        //            return (-1, new List<string>(), new List<string>());
        //        }

        //        uint allModules = 0xFFFFFFFF;
        //        lcmsAnalyserLib.LcmsRemoveProcessingModuleToSelection(allModules);
        //        uint requiredModules = GetModules(LcmsObjects);
        //        var AddprocessingModules = lcmsAnalyserLib.LcmsAddProcessingModuleToSelection(requiredModules);

        //        var firstFis = fisFiles.FirstOrDefault();
        //        if (firstFis == null)
        //        {
        //            _logger.LogError("No FIS files provided for processing.");
        //            return (-1, new List<string>(), new List<string>());
        //        }

        //        DetailLogViewHelper firstFisLog = new DetailLogViewHelper();
        //        firstFisLog.FileName = firstFis;
        //        _logger.LogInformation($"2. Processing IRI: added processing modules and detailLogViewHelper");

        //        var openFile = lcmsAnalyserLib.LcmsOpenRoadSection(firstFis);

        //        if (openFile == (int)LcmsAnalyserLib.ErrorCodes.LCMS_ANALYSER_NO_ERROR)
        //        {
        //            // Perform IRI Processing
        //            var folderPath = Path.GetDirectoryName(firstFis);

        //            sLcmsSurveyInfo surveyInfo = new sLcmsSurveyInfo();
        //            var getSurveyInfo = lcmsAnalyserLib.LcmsGetSurveyInfo(ref surveyInfo, Timeout.Infinite);

        //            sLcmsRoadSectionInfo roadInfo = new sLcmsRoadSectionInfo();
        //            var getRoadInfo = lcmsAnalyserLib.LcmsGetRoadSectionInfo(ref roadInfo);

        //            if (getSurveyInfo == (int)LcmsAnalyserLib.ErrorCodes.LCMS_ANALYSER_NO_ERROR &&
        //                getRoadInfo == (int)LcmsAnalyserLib.ErrorCodes.LCMS_ANALYSER_NO_ERROR)
        //            {
        //                _logger.LogInformation($"3. Processing IRI: Get survey and road information");

        //                var distanceBegin = (float)roadInfo.dDistBE_m[0];
        //                var pathPrefix = Path.GetDirectoryName(firstFis);
        //                int resultReturn = 0; // 0 - return immediately, 1 - return at completion

        //                // Get the distance from selected fis files
        //                var selectedSections = fisFiles.Length;
        //                var totalLength = (float)(roadInfo.dSectionLength_m * selectedSections);

        //                Log.Information($"Processing IRI between {distanceBegin} and {totalLength} in Instance {_ProcessUnit}");
        //                var computeLongProfileResult = lcmsAnalyserLib.LcmsComputeLongitudinalProfile(distanceBegin, totalLength, pathPrefix, resultReturn);

        //                // Wait for completion
        //                float percentCompletion = 0;
        //                int isDone = 0;
        //                int waitTimeoutMs = 500;
        //                int lastReported = 0;

        //                while (isDone == 0)
        //                {
        //                    var waitResult = lcmsAnalyserLib.LcmsWaitComputeLongProfileCompletion(ref percentCompletion, ref isDone, waitTimeoutMs);
        //                    if (waitResult != (int)LcmsAnalyserLib.ErrorCodes.LCMS_ANALYSER_NO_ERROR)
        //                    {
        //                        processResults.Add(ErrorLogData(firstFisLog, LcmsObjects, $"Error waiting for longitudinal profile completion. Error Code: {waitResult}\""));
        //                        _logger.LogError($"Error waiting for longitudinal profile completion. Error Code: {waitResult}");
        //                        isDone = 1;
        //                        lcmsAnalyserLib.LcmsCloseRoadSection();
        //                        return ((int)waitResult, new List<string>(), processResults);
        //                    }
        //                    else
        //                    {
        //                        // Log & response every 5 percent progress
        //                        if (percentCompletion > lastReported + 5)
        //                        {
        //                            _logger.LogInformation($"Processing Longitudinal Profile: {percentCompletion}% of {totalLength} m");
        //                            lastReported = (int)percentCompletion;

        //                            var surveyResponse = new SurveyWSProcessingResponse();
        //                            surveyResponse.CurrentStatus = $"Processing Longitudinal Profile: {percentCompletion:F2}% of {totalLength} m";
        //                            surveyResponse.Progress = lastReported;
        //                            await surveyProcessingResponse.WriteAsync(surveyResponse);
        //                        }

        //                        if (cancellationTokenSource.IsCancellationRequested)
        //                        {
        //                            processResults.Add(ErrorLogData(firstFisLog, LcmsObjects, "A task cancelled by the user"));
        //                            _logger.LogError($"A task cancelled by the user");
        //                            return (-1, new List<string>(), processResults);
        //                        }
        //                    }
        //                }

        //                if (computeLongProfileResult != (int)LcmsAnalyserLib.ErrorCodes.LCMS_ANALYSER_NO_ERROR)
        //                {
        //                    processResults.Add(ErrorLogData(firstFisLog, LcmsObjects, $"Error computing longitudinal profile: {computeLongProfileResult}"));
        //                    _logger.LogError($"Error computing longitudinal profile: {computeLongProfileResult}");
        //                    lcmsAnalyserLib.LcmsCloseRoadSection();
        //                    return ((int)computeLongProfileResult, xmlFiles, processResults);
        //                }
        //                else
        //                {
        //                    lcmsAnalyserLib.LcmsCloseRoadSection();

        //                    try
        //                    {
        //                        _logger.LogInformation($"4. Completed processing IRI in Unit {_ProcessUnit}");

        //                        // Process fisfiles
        //                        _logger.LogInformation($"5. Started fis file processing for roughness in Unit {_ProcessUnit}");

        //                        var processed = 0;
        //                        foreach (var fisFile in fisFiles)
        //                        {
        //                            DetailLogViewHelper iriLog = new DetailLogViewHelper();
        //                            iriLog.FileName = fisFile;

        //                            if (cancellationTokenSource.IsCancellationRequested)
        //                            {
        //                                return (-1, new List<string>(), processResults);
        //                            }

        //                            var openfisFile = lcmsAnalyserLib.LcmsOpenRoadSection(fisFile);

        //                            if (openfisFile != (int)LcmsAnalyserLib.ErrorCodes.LCMS_ANALYSER_NO_ERROR)
        //                            {
        //                                processResults.Add(ErrorLogData(iriLog, LcmsObjects, $"Error opening file: {fisFile}"));
        //                                _logger.LogError($"Error opening file: {fisFile}");
        //                                continue;
        //                            }

        //                            var fileName = Path.GetFileNameWithoutExtension(fisFile);

        //                            var process = lcmsAnalyserLib.LcmsProcessRoadSection();
        //                            if (process == (int)LcmsAnalyserLib.ErrorCodes.LCMS_ANALYSER_NO_ERROR)
        //                            {
        //                                _logger.LogInformation($"Processing file: {fisFile} in Unit {_ProcessUnit}");
        //                                var progress = processed * 100 / fisFiles.Count();
        //                                var surveyResponse = new SurveyWSProcessingResponse();
        //                                surveyResponse.CurrentStatus = $"Processing FIS File: {fisFile}";
        //                                surveyResponse.Progress = progress;
        //                                await surveyProcessingResponse.WriteAsync(surveyResponse);

        //                                var result = lcmsAnalyserLib.LcmsGetResult(ref xmlResultPtr, ref stringLength);
        //                                if (result == (int)LcmsAnalyserLib.ErrorCodes.LCMS_ANALYSER_NO_ERROR)
        //                                {
        //                                    var xmlResult = Marshal.PtrToStringAnsi(xmlResultPtr);
        //                                    string xmlFolder = Path.Combine(folderPath, "XmlResult");

        //                                    if (!Directory.Exists(xmlFolder))
        //                                    {
        //                                        Directory.CreateDirectory(xmlFolder);
        //                                    }

        //                                    string xmlPath = null;
        //                                    if (LcmsObjects.Contains(LayerNames.PpfErd))
        //                                    {
        //                                        xmlPath = Path.Combine(xmlFolder, "IRI_" + fileName + ".xml");
        //                                    }
        //                                    else
        //                                    {
        //                                        xmlPath = Path.Combine(xmlFolder, fileName + ".xml");
        //                                    }

        //                                    var saveXmlResponse = SaveXmlData(xmlResult, xmlPath);

        //                                    if (saveXmlResponse != 0)
        //                                    {
        //                                        processResults.Add(ErrorLogData(iriLog, LcmsObjects, "Error in saving XML from IRI processing"));
        //                                        _logger.LogError($"Error in saving XML: {fisFile}");
        //                                        lcmsAnalyserLib.LcmsCloseRoadSection();
        //                                        continue;
        //                                    }
        //                                    else
        //                                    {
        //                                        iriLog.XMLPath = xmlPath;
        //                                        xmlFiles.Add(xmlPath);
        //                                        lcmsAnalyserLib.LcmsCloseRoadSection();
        //                                    }
        //                                    processed++;
        //                                }
        //                                else
        //                                {
        //                                    string errorName = Enum.GetName(typeof(LcmsAnalyserLib.ErrorCodes), process);
        //                                    _logger.LogError($"Error opening file: {fisFile} ERROR: {errorName} ");
        //                                    processResults.Add(ErrorLogData(firstFisLog, LcmsObjects, $"Error Opening File: {errorName}"));
        //                                }
        //                            }

        //                            else {
        //                                string errorName = Enum.GetName(typeof(LcmsAnalyserLib.ErrorCodes), process);
        //                                _logger.LogError($"Error opening file: {fisFile} ERROR: {errorName} ");
        //                                processResults.Add(ErrorLogData(firstFisLog, LcmsObjects, $"Error Opening File: {errorName}"));
        //                            }
        //                        }
        //                    }
        //                    finally
        //                    {
        //                        lcmsAnalyserLib.LcmsCloseRoadSection();
        //                    }
        //                    return ((int)computeLongProfileResult, xmlFiles, processResults);
        //                }
        //            }
        //            else
        //            {
        //                processResults.Add(ErrorLogData(firstFisLog, LcmsObjects, "Error getting survey info"));
        //                _logger.LogError($"Error getting survey info: {getSurveyInfo}");
        //                lcmsAnalyserLib.LcmsCloseRoadSection();
        //                return ((int)getSurveyInfo, xmlFiles, processResults);
        //            }
        //        }
        //        else
        //        {
        //            processResults.Add(ErrorLogData(firstFisLog, LcmsObjects, "Error opening file for IRI processing"));
        //            _logger.LogError($"Error opening file for IRI processing: {openFile}");
        //            return ((int)openFile, xmlFiles, processResults);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError($"Exception during IRI processing: {ex.Message}");
        //        return (-1, new List<string>(), new List<string>());
        //    }
        //    finally
        //    {
        //        DisposePtr(xmlResultBytes);
        //    }
        //}

        private string GetDetailLogData(string fisFile, List<string> lcmsObjects, string error = null)
        {
            
            string folderPath = Path.GetDirectoryName(fisFile);
            string fileName = Path.GetFileNameWithoutExtension(fisFile);
            var xmlFolder = Path.Combine(folderPath, "XmlResult");
            var xmlPath = Path.Combine(xmlFolder, fileName + ".xml");
            var ImageFolder = Path.Combine(folderPath, "ImageResult");
            var rangeImgPath = Path.Combine(ImageFolder, fileName + "_Range.jpg");
            var overlayImgPath = Path.Combine(ImageFolder, fileName + "_Overlay.jpg");

            var fisLog = new DetailLogViewHelper 
            {
                FileName = fileName,
                FileType = ".fis",
                Status = string.IsNullOrEmpty(error) ? "PASS" : "FAIL",
                SelectedLcmsObjects = string.Join(",", lcmsObjects),
                LogDetails = string.IsNullOrEmpty(error)
                    ? "Fis file processing completed successfully" //pass
                    : error //fail
            };

            if (File.Exists(xmlPath))
            {
                fisLog.XMLPath = xmlPath;
            }
            if (File.Exists(rangeImgPath) && File.Exists(overlayImgPath))
            {
                fisLog.ImagePath = ImageFolder;
            }

            if (lcmsObjects.Contains(LayerNames.PpfErd))
            {
                var ppfFolder = Path.Combine(folderPath, "PpfResult");
                var erdFolder = Path.Combine(folderPath, "ErdResult");
                fisLog.PpfPath = ppfFolder;
                fisLog.ErdPath = erdFolder;
            }
            return Newtonsoft.Json.JsonConvert.SerializeObject(fisLog);
        }

        public async Task<int> ComputeLongitudinalProfile(
            string[] fisFiles,
            string cfgFolder,
            string cfgFileName,
            List<string> LcmsObjects,
            CancellationTokenSource cancellationTokenSource,
            SafeSurveyWriter<SurveyWSProcessingResponse> safeWriter)
        {
            var firstFis = fisFiles.FirstOrDefault();
            if (firstFis == null)
            {
                _logger.LogError("No FIS files provided for processing.");
                return -1;
            }

            bool paramsSet = SetConfigParams(LcmsObjects);
            if (!paramsSet)
            {
                _logger.LogError($"Error while setting config parameters for batch from {fisFiles.First()} to {fisFiles.Last()}.");
                return -1;
            }

            //IRI processing
            try
            {
                _logger.LogInformation($"2. Processing IRI: added processing modules and detailLogViewHelper");

                var openFile = lcmsAnalyserLib.LcmsOpenRoadSection(firstFis);

                if (openFile != (int)LcmsAnalyserLib.ErrorCodes.LCMS_ANALYSER_NO_ERROR)
                {
                    _logger.LogError($"Error opening file for IRI processing: {openFile}");
                    return -1;
                }

                var folderPath = Path.GetDirectoryName(firstFis);

                sLcmsSurveyInfo surveyInfo = new sLcmsSurveyInfo();
                var getSurveyInfo = lcmsAnalyserLib.LcmsGetSurveyInfo(ref surveyInfo, Timeout.Infinite);

                sLcmsRoadSectionInfo roadInfo = new sLcmsRoadSectionInfo();
                var getRoadInfo = lcmsAnalyserLib.LcmsGetRoadSectionInfo(ref roadInfo);
                _logger.LogInformation($"3a. Processing IRI: Get survey and road information");
                if (getSurveyInfo != (int)LcmsAnalyserLib.ErrorCodes.LCMS_ANALYSER_NO_ERROR || getRoadInfo != (int)LcmsAnalyserLib.ErrorCodes.LCMS_ANALYSER_NO_ERROR)
                {
                    _logger.LogError($"Error getting survey info: {getSurveyInfo}");
                    lcmsAnalyserLib.LcmsCloseRoadSection();
                    return -1;
                }

                var distanceBegin = (float)roadInfo.dDistBE_m[0];
                var pathPrefix = Path.GetDirectoryName(firstFis);
                int resultReturn = 0; // 0 - return immediately, 1 - return at completion

                // Get the distance from selected fis files
                var selectedSections = fisFiles.Length;
                var totalLength = (float)(roadInfo.dSectionLength_m * selectedSections);

                Log.Information($"3b. Processing IRI between {distanceBegin} and {totalLength} in Instance {_ProcessUnit}");
                var computeLongProfileResult = lcmsAnalyserLib.LcmsComputeLongitudinalProfile(distanceBegin, totalLength, pathPrefix, resultReturn);

                // Wait for completion
                float percentCompletion = 0;
                int isDone = 0;
                int waitTimeoutMs = 500;
                int lastReported = 0;

                await safeWriter.WriteAsync(new SurveyWSProcessingResponse
                {
                    Message = $"Processing Longitudinal Profile of {totalLength} m...."
                });

                while (isDone == 0)
                {
                    var waitResult = lcmsAnalyserLib.LcmsWaitComputeLongProfileCompletion(ref percentCompletion, ref isDone, waitTimeoutMs);
                    if (waitResult != (int)LcmsAnalyserLib.ErrorCodes.LCMS_ANALYSER_NO_ERROR)
                    {
                        _logger.LogError($"Error waiting for longitudinal profile completion. Error Code: {waitResult}");
                        isDone = 1;
                        return -1;
                    }
                    else
                    {
                        // Log & response every 5 percent progress
                        if (percentCompletion > lastReported + 5)
                        {
                            _logger.LogInformation($"Processing Longitudinal Profile: {percentCompletion}% of {totalLength} m");
                            lastReported = (int)percentCompletion;
                        }
                    }

                    if (cancellationTokenSource.IsCancellationRequested)
                    {
                        _logger.LogError($"A task cancelled by the user");
                        return -1;
                    }
                }

                if (computeLongProfileResult != (int)LcmsAnalyserLib.ErrorCodes.LCMS_ANALYSER_NO_ERROR)
                {
                    _logger.LogError($"Error computing longitudinal profile: {computeLongProfileResult}");
                    return -1;
                }
                _logger.LogInformation($"4. Completed processing IRI in Unit {_ProcessUnit}");

                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return -1;
            }
            finally
            {
                lcmsAnalyserLib.LcmsCloseRoadSection(); //close first fis after iri processing
            }

        }

        public async Task<List<string>> ProcessIRIAndFisFiles(
            string[] fisFiles, 
            string cfgFolder, 
            string cfgFileName, 
            List<string> LcmsObjects, 
            CancellationTokenSource cancellationTokenSource,
            SafeSurveyWriter<SurveyWSProcessingResponse> safeWriter)
        {
            var detailLogs = new List<string>();

            try
            {
                _logger.LogInformation($"1. Processing IRI in Unit {_ProcessUnit}");
                //Process only selected LCMS objects
                uint allModules = 0xFFFFFFFF;
                uint selectedModules = GetModules(LcmsObjects);

                lcmsAnalyserLib.LcmsRemoveProcessingModuleToSelection(allModules);
                var AddprocessingModules = lcmsAnalyserLib.LcmsAddProcessingModuleToSelection(selectedModules);

                //Use selected Config file
                lcmsAnalyserLib.LcmsSetConfigFileName(cfgFolder, cfgFileName);

                var response = await ComputeLongitudinalProfile(fisFiles, cfgFolder, cfgFileName, LcmsObjects, cancellationTokenSource, safeWriter);
                if (response != 0)
                {
                    var firstFis = Path.GetFileName(fisFiles.FirstOrDefault());
                    var lastFis = Path.GetFileName(fisFiles.LastOrDefault());
                    var errorMessage = $"Failed to compute longitudinal profile; FIS file processing aborted for files from {firstFis} to {lastFis}.";
                    var detailLog = new DetailLogViewHelper
                    {
                        FileName = $"{firstFis} - {lastFis}",
                        FileType = ".fis",
                        LogDetails = errorMessage,
                        Status = "FAIL",
                    };
                    _logger.LogError(errorMessage);
                    detailLogs.Add(Newtonsoft.Json.JsonConvert.SerializeObject(detailLog));
                    return detailLogs;
                }

                //FisFile processing for ALL defects (not just iri)
                try
                { 
                    _logger.LogInformation($"5. Started fis file processing in Unit {_ProcessUnit}");

                    foreach (var file in fisFiles)
                    {
                        _logger.LogInformation($"PennyLog IRIAndFis Processing file: {Path.GetFileName(file)}");

                        await safeWriter.WriteAsync(new SurveyWSProcessingResponse
                        {
                            Message = $"Processing fis : {Path.GetFileName(file)}"
                        });

                        if (cancellationTokenSource.IsCancellationRequested)
                        {
                            _logger.LogError($"A task cancelled by the user");
                            return detailLogs;
                        }

                        var result = ProcessIndividualFisFile(file);
                        if (result != 0)
                        {
                            string errorMessage = null;
                            string errorName = Enum.GetName(typeof(LcmsAnalyserLib.ErrorCodes), result);

                            if (errorName != null)
                            {
                                errorMessage = $"Error while processing fis file '{file}' : {errorName}";
                            }
                            else
                            {
                                errorMessage = $"Unknwon Error while processing fis file '{file}' : code {result}, due to invalid config file selected.";
                            }

                            detailLogs.Add(GetDetailLogData(file, LcmsObjects, errorMessage));
                        }
                        else
                        {
                            detailLogs.Add(GetDetailLogData(file, LcmsObjects));
                        }
                    }

                    _logger.LogInformation($"6. Finished fis file processing in Unit {_ProcessUnit}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            return detailLogs;
        }
        public async Task<List<string>> ProcessBatchFisFile(
            string[] fisFiles, 
            string cfgFolder, 
            string cfgFileName, 
            List<string> LcmsObjects, 
            CancellationTokenSource cancellationTokenSource,
            SafeSurveyWriter<SurveyWSProcessingResponse> safeWriter)
        {
            var detailLogs = new List<string>();
            var firstFis = fisFiles.FirstOrDefault();
            if (firstFis == null)
            {
                _logger.LogError("No FIS files provided for processing.");
                return detailLogs;
            }
            try
            {
                var folderPath = Path.GetDirectoryName(fisFiles[0]);
                _logger.LogInformation($"1a. Processing batch in Unit {_ProcessUnit}");

                //Process only selected LCMS objects
                uint allModules = 0xFFFFFFFF;
                uint selectedModules = GetModules(LcmsObjects);

                lcmsAnalyserLib.LcmsRemoveProcessingModuleToSelection(allModules);
                var AddprocessingModules = lcmsAnalyserLib.LcmsAddProcessingModuleToSelection(selectedModules);

                //Use selected Config file
                lcmsAnalyserLib.LcmsSetConfigFileName(cfgFolder, cfgFileName);

                //Setting params for LCMS objects : Groove, Rumble Strip, Sags & Bumps, Shove
                bool paramsSet = SetConfigParams(LcmsObjects);
                if (!paramsSet)
                {
                    var errorMessage = $"Error while setting config parameters for batch from {fisFiles.First()} to {fisFiles.Last()}.";
                    detailLogs.Add(GetDetailLogData(firstFis, LcmsObjects, errorMessage));
                    return detailLogs;
                }

                foreach (var file in fisFiles)
                {
                    _logger.LogInformation($"PennyLog Processing file: {Path.GetFileName(file)}");

                    await safeWriter.WriteAsync(new SurveyWSProcessingResponse
                    {
                        Message = $"Processing fis : {Path.GetFileName(file)}"
                    });

                    if (cancellationTokenSource.IsCancellationRequested)
                    {
                        _logger.LogError($"A task cancelled by the user");
                        return detailLogs;
                    }

                    var result = ProcessIndividualFisFile(file);
                    if (result != 0)
                    {
                        string errorMessage = null;
                        string errorName = Enum.GetName(typeof(LcmsAnalyserLib.ErrorCodes), result);

                        if (errorName != null)
                        {
                            errorMessage = $"Error while processing fis file '{file}' : {errorName}";
                        }
                        else
                        {
                            errorMessage = $"Unknwon Error while processing fis file '{file}' : code {result}, due to invalid config file selected.";
                        }

                        detailLogs.Add(GetDetailLogData(file, LcmsObjects, errorMessage));
                    }
                  
                    else
                    {
                        detailLogs.Add(GetDetailLogData(file, LcmsObjects));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error: {ex.Message}");
                var errorMessage = $"Unexpected error while processing batch from {fisFiles.FirstOrDefault()} to {fisFiles.LastOrDefault()}: {ex.Message}";
                detailLogs.Add(GetDetailLogData(firstFis, LcmsObjects, ex.Message));
            }

            return detailLogs;
        }

        private int ProcessIndividualFisFile(string file)
        {
            ByteValuePtr xmlResultBytes = new ByteValuePtr(new byte[4096]);
            nint xmlResultPtr = xmlResultBytes.Ref;

            try
            {
                var folderPath = Path.GetDirectoryName(file);
                var FileName = Path.GetFileNameWithoutExtension(file);

                _logger.LogInformation($"2a. Opening file: {file}");

                //Open fis file
                var openFile = lcmsAnalyserLib.LcmsOpenRoadSection(file);
                if (openFile != (int)LcmsAnalyserLib.ErrorCodes.LCMS_ANALYSER_NO_ERROR)
                {
                    _logger.LogInformation($"2b. ERROR opening file: {file}  error: {openFile}");
                    return (int)openFile;
                }

                _logger.LogInformation($"3a. Processing file: {file}");

                //Process fis file
                var process = lcmsAnalyserLib.LcmsProcessRoadSection();
                if (process != (int)LcmsAnalyserLib.ErrorCodes.LCMS_ANALYSER_NO_ERROR)
                {
                    _logger.LogInformation($"3b. ERROR Processing file: {file} error: {process}");
                    return process;
                }

                string xmlResult = string.Empty;

                // Get XML result
                var result = lcmsAnalyserLib.LcmsGetResult(ref xmlResultPtr, ref stringLength);
                if (result != (int)LcmsAnalyserLib.ErrorCodes.LCMS_ANALYSER_NO_ERROR)
                {
                    _logger.LogInformation($"3b. ERROR getting xml result from: {file} error: {result}");
                    return (int)result;
                }

                // Save XML and Images
                int saveXmlResponse = 0;
                xmlResult = Marshal.PtrToStringAnsi(xmlResultPtr);
                string xmlFolder = Path.Combine(folderPath, "XmlResult");

                if (!Directory.Exists(xmlFolder))
                {
                    Directory.CreateDirectory(xmlFolder);
                }

                string xmlPath = Path.Combine(xmlFolder, FileName + ".xml");
                saveXmlResponse = SaveXmlData(xmlResult, xmlPath);
                if (saveXmlResponse != 0)
                {
                    _logger.LogError($"Error in saving xml result: {file}");
                    return saveXmlResponse;
                }

                var generateImage = GenerateLCMSImages(xmlResult, folderPath, FileName);
                if (generateImage != 0)
                {
                    _logger.LogError($"Error in generating LCMS images: {file}. ErrorCode : {generateImage}");
                    return generateImage;
                }

                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error: {ex.Message}");
                return -1;
            }
            finally
            {
                //Always dispose ptr after use and close dll
                DisposePtr(xmlResultBytes);
                lcmsAnalyserLib.LcmsCloseRoadSection();
            }
        }

        private int GenerateLCMSImages(string xmlResult, string folderPath, string fileName)
        {
            //generate both range and intensity without overlay
            ByteValuePtr rangeImageBytes = new ByteValuePtr(new byte[4096]);
            nint rangeImagePtr = rangeImageBytes.Ref;

            ByteValuePtr intensityImageBytes = new ByteValuePtr(new byte[4096]);
            nint intensityImagePtr = intensityImageBytes.Ref;

            ByteValuePtr rangeOverlayImageBytes = new ByteValuePtr(new byte[4096]);
            nint rangeOverlayImagePtr = rangeOverlayImageBytes.Ref;

            ByteValuePtr intensityOverlayImageBytes = new ByteValuePtr(new byte[4096]);
            nint intensityOverlayImagePtr = intensityOverlayImageBytes.Ref;

            try
            {
                // Get Image result and save 
                //uint modules = (uint)(ProcessModules.LCMS_PROC_MODULE_CRACKING | ProcessModules.LCMS_PROC_MODULE_CONCRETE_PAVMNT_JOINT | ProcessModules.LCMS_PROC_MODULE_PATCHDETECTION
                //    | ProcessModules.LCMS_PROC_MODULE_RAVELING | ProcessModules.LCMS_PROC_MODULE_POTHOLES | ProcessModules.LCMS_PROC_MODULE_PICKOUT | ProcessModules.LCMS_PROC_MODULE_MANMADEOBJECT 
                //    | ProcessModules.LCMS_PROC_MODULE_LANE_MARKING | ProcessModules.LCMS_PROC_MODULE_SEALED_CRACKING );
                int deleteImageResponse = 0;
                //uint selectedImageModules = GetModules(overlayImageModules);

                string ImageFolder = Path.Combine(folderPath, "ImageResult");

                if (!Directory.Exists(ImageFolder))
                {
                    Directory.CreateDirectory(ImageFolder);
                }

                var getRangeImage = lcmsAnalyserLib.LcmsGetResultImage(rangeImageType, ref rangeImagePtr);
                var getIntensityImage = lcmsAnalyserLib.LcmsGetResultImage(intensityImageType, ref intensityImagePtr);

                //Range Image
                if (getRangeImage == (int)LcmsAnalyserLib.ErrorCodes.LCMS_ANALYSER_NO_ERROR)
                {
                    sLcmsResultImage rangeImage = Marshal.PtrToStructure<sLcmsResultImage>(rangeImagePtr);
                    string rangeJpgPath = Path.Combine(ImageFolder, fileName + "_Range.jpg");

                    deleteImageResponse = DeleteExistingRangeImage(rangeJpgPath);
                    if (deleteImageResponse != 0)
                    {
                        _logger.LogError($"Error in deleting existing images");
                        return deleteImageResponse;
                    }

                    var saveRangeImage = lcmsAnalyserLib.LcmsSaveResultImage(rangeJpgPath, ref rangeImage);

                    ////Range Overlay
                    //if (imageOutputs.Contains("Range Overlay"))
                    //{
                    //    var getOverlayImage = lcmsAnalyserLib.LcmsCreateOverlayImage(xmlResult, selectedImageModules, ref pcOptions, ref rangeImage, ref rangeOverlayImagePtr);
                    //    if (getOverlayImage == (int)LcmsAnalyserLib.ErrorCodes.LCMS_ANALYSER_NO_ERROR)
                    //    {
                    //        sLcmsResultImage rangeOverlayImage = Marshal.PtrToStructure<sLcmsResultImage>(rangeOverlayImagePtr);
                    //        var rangeOverlayJpgPath = Path.Combine(ImageFolder, fileName + "_RngOverlay.jpg");
                    //        deleteImageResponse = DeleteExistingRangeImage(rangeOverlayJpgPath);
                    //        if (deleteImageResponse != 0)
                    //        {
                    //            _logger.LogError($"Error in deleting existing images");
                    //            return deleteImageResponse;
                    //        }

                    //       var saveRangeOverlayImage = lcmsAnalyserLib.LcmsSaveResultImage(rangeOverlayJpgPath, ref rangeOverlayImage);
                    //    }
                    //}
                }

                //Intensity Image
                if (getIntensityImage == (int)LcmsAnalyserLib.ErrorCodes.LCMS_ANALYSER_NO_ERROR)
                {
                    sLcmsResultImage intensityImage = Marshal.PtrToStructure<sLcmsResultImage>(intensityImagePtr);

                    var intensityJpgPath = Path.Combine(ImageFolder, fileName + "_Intensity.jpg");
                    deleteImageResponse = DeleteExistingIntensityImage(intensityJpgPath);
                    if (deleteImageResponse != 0)
                    {
                        _logger.LogError($"Error in deleting existing images");
                        return deleteImageResponse;
                    }
                    var saveIntensityImage = lcmsAnalyserLib.LcmsSaveResultImage(intensityJpgPath, ref intensityImage);

                    ////Intensity Overlay
                    //var getOverlayImage = lcmsAnalyserLib.LcmsCreateOverlayImage(xmlResult, selectedImageModules, ref pcOptions, ref intensityImage, ref intensityOverlayImagePtr);

                    //if (getOverlayImage == (int)LcmsAnalyserLib.ErrorCodes.LCMS_ANALYSER_NO_ERROR)
                    //{
                    //    sLcmsResultImage IntensityOverlayImage = Marshal.PtrToStructure<sLcmsResultImage>(intensityOverlayImagePtr);
                    //    string IntensityOverlayJpgPath = Path.Combine(ImageFolder, fileName + "_Overlay.jpg");
                    //    deleteImageResponse = DeleteExistingIntensityImage(IntensityOverlayJpgPath);
                    //    if (deleteImageResponse != 0)
                    //    {
                    //        _logger.LogError($"Error in deleting existing images");
                    //        return deleteImageResponse;
                    //    }

                    //    var saveIntensityOverlayImage = lcmsAnalyserLib.LcmsSaveResultImage(IntensityOverlayJpgPath, ref IntensityOverlayImage);
                    //}
                }

                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in generating LCMS images.");
                return -1;
            }
            finally
            {
                DisposePtr(rangeImageBytes);
                DisposePtr(intensityImageBytes);
                DisposePtr(rangeOverlayImageBytes);
                DisposePtr(intensityOverlayImageBytes);
            }
        }

        private void DisposePtr(ByteValuePtr byteValuePtr)
        {
            byteValuePtr?.Dispose();
        }

        private bool SetConfigParams(List<string> lcmsObjects)
        {
            try
            {
                uint paramsSet = 1;
                List<uint> allParamsSet = new List<uint>();

                if (lcmsObjects.Contains(LayerNames.RumbleStrip))
                {
                    /*0: Disable (default)
                      1: Enable the rumble strip detection in the lane mark area using the lane mark information.
                      2: Enable the rumble strip detection in all surfaces of the road.
                      3: Enable the rumble strip detection on both sides of the road simultaneously using the lane mark information.*/

                    allParamsSet.Add(SetParam("RumbleModule_RumbleStripEnable", 1));
                }

                if (lcmsObjects.Contains(LayerNames.Shove))
                {
                    //Set this parameter to 1 (enable) to perform shoving detection.

                    allParamsSet.Add(SetParam("ShovingModule_EnableShovingDetection", 1));
                }

                if (lcmsObjects.Contains(LayerNames.Grooves))
                {
                    /* The pavement type, as defined by the user. 
                       This setting will be ignored if the automatic pavement type detection is enabled. Possible values are:
                       1: Asphalt
                       2: Concrete
                       3: Grooved concrete (transversally)
                       4: Grooved concrete (longitudinally)
                       5: Highly textured (or porous)
                       6: Concrete CRCP (CRCP = Continuously Reinforced Concrete Pavement).
                       Note: Concrete CRCP is concrete pavement with no transversal joints. The library is looking only for longitudinal joints when the pavement type is set to Concrete CRCP. 
                       Note that the automatic pavement type detection algorithm cannot automatically detect Concrete CRCP pavement type. */

                    allParamsSet.Add(SetParam("CrackingModule_UserDefinedPavementType", 3));

                    /*Enable-disable the automatic pavement type detection algorithm. 
                      The pavement type can be: Asphalt, Concrete, Concrete with transverse grooves, Concrete with longitudinal grooves, highly textured or Continuously Reinforced Concrete Pavement*/

                    allParamsSet.Add(SetParam("CrackingModule_AutoPavementTypeDetection", 0));

                    //Set default value of groove zone to 250 -- NOT WORKING FOR SOME REASON : Error code 13
                    //allParamsSet.Add(SetParam("JointModule_GrooveZoneWidth_mm", 250));
                    //allParamsSet.Add(SetParam("JointModule_GrooveZoneHeight_mm", 250));
                }

                if (lcmsObjects.Contains(LayerNames.Roughness))
                {
                     //0: the elevation is output in meter
                     //1: the elevation is output in millimeter
                     //Default: 0

                    allParamsSet.Add(SetParam("RoughnessModule_ERD_Elevation_Unit", 1));

                    if (lcmsObjects.Contains(LayerNames.PpfErd))
                    {
                        //set to 1, elevation profiles in PPF format will be created.Set to 0 to disable
                        //Default: 1 

                        //set to 1, elevation profiles will also be saved in the ERD format
                        //Default: 0
                        allParamsSet.Add(SetParam("RoughnessModule_CreatePPFfile", 1));
                        allParamsSet.Add(SetParam("RoughnessModule_CreateERDfile", 1));
                    }
                    else
                    {
                        //set 0 for making off the creation of ppf files
                        allParamsSet.Add(SetParam("RoughnessModule_CreatePPFfile", 0));
                        allParamsSet.Add(SetParam("RoughnessModule_CreateERDfile", 0));
                    }

                    if (lcmsObjects.Contains(LayerNames.SagsBumps))
                    {
                        //Set this parameter to 1 to enable the detection of sags and bumps.
                        //A value of 0 will disable this module.
                        //The IRI module must also be selected to detect the sags and bumps.

                        allParamsSet.Add(SetParam("SagsBumpsModule_EnableDetection", 1));
                    }
                }

                if (lcmsObjects.Any(l => l.StartsWith("PCI")))
                {
                    //Enable-disable the edge cracking detection
                    allParamsSet.Add(SetParam("CrackingModule_EdgeCrackingEnable", 1));

                    //Default: 0
                    //Set this parameter to 1 to compute the PCI.
                    allParamsSet.Add(SetParam("PciModule_Enable", 1));

                    //Default: 1
                    //Set this parameter to 0 to disable the display of the PCI value on the result image.
                    allParamsSet.Add(SetParam("ResultRenderer_EnablePCIDisplay", 1));
                }

                if (lcmsObjects.Contains(LayerNames.CrackFaulting))
                {
                    //Enable-disable the crack faulting computation.
                    //Default is value disabled.
                    //The faulting values are reported in the XML strings only if faulting computation is enabled.
                    allParamsSet.Add(SetParam("CrackingModule_EnableCrackFaulting", 1));
                }

                //if (overlayImageModules.Contains(LayerNames.Ravelling))
                //{
                //    allParamsSet.Add(SetParam("ResultRenderer_EnableRavelingDisplay", 1));
                //}

                //if (overlayImageModules.Contains("MTQ"))
                //{
                //    allParamsSet.Add(SetParam("ResultRenderer_EnableJointFaultingValueDisplay", 1));
                //    allParamsSet.Add(SetParam("ResultRenderer_Display_Alligator_Cracks", 1));
                //    allParamsSet.Add(SetParam("ResultRenderer_Display_Multiple_Cracks", 1));
                //}

                if (allParamsSet.Count > 0 && allParamsSet.Any(p => p != (int)LcmsAnalyserLib.ErrorCodes.LCMS_ANALYSER_NO_ERROR))
                {
                    _logger.LogError($"One of the param was not set correctly. returning false.");
                    return false;
                }
                else return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"ERROR in setting parameters : {ex.Message}");
                return false;
            }
        }

        private unsafe uint SetParam(string paramName, int paramValue)
        {
            nint unmanagedMemory = Marshal.AllocHGlobal(sizeof(int));
            uint paramsSet = 1;

            try
            {
                void* pAutoPavementTypeDetection = (void*)unmanagedMemory;
                *(int*)pAutoPavementTypeDetection = paramValue;
                paramsSet = lcmsAnalyserLib.LcmsSetProcessingParams(paramName, (nint)pAutoPavementTypeDetection);
                if(paramsSet != (int)LcmsAnalyserLib.ErrorCodes.LCMS_ANALYSER_NO_ERROR)
                {
                    _logger.LogError($"Error in setting {paramName} parameter with value {paramValue} error code: {paramsSet}");
                }
                return paramsSet;
            }
            catch (Exception ex)
            {
                _logger.LogError($"ERROR in setting parameters : {ex.Message}");
                return paramsSet;
            }
            finally
            {
                Marshal.FreeHGlobal(unmanagedMemory);
            }
        }

        public int DeleteExistingRangeImage(string jpgPath)
        {
            return DeleteExistingImage(jpgPath) == 0 ? 0 : -2;
        }

        public int DeleteExistingIntensityImage(string jpgPath)
        {
            return DeleteExistingImage(jpgPath) == 0 ? 0 : -3;
        }

        public int DeleteExistingImage(string jpgPath)
        {
            try
            {
                if(File.Exists(jpgPath)) File.Delete(jpgPath);
                return 0;
            }
            catch (Exception ex)
            {
                Log.Error($"Error in deleting image {ex.Message}");
                return -1;
            }
        }

        public uint GetModules(List<string> selectedObjects)
        {
            HashSet<ProcessModules> uniqueModules = new HashSet<ProcessModules>();

            Dictionary<string, ProcessModules> objectToModuleMapping = new Dictionary<string, ProcessModules>
            {
                { LayerNames.Pickout, ProcessModules.LCMS_PROC_MODULE_PICKOUT },
                { LayerNames.Cracking, ProcessModules.LCMS_PROC_MODULE_CRACKING },
                { LayerNames.Ravelling, ProcessModules.LCMS_PROC_MODULE_RAVELING },
                { LayerNames.Potholes,ProcessModules.LCMS_PROC_MODULE_POTHOLES },
                { LayerNames.Patch, ProcessModules.LCMS_PROC_MODULE_PATCHDETECTION },
                { LayerNames.ConcreteJoint, ProcessModules.LCMS_PROC_MODULE_CONCRETE_PAVMNT_JOINT },
                { LayerNames.MMO, ProcessModules.LCMS_PROC_MODULE_MANMADEOBJECT },
                { LayerNames.LaneMarking, ProcessModules.LCMS_PROC_MODULE_LANE_MARKING },
                { LayerNames.RumbleStrip, ProcessModules.LCMS_PROC_MODULE_LANE_MARKING},  // + RumbleModule_RumbleStripEnable should be on in cfg
                { LayerNames.SealedCrack, ProcessModules.LCMS_PROC_MODULE_SEALED_CRACKING },
                { LayerNames.Bleeding, ProcessModules.LCMS_PROC_MODULE_BLEEDING },
                { LayerNames.Rutting, ProcessModules.LCMS_PROC_MODULE_RUTTING },
                { LayerNames.Roughness, ProcessModules.LCMS_PROC_MODULE_LONG_PROFILE },
                { LayerNames.SagsBumps, ProcessModules.LCMS_PROC_MODULE_LONG_PROFILE }, // +SagsBumpsModule_EnableDetection should be on in cfg
                { LayerNames.Pumping, ProcessModules.LCMS_PROC_MODULE_PUMPINGDETECTION },
                { LayerNames.CurbDropOff, ProcessModules.LCMS_PROC_MODULE_DROPOFF_CURB },
                { LayerNames.MacroTexture, ProcessModules.LCMS_PROC_MODULE_MACRO_TEXTURE },
                { LayerNames.Geometry, ProcessModules.LCMS_PROC_MODULE_SLOPE_AND_CROSS_SLOPE },
                { LayerNames.Shove, ProcessModules.LCMS_PROC_MODULE_RUTTING }, // + ShovingModule_EnableShovingDetection should be on in cfg
                { "MTQ", ProcessModules.LCMS_PROC_MODULE_COMPILATION },
                { LayerNames.PASER, ProcessModules.LCMS_PROC_MODULE_PASER },

                //WATER ENTRAPMENT - Both the rutting and the road geometry modules are required
            };
            uint selectedModules = 0;

            if (selectedObjects != null)
            {
                foreach (string selectedObject in selectedObjects)
                {
                    if (objectToModuleMapping.TryGetValue(selectedObject, out ProcessModules selectedModule))
                    {
                        uniqueModules.Add(selectedModule);
                    }

                    //Add MTQ cracking protocol when paser is selected
                    if (selectedObject == "PASER")
                    {
                        uniqueModules.Add(ProcessModules.LCMS_PROC_MODULE_COMPILATION);
                    }
                }
                
                foreach (var module in uniqueModules)
                {
                    selectedModules |= (uint)module;
                }
            }

            return selectedModules;
        }

        public int SaveXmlData(string xmlResult, string filePath)
        {
            try
            {
                File.WriteAllText(filePath, xmlResult);
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in saving XML data : {ex.Message}");
                return -1;
            }
        }

        public Task<ProgressResponse> CreateImageFile(ImageCreationRequest request, CallContext context = default)
        {

            //Fix imamge type later
            var filePath = request.ImageFilePath;
            var folder = Path.GetDirectoryName(filePath);
            var parentFolder = Path.GetDirectoryName(folder);
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var fisFilePath = Path.Combine(parentFolder, fileName + ".fis");
            var imageType = request.ImageType;
            ByteValuePtr resultImageBytes = new ByteValuePtr(new byte[4096]);
            nint resultImagePtr = resultImageBytes.Ref;


            var openFile = lcmsAnalyserLib.LcmsOpenRoadSection(fisFilePath);

            if (openFile == (int)LcmsAnalyserLib.ErrorCodes.LCMS_ANALYSER_NO_ERROR)
            {
                var process = lcmsAnalyserLib.LcmsProcessRoadSection();
                if (process == (int)LcmsAnalyserLib.ErrorCodes.LCMS_ANALYSER_NO_ERROR)
                {
                    if (imageType == "range")
                    {
                        var getImage = lcmsAnalyserLib.LcmsGetResultImage(0x00000002, ref resultImagePtr);
                        if (getImage == (int)LcmsAnalyserLib.ErrorCodes.LCMS_ANALYSER_NO_ERROR)
                        {
                            sLcmsResultImage resultImage = Marshal.PtrToStructure<sLcmsResultImage>(resultImagePtr);

                            string jpgPath = Path.Combine(folder, fileName + "_Range.jpg");
                            var saveImage = lcmsAnalyserLib.LcmsSaveResultImage(jpgPath, ref resultImage);
                            return Task.FromResult(new ProgressResponse { Message = "Range Image Saved." });
                        }
                    }
                    else if (imageType == "intensity")
                    {
                        var getImage = lcmsAnalyserLib.LcmsGetResultImage(0x00000001, ref resultImagePtr);
                        if (getImage == (int)LcmsAnalyserLib.ErrorCodes.LCMS_ANALYSER_NO_ERROR)
                        {
                            sLcmsResultImage resultImage = Marshal.PtrToStructure<sLcmsResultImage>(resultImagePtr);
                            string jpgPath = Path.Combine(folder, fileName + "_Intensity.jpg");
                            var saveImage = lcmsAnalyserLib.LcmsSaveResultImage(jpgPath, ref resultImage);
                            return Task.FromResult(new ProgressResponse { Message = "Intensity Image Saved." });
                        }
                    }
                }
            }
            lcmsAnalyserLib.LcmsCloseRoadSection();
            //lcmsAnalyserLib.Dispose();

            return Task.FromResult(new ProgressResponse { Message = ""});
        }
        public static double[] ConvertToGPSCoordinates(double x, double y, GPSCoordinate coordinates)
        {
            double latitude = coordinates.Latitude;
            double longitude = coordinates.Longitude;
            double altitude = coordinates.Altitude;
            double trackAngle = coordinates.TrackAngle;

            if(x == 0 && y == 0)
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


        //This only adds meters to the given coordinates in latitude , will need to add heading and others
        public static double[] addMetersToCoordinates(GPSCoordinate coordinates, double metersToAdd, double heading)
        {
            double latitude = coordinates.Latitude;
            double longitude = coordinates.Longitude;

            // Earth radius in meters (assuming a spherical Earth)
            double earthRadius = 6371000.0; // Approximately the average radius of the Earth

            // Calculate the change in latitude and longitude in degrees
            double deltaLatitude = metersToAdd / earthRadius;
            double deltaLongitude = metersToAdd / (earthRadius * Math.Cos(Math.PI * latitude / 180.0));

            // Calculate the new latitude and longitude
            double newLatitude = latitude + deltaLatitude;
            double newLongitude = longitude + deltaLongitude;

            // Update the track angle to match the heading
            double newTrackAngle = heading;

            double[] result = { newLatitude, newLongitude };
            // Return the updated coordinates
            return result;
        }

        public void Dispose()
        {
            lcmsAnalyserLib.Dispose();
        }   
    }
}
