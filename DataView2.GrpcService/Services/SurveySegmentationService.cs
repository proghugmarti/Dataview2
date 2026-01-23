using DataView2.Core;
using DataView2.Core.Models;
using DataView2.Core.Models.LCMS_Data_Tables;
using DataView2.Core.Models.Other;
using DataView2.GrpcService.Data;
using DataView2.GrpcService.Interfaces;
using DataView2.GrpcService.Services.LCMS_Data_Services;
using DataView2.GrpcService.Services.OtherServices;
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;
using ProtoBuf.Grpc;
using static DataView2.Core.Helper.TableNameHelper;
using Serilog;
using DataView2.Core.Helper;
using Microsoft.EntityFrameworkCore.Metadata;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.Geometry;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using DataView2.GrpcService.Services.AppDbServices;
using System.Data.SQLite;
using DataView2.GrpcService.Data.Projects;
using DataView2.Core.Models.Database_Tables;

namespace DataView2.GrpcService.Services
{
    public class SurveySegmentationService : ISurveySegmentationService
    {
        private readonly AppDbContextProjectData _context;
        private readonly IRepository<SurveySegmentation> _repository;
        private readonly SurveyService _surveyService;
        private readonly SegmentService _segmentService;
        private readonly IDatabaseRegistryLocalService _databaseService;

        public SurveySegmentationService(IRepository<SurveySegmentation> repository, AppDbContextProjectData context, 
            SurveyService surveyService, SegmentService segmentService,  IDatabaseRegistryLocalService databaseService)
        {
            _repository = repository;
            _context = context;
            _surveyService = surveyService;
            _segmentService = segmentService;
            _databaseService = databaseService;
        }

        public async Task<List<string>> HasData(Empty empty, CallContext context = default)
        {
            var hasData = await _repository.AnyAsync();
            if (hasData)
            {
                //if there is a video data, return camera information
                var surveySegmentations = await _repository.GetAllAsync();

                var distinctSegmentationInfo = surveySegmentations
                    .Select(ss => ss.Name)
                    .Distinct()
                    .ToList();

                return distinctSegmentationInfo;
            }
            else
            {
                return new List<string>();
            }
        }

        public async Task<List<SurveySegmentation>> GetAll(Empty empty, CallContext context = default)
        {
            var entities = await _repository.GetAllAsync();
            return entities.ToList();
        }

        public async Task<SurveySegmentation> GetById(IdRequest idRequest, CallContext context = default)
        {
            var entities = await _repository.GetAllAsync();
            if (entities != null && entities.Any())
            {
                var matchingEntity = entities.FirstOrDefault(e => e.Id == idRequest.Id);
                if (matchingEntity != null)
                {
                    return matchingEntity;
                }
            }

            return new SurveySegmentation();
        }

        public async Task<SurveySegmentation> GetByName(string name)
        {
            var entities = await _repository.GetAllAsync();
            if (entities != null && entities.Any())
            {
                var matchingEntity = entities.FirstOrDefault(e => e.Name == name);
                if (matchingEntity != null)
                {
                    return matchingEntity;
                }
            }

            return new SurveySegmentation();
        }

        public async Task<IdReply> Create(SurveySegmentation surveySegmentation, CallContext context = default)
        {
            try
            {
                //Save the survey Segmentation only if it doesn't exist in the db
                var existingSurveySegmentation = await _repository.FirstOrDefaultAsync(s => s.Name == surveySegmentation.Name);

                if (existingSurveySegmentation == null)
                {
                    await _repository.CreateAsync(surveySegmentation);
                }

                return new IdReply
                {
                    Id = surveySegmentation.Id,
                    Message = "Survey created successfully."
                };
            }
            catch (Exception ex)
            {
                return new IdReply
                {
                    Id = -1,
                    Message = $"An error occurred: {ex.Message}"
                };
            }
        }
        public async Task<IdReply> DeleteObject(SurveySegmentation request, CallContext context = default)
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

