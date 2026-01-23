using Azure.Core;
using DataView2.Core.Models;
using DataView2.Core.Models.LCMS_Data_Tables;
using DataView2.Core.Models.Other;
using DataView2.GrpcService.Data;
using DataView2.GrpcService.Interfaces;
using Google.Protobuf.WellKnownTypes;
using ProtoBuf.Grpc;
using System.Data.Entity;

namespace DataView2.GrpcService.Services.OtherServices
{
    public class SampleUnitService : ISampleUnitService
    {
        private readonly IRepository<SampleUnit> _repository;
        private readonly AppDbContextProjectData _context;
        public SampleUnitService(IRepository<SampleUnit> repository, AppDbContextProjectData context)
        {
            _repository = repository;
            _context = context;
        }

        public async Task<List<SampleUnit>> GetAll(Empty empty, CallContext context = default)
        {
            var entities = await _repository.GetAllAsync();
            return entities.ToList();
        }

        public async Task<List<SampleUnit>> GetBySampleUnitSet(IdRequest sampleUnitSetId, CallContext context = default)
        {
            try
            {
                var entities = await _repository.GetAllAsync();
                if (entities != null && entities.Count() > 0)
                {
                    var matchingEntities = entities.Where(x => x.SampleUnitSetId == sampleUnitSetId.Id).ToList();
                    if(matchingEntities != null && matchingEntities.Count > 0)
                    {
                        return matchingEntities;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in GetBySampleUnitSet : " + ex.Message);
            }
            return new List<SampleUnit>();
        }

        public async Task<SampleUnit> GetById(IdRequest request, CallContext context = default)
        {
            var entity = await _repository.GetByIdAsync(request.Id);
            return entity;
        }

        public async Task<IdReply> Create(SampleUnit request, CallContext context = default)
        {
            try
            {
                var sameBoundaryName = await _repository.FirstOrDefaultAsync(b => b.Name == request.Name && b.SampleUnitSetId == request.SampleUnitSetId);

                if (sameBoundaryName != null)
                {
                    return new IdReply
                    {
                        Id = -1,
                        Message = "Sample Unit with the same name exists in the same set. Please rename it."
                    };
                }

                var entity = await _repository.CreateAsync(request);

                if (entity != null)
                {
                    return new IdReply
                    {
                        Id = entity.Id,
                        Message = "Sample Unit has been successfully saved."
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in creating sample unit" + ex.Message);
            }

            return new IdReply
            {
                Id = -1,
                Message = "Error occured. Sample Unit could not be saved."
            };
        }

        public async Task<IdReply> GetOrCreate(SampleUnit sampleUnit)
        {
            var sampleUnitName = sampleUnit.Name;
            var existingSampleUnit = _context.SampleUnit.FirstOrDefault(x => x.Name == sampleUnitName && x.Coordinates == sampleUnit.Coordinates);
            if (existingSampleUnit != null && existingSampleUnit.Coordinates != null)
            {
                //exists
                return new IdReply
                {
                    Id = existingSampleUnit.Id
                };
            }
            else
            {
                // No existing boundary, create a new one
                _context.SampleUnit.Add(sampleUnit);
                await _context.SaveChangesAsync();

                return new IdReply
                {
                    Id = sampleUnit.Id
                };
            }
        }

        public async Task<IdReply> DeleteObject(SampleUnit request, CallContext context = default)
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
                Console.WriteLine($"Error: {ex.Message}");
            }
            return new IdReply
            {
                Id = -1,
                Message = "Selecteed record is failed to be deleted."
            };

        }

        public async Task<SampleUnit> EditValue(SampleUnit request)
        {
            try
            {
                var updated = await _repository.UpdateAsync(request);
                return updated;

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return new SampleUnit();
            }
        }
    }
}
