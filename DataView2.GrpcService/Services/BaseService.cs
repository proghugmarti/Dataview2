using DataView2.Core.Models;
using DataView2.Core.Models.LCMS_Data_Tables;
using DataView2.Core.Models.Other;
using DataView2.GrpcService.Interfaces;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using ProtoBuf.Grpc;
using DataView2.Core;
using Serilog;
using DataView2.Core.Models.Database_Tables;


namespace DataView2.GrpcService.Services
{
    public class BaseService<T, TRepository> where T : class, IEntity where TRepository : IRepository<T>
    {
        protected readonly TRepository _repository;

        public BaseService(TRepository repository)
        {
            _repository = repository;
        }

        public async Task<IdReply> Create(T request, CallContext context = default)
        {
            try
            {
                var entity = await _repository.CreateAsync(request);
                return new IdReply
                {
                    Id = entity.Id,
                    Message = "Created Successfully."
                };
            }
            catch
            (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            return new IdReply { }; 

        }

        public async Task<List<T>> GetAll(Empty empty, CallContext context = default)
        {

            var entities = await _repository.GetAllAsync();
            return new List<T>(entities);

        }

        public async Task<IdReply> HasData(Empty empty, CallContext context = default)
        {
            try
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
            catch
            {
                return new IdReply
                {
                    Id = 0
                };
            }

        }

        public async Task<IdReply> HasDataBySurvey(string surveyId, CallContext context = default)
        {
            var hasServiceData = await _repository.AnyAsync(); // Check if any service data exists

            if (!hasServiceData)
            {
                return new IdReply { Id = 0 }; // No service data found, return 0
            }

            var surveyData = await _repository.FirstOrDefaultAsync(x => x.SurveyId == surveyId); // Find data with the provided surveyId

            if (surveyData != null)
            {
                return new IdReply { Id = 1 }; // Survey data found, return 1
            }
            else
            {
                return new IdReply { Id = 0 }; // No survey data found, return 0
            }
        }

        public async Task<T> GetById(IdRequest request, CallContext context = default)
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

        public async Task<IdReply> DeleteValue(IdRequest request, CallContext context = default)
        {
            try
            {
                var id = request.Id;
                var entity = await _repository.GetByIdAsync(id);
                if (entity != null)
                {
                    await _repository.DeleteAsync(id);
                    return new IdReply
                    {
                        Id = entity.Id,
                        Message = $"Entity with Id {id} deleted"
                    };
                }
                else
                {
                    throw new Exception($"Entity with ID {id} not found");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            return new IdReply { };
        }

        public async Task<T> EditValue(T request, CallContext context = default)
        {
            return await _repository.UpdateAsync(request);
        }

        public async Task<IdReply> DeleteObject(T request, CallContext context = default)
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
            return new IdReply {
                Id = 0,
                Message = "Selected record is failed to be deleted."
            };
        }

        public async Task<IdReply> DeleteAll(Empty empty, CallContext context = default)
        {
            try
            {
                await _repository.DeleteAllAsync();
                return new IdReply
                {
                    Id = 0,
                    Message = "All records deleted successfully."
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            return new IdReply { };
        }


        public async Task<IdReply> Exists(ExistRequest<T> existRequest, CallContext context = default)
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

        public async Task<IdReply> Update(T request, CallContext context = default)
        {
            try
            {
                await _repository.UpdateAsync(request);
                return new IdReply
                {
                    Id = (request as IEntity)?.Id ?? 0,
                    Message = "Updated Successfully."
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return new IdReply { };
            }
        }

        public async Task<List<T>> GetWithinRange(CoordinateRangeRequest coordinateRequest)
        {
            try
            {
                var entities = await _repository.Query()
                    .Where(x => x.RoundedGPSLatitude >= coordinateRequest.MinLatitude &&
                                x.RoundedGPSLatitude <= coordinateRequest.MaxLatitude &&
                                x.RoundedGPSLongitude >= coordinateRequest.MinLongitude &&
                                x.RoundedGPSLongitude <= coordinateRequest.MaxLongitude)
                    .ToListAsync();

                return entities;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return new List<T>();
            }
        }

        public async Task<List<T>> GetBySurveyAndSegment(SurveyAndSegmentRequest request)
        {
            try
            {
                var entities = await _repository.Query()
                    .Where(x => x.SurveyId == request.SurveyId && x.SegmentId == request.SegmentId).ToListAsync();

                return entities;
            }
            catch (Exception ex)
            {
                Log.Error($"Error in BaseService Loading an object type by survey and Segment: {ex.Message}");
                Console.WriteLine($"Error: {ex.Message}");
                return new List<T>();
            }
        }

        public async Task<List<T>> GetBySurvey(SurveyRequest request)
        {
            try
            {
                var entities = await _repository.Query()
                    .Where(x => x.SurveyId == request.SurveyId).ToListAsync();

                return entities;
            }
            catch (Exception ex)
            {
                Log.Error($"Error in BaseService Loading an object type by survey Id: {ex.Message}" );
                Console.WriteLine($"Error: {ex.Message}");
                return new List<T>();
            }
        }
    }

}
