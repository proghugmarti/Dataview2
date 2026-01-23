using CsvHelper;
using Dapper;
using DataView2.Core;
using DataView2.Core.Communication;
using DataView2.Core.Helper;
using DataView2.Core.Models;
using DataView2.GrpcService.Data;
using DataView2.GrpcService.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using ProjNet.CoordinateSystems;
using ProtoBuf.Grpc;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Net.NetworkInformation;
using static DataView2.Core.Helper.Tools;

namespace DataView2.GrpcService.Services
{
    public class SettingTablesService : RepositoryTables<TablesSetting>, ISettingTablesService
    {
        private readonly AppDbContextProjectData _context;
        private readonly AppDbContextMetadata _metadataContext;
        static private string _configAppPath = Path.Combine(AppPaths.DocumentsFolder, "User Export Files");
        static private string _userFiles = "User CSV Files";
        static private string _configPath = $"{_configAppPath}\\{_userFiles}".Replace("\\", "/"); //@"D:\temp\FilesCSV";
        public SettingTablesService(AppDbContextProjectData context, AppDbContextMetadata metadataContext) : base(context)
        {
            _context = context;
            _metadataContext = metadataContext;
        }

        public async override Task<IEnumerable<TablesSetting>> QueryAsync(string predicate)
        {
            try
            {
                var sqlQuery = predicate;
                //var lstTables = await _context.Set<T>().FromSqlRaw(sqlQuery).ToListAsync();
                var lstTables = await _context.TableExport.FromSqlRaw(sqlQuery).ToListAsync();

                return lstTables;
            }
            catch (Exception ex)
            {
                Utils.RegError($"Error when execute query: {ex.Message}");
                return new List<TablesSetting>();
            }
        }

