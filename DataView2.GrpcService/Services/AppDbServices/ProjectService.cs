using DataView2.Core.Models;
using DataView2.Core.Models.Database_Tables;
using DataView2.Core.Models.LCMS_Data_Tables;
using DataView2.GrpcService.Data;
using DataView2.GrpcService.Data.Projects;
using DataView2.GrpcService.Interfaces;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ProtoBuf.Grpc;
using Serilog;
using System.Diagnostics;


namespace DataView2.GrpcService.Services.AppDbServices
{
    public class ProjectService : IProjectService
    {
        private readonly IRepository<Project> _repository;
        private readonly AppDbContextMetadataLocal _context;
        private readonly IServiceProvider _serviceProvider;

        public ProjectService(IRepository<Project> repository, AppDbContextMetadataLocal appDbContextMetadata,
            IServiceProvider serviceProvider)
        {
            _repository = repository;
            _context = appDbContextMetadata;
            _serviceProvider = serviceProvider;
        }

        public async Task<IdReply> Create(Project request, CallContext context = default)
        {
            try
            {
                if (request.Id != default)
                {
                    request.Id = default;
                }

                var entityEntry = _context.Entry(request);

                if (entityEntry.State == EntityState.Detached)
                {
                    // If detached, explicitly attach the entity
                    _context.Attach(request);
                }
                Console.WriteLine($"Entity State: {entityEntry.State}");

                await _context.SaveChangesAsync();
                request.Id = entityEntry.Entity.Id;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating Project: {ex.Message}");
            }

            return new IdReply
            {
                Id = request.Id,
                Message = "New Project created succesfully."
            };

        }

        public async Task<List<Project>> GetAll(Empty empty, CallContext context = default)
        {
            var entities = await _repository.GetAllAsync();
            return new List<Project>(entities);
        }

