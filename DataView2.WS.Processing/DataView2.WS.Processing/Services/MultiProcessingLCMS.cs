using DataView2.Core;
using DataView2.Core.Protos;
using DataView2.Packages.Lcms;
using Grpc.Core;
using Serilog;
using DataView2.WS.Processing.Protos;
using static DataView2.Core.Helper.TableNameHelper;
using DataView2.Core.Helper;
using DataView2.Core.Models;
using System.Threading;

namespace DataView2.WS.Processing.Services
{
    public class MultiProcessingLCMS
    {
        private static int _MaximumDegreeOfParallelism = 8;
        private int[] progressArray = new int[_MaximumDegreeOfParallelism];
        private RoadInspectService[] roadInspectService = new RoadInspectService[_MaximumDegreeOfParallelism];
        private List<SurveyWSProcessingResponse> processedFilesBuffer = new List<SurveyWSProcessingResponse>();
        public bool foundInvalidConfig = false;
        public bool foundInvalidLicense = false;
        private SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);
        private readonly IServiceProvider _serviceProvider;

        public List<string> detailLogViewHelpers = new List<string>();

        public MultiProcessingLCMS(IServiceProvider serviceProvider) {
            _serviceProvider = serviceProvider;
        }
        public async Task<SurveyWSProcessingResponse> CheckLicenseBeforeProcessing(string[] fisFileRequest, List<string> LcmsObjects)
        {
            try
            {
                var surveyResponse = new SurveyWSProcessingResponse();

                int AnalyserToBeUsed = await GetFirstAvailablePositionAsync();
                if (roadInspectService[AnalyserToBeUsed] == null)
                {
                    var logger = _serviceProvider.GetRequiredService<ILogger<RoadInspectService>>();
                    roadInspectService[AnalyserToBeUsed] = new RoadInspectService(AnalyserToBeUsed, logger);
                }

                Log.Information($"AnalyserToBeUsed for checking license: {AnalyserToBeUsed} ");

                var checkLicense = roadInspectService[AnalyserToBeUsed].CheckLicenseModules(fisFileRequest[0], LcmsObjects);
                if (checkLicense != 0)
                {
                    foundInvalidLicense = true;
                    if (checkLicense == uint.MaxValue)
                    {
                        surveyResponse.Error = "An error occurred while checking the modules.";
                    }
                    else if (new List<uint> { 100019, 100020, 100021, 100022, 100023 }.Contains(checkLicense))
                    {
                        /*100019 eERROR_LICENSE_FILE_NOT_FOUND,
                          100020 eERROR_EXPIRED_LICENSE
                          100021 eERROR_LICENCE_ACQUI_LIB_VER_NOT_SUPPORTED,
                          100022 eERROR_LICENCE_ANALYSIS_LIB_VER_NOT_SUPPORTED,
                          100023 eERROR_INVALID_LICENSE,
                          100024 eERROR_LICENSE_OPTION_NOT_ALLOWED,*/

                        foundInvalidConfig = false;
                        surveyResponse.Error = "Error occurred while processing : ";
                        switch (checkLicense)
                        {
                            case 100019:
                                surveyResponse.Error += "License file not found";
                                break;
                            case 100020:
                                surveyResponse.Error += "Expired license";
                                break;
                            case 100021:
                                surveyResponse.Error += "License acquisition lib version not supported";
                                break;
                            case 100022:
                                surveyResponse.Error += "License analysis lib version not supported";
                                break;
                            case 100023:
                                surveyResponse.Error += "Invalid license";
                                break;
                            case 100024:
                                surveyResponse.Error += "License option not allowed";
                                break;
                            default:
                                break;
                        }
                    }
                    else
                    {
                        List<string> unavailableModules = new List<string>();

                        foreach (ProcessModules module in Enum.GetValues(typeof(ProcessModules)))
                        {
                            if ((checkLicense & (uint)module) != 0)
                            {
                                string moduleName;
                                if (module.ToString() == "LCMS_PROC_MODULE_LONG_PROFILE")
                                {
                                    moduleName = "Roughness";
                                }
                                else if (module.ToString() == "LCMS_PROC_MODULE_SLOPE_AND_CROSS_SLOPE")
                                {
                                    moduleName = "Geometry";
                                }
                                else
                                {
                                    moduleName = module.ToString();
                                }
                                unavailableModules.Add(moduleName);
                            }
                        }
                        surveyResponse.Error = $"The following modules are unavailable: '{string.Join(", ", unavailableModules)}'. Check your License.";
                        foundInvalidConfig = false;
                    }
                }
                else
                {
                    foundInvalidLicense = false;
                }

                int position = await ReleasePosition(AnalyserToBeUsed);
                Log.Information($"Released Position after checking license : {position} ");

                return surveyResponse;
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to check a license: {ex.Message}"); 
                return new SurveyWSProcessingResponse { Error = "Failed to check a license. Please check the log." };
            }
        }

