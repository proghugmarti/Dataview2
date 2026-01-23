using DataView2.Core.Models;
using DataView2.Core.Models.Other;
using DataView2.Core.Models.Setting;
using DataView2.GrpcService.Data;
using DataView2.GrpcService.Interfaces;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using ProtoBuf.Grpc;
using Serilog;

namespace DataView2.GrpcService.Services
{
    public class SettingsService :  ISettingsService
    {
        private readonly IRepository<GeneralSetting> _repository;
        private readonly AppDbContextMetadata _context;
        private readonly ILogger<SettingsService> _logger;

        public SettingsService(IRepository<GeneralSetting> repository, AppDbContextMetadata context, ILogger<SettingsService> logger) 
        {
            _repository = repository;
            _context = context;
            _logger = logger;
        }
        public async Task<IdReply> Create(GeneralSetting request, CallContext context = default)
        {
            try
            {

                var entityEntry = _repository.CreateAsync(request);

                await _context.SaveChangesAsync();

                return new IdReply
                {
                    Id = entityEntry.Id,
                    Message = "New Setting created successfully."
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating setting: {ex.Message}");
            }

            return new IdReply
            {
                Id = request.Id,
                Message = "Failed to create setting. "
            };
        }
        public async Task<List<GeneralSetting>> GetAllOrCreate(Empty empty, CallContext context = default)
        {
            var entities = await _repository.GetAllAsync();

            //If entities doesn't exist, create a new network setting
            if(!entities.Any())
            {
                //Temporarily use this path for offline map 
                string currentDirectory = Directory.GetCurrentDirectory();
                string mapFilePath = Path.Combine(currentDirectory, "offlineMap.vtpk");

                var newGeneralSetting = new GeneralSetting
                {
                    Name = "Offline Map Path",
                    Description = "Offline Map Path",
                    Type = SettingType.String,
                    Value = mapFilePath,
                    Category = "Path"
                };

                //To return a new setting, put it in the list
                var list = new List<GeneralSetting>();
                await _repository.CreateAsync(newGeneralSetting);
                list.Add(newGeneralSetting);

                return list;
            }

            return entities.ToList();
        }

        public async Task<GeneralSetting> EditValue(GeneralSetting request, CallContext context = default)
        {
            return await _repository.UpdateAsync(request);
        }

        public async Task<List<GeneralSetting>> GetByName(SettingName request, CallContext context = default)
        {
            var name = request.name;

            var entities = await _repository.GetAllAsync();
            if (entities.Any())
            {
                var matchingEntities = entities.Where(entity => entity.Name == name);
                return matchingEntities.ToList();
            }

            return null;
        }

        public async Task<IdReply> DeleteByName(SettingName request, CallContext context = default)
        {
            var name = request.name;

            var entities = await _repository.GetAllAsync();
            if (entities.Any())
            {
                var matchingEntities = entities.Where(entity => entity.Name == name).ToList();
                if (matchingEntities.Any())
                {
                    // Remove each matching entity
                    foreach (var entity in matchingEntities)
                    {
                        await _repository.DeleteAsync(entity.Id);
                    }

                    // Return a successful response with the count of deleted entities
                    return new IdReply { Id = matchingEntities.Count };
                }
            }

            return new IdReply { Id = 0 };
        }
        public Task<LicenseSetting> GetLicense(Empty empty, CallContext context = default)
        {
            try
            {
                string appDirectory = AppContext.BaseDirectory;
                string licensePath = Path.Combine(appDirectory, "License.txt");
                Log.Information("licensePath :" + licensePath);
                if (File.Exists(licensePath))
                {
                    string licenseContent = File.ReadAllText(licensePath);
                    return Task.FromResult(new LicenseSetting { content = licenseContent });
                }
                else
                {
                    return Task.FromResult(new LicenseSetting { content = "No License Found." });
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                return Task.FromResult(new LicenseSetting { content = "Unexpected error ccured" });
            }
            
        }

        public Task<IdReply> UpdateLicense(LicenseSetting request, CallContext context = default)
        {
            try
            {
                var licenseContent = request.content;
                string appDirectory = AppContext.BaseDirectory;
                string licensePath = Path.Combine(appDirectory, "License.txt");
                Log.Information("licensePath :" + licensePath);
                if (File.Exists(licensePath))
                {
                    File.WriteAllText(licensePath, licenseContent);
                    return Task.FromResult(new IdReply { Id = 1, Message = "Successfully updated the license." });
                }
                else
                {
                    return Task.FromResult(new IdReply { Id = 0, Message = "Unexpected error occured." });
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                return Task.FromResult(new IdReply { Id = 0, Message = "Unexpected error occured." });
            }
        }

        public async Task<PavementTypeResponse> ParseSelectedCfgAsync(string fileName)
        {

            if (!File.Exists(fileName))
            {
                throw new FileNotFoundException($"Config file {fileName} not found.");
            }

            var lines = await File.ReadAllLinesAsync(fileName);

            int userDefinedPavementType = -1; // Default value if not found
            int autoPavementTypeDetection = -1;

            foreach (var line in lines)
            {
                if (line.StartsWith("CrackingModule_UserDefinedPavementType"))
                {
                    var parts = line.Split('\t', ' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 1 && int.TryParse(parts[1], out int parsedValue))
                    {
                        userDefinedPavementType = parsedValue;
                    }
                }
                else if (line.StartsWith("CrackingModule_AutoPavementTypeDetection"))
                {
                    var parts = line.Split('\t', ' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 1 && int.TryParse(parts[1], out int parsedValue))
                    {
                        autoPavementTypeDetection = parsedValue;
                    }
                }
            }

            return new PavementTypeResponse
            {
                UserDefinedPavementType = userDefinedPavementType,
                AutoPavementTypeDetection = autoPavementTypeDetection
            };
        }

    }
}
