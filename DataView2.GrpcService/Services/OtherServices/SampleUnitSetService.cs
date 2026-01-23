using DataView2.Core.Models;
using DataView2.Core.Models.LCMS_Data_Tables;
using DataView2.Core.Models.Other;
using DataView2.GrpcService.Data;
using DataView2.GrpcService.Interfaces;
using Google.Protobuf.WellKnownTypes;
using ProtoBuf.Grpc;
using Serilog;
using System.Data.Entity;

namespace DataView2.GrpcService.Services.OtherServices
{
    public class SampleUnitSetService : ISampleUnitSetService
    {
        private readonly IRepository<SampleUnit_Set> _repository;
        private readonly AppDbContextProjectData _context;
        public SampleUnitSetService(IRepository<SampleUnit_Set> repository, AppDbContextProjectData context)
        {
            _repository = repository;
            _context = context;
        }
        public async Task<IdReply> Create(SampleUnit_Set request, CallContext context = default)
        {
            try
            {
                var sameSampleUnitSet = await _repository.FirstOrDefaultAsync(x => x.Name == request.Name);
                if (sameSampleUnitSet != null)
                {
                    return new IdReply { Id = -1, Message = "Same name already exists. Please choose a different name" };
                }
                else
                {
                    var entity = await _repository.CreateAsync(request);
                    if (entity != null)
                    {
                        return new IdReply { Id = entity.Id, Message = "Successfully created" };
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error: {ex.Message}");
            }
            return new IdReply
            {
                Id = -1,
                Message = "Failed in Creating Sample Unit Set"
            };
        }

        public async Task<IdReply> GetOrCreate(SampleUnit_Set sampleUnitSet)
        {
            try
            {
                var existingEntity = _context.SampleUnit_Set.FirstOrDefault(x => x.Name == sampleUnitSet.Name && x.Type == sampleUnitSet.Type);
                if (existingEntity != null)
                {
                    return new IdReply
                    {
                        Id = existingEntity.Id
                    };
                }
                else
                {
                    var newEntity = await _repository.CreateAsync(sampleUnitSet);
                    return new IdReply
                    {
                        Id = newEntity.Id
                    };
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error: {ex.Message}");
            }

            return new IdReply
            {
                Id = -1
            };
        }

        public async Task<List<SampleUnit_Set>> GetAll(Empty empty, CallContext context = default)
        {
            try
            {
                // Retrieve all SampleUnitSets
                var sampleUnitSets = _context.SampleUnit_Set.ToList();

                if (sampleUnitSets.Any())
                {
                    foreach (var set in sampleUnitSets)
                    {
                        await _context.Entry(set).Collection(s => s.SampleUnits).LoadAsync(); // Load Boundaries for each set
                    }
                }

                return sampleUnitSets;
            }
            catch (Exception ex)
            {
                Log.Error($"Error: {ex.Message}");
            }
            return new List<SampleUnit_Set>();
        }

        public async Task<SampleUnit_Set> GetByName(string name, CallContext context = default)
        {
            try
            {
                // Retrieve all SampleUnitSets
                var sampleUnitSet = _context.SampleUnit_Set.Where(set => set.Name == name).FirstOrDefault();

                // Manually fetch Boundaries for each SampleUnitSet

                if (sampleUnitSet != null)
                {
                    // Explicitly load boundaries
                    await _context.Entry(sampleUnitSet).Collection(s => s.SampleUnits).LoadAsync();
                }

                return sampleUnitSet;
            }
            catch (Exception ex)
            {
                Log.Error($"Error: {ex.Message}");
            }
            return new SampleUnit_Set();
        }

        public async Task<SampleUnit_Set> GetById(IdRequest request, CallContext context = default)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(request.Id);

                if (entity != null)
                {
                    await _context.Entry(entity).Collection(s => s.SampleUnits).LoadAsync();
                }
                return entity;
            }
            catch (Exception ex)
            {
                Log.Error($"Error: {ex.Message}");
            }
            return new SampleUnit_Set();
        }

        public async Task<IdReply> DeleteObject(SampleUnit_Set request, CallContext context = default)
        {
            try
            {
                await _repository.DeleteAsync(request.Id);
                return new IdReply
                {
                    Id = request.Id,
                    Message = "Selecteed record deleted successfully."
                };
            }
            catch (Exception ex)
            {
                Log.Error($"Error: {ex.Message}");
            }
            return new IdReply
            {
                Id = -1,
                Message = "Selecteed record is failed to be deleted."
            };
        }

        public async Task<IdReply> DeleteRange(List<SampleUnit_Set> request, CallContext context = default)
        {
            try
            {
                foreach (var sampleUnitSet in request)
                {
                    var response = await DeleteObject(sampleUnitSet);
                }
                return new IdReply
                {
                    Id = 0,
                    Message = "Successfully deleted"
                };
            }
            catch (Exception ex)
            {
                Log.Error($"Error: {ex.Message}");
            }
            return new IdReply
            {
                Id = -1,
                Message = "Selecteed record is failed to be deleted."
            };
        }
    }
}
