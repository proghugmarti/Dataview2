using DataView2.Core;
using DataView2.Core.Communication;
using DataView2.Core.Models;
using DataView2.Core.Models.CrackClassification;
using DataView2.Core.Models.ExportTemplate;
using DataView2.Core.Models.LCMS_Data_Tables;
using DataView2.GrpcService.Data;
using DataView2.GrpcService.Interfaces;
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Newtonsoft.Json;
using ProtoBuf.Grpc;
using System.Text.Json;
using System.Xml;

namespace DataView2.GrpcService.Services.LCMS_Data_Services
{
    public class BleedingService : BaseService<LCMS_Bleeding, IRepository<LCMS_Bleeding>>, IBleedingService
    {
        private readonly AppDbContextProjectData _context;
        IDbContextFactory<AppDbContextProjectData> _dbContextFactory;


        public BleedingService(IRepository<LCMS_Bleeding> repository, IDbContextFactory<AppDbContextProjectData> dbContextFactor) : base(repository)
        {
            _dbContextFactory = dbContextFactor;
            _context = _dbContextFactory.CreateDbContext();
        }

        public double getDoubleFromNode(XmlNode doc, string fieldName)
        {
            double result = 0;

            var xmlselNode = doc.SelectSingleNode(fieldName);

            if (xmlselNode != null)
            {
                result = Math.Round(Convert.ToDouble(xmlselNode.InnerText), 2);
            }

            return result;
        }

		public async Task<CountReply> GetRecordCount(Empty empty, CallContext context = default)
		{
			var iCount = await _repository.GetRecordCountAsync();

			return new CountReply { Count = iCount };
		}

		public async Task<IEnumerable<LCMS_Bleeding>> QueryAsync(string predicate)
        {
            try
            {
                var sqlQuery = predicate;
                var lstTables = await _context.LCMS_Bleeding.FromSqlRaw(sqlQuery).ToListAsync();

                return lstTables;
            }
            catch (Exception ex)
            {
                Utils.RegError($"Error when execute query: {ex.Message}");
                return new List<LCMS_Bleeding>();
            }
        }

        public async Task<CountReply> GetCountAsync(string sqlQuery)
        {
            try
            {
                var count = await _context.LCMS_Bleeding.FromSqlRaw(sqlQuery).CountAsync();

                return new CountReply { Count = count };
            }
            catch (Exception ex)
            {
                Utils.RegError($"Error when execute query: {ex.Message}");
                return new CountReply { Count = 0 };
            }
        }

        public async Task<IdReply> HasData(Empty empty, CallContext context = default)
        {
            try
            {
                var query = _repository.Query().Where(x => x.LeftSeverity != "No Bleeding" || x.RightSeverity != "No Bleeding");
                var hasData = await query.AnyAsync();
                return new IdReply
                {
                    Id = hasData ? 1 : 0
                };
            }
            catch
            {
                return new IdReply
                {
                    Id = 0
                };
            }

        }

        public async Task<LCMS_Bleeding> UpdateGenericData(string fieldsToUpdateSerialized)
        {
            var entity = new LCMS_Bleeding();
            Dictionary<string, object> fieldsToUpdate = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(fieldsToUpdateSerialized);
            entity.Id = Convert.ToInt32(fieldsToUpdate["Id"]);
            return await _repository.UpdateEntityAsync(entity, fieldsToUpdate, entity.Id);
        }
    }
}
