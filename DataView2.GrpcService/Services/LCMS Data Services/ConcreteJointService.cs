using DataView2.Core;
using DataView2.Core.Communication;
using DataView2.Core.Models;
using DataView2.Core.Models.CrackClassification;
using DataView2.Core.Models.LCMS_Data_Tables;
using DataView2.GrpcService.Data;
using DataView2.GrpcService.Interfaces;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using ProtoBuf.Grpc;
using System.Text.Json;
using System.Xml;
using Newtonsoft;

namespace DataView2.GrpcService.Services.LCMS_Data_Services
{
    public class ConcreteJointService: BaseService<LCMS_Concrete_Joints, IRepository<LCMS_Concrete_Joints>>, IConcreteJointService 
    {

        private readonly AppDbContextProjectData _context;
        IDbContextFactory<AppDbContextProjectData> _dbContextFactory;

        public ConcreteJointService(IRepository<LCMS_Concrete_Joints> repository, IDbContextFactory<AppDbContextProjectData> dbContextFactor) : base(repository)
        {
            _dbContextFactory = dbContextFactor;
            _context = _dbContextFactory.CreateDbContext();
        }

		public async Task<CountReply> GetRecordCount(Empty empty, CallContext context = default)
		{
			var iCount = await _repository.GetRecordCountAsync();

			return new CountReply { Count = iCount };
		}
        public async Task SaveToDatabase(List<LCMS_Concrete_Joints> entities, CancellationToken cancellation)
        {
            if (cancellation.IsCancellationRequested)
            {
                return;
            }

            await _repository.CreateRangeAsync(entities);
        }

        public async Task<IEnumerable<LCMS_Concrete_Joints>> QueryAsync(string predicate)
        {
            try
            {
                var sqlQuery = predicate;
                var lstTables = await _context.LCMS_Concrete_Joints.FromSqlRaw(sqlQuery).ToListAsync();

                return lstTables;
            }
            catch (Exception ex)
            {
                Utils.RegError($"Error when execute query: {ex.Message}");
                return new List<LCMS_Concrete_Joints>();
            }
        }
        public async Task<CountReply> GetCountAsync(string sqlQuery)
        {
            try
            {
                var count = await _context.LCMS_Concrete_Joints.FromSqlRaw(sqlQuery).CountAsync();

                return new CountReply { Count = count };
            }
            catch (Exception ex)
            {
                Utils.RegError($"Error when execute query: {ex.Message}");
                return new CountReply { Count = 0 };
            }
        }

        public async Task<IEnumerable<int>> QueryIdsAsync(string predicate)
        {
            try
            {
                var sqlQuery = predicate;
                var ids = await _context.LCMS_Concrete_Joints.FromSqlRaw(sqlQuery)
                                                          .Select(item => item.Id)
                                                          .ToListAsync();

                return ids;
            }
            catch (Exception ex)
            {
                Utils.RegError($"Error when executing query: {ex.Message}");
                return new List<int>();
            }
        }

        public async Task<LCMS_Concrete_Joints> UpdateGenericData(string fieldsToUpdateSerialized)
        {
            var entity = new LCMS_Concrete_Joints();
            Dictionary<string, object> fieldsToUpdate = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(fieldsToUpdateSerialized);
            entity.Id = Convert.ToInt32(fieldsToUpdate["Id"]);
            return await _repository.UpdateEntityAsync(entity, fieldsToUpdate, entity.Id);
        }
    }
}
