using DataView2.Core.Models;
using DataView2.Core.Models.Database_Tables;
using DataView2.Core.Models.LCMS_Data_Tables;
using DataView2.GrpcService.Data;
using DataView2.GrpcService.Interfaces;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ProtoBuf.Grpc;
using Serilog;
using System.Data;
using System.Data.SQLite;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations.Schema;
using DataView2.Core.Helper;
using System.Data.Entity;

namespace DataView2.GrpcService.Services.AppDbServices
{
    public class DataBaseRegistryLocalService : IDatabaseRegistryLocalService
    {
        private readonly IRepository<DatabaseRegistryLocal> _repository;
        private readonly ILogger<DataBaseRegistryLocalService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private string datasetPath = "";


        public DataBaseRegistryLocalService(
            IRepository<DatabaseRegistryLocal> repository,
            IServiceProvider serviceProvider,
            ILogger<DataBaseRegistryLocalService> logger)

        {
            _repository = repository;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public AppDbContextProjectData CreateDbContext(string databasePath)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContextProjectData>();
            optionsBuilder.UseSqlite($"Data Source={databasePath}");

            var databasePathProvider = new DatabasePathProvider(databasePath, string.Empty, databasePath);

            return new AppDbContextProjectData(optionsBuilder.Options, databasePathProvider);
        }
        public async Task<DatabaseRegistryLocal> GetById(IdRequest request, CallContext context = default)
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

        public async Task<List<DatabaseRegistryLocal>> GetAll(Empty empty, CallContext context = default)
        {
            var entities = await _repository.GetAllAsync();
            return new List<DatabaseRegistryLocal>(entities);
        }

        public async Task<DatabaseRegistryLocal> GetByName(string datasetName)
        {
            var entities = await _repository.GetAllAsync();
            var matchingEntity = entities.FirstOrDefault(e => e.Name == datasetName);
            if (matchingEntity != null)
            {
                return matchingEntity;
            }

            return new DatabaseRegistryLocal();
        }

