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
    public class SagsBumpsService : BaseService<LCMS_Sags_Bumps, IRepository<LCMS_Sags_Bumps>>, ISagsBumpsService
    {
        private readonly AppDbContextProjectData _context;
        IDbContextFactory<AppDbContextProjectData> _dbContextFactory;
        public SagsBumpsService(IRepository<LCMS_Sags_Bumps> repository, IDbContextFactory<AppDbContextProjectData> dbContextFactor) : base(repository)
        {
            _dbContextFactory = dbContextFactor;
            _context = _dbContextFactory.CreateDbContext();
        }

        public async Task<IEnumerable<LCMS_Sags_Bumps>> QueryAsync(string predicate)
        {
            try
            {
                var sqlQuery = predicate;
                var lstTables = await _context.LCMS_Sags_Bumps.FromSqlRaw(sqlQuery).ToListAsync();

                return lstTables;
            }
            catch (Exception ex)
            {
                Utils.RegError($"Error when execute query: {ex.Message}");
                return new List<LCMS_Sags_Bumps>();
            }
        }

        public async Task<CountReply> GetCountAsync(string sqlQuery)
        {
            try
            {
                var count = await _context.LCMS_Sags_Bumps.FromSqlRaw(sqlQuery).CountAsync();

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

        public async Task<LCMS_Sags_Bumps> UpdateGenericData(string fieldsToUpdateSerialized)
        {
            var entity = new LCMS_Sags_Bumps();
            Dictionary<string, object> fieldsToUpdate = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(fieldsToUpdateSerialized);
            entity.Id = Convert.ToInt32(fieldsToUpdate["Id"]);
            return await _repository.UpdateEntityAsync(entity, fieldsToUpdate, entity.Id);
        }
    }

}
