using Dapper;
using DataView2.Core;
using DataView2.Core.Communication;
using DataView2.Core.Helper;
using DataView2.Core.Models;
using DataView2.Core.Models.CrackClassification;
using DataView2.Core.Models.ExportTemplate;
using DataView2.Core.Models.LCMS_Data_Tables;
using DataView2.GrpcService.Data;
using DataView2.GrpcService.Interfaces;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ProtoBuf.Grpc;
using System.Data;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Text.Json;
using System.Xml;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;


namespace DataView2.GrpcService.Services
{
    public class OutputColumnTemplateService : IOutputColumnTemplateService
    {
        private readonly IRepository<OutputColumnTemplate> _repository;
        private readonly AppDbContextProjectData _context;

        public OutputColumnTemplateService(IRepository<OutputColumnTemplate> repository, AppDbContextProjectData context)
        {
            _repository = repository;
            _context = context;
        }

        public async Task<List<OutputColumnTemplate>> GetAll(Empty empty, CallContext context = default)
        {
            var entities = await _repository.GetAllAsync();
            return new List<OutputColumnTemplate>(entities);
        }

        public async Task<OutputColumnTemplate> EditValue(OutputColumnTemplate request, CallContext context = default)
        {
            return await _repository.UpdateAsync(request);
        }

		public async Task<CountReply> GetRecordCount(Empty empty, CallContext context = default)
		{
			var iCount = await _repository.GetRecordCountAsync();

			return new CountReply { Count = iCount };
		}

		public async Task<IdReply> DeleteObject(OutputColumnTemplate request, CallContext context = default)
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
            return new IdReply
            {
                Id = request.Id,
                Message = "Selected record is failed to be deleted."
            };

        }

        public async Task<IEnumerable<OutputColumnTemplate>> QueryAsync(string predicate)
        {
            try
            {
                var sqlQuery = predicate;
                var lstTables = await _context.OutputColumnTemplate.FromSqlRaw(sqlQuery).ToListAsync();

                return lstTables;
            }
            catch (Exception ex)
            {
                Utils.RegError($"Error when execute query: {ex.Message}");
                return new List<OutputColumnTemplate>();
            }
        }
    }
}
