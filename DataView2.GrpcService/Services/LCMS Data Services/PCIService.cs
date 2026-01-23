using DataView2.Core;
using DataView2.Core.Models;
using DataView2.Core.Models.LCMS_Data_Tables;
using DataView2.GrpcService.Data;
using DataView2.GrpcService.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DataView2.GrpcService.Services.LCMS_Data_Services
{
    public class PCIService : BaseService<LCMS_PCI, IRepository<LCMS_PCI>>, IPCIService
    {
        private readonly AppDbContextProjectData _context;
        IDbContextFactory<AppDbContextProjectData> _dbContextFactory;

        public PCIService(IRepository<LCMS_PCI> repository, IDbContextFactory<AppDbContextProjectData> dbContextFactor) : base(repository)
        {
            _dbContextFactory = dbContextFactor;
            _context = _dbContextFactory.CreateDbContext();
        }

        public async Task<IEnumerable<LCMS_PCI>> QueryAsync(string predicate)
        {
            try
            {
                var sqlQuery = predicate;
                var lstTables = await _context.LCMS_PCI.FromSqlRaw(sqlQuery).ToListAsync();

                return lstTables;
            }
            catch (Exception ex)
            {
                Utils.RegError($"Error when execute query: {ex.Message}");
                return new List<LCMS_PCI>();
            }
        }

        public async Task<LCMS_PCI> UpdateGenericData(string fieldsToUpdateSerialized)
        {
            var entity = new LCMS_PCI();
            Dictionary<string, object> fieldsToUpdate = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(fieldsToUpdateSerialized);
            entity.Id = Convert.ToInt32(fieldsToUpdate["Id"]);
            return await _repository.UpdateEntityAsync(entity, fieldsToUpdate, entity.Id);
        }
    }
}