        //public async Task BatchProcessFisfiles(string[] fisFileRequest, string cfgFolder, string cfgFileName, List<string> LcmsObjects, IServerStreamWriter<SurveyWSProcessingResponse> responseStream, CancellationTokenSource cancellationTokenSource)
        //{
        //    try
        //    {
        //        int chunkSize = 10;
        //        var fileCount = fisFileRequest.Count();
        //        //this will be replaced by setting
        //        if (fileCount <= 50)
        //        {
        //            chunkSize = 5;
        //        }
        //        else if (fileCount <= 100)
        //        {
        //            chunkSize = 10;
        //        }
        //        else if (fileCount <= 500)
        //        {
        //            chunkSize = 50;
        //        }
        //        else
        //        {
        //            chunkSize = 100;
        //        }

        //        int totalChunks = (int)Math.Ceiling((double)fisFileRequest.Length / chunkSize);

        //        if (LcmsObjects.Contains(LayerNames.Roughness))
        //        {
        //            // Build tasks for each chunk
        //            var tasks = Enumerable.Range(0, totalChunks).Select(i =>
        //            {
        //                var chunkFisFileList = fisFileRequest
        //                    .Skip(i * chunkSize)
        //                    .Take(chunkSize)
        //                    .ToArray();

        //                return ProcessIRIAndFisFiles(chunkFisFileList, cfgFolder, cfgFileName, LcmsObjects, responseStream, cancellationTokenSource);
        //            });

        //            // Run them all in parallel
        //            await Task.WhenAll(tasks);
        //        }
        //        else
        //        {
        //            // Build tasks for each chunk
        //            var tasks = Enumerable.Range(0, totalChunks).Select(i =>
        //            {
        //                var chunkFisFileList = fisFileRequest
        //                    .Skip(i * chunkSize)
        //                    .Take(chunkSize)
        //                    .ToArray();

        //                return ProcessFisFilesGeneral(chunkFisFileList, cfgFolder, cfgFileName, LcmsObjects, responseStream, cancellationTokenSource);
        //            });

        //            // Run them all in parallel
        //            await Task.WhenAll(tasks);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error(ex.Message);
        //    }
        //    finally
        //    {
        //        CleanupServices();
        //    }
        //}

        //public async Task<List<string>> ProcessIRIandFisFilesAsync(string[] fisFileRequest, string cfgFolder, string cfgFileName, List<string> LcmsObjects, IServerStreamWriter<SurveyWSProcessingResponse> responseStream, CancellationTokenSource cancellationTokenSource, enumProcessTypes processType, List<string> overlayImageModules = null)
        //{

        //    List<string> resultXMLList = new List<string>();

        //    foundInvalidConfig = false;
        //    var surveyResponse = new SurveyWSProcessingResponse();

        //    //
        //    int chunkSize = 10; // number of Fis files per batch for IRI 

        //    try
        //    {
        //        string baseDirectory = AppContext.BaseDirectory;

        //        // Construir la ruta completa al archivo
        //        string filePath = Path.Combine(baseDirectory, "Services", "SegmentChunkSize.txt");

        //        //string filePath = "SegmentChunkSize.txt";
        //        if (File.Exists(filePath))
        //        {
        //            string chunkSizeText = File.ReadAllText(filePath);
        //            if (int.TryParse(chunkSizeText, out int parsedChunkSize))
        //            {
        //                chunkSize = parsedChunkSize;
        //            }
        //            else
        //            {
        //                throw new FormatException("El contenido de SegmentChunkSize.txt no es un número válido.");
        //            }
        //        }
        //        else
        //        {
        //            throw new FileNotFoundException($"El archivo {filePath} no se encontró.");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        // Manejo de errores y asignación de un valor predeterminado
        //        chunkSize = 10; // Valor predeterminado
        //        Log.Error($"Error al cargar ChunkSize: {ex.Message}. Usando valor predeterminado: {chunkSize}");
        //    }



