using DataView2.Core;
using DataView2.Core.Helper;
using DataView2.Core.MapHelpers;
using DataView2.Core.Models;
using DataView2.Core.Models.LCMS_Data_Tables;
using DataView2.Core.Models.Other;
using DataView2.GrpcService.Data;
using DataView2.GrpcService.Interfaces;
using DataView2.GrpcService.Services.AppDbServices;
using Esri.ArcGISRuntime.Geometry;
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ProtoBuf.Grpc;
using Serilog;
using System.Data.SQLite;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using static DataView2.Core.Helper.TableNameHelper;


namespace DataView2.GrpcService.Services.LCMS_Data_Services
{
    public class SegmentService : BaseService<LCMS_Segment, IRepository<LCMS_Segment>>, ISegmentService
    {
        private readonly AppDbContextProjectData _context;
        private readonly IServiceProvider _serviceProvider;
        private readonly VideoFrameService _videoFrameService;
        private readonly SurveyService _surveyService;


        IDbContextFactory<AppDbContextProjectData> _dbContextFactory;
        List<LCMS_Segment> xmlSegmentList = new List<LCMS_Segment>();
        private string databasePath = "";


        public SegmentService(IRepository<LCMS_Segment> repository, IDbContextFactory<AppDbContextProjectData> dbContextFactor,
            IServiceProvider serviceProvider, VideoFrameService videoFrameService, SurveyService surveyservice) : base(repository)
        {
            _dbContextFactory = dbContextFactor;
            _context = _dbContextFactory.CreateDbContext();
            _serviceProvider = serviceProvider;
            _videoFrameService = videoFrameService;
            _surveyService = surveyservice;
        }

        public async Task<CountReply> GetRecordCount(Empty empty, CallContext context = default)
        {
            var iCount = await _repository.GetRecordCountAsync();

            return new CountReply { Count = iCount };
        }
        public async Task<IEnumerable<LCMS_Segment>> QueryAsync(string predicate)
        {
            try
            {
                var sqlQuery = predicate;
                var lstTables = await _context.LCMS_Segment.FromSqlRaw(sqlQuery).ToListAsync();

                return lstTables;
            }
            catch (Exception ex)
            {
                Utils.RegError($"Error when execute query: {ex.Message}");
                return new List<LCMS_Segment>();
            }
        }

        public async Task<CountReply> GetCountAsyncBySurveyId(string surveyId)
        {
            try
            {
                var count = await _context.LCMS_Segment.Where(x => x.SurveyId == surveyId).CountAsync();

                return new CountReply { Count = count };
            }
            catch (Exception ex)
            {
                Utils.RegError($"Error when execute query: {ex.Message}");
                return new CountReply { Count = 0 };
            }
        }

        public async Task<CountReply> GetCountAsync(string sqlQuery)
        {
            try
            {
                var count = await _context.LCMS_Segment.FromSqlRaw(sqlQuery).CountAsync();

                return new CountReply { Count = count };
            }
            catch (Exception ex)
            {
                Utils.RegError($"Error when execute query: {ex.Message}");
                return new CountReply { Count = 0 };
            }
        }

        public async Task<IdReply> UpdateSegmentOffsetInDB(SegmentMovementRequest movementRequest, CallContext context = default)
        {
            List<string> queries = new List<string>();

            Log.Information($"SegmentMovementRequest items {movementRequest.SegmentIds.Count}");

            var segmentIdSet = movementRequest.SegmentIds.ToHashSet();
            var allSegments = await _context.LCMS_Segment
                .Where(s => segmentIdSet.Contains(s.Id))
                .ToListAsync();
            foreach (var segment in allSegments)
            {

                double[] offset = new double[] { movementRequest.HorizontalOffset, movementRequest.VerticalOffset };
                var segmentId = segment.SegmentId;
                var surveyId = segment.SurveyId;

                try
                {
                    foreach (var tableNameMapping in TableNameHelper.TableNameMappings)
                    {
                        var dbName = tableNameMapping.DBName;
                        //get only lcms tables
                        if (dbName.StartsWith("LCMS"))
                        {
                            var dbSetProperty = _context.GetType().GetProperty(dbName);
                            if (dbSetProperty != null)
                            {
                                var dbSet = dbSetProperty.GetValue(_context);

                                // Use LINQ dynamically
                                var whereMethod = typeof(Queryable).GetMethods()
                                    .First(m => m.Name == "Where" && m.GetParameters().Length == 2)
                                    .MakeGenericMethod(dbSetProperty.PropertyType.GenericTypeArguments[0]);

                                // Build the predicate
                                var parameter = Expression.Parameter(dbSetProperty.PropertyType.GenericTypeArguments[0], "x");
                                var predicate = Expression.Lambda(
                                    Expression.AndAlso(
                                        Expression.Equal(Expression.Property(parameter, "SurveyId"), Expression.Constant(surveyId)),
                                        Expression.Equal(Expression.Property(parameter, "SegmentId"), Expression.Constant(segmentId))
                                    ),
                                    parameter
                                );

                                // Apply the Where clause
                                var filteredQuery = whereMethod.Invoke(null, new object[] { dbSet, predicate }) as IQueryable;

                                //handle multi layers differently
                                if (tableNameMapping.LayerName == LayerNames.Roughness || tableNameMapping.LayerName == LayerNames.Rutting)
                                {
                                    await UpdateMultiGeoJsonWithOffsetAsync(filteredQuery, offset, false);
                                }
                                else
                                {
                                    queries.AddRange(await AddQueryForOffsetAsync(filteredQuery, dbName, offset));
                                }
                            }
                        }
                    }

                        queries.AddRange(await AddQueryForOffsetAsync(_context.MetaTableValue.Where(x => x.SegmentId == segmentId && x.SurveyId == surveyId), "MetaTableValue", offset));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception in creating update queries for defects : {ex.Message}");
                }
            }

            DateTime end = DateTime.Now;
            await ExecuteQueryInDb(queries);
            Log.Information($"Time(seconds) to update GPS data in database : {(DateTime.Now - end).TotalSeconds}");

            IdReply reply = new IdReply();
            return reply;
        }

        private async Task<List<string>> AddQueryForOffsetAsync(IQueryable defectsQuery, string tableName, double[] offset)
        {
            var entityQuery = defectsQuery.Cast<IEntity>();
            var defects = entityQuery.ToList();
            if (defects == null || !defects.Any())
                return new List<string>();

            return UpdateCoordinatesWithOffset(defects.Select(x => new TargetedGPSProperties
            {
                Id = x.Id,
                GPSLatitude = x.GPSLatitude,
                GPSLongitude = x.GPSLongitude,
                RoundedGPSLatitude = x.RoundedGPSLatitude,
                RoundedGPSLongitude = x.RoundedGPSLongitude,
                GeoJSON = x.GeoJSON,
                TrackAngle = x.GPSTrackAngle,
            }).ToList(), offset, tableName, false);
        }

        private async Task UpdateMultiGeoJsonWithOffsetAsync(IQueryable defectsQuery, double[] offset, bool useTrackAngle = false)
        {
            foreach (var entity in defectsQuery)
            {
                if (entity != null)
                {
                    // Define the list of GeoJSON properties to process
                    var geoJsonProperties = new List<string> { "GeoJSON", "LwpGeoJSON", "RwpGeoJSON" };

                    // Add "CwpGeoJSON" only if the entity has this property
                    if (entity.GetType().GetProperty("CwpGeoJSON") != null)
                    {
                        geoJsonProperties.Add("CwpGeoJSON");
                    }

                    foreach (var fieldName in geoJsonProperties)
                    {
                        var propertyInfo = entity.GetType().GetProperty(fieldName);
                        if (propertyInfo == null)
                            continue;

                        var geoJsonValue = propertyInfo.GetValue(entity) as string;
                        if (geoJsonValue == null)
                            continue;

                        dynamic geoJson = JsonConvert.DeserializeObject(geoJsonValue);
                        JArray coordinates = geoJson.geometry.coordinates;

                        if (useTrackAngle)
                        {
                            var trackAngle = (double)entity.GetType().GetProperty("GPSTrackAngle").GetValue(entity);
                            foreach (JArray point in coordinates)
                            {
                                var newCoordinate = GeneralHelper.ConvertToGPSCoordinates(offset[0], offset[1], (double)point[0], (double)point[1], trackAngle);
                                point[0] = newCoordinate[0];
                                point[1] = newCoordinate[1];
                            }
                        }
                        else
                        {
                            foreach (JArray point in coordinates)
                            {
                                point[0] = (double)point[0] + offset[0];
                                point[1] = (double)point[1] + offset[1];
                            }
                        }

                        propertyInfo.SetValue(entity, JsonConvert.SerializeObject(geoJson));
                    }
                    _context.Update(entity);
                }
            }

            // Save all changes to the database
            await _context.SaveChangesAsync();
        }

     


