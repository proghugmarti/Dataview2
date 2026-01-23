using Azure.Core;
using DataView2.Core;
using DataView2.Core.Helper;
using DataView2.Core.Models;
using DataView2.Core.Models.ExportTemplate;
using DataView2.Core.Models.LCMS_Data_Tables;
using DataView2.Core.Models.Positioning;
using DataView2.Core.Records.Sinks;
using DataView2.Core.SurveyRecordsProto;
using DataView2.GrpcService.Data;
using DataView2.GrpcService.Interfaces;
using DataView2.GrpcService.Services.AppDbServices;
using DataView2.GrpcService.Services.Positioning;
using DataView2.Packages.Lcms;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.UI;
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using NmeaParser.Messages;
using ProtoBuf.Grpc;
using Serilog;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Windows.System;

namespace DataView2.GrpcService.Services.OtherServices
{
    public class GPSProcessedService : IGPSProcessedService
    {
        private readonly AppDbContextProjectData _context;
        private readonly IRepository<GPS_Processed> _repository;


        private readonly OdoDataService _odoDataService;
        private readonly GPS_RawService _gPS_RawService;
        private readonly VideoFrameService _videoFrameService;



        public GPSProcessedService(IRepository<GPS_Processed> repository, AppDbContextProjectData context, 
            GPS_RawService gPS_RawService, OdoDataService odoDataService, VideoFrameService videoFrameService)
        {
            _context = context;
            _repository = repository;
            _gPS_RawService = gPS_RawService;
            _odoDataService = odoDataService;
            _videoFrameService = videoFrameService;
        }
        
        public async Task ProcessPositioningFiles(string folderPath, SurveyIdRequest surveyId)
        {
            // Validate folder exists
            if (!Directory.Exists(folderPath))
            {
                throw new DirectoryNotFoundException($"Folder not found: {folderPath}");
            }

            // Find odo file (should be exactly one)
            var odoFiles = Directory.GetFiles(folderPath, "*odoFile*.txt");
            if (odoFiles.Length != 1)
            {
                throw new FileNotFoundException($"Expected 1 odo file, found {odoFiles.Length} in {folderPath}");
            }

            // Find GPS raw file (pattern matching your example)
            var gpsFiles = Directory.GetFiles(folderPath, "GPS_Raw*.txt");
            if (gpsFiles.Length != 1)
            {
                throw new FileNotFoundException($"Expected 1 GPS raw file, found {gpsFiles.Length} in {folderPath}");
            }

            // Process files - please don't comment this out
            await _odoDataService.ProcessOdoFile(odoFiles[0], surveyId);
            await _gPS_RawService.ProcessGPSrawFile(gpsFiles[0], surveyId);

            Console.WriteLine($"Processed files:\n- {odoFiles[0]}\n- {gpsFiles[0]}");
        }