        public async Task<IEnumerable<TablesSetting>> DBTablesAsync(string folderPath)
        {
            try
            {
                _configPath = folderPath.Replace("\\", "/");
                var sqlQuery = "SELECT rootpage, name, '' as file FROM sqlite_master WHERE type='table' AND name NOT LIKE '%Migration%' AND name NOT LIKE '%sequence%';";

                //Solution without Dapper:
                //var lstTables = await _context.TableExport.FromSqlRaw(sqlQuery).ToListAsync();

                //********************Solution with Dapper:
                var connection = _context.Database.GetDbConnection();
                using (connection)
                {
                    var lstTables = await connection.QueryAsync<TablesSetting>(sqlQuery);

                    foreach (var tableInfo in lstTables)
                    {
                        bool res = await WriteTableToCSVDapper(tableInfo.Name, $"{tableInfo.Name}.csv");
                        if (res)
                            tableInfo.File = $"{tableInfo.Name}.csv";
                    }
                    return lstTables;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error when execute query: {ex.Message}");
                Utils.RegError($"Error when execute query: {ex.Message}");
                return new List<TablesSetting>();
            }
        }

        private async Task<bool> WriteTableToCSVH(string tableName, string filename)
        {
            try
            {
                var connection = _context.Database.GetDbConnection();
                if (connection.State != ConnectionState.Open)
                {
                    await connection.OpenAsync();
                }
                using var command = _context.Database.GetDbConnection().CreateCommand();
                command.CommandText = $"SELECT * FROM {tableName}";
                using var reader = command.ExecuteReader();

                if (!Directory.Exists(_configPath))
                {
                    Directory.CreateDirectory(_configPath);
                }

                string file = Path.Combine(_configPath, filename);
                using var writer = new StreamWriter(file);
                using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

                // Write Headers
                var headers = new List<string>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    headers.Add(reader.GetName(i));
                }
                csv.WriteField(headers);
                csv.NextRecord();

                // Write data
                while (reader.Read())
                {
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        csv.WriteField(reader.GetValue(i));
                    }
                    csv.NextRecord();
                }
                return true;
            }
            catch (Exception ex)
            {
                Utils.RegError($"Error when generating CSV files: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> WriteTableToCSVDapper(string tableName, string filename)
        {
            string sqlQuery = $"SELECT * FROM {tableName}";
            try
            {
                var resultsIDagger = await QueryAsyncDagger<GeneralSetting>(sqlQuery);
                var connection = _context.Database.GetDbConnection();
                using (connection)
                {
                    var results = await connection.QueryAsync<dynamic>(sqlQuery);

                    if (!Directory.Exists(_configPath))
                    {
                        Directory.CreateDirectory(_configPath);
                    }

                    string file = Path.Combine(_configPath, filename);
                    using var writer = new StreamWriter(file);
                    using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

                    //Exit when not results
                    if (!results.Any()) return true;

                    var firstResult = results.First();
                    var headers = ((IDictionary<string, object>)firstResult).Keys.ToList();
                    // Write Headers
                    foreach (var header in headers)
                    {
                        csv.WriteField(header);
                    }
                    csv.NextRecord();

                    // Write data
                    foreach (var result in results)
                    {
                        foreach (var header in headers)
                        {
                            var value = ((IDictionary<string, object>)result)[header];
                            csv.WriteField(value);
                        }
                        csv.NextRecord();
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                Utils.RegError($"Error when generating Dapper - CSV files: {ex.Message}");
                return false;
            }
        }

        public async Task<TablesSettingReply> SaveTemplatedCSV(TablesSetting tablesSetting, CallContext context = default)
        {
            string sqlQuery = tablesSetting.Name;//added query for filtered data //$"SELECT * FROM {tablesSetting.Name}";
            string coordinatesSystem = await GetCoordinateSystemType();

            try
            {
                var resultsIDagger = await QueryAsyncDagger<GeneralSetting>(sqlQuery);
                var connection = _context.Database.GetDbConnection();
                string latitudeCoordinateTransformation = "";
                string longitudeCoordinateTransformation = "";
                bool transformCoordinates = false;

                using (connection)
                {
                    var results = await connection.QueryAsync<dynamic>(sqlQuery);

                    if (!Directory.Exists(Path.GetDirectoryName(tablesSetting.File)))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(tablesSetting.File));
                    }

                    using var writer = new StreamWriter(tablesSetting.File);
                    using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

                    //Exit when not results
                    if (results.Any())
                    {
                        var firstResult = results.First();
                        var headers = ((IDictionary<string, object>)firstResult).Keys.ToList();

                        //Coordinates formats:
                        if (!string.IsNullOrEmpty(coordinatesSystem) && Enum.TryParse<cstFormats>(coordinatesSystem.Replace("-", "_"), out var format) && format != cstFormats.WGS84)
                        {
                            latitudeCoordinateTransformation = $"GPSLatitude_{coordinatesSystem}";
                            longitudeCoordinateTransformation = $"GPSLongitude_{coordinatesSystem}";
                            headers.Add(latitudeCoordinateTransformation);
                            headers.Add(longitudeCoordinateTransformation);
                            transformCoordinates = true;
                        }

                        // Write Headers
                        foreach (var header in headers)
                        {
                            csv.WriteField(header);
                        }
                        csv.NextRecord();

                            // Write data
                        foreach (var result in results)
                        {

                            //Coordinates formats:
                            Core.Communication.GPSCoordinate coordinate = new Core.Communication.GPSCoordinate();
                            coordinate.Latitude = 0.0;
                            coordinate.Longitude = 0.0;
                            coordinate.Altitude = 0.0;
                            if (((IDictionary<string, object>)result).TryGetValue("GPSLatitude", out var latValue) &&
                                latValue != null && double.TryParse(latValue.ToString(), out double latitude) && !double.IsNaN(latitude))
                            {
                                coordinate.Latitude = latitude;
                            }

                            if (((IDictionary<string, object>)result).TryGetValue("GPSLongitude", out var lonValue) &&
                                lonValue != null && double.TryParse(lonValue.ToString(), out double longitude) && !double.IsNaN(longitude))
                            {
                                coordinate.Longitude = longitude;
                            }

                            if (((IDictionary<string, object>)result).TryGetValue("GPSAltitude", out var altValue) &&
                                lonValue != null && double.TryParse(lonValue.ToString(), out double altitude) && !double.IsNaN(altitude))
                            {
                                coordinate.Altitude = altitude;
                            }


                            foreach (var header in headers)
                            {
                                //var value = ((IDictionary<string, object>)result)[header];
                                //csv.WriteField(value);
                                if (header == "SurveyDate")
                                {
                                    if (((IDictionary<string, object>)result).TryGetValue(header, out var objDate))
                                    {
                                        string strDate = convertToUtcDateTime(objDate);
                                        ((IDictionary<string, object>)result)[header] = strDate;
                                        csv.WriteField(strDate);
                                    }
                                    else
                                    {
                                        csv.WriteField("");
                                    }
                                }
                                else
                                {
                                    if (((IDictionary<string, object>)result).TryGetValue(header, out var value))
                                    {
                                        csv.WriteField(value);
                                    }
                                    else if (header != latitudeCoordinateTransformation && header != longitudeCoordinateTransformation)
                                    {
                                        csv.WriteField("");
                                    }
                                }
                            }
                            if (transformCoordinates && Enum.TryParse<cstFormats>(coordinatesSystem.Replace("-", "_"), out var formatD) && formatD != cstFormats.WGS84)
                            {
                                
                                var point = CoordinateHelper.TransformCoordinate(coordinate.Latitude, coordinate.Longitude, 16370);

                                csv.WriteField(point.X.ToString());
                                csv.WriteField(point.Y.ToString());
                            }
                            csv.NextRecord();
                        }

                        return await Task.FromResult(new TablesSettingReply { FileDownloaded = true });
                    }
                    else
                        return await Task.FromResult(new TablesSettingReply { FileDownloaded = false });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error when generating Dapper - CSV files: {ex.Message}");
                return await Task.FromResult(new TablesSettingReply { FileDownloaded = false });
            }
        }

        private async Task<string> GetCoordinateSystemType()
        {
            string sqlQuery = $"SELECT CoordinateSystemType FROM GeneralSettings where Name='ExportURL' LIMIT 1";
            string coordinateSystemType = "";
            try
            {
                var connection = _metadataContext.Database.GetDbConnection();

                using (connection)
                {
                    await connection.OpenAsync(); // Abre la conexión antes de ejecutar la consulta
                    var result = await connection.QueryFirstOrDefaultAsync<string>(sqlQuery);

                    if (result != null)
                    {
                        coordinateSystemType = result;
                    }
                }
                return coordinateSystemType;
            }
            catch (Exception ex)
            {
                Utils.RegError($"Error when generating Dapper - CSV files: {ex.Message}");
                return string.Empty;
            }
        }

        public static string convertToUtcDateTime(object date)
        {
            DateTime dateTime = TimeZoneInfo.ConvertTimeToUtc(Convert.ToDateTime(date));
            string strIsoDate = dateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            return strIsoDate;
        }

        public async Task<TablesSettingsDataReply> TableHasDataAsync(TablesSettingsData table)
        {
            await using var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();

            try
            {
                // Checks if the table has a SurveyId column
                var entityType = _context.Model.GetEntityTypes().FirstOrDefault(e => e.GetTableName().Equals(table.Name));

                if(entityType == null)
                {
                    return new TablesSettingsDataReply { RowsExist = false };
                }

                var actualColumns = entityType.GetProperties()
                    .Select(p => p.GetColumnName(StoreObjectIdentifier.Table(table.Name, null)))
                    .Where(c => c != null).ToHashSet();

                string sql;
                object parameters = null;

                if (actualColumns.Contains("SurveyId"))
                {
                    if (table.Name.Contains("LCMS") || table.Name.Contains("LASfile")) 
                    {
                        sql = $"SELECT 1 FROM {table.Name} WHERE SurveyId = @SurveyExternalId LIMIT 1";
                        parameters = new { SurveyExternalId = table.SurveyExternalId };
                    }
                    else
                    {
                        sql = $"SELECT 1 FROM {table.Name} WHERE SurveyId = @SurveyId LIMIT 1";
                        parameters = new { SurveyId = table.SurveyId };
                    }
                }
                else
                {
                    sql = $"SELECT 1 FROM {table.Name} LIMIT 1";
                }

                var results = await connection.QueryAsync<dynamic>(sql, parameters);

                return new TablesSettingsDataReply
                {
                    RowsExist = results.Any()
                };
            }
            catch (Exception ex)
            {
                Utils.RegError($"Error when finding table data: {ex.Message}");
                return new TablesSettingsDataReply{RowsExist = false};
            }
        }
    }
}
