using DataView2.Core.Models.LCMS_Data_Tables;
using DataView2.GrpcService.Data;
using DataView2.GrpcService.Interfaces;
using ProtoBuf.Grpc;
using DataView2.Core.Models.LCMS_Data_Tables;
using DataView2.Core.Models;
using Microsoft.EntityFrameworkCore;
using System.Xml;
using DataView2.Core;
using Google.Protobuf.WellKnownTypes;
using System.ServiceModel;
using DataView2.Core.Helper;
using DataView2.GrpcService.Services.AppDbServices;
using Serilog;
using System.Data.SQLite;

namespace DataView2.GrpcService.Services.LCMS_Data_Services
{
    public class SegmentGridService : BaseService<LCMS_Segment_Grid, IRepository<LCMS_Segment_Grid>>, ISegmentGridService
    {
        private readonly AppDbContextProjectData _context;
        IDbContextFactory<AppDbContextProjectData> _dbContextFactory;
        private string datasetPath = "";
        private readonly IServiceProvider _serviceProvider;

        public SegmentGridService(IRepository<LCMS_Segment_Grid> repository, IDbContextFactory<AppDbContextProjectData> dbContextFactor, IServiceProvider serviceProvider) : base(repository)
        {
            _dbContextFactory = dbContextFactor;
            _serviceProvider = serviceProvider;
            _context = _dbContextFactory.CreateDbContext();
        }       
        public async Task<IEnumerable<LCMS_Segment_Grid>> QueryAsync(string predicate)
        {
            try
            {
                var sqlQuery = predicate;
                var lstTables = await _context.LCMS_Segment_Grid.FromSqlRaw(sqlQuery).ToListAsync();

                return lstTables;
            }
            catch (Exception ex)
            {
                Utils.RegError($"Error when execute query: {ex.Message}");
                return new List<LCMS_Segment_Grid>();
            }
        }
        public async Task<CountReply> GetCountAsync(string sqlQuery)
        {
            try
            {
                var count = await _context.LCMS_Segment_Grid.FromSqlRaw(sqlQuery).CountAsync();

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

        public async Task<List<LCMS_Segment_Grid>> GetSegmentGridsBySurveyIDSectionId(Segment_Grid_Params segment_Grid_Params)
        {
            var segment_Grids = await _repository.GetAllAsync();
            if (segment_Grids != null && segment_Grids.ToList().Count>0)
            {
                return segment_Grids.Where(s => s.SurveyId == segment_Grid_Params.SurveyId && s.SegmentId == Convert.ToInt32(segment_Grid_Params.SectionId)).ToList();
            }
            return new List<LCMS_Segment_Grid>();
        }

        public async Task ExecuteQueryInDb(List<string> queries)
        {
            try
            { 
                int queryExecuted = 0;

                if (String.IsNullOrEmpty(datasetPath))
                {
                    var databasePathProvider = _serviceProvider.GetRequiredService<DatabasePathProvider>();
                    string actualDatabasePath = databasePathProvider.GetDatasetDatabasePath();
                    datasetPath = actualDatabasePath;
                }

                using (SQLiteConnection conn = new SQLiteConnection($"Data Source = {datasetPath};"))
                {
                    conn.Open();
                    using (var transaction = conn.BeginTransaction())
                    {
                        using (var command = new SQLiteCommand(conn))
                        {
                            command.Transaction = transaction;

                            foreach (string q in queries)
                            {
                                try
                                {
                                    if (q != null && !string.IsNullOrEmpty(q))
                                    {
                                        command.CommandText = q;
                                        command.ExecuteNonQuery();

                                        queryExecuted++;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Log.Error($"Error in ExecuteQueryInDb while updating data : {ex.Message}");
                                }
                            }
                        }
                        transaction.Commit();
                    }
                    conn.Close();
                }

                Log.Information($"Total : {queries.Count}, Query executed successfully : {queryExecuted}");
            }
            catch (Exception ex)
            {
                Log.Error($"Error in executing query : {ex.Message}");
            }
        }

        public async Task<LCMS_Segment_Grid> UpdateGenericData(string fieldsToUpdateSerialized)
        {
            var entity = new LCMS_Segment_Grid();
            Dictionary<string, object> fieldsToUpdate = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(fieldsToUpdateSerialized);
            entity.Id = Convert.ToInt32(fieldsToUpdate["Id"]);
            return await _repository.UpdateEntityAsync(entity, fieldsToUpdate, entity.Id);
        }
    }
}
