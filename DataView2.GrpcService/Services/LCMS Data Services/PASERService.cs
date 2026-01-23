using DataView2.Core.Models.LCMS_Data_Tables;
using DataView2.GrpcService.Data;
using DataView2.GrpcService.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DataView2.GrpcService.Services.LCMS_Data_Services
{
    public class PASERService : BaseService<LCMS_PASER, IRepository<LCMS_PASER>>, IPASERService
    {
        private readonly AppDbContextProjectData _context;
        IDbContextFactory<AppDbContextProjectData> _dbContextFactory;

        public PASERService(IRepository<LCMS_PASER> repository, IDbContextFactory<AppDbContextProjectData> dbContextFactor) : base(repository)
        {
            _dbContextFactory = dbContextFactor;
            _context = _dbContextFactory.CreateDbContext();
        }

        public async Task<IEnumerable<LCMS_PASER>> QueryAsync(string predicate)
        {
            try
            {
                var sqlQuery = predicate;
                var lstTables = await _context.LCMS_PASER.FromSqlRaw(sqlQuery).ToListAsync();

                return lstTables;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error when execute query: {ex.Message}");
                return new List<LCMS_PASER>();
            }
        }
    }
}
