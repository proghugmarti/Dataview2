using DataView2.Core;
using DataView2.Core.Helper;
using DataView2.Core.Models;
using DataView2.Core.Models.Other;
using DataView2.GrpcService.Data;
using DataView2.GrpcService.Helpers;
using DataView2.GrpcService.Interfaces;
using DataView2.GrpcService.Protos;
using DataView2.GrpcService.Services.OtherServices;
using Esri.ArcGISRuntime.Geometry;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using ProtoBuf.Grpc;
using Serilog;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;

namespace DataView2.GrpcService.Services
{
    public class VideoFrameService: IVideoFrameService
    {
        private readonly AppDbContextProjectData _context;
        private readonly IRepository<VideoFrame> _repository;
        private readonly SurveyService _surveyService;
        public List<string> detailLogViewHelpers = new List<string>();

        public VideoFrameService(IRepository<VideoFrame> repository, AppDbContextProjectData context, SurveyService surveyService)
        {
            _repository = repository;
            _context = context;
            _surveyService = surveyService;
        }

        public async Task ProcessVideoRating(List<string> videoJsonPaths, string videoPath, SurveyIdRequest surveyIdRequest, Action<int, string> reportProgress)
        {

            try
            {
                int totalFiles = videoJsonPaths.Count;
                int processedFiles = 0;
                foreach (var jsonPath in videoJsonPaths)
                {
                    processedFiles++;
                    var fileName = Path.GetFileName(jsonPath);
                    // Notify UI that this JSON file processing started
                    reportProgress((int)((processedFiles - 1) * 100.0 / totalFiles), $"Processing video file {fileName} ({processedFiles}/{totalFiles})...");
                    var directory = Path.GetDirectoryName(jsonPath);
                    var jpgFiles = Directory.GetFiles(directory, "*.jpg");

                    if (jsonPath != null && jpgFiles.Any())
                    {
                        Core.Helper.DetailLogViewHelper detailLogViewHelper = new Core.Helper.DetailLogViewHelper();
                        detailLogViewHelper.FileName = jsonPath;
                        detailLogViewHelper.FileType = ".json";

                        try
                        {
                            Log.Information($"1. Started reading a json file for video rating {jsonPath}");

                            string jsonContent = File.ReadAllText(jsonPath);

                            var jsonData = System.Text.Json.JsonSerializer.Deserialize<VideoJson>(jsonContent);

                            if (jsonData != null)
                            {
                                var cameraName = jsonData.CameraName;
                                var cameraSerial = jsonData.CameraSerial;
                                var cameraType = jsonData.CameraType;
                                var cameraOffset = jsonData.CameraOffset;

                                var firstRecord = jsonData.SurveyVideoLocationTriggerRecordList.FirstOrDefault();
                                if (firstRecord == null)
                                {
                                    throw new InvalidOperationException("No SurveyVideoLocationTriggerRecord found.");
                                }

                                //Update Video Path in survey
                                await UpdateVideoPathInSurvey(videoPath, surveyIdRequest.SurveyId);

                                //Get GPS_Processed records with same survey Id
                                var gpsProcessed = _context.GPS_Processed.Where(x => x.SurveyId == surveyIdRequest.SurveyId).ToList();

                                if (gpsProcessed == null || gpsProcessed.Count == 0)
                                {
                                    //no coordinate information found.
                                    Log.Error($"GPS information not found with the survey Id : {surveyIdRequest.SurveyId}");
                                    detailLogViewHelper.Status = "FAIL";
                                    detailLogViewHelper.LogDetails = "Video processing failed because GPS information was not found. Ensure that 'GPS_Raw.txt' and 'OdoFile.txt' exist in the folder before proceeding.";

                                    detailLogViewHelpers.Add(Newtonsoft.Json.JsonConvert.SerializeObject(detailLogViewHelper));
                                    return;
                                }

                                List<VideoFrame> frames = new List<VideoFrame>();
                                int videoFrameId = 0;

                                var firstPCTime = jsonData.SurveyVideoLocationTriggerRecordList.FirstOrDefault()?.PCTime;
                                foreach (var videoFrame in jsonData.SurveyVideoLocationTriggerRecordList)
                                {
                                    string imageFilePath = videoFrame.ImageFilePath;
                                    string matchingJpgFile = jpgFiles.FirstOrDefault(file => Path.GetFileName(file) == imageFilePath);

                                    if (!string.IsNullOrEmpty(matchingJpgFile) && !string.IsNullOrEmpty(videoPath))
                                    {
                                        if (!videoPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
                                        {
                                            videoPath += Path.DirectorySeparatorChar;
                                        }

                                        if (matchingJpgFile.StartsWith(videoPath, StringComparison.OrdinalIgnoreCase))
                                        {
                                            matchingJpgFile = matchingJpgFile.Replace(videoPath, "");
                                        }
                                    }

                                    long cameraTime = videoFrame.CameraTime;

                                    long pcTime = videoFrame.PCTime;
                                    DateTime dateTime = new DateTime(pcTime);
                                    DateTime firstDateTime = new DateTime(firstPCTime.Value);

                                    TimeSpan diff = dateTime - firstDateTime;
                                    long milliseconds = (long)diff.TotalMilliseconds;

                                    var timeValues = gpsProcessed.Select(x => x.Time).ToList();
                                    var longitudeValues = gpsProcessed.Select(x => x.Longitude).ToList();
                                    var latitudeValues = gpsProcessed.Select(x => x.Latitude).ToList();
                                    var trackAngleValues = gpsProcessed.Select(x => x.Heading).ToList();
                                    var chainageValues = gpsProcessed.Select(x => x.Chainage).ToList();

                                    var longitude = CoordinateHelper.LinearInterpolateSingle(milliseconds, timeValues, longitudeValues);
                                    var latitude = CoordinateHelper.LinearInterpolateSingle(milliseconds, timeValues, latitudeValues);
                                    var trackAngle = CoordinateHelper.LinearInterpolateSingle(milliseconds, timeValues, trackAngleValues);
                                    var chainage = CoordinateHelper.LinearInterpolateSingle(milliseconds, timeValues, chainageValues);

                                    //offset application
                                    var verticalOffset = Convert.ToDouble(cameraOffset) * 1000; //meter to mm
                                    double[] coordinate = GeneralHelper.ConvertToGPSCoordinates(0, verticalOffset, longitude, latitude, trackAngle);
                                    //double[] coordinate = new double[] { longitude, latitude };

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
                                            type = "Video Rating"
                                        }
                                    };

                                    string geoJsonData = JsonSerializer.Serialize(jsonDataObject);

                                    var surveyName = await  _surveyService.GetSurveyNameById(surveyIdRequest);

                                    var frame = new VideoFrame
                                    {
                                        VideoFrameId = videoFrameId,
                                        Chainage = Math.Round(chainage,2),
                                        GPSLongitude = coordinate[0],
                                        GPSLatitude = coordinate[1],
                                        ImageFileName = matchingJpgFile,
                                        GeoJSON = geoJsonData,
                                        CameraName = cameraName,
                                        CameraSerial = cameraSerial,
                                        CameraType = cameraType,
                                        SurveyName = surveyName,
                                        GPSTrackAngle = trackAngle,
                                        CameraTime = cameraTime,
                                        PCTime = milliseconds,
                                        SurveyId = surveyIdRequest.SurveyId
                                    };
                                    frames.Add(frame);
                                    videoFrameId++;
                                }

                                Log.Information($"2. Saving video frame information in db");

                                //Save frames into the database
                                await Create(frames);
                            }

                            detailLogViewHelper.Status = "PASS";
                            detailLogViewHelper.LogDetails = "Video processing completed successfully.";
                            detailLogViewHelpers.Add(Newtonsoft.Json.JsonConvert.SerializeObject(detailLogViewHelper));
                            Log.Information($"3. Successfully processed {jsonPath} video processing");

                        }
                        catch (Exception ex)
                        {
                            detailLogViewHelper.Status = "FAIL";
                            detailLogViewHelper.LogDetails = $"Video processing failed due to {ex.Message}";
                            detailLogViewHelpers.Add(Newtonsoft.Json.JsonConvert.SerializeObject(detailLogViewHelper));
                        }
                    }
                }

                //recalculate chainage if lcms data available
                var matchingSurvey = _context.Survey.FirstOrDefault(x => x.Id == surveyIdRequest.SurveyId);
                if (matchingSurvey != null)
                {
                    //Get first segment in case there is no segment Id 0
                    var firstSegment = _context.LCMS_Segment
                            .Where(x => x.SurveyId == matchingSurvey.SurveyIdExternal)
                            .OrderBy(x => x.SegmentId)
                            .FirstOrDefault();

                    if (firstSegment != null)
                    {
                        var videos = _context.VideoFrame.Where(x => x.SurveyId == surveyIdRequest.SurveyId);
                        if (videos == null || videos.Count() == 0) return;

                        var firstVideo = videos.FirstOrDefault(x => x.VideoFrameId == 0);
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
            catch (Exception ex)
            {
                Log.Error($"Error in ProcessVideoRating : {ex.Message}");
            }
        }
        private async Task UpdateVideoPathInSurvey(string videoPath, int surveyId)
        {
            try
            {
                //Edit the existing survey video folder path if the matching survey exists
                var matchingSurvey = _context.Survey.FirstOrDefault(x => x.Id == surveyId);
                if (matchingSurvey != null)
                {
                    if (matchingSurvey.VideoFolderPath != videoPath)
                    {
                        matchingSurvey.VideoFolderPath = videoPath;
                        _context.Survey.Update(matchingSurvey);
                        await _context.SaveChangesAsync();
                    }
                }
                
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
            }
        }

        public async Task<IdReply> Create(List<VideoFrame> videoFrames, CallContext context =default)
        {
            try
            {
                foreach (var videoFrame in videoFrames)
                {
                    var existingEntity = await _repository.FirstOrDefaultAsync(s => s.SurveyId == videoFrame.SurveyId && s.ImageFileName == videoFrame.ImageFileName);

                    if (existingEntity == null)
                    {
                        await _repository.CreateAsync(videoFrame);
                    }
                    else
                    {
                        //if the same video exists, overwrite
                        existingEntity.SurveyName = videoFrame.SurveyName;
                        existingEntity.VideoFrameId = videoFrame.VideoFrameId;
                        existingEntity.GPSLongitude = videoFrame.GPSLongitude;
                        existingEntity.GPSLatitude = videoFrame.GPSLatitude;
                        existingEntity.CameraName = videoFrame.CameraName;
                        existingEntity.CameraSerial = videoFrame.CameraSerial;
                        existingEntity.Chainage = videoFrame.Chainage;
                        existingEntity.CameraTime = videoFrame.CameraTime;
                        existingEntity.PCTime = videoFrame.PCTime;

                        await _repository.UpdateAsync(existingEntity);
                    }
                }

                return new IdReply
                {
                    Id = 0,
                    Message = "Survey created successfully."
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new IdReply
                {
                    Id = -1,
                    Message = $"An error occurred: {ex.Message}"
                };
            }
        }

        public async Task<List<VideoFrame>> GetAll(Empty empty, CallContext context = default)
        {
            try
            {
                var entities = await _repository.GetAllAsync();
                return entities.ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new List<VideoFrame>();
            }

        }

        public async Task<List<VideoFrame>> GetByName(string cameraName, CallContext context = default)
        {
            try
            {
                var entities = await _repository.GetAllAsync();
                if(entities != null)
                {
                    var matchingEntities = entities.Where(x => x.CameraName == cameraName).ToList();
                    if(matchingEntities != null)
                    {
                        return matchingEntities;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return new List<VideoFrame>();
        }

        public async Task<List<VideoFrame>> GetBySurveyId(SurveyIdRequest idRequest)
        {
            try
            {
                var entities = await _repository.GetAllAsync();
                if (entities != null)
                {
                    var matchingEntities = entities.Where(x => x.SurveyId == idRequest.SurveyId).ToList();
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
            return new List<VideoFrame>();
        }

        public async Task<List<string>> HasData(Empty empty, CallContext context = default)
        {
            var hasData = await _repository.AnyAsync();
            if (hasData)
            {
                //if there is a video data, return camera information
                var videoFrames = await _repository.GetAllAsync();

                var distinctCameraInfo = videoFrames
                    .Select(vf => vf.CameraName)
                    .Distinct()
                    .ToList();

                return distinctCameraInfo;
            }
            else
            {
                return new List<string>();
            }
        }

        public async Task<IdReply> DeleteObject(VideoFrame request, CallContext context = default)
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

        public async Task<CountReply> GetRecordCount(Empty empty, CallContext context = default)
        {
            var iCount = await _repository.GetRecordCountAsync();

            return new CountReply { Count = iCount };
        }

        public async Task<IEnumerable<VideoFrame>> QueryAsync(string predicate)
        {
            try
            {
                var sqlQuery = predicate;
                var lstTables = await _context.VideoFrame.FromSqlRaw(sqlQuery).ToListAsync();

                return lstTables;
            }
            catch (Exception ex)
            {
                Utils.RegError($"Error when execute query: {ex.Message}");
                return new List<VideoFrame>();
            }
        }
        public async Task<CountReply> GetCountAsync(string sqlQuery)
        {
            try
            {
                var count = await _context.VideoFrame.FromSqlRaw(sqlQuery).CountAsync();

                return new CountReply { Count = count };
            }
            catch (Exception ex)
            {
                Utils.RegError($"Error when execute query: {ex.Message}");
                return new CountReply { Count = 0 };
            }
        }
        public async Task<VideoFrame> EditValue(VideoFrame request, CallContext context = default)
        {
            return await _repository.UpdateAsync(request);
        }

        public async Task<List<string>> HasDataBySurvey(SurveyIdRequest surveyIdRequests, CallContext context = default)
        {
            var distinctCameraBySurvey = new List<string>();
            var videoFrames = await _repository.GetAllAsync();
            if (videoFrames != null && videoFrames.Any())
            {
                var videoFrameCameras = videoFrames
                    .Where(vf => vf.SurveyId == surveyIdRequests.SurveyId)
                    .Select(vf => vf.CameraName)
                    .Distinct()
                    .ToList();

                distinctCameraBySurvey.AddRange(videoFrameCameras);
            }

            var _360CameraFrames = _context.Camera360Frame.Where(x => x.SurveyId == surveyIdRequests.SurveyId).ToList();
            if( _360CameraFrames != null && _360CameraFrames.Count > 0)
            {
                distinctCameraBySurvey.Add("360 Video");
            }
            return distinctCameraBySurvey;
        }

        public async Task<VideoFrame> UpdateGenericData(string fieldsToUpdateSerialized)
        {
            var entity = new VideoFrame();
            Dictionary<string, object> fieldsToUpdate = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(fieldsToUpdateSerialized);
            entity.Id = Convert.ToInt32(fieldsToUpdate["Id"]);
            return await _repository.UpdateEntityAsync(entity, fieldsToUpdate, entity.Id);
        }
    }
}