        public async Task<IdReply> Create(GPS_Processed request, CallContext context = default)
        {
            try
            {

                var entityEntry = _context.Entry(request);

                if (entityEntry.State == EntityState.Detached)
                {
                    // If detached, explicitly attach the entity
                    _context.Attach(request);
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating GPS: {ex.Message}");
            }

            return new IdReply
            {
                Id = request.Id,
                Message = "New GPS created succesfully."
            };
        }

        public async Task<CountReply> CreateAll(List<GPS_Processed> request, CallContext context = default)
        {
            int count = request.Count;
            try
            {
                var entityEntry = _context.AddRangeAsync(request);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                count = 0;
                Console.WriteLine($"Error creating GPS: {ex.Message}");
            }

            return new CountReply { Count = count };
        }

        public async Task<GPS_Processed> EditValue(GPS_Processed request, CallContext context = default)
        {
            return await _repository.UpdateAsync(request);
        }

        public async Task<List<GPS_Processed>> GetAll(Empty empty, CallContext context = default)
        {
            var entities = await _repository.GetAllAsync();
            return new List<GPS_Processed>(entities);
        }

        public async Task<CountReply> GetRecordCount(Empty empty, CallContext context = default)
        {
            var iCount = await _repository.GetRecordCountAsync();

            return new CountReply { Count = iCount };
        }
        public async Task<IEnumerable<GPS_Processed>> QueryAsync(string predicate)
        {
            try
            {
                var sqlQuery = predicate;
                var lstTables = await _context.GPS_Processed.FromSqlRaw(sqlQuery).ToListAsync();

                return lstTables;
            }
            catch (Exception ex)
            {
                Utils.RegError($"Error when execute query: {ex.Message}");
                return new List<GPS_Processed>();
            }
        }

        public async Task<CountReply> GetCountAsync(string sqlQuery)
        {
            try
            {
                var count = await _context.GPS_Processed.FromSqlRaw(sqlQuery).CountAsync();

                return new CountReply { Count = count };
            }
            catch (Exception ex)
            {
                Utils.RegError($"Error when execute query: {ex.Message}");
                return new CountReply { Count = 0 };
            }
        }
        public async Task<IdReply> DeleteObject(GPS_Processed request, CallContext context = default)
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

        public async Task ProcessGPS(string folderPath, SurveyIdRequest surveyRequestId)
        {
            // Extract just the numeric survey ID from folder name
            string folderName = Path.GetFileName(folderPath);


            // Check if already processed
            bool alreadyProcessed = await _context.GPS_Processed.AnyAsync(g => g.SurveyId == surveyRequestId.SurveyId);
            if (alreadyProcessed)
            {
                Console.WriteLine($"From GPS processed : Survey {surveyRequestId.SurveyId} has already been processed. Skipping.");
                return;
            }

            Log.Warning($"ProcessPositioningFiles");
            await ProcessPositioningFiles(folderPath, surveyRequestId);

            // Use the numeric ID in queries
            var odoData = await _context.OdoData
                .Where(o => o.SurveyId == surveyRequestId.SurveyId)
                .OrderBy(o => o.OdoTime)
                .ToListAsync();

            var gpsRawData = await _context.GPS_Raw
                .Where(g => g.SurveyId == surveyRequestId.SurveyId)
                .OrderBy(g => g.UTCTime)
                .ToListAsync();

            if (odoData.Count == 0 || gpsRawData.Count == 0)
            {
                Console.WriteLine("No data found for processing");
                return;
            }

            Log.Warning($"Normalize timing");

            // 2. Normalize time data (same as ProcessData)
            var odoTimeStart = odoData[0].OdoTime;
            var gpsTimeStart = gpsRawData[0].UTCTime;

            var odoTimes = odoData.Select(o => o.OdoTime - odoTimeStart).ToList();
            var odoCounts = odoData.Select(o => o.OdoCount).ToList();
            var chainages = odoData.Select(o => o.Chainage).ToList();
            var gpsTimes = gpsRawData.Select(g => (g.UTCTime - gpsTimeStart)).ToList();

            Log.Warning($"LinearInterpolate");

            // 3. Use external interpolation function (like ProcessData)
            var interpolatedOdoCounts = LinearInterpolate(
                desiredTimes: gpsTimes,
                baseTimes: odoTimes,
                baseValues: odoCounts.Select(x => x).ToList()
            ).Select(x => (int)x).ToList();

            // 4. Calculate chainage (same as ProcessData)
            var interpolatedChainages = new List<double>();

            Stopwatch stopwatch = Stopwatch.StartNew();
            Log.Warning($"Calculation for interpolated chainage starts");

            var odoDict = odoData.GroupBy(o => o.OdoCount).ToDictionary(g => g.Key, g => g.First());

            for (int i = 0; i < interpolatedOdoCounts.Count; i++)
            {
                // Find exact match first
                //var odoRecord = odoData.FirstOrDefault(o => o.OdoCount == interpolatedOdoCounts[i]);
                if (odoDict.TryGetValue(interpolatedOdoCounts[i], out var odoRecord))
                {
                    if (odoRecord != null)
                    {
                        interpolatedChainages.Add(odoRecord.Chainage);
                    }
                    else
                    {
                        // Interpolate between nearest OdoCounts
                        int lowerIndex = 0;
                        while (lowerIndex < odoCounts.Count - 1 &&
                               odoCounts[lowerIndex + 1] <= interpolatedOdoCounts[i])
                        {
                            lowerIndex++;
                        }

                        if (lowerIndex == odoCounts.Count - 1)
                        {
                            interpolatedChainages.Add(chainages.Last());
                        }
                        else
                        {
                            double ratio = (interpolatedOdoCounts[i] - odoCounts[lowerIndex]) /
                                         (double)(odoCounts[lowerIndex + 1] - odoCounts[lowerIndex]);
                            double chainage = chainages[lowerIndex] + ratio *
                                            (chainages[lowerIndex + 1] - chainages[lowerIndex]);
                            interpolatedChainages.Add(chainage);
                        }
                    }
                }
            }

            stopwatch.Stop();
            Log.Warning($"Calculated interpolated chainage {stopwatch.Elapsed.TotalSeconds} seconds");
            stopwatch.Restart();

            // 5. Create GpsProcessed objects
            var processedData = new List<GPS_Processed>();

            Log.Warning($"Calculation for processedData starts");
            for (int i = 0; i < gpsTimes.Count; i++)
            {
                processedData.Add(new GPS_Processed
                {
                    SurveyId = surveyRequestId.SurveyId,
                    Time = gpsTimes[i],
                    OdoCount = interpolatedOdoCounts[i],
                    Chainage = interpolatedChainages[i],
                    Latitude = gpsRawData[i].Latitude,
                    Longitude = gpsRawData[i].Longitude,
                    Heading = gpsRawData[i].Heading
                });
            }

            stopwatch.Stop();
            Log.Warning($"Calculated GPS_Processed {stopwatch.Elapsed.TotalSeconds} seconds");
            stopwatch.Restart();

            if (processedData.Any())
            {
                Log.Warning($"Before saving Processed data into the DB");
                await _context.GPS_Processed.AddRangeAsync(processedData);
                Log.Warning($"saving Processed data into the DB");
                await _context.SaveChangesAsync();
                stopwatch.Stop();
                Log.Information($"Saved {processedData.Count} GPS_Processed entries to the database in {stopwatch.Elapsed.TotalSeconds} seconds.");
            }
            else
            {
                Log.Information("No GPS_Processed data to save.");
            }
        }

        private List<int> LinearInterpolate(List<int> desiredTimes, List<int> baseTimes, List<int> baseValues)
        {
            var results = new List<int>();
            int j = 1; // Start with second base point

            foreach (var desiredTime in desiredTimes)
            {
                // Find the interval where desiredTime falls
                while (j < baseTimes.Count && desiredTime > baseTimes[j])
                {
                    j++;
                }

                if (j >= baseTimes.Count)
                {
                    // Extrapolate beyond last point if needed
                    j = baseTimes.Count - 1;
                    results.Add(baseValues[j]);
                    continue;
                }

                if (desiredTime == baseTimes[j])
                {
                    results.Add(baseValues[j]);
                }
                else
                {
                    // Linear interpolation
                    int timeDiff = baseTimes[j] - baseTimes[j - 1];
                    int valueDiff = baseValues[j] - baseValues[j - 1];
                    int ratio = (desiredTime - baseTimes[j - 1]) / timeDiff;
                    int interpolatedValue = baseValues[j - 1] + ratio * valueDiff;
                    results.Add(interpolatedValue);
                }
            }

            return results;
        }

        public async Task<GPS_Processed> UpdateGenericData(string fieldsToUpdateSerialized)
        {
            var entity = new GPS_Processed();
            Dictionary<string, object> fieldsToUpdate = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(fieldsToUpdateSerialized);
            entity.Id = Convert.ToInt32(fieldsToUpdate["Id"]);
            return await _repository.UpdateEntityAsync(entity, fieldsToUpdate, entity.Id);
        }

        public async Task RecalculateChainageAsync(int surveyId, double newChainageStart)
        {
            using (_context)
            {
                // Retrieve all records for the specific survey
                var gpsRecords = await _context.GPS_Processed
                                                .Where(g => g.SurveyId == surveyId)
                                                .OrderBy(g => g.Time) 
                                                .ToListAsync();
                if (gpsRecords.Count > 0)
                {
                    // Calculate the difference between the current starting chainage and the new desired starting chainage
                    double chainageDifference = newChainageStart - gpsRecords[0].Chainage;
                    foreach (var record in gpsRecords)
                    {
                        // Apply the difference to adjust chainage
                        record.Chainage += chainageDifference;
                    }
                    // Save the changes to the database
                    await _context.SaveChangesAsync();
                }
            }
        }

        public async Task<IdReply> HasData(Empty empty, CallContext context = default)
        {
            try
            {
                var hasData = await _repository.AnyAsync();
                if (hasData)
                {
                    return new IdReply
                    {
                        Id = 1
                    };
                }
                else
                {
                    return new IdReply
                    {
                        Id = 0
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in HasData"+ ex.Message);
                return new IdReply
                {
                    Id = 0
                };
            }
        }

        public async Task<IdReply> HasINSGeometryData(Empty empty, CallContext context = default)
        {
            try
            {
                // Check if there is at least one record in the Geometry_Processed table
                bool hasData = await _context.Geometry_Processed.AnyAsync();

                return new IdReply
                {
                    Id = hasData ? 1 : 0
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in HasINSGeometryData: " + ex.Message);
                return new IdReply
                {
                    Id = 0
                };
            }
        }

        public async Task<List<GPS_Processed>> GetBySurvey(string surveyId, CallContext context = default)
        {
            var gpsProcessedList = new List<GPS_Processed>();
            try
            {
                var survey = _context.Survey.FirstOrDefault(x => x.SurveyIdExternal == surveyId);
                if (survey != null)
                {
                    var entities = await _repository.Query().Where(x => x.SurveyId == survey.Id).ToListAsync();
                    if (entities != null && entities.Count > 0)
                    {
                        gpsProcessedList.AddRange(entities);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in GetBySurvey" + ex.Message);
            }
            return gpsProcessedList;
        }

        public async Task<List<GPS_Processed>> GetBySurveyAndChainage(GpsBySurveyAndChainageRequest request, CallContext context = default)
        {
            var gpsProcessedList = new List<GPS_Processed>();
            try
            {
               // Fetch entities that match the SurveyId and their Chainage is within the specified range
                var entities = await _repository.Query()
                .Where(x => x.SurveyId == request.SurveyId && x.Chainage >= request.StartChainage && x.Chainage <= request.EndChainage)
                .ToListAsync();

               if (entities != null && entities.Count > 0)
               {
                  gpsProcessedList.AddRange(entities);
               }
                
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in GetBySurvey: " + ex.Message);
            }
            return gpsProcessedList;
        }

        public async Task DeleteBySurveyID(string surveyID)
        {
            try
            {
                string tableName = _context.Model.FindEntityType(typeof(GPS_Processed))?.GetTableName();
                string query = $"DELETE FROM {tableName} WHERE SurveyID = {surveyID};";
                await _context.Database.ExecuteSqlRawAsync(query);

                Console.WriteLine($"Deleted all GPS_Processed records for SurveyID {surveyID}.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting GPS_Processed records for SurveyID {surveyID}: {ex.Message}");
            }
        }

        public async Task RunGeometryCalcFromGpsRaw(
            int surveyId,
            double processingInterval,
            double maxCurvature,
            bool negCrossAndCurvature = true,
            bool enableVerticalCurvature = true,
            string crossSlopeDevice = "None" // NEW: placeholder parameter
        )
        {
            bool exists = await _context.Geometry_Processed
    .AnyAsync(g => g.SurveyId == surveyId.ToString());

            if (exists)
            {
                // Data already exists for this SurveyId
                Log.Information($"Geometry_Processed data for surveyId: {surveyId.ToString()} already exist, will not process again!");
                return;
            }

            var gpsData = await _context.GPS_Raw
                .Where(g => g.SurveyId == surveyId)
                .OrderBy(g => g.Chainage)
                .ToListAsync();

            double[] chainages = gpsData.Select(g => g.Chainage).ToArray();
            double[] rolls = gpsData.Select(g => g.Roll ?? 0).ToArray();
            double[] pitches = gpsData.Select(g => g.Pitch ?? 0).ToArray();
            double[] yaws = gpsData.Select(g => g.Yaw ?? 0).ToArray();

            // Placeholder slope initialization
            double[] slopes;
            switch (crossSlopeDevice)
            {
                case "TPL":
                    // TODO: fetch slope data from TPL device records
                    slopes = new double[chainages.Length];
                    break;

                case "Laser":
                    // TODO: fetch slope data from Laser device records
                    slopes = new double[chainages.Length];
                    break;

                case "LCMS":
                    // TODO: fetch slope data from LCMS device records
                    slopes = new double[chainages.Length];
                    break;

                case "None":
                default:
                    // No slope device, initialize empty slope array
                    slopes = new double[chainages.Length];
                    break;
            }

            int outputSize = (int)(chainages.Length / processingInterval) + 1;
            double[] chainageOut = new double[outputSize];
            double[] gradientOut = new double[outputSize];
            double[] slopeOut = new double[outputSize];
            double[] radiusOut = new double[outputSize];

            using var chainageWrapper = new EmxArrayRealTWrapper(chainages);
            using var rollWrapper = new EmxArrayRealTWrapper(rolls);
            using var pitchWrapper = new EmxArrayRealTWrapper(pitches);
            using var yawWrapper = new EmxArrayRealTWrapper(yaws);
            using var slopeWrapper = new EmxArrayRealTWrapper(slopes);

            using var chainageOutWrapper = new EmxArrayRealTWrapper(chainageOut);
            using var gradientOutWrapper = new EmxArrayRealTWrapper(gradientOut);
            using var slopeOutWrapper = new EmxArrayRealTWrapper(slopeOut);
            using var radiusOutWrapper = new EmxArrayRealTWrapper(radiusOut);

            try
            {
                GeometryInterop.Geometry_calc(
                    ref chainageWrapper.Value,
                    ref pitchWrapper.Value,
                    ref rollWrapper.Value,
                    ref yawWrapper.Value,
                    processingInterval,
                    maxCurvature,
                    ref slopeWrapper.Value,
                    ref chainageOutWrapper.Value,
                    ref gradientOutWrapper.Value,
                    ref slopeOutWrapper.Value,
                    ref radiusOutWrapper.Value
                );
            }
            catch (Exception ex)
            {
                Log.Error($"Geometry_calc method call failed: {ex.Message}");
            }

            double[] verticalCurveOut = GeometryUtils.CalculateVerticalCurvature(chainages, pitches, (int)processingInterval);

            try
            {
                var geometryData = new List<Geometry_Processed>();

                int minSize = new[]
                {
            chainageOut.Length,
            gradientOut.Length,
            slopeOut.Length,
            radiusOut.Length,
            verticalCurveOut.Length
        }.Min();

                for (int i = 0; i < minSize; i++)
                {
                    geometryData.Add(new Geometry_Processed
                    {
                        SurveyId = surveyId.ToString(),
                        Chainage = SafeFloat(chainageOut[i]),
                        Gradient = SafeFloat(gradientOut[i]),
                        CrossSlope = SafeFloat(slopeOut[i]),
                        GeoJSON = "", // placeholder
                        PavementType = "", // placeholder
                        HorizontalCurve = SafeFloat(radiusOut[i] * (negCrossAndCurvature ? -1 : 1)),
                        VerticalCurve = enableVerticalCurvature ? SafeFloat(verticalCurveOut[i]) : -1f,
                        Speed = 0f // placeholder
                    });
                }

                if (geometryData.Any())
                {
                    var existingRecords = _context.Geometry_Processed
                        .Where(g => g.SurveyId == surveyId.ToString());

                    _context.Geometry_Processed.RemoveRange(existingRecords);
                    await _context.Geometry_Processed.AddRangeAsync(geometryData);
                    await _context.SaveChangesAsync();
                    await UpdateGeometryGpsAsync(surveyId);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error while saving Geometry_Processed data: {ex.Message}");
            }
        }

        public async Task UpdateGeometryGpsAsync(int surveyId)
        {
            var gpsList = await _context.GPS_Raw
                .Where(r => r.SurveyId == surveyId)
                .OrderBy(r => r.Chainage)
                .ToListAsync();

            if (gpsList.Count == 0)
                return;

            var geometries = await _context.Geometry_Processed
                .Where(g => g.SurveyId == surveyId.ToString())
                .ToListAsync();

            foreach (var geom in geometries)
            {
                var exact = gpsList.FirstOrDefault(r => r.Chainage == geom.Chainage);
                if (exact != null)
                {
                    geom.GPSLatitude = exact.Latitude;
                    geom.GPSLongitude = exact.Longitude;
                }
                else
                {
                    var lower = gpsList.LastOrDefault(r => r.Chainage < geom.Chainage);
                    var upper = gpsList.FirstOrDefault(r => r.Chainage > geom.Chainage);

                    if (lower != null && upper != null)
                    {
                        double ratio = (geom.Chainage - lower.Chainage) / (upper.Chainage - lower.Chainage);
                        geom.GPSLatitude = lower.Latitude + ratio * (upper.Latitude - lower.Latitude);
                        geom.GPSLongitude = lower.Longitude + ratio * (upper.Longitude - lower.Longitude);
                    }
                    else if (lower != null)
                    {
                        geom.GPSLatitude = lower.Latitude;
                        geom.GPSLongitude = lower.Longitude;
                    }
                    else if (upper != null)
                    {
                        geom.GPSLatitude = upper.Latitude;
                        geom.GPSLongitude = upper.Longitude;
                    }
                }

                // Generate GeoJSON Point Feature
                geom.GeoJSON = JsonSerializer.Serialize(new
                {
                    type = "Feature",
                    geometry = new
                    {
                        type = "Point",
                        coordinates = new[] { geom.GPSLongitude, geom.GPSLatitude }
                    },
                    properties = new
                    {
                        type = "INS Geometry",
                        surveyId = geom.SurveyId,
                        chainage = geom.Chainage,
                        gradient = geom.Gradient,
                        crossSlope = geom.CrossSlope,
                        horizontalCurve = geom.HorizontalCurve,
                        verticalCurve = geom.VerticalCurve
                    }
                });
            }

            await _context.SaveChangesAsync();
        }

        private static float SafeFloat(double value, float defaultValue = 0f)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return defaultValue;
            return (float)value;
        }

        public async Task<GPS_Processed> FindClosestChainageFromGPS(double latitude, double longitud, int surveyId)
        {
            var clickedPoint = new MapPoint(longitud, latitude, SpatialReferences.Wgs84);
            var projectedClickedPoint = (MapPoint)GeometryEngine.Project(clickedPoint, SpatialReferences.Wgs84);


            GPS_Processed closestGPSPoint = null; // To store the closest GPS_Processed point
            double closestDistance = double.MaxValue; // Initialize with maximum possible value


            var gpsProcessedData = await _repository.Query().Where(g => g.SurveyId == surveyId).ToListAsync();

            foreach (var gps in gpsProcessedData)
            {
                var gpsPoint = new MapPoint(gps.Longitude, gps.Latitude, SpatialReferences.Wgs84);
                var projectedGpsPoint = (MapPoint)GeometryEngine.Project(gpsPoint, SpatialReferences.Wgs84);
                double distanceKm = GeometryEngine.Distance(projectedClickedPoint, projectedGpsPoint) / 1000.0;

                // Check if the distance is within the threshold and also if it's the closest found so far
                if (distanceKm < closestDistance)
                {
                    closestDistance = distanceKm; // Update the closest distance
                    closestGPSPoint = gps; // Update to the current GPS_Processed point
                }
            }
            return closestGPSPoint; // Return the closest GPS_Processed point found or null if none was within the threshold

        }


    }
}
