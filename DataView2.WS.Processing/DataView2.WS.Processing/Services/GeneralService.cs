using DataView2.Core;
using DataView2.Core.Helper;
using DataView2.Core.Models;
using Grpc.Core;
using Serilog;
using DataView2.WS.Processing.Protos;
using System.Data;
using System.Runtime.InteropServices;
using static DataView2.Core.Helper.TableNameHelper;
using static DataView2.WS.Processing.Services.SurveySections;
using DataView2.Core.Models.LCMS_Data_Tables;
using System.Diagnostics;
using System.Reflection;

namespace DataView2.WS.Processing.Services
{
    public class GeneralService : GeneralWorkerService.GeneralWorkerServiceBase
    {
        private readonly ILogger<GeneralService> _logger;

        public event Action? OnProcessingCancelled;

        private readonly MultiProcessingLCMS _multiProcessingLCMS;


        public GeneralService(ILogger<GeneralService> logger,
            MultiProcessingLCMS MultiProcessingLCMS)
        {
            _logger = logger;
            _multiProcessingLCMS = MultiProcessingLCMS;
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

        public override Task<TestResponse> TestConnection(EmptyWS request, ServerCallContext context)
        {
            return Task.FromResult(new TestResponse { IsConnected = true });
        }
        public List<string> _detailLogViewHelpers = new List<string>();
        public override async Task ProcessSurvey(
                    SurveyWSProcessingRequest requestWS,
                    IServerStreamWriter<SurveyWSProcessingResponse> responseStream,
                    ServerCallContext context)
        {
            var safeWriter = new SafeSurveyWriter<SurveyWSProcessingResponse>(responseStream);
            try
            {
                LoadLicense(requestWS.LicensePath);

                // 1) Prepare selected fis files
                var fisFileRequest = new FisFileRequest();

                if (!requestWS.SelectedFiles.Any())
                {
                    return;
                }

                var allSelectedFiles = requestWS.SelectedFiles.ToList();
                // Collect FIS files
                var fisFiles = allSelectedFiles.Where(file => file.EndsWith(".fis")).ToList();
                fisFileRequest.FisFilesPath = fisFiles;

                var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken);

                // 2) Process fis files
                if (fisFileRequest.FisFilesPath.Count != 0)
                {
                    _multiProcessingLCMS.detailLogViewHelpers = new List<string>();
                    _detailLogViewHelpers = new List<string>();
                    await HandleFisFilesAsync(
                        requestWS,
                        fisFileRequest,
                        safeWriter,
                        cancellationTokenSource,
                        context
                    );
                    _detailLogViewHelpers.AddRange(_multiProcessingLCMS.detailLogViewHelpers);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error in ProcessSurvey : {ex.Message}");

                await safeWriter.WriteAsync(new SurveyWSProcessingResponse
                {
                    Error = ex.Message
                });
            }
        }
        public override Task<ProcessingFisResponse> ProcessingLCMSVariables(EmptyWS request, ServerCallContext context)
        {
            return Task.FromResult(new ProcessingFisResponse
            {
                DetailLogViewHelpers = { _detailLogViewHelpers },
                FoundInvalidLicense = _multiProcessingLCMS.foundInvalidLicense,
                FoundInvalidConfig = _multiProcessingLCMS.foundInvalidConfig
            });
        }

        /// <summary>
        /// Handles the logic for FIS files, including license checks, parallel tasks, etc.
        /// Returns true if we determined that this is PCI processing (isPCI = true),
        /// or false otherwise.
        /// </summary>
        private async Task HandleFisFilesAsync(
            SurveyWSProcessingRequest request,
            FisFileRequest fisFileRequest,
            SafeSurveyWriter<SurveyWSProcessingResponse> safeWriter,
            CancellationTokenSource cancellationTokenSource,
            ServerCallContext context)
        {
            DateTime startTimeFis = DateTime.Now;

            try
            {
                var fisFiles = fisFileRequest.FisFilesPath.ToArray();

                // Check license before processing
                var licenseCheckResponse = await _multiProcessingLCMS.CheckLicenseBeforeProcessing(fisFiles, request.ProcessingObjects.ToList());
                if (!string.IsNullOrEmpty(licenseCheckResponse.Error))
                {
                    await safeWriter.WriteAsync(licenseCheckResponse);
                    return;
                }

                var LcmsObjects = request.ProcessingObjects.ToList();

                int chunkSize = request.BatchSize;
                var fileCount = fisFiles.Count();

                int totalChunks = (int)Math.Ceiling((double)fileCount / chunkSize);

                if (LcmsObjects.Contains(LayerNames.PpfErd))
                {
                    var allTasks = new List<Task>();

                    if (totalChunks > 1)
                    {
                        // 1) Build the iri task (uses 1 processing unit)
                        var roughnessModule = new List<string> { LayerNames.Roughness, LayerNames.PpfErd };

                        if (LcmsObjects.Contains("Sags Bumps"))
                            roughnessModule.Add("Sags Bumps");

                        var iriTask = _multiProcessingLCMS.ProcessIRIForPpfAndErd(
                               fisFiles,
                               request.CfgFolder,
                               request.CfgFileName,
                               roughnessModule,
                               safeWriter,
                               cancellationTokenSource
                            );
                        allTasks.Add(iriTask);

                        LcmsObjects = LcmsObjects
                         .Where(x => x != LayerNames.PpfErd)
                         .ToList(); //should not pass ppf and erd otherwise it will create duplicate
                    }


                    // 2) Build FIS processing tasks (the rest processing units parallel)
                    var fisTasks = Enumerable.Range(0, totalChunks).Select(i =>
                    {
                        var chunkFisFileList = fisFiles
                            .Skip(i * chunkSize)
                            .Take(chunkSize)
                            .ToArray();

                        return _multiProcessingLCMS.ProcessIRIAndFisFiles(chunkFisFileList, request.CfgFolder, request.CfgFileName, LcmsObjects, safeWriter, cancellationTokenSource);
                    });

                    // 3) Run all tasks in parallel
                   
                    allTasks.AddRange(fisTasks);
                    await Task.WhenAll(allTasks);

                    // 4) Copy PPF/ERD after roughness task is done
                    CopyPpfErdFiles(Path.Combine(request.FolderPath, "PpfResult"), "*.ppf");
                    CopyPpfErdFiles(Path.Combine(request.FolderPath, "ErdResult"), "*.erd");
                }
                else
                {
                    //batch & parallel processing if ppf erd not ticked

                    if (LcmsObjects.Contains(LayerNames.Roughness))
                    {
                        // Build tasks for each chunk
                        var tasks = Enumerable.Range(0, totalChunks).Select(i =>
                        {
                            var chunkFisFileList = fisFiles
                                .Skip(i * chunkSize)
                                .Take(chunkSize)
                                .ToArray();

                            return _multiProcessingLCMS.ProcessIRIAndFisFiles(chunkFisFileList, request.CfgFolder, request.CfgFileName, LcmsObjects, safeWriter, cancellationTokenSource);
                        });

                        // Run them all in parallel
                        await Task.WhenAll(tasks);
                    }
                    else
                    {
                        // Build tasks for each chunk
                        var tasks = Enumerable.Range(0, totalChunks).Select(i =>
                        {
                            var chunkFisFileList = fisFiles
                                .Skip(i * chunkSize)
                                .Take(chunkSize)
                                .ToArray();

                            return _multiProcessingLCMS.ProcessFisFilesGeneral(chunkFisFileList, request.CfgFolder, request.CfgFileName, LcmsObjects, safeWriter, cancellationTokenSource);
                        });

                        // Run them all in parallel
                        await Task.WhenAll(tasks);
                    }
                }


                //// PCI with Roughness
                //if (LcmsObjects.Contains(LayerNames.PCIwithRoughness))
                //{
                //    Log.Information("Processing with Single dll for PCI");
                //    var combinedModules = pciModules.Union(LcmsObjects).ToList();

                //    //Pass overlay modules and image outputs only for single processing (image generated)
                //    var xmlPaths = await _multiProcessingLCMS.ProcessIRIandFisFilesAsync(
                //        fisFiles,
                //        request.CfgFolder,
                //        request.CfgFileName,
                //        combinedModules,
                //        responseStream,
                //        cancellationTokenSource,
                //        processType,
                //        overlayModules
                //    );
                //}
                //else
                //{

                //    Log.Information("HandleFisFilesAsync => Roughness is in the list, run parallel tasks");
                //    // If Roughness is in the list, run parallel tasks
                //    if (LcmsObjects.Contains(LayerNames.Roughness))
                //    {
                //        Log.Information("LcmsObjects.Contains(LayerNames.Roughness)  R1A1");
                //        tasks.Add(Task.Run(async () =>
                //        {
                //            var roughnessModule = new List<string> { "Roughness" };

                //            if (LcmsObjects.Contains("Sags Bumps"))
                //                roughnessModule.Add("Sags Bumps");

                //            if (LcmsObjects.Contains("PPF ERD"))
                //                roughnessModule.Add("PPF ERD");

                //            // Process roughness first (no image generated)
                //            var xmlPaths = await _multiProcessingLCMS.ProcessIRIandFisFilesAsync(
                //               fisFiles,
                //               request.CfgFolder,
                //               request.CfgFileName,
                //               roughnessModule,
                //               responseStream,
                //               cancellationTokenSource,
                //               processType,
                //               overlayModules
                //            );

                //            // Copy all generated PPF and ERD files -- Comented by Ivan, looking for the source folder, I found it delete all the parent folder with same extensions, so other surveys are affected
                //            // Log.Information("Copying PPF/ERD files at required location");
                //            CopyPpfErdFiles(Path.Combine(request.FolderPath, "PpfResult"), "*.ppf");
                //            CopyPpfErdFiles(Path.Combine(request.FolderPath, "ErdResult"), "*.erd");

                //            // If LcmsObjects contains "PPF ERD", update detail logs
                //            if (LcmsObjects.Contains("PPF ERD"))
                //            {
                //                if (_multiProcessingLCMS.detailLogViewHelpers != null && _multiProcessingLCMS.detailLogViewHelpers.Count > 0)
                //                {
                //                    var ppfPath = Path.Combine(request.FolderPath, "PpfResult");
                //                    var erdPath = Path.Combine(request.FolderPath, "ErdResult");

                //                    List<string> updatedLogDetails = new List<string>();
                //                    foreach (string iriLog in _multiProcessingLCMS.detailLogViewHelpers)
                //                    {
                //                        DetailLogViewHelper logObj = Newtonsoft.Json.JsonConvert.DeserializeObject<DetailLogViewHelper>(iriLog);
                //                        if (logObj != null)
                //                        {
                //                            logObj.PpfPath = ppfPath;
                //                            logObj.ErdPath = erdPath;
                //                        }
                //                        updatedLogDetails.Add(Newtonsoft.Json.JsonConvert.SerializeObject(logObj));
                //                    }

                //                    _multiProcessingLCMS.detailLogViewHelpers.Clear();
                //                    _multiProcessingLCMS.detailLogViewHelpers.AddRange(updatedLogDetails);
                //                }
                //            }
                //        }));
                //    }


                //    if (filteredLcmsObjects.Count > 0)
                //    {
                //        Log.Information("HandleFisFilesAsync => filteredLcmsObjects 12B LcmsObjects=" + LcmsObjects.ToString());

                //        tasks.Add(Task.Run(async () =>
                //        {
                //            // Without roughness -- multiple dlls
                //            if (LcmsObjects.Contains(LayerNames.PCIwithoutRoughness))
                //            {
                //                Log.Information("HandleFisFilesAsync => Processing PCI without roughness");
                //                var pciModulesWithoutRoughness = pciModules.Where(module => module != LayerNames.Roughness).ToList();
                //                var combinedModules = pciModulesWithoutRoughness.Union(LcmsObjects).ToList();

                //                await _multiProcessingLCMS.ProcessFISFilesAsync(
                //                    fisFiles,
                //                    request.CfgFolder,
                //                    request.CfgFileName,
                //                    combinedModules,
                //                    overlayModules,
                //                    responseStream,
                //                    cancellationTokenSource
                //                );
                //            }
                //            else
                //            {
                //                Log.Information("HandleFisFilesAsync => Process other modules");
                //                // Process other modules
                //                await _multiProcessingLCMS.ProcessFISFilesAsync(
                //                    fisFiles,
                //                    request.CfgFolder,
                //                    request.CfgFileName,
                //                    LcmsObjects,
                //                    overlayModules,
                //                    responseStream,
                //                    cancellationTokenSource
                //                );
                //            }
                //        }));
                //    }


                //    Log.Information($"HandleFisFilesAsync => Starting parallel tasks: {tasks.Count} tasks");
                //    // Wait for parallel tasks
                //    await Task.WhenAll(tasks);
                //    Log.Information($"HandleFisFilesAsync => Ended parallel tasks");

                //}

                Log.Information($"HandleFisFilesAsync => Total Time(seconds) for Fis Files Analysis: {Convert.ToInt64((DateTime.Now - startTimeFis).TotalSeconds)}");
            }
            catch (Exception ex)
            {
                Log.Information($"HandleFisFilesAsync => ERROR");
                Log.Information(ex.Message);
                if (ex.Message.IndexOf("Failed", StringComparison.OrdinalIgnoreCase) >= 0 && ex.Message.IndexOf("Library", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    await safeWriter.WriteAsync(new SurveyWSProcessingResponse
                    {
                        Error = ex.Message
                    });
                    return;
                }
            }
            finally
            {
                _multiProcessingLCMS.CleanupServices();
            }

            if (_multiProcessingLCMS.foundInvalidLicense)
            {
                Log.Information($"HandleFisFilesAsync => Invalid license");
                Log.Error("Invalid license error occurred.");
                return;
            }
        }



        private bool CopyPpfErdFiles(string filePath, string fileExtension)
        {
            try
            {
                if (!Directory.Exists(filePath))
                {
                    Directory.CreateDirectory(filePath);
                }

                Log.Information($"Copying {fileExtension} file to the folder...");
                DirectoryInfo directoryInfo = new DirectoryInfo(filePath);
                if (directoryInfo != null)
                {
                    List<string> files = Directory.GetFiles(directoryInfo?.Parent?.Parent?.FullName, fileExtension).ToList();

                    if (files != null && files.Count > 0)
                    {
                        foreach (string file in files)
                        {
                            string originalName = Path.GetFileNameWithoutExtension(file);
                            string extension = Path.GetExtension(file);
                            // Nombre candidato inicial (sin sufijo)
                            string destFileName = originalName + extension;
                            string destFilePath = Path.Combine(filePath, destFileName);
                            // Si ya existe, añadir sufijo incremental: _1, _2, ...
                            int counter = 1;
                            while (File.Exists(destFilePath))
                            {
                                destFileName = $"{originalName}_{counter}{extension}";
                                destFilePath = Path.Combine(filePath, destFileName);
                                counter++;
                            }
                            File.Copy(file, destFilePath, true);
                            File.Delete(file);
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"Error in copying PPF/ERD files : {ex.Message}");
                return false;
            }
        }

        public static double[] ConvertToGPSCoordinates(double x, double y, DataView2.Core.Communication.GPSCoordinate coordinates)
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
            double relativeAngle = (180 / Math.PI) * Math.Atan2(delta_X, delta_Y);
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

        public static double CalculateBearing(XMLParser.GPSCoordinate from, XMLParser.GPSCoordinate to)
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

            // Convertir de radianes a grados
            bearing = RadianToDegree(bearing);

            // Normalizar el ángulo a 0° - 360°
            bearing = (bearing + 360) % 360;

            return bearing;
        }

        private static double RadianToDegree(double radian)
        {
            return radian * 180.0 / Math.PI;
        }

        private static double DegreeToRadian(double degree)
        {
            return degree * Math.PI / 180.0;
        }

        private void LoadLicense(string licensePath)
        {
            try
            {
                if (File.Exists(licensePath))
                {
                    string destinationPath = Path.Combine(AppContext.BaseDirectory, Path.GetFileName(licensePath));
                    File.Copy(licensePath, destinationPath, true);
                }
                else
                {
                    Log.Error("The file does not exist at that path.");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error loading license: {ex.Message}");
            }
        }


    }
}
