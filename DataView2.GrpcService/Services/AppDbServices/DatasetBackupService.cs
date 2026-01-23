using System.Diagnostics;
using DataView2.Core.Models;
using DataView2.Core.Models.Database_Tables;
using DataView2.GrpcService.Data;
using DataView2.GrpcService.Data.Projects;
using DataView2.GrpcService.Interfaces;
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;
using ProtoBuf.Grpc;
using Serilog;
using static DataView2.GrpcService.Services.AppDbServices.DatasetBackupService;

namespace DataView2.GrpcService.Services.AppDbServices
{
    public class DatasetBackupService : IDatasetBackupService
    {
        private readonly IRepository<DatasetBackup> _repository;
        private readonly AppDbContextMetadataLocal _context;
        private readonly DatabasePathProvider _databasePathProvider;
        private readonly IServiceProvider _serviceProvider;


        public DatasetBackupService(IRepository<DatasetBackup> repository, AppDbContextMetadataLocal context, DatabasePathProvider databasePathProvider, IServiceProvider serviceProvider)
        {
            _repository = repository;
            _context = context;
            _databasePathProvider = databasePathProvider;
            _serviceProvider = serviceProvider;
        }

        public async Task<IdReply> CreateBackup(NewBackupRequest request, CallContext context = default)
        {
            try
            {
                string sourceFilePath = request.FilePath;
                string backupFolderPath = Path.Combine(Path.GetDirectoryName(sourceFilePath), "Backups");
                CreateBackupFolder(backupFolderPath);

                // Use request.Name to name the backup file
                string sanitizedName = string.Join("_", request.Name.Split(Path.GetInvalidFileNameChars()));
                string uniqueId = Guid.NewGuid().ToString("N").Substring(0, 6); // 6-character unique string
                string backupFileName = $"{sanitizedName}_{uniqueId}.db";
                string backupFilePath = Path.Combine(backupFolderPath, backupFileName);

                Log.Information($"Source file path: {sourceFilePath}");
                Log.Information($"Backup file path: {backupFilePath}");

                await CopyFile(sourceFilePath, backupFilePath);

                // Create entry into backup table
                var databaseRegistryService = _serviceProvider.GetService<IDatabaseRegistryLocalService>();
                string databaseName = Path.GetFileNameWithoutExtension(sourceFilePath);
                var dataset = await databaseRegistryService.GetByName(databaseName);
                if (dataset == null)
                {
                    throw new KeyNotFoundException($"Dataset with name {request.Name} not found.");
                }

                var datasetBackup = new DatasetBackup
                {
                    Name = request.Name,
                    Description = request.Description,
                    Timestamp = DateTime.Now,
                    Path = backupFilePath,
                    DatasetId = dataset.Id
                };

                var entityEntry = await _repository.CreateAsync(datasetBackup);

                // Keep only the 5 most recent backups
                var backupsForDataset = (await _repository.GetAllAsync()).Where(b => b.DatasetId == dataset.Id);
                if (backupsForDataset.Count() > 5)
                {
                    var oldestBackup = backupsForDataset.OrderBy(b => b.Timestamp).First();
                    await DeleteBackup(new BackupActionRequest { BackupId = oldestBackup.Id });
                }

                await _context.SaveChangesAsync();
                await Task.Delay(500);

                return new IdReply
                {
                    Id = entityEntry.Id,
                    Message = $"Backup {request.Name} created."
                };
            }
            catch (Exception ex)
            {
                return new IdReply { Id = 0, Message = $"Failed to create dataset backup : {ex.Message}" };
            }
        }


        private void CreateBackupFolder(string backupFolderPath)
        {
            if (!Directory.Exists(backupFolderPath))
            {
                Directory.CreateDirectory(backupFolderPath);
            }
        }
        private string GetBackupFilePath(string backupFolderPath, string sourceFilePath)
        {
            string fileName = Path.GetFileNameWithoutExtension(sourceFilePath);
            string extension = Path.GetExtension(sourceFilePath);
            string timestamp = DateTime.Now.ToString("dd_MM_HHmm");
            string backupFileName = $"{fileName}_backup_{timestamp}";
            string backupFilePath = Path.Combine(backupFolderPath, $"{backupFileName}{extension}");

            // If a file with the same name exists, add a counter to make it unique
            int counter = 1;
            while (File.Exists(backupFilePath))
            {
                backupFilePath = Path.Combine(backupFolderPath, $"{backupFileName}({counter}){extension}");
                counter++;
            }

            return backupFilePath;
        }

