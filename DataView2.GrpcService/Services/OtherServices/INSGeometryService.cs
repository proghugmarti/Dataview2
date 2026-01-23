using DataView2.Core;
using DataView2.Core.Models;
using DataView2.Core.Models.LCMS_Data_Tables;
using DataView2.Core.Models.Positioning;
using DataView2.GrpcService.Data;
using DataView2.GrpcService.Interfaces;
using Microsoft.EntityFrameworkCore;              // ✅ EF Core DbContext, DbSet, FromSqlRaw, ToListAsync
using Microsoft.EntityFrameworkCore.Infrastructure; // ✅ EF Core IDbContextFactory


namespace DataView2.GrpcService.Services.OtherServices
{
    public class INSGeometryService
    : BaseService<Geometry_Processed, IRepository<Geometry_Processed>>, IINSGeometryService
    {
        private readonly AppDbContextProjectData _context;
        private readonly IDbContextFactory<AppDbContextProjectData> _dbContextFactory;

        public INSGeometryService(
            IRepository<Geometry_Processed> repository,
            IDbContextFactory<AppDbContextProjectData> dbContextFactory)
            : base(repository)
        {
            _dbContextFactory = dbContextFactory;
            _context = _dbContextFactory.CreateDbContext();
        }

        public async Task<IEnumerable<Geometry_Processed>> QueryAsync(string sqlQuery)
        {
            try
            {
                var rows = await _context.Geometry_Processed.FromSqlRaw(sqlQuery).ToListAsync();
                return rows;
            }
            catch (Exception ex)
            {
                Utils.RegError($"Error executing INSGeometry query: {ex.Message}");
                return new List<Geometry_Processed>();
            }
        }

        public async Task<CountReply> GetCountAsync(string sqlQuery)
        {
            try
            {
                var count = await _context.Geometry_Processed.FromSqlRaw(sqlQuery).CountAsync();
                return new CountReply { Count = count };
            }
            catch (Exception ex)
            {
                Utils.RegError($"Error executing INSGeometry count: {ex.Message}");
                return new CountReply { Count = 0 };
            }
        }

        public async Task<List<Geometry_Processed>> GetBySurvey(string surveyId)
        {
            var gpsProcessedList = new List<Geometry_Processed>();
            try
            {
                var survey = _context.Survey.FirstOrDefault(x => x.SurveyIdExternal == surveyId);
                if (survey != null)
                {
                    var entities = await _context.Geometry_Processed
                    .Where(g => g.SurveyId == survey.Id.ToString())
                    .OrderBy(g => g.Chainage)
                    .ToListAsync();
                    if (entities != null && entities.Count > 0)
                    {
                        gpsProcessedList.AddRange(entities);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in GetBySurvey" + ex.Message);
            }
            return gpsProcessedList;
        }

    }
}