        public async Task<IdReply> Exists(ExistRequest<DatabaseRegistryLocal> existRequest, CallContext context = default)
        {
            try
            {
                var entities = await _repository.GetAllAsync();
                var matchingEntity = entities.FirstOrDefault(entity => existRequest.Condition(entity));

                if (matchingEntity != null)
                {
                    // Matching entity found
                    return new IdReply
                    {
                        Id = -1,
                        Message = "Dataset with same name already exists"
                    };
                }
                else
                {
                    // No matching entity found
                    return new IdReply
                    {
                        Id = 0,
                        Message = "Dataset does not exist"
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return new IdReply
                {
                    Id = -1,
                    Message = "Error checking dataset existence."
                };
            }
        }

        public async Task<IdReply> CreateNewDatasetAsync(NewDatasetRequest request)
        {
            try
            {
                // Combine the picked folder path with the Dataset name to create the new database path
                var newPath = Path.Combine(request.DatasetLocation, $"{request.DatasetName}.db");

                var newRegistryEntry = new DatabaseRegistryLocal
                {
                    Name = request.DatasetName,
                    Path = newPath,
                    CreatedAtActionResult = DateTime.Now,
                    UpdatedAtActionResult = DateTime.Now,
                    ProjectId = request.ProjectId
                };

                var existRequest = new ExistRequest<DatabaseRegistryLocal>
                {
                    Entity = newRegistryEntry,
                    Condition = reg => reg.Name == request.DatasetName
                };
                var existResult = await Exists(existRequest);

                if (existResult.Id == -1)
                {
                    // Dataset with the same name already exists
                    return existResult;
                }
                else if (existResult.Id == 0)
                {
                    // Continue with dataset creation logic
                    var response = await _repository.CreateAsync(newRegistryEntry);

                    // Create and migrate the new Dataset data DbContext
                    using (var context = CreateDbContext(newPath))
                    {
                        context.Database.Migrate();
                    }

                    //// Change the database to the newly created one
                    //NewDatabaseRequest requestDataBase = new NewDatabaseRequest { NewDatabasePath = newPath, DbType = nameof(Tools.dbContextType.Dataset) };
                    //await ChangeDatabase(requestDataBase);

                    return new IdReply
                    {
                        Id = response.Id,
                        Message = "Dataset Created Successfully."
                    };
                }
                else
                {
                    // Handle other error cases
                    return new IdReply
                    {
                        Id = -1,
                        Message = "Error checking dataset existence."
                    };
                }


            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            return new IdReply { };

        }

        public async Task<IdReply> ImportDatasetAsync(ImportDatasetRequest request)
        {
            try
            {
                _logger.LogInformation("Starting ImportDatasetAsync with DatasetName: {DatasetName}, ProjectId: {ProjectId}",
                    request.DatasetName, request.ProjectId);

                var newRegistryEntry = new DatabaseRegistryLocal
                {
                    Name = request.DatasetName,
                    Path = request.DestinationLocation,  // Store the final location
                    CreatedAtActionResult = DateTime.Now,
                    UpdatedAtActionResult = DateTime.Now,
                    ProjectId = request.ProjectId
                };
                var destinationFilePath = request.DestinationLocation;

                // Step 1: Copy dataset file
                string sourceFilePath = request.SourceLocation; // Original dataset file path

                _logger.LogInformation("Copying dataset file from {Source} to {Destination}", sourceFilePath, request.DestinationLocation);
                var copyResult = CopyDatasetFile(sourceFilePath, request.DestinationLocation);

                if (!copyResult.IsSuccess)
                {
                    _logger.LogError("Dataset copy failed: {ErrorMessage}", copyResult.Message);
                    return new IdReply
                    {
                        Id = -1,
                        Message = $"Error copying dataset: {copyResult.Message}"
                    };
                }

                _logger.LogInformation("Dataset file copied successfully.");

                // Step 2: Ensure database schema is up-to-date
                _logger.LogInformation("Ensuring database schema is up-to-date for database at: {DestinationPath}", destinationFilePath);
                var migrationStatus = EnsureDatabaseIsUpToDate(destinationFilePath);

                Log.Information(migrationStatus.Result.Message, destinationFilePath);

                _logger.LogInformation("Database migration successful.");

                // Step 3: Save dataset to registry
                var response = await _repository.CreateAsync(newRegistryEntry);
                _logger.LogInformation("Dataset created with ID: {Id}", response.Id);

                return new IdReply
                {
                    Id = response.Id,
                    Message = "Dataset imported successfully."
                };


            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred in ImportDatasetAsync. DatasetName: {DatasetName}, SourceLocation: {SourceLocation}, DestinationLocation: {DestinationLocation}",
                    request.DatasetName, request.SourceLocation, request.DestinationLocation);

                return new IdReply
                {
                    Id = -1,
                    Message = $"Error: {ex.Message}"
                };
            }
        }


        public async Task<IdReply> EnsureDatabaseIsUpToDate(string databasePath)
        {

            Log.Information("Starting database migration check for database at: {DatabasePath}", databasePath);

            // Retrieve the DatabasePathProvider and update it
            var databasePathProvider = _serviceProvider.GetRequiredService<DatabasePathProvider>();
            databasePathProvider.SetDatasetDatabasePath(databasePath);


            // Use a new DbContext instance to ensure updated path
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContextProjectData>();
            optionsBuilder.UseSqlite($"Data Source={databasePath}");

            using var newContext = new AppDbContextProjectData(optionsBuilder.Options, databasePathProvider);

            // Check if the __EFMigrationsHistory table exists
            //bool migrationTableExists = newContext.Database.ExecuteSqlRaw("SELECT name FROM sqlite_master WHERE type = 'table' AND name = '__EFMigrationsHistory'") > 0;
            try
            {
                var pendingMigrations = newContext.Database.GetPendingMigrations();
                if (pendingMigrations.Any())
                {
                    Log.Information("Applying migrations...");
                    var contextProject = _serviceProvider.GetRequiredService<AppDbContextProjectData>();
                    contextProject.Database.Migrate();
                    Log.Information("Database migration completed successfully.");
                }
                else
                {
                    Log.Information("Database is up-to-date. No need to migrate.");
                }
                return new IdReply
                {
                    Id = 1,
                    Message = "Dataset migrated to the latest version"
                };
            }

            catch (Exception ex)
            {
                Log.Error($"Error during migration: {ex.Message}");
                return new IdReply
                {
                    Id = -1,
                    Message = "Error migrating dataset"
                };
            }

        }


        private (bool IsSuccess, string Message) CopyDatasetFile(string sourcePath, string destinationPath)
        {
            try
            {
                if (!File.Exists(sourcePath))
                {
                    return (false, "Source file does not exist.");
                }

                File.Copy(sourcePath, destinationPath, true); // Overwrite if exists
                return (true, "File copied successfully.");
            }
            catch (Exception ex)
            {
                return (false, $"File copy failed: {ex.Message}");
            }
        }

        public async Task<IdReply> UpdateGPSCoordinates(UpdateCoordinatesRequest request, CallContext context = default)
        {
            try
            {

                var existingEntry = await _repository.GetByIdAsync(request.Id);
                if (existingEntry != null)
                {

                    if (!(request.GPSLatitude == 0 && request.GPSLongitude == 0))
                    { // Update GPS coordinates
                        existingEntry.GPSLatitude = request.GPSLatitude;
                        existingEntry.GPSLongitude = request.GPSLongitude;
                        await _repository.UpdateAsync(existingEntry);
                    }
                    return new IdReply
                    {
                        Id = existingEntry.Id,
                        Message = "GPS coordinates updated successfully."
                    };
                }

                return new IdReply
                {
                    Id = 0,
                    Message = "Entry not found for updating GPS coordinates."
                };
            }

            catch (Exception ex)
            {
                return new IdReply
                {
                    Id = 0,
                    Message = "Error updating GPS coordinates: " + ex.Message
                };
            }
        }

        public async Task ChangeDatabase(NewDatabaseRequest newDatabasePath, CallContext context = default)
        {
            try
            {
                string newDatabasePathAfterChange = "";
                if (newDatabasePath.DbType == "Dataset")
                {
                    var databasePathProvider = _serviceProvider.GetRequiredService<DatabasePathProvider>();
                    databasePathProvider.SetDatasetDatabasePath(newDatabasePath.NewDatabasePath);
                    newDatabasePathAfterChange = databasePathProvider.GetDatabasePath();
                    await LoadChangedDatabase(newDatabasePath.NewDatabasePath);
                }
                else if (newDatabasePath.DbType == "Metadata")
                {
                    var databasePathProvider = _serviceProvider.GetRequiredService<DatabasePathProvider>();
                    databasePathProvider.SetMetadataDatabasePath(newDatabasePath.NewDatabasePath);
                    newDatabasePathAfterChange = databasePathProvider.GetMetadataDatabasePath();
                }

                // Print the new connection string
                Console.WriteLine($"Current Database Path After Change: {newDatabasePathAfterChange}");

                //added in the new function to load registries on change of dataset only
                //var registries = await _repository.GetAllAsync();
                //var registryToUpdate = registries.FirstOrDefault(reg => reg.Path == newDatabasePath.NewDatabasePath);

                //if (registryToUpdate != null)
                //{
                //    // registryToUpdate.UpdatedAtActionResult = DateTime.Now;
                //    // await _repository.UpdateAsync(registryToUpdate);
                //    var fieldsToUpdate = new Dictionary<string, string> { { "UpdatedAtActionResult", DateTime.Now.ToString("o") } };
                //    await _repository.UpdateSpecificSQLAsync(registryToUpdate, fieldsToUpdate, registryToUpdate.Id);
                //}
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                throw;
            }
        }
        private async Task LoadChangedDatabase(string newDatabasePath, CallContext context = default)
        {
            try
            {
                var registries = await _repository.GetAllAsync();
                var registryToUpdate = registries.FirstOrDefault(reg => reg.Path == newDatabasePath);

                if (registryToUpdate != null)
                {
                    var fieldsToUpdate = new Dictionary<string, string> { { "UpdatedAtActionResult", DateTime.Now.ToString("o") } };
                    await _repository.UpdateSpecificSQLAsync(registryToUpdate, fieldsToUpdate, registryToUpdate.Id);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                throw;
            }
        }

        public Task<string> GetActualDatabasePath(CallContext context = default)
        {
            try
            {
                var databasePathProvider = _serviceProvider.GetRequiredService<DatabasePathProvider>();
                string actualDatabasePath = databasePathProvider.GetDatabasePath();
                return Task.FromResult(actualDatabasePath);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                throw;
            }
        }

        public async Task<DatabaseRegistryLocal> GetActualDatasetRegistry(IdRequest idRequest, CallContext context = default)
        {
            try
            {

                // var path = await GetActualDatabasePath();
                //if (path == "DataView_ProjectDB.db")
                //{
                //    DatabaseRegistryLocal defaultdb = new DatabaseRegistryLocal
                //    {
                //        Name = "Default",
                //        Path = path,
                //        ProjectId = -1,
                //        CreatedAtActionResult = DateTime.Now,
                //        UpdatedAtActionResult = DateTime.Now
                //    };
                //    return defaultdb;
                //}

                var registry = await _repository.FirstOrDefaultAsync(reg => reg.Id == idRequest.Id);
                //var registry = await _repository.FirstOrDefaultAsync(reg => reg.DbPath == path);


                return registry;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                throw;
            }
        }

        public async Task<List<DatabaseRegistryLocal>> GetAllByProjectId(Project projectId)
        {
            try
            {
                var registries = await _repository.GetAllAsync();
                return registries.Where(reg => reg.ProjectId == projectId.Id).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                throw;
            }
        }

        public async Task<IdReply> DeleteDataset(DatabaseRegistryLocal request, CallContext context = default)
        {
            try
            {
                //release the db handle from the context
                SqliteConnection.ClearAllPools();

                //removing dataset from the physical filepath
                Thread.Sleep(50);
                if (File.Exists(request.Path))
                    File.Delete(request.Path);

                await _repository.DeleteAsync(request.Id);
                return new IdReply
                {
                    Id = 0,
                    Message = "Dataset deleted Successfully."
                };
            }
            catch (Exception ex)
            {
                Console.Out.WriteLineAsync($"Error in deleting dataset : {ex.Message}");
                return new IdReply
                {
                    Id = -1,
                    Message = $"Dataset deletion is failed as {ex.Message}"
                };
            }
        }

        public async Task<IdReply> RenameDataset(DatabaseRegistryLocal request, CallContext context = default)
        {
            try
            {
                DatabaseRegistryLocal registry = await _repository.UpdateAsync(request);

                //disposing context to release handler
                SqliteConnection.ClearAllPools();

                return new IdReply
                {
                    Id = 0,
                    Message = "Dataset renamed Successfully."
                };
            }
            catch (Exception ex)
            {
                Console.Out.WriteLineAsync($"Error in deleting dataset : {ex.Message}");
                return new IdReply
                {
                    Id = -1,
                    Message = $"Dataset deletion is failed as {ex.Message}"
                };
            }
        }


        public async Task CloseDatabaseConnection(string databasePath)
        {
            SqliteConnection.ClearAllPools();
            using (var context = CreateDbContext(databasePath))
            {
                await context.Database.CloseConnectionAsync();
            }
        }




        public async Task DeleteSurveysAsync(DeleteSurveysRequest request)
        {
            if (request.SelectedSurveys == null || request.SelectedSurveys.Count == 0 || request.LcmsTables == null || request.LcmsTables.Count == 0)
            {
                throw new ArgumentException("No surveys or tables specified for deletion.");
            }

            var deleteQueries = new List<string>();

            foreach (var survey in request.SelectedSurveys)
            {
                // Generate DELETE queries for LCMS tables
                foreach (var tblName in request.LcmsTables)
                {
                    if (tblName == "LASfile")
                    {
                        // Delete LASPoints related to the LASfile records of the given SurveyId
                        deleteQueries.Add($"DELETE FROM LasPoint WHERE LASfileId IN (SELECT Id FROM LasFile WHERE SurveyId='{survey.SurveyExternalId}');");

                        // Delete LASfile records after deleting related LASPoints
                        deleteQueries.Add($"DELETE FROM LasFile WHERE SurveyId='{survey.SurveyExternalId}';");
                    }
                    else if (tblName == "Summary")
                    {
                        // Delete related summary defect first
                        deleteQueries.Add($"DELETE FROM SummaryDefect WHERE SummaryId IN (SELECT Id FROM Summary WHERE SurveyId = '{survey.SurveyExternalId}');");

                        deleteQueries.Add($"DELETE FROM {tblName} WHERE SurveyId='{survey.SurveyExternalId}';");
                    }
                    else if (tblName == "Survey")
                    {
                        // Generate DELETE query for the Survey table
                        deleteQueries.Add($"DELETE FROM {tblName} WHERE SurveyIdExternal='{survey.SurveyExternalId}';");
                    }
                    else
                    {
                        // General DELETE query for other tables (try with both survey db id and external id)
                        deleteQueries.Add($"DELETE FROM {tblName} WHERE SurveyId = '{survey.SurveyExternalId}' OR SurveyId = {survey.SurveyId};");
                    }
                }
            }

            await ExecuteQueriesInSpecificDb(deleteQueries, request.DatabasePath);
        }

        private async Task ExecuteQueriesInSpecificDb(List<string> queries, string databasePath)
        {
            try
            {
                using (var conn = new SQLiteConnection($"Data Source={databasePath};"))
                {
                    conn.Open();
                    using (var transaction = conn.BeginTransaction())
                    {
                        using (var command = new SQLiteCommand(conn))
                        {
                            command.Transaction = transaction;

                            foreach (var query in queries)
                            {
                                try
                                {
                                    command.CommandText = query;
                                    command.ExecuteNonQuery();
                                }
                                catch (Exception ex)
                                {
                                    Log.Error($"Error while executing query: {query}, Exception: {ex.Message}");
                                }
                            }
                        }
                        transaction.Commit();
                    }
                }
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Log.Error($"Error in executing queries: {ex.Message}");
            }
        }

        public async Task<IdReply> CheckExistByName(string datasetName)
        {
            try
            {
                var entities = await _repository.GetAllAsync();
                var returnValue = entities.FirstOrDefault(e => e.Name == datasetName);
                if (returnValue != null)
                {
                    // Matching entity found
                    return new IdReply
                    {
                        Id = -1,
                        Message = "Dataset with same name already exists"
                    };
                }
                else
                {
                    // No matching entity found
                    return new IdReply
                    {
                        Id = 0,
                        Message = "Dataset does not exist"
                    };
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error: {ex.Message}");

                // Optionally, throw a gRPC-specific error to return a meaningful message
                throw new RpcException(new Status(StatusCode.Internal, "An error occurred while retrieving the database."));
            }
        }



        public async Task ChangeDataset(string newDataset, CallContext context = default)
        {
            try
            {
                var registries = await _repository.GetAllAsync();
                var registryToUpdate = registries.FirstOrDefault(reg => reg.Name == newDataset);

                if (registryToUpdate != null)
                {
                    // Ensure database schema is up-to-date
                    _logger.LogInformation("Ensuring database schema is up-to-date for database at: {DestinationPath}", registryToUpdate.Path);
                    var migrationStatus = EnsureDatabaseIsUpToDate(registryToUpdate.Path);

                    if (migrationStatus.Result.Id == -1)
                    {
                        _logger.LogError("Database migration failed: {MigrationMessage}", migrationStatus.Result.Message);
                        new IdReply
                        {
                            Id = -1,
                            Message = $"Error updating database schema: {migrationStatus.Result.Message}"
                        };
                    }
                    else
                    {
                        _logger.LogInformation("Database migration successful.");

                        var fieldsToUpdate = new Dictionary<string, string> { { "UpdatedAtActionResult", DateTime.Now.ToString("o") } };
                        await _repository.UpdateSpecificSQLAsync(registryToUpdate, fieldsToUpdate, registryToUpdate.Id);
                    }
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                throw;
            }
        }

        public async Task<ListRequest> ChangeDatasets(DatsetPathRequest datasets, CallContext context = default)
        {
            List<string> updatePathsDataSetqry = new List<string>();
            ListRequest listDatasetsUpdated = new ListRequest { ListData = new List<string>() };
            foreach (string file in datasets.DatsetPaths.ToArray())
            {
                string fileName = Path.Combine(datasets.folderDataSetToChange, Path.GetFileName(file));
                string targetFilePath = Path.Combine(datasets.folderDataSetTarget, fileName);
                string targetFileDsPath = Path.Combine(datasets.folderDataSetTarget, Path.GetFileName(file));
                try
                {
                    if (Path.GetExtension(file).Equals(".db", StringComparison.OrdinalIgnoreCase))
                    {
                        await CloseDatabaseConnection(targetFilePath);
                        Thread.Sleep(50);
                        string dataSetdbFile = targetFileDsPath.Replace("\\", @"\");
                        var temp = $"Update  DatabaseRegistry set Path='{dataSetdbFile}' where Name='{Path.GetFileNameWithoutExtension(file)}';";
                        await ExecuteQueryInSpecificDb($"Update  DatabaseRegistry set Path='{dataSetdbFile}' where Name='{Path.GetFileNameWithoutExtension(file)}';", datasets.DatabasePath);
                        updatePathsDataSetqry.Add(dataSetdbFile);
                    }
                    if (!File.Exists(targetFileDsPath))
                        File.Copy(fileName, targetFileDsPath);
                    Log.Information("DataSet Moved: " + file + " -> " + targetFilePath);
                }
                catch (Exception ex)
                {
                    Log.Error("Error moving the file " + file + ": " + ex.Message);
                    return new ListRequest { ListData = new List<string>() };
                }
            }
            if (updatePathsDataSetqry.Count() > 0) listDatasetsUpdated.ListData = updatePathsDataSetqry;
            return listDatasetsUpdated;
        }

        public async Task<ListRequest> ChangeBackups(BackupPathRequest backups, CallContext context = default)
        {
            List<string> updatePathsDataSetqry = new List<string>();
            ListRequest listDatasetsUpdated = new ListRequest { ListData = new List<string>() };
            foreach (string file in backups.BackupPaths.ToArray())
            {
                string fileName = Path.Combine(backups.folderBackupToChange, Path.GetFileName(file));
                string targetFilePath = Path.Combine(backups.folderBackupTarget, fileName);
                string targetFileDsPath = Path.Combine(backups.folderBackupTarget, Path.GetFileName(file));
                try
                {
                    if (Path.GetExtension(file).Equals(".db", StringComparison.OrdinalIgnoreCase))
                    {
                        await CloseDatabaseConnection(targetFilePath);
                        Thread.Sleep(50);
                        string backUpdbFile = targetFileDsPath.Replace("\\", @"\");
                        var temp = $"Update  Backups set Path='{backUpdbFile}' where Path='{fileName}';";
                        await ExecuteQueryInSpecificDb($"Update  Backups set Path='{backUpdbFile}' where Path='{fileName}';", backups.DatabasePath);
                        updatePathsDataSetqry.Add(backUpdbFile);
                    }
                    File.Copy(fileName, targetFileDsPath);
                    //File.Delete(fileName);


                    Log.Information("DataSet Moved: " + file + " -> " + targetFilePath);

                }
                catch (Exception ex)
                {
                    Log.Error("Error moving the file " + file + ": " + ex.Message);
                    return new ListRequest { ListData = new List<string>() };
                }
            }
            if (updatePathsDataSetqry.Count() > 0) listDatasetsUpdated.ListData = updatePathsDataSetqry;
            return listDatasetsUpdated;
        }

        private async Task ExecuteQueryInSpecificDb(string queryString, string DatabasePath)
        {
            try
            {
                Log.Information("Execute Query In Db");
                using (var conn = new SQLiteConnection($"Data Source={DatabasePath};"))
                {
                    conn.Open();
                    using (var transaction = conn.BeginTransaction())
                    {
                        using (var command = new SQLiteCommand(conn))
                        {
                            command.Transaction = transaction;


                            try
                            {
                                //Log.Information($"Consulta: {query}");
                                //Console.WriteLine($"Consulta: {query}");
                                command.CommandText = queryString;
                                command.ExecuteNonQuery();
                            }
                            catch (Exception ex)
                            {
                                Log.Error($"Error while executing query: {queryString}, Exception: {ex.Message}");
                            }

                        }
                        transaction.Commit();
                    }
                }
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Log.Error($"Error in executing queries: {ex.Message}");
            }
        }
        public async Task<ListRequest> GetAllTableColumns(string tableName, CallContext context = default)
        {
            var columns = new List<string>();

            try
            {
                var databasePathProvider = _serviceProvider.GetRequiredService<DatabasePathProvider>();
                string databasePath = databasePathProvider.GetDatabasePath();

                using var connection = new SQLiteConnection($"Data Source={databasePath};");
                await connection.OpenAsync();

                using var command = connection.CreateCommand();
                command.CommandText = $"PRAGMA table_info({tableName});";

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    string columnName = reader["name"].ToString();
                    if (!string.Equals(columnName, "Id", StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(columnName, "SurveyId", StringComparison.OrdinalIgnoreCase))
                    {
                        columns.Add(columnName);
                    }
                }

                return new ListRequest { ListData = columns };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetAllTableColumns: {ex.Message}");
                return new ListRequest { ListData = new List<string> { $"Error: {ex.Message}" } };
            }
        }

        public async Task ExecuteQueryInDb(List<string> queries)
        {
            try
            {
                int queryExecuted = 0;

                if (String.IsNullOrEmpty(datasetPath))
                {
                    var databasePathProvider = _serviceProvider.GetRequiredService<DatabasePathProvider>();
                    string actualDatabasePath = databasePathProvider.GetDatasetDatabasePath();
                    datasetPath = actualDatabasePath;
                }

                using (SQLiteConnection conn = new SQLiteConnection($"Data Source = {datasetPath};"))
                {
                    conn.Open();
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
                                    Log.Error($"Error in ExecuteQueryInDb while updating data : {ex.Message}");
                                }
                            }
                        }
                        transaction.Commit();
                    }
                    conn.Close();
                }

                Log.Information($"Total : {queries.Count}, Query executed successfully : {queryExecuted}");
            }
            catch (Exception ex)
            {
                Log.Error($"Error in executing query : {ex.Message}");
            }
        }
    }


}
