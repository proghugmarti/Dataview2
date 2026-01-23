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
using Serilog;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Xml;

namespace DataView2.GrpcService.Services.LCMS_Data_Services
{
    public class CrackingRawService : BaseService<LCMS_Cracking_Raw, IRepository<LCMS_Cracking_Raw>>, ICrackingRawService
    {
        private readonly SettingsService settingsService;
        private readonly AppDbContextProjectData _context;
        IDbContextFactory<AppDbContextProjectData> _dbContextFactory;


        public CrackingRawService(IRepository<LCMS_Cracking_Raw> repository, IDbContextFactory<AppDbContextProjectData> dbContextFactor, SettingsService settingsService) : base(repository)
        {
            _dbContextFactory = dbContextFactor;
            _context = _dbContextFactory.CreateDbContext();
            this.settingsService = settingsService;
        }

        List<LCMS_Cracking_Raw> cracksList = new List<LCMS_Cracking_Raw>();

        public async Task DeleteAll()
        {
            await _repository.DeleteAllAsync();
        }

        static double CalculateDistance(double x1, double y1, double x2, double y2)
        {
            double distance = Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));
            return Math.Round(distance, 2);
        }

        public async Task<CountReply> GetRecordCount(Empty empty, CallContext context = default)
        {
            var iCount = await _repository.GetRecordCountAsync();

            return new CountReply { Count = iCount };
        }
        public async Task<IEnumerable<LCMS_Cracking_Raw>> QueryAsync(string predicate)
        {
            try
            {
                var sqlQuery = predicate;
                var lstTables = await _context.LCMS_Cracking_Raw.FromSqlRaw(sqlQuery).ToListAsync();

                return lstTables;
            }
            catch (Exception ex)
            {
                Utils.RegError($"Error when execute query: {ex.Message}");
                return new List<LCMS_Cracking_Raw>();
            }

        }

        public async Task<CountReply> GetCountAsync(string sqlQuery)
        {
            try
            {
                var count = await _context.LCMS_Cracking_Raw.FromSqlRaw(sqlQuery).CountAsync();

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
                var ids = await _context.LCMS_Cracking_Raw.FromSqlRaw(sqlQuery)
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

        public async Task<LCMS_Cracking_Raw> UpdateGenericData(string fieldsToUpdateSerialized)
        {
            var entity = new LCMS_Cracking_Raw();
            Dictionary<string, object> fieldsToUpdate = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(fieldsToUpdateSerialized);
            entity.Id = Convert.ToInt32(fieldsToUpdate["Id"]);
            return await _repository.UpdateEntityAsync(entity, fieldsToUpdate, entity.Id);
        }

        //public async Task<string> GetCrackSummary(List<string> selectedSurveys)
        //{
        //    try
        //    {
        //        List<LCMS_Cracking_Raw> cracks = new List<LCMS_Cracking_Raw>();
        //        if (!selectedSurveys.Any(s => s == "All"))
        //        {
        //            foreach (var survey in selectedSurveys)
        //            {
        //                cracks.AddRange(await GetBySurvey(new SurveyRequest
        //                {
        //                    SurveyId = survey
        //                }));
        //            }
        //        }
        //        else
        //        {
        //            cracks = await GetAll(new Empty());
        //        }

        //        if (cracks != null && cracks.Count > 0)
        //        {
        //            var groupedCracks = cracks
        //                .GroupBy(s => new { s.SurveyId, s.CrackId })
        //                .Select(g => new
        //                {
        //                    CrackId = g.Key.CrackId,
        //                    SurveyId = g.Key.SurveyId,
        //                    SurveyDate = g.First().SurveyDate,
        //                    Avg_NodeLength_mm = Math.Round((double)g.Average(s => s.NodeLength_mm != null ? s.NodeLength_mm : 0), 2),
        //                    Avg_NodeWidth_mm = Math.Round((double)g.Average(s => s.NodeWidth_mm!= null ? s.NodeWidth_mm : 0), 2),
        //                    Avg_NodeDepth_mm = Math.Round((double)g.Average(s => s.NodeDepth_mm != null ? s.NodeDepth_mm : 0), 2),
        //                    Avg_Faulting = Math.Round((double)g.Average(s => s.Faulting != null ? s.Faulting : 0), 2),
        //                    Severity_VeryLow = g.Count(s => s.Severity == "Very Low"),
        //                    Severity_Low = g.Count(s => s.Severity == "Low"),
        //                    Severity_Med = g.Count(s => s.Severity == "Medium"),
        //                    Severity_High = g.Count(s => s.Severity == "High"),
        //                    ImageFileIndex = g.First().ImageFileIndex,
        //                    SegmentId = g.First().SegmentId
        //                }).OrderBy(g => g.CrackId)
        //                .ToList();

        //            List<Dictionary<string, object>> data = groupedCracks.Select(obj => obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
        //            .ToDictionary(prop => prop.Name, prop => prop.GetValue(obj) ?? DBNull.Value)).ToList();

        //            cracks.Clear();
        //            groupedCracks.Clear();

        //            return Newtonsoft.Json.JsonConvert.SerializeObject(data);
        //        }
        //        else return string.Empty;
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error($"Error in GetCrackSummary : {ex.Message}");
        //        return string.Empty;
        //    }
        //}
    }
}
