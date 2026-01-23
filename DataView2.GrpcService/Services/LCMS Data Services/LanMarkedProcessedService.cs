using CsvHelper;
using CsvHelper.Configuration;
using DataView2.Core;
using DataView2.Core.Models;
using DataView2.Core.Models.LCMS_Data_Tables;
using DataView2.Core.Models.Other;
using DataView2.GrpcService.Data;
using DataView2.GrpcService.Interfaces;
using DotSpatial.Data.MiscUtil;
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Maui.Storage;
using ProtoBuf.Grpc;
using Serilog;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using static DataView2.Core.Helper.XMLParser;

namespace DataView2.GrpcService.Services.LCMS_Data_Services
{
    public class LaneMarkedProcessedService : BaseService<LCMS_Lane_Mark_Processed, IRepository<LCMS_Lane_Mark_Processed>>, ILanMarkedProcessedService
    {
        private readonly AppDbContextProjectData _context;

        public LaneMarkedProcessedService(IRepository<LCMS_Lane_Mark_Processed> repository, AppDbContextProjectData context) : base(repository)
        {
            _context = context;
        }

        public async Task<IEnumerable<LCMS_Lane_Mark_Processed>> QueryAsync(string predicate)
        {
            try
            {
                var sqlQuery = predicate;
                var lstTables = await _context.LCMS_Lane_Mark_Processed.FromSqlRaw(sqlQuery).ToListAsync();

                return lstTables;
            }
            catch (Exception ex)
            {
                Utils.RegError($"Error when execute query: {ex.Message}");
                return new List<LCMS_Lane_Mark_Processed>();
            }
        }

        public async Task<CountReply> GetCountAsync(string sqlQuery)
        {
            try
            {
                var count = await _context.LCMS_Lane_Mark_Processed.FromSqlRaw(sqlQuery).CountAsync();

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

        public async Task<LCMS_Lane_Mark_Processed> EditValue(LCMS_Lane_Mark_Processed request, CallContext context = default)
        {
            return await _repository.UpdateAsync(request);
        }

        public async Task<LCMS_Lane_Mark_Processed> UpdateGenericData(string fieldsToUpdateSerialized)
        {
            var entity = new LCMS_Lane_Mark_Processed();
            Dictionary<string, object> fieldsToUpdate = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(fieldsToUpdateSerialized);
            entity.Id = Convert.ToInt32(fieldsToUpdate["Id"]);
            return await _repository.UpdateEntityAsync(entity, fieldsToUpdate, entity.Id);
        }
    }
}
