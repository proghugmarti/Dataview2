using DataView2.Core;
using DataView2.Core.Communication;
using DataView2.Core.Models;
using DataView2.Core.Models.CrackClassification;
using DataView2.Core.Models.ExportTemplate;
using DataView2.Core.Models.LCMS_Data_Tables;
using DataView2.Core.Models.Setting;
using DataView2.GrpcService.Data;
using DataView2.GrpcService.Interfaces;
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;
using ProtoBuf.Grpc;
using System.Text.Json;
using System.Xml;

namespace DataView2.GrpcService.Services
{
	public class OutputTemplateService : IOutputTemplateService
	{
		private readonly IRepository<OutputTemplate> _repository;
        private readonly AppDbContextProjectData _context;

        public OutputTemplateService(IRepository<OutputTemplate> repository, AppDbContextProjectData context)
		{
			_repository = repository;
            _context = context;
		}

        public async Task<List<OutputTemplate>> GetAll(Empty empty, CallContext context = default)
        {
            var entities = await _repository.GetAllAsync();
            return new List<OutputTemplate>(entities);
        }

		public async Task<CountReply> GetRecordCount(Empty empty, CallContext context = default)
		{
			var iCount = await _repository.GetRecordCountAsync();

			return new CountReply { Count = iCount };
		}
		public async Task<OutputTemplate> EditValue(OutputTemplate request, CallContext context = default)
        {
            return await _repository.UpdateAsync(request);
        }

        public async Task<IdReply> DeleteObject(OutputTemplate request, CallContext context = default)
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
                Id = request.Id,
                Message = "Selecteed record is failed to be deleted."
            };

        }

        public async Task<IEnumerable<OutputTemplate>> QueryAsync(string predicate)
        {
            try
            {
                var sqlQuery = predicate;
                var lstTables = await _context.OutputTemplate.FromSqlRaw(sqlQuery).ToListAsync();

                return lstTables;
            }
            catch (Exception ex)
            {
                Utils.RegError($"Error when execute query: {ex.Message}");
                return new List<OutputTemplate>();
            }
        }
    }
}
