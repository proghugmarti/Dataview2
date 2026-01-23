using DataView2.Core.Models.LCMS_Data_Tables;
using DataView2.Core;
using DataView2.GrpcService.Data;
using DataView2.GrpcService.Interfaces;
using Microsoft.EntityFrameworkCore;
using DataView2.Core.Models;
using Google.Protobuf.WellKnownTypes;
using ProtoBuf.Grpc;

namespace DataView2.GrpcService.Services.LCMS_Data_Services
{
    public class ShoveService : BaseService<LCMS_Shove_Processed, IRepository<LCMS_Shove_Processed>>, IShoveService
    {
        private readonly AppDbContextProjectData _context;
        IDbContextFactory<AppDbContextProjectData> _dbContextFactory;

        public ShoveService(IRepository<LCMS_Shove_Processed> repository, IDbContextFactory<AppDbContextProjectData> dbContextFactor) : base(repository)
        {
            _dbContextFactory = dbContextFactor;
            _context = _dbContextFactory.CreateDbContext();
        }

        public async Task<IEnumerable<LCMS_Shove_Processed>> QueryAsync(string predicate)
        {
            try
            {
                var sqlQuery = predicate;
                var lstTables = await _context.LCMS_Shove_Processed.FromSqlRaw(sqlQuery).ToListAsync();

                return lstTables;
            }
            catch (Exception ex)
            {
                Utils.RegError($"Error when execute query: {ex.Message}");
                return new List<LCMS_Shove_Processed>();
            }
        }

        public async Task<CountReply> GetCountAsync(string sqlQuery)
        {
            try
            {
                var count = await _context.LCMS_Shove_Processed.FromSqlRaw(sqlQuery).CountAsync();

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

        public async Task<LCMS_Shove_Processed> UpdateGenericData(string fieldsToUpdateSerialized)
        {
            var entity = new LCMS_Shove_Processed();
            Dictionary<string, object> fieldsToUpdate = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(fieldsToUpdateSerialized);
            entity.Id = Convert.ToInt32(fieldsToUpdate["Id"]);
            return await _repository.UpdateEntityAsync(entity, fieldsToUpdate, entity.Id);
        }
    }
}
