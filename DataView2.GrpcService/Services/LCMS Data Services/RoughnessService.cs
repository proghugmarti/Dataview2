using DataView2.Core.Models.LCMS_Data_Tables;
using DataView2.Core;
using DataView2.GrpcService.Data;
using DataView2.GrpcService.Interfaces;
using Microsoft.EntityFrameworkCore;
using DataView2.Core.Models;
using Google.Protobuf.WellKnownTypes;
using ProtoBuf.Grpc;
using System.Text.Json;

namespace DataView2.GrpcService.Services.LCMS_Data_Services
{
    public class RoughnessService : BaseService<LCMS_Rough_Processed, IRepository<LCMS_Rough_Processed>>, IRoughnessService
    {
        private readonly AppDbContextProjectData _context;
        IDbContextFactory<AppDbContextProjectData> _dbContextFactory;

        public RoughnessService(IRepository<LCMS_Rough_Processed> repository, IDbContextFactory<AppDbContextProjectData> dbContextFactor) : base(repository)
        {
            _dbContextFactory = dbContextFactor;
            _context = _dbContextFactory.CreateDbContext();
        }

        public async Task<IdReply> HasCwpIRI(Empty empty, CallContext ctx = default)
        {
            var hasCwpIRIData = await _context.LCMS_Rough_Processed.Where(x => x.CwpIRI != null).AnyAsync();
            return new IdReply
            {
                Id = hasCwpIRIData ? 1 : 0
            };
        }

        public async Task<IEnumerable<LCMS_Rough_Processed>> QueryAsync(string predicate)
        {
            try
            {
                var sqlQuery = predicate;
                var lstTables = await _context.LCMS_Rough_Processed.FromSqlRaw(sqlQuery).ToListAsync();

                return lstTables;
            }
            catch (Exception ex)
            {
                Utils.RegError($"Error when execute query: {ex.Message}");
                return new List<LCMS_Rough_Processed>();
            }
        }

        public async Task<CountReply> GetCountAsync(string sqlQuery)
        {
            try
            {
                var count = await _context.LCMS_Rough_Processed.FromSqlRaw(sqlQuery).CountAsync();

                return new CountReply { Count = count };
            }
            catch (Exception ex)
            {
                Utils.RegError($"Error when execute query: {ex.Message}");
                return new CountReply { Count = 0 };
            }
        }

        public async Task<CountReply> GetRecordCount(Empty empty, CallContext context = default)
        {
            var iCount = await _repository.GetRecordCountAsync();

            return new CountReply { Count = iCount };
        }

        public async Task<LCMS_Rough_Processed> UpdateGenericData(string fieldsToUpdateSerialized)
        {
            var entity = new LCMS_Rough_Processed();
            Dictionary<string, object> fieldsToUpdate = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(fieldsToUpdateSerialized);
            entity.Id = Convert.ToInt32(fieldsToUpdate["Id"]);
            return await _repository.UpdateEntityAsync(entity, fieldsToUpdate, entity.Id);
        }

        public async Task<List<LCMS_Rough_Processed>> GetBetweenChainages(ChainagePoints request, CallContext context = default)
        {
            try
            {
                int roundedStart = (int)Math.Round(request.StartChainage);
                int roundedEnd = (int)Math.Round(request.EndChainage);

                var entities = await _repository.Query()
                    .Where(x => x.Chainage >= roundedStart && x.Chainage <= roundedEnd)
                    .ToListAsync();

                if (entities != null)
                {
                    return entities;
                }
            }
            catch (Exception ex)
            {
                Utils.RegError($"Error in GetBetweenChainages: {ex.Message}");
            }

            return new List<LCMS_Rough_Processed>();
        }
    }
}