        //    int totalChunks = (int)Math.Ceiling((double)fisFileRequest.Length / chunkSize);

        //    for (int i = 0; i < totalChunks; i++)
        //    {
        //        var chunkFisFileList = fisFileRequest.Skip(i * chunkSize).Take(chunkSize).ToArray();
        //        int AnalyserToBeUsed = await GetFirstAvailablePositionAsync();

        //        Console.WriteLine($"ProcessIRIandFisFilesAsync Processing chunk {i + 1}/{totalChunks}:");

        //        if (roadInspectService[AnalyserToBeUsed] == null)
        //        {
        //            var logger = _serviceProvider.GetRequiredService<ILogger<RoadInspectService>>();
        //            roadInspectService[AnalyserToBeUsed] = new RoadInspectService(AnalyserToBeUsed, logger);
        //        }
        //        Log.Information($"ProcessIRIandFisFilesAsync AnalyserToBeUsed : {AnalyserToBeUsed} ");

        //        var iriResult = await roadInspectService[AnalyserToBeUsed].ProcessIRIAndFisFiles(chunkFisFileList, cfgFolder, cfgFileName, LcmsObjects, cancellationTokenSource, responseStream, processType, overlayImageModules);

        //        resultXMLList.AddRange(iriResult.xmlPaths);

        //        if (iriResult.resultCode != 0)
        //        {
        //            Log.Error($"Error while processing IRI: code {iriResult.resultCode}");
        //        }
        //        else
        //        {
        //            //surveyResponse.CurrentStatus = "Completed IRI processing";
        //            Log.Information($"Completed IRI processing");
        //        }
        //        surveyResponse.DetailLogs.AddRange(iriResult.processResults);
        //        int position = await ReleasePosition(AnalyserToBeUsed);
        //        Log.Information($"Released Position : {position} ");


        //    }
            
        //    await responseStream.WriteAsync(surveyResponse);
        //    detailLogViewHelpers.AddRange(surveyResponse.DetailLogs);

        //    return resultXMLList;
        //}
        public async Task ProcessIRIAndFisFiles(string[] fisFileRequest, string cfgFolder, string cfgFileName, List<string> LcmsObjects, SafeSurveyWriter<SurveyWSProcessingResponse> safeWriter, CancellationTokenSource cancellationTokenSource)
        {
            int AnalyserToBeUsed = await GetFirstAvailablePositionAsync();

            try
            {
                foundInvalidConfig = false;
                if (roadInspectService[AnalyserToBeUsed] == null)
                {
                    var logger = _serviceProvider.GetRequiredService<ILogger<RoadInspectService>>();
                    roadInspectService[AnalyserToBeUsed] = new RoadInspectService(AnalyserToBeUsed, logger);
                }
                Log.Information($"ProcessIRIandFisFilesAsync AnalyserToBeUsed : {AnalyserToBeUsed} ");

                var result = await roadInspectService[AnalyserToBeUsed].ProcessIRIAndFisFiles(fisFileRequest, cfgFolder, cfgFileName, LcmsObjects, cancellationTokenSource, safeWriter);

                //add detail logs
                if (result.Any())
                {
                    detailLogViewHelpers.AddRange(result);
                }     
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
            }
            finally
            {
                int position = await ReleasePosition(AnalyserToBeUsed);
                Log.Information($"ProcessIRIandFisFilesAsync Released Position : {position} ");
            }
        }