        private async Task CopyFile(string sourceFilePath, string destinationFilePath)
        {
            var databaseRegistryService = _serviceProvider.GetService<IDatabaseRegistryLocalService>();

            await databaseRegistryService.CloseDatabaseConnection(sourceFilePath);
            await databaseRegistryService.CloseDatabaseConnection(destinationFilePath);

            if (File.Exists(sourceFilePath))
            {
                using (var sourceStream = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var destinationStream = new FileStream(destinationFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await sourceStream.CopyToAsync(destinationStream);
                }
                Log.Information($"File copied from {sourceFilePath} to {destinationFilePath}");
                await Task.Delay(300);

            }
            else
            {
                throw new FileNotFoundException("Source file not found", sourceFilePath);
            }
        }

        public async Task RestoreBackup(BackupActionRequest request)
        {
            var backup = await _repository.GetByIdAsync(request.BackupId);
            if (backup == null)
                throw new Exception("Backup not found");

            string backupFilePath = backup.Path;
           // string originalFilePath = _databasePathProvider.GetDatabasePath();
            string originalFilePath = _databasePathProvider.GetDatasetDatabasePath();

            // Output the paths to debug and verify they are correct
            Debug.WriteLine($"Backup Path: {backupFilePath}, Original Path: {originalFilePath}");
           
            await CopyFile(backupFilePath, originalFilePath);

            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    await _context.SaveChangesAsync();
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    throw new Exception("Failed to restore backup. Transaction rolled back.", ex);
                }
            }
        }

       
        public async Task DeleteBackup(BackupActionRequest request)
        {
            var backup = await _repository.GetByIdAsync(request.BackupId);
            if (backup == null)
            {
                throw new KeyNotFoundException($"Backup with ID {request.BackupId} not found.");
            }

            var databaseRegistryService = _serviceProvider.GetService<IDatabaseRegistryLocalService>();
            await databaseRegistryService.CloseDatabaseConnection(backup.Path);

            // Delete the file from the filesystem
            if (File.Exists(backup.Path))
            {
                File.Delete(backup.Path);
            }

            // Remove the backup from the database
            await _repository.DeleteAsync(backup.Id);
            await _context.SaveChangesAsync();

            await databaseRegistryService.CloseDatabaseConnection(backup.Path);

        }

        public async Task<IdReply> HandleBackupImportOption(ImportBackupRequest request)
        {
            try
            {
                var sourceBackupFolder = Path.Combine(request.SourceFolder, "Backups");
                var destinationBackupFolder = Path.Combine(request.DestinationFolder, "Backups");

                if (!Directory.Exists(sourceBackupFolder))
                {
                    Log.Error("Origin folder does not exist: " + sourceBackupFolder);
                    return new IdReply
                    {
                        Id = -1,
                        Message = "Origin folder does not exist: " + sourceBackupFolder
                    };
                }

                if (request.ImportBackup)
                {
                    //if source folder and destination folder is same, no need to copy files
                    if (sourceBackupFolder != destinationBackupFolder)
                    {
                        if (Directory.Exists(destinationBackupFolder))
                        {
                            Directory.Delete(destinationBackupFolder, true);
                            Directory.CreateDirectory(destinationBackupFolder);
                        }
                        else
                        {
                            Directory.CreateDirectory(destinationBackupFolder);
                        }

                        //copy backups 
                        string[] backupFiles = Directory.GetFiles(sourceBackupFolder, "*.db");
                        if (backupFiles != null && backupFiles.Count() > 0)
                        {
                            foreach (var backup in backupFiles)
                            {
                                var newPath = Path.Combine(destinationBackupFolder, Path.GetFileName(backup));
                                File.Copy(backup, newPath);

                                //update backup record in db
                                var existingBackup = await GetBackupByPath(backup);
                                if (existingBackup != null)
                                {
                                    existingBackup.Path = newPath;
                                    await _repository.UpdateAsync(existingBackup);
                                }
                            }
                        }
                    }
                  
                    return new IdReply
                    {
                        Id = 1,
                        Message = "Backups successfully imported"
                    };
                }
                else
                {
                    //remove backup record from db 
                    var backupRecords = await _repository.GetAllAsync();
                    if (backupRecords != null && backupRecords.Count() > 0)
                    {
                        await _repository.DeleteAllAsync();
                    }

                    //delete db if exists in destination folder
                    if (Directory.Exists(destinationBackupFolder))
                    {
                        string[] backupFiles = Directory.GetFiles(destinationBackupFolder, "*.db");
                        if (backupFiles != null && backupFiles.Count() > 0)
                        {
                            foreach (var backup in backupFiles)
                            {
                                File.Delete(backup);
                            }
                        }
                    }

                    return new IdReply
                    {
                        Id = 1,
                        Message = "Backup Records are successfully deleted"
                    };
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error in HandleBackupImportOption: " + ex.Message);
                return new IdReply
                {
                    Id = -1,
                    Message = "Error in handling backup import option"
                };
            }
        }

        public async Task<List<DatasetBackup>> GetAllBackups()
        {
            var backups = await _repository.GetAllAsync();
            return backups.ToList();
        }

        public async Task<List<DatasetBackup>> GetBackupsByDatasetId(DatasetIdRequest request)
        {
            var backups = await _repository.GetAllAsync();
            return backups.Where(b => b.DatasetId == request.DatasetId).ToList();
        }

        public async Task<DatasetBackup> GetBackupByPath(string path)
        {
            var backups = await _repository.GetAllAsync();
            return backups.FirstOrDefault(b => b.Path == path);
        }

        public async Task<DatasetBackup> GetBackupByName(string name)
        {
            var backups = await _repository.GetAllAsync();
            return backups.FirstOrDefault(b => b.Name == name);
        }
    }

}
