using Azure.Core;
using DataView2.Core;
using DataView2.Core.Helper;
using DataView2.Core.Models;
using DataView2.Core.Models.LCMS_Data_Tables;
using DataView2.GrpcService.Data;
using DataView2.GrpcService.Helpers;
using DataView2.GrpcService.Interfaces;
using DataView2.GrpcService.Protos;
using Esri.ArcGISRuntime.Geometry;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using LadybugAPI;
using ladybugProcessStream_CSharp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Maui;
using ProtoBuf.Grpc;
using Serilog;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace DataView2.GrpcService.Services
{
    public class Camera360FrameService: ICamera360FrameService
    {
        private readonly IRepository<Camera360Frame> _repository;
        private readonly AppDbContextProjectData _context;
        public List<string> detailLogViewHelpers = new List<string>();

        //cast pgr param string
        private static readonly Dictionary<string, (uint width, uint height)> OutputSizeMap = new Dictionary<string, (uint, uint)>()
        {
            { "512 x 256", (512, 256) },
            { "1024 x 512", (1024, 512) },
            { "2048 x 1024", (2048, 1024) },
            { "3500 x 1750", (3500, 1750) },
            { "4096 x 2048", (4096, 2048) },
            { "5400 x 2700", (5400, 2700) }
        };

        private static readonly Dictionary<string, LadybugColorProcessingMethod> ColorProcessingMap = new Dictionary<string, LadybugColorProcessingMethod>()
        {
            { "Disable", LadybugColorProcessingMethod.LADYBUG_DISABLE },
            { "Edge Sensing", LadybugColorProcessingMethod.LADYBUG_EDGE_SENSING },
            { "Nearest Neighbour (Fast)", LadybugColorProcessingMethod.LADYBUG_NEAREST_NEIGHBOR_FAST },
            { "Rigorous (Very Slow)", LadybugColorProcessingMethod.LADYBUG_RIGOROUS },
            { "Downsample4 (Fast)", LadybugColorProcessingMethod.LADYBUG_DOWNSAMPLE4 },
            { "Downsample16 (Faster)", LadybugColorProcessingMethod.LADYBUG_DOWNSAMPLE16 },
            { "Downsample64 (Fastest)", LadybugColorProcessingMethod.LADYBUG_DOWNSAMPLE64 },
            { "Downsample4 Mono (Fast)", LadybugColorProcessingMethod.LADYBUG_MONO },
            { "High Quality Linear", LadybugColorProcessingMethod.LADYBUG_HQLINEAR },
            { "High Quality Linear on GPU", LadybugColorProcessingMethod.LADYBUG_HQLINEAR_GPU },
            { "Directional Filter", LadybugColorProcessingMethod.LADYBUG_DIRECTIONAL_FILTER }
        };

        private uint m_numImages = 0;
        //default params
        private LadybugColorProcessingMethod colorProcessingMethod = LadybugColorProcessingMethod.LADYBUG_HQLINEAR;
        private LadybugOutputImage outputRenderingType = LadybugOutputImage.LADYBUG_PANORAMIC;
        private LadybugSaveFileFormat saveFileFormat = LadybugSaveFileFormat.LADYBUG_FILEFORMAT_JPG;
        private uint imageSizeWidth = 2048, imageSizeHeight = 1024;
        public Camera360FrameService(IRepository<Camera360Frame> repository, AppDbContextProjectData context)
        {
            _repository = repository;
            _context = context;
        }

        public async Task ProcessPGRFiles(List<string> pgrFilePaths, int surveyId, string pgrOutputSize, string pgrColorProcessing, bool pgrAdd6Images, Action<int, string> reportProgress)
        {
            //Get GPS_Processed records with same survey Id
            var gpsProcessed = _context.GPS_Processed.Where(x => x.SurveyId == surveyId).ToList();

            if (gpsProcessed == null || gpsProcessed.Count == 0)
            {
                //no coordinate information found.
                Log.Error($"GPS information not found with the survey Id : {surveyId}");
                var detailLogViewHelper = new DetailLogViewHelper
                {
                    Status = "FAIL",
                    LogDetails = "360 Camera processing failed because GPS information was not found. Ensure that 'GPS_Raw.txt' and 'OdoFile.txt' exist in the folder before proceeding.",
                    FileType = ".pgr",
                    FileName = pgrFilePaths.FirstOrDefault()
                };
                detailLogViewHelpers.Add(Newtonsoft.Json.JsonConvert.SerializeObject(detailLogViewHelper));
                return;
            }

            //map output size string to width and height
            if (pgrOutputSize != null && OutputSizeMap.TryGetValue(pgrOutputSize, out var size))
            {
                imageSizeWidth = size.width;
                imageSizeHeight = size.height;
            }

            //map color processing string to enum
            if (pgrColorProcessing != null && ColorProcessingMap.TryGetValue(pgrColorProcessing, out var method))
            {
                colorProcessingMethod = method;
            }

            Log.Information($"Start processing pgr with color processing method of {colorProcessingMethod.ToString()} and output size : {imageSizeWidth} x {imageSizeHeight}");

            string surveyName = string.Empty;
            var survey = _context.Survey.FirstOrDefault(x => x.Id == surveyId);
            if (survey != null)
            {
                surveyName = survey.SurveyName;
            }

            var pgrDirectory = Path.GetDirectoryName(pgrFilePaths.FirstOrDefault());
            var pgrFiles = Directory.GetFiles(pgrDirectory, "*.pgr");

            LadybugTimestamp? lastTimestamp = null;
            double previousTimeStamp = 0;
            int cameraId = 0;
            foreach (var path in pgrFiles)
            {
                Log.Information($"1. Started processing a pgr file {path}");
                Core.Helper.DetailLogViewHelper detailLogViewHelper = new Core.Helper.DetailLogViewHelper();
                detailLogViewHelper.FileName = path;
                detailLogViewHelper.FileType = ".pgr";

                reportProgress(0, $"Started processing video file {Path.GetFileName(path)}");
                try
                {
                    var outputPath = Path.GetDirectoryName(path);
                    var imageOutputPath = Path.Combine(outputPath, "PgrImageResult");

                    if (!Directory.Exists(imageOutputPath))
                    {
                        Directory.CreateDirectory(imageOutputPath);
                    }
                    var fileName = Path.GetFileNameWithoutExtension(path);
                    var OUT_FILE_PREFIX = fileName + "_ImageOutput_";
                    using (ContextHolder ldContexts = new ContextHolder())
                    {
                        LadybugError error;
                        Log.Information($"2. Initializing reading images in ladybug dll");

                        // Open stream file
                        error = Ladybug.InitializeStreamForReading(ldContexts.GetStreamContext(), path, true);
                        HandleError(error);

                        error = Ladybug.GetStreamNumOfImages(ldContexts.GetStreamContext(), out m_numImages);
                        HandleError(error);

                        LadybugImage image = new LadybugImage();
                        LadybugStreamHeadInfo streamHeaderInfo = new LadybugStreamHeadInfo();
                        uint textureWidth, textureHeight;
                        LadybugStabilizationParams stabilizationParams = new LadybugStabilizationParams();
                        LadybugNMEAGPGGA gpsData = new LadybugNMEAGPGGA();
                        bool saveGpsToFile = true;
                        int percentComplete = 0;

                        string configFilename = Path.GetTempFileName();
                        error = Ladybug.GetStreamConfigFile(ldContexts.GetStreamContext(), configFilename);
                        HandleError(error);

                        error = Ladybug.GetStreamHeader(ldContexts.GetStreamContext(), out streamHeaderInfo, null);
                        HandleError(error);

                        error = Ladybug.SetColorTileFormat(ldContexts.GetContext(), streamHeaderInfo.stippledFormat);
                        HandleError(error);

                        error = Ladybug.LoadConfig(ldContexts.GetContext(), configFilename);
                        HandleError(error);

                        System.IO.File.Delete(configFilename);

                        error = Ladybug.SetColorProcessingMethod(ldContexts.GetContext(), colorProcessingMethod);
                        HandleError(error);

                        // read one frame from stream
                        error = Ladybug.ReadImageFromStream(ldContexts.GetStreamContext(), out image);
                        HandleError(error);

                        if (colorProcessingMethod == LadybugColorProcessingMethod.LADYBUG_DOWNSAMPLE16)
                        {
                            textureWidth = image.uiCols / 4;
                            textureHeight = image.uiRows / 4;
                        }
                        else if (colorProcessingMethod == LadybugColorProcessingMethod.LADYBUG_DOWNSAMPLE4 || colorProcessingMethod == LadybugColorProcessingMethod.LADYBUG_MONO)
                        {
                            textureWidth = image.uiCols / 2;
                            textureHeight = image.uiRows / 2;
                        }
                        else
                        {
                            textureWidth = image.uiCols;
                            textureHeight = image.uiRows;
                        }

                        // Initialize alpha mask size
                        error = Ladybug.InitializeAlphaMasks(ldContexts.GetContext(), textureWidth, textureHeight, false);
                        HandleError(error);

                        // Make the rendering engine use the alpha mask
                        error = Ladybug.SetAlphaMasking(ldContexts.GetContext(), true);
                        HandleError(error);

                        error = Ladybug.ConfigureOutputImages(ldContexts.GetContext(), (uint)outputRenderingType);
                        HandleError(error);

                        error = Ladybug.SetOffScreenImageSize(ldContexts.GetContext(), outputRenderingType, imageSizeWidth, imageSizeHeight);
                        HandleError(error);

                        bool isHighBitDepth = dataFormat.isHighBitDepth(image.dataFormat);

                        // Go to the first frame to process
                        error = Ladybug.GoToImage(ldContexts.GetStreamContext(), 0);
                        HandleError(error);


                        int bytesPerPixel = 4 * (isHighBitDepth ? 2 : 1);
                        int bufferSize = (int)(Ladybug.LADYBUG_NUM_CAMERAS * textureWidth * textureHeight * bytesPerPixel);

                        byte[] textureBuffer = new byte[bufferSize];
                        IntPtr[] texturePtrs = new IntPtr[Ladybug.LADYBUG_NUM_CAMERAS];

                        //Pin only once outside the loop
                        GCHandle handle = GCHandle.Alloc(textureBuffer, GCHandleType.Pinned);

                        try
                        {
                            IntPtr basePtr = handle.AddrOfPinnedObject();
                            for (int i = 0; i < Ladybug.LADYBUG_NUM_CAMERAS; i++)
                            {
                                int offset = (int)(i * textureWidth * textureHeight) * bytesPerPixel;
                                texturePtrs[i] = basePtr + offset;
                            }
                        }
                        finally
                        {
                            //Free the buffer after usuage to avoid memory leaks
                            if (handle.IsAllocated)
                                handle.Free();
                        }

                        if (lastTimestamp == null)
                        {
                            lastTimestamp = image.timeStamp;
                        }

                        var cameraFrames = new List<Camera360Frame>();

                        Log.Information($"3. Starting converting {m_numImages} images...");

                        // Process frames
                        for (int iFrame = 0; iFrame < m_numImages; iFrame++)
                        {
                            error = Ladybug.ReadImageFromStream(ldContexts.GetStreamContext(), out image);
                            HandleError(error);

                            double timeStamp = TimeDifferenceFromCameraTime(image.timeStamp, lastTimestamp.Value) + previousTimeStamp; //Time difference between the last two timestamps  + the time of the last timestamp = current time
                            previousTimeStamp = timeStamp;
                            lastTimestamp = image.timeStamp;

                            error = Ladybug.ConvertImage(
                                ldContexts.GetContext(),
                                ref image,
                                texturePtrs,
                                isHighBitDepth ? LadybugPixelFormat.LADYBUG_BGRU16 : LadybugPixelFormat.LADYBUG_BGRU);
                            HandleError(error);

                            error = Ladybug.UpdateTextures(
                                ldContexts.GetContext(),
                                Ladybug.LADYBUG_NUM_CAMERAS,
                                texturePtrs,
                                isHighBitDepth ? LadybugPixelFormat.LADYBUG_BGRU16 : LadybugPixelFormat.LADYBUG_BGRU);
                            HandleError(error);

                            //additionally generate 6 individual images using textureBuffer
                            if (pgrAdd6Images)
                            {
                                var individualFolder = Path.Combine(imageOutputPath, "IndividualCameras");
                                if (!Directory.Exists(individualFolder))
                                {
                                    Directory.CreateDirectory(individualFolder);
                                }

                                for (int camIndex = 0; camIndex < Ladybug.LADYBUG_NUM_CAMERAS; camIndex++)
                                {
                                    // Extract raw data for this camera
                                    int offset = (int)(camIndex * textureWidth * textureHeight) * bytesPerPixel;
                                    // Create a Bitmap from raw data
                                    Bitmap bmp = CreateBitmapFromRaw(
                                        textureBuffer, offset, (int)textureWidth, (int)textureHeight, bytesPerPixel);

                                    // Rotate the image 90 degrees clockwise
                                    bmp.RotateFlip(RotateFlipType.Rotate90FlipNone);

                                    // Save to file
                                    string camFile = Path.Combine(individualFolder, $"{OUT_FILE_PREFIX}_Cam{camIndex}_{iFrame:D8}.jpg");
                                    bmp.Save(camFile, System.Drawing.Imaging.ImageFormat.Jpeg);
                                    bmp.Dispose();
                                }
                            }

                            string outputFilename = Path.Combine(imageOutputPath, $"{OUT_FILE_PREFIX}_{iFrame:D8}.jpg");

                            LadybugProcessedImage processedImage;
                            error = Ladybug.RenderOffScreenImage(
                                ldContexts.GetContext(),
                                outputRenderingType,
                                isHighBitDepth ? LadybugPixelFormat.LADYBUG_BGR16 : LadybugPixelFormat.LADYBUG_BGR,
                                out processedImage);
                            HandleError(error);

                            error = Ladybug.SaveImage(ldContexts.GetContext(), ref processedImage, outputFilename, saveFileFormat, true);
                            HandleError(error);

                            if (iFrame % 20 == 0) // Only log every 20th frame
                            {
                                float processedFrames = (float)iFrame + 1;
                                percentComplete = (int)(processedFrames / m_numImages * 100);

                                Log.Information($"{percentComplete} % done in converting images.");
                                reportProgress(percentComplete, $"Converting PGR to JPG images for file {Path.GetFileName(path)}...");
                            }

                            //Convert time to coordinates using GPS_Processed
                            var timeStampInMillisecond = timeStamp * 1000;
                            var timeValues = gpsProcessed.Select(x => x.Time).ToList();
                            var longitudeValues = gpsProcessed.Select(x => x.Longitude).ToList();
                            var latitudeValues = gpsProcessed.Select(x => x.Latitude).ToList();
                            var trackAngleValues = gpsProcessed.Select(x => x.Heading).ToList();
                            var chainageValues = gpsProcessed.Select(x => x.Chainage).ToList();

                            var longitude = CoordinateHelper.LinearInterpolateSingle(timeStampInMillisecond, timeValues, longitudeValues);
                            var latitude = CoordinateHelper.LinearInterpolateSingle(timeStampInMillisecond, timeValues, latitudeValues);
                            var trackAngle = CoordinateHelper.LinearInterpolateSingle(timeStampInMillisecond, timeValues, trackAngleValues);
                            var chainage = CoordinateHelper.LinearInterpolateSingle(timeStampInMillisecond, timeValues, chainageValues);

                            var jsonDataObject = new
                            {
                                type = "Feature",
                                geometry = new
                                {
                                    type = "Point",
                                    coordinates = new[] { longitude, latitude }
                                },
                                properties = new
                                {
                                    type = "360 Camera"
                                }
                            };

                            string geoJsonData = JsonSerializer.Serialize(jsonDataObject);

                            var frame = new Camera360Frame
                            {
                                ImagePath = outputFilename,
                                TimeStamp = timeStampInMillisecond,
                                SurveyId = surveyId,
                                SurveyName = surveyName,
                                Chainage = Math.Round(chainage, 2),
                                GPSLongitude = longitude,
                                GPSLatitude = latitude,
                                GPSTrackAngle = trackAngle,
                                Camera360FrameId = cameraId,
                                GeoJSON = geoJsonData
                            };
                            cameraFrames.Add(frame);
                            cameraId++;
                        }

                        Log.Information($"4. Finished converting {m_numImages} images and now saving 360 camera information in db...");

                        //Save cameraFrames into DB
                        await CreateRangeAsync(cameraFrames);

                        //Release image
                        error = Ladybug.ReleaseOffScreenImage(ldContexts.GetContext(), outputRenderingType);
                        HandleError(error);
                    }

                    Log.Information($"5. a pgr file {path} successfully Processed");
                    detailLogViewHelper.Status = "PASS";
                    detailLogViewHelper.LogDetails = "360 Camera (pgr file) was processed successfully.";
                    detailLogViewHelpers.Add(Newtonsoft.Json.JsonConvert.SerializeObject(detailLogViewHelper));
                }
                catch (Exception ex)
                {
                    Log.Error($"Error in Processing a pgr file {path}" + ex.Message);
                    detailLogViewHelper.Status = "FAIL";
                    detailLogViewHelper.LogDetails = $"360 Camera (Pgr file) processing failed due to {ex.Message}";
                    detailLogViewHelpers.Add(Newtonsoft.Json.JsonConvert.SerializeObject(detailLogViewHelper));
                }
            }

            //recalculate chainage if lcms data available
            if (survey != null)
            {
                //Get first segment in case there is no segment Id 0
                var firstSegment = _context.LCMS_Segment
                        .Where(x => x.SurveyId == survey.SurveyIdExternal)
                        .OrderBy(x => x.SegmentId)
                        .FirstOrDefault();

                if (firstSegment != null)
                {
                    var videos = _context.Camera360Frame.Where(x => x.SurveyId == surveyId);
                    if (videos == null || videos.Count() == 0) return;

                    var firstVideo = videos.FirstOrDefault(x => x.Camera360FrameId == 0);
                    if (firstVideo == null || string.IsNullOrWhiteSpace(firstVideo.GeoJSON)) return;
                    var midpoint = ProcessingHelper.TryGetBottomMidpoint(firstSegment.GeoJSON);
                    if (midpoint == null) return;

                    // get the distance between firstSegment and firstVideo
                    MapPoint point1 = new MapPoint(midpoint[0], midpoint[1], SpatialReferences.Wgs84);
                    MapPoint point2 = new MapPoint(firstVideo.GPSLongitude, firstVideo.GPSLatitude, SpatialReferences.Wgs84);
                    var distance = GeometryEngine.DistanceGeodetic(point1, point2, LinearUnits.Meters, AngularUnits.Degrees, GeodeticCurveType.Geodesic);
                    double offset;
                    //check if video is ahead or segment is ahead
                    bool videoAhead = ProcessingHelper.IsVideoAfterSegment(firstSegment.GPSLatitude, firstSegment.GPSLongitude, firstVideo.GPSLatitude, firstVideo.GPSLongitude, firstSegment.GPSTrackAngle);
                    if (videoAhead)
                    {
                        offset = firstSegment.Chainage - firstVideo.Chainage + distance.Distance;
                    }
                    else
                    {
                        offset = firstSegment.Chainage - firstVideo.Chainage - distance.Distance;
                    }
                    //adjust the video chainage based on lcms chainage
                    foreach (var video in videos)
                    {
                        video.Chainage = Math.Round(video.Chainage + offset, 2);
                    }
                    await _context.SaveChangesAsync();
                    Log.Information("Successfully adjusted video chainage based on the first LCMS segment chainage.");
                }
                else
                {
                    Log.Warning("Failed to adjust video chainage due to missing lcms segment data");
                }
            }
        }

        private Bitmap CreateBitmapFromRaw(byte[] rawData, int offset, int width, int height, int bytesPerPixel)
        {
            var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            var rect = new Rectangle(0, 0, width, height);
            BitmapData bmpData = bmp.LockBits(rect, ImageLockMode.WriteOnly, bmp.PixelFormat);
            // Calculate pointer to bitmap data
            IntPtr ptrBmp = bmpData.Scan0;
            // Copy raw data for this camera from offset to bitmap buffer
            Marshal.Copy(rawData, offset, ptrBmp, width * height * bytesPerPixel);
            bmp.UnlockBits(bmpData);
            return bmp;
        }

        private async Task CreateRangeAsync(List<Camera360Frame> cameraFrames)
        {
            try
            {
                var existingFrames = (await _repository.GetAllAsync()).ToList();

                var newFrames = cameraFrames
                    .Where(newFrame => !existingFrames.Any(existing =>
                        existing.TimeStamp == newFrame.TimeStamp &&
                        existing.ImagePath == newFrame.ImagePath &&
                        existing.SurveyId == newFrame.SurveyId
                    ))
                    .ToList();

                if (newFrames.Any())
                    await _repository.CreateRangeAsync(newFrames);
            }
            catch (Exception ex)
            {
                Log.Error($"Error in CreateRangeAsync: {ex.Message}");
            }
        }

        void HandleError(LadybugError error)
        {
            if (error != LadybugError.LADYBUG_OK)
            {
                string errorMsg = Marshal.PtrToStringAnsi(Ladybug.ErrorToString(error));
                throw new Exception($"Ladybug SDK Error: {errorMsg}");
            }
        }

        public double TimeDifferenceFromCameraTime(LadybugTimestamp currentTime, LadybugTimestamp previousTime)
        //Returns the difference as a double in seconds between two camera timestamps. Cycletime is used because it is the camera time rather than the getImage time that seconds are
        {
            double difference = 0; //0-127 second cycle made up on multiple parts
            if (currentTime.ulCycleSeconds >= previousTime.ulCycleSeconds)
            {
                //difference = (double)(currentTime.ulCycleSeconds - previousTime.ulCycleSeconds) + (double)(1/8000)*(currentTime.ulCycleCount - previousTime.ulCycleCount) + (double)(1/8000)*(double)(1/3072)*(currentTime.ulCycleOffset - previousTime.ulCycleOffset);
                int secondsDiff = (int)(currentTime.ulCycleSeconds - previousTime.ulCycleSeconds);
                int countDiff = (int)(currentTime.ulCycleCount - previousTime.ulCycleCount);
                int offsetDiff = (int)(currentTime.ulCycleOffset - previousTime.ulCycleOffset);

                //Broken into steps as cannot be directly converted from timestamp -> double
                Double countDiffDouble = ((double)countDiff) / 8000.0;
                Double offsetDiffDouble = (((double)countDiff) / 8000.0) / 3072;
                Double secondsDiffDouble = (double)secondsDiff;

                difference = secondsDiffDouble + countDiffDouble + offsetDiffDouble;

            }
            else
            {
                //difference = (double)(currentTime.ulCycleSeconds + (127 - previousTime.ulCycleSeconds)) + (double)(1 / 8000) * (currentTime.ulCycleCount - previousTime.ulCycleCount) + (double)(1 / 8000) * (double)(1 / 3072) * (currentTime.ulCycleOffset - previousTime.ulCycleOffset);

                int secondsDiff = (int)(currentTime.ulCycleSeconds + (128 - previousTime.ulCycleSeconds)); //Handling counter overflowing
                int countDiff = (int)(currentTime.ulCycleCount - previousTime.ulCycleCount);
                int offsetDiff = (int)(currentTime.ulCycleOffset - previousTime.ulCycleOffset);

                //Broken into steps as cannot be directly converted from timestamp -> double
                Double countDiffDouble = ((double)countDiff) / 8000.0;
                Double offsetDiffDouble = (((double)countDiff) / 8000.0) / 3072;
                Double secondsDiffDouble = (double)secondsDiff;

                difference = secondsDiffDouble + countDiffDouble + offsetDiffDouble;
            }

            return difference;
        }

        public async Task<List<Camera360Frame>> GetAll(Empty empty, CallContext context = default)
        {
            try
            {
                var entities = await _repository.GetAllAsync();
                return entities.ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new List<Camera360Frame>();
            }
        }

        public async Task<IdReply> HasData(Empty empty, CallContext context = default)
        {
            var hasData = await _repository.AnyAsync();
            return new IdReply
            {
                Id = hasData ? 1 : 0,
                Message = "1 true 0 false"
            };
        }

        public async Task<IdReply> DeleteObject(Camera360Frame request, CallContext context = default)
        {
            try
            {
                await _repository.DeleteAsync(request.Id);
                return new IdReply
                {
                    Id = request.Id,
                    Message = "Selected record deleted successfully."
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            return new IdReply
            {
                Id = request.Id,
                Message = "Selected record is failed to be deleted."
            };
        }

        public async Task<List<Camera360Frame>> GetBySurveyId(SurveyIdRequest request, CallContext context = default)
        {
            try
            {
                var entities = await _repository.GetAllAsync();
                if (entities != null && entities.Count() > 0)
                {
                    var matchingEntities = entities.Where(x => x.SurveyId == request.SurveyId).ToList();
                    if (matchingEntities != null)
                    {
                        return matchingEntities;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return new List<Camera360Frame>();
        }


        public async Task<IEnumerable<Camera360Frame>> QueryAsync(string predicate)
        {
            try
            {
                var sqlQuery = predicate;
                var lstTables = await _context.Camera360Frame.FromSqlRaw(sqlQuery).ToListAsync();

                return lstTables;
            }
            catch (Exception ex)
            {
                Utils.RegError($"Error when execute query: {ex.Message}");
                return new List<Camera360Frame>();
            }
        }
    }
}