        public async Task<Project> GetById(IdRequest request, CallContext context = default)
        {
            int projectId = request.Id;

            try
            {
                return await _repository.GetByIdAsync(projectId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting Project by ID: {ex.Message}");
                throw;
            }
        }

        public async Task<IdReply> RenameProject(Project request, CallContext context = default)
        {
            try
            {
                await _repository.UpdateAsync(request);
                return new IdReply
                {
                    Id = 0,
                    Message = "Project renamed Successfully."
                };
            }
            catch (Exception ex)
            {
                Serilog.Log.Error($"Error in deleting Project : {ex.Message}");
                return new IdReply
                {
                    Id = -1,
                    Message = $"Project deletion is failed as {ex.Message}"
                };
            }
        }

        public async Task<IdReply> DeleteProject(IdRequest request, CallContext context = default)
        {
            try
            {
                // Fetch the project from the repository to check its file location
                var project = await _repository.GetByIdAsync(request.Id);
                if (project == null)
                {
                    return new IdReply
                    {
                        Id = -1,
                        Message = "Project not found."
                    };
                }

                if (File.Exists(project.DBPath))
                {
                    File.Delete(project.DBPath);  // Delete the project GeoJSON file
                }

                // If there are other project-related files or folders, delete them as well
                string projectFolder = Path.GetDirectoryName(project.DBPath);
                if (Directory.Exists(projectFolder))
                {
                    Directory.Delete(projectFolder, true);  // Delete the entire folder and its contents
                }

                // Now delete the project entry from the database
                await _repository.DeleteAsync(request.Id);

                return new IdReply
                {
                    Id = request.Id,
                    Message = "Project deleted successfully."
                };
            }
            catch (Exception ex)
            {
                Console.Out.WriteLineAsync($"Error in deleting project: {ex.Message}");
                return new IdReply
                {
                    Id = -1,
                    Message = $"Project deletion failed: {ex.Message}"
                };
            }
        }
        public async Task<IdReply> DeleteProjectUI(IdRequest request, CallContext context = default)
        {
            try
            {
                // Clear any existing connections
                SqliteConnection.ClearAllPools();

                // Fetch the project from the repository to check its file location
                var project = await _repository.GetByIdAsync(request.Id);
                if (project == null)
                {
                    return new IdReply
                    {
                        Id = -1,
                        Message = "Project not found."
                    };
                }

                // Now delete the project entry from the database
                await _repository.DeleteAsync(request.Id);

                return new IdReply
                {
                    Id = request.Id,
                    Message = "Project deleted successfully."
                };
            }
            catch (Exception ex)
            {
                Console.Out.WriteLineAsync($"Error in deleting project: {ex.Message}");
                return new IdReply
                {
                    Id = -1,
                    Message = $"Project deletion failed: {ex.Message}"
                };
            }
        }



        public async Task<IdReply> CheckExistByName(string projectName)
        {
            try
            {
                var entities = await _repository.GetAllAsync();
                var returnValue = entities.FirstOrDefault(e => e.Name == projectName);
                if (returnValue != null)
                {
                    // Matching entity found
                    return new IdReply
                    {
                        Id = -1,
                        Message = "Project with same name already exists"
                    };
                }
                else
                {
                    // No matching entity found
                    return new IdReply
                    {
                        Id = 0,
                        Message = "Project does not exist"
                    };
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error: {ex.Message}");

                // Optionally, throw a gRPC-specific error to return a meaningful message
                throw new RpcException(new Status(StatusCode.Internal, "An error occurred while retrieving the project."));
            }
        }

        public async Task<Project> GetByName(string projectName, CallContext context = default)
        {

            try
            {
                var entities = await _repository.GetAllAsync();
                if (entities.Count() > 0)
                {
                    var returnValue = entities.FirstOrDefault(e => e.Name == projectName);
                    if (returnValue != null) return returnValue;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting Project by Name: {ex.Message}");
            }
            return new Project();
        }

        public async Task CloseDatabaseConnection(string databasePath)
        {
            try
            {
                SqliteConnection.ClearAllPools();

                if (_context.Database.GetDbConnection() is SqliteConnection sqliteConnection)
                {
                    await sqliteConnection.CloseAsync();
                    await Task.Delay(100); // Ensure OS releases file locks
                }

                Debug.WriteLine($"Database connection closed for {databasePath}.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error when closing database connection: {ex.Message}");
                throw;
            }
        }


        public async Task<Project> GetByIdProject(IdRequestGUID request, CallContext context = default)
        {
            string projectId = request.Id;

            try
            {
                var entities = await _repository.GetAllAsync();
                if (Guid.TryParse(projectId, out Guid guidId))
                {
                    var returnValue = entities.FirstOrDefault(e => e.IdProject == guidId);
                    if(returnValue != null)
                    {
                        return returnValue;
                    }
                }
                else
                {
                    Console.WriteLine("Invalid GUID format.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting Project by IdProject: {ex.Message}");
            }
            return new Project();
        }
        public async Task<string> GetProjectIdFromDb(string dbPath)
        {
            try
            {
                using var connection = new SqliteConnection($"Data Source={dbPath}");
                await connection.OpenAsync();

                var command = connection.CreateCommand();
                command.CommandText = "SELECT IdProject FROM ProjectRegistries LIMIT 1";

                var result = await command.ExecuteScalarAsync();
                return result != null ? result.ToString() : string.Empty;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading IdProject from DB: {ex.Message}");
                return string.Empty;
            }
        }

        public async Task<IdReply> ImportProjectAsync(ImportProjectPath request)
        {
            try
            {
                Log.Information("Starting ImportProjectAsync from {Source} to {Destination}", request.SourceProjectPath, request.DestinationProjectPath);

                var sourceDirectoryPath = request.SourceProjectPath;
                var destinationFolderPath = request.DestinationProjectPath;

                if (!Directory.Exists(sourceDirectoryPath))
                {
                    Log.Warning($"Source directory does not exist: {sourceDirectoryPath}");
                    return new IdReply { Id = -1, Message = "Source directory does not exist" };
                }

                if (!Directory.Exists(destinationFolderPath))
                {
                    Directory.CreateDirectory(destinationFolderPath);
                }

                //if (sourceDirectoryPath != destinationFolderPath)
                //{
                //    //copy all the directories except metadata local db
                //    foreach (var dirPath in Directory.GetDirectories(sourceDirectoryPath, "*", SearchOption.AllDirectories))
                //    {
                //        string targetDirPath = dirPath.Replace(sourceDirectoryPath, destinationFolderPath);
                //        if (!Directory.Exists(targetDirPath)
                //            Directory.CreateDirectory(targetDirPath);
                //    }

                //    foreach (var filePath in Directory.GetFiles(sourceDirectoryPath, "*.*", SearchOption.AllDirectories))
                //    {
                //        string parentDir = Path.GetDirectoryName(filePath);
                //        if (string.Equals(parentDir, sourceDirectoryPath, StringComparison.OrdinalIgnoreCase))
                //        {
                //            // Skip root-level files
                //            continue;
                //        }
                //        string targetFilePath = filePath.Replace(sourceDirectoryPath, destinationFolderPath);
                //        File.Copy(filePath, targetFilePath, overwrite: true);
                //    }
                //}

                string destinationDbPath = Path.Combine(destinationFolderPath, Path.GetFileName(destinationFolderPath) + ".db");

                // Find source .db file
                string sourceDbPath = Directory.GetFiles(sourceDirectoryPath, "*.db").FirstOrDefault();
                if (sourceDbPath == null)
                {
                    Log.Warning("No .db file found in source directory.");
                    return new IdReply { Id = -1, Message = "No database file found in source directory." };
                }

                //if source and destination path is same, no need to copy
                if (sourceDbPath != destinationDbPath)
                {
                    try
                    {
                        File.Copy(sourceDbPath, destinationDbPath, overwrite: true);

                        if (File.Exists(destinationDbPath))
                        {
                            Log.Information("Database file copied and original deleted: {DestinationDbPath}", destinationDbPath);
                        }
                        else
                        {
                            Log.Warning("Copy succeeded but destination file not found: {DestinationDbPath}", destinationDbPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Error copying database file: " + ex.Message);
                    }
                }

                // Step 2: Apply migrations
                var migrationResult = await EnsureDatabaseIsUpToDate(destinationDbPath);
                if (migrationResult.Id < 0)
                {
                    return migrationResult;
                }

                // Step 3: Update ProjectRegistry entries
                var optionsBuilder = new DbContextOptionsBuilder<AppDbContextMetadataLocal>();
                optionsBuilder.UseSqlite($"Data Source={destinationDbPath}");

                var dbPathProvider = _serviceProvider.GetRequiredService<DatabasePathProvider>();
                dbPathProvider.SetMetadataDatabasePath(destinationDbPath);

                using var context = new AppDbContextMetadataLocal(optionsBuilder.Options, dbPathProvider);

                var registry = context.ProjectRegistries.FirstOrDefault();               
                registry.Name = Path.GetFileNameWithoutExtension(destinationDbPath);
                registry.FolderPath = Path.GetDirectoryName(destinationDbPath);
                registry.DBPath = destinationDbPath;

                context.SaveChanges();
                Log.Information("ProjectRegistry entries updated with FolderPath and DbPath.");

                return new IdReply
                {
                    Id = 0,
                    Message = registry.Name
                };
            }
            catch (Exception ex)
            {
                Log.Error("Error during ImportProjectAsync. " + ex.Message);
                return new IdReply
                {
                    Id = -1,
                    Message = $"Error importing project: {ex.Message}"
                };
            }
        }
        public async Task<IdReply> EnsureDatabaseIsUpToDate(string databasePath)
        {
            Log.Information("Starting database migration check for database at: {DatabasePath}", databasePath);

            var optionsBuilder = new DbContextOptionsBuilder<AppDbContextMetadata>();
            optionsBuilder.UseSqlite($"Data Source={databasePath}");

            using var contextProject = new AppDbContextMetadata(optionsBuilder.Options);

            try
            {
                var pendingMigrations = contextProject.Database.GetPendingMigrations();
                if (pendingMigrations.Any())
                {
                    Log.Information("Pending migrations found. Applying...");
                    contextProject.Database.Migrate();
                    Log.Information("Migrations applied successfully.");
                    return new IdReply { Id = 1, Message = "Database migrated to the latest version." };
                }
                else
                {
                    Log.Information("No pending migrations. Database is up-to-date.");
                    return new IdReply { Id = 0, Message = "Database already up-to-date." };
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error during migration: {ex.Message}");
                return new IdReply
                {
                    Id = -1,
                    Message = "Error migrating database"
                };
            }
        }

        public async Task<IdReply> Update(Project request, CallContext context = default)
        {
            try
            {
                await _repository.UpdateAsync(request);
                return new IdReply
                {
                    Id = request.Id,
                    Message = "Updated"
                };
            }
            catch (Exception ex)
            {
                Log.Error($"Error in updating Project: {ex.Message}");
                return new IdReply
                {
                    Id = -1,
                    Message = "Error updating Project"
                };
            }
        }
    }
}