        public async Task UpdateDefectsOnly(List<DefectMovementRequest> requests)
        {
            try
            {
                var queries = new List<string>();

                foreach (var request in requests)
                {
                    var dbName = TableNameHelper.TableNameMappings.FirstOrDefault(x => x.LayerName == request.Table).DBName ?? request.Table;
                    var offset = new double[] { request.HorizontalOffset, request.VerticalOffset };
                    var filteredQuery = GetFilteredQueryWithId(request.Id, dbName);

                    //handle multi layers differently
                    if (request.Table == LayerNames.Roughness || request.Table == LayerNames.Rutting)
                    {
                        await UpdateMultiGeoJsonWithOffsetAsync(filteredQuery, offset, false);
                    }
                    else if(request.Table == "PCIDefects") //not IEntity
                    {
                        var list = new List<TargetedGPSProperties>();

                        foreach (var item in filteredQuery)
                        {
                            var idProp = item.GetType().GetProperty("Id");
                            var geoJsonProp = item.GetType().GetProperty("GeoJSON");
                            if (idProp != null && geoJsonProp != null)
                            {
                                var id = (int)idProp.GetValue(item);
                                var geoJson = (string)geoJsonProp.GetValue(item);

                                list.Add(new TargetedGPSProperties
                                {
                                    Id = id,
                                    GeoJSON = geoJson,
                                    GPSLatitude = null,
                                    GPSLongitude = null,
                                    RoundedGPSLatitude = null,
                                    RoundedGPSLongitude = null
                                });
                            }
                        }

                        queries.AddRange(UpdateCoordinatesWithOffset(list, offset, request.Table, false));
                    }
                    else
                    {
                        queries.AddRange(await AddQueryForOffsetAsync(filteredQuery, dbName, offset));
                    }
                }
                if (queries.Any())
                {
                    await ExecuteQueryInDb(queries);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UpdateDefectsOnly: {ex.Message}");
            }
        }

        private IQueryable GetFilteredQueryWithId(int id, string dbName)
        {
            if (string.IsNullOrEmpty(dbName))
            {
                throw new ArgumentException($"Table name '{dbName}' not found in mappings.");
            }

            var dbSetProperty = _context.GetType().GetProperty(dbName);

            if (dbSetProperty == null)
            {
                throw new InvalidOperationException($"No DbSet found for '{dbName}' in the DbContext.");
            }

            var dbSet = dbSetProperty.GetValue(_context);

            // Use LINQ dynamically
            var whereMethod = typeof(Queryable).GetMethods()
                .First(m => m.Name == "Where" && m.GetParameters().Length == 2)
                .MakeGenericMethod(dbSetProperty.PropertyType.GenericTypeArguments[0]);

            // Build the predicate
            var parameter = Expression.Parameter(dbSetProperty.PropertyType.GenericTypeArguments[0], "x");
            var predicate = Expression.Lambda(
                Expression.Equal(
                    Expression.Property(parameter, "Id"),
                    Expression.Constant(id)
                ),
                parameter
            );

            // Apply the Where clause
            var filteredQuery = whereMethod.Invoke(null, new object[] { dbSet, predicate }) as IQueryable;

            return filteredQuery ?? throw new InvalidOperationException("Failed to create filtered query.");
        }

        private List<string> UpdateCoordinatesWithOffset(List<TargetedGPSProperties> lcmsObj, double[] offset, string tableName, bool useTrackAngle = false)
        {
            List<string> updateQueries = new List<string>();
            try
            {
                if (lcmsObj != null && lcmsObj.Count > 0)
                {
                    lcmsObj.ForEach(existingEntity =>
                    {
                        bool gpsCoordinateNotNull = false;
                        if (existingEntity.GPSLatitude != null && existingEntity.GPSLongitude != null && existingEntity.RoundedGPSLatitude != null && existingEntity.RoundedGPSLongitude != null)
                        {
                            gpsCoordinateNotNull = true;
                        }

                        // Parse the GeoJSON string
                        dynamic geoJson = JsonConvert.DeserializeObject(existingEntity.GeoJSON);
                        // Check if it's a Polygon or Polyline
                        string geometryType = geoJson.geometry.type;
                        JArray coordinates = geoJson.geometry.coordinates;
                        JToken firstCoordinate;

                        // Apply offset to each coordinate
                        if (geometryType == "Polygon")
                        {
                            JArray polygonPoints = coordinates[0] as JArray;
                            foreach (JArray point in polygonPoints)
                            {
                                if (useTrackAngle)
                                {
                                    var trackAngle = existingEntity.TrackAngle;
                                    var newCoordinate = GeneralHelper.ConvertToGPSCoordinates(offset[0], offset[1], (double)point[0], (double)point[1], trackAngle);
                                    point[0] = newCoordinate[0];
                                    point[1] = newCoordinate[1];
                                }
                                else
                                {
                                    double longitude = (double)point[0] + offset[0];
                                    double latitude = (double)point[1] + offset[1];
                                    point[0] = longitude;
                                    point[1] = latitude;
                                }
                            }
                            
                            if (gpsCoordinateNotNull)
                            {
                                firstCoordinate = polygonPoints.FirstOrDefault();
                                var gpsLatitude = firstCoordinate != null ? firstCoordinate[1] : 0.0;
                                var gpsLongitude = firstCoordinate != null ? firstCoordinate[0] : 0.0;
                                existingEntity.GPSLatitude = (double)gpsLatitude;
                                existingEntity.GPSLongitude = (double)gpsLongitude;
                                existingEntity.RoundedGPSLatitude = Math.Round((double)gpsLatitude, 4);
                                existingEntity.RoundedGPSLongitude = Math.Round((double)gpsLongitude, 4);
                            }
                        }
                        else if (geometryType == "Polyline")
                        {
                            foreach (JArray point in coordinates)
                            {
                                if (useTrackAngle)
                                {
                                    var trackAngle = existingEntity.TrackAngle;
                                    var newCoordinate = GeneralHelper.ConvertToGPSCoordinates(offset[0], offset[1], (double)point[0], (double)point[1], trackAngle);
                                    point[0] = newCoordinate[0];
                                    point[1] = newCoordinate[1];
                                }
                                else
                                {
                                    double longitude = (double)point[0] + offset[0];
                                    double latitude = (double)point[1] + offset[1];
                                    point[0] = longitude;
                                    point[1] = latitude;
                                }
                            }

                            if (gpsCoordinateNotNull)
                            {
                                firstCoordinate = coordinates.FirstOrDefault();
                                var gpsLatitude = firstCoordinate != null ? firstCoordinate[1] : 0.0;
                                var gpsLongitude = firstCoordinate != null ? firstCoordinate[0] : 0.0;

                                existingEntity.GPSLatitude = (double)gpsLatitude;
                                existingEntity.GPSLongitude = (double)gpsLongitude;
                                existingEntity.RoundedGPSLatitude = Math.Round((double)gpsLatitude, 4);
                                existingEntity.RoundedGPSLongitude = Math.Round((double)gpsLongitude, 4);
                            }
                        }
                        else if (geometryType == "Point")
                        {
                            if (useTrackAngle)
                            {
                                var trackAngle = existingEntity.TrackAngle;
                                var newCoordinate = GeneralHelper.ConvertToGPSCoordinates(offset[0], offset[1], (double)coordinates[0], (double)coordinates[1], trackAngle);
                                coordinates[0] = newCoordinate[0];
                                coordinates[1] = newCoordinate[1];

                                if (gpsCoordinateNotNull)
                                {
                                    existingEntity.GPSLatitude = newCoordinate[1];
                                    existingEntity.GPSLongitude = newCoordinate[0];
                                    existingEntity.RoundedGPSLatitude = Math.Round(newCoordinate[1], 4);
                                    existingEntity.RoundedGPSLongitude = Math.Round(newCoordinate[0], 4);
                                }
                            }
                            else
                            {
                                double longitude = (double)coordinates[0] + offset[0];
                                double latitude = (double)coordinates[1] + offset[1];
                                coordinates[0] = longitude;
                                coordinates[1] = latitude;

                                if (gpsCoordinateNotNull)
                                {
                                    existingEntity.GPSLatitude = latitude;
                                    existingEntity.GPSLongitude = longitude;
                                    existingEntity.RoundedGPSLatitude = Math.Round(latitude, 4);
                                    existingEntity.RoundedGPSLongitude = Math.Round(longitude, 4);
                                }
                            }
                        }
                        else if (geometryType == "MultiPolygon")
                        {
                            foreach (JArray polygon in coordinates)
                            {
                                foreach (JArray point in polygon)
                                {
                                    if (useTrackAngle)
                                    {
                                        var trackAngle = existingEntity.TrackAngle;
                                        var newCoordinate = GeneralHelper.ConvertToGPSCoordinates(offset[0], offset[1], (double)point[0], (double)point[1], trackAngle);
                                        point[0] = newCoordinate[0];
                                        point[1] = newCoordinate[1];
                                    }
                                    else
                                    {
                                        double longitude = (double)point[0] + offset[0];
                                        double latitude = (double)point[1] + offset[1];
                                        point[0] = longitude;
                                        point[1] = latitude;
                                    }
                                }
                            }
                            if (gpsCoordinateNotNull)
                            {
                                var firstPolygon = coordinates.FirstOrDefault() as JArray;
                                firstCoordinate = firstPolygon?[0] as JArray;
                                var gpsLatitude = firstCoordinate != null ? firstCoordinate[1] : 0.0;
                                var gpsLongitude = firstCoordinate != null ? firstCoordinate[0] : 0.0;

                                // Update table values with the modified first coordinate
                                existingEntity.GPSLatitude = (double)gpsLatitude;
                                existingEntity.GPSLongitude = (double)gpsLongitude;
                                existingEntity.RoundedGPSLatitude = Math.Round((double)gpsLatitude, 4);
                                existingEntity.RoundedGPSLongitude = Math.Round((double)gpsLongitude, 4);
                            }
                        }

                        existingEntity.GeoJSON = JsonConvert.SerializeObject(geoJson);

                        if(gpsCoordinateNotNull)
                        {
                            updateQueries.Add($"UPDATE {tableName} SET GPSLatitude={existingEntity.GPSLatitude}, GPSLongitude={existingEntity.GPSLongitude}, GeoJSON='{existingEntity.GeoJSON}', RoundedGPSLatitude={existingEntity.RoundedGPSLatitude}, RoundedGPSLongitude={existingEntity.RoundedGPSLongitude} WHERE Id={existingEntity.Id};");
                        }
                        else
                        {
                            updateQueries.Add($"UPDATE {tableName} SET GeoJSON='{existingEntity.GeoJSON}' WHERE Id={existingEntity.Id};");
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UpdateCoordinatesWithOffset : {ex.Message}");
            }
            return updateQueries;
        }


        private class TargetedGPSProperties
        {
            public int Id { get; set; }
            public double? GPSLatitude { get; set; }
            public double? GPSLongitude { get; set; }
            public double? RoundedGPSLatitude { get; set; }
            public double? RoundedGPSLongitude { get; set; }
            public string GeoJSON { get; set; }
            public double TrackAngle { get; set; } = 0.00; //default value
        }

        private async Task ExecuteQueryInDb(List<string> queries)
        {
            try
            {
                int queryExecuted = 0;

                if (String.IsNullOrEmpty(databasePath))
                {
                    var databasePathProvider = _serviceProvider.GetRequiredService<DatabasePathProvider>();
                    string actualDatabasePath = databasePathProvider.GetDatasetDatabasePath();
                    databasePath = actualDatabasePath;
                }

                //separate the queries for LasFile cascade delete, txn doesn't delete
                List<string> queriesForeign = new List<string>();
                Log.Warning($" === Checking queries for LAS === main queries : {queries.Count}");
                if (queries.Any(q => q.Contains("LASFile") || q.Contains("LASPoint")))
                {
                    queriesForeign = queries.FindAll(q => q.Contains("LASFile") || q.Contains("LASPoint"));
                    queries.RemoveAll(q => q.Contains("LASFile") || q.Contains("LASPoint"));
                    Log.Warning($"LAS : {queriesForeign.Count} ");
                }
                Log.Warning($" Final main queries : {queries.Count}");

                using (SQLiteConnection conn = new SQLiteConnection($"Data Source = {databasePath};"))
                {
                    SQLiteLog.RemoveDefaultHandler();
                    conn.Open();

                    if (queries.Count > 0)
                    {
                        using (var transaction = conn.BeginTransaction())
                        {
                            using (var command = new SQLiteCommand(conn))
                            {
                                command.Transaction = transaction;

                                foreach (string q in queries)
                                {
                                    try
                                    {
                                        if (q != null && !string.IsNullOrEmpty(q))
                                        {
                                            command.CommandText = q;
                                            command.ExecuteNonQuery();

                                            queryExecuted++;
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Error($"Error in ExecuteQueryInDb : {ex.Message}");
                                    }
                                }
                            }
                            transaction.Commit();
                        }
                    }

                    if (queriesForeign.Count > 0)
                    {
                        using (var command = new SQLiteCommand(conn))
                        {
                            foreach (string q in queriesForeign)
                            {
                                try
                                {
                                    if (q != null && !string.IsNullOrEmpty(q))
                                    {
                                        command.CommandText = q;
                                        command.ExecuteNonQuery();

                                        queryExecuted++;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Log.Error($"Error while deleting LASFiles : {q}, {ex.Message}");
                                }
                            }
                        }
                    }

                    conn.Close();
                }

                Log.Information($"Total : {queries.Count + queriesForeign.Count}, Query executed successfully : {queryExecuted}");
            }
            catch (Exception ex)
            {
                Log.Error($"Error in executing query : {ex.Message}");
            }
        }

        public Task<List<Dictionary<string, object>>> ExecuteQueryAndReturnRows(string query)
        {
            var results = new List<Dictionary<string, object>>();

            try
            {
                Log.Information("Execute Query And Return IDs");

                if (string.IsNullOrEmpty(databasePath))
                {
                    var databasePathProvider = _serviceProvider.GetRequiredService<DatabasePathProvider>();
                    string actualDatabasePath = databasePathProvider.GetDatasetDatabasePath();
                    databasePath = actualDatabasePath;
                }

                using (SQLiteConnection conn = new SQLiteConnection($"Data Source = {databasePath};"))
                {
                    conn.Open();

                    using (var command = new SQLiteCommand(query, conn))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var row = new Dictionary<string, object>();

                                // Loop through all columns in the result set
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                                }

                                results.Add(row);
                            }
                        }
                    }

                    conn.Close();
                }

                Log.Information($"Query executed successfully, Results Retrived: {results.Count}");
            }
            catch (Exception ex)
            {
                Log.Error($"Error in executing query: {ex.Message}");
                return null;
            }

            return Task.FromResult(results);
        }

        public async Task<List<QueryResponse>> ExecuteQueryAndReturnGeoJSON(string query)
        {
            var geoJsons = new List<QueryResponse>();

            try
            {
                var response = await ExecuteQueryAndReturnRows(query);
                if (response == null) return null;

                foreach (var row in response)
                {
                    if (row.ContainsKey("GeoJSON") && row["GeoJSON"] != DBNull.Value &&
                        row.ContainsKey("Id") && row["Id"] != DBNull.Value)
                    {
                        string geoJson = row["GeoJSON"].ToString();
                        int id = Convert.ToInt32(row["Id"]);
                        string surveyId = row.ContainsKey("SurveyId") && row["SurveyId"] != DBNull.Value
                            ? row["SurveyId"].ToString()
                            : null;
                        int segmentId = row.ContainsKey("SegmentId") && row["SegmentId"] != DBNull.Value
                            ? Convert.ToInt32(row["SegmentId"])
                            : 0;

                        if (!string.IsNullOrEmpty(geoJson))
                        {
                            geoJsons.Add(new QueryResponse
                            {
                                Id = id,
                                GeoJSON = geoJson,
                                SurveyId = surveyId,
                                SegmentId = segmentId
                            });
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("Error in ExecuteQueryAndReturnGeoJSON" + ex.Message);
            }

            return geoJsons;
        }
        public async Task<List<int>> ExecuteQueryAndReturnIds(string query)
        {
            var ids = new List<int>();

            try
            {
                var response = await ExecuteQueryAndReturnRows(query);
                if (response == null) return null;

                foreach (var row in response)
                {
                    // Extract the ID from the row
                    if (row.ContainsKey("Id") && row["Id"] != DBNull.Value)
                    {
                        int id = Convert.ToInt32(row["Id"]);
                        ids.Add(id);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in ExecuteQueryAndReturnIds" + ex.Message);
            }
            return ids;
        }

        public async Task<IdReply> DeleteDefectsWithQueryAsync(List<DeletionInfo> deletionInfos)
        {
            try
            {
                var deleteQueries = new List<string>();
                if (deletionInfos != null && deletionInfos.Any(d => d.Table == "LASFile"))
                {
                    //add query to enable foreign keys
                    deleteQueries.Add("PRAGMA foreign_keys = ON;");
                }

                string idInfo = string.Empty;
                foreach (var info in deletionInfos)
                {
                    if (info.Table == LayerNames.Segment)
                    {
                        // Delete Segment
                        if (string.IsNullOrEmpty(info.Id) && string.IsNullOrEmpty(info.Table)) continue;

                        var tableName = TableNameHelper.GetDBTableName(info.Table);
                        deleteQueries.Add($"DELETE FROM {tableName} WHERE Id IN ({info.Id});");

                        var affectedSurveyIds = new HashSet<string>();

                        // Split the concatenated Id string
                        var idArray = info.Id.Split(',');
                        foreach (var idStr in idArray)
                        {
                            if (int.TryParse(idStr, out int id))
                            {
                                var segmentInfo = await _repository.GetByIdAsync(id);

                                var surveyId = segmentInfo.SurveyId;
                                if (!affectedSurveyIds.Contains(surveyId))
                                    affectedSurveyIds.Add(surveyId);
                                var segmentId = segmentInfo.SegmentId;

                                var tablesToDeleteFrom = TableNameHelper.GetAllLCMSTables();

                                foreach (var tableNameToDelete in tablesToDeleteFrom)
                                {
                                    var dbTableName = TableNameHelper.GetDBTableName(tableNameToDelete);
                                    deleteQueries.Add($"DELETE FROM {dbTableName} WHERE SurveyId = '{surveyId}' AND SegmentId = {segmentId};");
                                }
                            }
                        }

                        foreach (var surveyId in affectedSurveyIds)
                        {
                            var allsegments = await GetBySurvey(new SurveyRequest { SurveyId = surveyId });

                            if (allsegments.Any())
                            {
                                var firstSegment = allsegments.OrderBy(x => x.Chainage).First();
                                var lastSegment = allsegments.OrderByDescending(x => x.ChainageEnd).First();

                                bool deletingFirst = idArray.Contains(firstSegment.Id.ToString()); 
                                bool deletingLast = idArray.Contains(lastSegment.Id.ToString());

                                // If deleting first or last segment, update survey chainages 
                                if (deletingFirst || deletingLast)
                                {
                                    // Remaining segments after deletion
                                    var remainingSegments = allsegments.Where(s => !idArray.Contains(s.Id.ToString())).ToList();

                                    if (remainingSegments.Any())
                                    {
                                        var newFirst = remainingSegments.OrderBy(s => s.Chainage).First(); 
                                        var newLast = remainingSegments.OrderByDescending(s => s.ChainageEnd).First(); 
                                         
                                        deleteQueries.Add($"UPDATE Survey SET StartChainage = {newFirst.Chainage}, EndChainage = {newLast.ChainageEnd} WHERE SurveyIdExternal = {surveyId};");
                                    }
                                    else
                                    {
                                        deleteQueries.Add($"UPDATE Survey SET StartChainage = NULL, EndChainage = NULL WHERE Id = {surveyId};");
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(info.Id) && !string.IsNullOrEmpty(info.Table))
                        {
                            var tableName = TableNameHelper.GetDBTableName(info.Table);
                            deleteQueries.Add($"DELETE FROM {tableName} WHERE Id IN ({info.Id});");

                            if(tableName == "LASFile")
                            {
                                deleteQueries.Add($"DELETE FROM LASPoint WHERE LASfileId IN ({info.Id});");
                            }
                        }
                    }
                }
                await ExecuteQueryInDb(deleteQueries);
                return new IdReply { Id = 1, Message = "Successfully deleted" };
            }
            catch (Exception ex)
            {
                Log.Error($"Error in DeleteDefectsWithQueryAsync :{ex.Message}");
                return new IdReply { Id = 0, Message = "Failed to delete" };
            }
        }

        public async Task<IdReply> UpdateOffsetBySurvey(OffsetData offsetData)
        {
            var surveyExternalIds = offsetData.SurveyIds;
            var defects = offsetData.Defects;
            var horizontalOffset = offsetData.HorizontalOffset;
            var verticalOffset = offsetData.VerticalOffset;

            List<string> queries = new List<string>();

            foreach (var defect in defects)
            {
                var dbName = TableNameHelper.TableNameMappings
                    .FirstOrDefault(mapping => mapping.LayerName == defect)
                    .DBName;

                if (dbName != null)
                {
                    var dbSetProperty = _context.GetType().GetProperty(dbName);
                    if (dbSetProperty != null)
                    {
                        var dbSet = dbSetProperty.GetValue(_context);

                        // Use LINQ dynamically
                        var whereMethod = typeof(Queryable).GetMethods()
                            .First(m => m.Name == "Where" && m.GetParameters().Length == 2)
                            .MakeGenericMethod(dbSetProperty.PropertyType.GenericTypeArguments[0]);

                        // Build the predicate
                        var parameter = Expression.Parameter(dbSetProperty.PropertyType.GenericTypeArguments[0], "x");
                        var surveyIdChecks = surveyExternalIds.Select(id =>
                           Expression.Equal(
                               Expression.Property(parameter, "SurveyId"),
                               Expression.Constant(id)
                           )
                       );
                        var surveyIdPredicate = surveyIdChecks.Aggregate(Expression.OrElse);

                        var predicate = Expression.Lambda(
                           Expression.AndAlso(
                               surveyIdPredicate,
                               Expression.NotEqual(
                                   Expression.Property(parameter, "SegmentId"),
                                   Expression.Constant(-1)
                               )
                           ),
                           parameter
                       );


                        // Apply the Where clause
                        var filteredQuery = whereMethod.Invoke(null, new object[] { dbSet, predicate }) as IQueryable;

                        var offset = new double[] { horizontalOffset, verticalOffset };
                        // Treat multi geojson differently
                        if (defect == LayerNames.Roughness || defect == LayerNames.Rutting)
                        {
                            await UpdateMultiGeoJsonWithOffsetAsync(filteredQuery, offset, true);
                        }
                        else
                        {
                            // Pass the query to AddDefectQueryAsync
                            queries.AddRange(await AddQueryForTrackAngleOffsetAsync(filteredQuery, dbName, offset));
                        }
                    }
                }
            }

            var videoFrames = new List<VideoFrame>();
            var camera360Frames = new List<Camera360Frame>();

            var cameraNames = await _videoFrameService.HasData(new Empty());
            foreach (var surveyExternalId in surveyExternalIds)
            {
                var surveyEntity = await _surveyService.GetSurveyEntityByExternalId(surveyExternalId);

                //Normal Video 
                foreach (var camera in cameraNames)
                {
                    if (defects.Contains(camera))
                    {
                        var videos = _context.VideoFrame.Where(x => x.SurveyId == surveyEntity.Id && x.CameraName == camera).ToList();
                        if (videos != null && videos.Count > 0)
                        {
                            videoFrames.AddRange(videos);
                        }
                    }
                }

                //360 Camera Video
                if (defects.Contains("360 Video"))
                {
                    var camera360 = _context.Camera360Frame.Where(x => x.SurveyId == surveyEntity.Id).ToList();
                    if (camera360 != null && camera360.Count > 0)
                    {
                        camera360Frames.AddRange(camera360);
                    }
                }
            }

            if (videoFrames.Count > 0)
            {
                await UpdateVideos(videoFrames, horizontalOffset, verticalOffset, _context.VideoFrame);
            }

            if (camera360Frames.Count > 0)
            {
                await UpdateVideos(camera360Frames, horizontalOffset, verticalOffset, _context.Camera360Frame);
            }

            if (queries.Any())
            {
                await ExecuteQueryInDb(queries);
                return new IdReply { Id = 1, Message = "Successfully Updated" };
            }

            return new IdReply { Id = 0, Message = "Nothing to Update" };
        }

        private async Task<List<string>> AddQueryForTrackAngleOffsetAsync(IQueryable filteredQuery, string tableName, double[] offset)
        {
            var entityQuery = filteredQuery.Cast<IEntity>();

            var defects = entityQuery.ToList();
            if (defects == null || !defects.Any())
                return new List<string>();

            return UpdateCoordinatesWithOffset(defects.Select(x => new TargetedGPSProperties
            {
                Id = x.Id,
                GPSLatitude = x.GPSLatitude,
                GPSLongitude = x.GPSLongitude,
                RoundedGPSLatitude = x.RoundedGPSLatitude,
                RoundedGPSLongitude = x.RoundedGPSLongitude,
                GeoJSON = x.GeoJSON,
                TrackAngle = x.GPSTrackAngle,
            }).ToList(), offset, tableName, true);

        }

        private async Task UpdateVideos<T>(List<T> frames, double horizontalOffset, double verticalOffset, DbSet<T> dbSet) where T : class
        {
            foreach (var frame in frames)
            {
                dynamic dynamicFrame = frame; // Using dynamic since both types have similar properties
                var trackAngle = dynamicFrame.GPSTrackAngle;

                if (dynamicFrame.GeoJSON != null)
                {
                    dynamic geoJson = JsonConvert.DeserializeObject(dynamicFrame.GeoJSON);
                    JArray coordinates = geoJson.geometry.coordinates;

                    var newCoordinate = GeneralHelper.ConvertToGPSCoordinates(horizontalOffset, verticalOffset, (double)coordinates[0], (double)coordinates[1], trackAngle);
                    coordinates[0] = newCoordinate[0];
                    coordinates[1] = newCoordinate[1];
                    dynamicFrame.GPSLatitude = newCoordinate[1];
                    dynamicFrame.GPSLongitude = newCoordinate[0];
                    dynamicFrame.GeoJSON = JsonConvert.SerializeObject(geoJson);
                }

                dbSet.Update(dynamicFrame); // Update based on the provided DbSet
            }

            await _context.SaveChangesAsync();
        }

        //update LCMS_Segments and Survey fo the image paths
        public async Task<IdReply> UpdateImagePathInSegmentAndSurvey(List<string> imageFilePaths)
        {
            List<string> queries = new List<string>();

            string[] splittedFilename = Path.GetFileNameWithoutExtension(imageFilePaths[0]).Split("_");
            string filePathDirectory = Path.GetDirectoryName(imageFilePaths[0]);
            string surveyName = string.Join(" ", splittedFilename.Take(splittedFilename.Length - 1));


            var survey = _context.Survey.Where(x => x.SurveyName == surveyName).FirstOrDefault();

            if (survey == null)
            {
                return new IdReply { Id = 0, Message = "Survey not found" };
            }

            string surveyId = survey.SurveyIdExternal;
            foreach (var imageFilePath in imageFilePaths)
            {
                long SectionId = Convert.ToInt64(Path.GetFileNameWithoutExtension(imageFilePath).Split("_").Last());
                string strquery = $"UPDATE LCMS_Segment SET ImageFilePath='{Path.GetFileName(imageFilePath)}' WHERE SurveyId='{surveyId}' and SectionId='{SectionId}';";
                queries.Add(strquery);
                Log.Information("StrQuery:" + strquery);
            }

            queries.Add($"UPDATE Survey SET ImageFolderPath='{filePathDirectory}' WHERE SurveyIdExternal='{surveyId}';");

            if (queries.Any())
            {
                await ExecuteQueryInDb(queries);
                return new IdReply { Id = 1, Message = "Successfully Updated" };
            }

            return new IdReply { Id = 0, Message = "Nothing to Update" };
        }

        public async Task<IdReply> SaveUserDefinedDefect(List<KeyValueField> defectFields, CallContext context = default)
        {
            try
            {
                var dictionary = new Dictionary<string, string>();
                string tableName = string.Empty;

                foreach (var field in defectFields)
                {
                    if (field.Key == "Table")
                    {
                        tableName = field.Value;
                        continue;
                    }
                    string value = FormatValue(field.Value, field.Type);
                    dictionary[field.Key] = value;
                }

                if (!string.IsNullOrEmpty(tableName))
                {
                    var dbTable = TableNameHelper.GetDBTableName(tableName);

                    string columns = string.Join(", ", dictionary.Keys);
                    string values = string.Join(", ", dictionary.Values);
                    string sqlCommand = $"INSERT INTO {dbTable} ({columns}) VALUES ({values})";

                    // Retrieve the last inserted ID
                    int lastInsertedId = 0;
                    if (InsertDataAndGetId(sqlCommand, out lastInsertedId))
                    {
                        return new IdReply { Id = lastInsertedId, Message = $"{tableName} successfully saved." };
                    }
                }

                return new IdReply { Id = -1, Message = "" };
            }
            catch (Exception ex)
            {
                Log.Error($"Error occurred while saving a new defect: {ex.Message}");
                return new IdReply { Id = -1, Message = $"Error occurred while saving a new defect: {ex.Message}" };
            }

        }

        private bool InsertDataAndGetId(string sqlCommand, out int lastInsertedId)
        {
            lastInsertedId = 0;
            try
            {
                if (String.IsNullOrEmpty(databasePath))
                {
                    var databasePathProvider = _serviceProvider.GetRequiredService<DatabasePathProvider>();
                    string actualDatabasePath = databasePathProvider.GetDatasetDatabasePath();
                    databasePath = actualDatabasePath;
                }

                using (SQLiteConnection conn = new SQLiteConnection($"Data Source={databasePath};"))
                {
                    conn.Open();
                    using (var transaction = conn.BeginTransaction())
                    {
                        using (var command = new SQLiteCommand(conn))
                        {
                            command.Transaction = transaction;
                            command.CommandText = sqlCommand;
                            command.ExecuteNonQuery();

                            // Retrieve the last inserted ID
                            command.CommandText = "SELECT LAST_INSERT_ROWID();";
                            var result = command.ExecuteScalar();
                            lastInsertedId = Convert.ToInt32(result);

                            transaction.Commit();
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"Error retrieving last insert row ID: {ex.Message}");
                return false;
            }
        }

        private string FormatValue(string value, string type)
        {
            if (type == "String")
            {
                return $"'{value}'"; // Wrap strings in single quotes
            }
            else if (type == "Number")
            {
                return Convert.ToString(value, CultureInfo.InvariantCulture); // Convert numbers to string using invariant culture
            }
            else if (type == "DateTime")
            {
                return $"'{value}'";
            }
            else
            {
                throw new InvalidOperationException("Unsupported data type");
            }
        }

        public async Task<IdReply> ExecuteSQlQueries(List<string> queries)
        {
            try
            {
                if (queries.Count > 0)
                {
                    await ExecuteQueryInDb(queries);
                }
                return new IdReply { Id = 0, Message = "query executed" };
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                return new IdReply { Id = -1, Message = "error in Executing queries" };
            }
        }

        public async Task<List<PCIDefects>> CalculateAutofillPCIDefects(PCIAutofillDefectRequest request)
        {
            var pciDefects = new List<PCIDefects>();
            try
            {
                var coordinateString = request.Coordinates;

                var boundaryCoordinates = System.Text.Json.JsonSerializer.Deserialize<List<double[]>>(coordinateString);

                // Convert boundary coordinates to Esri Polygon
                var boundaryPolygon = new Polygon(
                    new Esri.ArcGISRuntime.Geometry.PointCollection(
                        boundaryCoordinates.Select(coord =>
                            new MapPoint(coord[0], coord[1], SpatialReferences.Wgs84))
                    )
                );

                var tasks = new List<Task>();

                if(request.PavementType == "Asphalt")
                {
                    //// 1. Alligator Cracking (Fatigue) by severity
                    //var fatigueCracks = _context.LCMS_Segment_Grid
                    //        .Where(x => x.CrackType == "Alligator" && x.Severity != "None")
                    //        .AsParallel() // Enables parallel processing
                    //        .ToLookup(x => x.Severity);
                    //foreach (var grouped in fatigueCracks)
                    //{
                    //    tasks.Add(Task.Run(async () =>
                    //    {
                    //        var severity = grouped.Key;

                    //        var areas = await Task.WhenAll(grouped.Select(async fatigue =>
                    //        {
                    //            var geometries = await GetGeometryFromGeoJSON(fatigue.GeoJSON);
                    //            var containedGeometries = geometries.Where(g => GeometryEngine.Contains(boundaryPolygon, g));
                    //            var totalArea = containedGeometries.Sum(g => g.AreaGeodetic(AreaUnits.SquareMeters));
                    //            return totalArea;
                    //        }));

                    //        var combinedArea = areas.Sum();

                    //        if (combinedArea > 0)
                    //        {
                    //            //PAVER software does not take "Very High"
                    //            if (severity == "Very High")
                    //            {
                    //                severity = "High";
                    //            }
                    //            lock (pciDefects)
                    //            {
                    //                pciDefects.Add(new PCIDefects
                    //                {
                    //                    DefectName = "Alligator cracking (Fatigue)",
                    //                    Qty = Math.Round(combinedArea, 2),
                    //                    Severity = severity
                    //                });
                    //            }
                    //        }
                    //    }));
                    //}

                    // 2. Bleeding (all areas) 
                    var bleeding = _context.LCMS_Bleeding
                                .Where(x => x.LeftSeverity != "No Bleeding" || x.RightSeverity != "No Bleeding")
                                .AsParallel()
                                .ToList();

                    if (bleeding.Any())
                    {
                        double combinedArea = 0.0;
                        await Parallel.ForEachAsync(bleeding, async (item, _) =>
                        {
                            var geometries = await GetGeometryFromGeoJSON(item.GeoJSON);
                            if (geometries.Count >= 2)
                            {
                                var leftGeometry = geometries[0];
                                if (GeometryEngine.Contains(boundaryPolygon, leftGeometry) && item.LeftSeverity != "No Bleeding")
                                {
                                    var area = leftGeometry.AreaGeodetic(AreaUnits.SquareMeters);
                                    combinedArea += area;
                                }

                                var rightGeometry = geometries[1];
                                if (GeometryEngine.Contains(boundaryPolygon, rightGeometry) && item.RightSeverity != "No Bleeding")
                                {
                                    var area = rightGeometry.AreaGeodetic(AreaUnits.SquareMeters);
                                    combinedArea += area;
                                }
                            }
                        });

                        if (combinedArea > 0)
                        {
                            var defect = new PCIDefects
                            {
                                DefectName = "Bleeding",
                                Qty = Math.Round(combinedArea, 2),
                                Severity = "N/A"
                            };

                            pciDefects.Add(defect);
                        }
                    }

                    ////3. Joint Reflection Cracking (length) or Longitudinal Transverse Cracking by severity
                    //var LTCracking = _context.LCMS_Segment_Grid
                    //      .Where(x => x.CrackType == "Longitudinal" || x.CrackType == "Transversal")
                    //      .Where(x => x.Severity != "None")
                    //      .AsParallel() // Enables parallel processing
                    //      .ToLookup(x => x.Severity);
                    //foreach (var grouped in LTCracking)
                    //{
                    //    string defectName = string.Empty;
                    //    if(request.IsPCCPavement)
                    //    {
                    //        defectName = "Joint Reflection Cracking";
                    //    }
                    //    else
                    //    {
                    //        defectName = "Longitudinal & Transverse Cracking";
                    //    }

                    //    tasks.Add(Task.Run(async () =>
                    //    {
                    //        var severity = grouped.Key;

                    //        var totalCount = await Task.WhenAll(grouped.Select(async lt =>
                    //        {
                    //            var geometries = await GetGeometryFromGeoJSON(lt.GeoJSON);
                    //            return geometries.Count(g => GeometryEngine.Contains(boundaryPolygon, g));
                    //        }));

                    //        var totalLength = totalCount.Sum() * 0.25; // Multiply by 0.25

                    //        if (totalLength > 0)
                    //        {
                    //            //PAVER software does not take "Very High"
                    //            if (severity == "Very High")
                    //            {
                    //                severity = "High";
                    //            }
                    //            lock (pciDefects)
                    //            {
                    //                pciDefects.Add(new PCIDefects
                    //                {
                    //                    DefectName = defectName,
                    //                    Qty = Math.Round(totalLength, 2),
                    //                    Severity = severity
                    //                });
                    //            }
                    //        }
                    //    }));
                    //}

                    //4. Ravelling (area) by severity
                    var groupedRavelling = _context.LCMS_Ravelling_Raw
                       .AsParallel()
                       .GroupBy(x => x.Severity);

                    foreach (var grouped in groupedRavelling)
                    {
                        tasks.Add(Task.Run(async () =>
                        {
                            var severity = grouped.Key;

                            var areas = await Task.WhenAll(grouped.Select(async ravelling =>
                            {
                                var geometries = await GetGeometryFromGeoJSON(ravelling.GeoJSON);
                                var containedGeometries = geometries.Where(g => GeometryEngine.Contains(boundaryPolygon, g));
                                var totalArea = containedGeometries.Sum(g => g.AreaGeodetic(AreaUnits.SquareMeters));
                                return totalArea;
                            }));

                            var combinedArea = areas.Sum();

                            if (combinedArea > 0)
                            {
                                lock (pciDefects)
                                {
                                    pciDefects.Add(new PCIDefects
                                    {
                                        DefectName = "Ravelling",
                                        Qty = Math.Round(combinedArea, 2),
                                        Severity = severity
                                    });
                                }
                            }
                        }));
                    }
                }
                else if (request.PavementType == "Concrete")
                {
                    //1. Pickout
                    var pickouts = _context.LCMS_PickOuts_Raw.ToList();

                    var totalCount = await Task.WhenAll(pickouts.Select(async pickout =>
                    {
                        var geometries = await GetGeometryFromGeoJSON(pickout.GeoJSON);
                        return geometries.Count(g => GeometryEngine.Contains(boundaryPolygon, g));
                    }));

                    var boundaryArea = boundaryPolygon.AreaGeodetic(AreaUnits.SquareMeters);
                    var pickoutPerSquareMeter = totalCount.Sum() / boundaryArea;
                    if(pickoutPerSquareMeter >= 3)
                    {
                        lock (pciDefects)
                        {
                            pciDefects.Add(new PCIDefects
                            {
                                DefectName = "Popouts (Pickouts)",
                                Qty = 1,
                                Severity = "N/A"
                            });
                        }
                    }
                }

                await Task.WhenAll(tasks);

            }
            catch (Exception ex)
            {
                Log.Error("Error in CalculateAutofillPCIDefects" + ex.Message);
            }

            return pciDefects;
        }
        public async Task<List<SummaryItem>> GetNumericValueWithinBoundary(SummaryRequest request)
        {
            var coordinateString = request.CoordinateString;
            var survey = request.SelectedSurvey;

            var boundaryCoordinates = System.Text.Json.JsonSerializer.Deserialize<List<double[]>>(coordinateString);

            // Convert boundary coordinates to Esri Polygon
            var boundaryPolygon = new Polygon(
                new Esri.ArcGISRuntime.Geometry.PointCollection(
                    boundaryCoordinates.Select(coord =>
                        new MapPoint(coord[0], coord[1], SpatialReferences.Wgs84))
                )
            );

            var updatedSummaryItems = new List<SummaryItem>();

            foreach (var summaryItem in request.SummaryItems)
            {
                var numericValues = new List<double>();
                var tableName = TableNameHelper.GetDBTableName(summaryItem.TableName);
                var field = summaryItem.NumericField;

                string query = string.Empty;

                if (summaryItem.IsMetaTable)
                {
                    query = $"SELECT GeoJSON, {field} FROM MetaTableValue WHERE SurveyId = '{survey}' AND TableName = '{summaryItem.TableName}'";
                }
                else
                {
                    query = $"SELECT GeoJSON, {field} FROM {tableName} WHERE SurveyId = '{survey}'";
                }

                // Execute the query and retrieve the rows
                var rows = await ExecuteQueryAndReturnRows(query);

                foreach (var row in rows)
                {
                    var geoJson = row["GeoJSON"]?.ToString();
                    double? numericValue = null;
                    if (row[field] != null &&
                        row[field] != DBNull.Value &&
                        !string.IsNullOrWhiteSpace(row[field].ToString()))
                    {
                        numericValue = Convert.ToDouble(row[field]);
                    }

                    var tableGeometry = await GetGeometryFromGeoJSON(geoJson);

                    //these polygons are most likely not contained in the boundary polygon so need to compare it with the center point 
                    if (summaryItem.TableName == LayerNames.PCI || summaryItem.TableName == LayerNames.PASER ||
                        summaryItem.TableName == LayerNames.Segment || summaryItem.TableName == MultiLayerName.BandTexture)
                    {
                        var centerPoints = tableGeometry
                            .Where(geometry => geometry is Polygon)
                            .Select(geometry => GeometryEngine.LabelPoint((Polygon)geometry))
                            .ToList();
                        tableGeometry = new List<Geometry>(centerPoints);
                    }

                    // Check if graphics are inside the boundary
                    if (tableGeometry.Any())
                    {
                        var allGeometryContained = tableGeometry.All(geometry => GeometryEngine.Contains(boundaryPolygon, geometry));
                        if (allGeometryContained && numericValue != null)
                        {
                            numericValues.Add(numericValue.Value); // Add the numeric value if the geometry is within the boundary
                        }
                    }
                }

                double roundedResult = 0.0;
                if (numericValues.Any())
                {
                    double result = summaryItem.Operation switch
                    {
                        "SUM" => numericValues.Sum(),
                        "AVG" => numericValues.Average(),
                        "COUNT" => numericValues.Count(),
                        "MIN" => numericValues.Min(),
                        "MAX" => numericValues.Max(),
                        _ => 0.0
                    };
                    roundedResult = Math.Round(result, 2);
                }

                //If numericValues is empty, save the value as 0.0
                var newSummaryItem = new SummaryItem
                {
                    TableName = summaryItem.TableName,
                    NumericField = summaryItem.NumericField,
                    Operation = summaryItem.Operation,
                    NumericValue = roundedResult
                };
                updatedSummaryItems.Add(newSummaryItem);
            }

            return updatedSummaryItems;
        }

        public async Task<List<Geometry>> GetGeometryFromGeoJSON(string geoJson)
        {
            // Parse the GeoJSON
            var geoJsonObject = JObject.Parse(geoJson);
            var geometryType = geoJsonObject["geometry"]?["type"]?.ToString();
            var jsonCoordinates = geoJsonObject["geometry"]?["coordinates"];
            List<Geometry> tableGeometries = new List<Geometry>();
            if (jsonCoordinates != null && geometryType != null)
            {
                var spatialReference = SpatialReferences.Wgs84; // Ensure WGS84 reference

                // Handle geometry based on type
                switch (geometryType.ToLower())
                {
                    case "point":
                        var pointCoords = jsonCoordinates.ToObject<double[]>();
                        var pointGeometry = new MapPoint(pointCoords[0], pointCoords[1], spatialReference);
                        tableGeometries.Add(pointGeometry);
                        break;

                    case "polygon":
                        var rings = jsonCoordinates.ToObject<List<List<double[]>>>();
                        var polygonPoints = new Esri.ArcGISRuntime.Geometry.PointCollection(spatialReference);
                        foreach (var ring in rings)
                        {
                            foreach (var coord in ring)
                            {
                                polygonPoints.Add(new MapPoint(coord[0], coord[1], spatialReference));
                            }
                        }
                        var polygonGeometry = new Polygon(polygonPoints, spatialReference);
                        tableGeometries.Add(polygonGeometry);
                        break;

                    case "polyline":
                        var paths = jsonCoordinates.ToObject<List<double[]>>();
                        var polylinePoints = new Esri.ArcGISRuntime.Geometry.PointCollection(spatialReference);
                        foreach (var coord in paths)
                        {
                            polylinePoints.Add(new MapPoint(coord[0], coord[1], spatialReference));
                        }
                        var polylineGeometry = new Polyline(polylinePoints, spatialReference);
                        tableGeometries.Add(polylineGeometry);
                        break;

                    case "multipolygon":
                        var multiPolygonCoordinates = jsonCoordinates.ToObject<List<List<double[]>>>(); // List of multiple polygons
                        var polygons = new List<Polygon>(); // List to store individual polygons

                        foreach (var ring in multiPolygonCoordinates)
                        {
                            var multiPolygonPoints = new Esri.ArcGISRuntime.Geometry.PointCollection(spatialReference);

                            foreach (var coord in ring)
                            {
                                multiPolygonPoints.Add(new MapPoint(coord[0], coord[1], spatialReference));
                            }

                            // Create each individual Polygon from the points and add it to the list
                            var individualPolygon = new Polygon(multiPolygonPoints, spatialReference);
                            polygons.Add(individualPolygon);
                        }

                        tableGeometries.AddRange(polygons);
                        break;

                    default:
                        // Ignore unsupported geometry types
                        break;
                }
            }

            return tableGeometries;
        }

        public async Task<List<KeyValueField>> ExtractAttributes(IdTableRequest request, CallContext context = default)
        {
            var keyValues = new List<KeyValueField>();

            var dbName = TableNameHelper.TableNameMappings.FirstOrDefault(x => x.LayerName == request.Table).DBName;

            //get entity
            var filteredQuery = GetFilteredQueryWithId(request.Id, dbName);

            //get attributes
            if (filteredQuery != null)
            {
                foreach (var query in filteredQuery)
                {
                    var properties = query.GetType().GetProperties();

                    foreach (var property in properties)
                    {
                        if (property.Name.Contains("GPS") || property.Name.Contains("QC") || property.Name.Contains("GeoJSON") || property.Name.Contains("Image"))
                        {
                            continue;
                        }

                        var propertyName = property.Name;
                        var propertyValue = property.GetValue(query);

                        if (propertyValue != null && !string.IsNullOrEmpty(propertyValue.ToString()))
                        {
                            if (propertyName == "DeductedValues")
                            {
                                //Extract PCI deducted values from json format
                                var dict = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(propertyValue.ToString());
                                foreach (var kvp in dict)
                                {
                                    var key = kvp.Key + " (DV)";
                                    var kvf = new KeyValueField { Key = key, Value = kvp.Value.ToString() };
                                    keyValues.Add(kvf);
                                }
                            }
                            else
                            {
                                var kvp = new KeyValueField { Key = propertyName, Value = propertyValue.ToString() };
                                keyValues.Add(kvp);
                            }
                        }

                    }
                }
            }
            return keyValues;
        }

        public async Task<string> GetDynamicDataAsync(string tableNameWithSurveyId)
        {
            try
            {
                string surveyIdValue = "All", tableName= tableNameWithSurveyId;
                if (tableNameWithSurveyId.Contains(','))
                {
                    tableName = tableNameWithSurveyId.Split(',')[0];
                    surveyIdValue = tableNameWithSurveyId.Split(',')[1];
                }

                var tableType = _context.Model.GetEntityTypes()
                    .FirstOrDefault(e => e.GetTableName() == tableName)?.ClrType;

                if (tableType == null)
                    return string.Empty;

                if (tableType != typeof(Survey) && tableType != typeof(SampleUnit) && tableType != typeof(SampleUnit_Set))
                {
                    return await GetDynamicJoinAsync(
                        new DynamicJoinProperties
                        {
                            Table1Type = tableType,
                            Table2Type = typeof(Survey),
                            PropertyName = "SurveyId",
                            Table2Columns = new List<string> { "StartChainage", "Direction" },
                            SurveyIdColumnName = "SurveyIdExternal",
                            SurveyIds = surveyIdValue != "All" ? new List<string> { surveyIdValue } : null
                        });
                }

                var dbSet = _context.GetType()
                .GetMethod("Set", System.Type.EmptyTypes)?
                .MakeGenericMethod(tableType)
                .Invoke(_context, null);

                if (dbSet == null)
                    return string.Empty;

                // Convert to IQueryable dynamically
                var asQueryableMethod = typeof(Queryable)
                    .GetMethods()
                    .First(m => m.Name == "AsQueryable" && m.IsGenericMethod)
                    .MakeGenericMethod(tableType);

                var queryable = asQueryableMethod.Invoke(null, new[] { dbSet });

                // Call ToListAsync dynamically
                var toListAsyncMethod = typeof(EntityFrameworkQueryableExtensions)
                    .GetMethods()
                    .First(m => m.Name == "ToListAsync" && m.IsGenericMethod)
                    .MakeGenericMethod(tableType);

                var task = (Task)toListAsyncMethod.Invoke(null, new object[] { queryable, new CancellationToken() });

                await task.ConfigureAwait(false);
                var data = (IEnumerable<object>)task.GetType().GetProperty("Result")!.GetValue(task)!;
                if (data == null || data.Count() == 0) return null;

                //check for surveyId
                if (surveyIdValue != "All")
                {
                    var itemWithSurvey = data.First().GetType().GetProperty("SurveyId", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                    if (itemWithSurvey != null)
                    {
                        var filteredData = data.Where(item =>
                        {
                            var value = itemWithSurvey.GetValue(item);
                            return value != null && value.Equals(surveyIdValue);
                        });

                        data = filteredData;
                    }
                }

                var dynamicData = new List<Dictionary<string, object>>();

                var entityType = _context.Model.GetEntityTypes().FirstOrDefault(e => e.GetTableName() == tableName);
                var navigationProperties = entityType?.GetNavigations().Select(n => n.Name).ToHashSet();

                // Filter out navigation properties
                foreach (var item in data)
                {
                    var entity = new Dictionary<string, object>();
                    foreach (var prop in tableType.GetProperties().Where(prop => !navigationProperties.Contains(prop.Name)).ToList())
                    {
                        if (prop.Name.ToLower() != "geojson" && prop.Name.ToLower() != "surveyid" && (tableName.ToLower() == "survey" || !prop.Name.ToLower().Contains("gps"))  && prop.Name.ToLower() != "pavementtype")
                            entity.Add(prop.Name, prop.GetValue(item) != null ? prop.GetValue(item) : null);
                    }

                    dynamicData.Add(entity);
                }

                return JsonConvert.SerializeObject(dynamicData);
            }
            catch (Exception ex)
            {
                Log.Error($"Error while getting dynamic data from DB : {ex.Message}");
                return null;
            }
        }

public async Task<string> GetDynamicJoinAsync(DynamicJoinProperties dynamicJoinProperties)
{
    try
    {
        System.Type table1Type = dynamicJoinProperties.Table1Type;
        System.Type table2Type = dynamicJoinProperties.Table2Type;
        string joinColumnName = dynamicJoinProperties.PropertyName;
        List<string> table2SelectedColumns = dynamicJoinProperties.Table2Columns;
        string surveyIdColumnName = dynamicJoinProperties.SurveyIdColumnName;
        List<string> surveyIds = dynamicJoinProperties.SurveyIds;

        // Get DbSet<T1> and DbSet<T2>
        var dbSet1 = _context.GetType().GetMethod("Set", System.Type.EmptyTypes)!.MakeGenericMethod(table1Type).Invoke(_context, null)!;
        var dbSet2 = _context.GetType().GetMethod("Set", System.Type.EmptyTypes)!.MakeGenericMethod(table2Type).Invoke(_context, null)!;

        var queryable1 = (IQueryable)dbSet1;
        var queryable2 = (IQueryable)dbSet2;

        // ToListAsync on Table1
        var toListAsync1 = typeof(EntityFrameworkQueryableExtensions)
            .GetMethods().First(m => m.Name == "ToListAsync" && m.IsGenericMethod && m.GetParameters().Length == 2)
            .MakeGenericMethod(table1Type);
        var task1 = (Task)toListAsync1.Invoke(null, new object[] { queryable1, CancellationToken.None })!;
        await task1;
        var list1 = (IEnumerable<object>)task1.GetType().GetProperty("Result")!.GetValue(task1)!;

        // ToListAsync on Table2
        var toListAsync2 = typeof(EntityFrameworkQueryableExtensions)
            .GetMethods().First(m => m.Name == "ToListAsync" && m.IsGenericMethod && m.GetParameters().Length == 2)
            .MakeGenericMethod(table2Type);
        var task2 = (Task)toListAsync2.Invoke(null, new object[] { queryable2, CancellationToken.None })!;
        await task2;
        var list2 = (IEnumerable<object>)task2.GetType().GetProperty("Result")!.GetValue(task2)!;

        // Filter if surveyIds provided
        if (surveyIds != null && surveyIds.Any())
        {
            var table1Prop = table1Type.GetProperty(joinColumnName)!;
            var table2Prop = table2Type.GetProperty(surveyIdColumnName)!;

            var typedSurveyIds1 = surveyIds.Select(id => Convert.ChangeType(id, table1Prop.PropertyType)).ToList();
            var typedSurveyIds2 = surveyIds.Select(id => Convert.ChangeType(id, table2Prop.PropertyType)).ToList();

            list1 = list1.Where(x =>
            {
                var val = table1Prop.GetValue(x);
                return val != null && typedSurveyIds1.Contains(val);
            }).ToList();

            list2 = list2.Where(x =>
            {
                var val = table2Prop.GetValue(x);
                return val != null && typedSurveyIds2.Contains(val);
            }).ToList();
        }

        // Build the join
        var dictList = new List<Dictionary<string, object>>();
        var table2PropForJoin = table2Type.GetProperty(surveyIdColumnName)!;
        var table1PropForJoin = table1Type.GetProperty(joinColumnName)!;

        foreach (var row1 in list1)
        {
            var row1Key = table1PropForJoin.GetValue(row1);
            var row1KeyTyped = Convert.ChangeType(row1Key, table2PropForJoin.PropertyType);

            var row2 = list2.FirstOrDefault(r =>
                Equals(table2PropForJoin.GetValue(r), row1KeyTyped));

            var dict = new Dictionary<string, object>();

            // Add all properties from Table1
            foreach (var prop in table1Type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var value = prop.GetValue(row1);
                if (prop.Name == "SurveyId" && row2 != null)
                {
                    // Replace SurveyId with SurveyName from row2
                    var surveyNameProperty = table2Type.GetProperty("SurveyName");
                    if (surveyNameProperty != null)
                    {
                                value = surveyNameProperty.GetValue(row2);
                    }
                }
                dict[prop.Name] = value;
            }

            // Add selected columns from Table2 (if match found)
            if (row2 != null)
            {
                foreach (var col in table2SelectedColumns)
                {
                    var prop2 = table2Type.GetProperty(col);
                    if (prop2 != null)
                            {
                        dict[$"{table2Type.Name}.{col}"] = prop2.GetValue(row2);
                }
            }
                    }

            dictList.Add(dict);
        }
        return JsonConvert.SerializeObject(dictList);
    }
    catch (Exception ex)
    {
        Log.Error($"Error while getting dynamic data from DB : {ex.Message}");
        return null;
    }
}
        

        public async Task<IdReply> UpdateSegmentChainageInDB(ChainageUpdateRequest chainageRequest, CallContext context = default)
        {
            List<string> queries = new List<string>();
            SurveyRequest surveyRequest = new SurveyRequest
            {
                SurveyId = chainageRequest.SurveyId,
            };
          
            List<LCMS_Segment> lCMS_Segments = await GetBySurvey(surveyRequest);

            Log.Information($"ChainageUpdateRequest items {lCMS_Segments.Count} segments");
           
            foreach (var segment in lCMS_Segments)
            {
                var segmentId = segment.SegmentId;
                var surveyId = segment.SurveyId;
                try
                {
                   
                    double offset = chainageRequest.ChainageDifference;
                    foreach (var tableNameMapping in TableNameHelper.TableNameMappings)
                    {
                        var dbName = tableNameMapping.DBName;
                        // Update only tables related to LCMS
                        if (dbName.StartsWith("LCMS"))
                        {
                            var dbSetProperty = _context.GetType().GetProperty(dbName);
                            if (dbSetProperty != null)
                            {
                                var dbSet = dbSetProperty.GetValue(_context);
                                // Use LINQ dynamically to filter by survey and segment
                                var whereMethod = typeof(Queryable).GetMethods()
                                    .First(m => m.Name == "Where" && m.GetParameters().Length == 2)
                                    .MakeGenericMethod(dbSetProperty.PropertyType.GenericTypeArguments[0]);
                                var entityType = dbSetProperty.PropertyType.GenericTypeArguments[0];
                                var parameter = Expression.Parameter(entityType, "x");
                                var predicate = Expression.Lambda(
                                    Expression.AndAlso(
                                        Expression.Equal(Expression.Property(parameter, "SurveyId"), Expression.Constant(surveyId)),
                                        Expression.Equal(Expression.Property(parameter, "SegmentId"), Expression.Constant(segmentId))
                                    ),
                                    parameter
                                );
                                var filteredQuery = whereMethod.Invoke(null, new object[] { dbSet, predicate }) as IQueryable;
                                // Handle multi-layer differently if needed 
                                // (assuming chainage update logic is same for all tables and layers)
                                queries.AddRange(await AddQueryForChainageOffsetAsync(filteredQuery, dbName, offset));
                            }
                        }
                    }
                    // Also update MetaTableValue or other related tables with chainage
                    queries.AddRange(await AddQueryForChainageOffsetAsync(
                        _context.MetaTableValue.Where(x => x.SegmentId == segmentId && x.SurveyId == surveyId),
                        "MetaTableValue",
                        offset));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception in creating update queries for chainage: {ex.Message}");
                }
            }
            DateTime end = DateTime.Now;
            ExecuteQueryInDb(queries);
            Log.Information($"Time(seconds) to update chainage data in database: {(DateTime.Now - end).TotalSeconds}");
            return new IdReply();
        }

              
        private async Task<List<string>> AddQueryForChainageOffsetAsync(IQueryable chainageQuery, string tableName, double offset)
        {
            var entityQuery = chainageQuery.Cast<IEntity>();
            var entities = entityQuery.ToList();
            if (entities == null || !entities.Any())
                return new List<string>();
            // Using reflection to get Chainage value from each entity
            var chainageEntities = new List<ChainageEntity>();
            foreach (var entity in entities)
            {
                var chainageEntity = new ChainageEntity { Id = entity.Id };

                var chainageProperty = entity.GetType().GetProperty("Chainage");
                if (chainageProperty != null) 
                { 
                    var val = chainageProperty.GetValue(entity); 
                    chainageEntity.Chainage = ConvertToDouble(val); 
                }
                else
                {
                    Console.WriteLine($"Entity type {entity.GetType().Name} does not have a Chainage property.");
                }

                // Handle ChainageEnd
                var chainageEndProp = entity.GetType().GetProperty("ChainageEnd");
                if (chainageEndProp != null)
                {
                    var val = chainageEndProp.GetValue(entity);
                    chainageEntity.ChainageEnd = ConvertToDouble(val);
                }

                chainageEntities.Add(chainageEntity);
            }

            return UpdateChainageWithOffset(chainageEntities, offset, tableName);
        }


        private List<string> UpdateChainageWithOffset(List<ChainageEntity> entities, double offset, string tableName)
        {
            var updateQueries = new List<string>();
            try
            {
                if (entities != null && entities.Count > 0)
                {
                    foreach (var entity in entities)
                    {
                        var setClauses = new List<string>();
                        // Calculate new chainage by applying the offset
                        double newChainage = entity.Chainage + offset; 
                        setClauses.Add($"Chainage = {newChainage}"); 

                        if (entity.ChainageEnd.HasValue) 
                        { 
                            double newChainageEnd = entity.ChainageEnd.Value + offset; 
                            setClauses.Add($"ChainageEnd = {newChainageEnd}"); 
                        }
                        if (setClauses.Any()) 
                        {
                            // Generate SQL update query
                            string updateQuery = $"UPDATE {tableName} SET {string.Join(", ", setClauses)} WHERE Id = {entity.Id};"; 
                            updateQueries.Add(updateQuery); 
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UpdateChainageWithOffset: {ex.Message}");
            }
            return updateQueries;
        }

        private double ConvertToDouble(object val) 
        { 
            if (val == null) return 0.0; 
            if (val is double d) return d; 
            if (val is float f) return Convert.ToDouble(f); 
            if (val is decimal dec) return Convert.ToDouble(dec); 
            return 0.0; 
        }

        public async Task<ChainageReply> GetChainageFromSegmentIdToMapPoint(ChainageMapPointRequest chainageMapPointRequest)
        {
            try
            {
                var segmentId = chainageMapPointRequest.SegmentId;
                var surveyId = chainageMapPointRequest.SurveyId;
                var latitude = chainageMapPointRequest.Latitude;
                var longitude = chainageMapPointRequest.Longitude;

                if (surveyId == null) return new ChainageReply { Chainage = -1 };
                if (latitude == 0 && longitude == 0) return new ChainageReply { Chainage = -1 };

                LCMS_Segment segment = await GetBySectionIdAsync(segmentId, surveyId);
                // Create the start point using the segment's GPS coordinates
                MapPoint startSegmentPoint = new MapPoint(segment.GPSLongitude, segment.GPSLatitude, SpatialReferences.Wgs84);
         
                // Create the end point using the parsed coordinates
                MapPoint endPoint = new MapPoint(longitude, latitude, SpatialReferences.Wgs84);
                // Convert start and end points to a projected coordinate system for accurate distance calculation (e.g. Web Mercator)
                // This step is crucial because lat/lon degrees are not uniform distances
                MapPoint startProjected = (MapPoint)GeometryEngine.Project(startSegmentPoint, SpatialReferences.WebMercator);
                MapPoint endProjected = (MapPoint)GeometryEngine.Project(endPoint, SpatialReferences.WebMercator);
                // Vector from start to end in projected coordinates (meters)
                double dx = endProjected.X - startProjected.X;
                double dy = endProjected.Y - startProjected.Y;
                // Track angle in radians (convert from degrees, assuming 0 degrees = North, clockwise positive)
                double trackAngleRadians = (segment.GPSTrackAngle % 360) * (Math.PI / 180);
                // Direction vector of the segment (X points East, Y points North):
                // Since 0 degrees means heading North, angle 0 points along +Y axis
                // So direction vector: (sin(trackAngle), cos(trackAngle))
                // Because 0 deg = North means Y axis, and increasing angle clockwise means East is 90 deg.
                double dirX = Math.Sin(trackAngleRadians);
                double dirY = Math.Cos(trackAngleRadians);
                // Project the vector (dx, dy) onto the direction vector (dirX, dirY)
                double projectedDistance = dx * dirX + dy * dirY;
                // Add the segment's initial chainage to get total chainage
                double totalChainage = segment.Chainage + projectedDistance;
                ChainageReply chainageReply = new ChainageReply { Chainage = totalChainage };
                // Optional: debug output
                Console.WriteLine($"Start Projected Point: {startProjected}");
                Console.WriteLine($"End Projected Point: {endProjected}");
                Console.WriteLine($"Vector dx, dy: ({dx}, {dy})");
                Console.WriteLine($"Track Angle (degrees): {segment.GPSTrackAngle}");
                Console.WriteLine($"Track Angle (radians): {trackAngleRadians}");
                Console.WriteLine($"Direction Vector (X, Y): ({dirX}, {dirY})");
                Console.WriteLine($"Projected Distance along segment: {projectedDistance}");
                Console.WriteLine($"Total Chainage: {totalChainage}");
                return chainageReply;   
            }
            catch (Exception ex)
            {
                Log.Error($"Error in GetChainageFromSegmentIdToMapPoint : {ex.Message}");
                return new ChainageReply { Chainage = -1 };
            }
        }

        public class ChainageEntity
        {
            public int Id { get; set; }
            public double Chainage { get; set; }
            public double? ChainageEnd { get; set; }
        }


        public async Task<LCMS_Segment> GetBySectionIdAsync(int segmentId, string surveyId)
        {
            var segments = await _repository.GetAllAsync();
            if (segments != null)
            {
                var matchingSegment = segments.FirstOrDefault(s => s.SectionId == segmentId.ToString() && s.SurveyId == surveyId);
                if (matchingSegment != null)
                {
                    return matchingSegment;
                }
            }
            return new LCMS_Segment();
        }

        public async Task<LCMS_Segment> UpdateGenericData(string fieldsToUpdateSerialized)
        {
            var entity = new LCMS_Segment();
            Dictionary<string, object> fieldsToUpdate = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(fieldsToUpdateSerialized);
            entity.Id = Convert.ToInt32(fieldsToUpdate["Id"]);
            return await _repository.UpdateEntityAsync(entity, fieldsToUpdate, entity.Id);
        }

        public async Task<List<LCMS_Segment>> CalculateSegmentSummaryFromMap(List<SurveyAndSegmentRequest> request)
        {
            var segmentList = new List<LCMS_Segment>();
            try
            {
                foreach (var item in request)
                {
                    var segmentEntity = _context.LCMS_Segment
                        .FirstOrDefault(x => x.SurveyId == item.SurveyId && x.SegmentId == item.SegmentId);
                    if (segmentEntity != null)
                    {
                        segmentList.Add(segmentEntity);
                    }
                }

                return await CalculateSegmentSummary(segmentList);
            }
            catch (Exception ex)
            {
                Log.Error($"Error in CalculateSegmentSummary : {ex.Message}");
            }

            return segmentList;
        }

        public async Task<List<LCMS_Segment>> CalculateSegmentSummary(List<LCMS_Segment> segments)
        {
            var processingFunctions = new Dictionary<string, Func<List<object>, LCMS_Segment, LCMS_Segment>>()
            {
                { LayerNames.Pickout, (data, segment) => SegmentSummariesHelper.CalculatePickouts(data.Cast<LCMS_PickOuts_Raw>().ToList(),segment) },
                { LayerNames.Cracking, (data, segment) => SegmentSummariesHelper.CalculateCracks(data.Cast<LCMS_Cracking_Raw>().ToList(), segment) },
                { LayerNames.SegmentGrid, (data, segment) => SegmentSummariesHelper.CalculateSegmentGrids(data.Cast<LCMS_Segment_Grid>().ToList(),segment) },
                { LayerNames.Ravelling, (data, segment) => SegmentSummariesHelper.CalculateRavelling(data.Cast<LCMS_Ravelling_Raw>().ToList(),segment) },
                { LayerNames.Patch, (data, segment) => SegmentSummariesHelper.CalculatePatch(data.Cast<LCMS_Patch_Processed>().ToList(),segment) },
                { LayerNames.Bleeding, (data, segment) => SegmentSummariesHelper.CalculateBleeding(data.Cast<LCMS_Bleeding>().ToList(),segment) },
                { LayerNames.Pumping, (data, segment) => SegmentSummariesHelper.CalculatePumping(data.Cast<LCMS_Pumping_Processed>().ToList(),segment) },
                { LayerNames.Rutting, (data, segment) => SegmentSummariesHelper.CalculateRutting(data.Cast<LCMS_Rut_Processed>().ToList(),segment) },
                { LayerNames.Roughness, (data, segment) => SegmentSummariesHelper.CalculateRoughness(data.Cast<LCMS_Rough_Processed>().ToList(),segment) },
                { LayerNames.SealedCrack, (data, segment) => SegmentSummariesHelper.CalculateSealedCracks(data.Cast<LCMS_Sealed_Cracks>().ToList(),segment) },
                { LayerNames.Shove, (data, segment) => SegmentSummariesHelper.CalculateShove(data.Cast<LCMS_Shove_Processed>().ToList(),segment) },
                { LayerNames.MacroTexture, (data, segment) => SegmentSummariesHelper.CalculateTexture(data.Cast<LCMS_Texture_Processed>().ToList(),segment) },
                { LayerNames.SagsBumps, (data, segment) => SegmentSummariesHelper.CalculateSagsBumps(data.Cast<LCMS_Sags_Bumps>().ToList(),segment) },
                { LayerNames.Geometry, (data, segment) => SegmentSummariesHelper.CalculateGeometry(data.Cast<LCMS_Geometry_Processed>().ToList(),segment) },
                { LayerNames.Potholes, (data, segment) => SegmentSummariesHelper.CalculatePotholes(data.Cast<LCMS_Potholes_Processed>().ToList(),segment) },
                { LayerNames.MMO, (data, segment) => SegmentSummariesHelper.CalculateMMO(data.Cast<LCMS_MMO_Processed>().ToList(),segment) },
                { LayerNames.PASER, (data, segment) => SegmentSummariesHelper.CalculatePASER(data.Cast<LCMS_PASER>().ToList(),segment) },
            };

            try
            {
                foreach (var segmentEntity in segments)
                {
                    var surveyId = segmentEntity.SurveyId;
                    var segmentId = segmentEntity.SegmentId;

                    foreach (var tableNameMapping in TableNameHelper.TableNameMappings)
                    {
                        var layerName = tableNameMapping.LayerName;
                        var dbName = tableNameMapping.DBName;
                        if (dbName.StartsWith("LCMS") && processingFunctions.ContainsKey(layerName))
                        {
                            var dbSetProperty = _context.GetType().GetProperty(dbName);
                            if (dbSetProperty != null)
                            {
                                var dbSet = dbSetProperty.GetValue(_context);
                                // Use LINQ dynamically to filter by survey and segment
                                var whereMethod = typeof(Queryable).GetMethods()
                                    .First(m => m.Name == "Where" && m.GetParameters().Length == 2)
                                    .MakeGenericMethod(dbSetProperty.PropertyType.GenericTypeArguments[0]);
                                var entityType = dbSetProperty.PropertyType.GenericTypeArguments[0];
                                var parameter = Expression.Parameter(entityType, "x");
                                var predicate = Expression.Lambda(
                                    Expression.AndAlso(
                                        Expression.Equal(Expression.Property(parameter, "SurveyId"), Expression.Constant(surveyId)),
                                        Expression.Equal(Expression.Property(parameter, "SegmentId"), Expression.Constant(segmentId))
                                    ),
                                    parameter
                                );
                                var filteredQuery = whereMethod.Invoke(null, new object[] { dbSet, predicate }) as IQueryable;
                                var entityList = filteredQuery.Cast<object>().ToList();

                                if (entityList != null && entityList.Count > 0)
                                {
                                    processingFunctions[layerName](entityList, segmentEntity);
                                }
                            }
                        }
                    }
                }
                // Save the updated segment entity
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Log.Error($"Error in CalculateSegmentSummaryFromSurvey: {ex.Message}");
            }

            return segments;
        }
    }
}