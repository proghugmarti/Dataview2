using DataView2.Core;
using DataView2.Core.Models;
using DataView2.Core.Models.CrackClassification;
using DataView2.Core.Models.LCMS_Data_Tables;
using DataView2.Core.Models.Other;
using DataView2.GrpcService.Data;
using DataView2.GrpcService.Interfaces;
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Operation;
using ProtoBuf.Grpc;
using System.Xml.Linq;
using static Azure.Core.HttpHeader;

namespace DataView2.GrpcService.Services.OtherServices
{
    public class BoundariesService : IBoundariesService
    {
        private readonly IRepository<Boundary> _repository;
        private readonly AppDbContextProjectData _context;
        public BoundariesService(IRepository<Boundary> repository, AppDbContextProjectData context)
        {
            _repository = repository;
            _context = context;
        }

        public async Task<IdReply> Create(Boundary request, CallContext context = default)
        {
            try
            {
                //if the same boundary with same surveyId, surveyName and coordinates, don't save 
                var existingBoundary = await _repository.FirstOrDefaultAsync(x => x.SurveyId == request.SurveyId && x.SurveyName == request.SurveyName &&
                                                                x.Coordinates == request.Coordinates && x.BoundariesMode == request.BoundariesMode);
                if(existingBoundary != null)
                {
                    return new IdReply
                    {
                        Id = -1,
                        Message = "Same boundary already exists"
                    };
                }
                else
                {
                    var entityEntry = await _repository.CreateAsync(request);

                    return new IdReply
                    {
                        Id = entityEntry.Id,
                        Message = "New Boundaries created."
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new IdReply
                {
                    Id = -1,
                    Message = $"Error in Creating Boundary."
                };
            }
        }

        public async Task<IdReply> Edit(Boundary request, CallContext context = default)
        {
            try
            {
                var sameBoundaryName = await _repository.FirstOrDefaultAsync(b => b.BoundaryName == request.BoundaryName);

                if (sameBoundaryName != null)
                {
                    return new IdReply
                    {
                        Id = -1,
                        Message = "Boundary with the same name exists. Please rename it"
                    };
                }

                await _repository.UpdateAsync(request);
                return new IdReply
                {
                    Id = request.Id,
                    Message = $"Boundary Id {request.Id} successfully edited."
                };
            }
            catch (Exception ex) 
            {
                Console.WriteLine(ex.Message);
                return new IdReply
                {
                    Id = -1,
                    Message = $"Error in Editing Boundary Id {request.Id}"
                };
            }
        }

        public async Task<List<Boundary>> GetAll(Empty empty, CallContext context = default)
        {
            try
            {
                var entities = await _repository.GetAllAsync();
                return entities.ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new List<Boundary>();
            }
        }

        public async Task<Boundary> GetById(IdRequest request, CallContext context = default)
        {
            var entity = await _repository.GetByIdAsync(request.Id);
            return entity;
        }

        public async Task<Boundary> GetBySurveyId(string surveyId, CallContext context = default)
        {
            var entities = await _repository.GetAllAsync();
            if (entities.Any())
            {
                var matchingEntity = entities.FirstOrDefault(entity => entity.SurveyId == surveyId);
                if (matchingEntity != null)
                {
                    return matchingEntity;
                }
            }
            return new Boundary();
        }

        public async Task<CountReply> GetRecordCount(Empty empty, CallContext context = default)
        {
            var iCount = await _repository.GetRecordCountAsync();
            return new CountReply { Count = iCount };
        }

        public async Task<IdReply> HasData(Empty empty, CallContext context = default)
        {
            var boundaries = await _repository.GetAllAsync();
            var hasData = boundaries.Any();
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
        public async Task<IEnumerable<Boundary>> QueryAsync(string predicate)
        {
            try
            {
                var sqlQuery = predicate;
                var lstTables = await _context.Boundary.FromSqlRaw(sqlQuery).ToListAsync();

                return lstTables;
            }
            catch (Exception ex)
            {
                Utils.RegError($"Error when execute query: {ex.Message}");
                return new List<Boundary>();
            }
        }

        public async Task<CountReply> GetCountAsync(string sqlQuery)
        {
            try
            {
                var count = await _context.Boundary.FromSqlRaw(sqlQuery).CountAsync();

                return new CountReply { Count = count };
            }
            catch (Exception ex)
            {
                Utils.RegError($"Error when execute query: {ex.Message}");
                return new CountReply { Count = 0 };
            }
        }

        public async Task<Boundary> EditValue(Boundary request, CallContext context = default)
        {
            return await _repository.UpdateAsync(request);
        }

        public async Task<IdReply> DeleteObject(Boundary request, CallContext context = default)
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

        public async Task<Boundary> UpdateGenericData(string fieldsToUpdateSerialized)
        {
            var entity = new Boundary();
            Dictionary<string, object> fieldsToUpdate = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(fieldsToUpdateSerialized);
            entity.Id = Convert.ToInt32(fieldsToUpdate["Id"]);
            return await _repository.UpdateEntityAsync(entity, fieldsToUpdate, entity.Id);
        }
    }
}
