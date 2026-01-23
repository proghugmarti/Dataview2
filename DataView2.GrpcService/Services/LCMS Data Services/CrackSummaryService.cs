using DataView2.Core;
using DataView2.Core.Models;
using DataView2.Core.Models.LCMS_Data_Tables;
using DataView2.GrpcService.Data;
using DataView2.GrpcService.Interfaces;
using Esri.ArcGISRuntime.Geometry;
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using ProtoBuf.Grpc;
using Serilog;

namespace DataView2.GrpcService.Services.LCMS_Data_Services
{
    public class CrackSummaryService : BaseService<LCMS_CrackSummary, IRepository<LCMS_CrackSummary>>, ICrackSummaryService
    {
        private readonly AppDbContextProjectData _context;
        IDbContextFactory<AppDbContextProjectData> _dbContextFactory;


        public CrackSummaryService(IRepository<LCMS_CrackSummary> repository, IDbContextFactory<AppDbContextProjectData> dbContextFactor) : base(repository)
        {
            _dbContextFactory = dbContextFactor;
            _context = _dbContextFactory.CreateDbContext();
        }

        public async Task<IEnumerable<LCMS_CrackSummary>> QueryAsync(string predicate)
        {
            try
            {
                var sqlQuery = predicate;
                var lstTables = await _context.LCMS_CrackSummary.FromSqlRaw(sqlQuery).ToListAsync();

                return lstTables;
            }
            catch (Exception ex)
            {
                Utils.RegError($"Error when execute query: {ex.Message}");
                return new List<LCMS_CrackSummary>();
            }
        }

        public async Task<CrackSummaryRefreshResult> RefreshCrackSummaries(List<CrackReference> crackReferences)
        {
            try
            {
                var refreshResult = new CrackSummaryRefreshResult();

                foreach (var crackReference in crackReferences)
                {
                    //Find all remaining nodes for this crack
                    var remainingNodes = _context.LCMS_Cracking_Raw
                                .Where(x => x.CrackId == crackReference.CrackId && 
                                        x.SegmentId == crackReference.SegmentId && 
                                        x.SurveyId == crackReference.SurveyId)
                                .OrderBy(x => x.NodeId)
                                .ToList();

                    var relevantCrackSummary = _context.LCMS_CrackSummary.FirstOrDefault(x => x.CrackId == crackReference.CrackId &&
                         x.SegmentId == crackReference.SegmentId &&
                         x.SurveyId == crackReference.SurveyId);

                    if (relevantCrackSummary != null)
                    {
                        if (remainingNodes != null && remainingNodes.Count > 0)
                        {
                            var totalNodeWidth = remainingNodes.Sum(x => x.NodeWidth_mm * x.NodeLength_mm);
                            var totalNodeDepth = remainingNodes.Sum(x => x.NodeDepth_mm * x.NodeLength_mm);
                            var totalLength = remainingNodes.Sum(x => x.NodeLength_mm);

                            //update crack summary if nodes are partially deleted
                            var firstNodeGeojson = remainingNodes.First()?.GeoJSON;
                            var lastNodeGeojson = remainingNodes.Last()?.GeoJSON;

                            var firstFeature = JObject.Parse(firstNodeGeojson);
                            var lastFeature = JObject.Parse(lastNodeGeojson);

                            // Extract coordinate from first and last node
                            var firstCoordinate = firstFeature["geometry"]?["coordinates"]?.First?.ToObject<List<double>>();
                            var lastCoordinate = lastFeature["geometry"]?["coordinates"]?.Last?.ToObject<List<double>>();

                            if (firstCoordinate != null && lastCoordinate != null)
                            {
                                //recalculate crack length, weightedWidth and weightedDepth
                                relevantCrackSummary.CrackLength_mm = totalLength.Value;
                                var weightedWidth = (double)(totalNodeWidth / totalLength);
                                var weightedDepth = (double)(totalNodeDepth / totalLength);
                                relevantCrackSummary.WeightedWidth_mm = weightedWidth;
                                relevantCrackSummary.WeightedDepth_mm = weightedDepth;

                                //geojson
                                var summaryGeojson = JObject.Parse(relevantCrackSummary.GeoJSON);
                                if (summaryGeojson != null)
                                {
                                    summaryGeojson["geometry"]["coordinates"] = new JArray
                                    {
                                        new JArray(firstCoordinate),
                                        new JArray(lastCoordinate)
                                    };

                                    relevantCrackSummary.GeoJSON = summaryGeojson.ToString();

                                    refreshResult.Updated.Add(relevantCrackSummary);
                                }
                            }
                        }
                        else
                        {
                            //delete crack summary if all nodes are deleted
                            _context.LCMS_CrackSummary.Remove(relevantCrackSummary);
                            refreshResult.Deleted.Add(relevantCrackSummary);
                        }

                        await _context.SaveChangesAsync();
                    }
                }

                return refreshResult;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                return new CrackSummaryRefreshResult();
            }
        }
    }
}