        public async Task ProcessIRIForPpfAndErd(string[] fisFileRequest, string cfgFolder, string cfgFileName, List<string> LcmsObjects, SafeSurveyWriter<SurveyWSProcessingResponse> safeWriter, CancellationTokenSource cancellationTokenSource)
        {
            int AnalyserToBeUsed = await GetFirstAvailablePositionAsync();
            try
            {
                if (roadInspectService[AnalyserToBeUsed] == null)
                {
                    var logger = _serviceProvider.GetRequiredService<ILogger<RoadInspectService>>();
                    roadInspectService[AnalyserToBeUsed] = new RoadInspectService(AnalyserToBeUsed, logger);
                }
                Log.Information($"ProcessIRIForPpfAndErd AnalyserToBeUsed : {AnalyserToBeUsed} ");
                Log.Information($"1. Processing IRI in Unit {AnalyserToBeUsed} to generate ppf and erd");
                var result = await roadInspectService[AnalyserToBeUsed].ComputeLongitudinalProfile(fisFileRequest, cfgFolder, cfgFileName, LcmsObjects, cancellationTokenSource, safeWriter);

                if (result != 0)  //fail
                {
                    var folderPath = Path.GetDirectoryName(fisFileRequest.FirstOrDefault());
                    var detailLog = new DetailLogViewHelper
                    {
                        FileName = folderPath,
                        FileType = ".ppf / .erd",
                        LogDetails = "Failed to compute longitudinal profile: ppf and erd files were not generated.",
                        Status = "FAIL",
                    };

                    //ppf and erd fail log
                    detailLogViewHelpers.Add(Newtonsoft.Json.JsonConvert.SerializeObject(detailLog));
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
            }
            finally
            {
                int position = await ReleasePosition(AnalyserToBeUsed);
                Log.Information($"ProcessIRIForPpfAndErd Released Position : {position} ");
            }
        }

        public async Task ProcessFisFilesGeneral(string[] fisFileRequest, string cfgFolder, string cfgFileName, List<string> LcmsObjects, SafeSurveyWriter<SurveyWSProcessingResponse> safeWriter, CancellationTokenSource cancellationTokenSource)
        {
            int AnalyserToBeUsed = await GetFirstAvailablePositionAsync();

            Log.Information($"ProcessFisFilesGeneral AnalyserToBeUsed : {AnalyserToBeUsed} ");
            try
            {
                if (roadInspectService[AnalyserToBeUsed] == null)
                {
                    var logger = _serviceProvider.GetRequiredService<ILogger<RoadInspectService>>();
                    roadInspectService[AnalyserToBeUsed] = new RoadInspectService(AnalyserToBeUsed, logger);
                }
                
                var result = await roadInspectService[AnalyserToBeUsed].ProcessBatchFisFile(fisFileRequest, cfgFolder, cfgFileName, LcmsObjects, cancellationTokenSource, safeWriter);
                if (result.Any())
                {
                    detailLogViewHelpers.AddRange(result);
                }               
            }
            catch (Exception ex)
            {
                Log.Information(ex.Message);
            }
            finally
            {
                int position = await ReleasePosition(AnalyserToBeUsed);
                Log.Information($"ProcessFisFilesGeneral Released Position : {position} ");
            }
        }

        //public async Task ProcessFISFilesAsync(string[] fisFileRequest, string cfgFolder, string cfgFileName, List<string> LcmsObjects, List<string> overlayImageModules, IServerStreamWriter<SurveyWSProcessingResponse> responseStream, CancellationTokenSource cancellationTokenSource)
        //{
        //    try
        //    {
        //        var options = new ParallelOptions()
        //        {
        //            CancellationToken = cancellationTokenSource.Token,
        //            MaxDegreeOfParallelism = _MaximumDegreeOfParallelism
        //        };

        //        List<int> result = new List<int>();

        //        int processedFiles = 0;
        //        int progress = 0;
        //        int totalFiles = fisFileRequest.Count();
        //        int batchSize;

        //        if (totalFiles <= 10)
        //        {
        //            batchSize = 2;
        //        }
        //        else if (totalFiles <= 50)
        //        {
        //            batchSize = 5;
        //        }
        //        else if (totalFiles <= 100)
        //        {
        //            batchSize = 10;
        //        }
        //        else if (totalFiles <= 300)
        //        {
        //            batchSize = 15;
        //        }
        //        else
        //        {
        //            batchSize = 25;
        //        }

        //        try
        //        {
        //            //processing the first file to check the config file is proper or not
        //            #region first file process
        //            foundInvalidConfig = false;
        //            var surveyResponse = new SurveyWSProcessingResponse();
        //            int AnalyserToBeUsed = await GetFirstAvailablePositionAsync();
        //            if (roadInspectService[AnalyserToBeUsed] == null)
        //            {
        //                var logger = _serviceProvider.GetRequiredService<ILogger<RoadInspectService>>();
        //                roadInspectService[AnalyserToBeUsed] = new RoadInspectService(AnalyserToBeUsed, logger);
        //            }
        //            Log.Information($"AnalyserToBeUsed : {AnalyserToBeUsed} ");

        //            List<string> newLcmsObjects = new List<string>(LcmsObjects);

        //            //remove roughness and sags bumps if exist
        //            if (newLcmsObjects.Contains("Roughness"))
        //            {
        //                newLcmsObjects.Remove("Roughness");
        //            }

        //            if (newLcmsObjects.Contains("Sags Bumps"))
        //            {
        //                newLcmsObjects.Remove("Sags Bumps");
        //            }

        //            //Processing first fis file
        //            int j = roadInspectService[AnalyserToBeUsed].ProcessIndividualFisFile(fisFileRequest[0], cfgFolder, cfgFileName, newLcmsObjects, overlayImageModules);

        //            if (j != 0)
        //            {
        //                var fileName = Path.GetFileName(fisFileRequest[0]);
        //                surveyResponse.ErrorCode = j;
        //                if (j == -1)//error received in saving xmls
        //                {
        //                    surveyResponse.Error = $"Error while processing fis file '{fileName}' : FailedToSaveXml";
        //                }
        //                else if (j == -2)//error received in saving range images
        //                {
        //                    surveyResponse.Error = $"Error while processing fis file '{fileName}' : FailedToSaveRangeImage";
        //                }
        //                else if (j == -3)//error received in saving overlay images
        //                {
        //                    surveyResponse.Error = $"Error while processing fis file '{fileName}' : FailedToSaveOverlayImage";
        //                }
        //                else
        //                {
        //                    string errorName = Enum.GetName(typeof(LcmsAnalyserLib.ErrorCodes), j);

        //                    if (errorName != null)
        //                    {
        //                        surveyResponse.Error = $"Error while processing fis file '{fileName}' : {errorName}";
        //                    }
        //                    else
        //                    {
        //                        foundInvalidConfig = true;
        //                        surveyResponse.IncorrectConfig = true;
        //                        surveyResponse.Error = $"Unknwon Error while processing fis file '{fileName}' : code {j}, due to invalid config file selected.";
        //                    }
        //                }

        //                Log.Error(surveyResponse.Error);
        //                processedFiles++;
        //                progress = processedFiles * 100 / totalFiles;

        //                surveyResponse.Progress = progress;
        //                surveyResponse.CurrentStatus = fisFileRequest[0];

        //                processedFilesBuffer.Add(surveyResponse);
        //                AddBasicLogs(fileName, surveyResponse, newLcmsObjects);
        //                await responseStream.WriteAsync(surveyResponse);
        //                detailLogViewHelpers.Clear();
        //                processedFilesBuffer.Clear();

        //                Log.Information($"End currentResponse:{Path.GetFileName(fisFileRequest[0])} Progress:{surveyResponse.Progress}");
        //                int position = await ReleasePosition(AnalyserToBeUsed);
        //                Log.Information($"Released Position : {position} ");
        //            }
        //            else
        //                surveyResponse.Success = "Survey Processing Completed";

        //            CheckAndSetResponsePaths(fisFileRequest[0], surveyResponse, newLcmsObjects);
                    
        //            fisFileRequest = fisFileRequest.Skip(1).ToArray();
        //            #endregion

        //            if (!foundInvalidConfig && fisFileRequest.Length > 0)
        //            {

        //                await Parallel.ForEachAsync(fisFileRequest, options, async (requestFile, ct) =>
        //                {
        //                    if (ct.IsCancellationRequested)
        //                    {
        //                        return;
        //                    }

        //                    var surveyResponse = new SurveyWSProcessingResponse();

        //                    int AnalyserToBeUsed = await GetFirstAvailablePositionAsync();

        //                    if (roadInspectService[AnalyserToBeUsed] == null)
        //                    {
        //                        var logger = _serviceProvider.GetRequiredService<ILogger<RoadInspectService>>();
        //                        roadInspectService[AnalyserToBeUsed] = new RoadInspectService(AnalyserToBeUsed, logger);
        //                    }
        //                    Log.Information($"AnalyserToBeUsed : {AnalyserToBeUsed} ");

        //                    int j = roadInspectService[AnalyserToBeUsed].ProcessIndividualFisFile(requestFile, cfgFolder, cfgFileName, LcmsObjects, overlayImageModules);

        //                    if (j != 0)
        //                    {
        //                        var fileName = Path.GetFileName(requestFile);
        //                        surveyResponse.ErrorCode = j;
        //                        if (j == -1)//error received in saving xmls
        //                        {
        //                            surveyResponse.Error = $"Error while processing fis file '{fileName}' : FailedToSaveXml";
        //                        }
        //                        else if (j == -2)//error received in saving range images
        //                        {
        //                            surveyResponse.Error = $"Error while processing fis file '{fileName}' : FailedToSaveRangeImage";
        //                        }
        //                        else if (j == -3)//error received in saving overlay images
        //                        {
        //                            surveyResponse.Error = $"Error while processing fis file '{fileName}' : FailedToSaveOverlayImage";
        //                        }
        //                        else
        //                        {
        //                            string errorName = Enum.GetName(typeof(LcmsAnalyserLib.ErrorCodes), j);

        //                            if (errorName != null)
        //                            {
        //                                surveyResponse.Error = $"Error while processing fis file '{fileName}' : {errorName}";
        //                            }
        //                            else
        //                            {
        //                                surveyResponse.Error = $"Unknwon Error while processing fis file '{fileName}' : code {j}.";
        //                            }
        //                        }

        //                        Log.Error(surveyResponse.Error);
        //                        AddBasicLogs(fileName,surveyResponse,LcmsObjects);
        //                        await responseStream.WriteAsync(surveyResponse);
        //                        detailLogViewHelpers.Clear();

        //                        if (ct.IsCancellationRequested)
        //                        {
        //                            await ReleaseAllPosition();
        //                            return;
        //                        }
        //                    }
        //                    else
        //                        surveyResponse.Success = "Survey Processing Completed";
        //                    CheckAndSetResponsePaths(requestFile, surveyResponse, LcmsObjects);

        //                    processedFiles++;
        //                    progress = processedFiles * 100 / totalFiles;

        //                    surveyResponse.Progress = progress;
        //                    surveyResponse.CurrentStatus = requestFile;

        //                    processedFilesBuffer.Add(surveyResponse);
        //                    if (processedFilesBuffer.Count >= batchSize)
        //                    {
        //                        try
        //                        {
        //                            await responseStream.WriteAsync(surveyResponse);
        //                        }
        //                        catch (Exception ex)
        //                        {
        //                            Log.Warning("Status Message not set because other unit finish at the same time: " + ex.Message);
        //                        }
        //                        processedFilesBuffer.Clear();
        //                    }

        //                    Log.Information($"End currentResponse:{Path.GetFileName(requestFile)} Progress:{surveyResponse.Progress}");

        //                    int position = await ReleasePosition(AnalyserToBeUsed);
        //                    Log.Information($"Released Position : {position} ");
        //                });
        //            }
        //        }
        //        catch (OperationCanceledException e)
        //        {
        //            await ReleaseAllPosition();
        //            Log.Information(e.Message);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error($"Error Received in Processing FISFiles : {ex.Message}");
        //        Log.Error($"Error Trace : {ex.StackTrace}");
        //    }
        //    finally 
        //    {
        //        CleanupServices();
        //    }
        //}

        //private void AddBasicLogs(string fisFilePath, SurveyWSProcessingResponse surveyResponse, List<string> LcmsObjects)
        //{
        //    try
        //    {
        //        detailLogViewHelpers.Add(Newtonsoft.Json.JsonConvert.SerializeObject(new DataView2.Core.Helper.DetailLogViewHelper
        //        {
        //            FileName = fisFilePath,
        //            FileType = ".fis",
        //            ErdPath = surveyResponse.ErdPath,
        //            PpfPath = surveyResponse.PpfPath,
        //            SelectedLcmsObjects = string.Join(",",LcmsObjects),
        //            LogDetails = !string.IsNullOrEmpty(surveyResponse.Success) && string.IsNullOrEmpty(surveyResponse.Error) ? surveyResponse.Success : string.IsNullOrEmpty(surveyResponse.Success) && !string.IsNullOrEmpty(surveyResponse.Error) ? surveyResponse.Error : string.Empty,
        //            Status = !string.IsNullOrEmpty(surveyResponse.Success) && string.IsNullOrEmpty(surveyResponse.Error) ? "PASS" : string.IsNullOrEmpty(surveyResponse.Success) && !string.IsNullOrEmpty(surveyResponse.Error) ? "FAIL" : string.Empty
        //        }));
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error($"Error in saving basic detail log : {ex.Message}");
        //    }
        //}

        //private void CheckAndSetResponsePaths(string fisFilePath, SurveyWSProcessingResponse surveyResponse, List<string> LcmsObjects)
        //{
        //    try
        //    {
        //        string folderPath = Path.GetDirectoryName(fisFilePath),
        //         FileName = Path.GetFileNameWithoutExtension(fisFilePath),
        //         xmlFolder = Path.Combine(folderPath, "XmlResult"),
        //         xmlPath = Path.Combine(xmlFolder, FileName + ".xml"),
        //         ImageFolder = Path.Combine(folderPath, "ImageResult"),
        //         rangeImgPath = Path.Combine(ImageFolder, FileName + "_Range.jpg"),
        //         overlayImgPath = Path.Combine(ImageFolder, FileName + "_Overlay.jpg");

        //        if (File.Exists(xmlPath))
        //        {
        //            surveyResponse.XmlPath = xmlPath;
        //        }
        //        if (File.Exists(rangeImgPath) && File.Exists(overlayImgPath))
        //        {
        //            surveyResponse.ImagePath = ImageFolder;
        //        }

        //        if (detailLogViewHelpers != null && detailLogViewHelpers.Count > 0 && detailLogViewHelpers.Any(l => l.Contains(fisFilePath)))
        //        {
        //            detailLogViewHelpers.RemoveAll(l => l.Contains(fisFilePath));
        //        }

        //        detailLogViewHelpers.Add(Newtonsoft.Json.JsonConvert.SerializeObject(new DataView2.Core.Helper.DetailLogViewHelper
        //        {
        //            FileName = Path.GetFileName(fisFilePath),
        //            FileType = ".fis",
        //            ImagePath = ImageFolder,
        //            XMLPath = xmlPath,
        //            ErdPath = surveyResponse.ErdPath,
        //            PpfPath = surveyResponse.PpfPath,
        //            SelectedLcmsObjects = string.Join(",", LcmsObjects),
        //            LogDetails = !string.IsNullOrEmpty(surveyResponse.Success) && string.IsNullOrEmpty(surveyResponse.Error) ? surveyResponse.Success : string.IsNullOrEmpty(surveyResponse.Success) && !string.IsNullOrEmpty(surveyResponse.Error) ? surveyResponse.Error : string.Empty,
        //            Status = !string.IsNullOrEmpty(surveyResponse.Success) && string.IsNullOrEmpty(surveyResponse.Error) ? "PASS" : string.IsNullOrEmpty(surveyResponse.Success) && !string.IsNullOrEmpty(surveyResponse.Error) ? "FAIL" : string.Empty
        //        }));
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error($"Error in saving detail log : {ex.Message}");
        //    }
        //}

        public async Task<int> GetFirstAvailablePositionAsync()
        {
            while (true)
            {
                await semaphore.WaitAsync();
                try
                {
                    for (int i = 0; i < progressArray.Length; i++)
                    {
                        if (progressArray[i] == 0)
                        {
                            progressArray[i] = 1;
                            return i;
                        }
                    }
                }
                finally
                {
                    semaphore.Release();
                }

                await Task.Delay(100);
            }
        }

        public async Task<int> ReleasePosition(int position)
        {
            if (position >= 0 && position < progressArray.Length)
            {
                progressArray[position] = 0;
                await Task.Delay(100);
            }
            return position;
        }

        public async Task ReleaseAllPosition()
        {
            for(int i = 0; i < progressArray.Length; i++)
            {
                progressArray[i] = 0;
                await Task.Delay(100);
            }
        }

        public void CleanupServices()
        {
            for (int i = 0; i < roadInspectService.Length; i++)
            {
                if (roadInspectService[i] != null) {
                    roadInspectService[i].Dispose();

                }
            }
            // Suggest to the garbage collector to reclaim memory
            GC.Collect();
            GC.WaitForPendingFinalizers(); // Optionally wait for finalizers to complete before returning
        }


        public void Dispose()
        {
            for (int i = 0; i < roadInspectService.Length; i++)
            {
                if (roadInspectService[i] != null)
                {
                    roadInspectService[i].Dispose();
                }
            }
        }

    }
}
