using DataView2.Core.Models;
using DataView2.Core.Models.Other;
using DataView2.GrpcService.Data;
using DataView2.GrpcService.Interfaces;
using Google.Protobuf.WellKnownTypes;
using ProtoBuf.Grpc;
using Grpc.Core;
using System.Data.Entity;
using Microsoft.EntityFrameworkCore;
using DataView2.Core;
using Esri.ArcGISRuntime.Geometry;
using System.Data.SQLite;
using DataView2.GrpcService.Helpers;
using DataView2.Core.Models.LCMS_Data_Tables;
using System.Text.Json;
using Serilog;
using MIConvexHull;
using NPOI.OpenXmlFormats.Wordprocessing;


namespace DataView2.GrpcService.Services.OtherServices
{
    public class LASfileService : ILASfileService
    {
        private readonly ILogger<LASfileService> _logger;
        private readonly AppDbContextProjectData _context;
        private readonly IRepository<LASfile> _repository;
        private readonly IRepository<LASPoint> _repositoryPoint;
        private readonly SurveyService _surveyService;
        private readonly LAS_RuttingService _ruttingService;

        public LASfileService(ILogger<LASfileService> logger,
            AppDbContextProjectData appDbContextProject,
            IRepository<LASfile> repository,
            IRepository<LASPoint> repositoryPoints,
            SurveyService surveyService,
            LAS_RuttingService ruttingService
        )
        {
            _logger = logger;
            _context = appDbContextProject;
            _repository = repository;
            _repositoryPoint = repositoryPoints;
            _surveyService = surveyService;
            _ruttingService = ruttingService;
        }

        public async Task<IdReply> DeleteByName(string name, CallContext context = default)
        {
            var reply = new IdReply();

            try
            {
                var lasfile = _context.LASfile.FirstOrDefault(l => l.Name == name);

                if (lasfile == null)
                {
                    reply.Id = 0;
                    reply.Message = $"LAS file with name '{name}' not found";
                    return reply;
                }

                var lasPoints = await _context.LASPoint.Where(p => p.LASfileId == lasfile.Id).ToListAsync();
                _context.LASPoint.RemoveRange(lasPoints);

                // Remove the LAS file itself
                _context.LASfile.Remove(lasfile);

                await _context.SaveChangesAsync();

                reply.Id = lasfile.Id;
                reply.Message = $"LAS file '{name}' and its points were deleted successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting LAS file: {name}, Exception: {ex.Message}");
                reply.Id = 0;
                reply.Message = $"Failed: {ex.Message}";
            }

            return await Task.FromResult(reply);
        }

        public async Task<List<LASfile>> GetAll(Empty empty, CallContext context = default)
        {
            try
            {
                var lasfiles = _context.LASfile.ToList();
                return lasfiles;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving all LAS files: {ex.Message}");
                throw new RpcException(new Status(StatusCode.Internal, $"Failed to retrieve LAS files: {ex.Message}"));
            }
        }

        public async Task<LASfile> GetById(IdRequest request, CallContext context = default)
        {
            var id = request.Id;
            var entity = await _repository.GetByIdAsync(id);

            if (entity == null)
            {
                // Handle the case where the entity with the specified ID is not found
                throw new KeyNotFoundException($"Entity with ID {id} not found.");
            }

            return entity;
        }


        public async Task<List<LASPoint>> GetAllPoints(Empty empty, CallContext context = default)
        {
            try
            {
                var entities = await _repositoryPoint.GetAllAsync();
                return entities.ToList();

            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving all LAS points: {ex.Message}");
                throw new RpcException(new Status(StatusCode.Internal, $"Failed to retrieve LAS points: {ex.Message}"));
            }
        }

        public async Task<IdReply> HasData(Empty empty, CallContext context = default)
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

        public async Task<IdReply> HasDataBySurvey(string surveyId, CallContext context = default)
        {
            var hasServiceData = await _repository.AnyAsync();
            if (!hasServiceData)
            {
                return new IdReply { Id = 0 };
            }

            var surveyData = await _repository.FirstOrDefaultAsync(x => x.SurveyId == surveyId);

            if (surveyData != null)
            {
                return new IdReply { Id = 1 };
            }
            else
            {
                return new IdReply { Id = 0 };
            }
        }