        public async Task<IdReply> UpdateSegmentation(SurveySegmentation request, CallContext context = default)
        {
            try
            {
                await _repository.UpdateAsync(request);
                return new IdReply
                {
                    Id = request.Id,
                    Message = "Selected record updated successfully."
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            return new IdReply
            {
                Id = request.Id,
                Message = "Selected record is failed to be updated."
            };
        }


        public List<string> GetTableNamesWithColumn(string[] columnNames)
        {
            try
            {
                var matchedTables = new List<string>();
                var targetColumnSet = new HashSet<string>(columnNames, StringComparer.OrdinalIgnoreCase);

                foreach (var entityType in _context.Model.GetEntityTypes())
                {
                    var tableName = entityType.GetTableName();
                    var schema = entityType.GetSchema();

                    var actualColumns = entityType.GetProperties()
                        .Select(p => p.GetColumnName(StoreObjectIdentifier.Table(tableName, schema)))
                        .Where(c => c != null)
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);

                    if (targetColumnSet.All(col => actualColumns.Contains(col)))
                    {
                        matchedTables.Add(tableName);
                    }
                }

                return matchedTables;
            }
            catch (Exception ex)
            {
                Log.Error($"Error in GetTableNamesWithColumn : {ex.Message}");
                return new List<string>();
            }
        }

     
        public async Task CreateSurveysAndInsertSegments(List<SegmentationTableData> surveysToSave)
        {

            foreach (var segmentedSurvey in surveysToSave)
            {
                SegmentationData requestStart = new SegmentationData
                {
                    SurveyId = segmentedSurvey.OldSurvey.SurveyIdExternal,
                    SectionId = segmentedSurvey.SegmentRangeStart,
                    NewSurveyId = segmentedSurvey.SurveyName,
                    Chainage = segmentedSurvey.StartChainage,

                };

                SegmentationData requestEnd = new SegmentationData
                {
                    SurveyId = segmentedSurvey.OldSurvey.SurveyIdExternal,
                    SectionId = segmentedSurvey.SegmentRangeEnd,
                    NewSurveyId = segmentedSurvey.SurveyName,
                    Chainage = segmentedSurvey.EndChainage,
                };

                Survey newSurvey = await _surveyService.CreateSegmentedSurveyFromOther(requestStart);

                requestStart.NewSurveyId = newSurvey.SurveyIdExternal;
                requestEnd.NewSurveyId = newSurvey.SurveyIdExternal;

                List<SegmentationData> segments = new List<SegmentationData>();
                segments.Add(requestStart);
                segments.Add(requestEnd);
                await  ProcessSegmentationSegments(segments);

                //Update chainage of new survey

                double oldChainage = segmentedSurvey.StartChainage; // Use .Value to get the double
                double chainageDifference = segmentedSurvey.NewStartChainage - oldChainage;

                ChainageUpdateRequest chainageRequest = new ChainageUpdateRequest
                {
                    SurveyId = newSurvey.SurveyIdExternal,
                    ChainageDifference = chainageDifference
                };

                await _segmentService.UpdateSegmentChainageInDB(chainageRequest);

                //Update survey chainage 
                newSurvey.StartChainage = segmentedSurvey.NewStartChainage;
                newSurvey.EndChainage = newSurvey.EndChainage + chainageDifference;

                await _surveyService.EditValue(newSurvey);

            }
        }

        private List<int> GetMiddleSegments(List<SegmentationData> segmentsPointsInfos)
        {
            var firstSegmentPoint = segmentsPointsInfos.First();
            var lastSegmentPoint = segmentsPointsInfos.Last();
            List<int> middleSegments = new();

            if (lastSegmentPoint.SectionId - firstSegmentPoint.SectionId > 1)
            {
                middleSegments = Enumerable.Range((int)(firstSegmentPoint.SectionId + 1), (int)(lastSegmentPoint.SectionId - firstSegmentPoint.SectionId - 1)).ToList();
            }

            return middleSegments;
        }

        public async Task ProcessSegmentationSegments(List<SegmentationData> segmentationDatas)
        {

            var middleSegments = GetMiddleSegments(segmentationDatas);
            string sql = string.Empty;

            string oldSurveyId = segmentationDatas.FirstOrDefault().SurveyId;
            string newSurveyId = segmentationDatas.FirstOrDefault().NewSurveyId;

            Survey oldEntitySurvey = await _surveyService.GetSurveyEntityByExternalId(oldSurveyId);
            Survey newEntitySurvey = await _surveyService.GetSurveyEntityByExternalId(newSurveyId);

            List<string> tablesToChangeSurvey = GetTableNamesWithColumn(new string[] { "SurveyId", "SegmentId" });
            List<string> tablesToChangeSurveyByChainage = GetTableNamesWithColumn(new string[] { "SurveyId", "Chainage" });

            // Discard the tables in the second list that are already in the first list
            List<string> filteredTablesToChangeSurveyByChainage = tablesToChangeSurveyByChainage
                .Where(table => !tablesToChangeSurvey.Contains(table))
                .ToList();



            //Copy first segment 
            var segmentTableName = "LCMS_Segment";

            var columnListWithoutSurveyIdStringSegment = await GetColumnList(segmentTableName);
            var columnListWithoutSurveyIdStringse = await GetColumnListWithoutSurveyId(segmentTableName);

            // Copy the first segment record to the new survey
            sql += $"INSERT INTO {segmentTableName} (SurveyId, {columnListWithoutSurveyIdStringSegment}) " +
                   $"SELECT '{segmentationDatas[0].NewSurveyId}', {columnListWithoutSurveyIdStringse} " +
                   $"FROM {segmentTableName} " +
                   $"WHERE SurveyId = '{segmentationDatas[0].SurveyId}' " +
                   $"AND SegmentId = {segmentationDatas[0].SectionId};";


            //copy middle segments with defects 
            foreach (var tableName in tablesToChangeSurvey)
            {
                // Move middle segment defects exclusively to the new survey
                if (middleSegments.Any())
                {
                    sql += $"UPDATE {tableName} SET SurveyId = '{newSurveyId}'" +
                        $" WHERE SurveyId = '{oldSurveyId}' " +
                        $"AND SegmentId IN ({string.Join(",", middleSegments)});";
                }

                var columnListWithoutSurveyIdString = await GetColumnListWithoutSurveyId(tableName);

                // Copy defects to first and last segment
                sql += $"INSERT INTO {tableName} (SurveyId, {columnListWithoutSurveyIdString}) " +
                       $"SELECT '{newSurveyId}', {columnListWithoutSurveyIdString} FROM {tableName} " +
                       $"WHERE SurveyId = '{oldSurveyId}' " + 
                       $"AND SegmentId = {segmentationDatas.First().SectionId} " +
                       $"AND Chainage BETWEEN {segmentationDatas.First().Chainage} AND {segmentationDatas.Last().Chainage};";

                sql += $"INSERT INTO {tableName} (SurveyId, {columnListWithoutSurveyIdString}) " +
                       $"SELECT '{newSurveyId}', {columnListWithoutSurveyIdString} FROM {tableName} " +
                       $"WHERE SurveyId = '{oldSurveyId}' " +  
                       $"AND SegmentId = {segmentationDatas.Last().SectionId} " +
                       $"AND Chainage BETWEEN {segmentationDatas.First().Chainage} AND {segmentationDatas.Last().Chainage};";
            }

            foreach (var tableName in filteredTablesToChangeSurveyByChainage)
            {
                
                    sql += $"UPDATE {tableName} SET SurveyId = '{newEntitySurvey.Id}' " +
                        $"WHERE SurveyId = '{oldEntitySurvey.Id}' " +
                        $"AND Chainage BETWEEN {segmentationDatas.First().Chainage} AND {(segmentationDatas.Last().Chainage)+5};";
                
            }

            // Execute the SQL
            if (!string.IsNullOrEmpty(sql))
            {
                await _databaseService.ExecuteQueryInDb(new List<string> { sql });
            }
        }
        private async Task<string> GetColumnList(string tableName)
        {
            var columnsResult = await _databaseService.GetAllTableColumns(tableName);
            return string.Join(", ", columnsResult.ListData);
        }

        private async Task<string> GetColumnListWithoutSurveyId(string tableName)
        {
            var columnsResult = await _databaseService.GetAllTableColumns(tableName);
            return string.Join(", ", columnsResult.ListData.Where(c => c != "SurveyId"));
        }

    }
      

}
