using DataView2.Core.Models;
using DataView2.Core.Models.Database_Tables;
using DataView2.Core.Models.ExportTemplate;
using DataView2.Core.Models.LCMS_Data_Tables;
using DataView2.GrpcService.Data;
using DataView2.GrpcService.Interfaces;
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;
using DataView2.Core.Helper;
using Microsoft.UI.Xaml.Media;
using ProtoBuf.Grpc;
using Grpc.Core;
using Serilog;

namespace DataView2.GrpcService.Services.AppDbServices
{
    public class ProjectRegistryService : IProjectRegistryService
    {
        private readonly IRepository<ProjectRegistry> _repository;
        private readonly AppDbContextMetadata _context;

        private readonly IServiceProvider _serviceProvider;
        public ProjectRegistryService(IRepository<ProjectRegistry> repository, AppDbContextMetadata appDbContextMetadata, IServiceProvider serviceProvider)
        {
            _repository = repository;
            _context = appDbContextMetadata;
            _serviceProvider = serviceProvider;
        }

        public async Task<IdReply> Create(ProjectRegistry request, CallContext context = default)
        {
            try
            {
                //var entityEntry = _context.Entry(request);

                //if (entityEntry.State == EntityState.Detached)
                //{
                //    // If detached, explicitly attach the entity
                //    _context.Attach(request);
                //}

                //if (entityEntry.State == EntityState.Detached || entityEntry.State == EntityState.Added)
                //{
                //    _context.Add(request);
                //}

                //await _context.SaveChangesAsync();

                //var generatedId = request.Id;

                var entity = await _repository.CreateAsync(request);
                string pathProject = request.FolderPath;
                string newPath = Path.Combine(pathProject, $"{request.Name}.db");

                using (var contextDb = CreateDbContextMetadata(newPath))
                {
                    contextDb.Database.Migrate();
                }

                return new IdReply
                {
                    Id = entity.Id,
                    Message = "New Project created successfully."
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating Project: {ex.Message}");
                return new IdReply
                {
                    Id = -1,
                    Message = $"Error creating Project: {ex.Message}"
                };
            }
        }

        public async Task<List<ProjectRegistry>> GetAll(Empty empty, CallContext context = default)
        {
            var entities = await _repository.GetAllAsync();
            return new List<ProjectRegistry>(entities);
        }


        public async Task<ProjectRegistry> GetById(IdRequest request, CallContext context = default)
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

        public async Task<IdReply> RenameProject(ProjectRegistry request, CallContext context = default)
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

        /// <summary>
        //
        /// 
        ///  
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
                var path = project.DBPath;

                // Delete the project files from local storage
                if (Directory.Exists(project.FolderPath))
                {
                    Directory.Delete(project.FolderPath, true);  // Deletes the entire folder and its contents
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
                Log.Error($"Error in deleting project: {ex.Message}");
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

        public AppDbContextMetadata CreateDbContextMetadata(string databasePath)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContextMetadata>();
            optionsBuilder.UseSqlite($"Data Source={databasePath}");
            return new AppDbContextMetadata(optionsBuilder.Options);
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

        public async Task<IdReply> Import(ProjectRegistry request, CallContext context = default)
        {
            try
            {
                var entityEntry = _context.Entry(request);
                if (entityEntry.State == EntityState.Detached)
                {
                    _context.Attach(request);
                }

                if (entityEntry.State == EntityState.Detached || entityEntry.State == EntityState.Added)
                {
                    _context.Add(request);
                }

                await _context.SaveChangesAsync();

                var generatedId = request.Id;

                return new IdReply
                {
                    Id = generatedId,
                    Message = "New Project imported successfully."
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error importing Project: {ex.Message}");
                return new IdReply
                {
                    Id = 0,
                    Message = $"Error importing Project: {ex.Message}"
                };
            }
        }
    }
}