        public async Task<IdReply> ProcessLASfilesNewReader(List<LASfileRequest> requests, CallContext context = default)
        {
            DateTime start = DateTime.Now;
            _logger.LogInformation($"===>> Start Time: {start}");

            var replies = new List<IdReply>();
            int passCount = 0;
            int alreadyProcessedCount = 0;
            int failCount = 0;

            var existingLASfiles = (List<LASfile>)await _repository.GetAllAsync();

            foreach (var request in requests)
            {
                try
                {
                    var reply = await ProcessSingleLASFile(request, existingLASfiles);
                    switch (reply.Id)
                    {
                        case 0: // Failed
                            replies.Add(reply);
                            failCount++;
                            break;

                        case -1: // Already processed
                            alreadyProcessedCount++;
                            break;

                        default: // Success
                            passCount++;
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error while processing LAS file: {request.LASfileName}, Exception: {ex.Message}");
                    replies.Add(new IdReply { Id = 0, Message = $"Failed to process LAS file: {ex.Message}" });
                    failCount++;
                }
            }

            _logger.LogInformation($"===>> ProcessLASfilesOptimized Total time (sec): {(DateTime.Now - start).TotalSeconds}");

            // Builds las import message
            string combinedMessage = "";
            if (replies.Any()) // Error Path
            {
                if (passCount > 0)
                    combinedMessage = $"{passCount} LAS file(s) processed successfully. <br /><br />";

                if (alreadyProcessedCount > 0)
                    combinedMessage += $"{alreadyProcessedCount} selected LAS file(s) have already been processed.<br /><br />";

                if (failCount > 0)
                    combinedMessage += $"{failCount} LAS file(s) failed to process<br />Please check they are valid and try again: <br />";

                combinedMessage += string.Join("<br />", replies.Select(r => "• " + r.Message));
                return new IdReply { Id = 0, Message = combinedMessage };
            }
            if (passCount == 0)
            {
                return new IdReply { Id = -1, Message = "" };
            }

            // Success Path
            if (alreadyProcessedCount == 0)
                combinedMessage = $"All LAS file(s) processed successfully.";

            else if (passCount > 0)
                combinedMessage += $"{passCount} LAS file(s) processed successfully.<br /><br />";

            if (alreadyProcessedCount > 0)
                combinedMessage += $"{alreadyProcessedCount} selected LAS file(s) have already been processed.";

            return new IdReply { Id = 1, Message = combinedMessage };
        }

        private async Task<List<List<LASfileRequest>>> FilterAlreadyProcessedFiles(List<LASfileRequest> requests)
        {
            List<LASfile> existingLASfiles = (List<LASfile>)await _repository.GetAllAsync();
            List<LASfileRequest> processedRequests = new List<LASfileRequest>();
            List<LASfileRequest> unprocessedRequests = new List<LASfileRequest>(requests);

            if (existingLASfiles != null && existingLASfiles.Count > 0 && requests != null && requests.Count > 0)
            {
                unprocessedRequests.Clear();
                foreach (var request in requests)
                {
                    if (existingLASfiles.Any(f => f.Name == request.LASfileName))
                    {
                        processedRequests.Add(request);
                    }
                    else
                    {
                        unprocessedRequests.Add(request);
                    }
                }
            }
            return new List<List<LASfileRequest>> { unprocessedRequests, processedRequests };
        }
        private async Task<IdReply> ProcessSingleLASFile(LASfileRequest request, List<LASfile> existingLASfiles)
        {
            if (!File.Exists(request.FilePath))
            {
                return new IdReply { Id = 0, Message = $"LAS file not found: {request.LASfileName} " };
            }

            using (var reader = new LasFileReader(request.FilePath))
            {
                LasHeader header = reader.ReadHeader();

                var points = ReadPoints(reader, header);
                var coords = GetPointsCoordinates(points);

                // Check for any existing files with same X and Y.
                var existMinXCheck = existingLASfiles.FirstOrDefault(f => f.MinX == header.MinX);
                var existMinYCheck = existingLASfiles.FirstOrDefault(f => f.MinY == header.MinY);

                if (existMinXCheck == null && existMinYCheck == null)
                {
                    string folderName = Directory.GetParent(request.FilePath).Name;
                    string fileNameWithoutExt = Path.GetFileNameWithoutExtension(request.LASfileName);
                    string ext = Path.GetExtension(request.LASfileName);
                    string newName = $"{fileNameWithoutExt}_{folderName}{ext}";
                    var lasFileName = await _repository.FirstOrDefaultAsync(x => x.Name == newName);

                    if (lasFileName == null) // Checks if lasfile name already exists
                        request.LASfileName = newName;
                    else
                        return new IdReply { Id = -1, Message = "Selected LAS file(s) are already processed." };
                }
                else
                    return new IdReply { Id = -1, Message = "Selected LAS file(s) are already processed." };


                var lasfile = await CreateLASFileEntity(request, header, coords);
                var batchPoints = ReadAndPreparePoints(reader, header, lasfile);

                if (batchPoints == null || !batchPoints.Any())
                {
                    return new IdReply { Id = 0, Message = $"No valid points found in LAS file: {request.LASfileName}" };
                }

                await InsertPointsIntoDatabase(batchPoints);

                return new IdReply { Id = lasfile.Id, Message = "Selected LAS file processed successfully." };
            }
        }

        private List<LASPoint> ReadPoints(LasFileReader reader, LasHeader header)
        {
            var batchPoints = new List<LASPoint>();

            foreach (var point in reader.ReadPoints(header))
            {
                batchPoints.Add(new LASPoint
                {
                    X = point.X,
                    Y = point.Y,
                });
            }
            return batchPoints;
        }

        private string GetPointsCoordinates(List<LASPoint> points)
        {
            var vertices = points.Select(p => new LASPointVertex(p.X, p.Y)).ToList();
            var hull = ConvexHull.Create2D(vertices);


            if (hull.ErrorMessage != "")
            {
                Log.Error("Convex hull computation returned error:  " + hull.ErrorMessage);
                return null;
            }
            var hullCoordinates = hull.Result
                .Select(p => new List<double> { p.X, p.Y })
                .ToList();

            var coordinates = JsonSerializer.Serialize(hullCoordinates);

            return coordinates;
        }

        private async Task<LASfile> CreateLASFileEntity(LASfileRequest request, LasHeader header, string coordinates)
        {
            Survey survey;

            if (!string.IsNullOrEmpty(request.SurveyId))
            {
                survey = await _surveyService.GetSurveyEntityByExternalId(request.SurveyId);
                if (survey == null)
                {
                    throw new InvalidOperationException($"Survey with ID '{request.SurveyId}' not found.");
                }
            }
            else
            {
                throw new InvalidOperationException("SurveyId is required.");
            }
            await UpdateSurveyCoordinatesIfNeeded(survey, header.MinX, header.MinY);

            var lasfile = new LASfile
            {
                Name = request.LASfileName,
                SurveyId = request.SurveyId,
                MaxX = header.MaxX,
                MinX = header.MinX,
                MaxY = header.MaxY,
                MinY = header.MinY,
                MaxZ = header.MaxZ,
                MinZ = header.MinZ,
                NumberOfPointRecords = header.NumberOfPointRecords,
                PointDataFormatId = header.PointDataFormatId,
                PointDataRecordLength = header.PointDataRecordLength,
                Coordinates = coordinates
            };

            await _repository.CreateAsync(lasfile);
            await _context.SaveChangesAsync();

            return lasfile;
        }


        private List<LASPoint> ReadAndPreparePoints(LasFileReader reader, LasHeader header, LASfile lasfile)
        {
            var batchPoints = new List<LASPoint>();

            foreach (var point in reader.ReadPoints(header))
            {
                point.LASfile = lasfile;
                point.LASfileId = lasfile.Id;
                batchPoints.Add(point);
            }

            return batchPoints;
        }

        private async Task InsertPointsIntoDatabase(List<LASPoint> batchPoints)
        {
            if (batchPoints.Count == 0) return;

            using (var connection = new SQLiteConnection(_context.Database.GetConnectionString()))
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction())
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = $"INSERT INTO LASPoint (LASfileId, X, Y, Z) VALUES (@LASfileId, @X, @Y, @Z)";
                        command.Parameters.Add(new SQLiteParameter("@LASfileId"));
                        command.Parameters.Add(new SQLiteParameter("@X"));
                        command.Parameters.Add(new SQLiteParameter("@Y"));
                        command.Parameters.Add(new SQLiteParameter("@Z"));

                        foreach (var item in batchPoints)
                        {
                            command.Parameters["@LASfileId"].Value = item.LASfileId;
                            command.Parameters["@X"].Value = item.X;
                            command.Parameters["@Y"].Value = item.Y;
                            command.Parameters["@Z"].Value = item.Z;

                            command.ExecuteNonQuery();
                        }
                    }

