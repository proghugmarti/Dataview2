using DataView2.Core;
using DataView2.Core.Communication;
using DataView2.Core.Models;
using DataView2.Core.Models.CrackClassification;
using DataView2.Core.Models.ExportTemplate;
using DataView2.Core.Models.LCMS_Data_Tables;
using DataView2.GrpcService.Data;
using DataView2.GrpcService.Interfaces;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using ProtoBuf.Grpc;
using System.Text.Json;
using System.Xml;

namespace DataView2.GrpcService.Services.LCMS_Data_Services
{
    public class RavellingRawService: BaseService<LCMS_Ravelling_Raw, IRepository<LCMS_Ravelling_Raw>>, IRavellingService 
    {
        private readonly AppDbContextProjectData _context;
        IDbContextFactory<AppDbContextProjectData> _dbContextFactory;
        public RavellingRawService(IRepository<LCMS_Ravelling_Raw> repository, IDbContextFactory<AppDbContextProjectData> dbContextFactor) : base(repository)
        {
            _dbContextFactory = dbContextFactor;
            _context = _dbContextFactory.CreateDbContext();
        }

        public string DetermineSeverity(double ravellingIndex, double lowThreshold, double medThreshold, double highThreshold)
        {
            if (ravellingIndex > lowThreshold && ravellingIndex < medThreshold)
            {
                return "Low";
            }
            else if (ravellingIndex > medThreshold && ravellingIndex < highThreshold)
            {
                return "Med";
            }
            else if (ravellingIndex > highThreshold)
            {
                return "High";
            }
            else
            {
                return string.Empty;
            }
        }

		public async Task<CountReply> GetRecordCount(Empty empty, CallContext context = default)
		{
			var iCount = await _repository.GetRecordCountAsync();

			return new CountReply { Count = iCount };
		}
		public async Task<IEnumerable<LCMS_Ravelling_Raw>> QueryAsync(string predicate)
        {
            try
            {
                var sqlQuery = predicate;
                var lstTables = await _context.LCMS_Ravelling_Raw.FromSqlRaw(sqlQuery).ToListAsync();

                return lstTables;
            }
            catch (Exception ex)
            {
                Utils.RegError($"Error when execute query: {ex.Message}");
                return new List<LCMS_Ravelling_Raw>();
            }
        }

        public async Task<IEnumerable<int>> QueryIdsAsync(string predicate)
        {
            try
            {
                var sqlQuery = predicate;
                var ids = await _context.LCMS_Ravelling_Raw.FromSqlRaw(sqlQuery)
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

        public async Task<CountReply> GetCountAsync(string sqlQuery)
        {
            try
            {
                var count = await _context.LCMS_Ravelling_Raw.FromSqlRaw(sqlQuery).CountAsync();

                return new CountReply { Count = count };
            }
            catch (Exception ex)
            {
                Utils.RegError($"Error when execute query: {ex.Message}");
                return new CountReply { Count = 0 };
            }
        }

        public async Task<LCMS_Ravelling_Raw> UpdateGenericData(string fieldsToUpdateSerialized)
        {
            var entity = new LCMS_Ravelling_Raw();
            Dictionary<string, object> fieldsToUpdate = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(fieldsToUpdateSerialized);
            entity.Id = Convert.ToInt32(fieldsToUpdate["Id"]);
            return await _repository.UpdateEntityAsync(entity, fieldsToUpdate, entity.Id);
        }
    }
}