                    transaction.Commit();
                }
            }

            _logger.LogInformation($"Processed all points. Total points processed: {batchPoints.Count}");
        }


        public async Task<IdReply> DeleteFileObject(LASfile request, CallContext context = default)
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

        public async Task<IdReply> DeletePointObject(LASPoint request, CallContext context = default)
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

        public async Task<CountReply> GetFileRecordCount(Empty empty, CallContext context = default)
        {
            var iCount = await _repository.GetRecordCountAsync();

            return new CountReply { Count = iCount };
        }

        public async Task<CountReply> GetPointRecordCount(Empty empty, CallContext context = default)
        {
            var iCount = await _repositoryPoint.GetRecordCountAsync();

            return new CountReply { Count = iCount };
        }

        public async Task<IEnumerable<LASfile>> QueryAsyncFile(string predicate)
        {
            try
            {
                var sqlQuery = predicate;
                var lstTables = await _context.LASfile.FromSqlRaw(sqlQuery).ToListAsync();

                return lstTables;
            }
            catch (Exception ex)
            {
                Utils.RegError($"Error when execute query: {ex.Message}");
                return new List<LASfile>();
            }
        }

        public async Task<CountReply> GetCountFileAsync(string sqlQuery)
        {
            try
            {
                var count = await _context.LASfile.FromSqlRaw(sqlQuery).CountAsync();

                return new CountReply { Count = count };
            }
            catch (Exception ex)
            {
                Utils.RegError($"Error when execute query: {ex.Message}");
                return new CountReply { Count = 0 };
            }
        }

        public async Task<IEnumerable<LASPoint>> QueryAsyncPoint(string predicate)
        {
            try
            {
                var sqlQuery = predicate;
                var lstTables = await _context.LASPoint.FromSqlRaw(sqlQuery).ToListAsync();

                return lstTables;
            }
            catch (Exception ex)
            {
                Utils.RegError($"Error when execute query: {ex.Message}");
                return new List<LASPoint>();
            }
        }

        public async Task<CountReply> GetCountPointAsync(string sqlQuery)
        {
            try
            {
                var count = await _context.LASPoint.FromSqlRaw(sqlQuery).CountAsync();

                return new CountReply { Count = count };
            }
            catch (Exception ex)
            {
                Utils.RegError($"Error when execute query: {ex.Message}");
                return new CountReply { Count = 0 };
            }
        }

        public async Task<LASfile> EditFileValue(LASfile request, CallContext context = default)
        {
            return await _repository.UpdateAsync(request);
        }

        public async Task<LASPoint> EditPointValue(LASPoint request, CallContext context = default)
        {
            return await _repositoryPoint.UpdateAsync(request);
        }


        public async Task<List<LASPoint>> GetPointsAlongLineAsync(PointsRequest request)
        {
            Log.Information($"[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[rutting logs  { request.LasRuttingId}");

            Log.Information($"Fetching points for PointsRequest:  X1={request.X1}, Y1={request.Y1}, X2={request.X2}, Y2={request.Y2}, LASFileIds={string.Join(", ", request.LASFileIds ?? new List<int>())}");

            var allPoints = await _repositoryPoint.GetFilteredAsync(p => request.LASFileIds.Contains(p.LASfileId));
            Log.Information($"Total points retrieved from repository: {allPoints.Count}");

            var startPoint = new MapPoint(request.X1, request.Y1, SpatialReferences.Wgs84);
            var endPoint = new MapPoint(request.X2, request.Y2, SpatialReferences.Wgs84);
            var line = new Polyline(new[] { startPoint, endPoint }, SpatialReferences.Wgs84);

            double bufferDistance = 0.05; // Adjust if needed
            double intervalDistance = 0.25; // Target spacing in meters

            var pointsOnLine = allPoints
                .Where(p => p.Z != 0 && IsPointWithinBuffer(line, new MapPoint(p.X, p.Y, SpatialReferences.Wgs84), bufferDistance))
                .OrderBy(p => CalculateDistance(request.X1, request.Y1, p.X, p.Y))
                .ToList();

            List<LASPoint> selectedPoints = new List<LASPoint>();

            if (pointsOnLine.Count == 0)
            {
                Console.WriteLine("No points found on the line.");
                return selectedPoints;
            }

            selectedPoints.Add(pointsOnLine.First());

            double nextInterval = intervalDistance;
            while (nextInterval <= CalculateDistance(request.X1, request.Y1, request.X2, request.Y2))
            {
                var closestPoint = pointsOnLine
                    .Where(p => Math.Abs(CalculateDistance(request.X1, request.Y1, p.X, p.Y) - nextInterval) <= bufferDistance)
                    .OrderBy(p => CalculateDistance(request.X1, request.Y1, p.X, p.Y))
                    .FirstOrDefault();

                if (closestPoint != null)
                {
                    selectedPoints.Add(closestPoint);
                }

                nextInterval += intervalDistance;
            }

            // **Ensure last point is close to the end**
            var lastClosestPoint = pointsOnLine.OrderBy(p => CalculateDistance(request.X2, request.Y2, p.X, p.Y)).FirstOrDefault();
            if (lastClosestPoint != null && !selectedPoints.Contains(lastClosestPoint))
            {
                selectedPoints.Add(lastClosestPoint);
            }

            return selectedPoints;
        }

        private bool IsPointWithinBuffer(Polyline line, MapPoint point, double bufferDistance)
        {
            var nearestCoord = GeometryEngine.NearestCoordinate(line, point);
            if (nearestCoord == null) return false;

            var projectedPoint = nearestCoord.Coordinate;

            // Measure the perpendicular distance (in meters)
            var distanceResult = GeometryEngine.DistanceGeodetic(projectedPoint, point,
                                        LinearUnits.Meters, AngularUnits.Degrees, GeodeticCurveType.Geodesic);

            return distanceResult.Distance <= bufferDistance; // Extract the .Distance property
        }



        private double CalculateDistance(double x1, double y1, double x2, double y2)
        {
            // Create MapPoint objects for each set of coordinates, assuming they are in WGS84 (latitude and longitude)
            var point1 = new MapPoint(x1, y1, SpatialReferences.Wgs84);
            var point2 = new MapPoint(x2, y2, SpatialReferences.Wgs84);

            // Calculate geodetic distance between points
            var distanceResult = GeometryEngine.DistanceGeodetic(point1, point2, LinearUnits.Meters, AngularUnits.Degrees, GeodeticCurveType.Geodesic);

            // Return the distance in meters
            return distanceResult.Distance;
        }

        private List<LASPoint> LinearRegression(List<LASPoint> points)
        {
            // Perform linear regression using indices instead of X
            double indexSum = Enumerable.Range(0, points.Count).Sum();
            double zSum = points.Sum(p => p.Z);
            double indexZSum = points.Select((p, i) => i * p.Z).Sum();
            double indexSquaredSum = Enumerable.Range(0, points.Count).Sum(i => i * i);
            int n = points.Count;

            double slope = (n * indexZSum - indexSum * zSum) / (n * indexSquaredSum - indexSum * indexSum);
            double intercept = (zSum - slope * indexSum) / n;

            // Flatten using index-based regression
            var flattenedPoints = points.Select((p, i) => new LASPoint
            {
                Id = p.Id,
                X = p.X,
                Z = p.Z - (slope * i + intercept) // Flatten Z-values using index-based regression
            }).ToList();

            return flattenedPoints;
        }

        public Task<RuttingResult> CalculateRutting(CalculateRuttingsRequest rutRequest)
        {
            var rutPoint = rutRequest.RutPoint;
            var points = rutRequest.Points;
            double straightEdgeLength = rutRequest.strghtEdgLength;
            double spacing = rutRequest.distanceBetweenPoints; // Distance between points is 0.25m
            double rutDepth = 0;

            int pointsOnEachSide = (int)Math.Round((straightEdgeLength / 2) / spacing);

            // Find the index of the rut point
            int rutPointIndex = points.FindIndex(p => p.Id == rutPoint.Id);
            if (rutPointIndex == -1)
                throw new InvalidOperationException("Rut point not found in the points list.");

            // Get range based on index distance (instead of X)
            int leftIndex = Math.Max(rutPointIndex - pointsOnEachSide, 0);
            int rightIndex = Math.Min(rutPointIndex + pointsOnEachSide, points.Count - 1);

            var rangePoints = points.Skip(leftIndex).Take(rightIndex - leftIndex + 1).ToList();

            // Perform linear regression using indices instead of X
            var flattenedPoints = LinearRegression(rangePoints);

            // Find the highest contact points
            var leftPoints = flattenedPoints.Take(rutPointIndex - leftIndex + 1);
            var leftContactPoint = leftPoints.Any() ? leftPoints.OrderByDescending(p => p.Z).First() : null;

            var rightPoints = flattenedPoints.Skip(rutPointIndex + 1 - leftIndex);
            var rightContactPoint = rightPoints.Any() ? rightPoints.OrderByDescending(p => p.Z).First() : null;


            if (leftContactPoint != null && rightContactPoint != null)
            {
                // Get original Z-values for the straightedge calculation
                var originalLeftContact = points.First(p => p.Id == leftContactPoint.Id);
                var originalRightContact = points.First(p => p.Id == rightContactPoint.Id);

                double contactDistance = CalculateDistance(originalLeftContact.X, originalLeftContact.Y, originalRightContact.X, originalRightContact.Y);

                // Distance relative to the left contact point
                // ShiftedX and ShiftedZ is rutpoint's distance from leftcontact point's X & Z.
                double shiftedX = CalculateDistance(originalLeftContact.X, originalLeftContact.Y, rutPoint.X, rutPoint.Y);
                double shiftedZ = rutPoint.Z - originalLeftContact.Z;

                // m = (y2 - y1) / (x2 - x1) (Slope)
                double finalSlope = (originalRightContact.Z - originalLeftContact.Z) /
                    contactDistance;

                // y = mx + b (Straightedge)
                double finalStraightEdgeZ = finalSlope * shiftedX;

                // Compute rut depth
                rutDepth = finalStraightEdgeZ - shiftedZ;

                // Set any negative rut depth to 0
                if (rutDepth < 0)
                    rutDepth = 0;

                return Task.FromResult(new RuttingResult
                {
                    RutDepth = rutDepth * 1000, // Convert to mm
                    ContactPoints = new List<LASPoint> { originalLeftContact, originalRightContact },
                    Id = string.Join("", new[] { originalLeftContact.Id, originalRightContact.Id }.OrderBy(id => id))
                });
            }
            else
            {
                return Task.FromResult(new RuttingResult
                {
                    ContactPoints = new List<LASPoint>(),
                    Id = "No Contact Points"
                });
            }
        }

        public RuttingResult CalculateMaxRutting(CalculateMaxRuttingRequest rutRequest)
        {
            var point1 = rutRequest.Point1;
            var point2 = rutRequest.Point2;
            var points = rutRequest.Points;

            // Ensure point1 comes before point2 in the list
            int index1 = points.FindIndex(p => p.Id == point1.Id);
            int index2 = points.FindIndex(p => p.Id == point2.Id);
            if (index1 == -1 || index2 == -1) return null;

            if (index1 > index2)
            {
                (index1, index2) = (index2, index1);
                (point1, point2) = (point2, point1);
            }

            // Get points between the indices
            var rangePoints = points.Skip(index1).Take(index2 - index1 + 1).ToList();
            int n = rangePoints.Count;
            if (n < 2) return null;

            // Compute max rut depth
            double maxRutDepth = 0;
            LASPoint deepestPoint = null;

            double contactDistance = CalculateDistance(point1.X, point1.Y, point2.X, point2.Y);
            double finalSlope = (point2.Z - point1.Z) / contactDistance;

            for (int i = 0; i < n; i++)
            {
                // Distance relative to the left contact point
                // ShiftedX and ShiftedZ is rutpoint's distance from leftcontact point's X & Z.
                double shiftedX = CalculateDistance(point1.X, point1.Y, rangePoints[i].X, rangePoints[i].Y);
                double shiftedZ = (rangePoints[i].Z - point1.Z);
                double expectedZ = finalSlope * shiftedX;
                double rutDepth = (expectedZ - shiftedZ) * 1000;

                if (rutDepth > maxRutDepth)
                {
                    maxRutDepth = rutDepth;
                }
            }

            return new RuttingResult
            {
                RutDepth = maxRutDepth,
                ContactPoints = new List<LASPoint> { point1, point2 },
                Id = $"{point1.Id}_{point2.Id}"
            };
        }



        // Updates survey coordinates if this is the first file or they need updating
        private async Task UpdateSurveyCoordinatesIfNeeded(Survey surveyToUpdate, double minX, double minY)
        {


            // Update MinX and MinY if not already set or if these values are smaller
            bool updated = false;

            if (surveyToUpdate.GPSLatitude == 0.0)
            {
                surveyToUpdate.GPSLatitude = minY;
                updated = true;
            }

            if (surveyToUpdate.GPSLongitude == 0.0)
            {
                surveyToUpdate.GPSLongitude = minX;
                updated = true;
            }

            // Save changes if necessary
            if (updated)
            {
                surveyToUpdate = await _surveyService.EditValue(surveyToUpdate);
            }
        }

       // private int minimumPoints = 14; //Minimum points required to perform the calculation
        //private int additionalPoints = 3; //Number N of additional points to perform the calculation (N behind and N ahead)
        public async Task<RuttingResult> GetPointsAndCalculateRutFromLine(PointsRequest request)
        {
            var laspointsLine = GetPointsAlongLineAsync(request).Result; // Synchronous waiting on async task


            //LOG 
            Log.Information($"[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[rutting logs");
            Log.Information($"Processing PointsRequest: X1={request.X1}, Y1={request.Y1}, X2={request.X2}, Y2={request.Y2}, LasRuttingId={request.LasRuttingId}, LASFileIds={string.Join(", ", request.LASFileIds ?? new List<int>())}");
            Log.Information($"Retrieved {laspointsLine?.Count} points along the line.");


            if (laspointsLine == null || laspointsLine.Count == 0)
            {
                return new RuttingResult();
            }

            int straightEdge = laspointsLine.Count > 50 ? 6 : 4;
            double spacing = 0.25;

            double maxRutDepth = 0;
            int maxRutIndex = -1;

            int pointsOnEachSide = (int)Math.Round((straightEdge / 2) / spacing);

            for (int i = 1; i < laspointsLine.Count - 1; i++)
            {
                var rutPoint = laspointsLine[i];

                // Get range based on index distance (instead of X)
                int leftIndex = Math.Max(i - pointsOnEachSide, 0);
                int rightIndex = Math.Min(i + pointsOnEachSide, laspointsLine.Count - 1);

                var rangePoints = laspointsLine.Skip(leftIndex).Take(rightIndex - leftIndex + 1).ToList();
              
                // Perform linear regression using indices instead of X
                var flattenedPoints = LinearRegression(rangePoints);

                // Find the highest contact points
                var leftPoints = flattenedPoints.Take(i - leftIndex + 1);
                var rightPoints = flattenedPoints.Skip(i + 1 - leftIndex);

                var leftContactPoint = leftPoints.Any() ? leftPoints.OrderByDescending(p => p.Z).FirstOrDefault() : null;
                var rightContactPoint = rightPoints.Any() ? rightPoints.OrderByDescending(p => p.Z).FirstOrDefault() : null;

                if (leftContactPoint == null || rightContactPoint == null)
                    continue;

                var originalLeft = laspointsLine.First(p => p.Id == leftContactPoint.Id);
                var originalRight = laspointsLine.First(p => p.Id == rightContactPoint.Id);

                int leftRelativeIndex = rangePoints.FindIndex(p => p.Id == originalLeft.Id);
                int rightRelativeIndex = rangePoints.FindIndex(p => p.Id == originalRight.Id);
                int rutRelativeIndex = rangePoints.FindIndex(p => p.Id == rutPoint.Id);

                /*double finalSlope = (originalRight.Z - originalLeft.Z) / (rightRelativeIndex - leftRelativeIndex);
                double finalIntercept = originalLeft.Z - (finalSlope * leftRelativeIndex);
                double straightEdgeZ = finalSlope * rutRelativeIndex + finalIntercept;*/

                double contactDistance = CalculateDistance(originalLeft.X, originalLeft.Y, originalRight.X, originalRight.Y);

                // Distance relative to the left contact point
                // ShiftedX and ShiftedZ is rutpoint's distance from leftcontact point's X & Z.
                double shiftedX = CalculateDistance(originalLeft.X, originalLeft.Y, rutPoint.X, rutPoint.Y);
                double shiftedZ = rutPoint.Z - originalLeft.Z;

                // m = (y2 - y1) / (x2 - x1) (Slope)
                double finalSlope = (originalRight.Z - originalLeft.Z) / contactDistance;

                // y = mx + b (Straight edge) 
                double straightEdgeZ = finalSlope * shiftedX;

                double rutDepth = (straightEdgeZ - shiftedZ) * 1000;

                if (rutDepth > maxRutDepth)
                {
                    maxRutDepth = rutDepth;
                    maxRutIndex = i;
                }
            }


            RuttingResult ruttingResult;
            var point1 = laspointsLine[0];
            var point2 = laspointsLine[laspointsLine.Count - 1];

            if (maxRutDepth > 0 && maxRutIndex != -1)
            {
                ruttingResult = new RuttingResult()
                {
                    RutDepth = maxRutDepth,
                    ContactPoints = new List<LASPoint> { point1, point2 },
                    Id = $"{point1.Id}_{point2.Id}",
                    LasRuttingId = request.LasRuttingId

                };
                Log.Information($"Rutting result created with depth: {ruttingResult.RutDepth} mm.");

            }
            else // Perform point to point if rutDepth = 0 
            {
                var requestRut = new CalculateMaxRuttingRequest
                {
                    Point1 = point1,
                    Point2 = point2,
                    Points = laspointsLine,
                    
                };

                ruttingResult = CalculateMaxRutting(requestRut);
                ruttingResult.LasRuttingId = request.LasRuttingId;// Synchronous method call

            }

            SaveRuttingResultsToTableAsync(ruttingResult);


            return ruttingResult;
        }

      

     
        public async Task<IdReply> SaveRuttingResultsToTableAsync(RuttingResult resultToSave )
        {
            //if exist update it for recalculating 
            if (resultToSave.LasRuttingId != 0 )
            {
                var existingRutting = await _ruttingService.UpdateRecalculatedById(new 
                    LasRuttingRecalculateRequest { 
                    Id = resultToSave.LasRuttingId,
                    NewDepthFactor = resultToSave.RutDepth
                }); 

                return new IdReply
                {
                    Id = existingRutting.Id,
                    Message = "Rutting result recalculated and updated successfully."
                };
            }


            //if not, then create a new one 

            var laspoint = resultToSave.ContactPoints[0];
            var lasFileid = laspoint.LASfileId;
            var idrequestlasfile = new IdRequest
            {
                Id = lasFileid
            };
            // Extract SurveyId from LASfile
            LASfile lasfile = await GetById(idrequestlasfile);

            var spatialReference = SpatialReferences.Wgs84;

            var firstMapPoint = new MapPoint(resultToSave.ContactPoints.First().X, resultToSave.ContactPoints.First().Y, spatialReference);
            var lastMapPoint = new MapPoint(resultToSave.ContactPoints.Last().X, resultToSave.ContactPoints.Last().Y, spatialReference);
            var distance = GeometryEngine.DistanceGeodetic(firstMapPoint, lastMapPoint, LinearUnits.Meters, AngularUnits.Degrees, GeodeticCurveType.Geodesic).Distance;

            // Convert RutWidth metres to millimetres
            distance = distance * 1000;
            // Create GeoJSON using LAS points
            var geoJsonObject = new
            {
                type = "Feature",
                geometry = new
                {
                    type = "Polyline",
                    coordinates = new List<double[]>()
                },
                properties = new
                {
                    type = "LasRutting",
                    rutDepth = resultToSave.RutDepth,
                    rutWidth = distance,
                    SurveyId = lasfile.SurveyId
                }
            };

            // Assuming `result.LasPoints` contains the relevant LAS points for this rutting result
            foreach (var point in resultToSave.ContactPoints.OrderBy(p => p.Id))
            {
                geoJsonObject.geometry.coordinates.Add(new double[] { point.X, point.Y });
            }

            // Serialize to JSON
            string geoJsonString = JsonSerializer.Serialize(geoJsonObject);

            var newRutting = new LAS_Rutting
            {
            
                GeoJSON = geoJsonString,
                GPSLatitude = resultToSave.ContactPoints[0].Y,
                GPSLongitude = resultToSave.ContactPoints[0].X,
                SurveyId = lasfile.SurveyId,
                RutDepth_mm = resultToSave.RutDepth,
                RutWidth_m = distance,
              
            };

            var insertResponse = await _ruttingService.Create(newRutting);

            if (insertResponse.Id < 1)
            {
                throw new RpcException(new Status(StatusCode.Internal, $"Failed to insert result {resultToSave.Id}."));
            }
            return insertResponse;
        }

        public async Task<List<LASfile>> GetLasFilesByIdsAsync(List<int> ids)
        {

            return _context.LASfile
            .Where(lasFile => ids.Contains(lasFile.Id))
            .ToList();
        }

        public async Task<LASfile> UpdateGenericData(string fieldsToUpdateSerialized)
        {
            var entity = new LASfile();
            Dictionary<string, object> fieldsToUpdate = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(fieldsToUpdateSerialized);
            entity.Id = Convert.ToInt32(fieldsToUpdate["Id"]);
            return await _repository.UpdateEntityAsync(entity, fieldsToUpdate, entity.Id);
        }

        public async Task<LASPoint> UpdateGenericDataLasPoint(string fieldsToUpdateSerialized)
        {
            var entity = new LASPoint();
            Dictionary<string, object> fieldsToUpdate = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(fieldsToUpdateSerialized);
            entity.Id = Convert.ToInt32(fieldsToUpdate["Id"]);
            return await _repositoryPoint.UpdateEntityAsync(entity, fieldsToUpdate, entity.Id);
        }

        public async Task<List<LASfile>> GetAllLASFilesBySurvey(string surveyId)
        {
            try
            {
                var lasfiles = _context.LASfile.Where(x => x.SurveyId == surveyId).ToList();
                return lasfiles;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new List<LASfile>();
            }
        }
    }
}
