using DataView2.Core.Helper;
using DataView2.Core.Models;
using DataView2.Core.Models.LCMS_Data_Tables;
using DataView2.Core.Models.Other;
using DataView2.GrpcService.Data;
using DataView2.GrpcService.Interfaces;
using DataView2.GrpcService.Services.LCMS_Data_Services;
using Esri.ArcGISRuntime.Geometry;
using Google.Protobuf.WellKnownTypes;
using Newtonsoft.Json.Linq;
using ProtoBuf.Grpc;
using Serilog;
using System.Data.Entity;

namespace DataView2.GrpcService.Services.OtherServices
{
    public class SummaryService : ISummaryService
    {
        private readonly IRepository<Summary> _repository;
        private readonly AppDbContextProjectData _context;
        private readonly SegmentService _segmentService;
        public SummaryService(IRepository<Summary> repository, AppDbContextProjectData context, SegmentService segmentService)
        {
            _repository = repository;
            _context = context;
            _segmentService = segmentService;
        }

        public async Task<IdReply> Create(Summary summary)
        {
            try
            {
                var existingSummary = _context.Summary.FirstOrDefault(x => x.SampleUnitId == summary.SampleUnitId && x.Name == summary.Name && x.SurveyId == summary.SurveyId);
                if (existingSummary == null)
                {
                    await _repository.CreateAsync(summary);
                    existingSummary = summary;
                }
       
                if (existingSummary != null && summary.SummaryDefects != null)
                {
                    foreach (var summaryDefect in summary.SummaryDefects)
                    {
                        var existingDefect = _context.SummaryDefect.FirstOrDefault(x =>
                                               x.TableName == summaryDefect.TableName &&
                                               x.NumericField == summaryDefect.NumericField &&
                                               x.Operation == summaryDefect.Operation &&
                                               x.SummaryId == existingSummary.Id);

                        if (existingDefect == null)
                            _context.SummaryDefect.Add(summaryDefect);
                        else
                            existingDefect.Value = summaryDefect.Value;
                    }

                    await _context.SaveChangesAsync();
                }
                return new IdReply { Id = existingSummary.Id, Message = "summary successfully created" };
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                return new IdReply { Id = -1, Message = $"Error, {ex.Message}" };
            }
        }

        public async Task<IdReply> CreateSummary(SummaryRequest request)
        {
            try
            {
                var name = request.SummaryName;
                var survey = request.SelectedSurvey;
                var sampleUnitId = request.SampleUnitId;
                var sampleUnitSetId = request.SampleUnitSetId;
                var summaryItems = request.SummaryItems;

                //get center point of coordinates
                var coordinates = System.Text.Json.JsonSerializer.Deserialize<List<List<double>>>(request.CoordinateString);
                var mapPoints = coordinates
                .Select(coord => new MapPoint(coord[0], coord[1], SpatialReferences.Wgs84))
                .ToList();
                var polygon = new Polygon(mapPoints);
                var centerPoint = GeometryEngine.LabelPoint(polygon);

                var existingSummary = _context.Summary.FirstOrDefault(x => x.Name == name && x.SurveyId == survey && x.SampleUnitId == sampleUnitId);
                if (existingSummary == null)
                {
                    //Create a new Summary
                    var newSummary = new Summary
                    {
                        Name = name,
                        SurveyId = survey,
                        SampleUnitId = sampleUnitId,
                        SampleUnitSetId = sampleUnitSetId,
                        ChainageStart = request.ChainageStart,
                        ChainageEnd = request.ChainageEnd,
                        GPSLongitude = centerPoint.X,
                        GPSLatitude = centerPoint.Y,
                    };

                    //save summary
                    _context.Summary.Add(newSummary);
                    await _context.SaveChangesAsync();  // Save here to generate ID
                    existingSummary = newSummary;
                }
               

                if (summaryItems != null)
                {
                    foreach (var summaryItem in summaryItems)
                    {
                        var existingDefect = _context.SummaryDefect.FirstOrDefault(x =>
                                            x.TableName == summaryItem.TableName &&
                                            x.NumericField == summaryItem.NumericField &&
                                            x.Operation == summaryItem.Operation &&
                                            x.SummaryId == existingSummary.Id);

                        if (existingDefect != null)
                        {
                            existingDefect.Value = summaryItem.NumericValue;
                        }
                        else
                        {
                            var newSummaryDefect = new SummaryDefect
                            {
                                TableName = summaryItem.TableName,
                                NumericField = summaryItem.NumericField,
                                Operation = summaryItem.Operation,
                                Value = summaryItem.NumericValue,
                                SummaryId = existingSummary.Id
                            };

                            _context.SummaryDefect.Add(newSummaryDefect);
                        }
                    }
                }
         

                await _context.SaveChangesAsync();  // Save again for SummaryDefects
                return new IdReply { Id = 0, Message = "Saved" };
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                return new IdReply { Id = -1, Message = $"Error, {ex.Message}" };
            }
        }

        public async Task<List<Summary>> GetSummaryBySampleUnit(IdRequest request)
        {
            try
            {
                var summaries = _context.Summary.Where(x => x.SampleUnitId == request.Id).ToList();
                if (summaries != null)
                {
                    foreach (var summary in summaries)
                    {
                        await _context.Entry(summary).Collection(s => s.SummaryDefects).LoadAsync();
                    }
                }
                return summaries;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                return new List<Summary>();
            }
        }

        public async Task<List<Summary>> GetByName(string name)
        {
            try
            {
                var summaries = _context.Summary.Where(x => x.Name == name).ToList();
                if (summaries != null)
                {
                    foreach (var summary in summaries)
                    {
                        await _context.Entry(summary).Collection(s => s.SummaryDefects).LoadAsync();
                    }
                    return summaries;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
            }
            return new List<Summary>();
        }

        public async Task<List<Summary>> GetAll(Empty empty)
        {
            try
            {
                var entities = await _repository.GetAllAsync();
                if (entities != null)
                {
                    foreach (var entity in entities)
                    {
                        await _context.Entry(entity).Collection(s => s.SummaryDefects).LoadAsync();
                    }
                }
                return entities.ToList();
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                return new List<Summary>();
            }
        }

        public async Task<SUSAndSUName> GetSUNamesById(IdRequest request)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(request.Id);
                if (entity != null)
                {
                    await _context.Entry(entity).Reference(s => s.SampleUnitSet).LoadAsync();
                    await _context.Entry(entity).Reference(s => s.SampleUnit).LoadAsync();

                    if (entity.SampleUnit != null && entity.SampleUnitSet != null)
                    {
                        var name = new SUSAndSUName
                        {
                            SUSName = entity.SampleUnitSet.Name,
                            SUName = entity.SampleUnit.Name
                        };

                        return name;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error in GetSUNamesById in SummaryService: " + ex.Message);
            }
            return new SUSAndSUName();
        }

        public async Task<List<string>> HasData(Empty empty, CallContext context = default)
        {
            try
            {
                HashSet<string> summaryNames = new HashSet<string>();
                var entities = await _repository.GetAllAsync();
                if (entities != null)
                {
                    foreach (var entity in entities)
                    {
                        summaryNames.Add(entity.Name);
                    }
                }

                return summaryNames.ToList();
            }
            catch (Exception ex)
            {
                Log.Error("Error in HasData in SummaryService: " + ex.Message);
                return new List<string>();
            }
        }

        public async Task<IdReply> HasSameNameSummary(string summaryName)
        {
            try
            {
                bool exists = _context.Summary.Any(x => x.Name == summaryName);
                return new IdReply
                {
                    Id = exists ? 1 : 0, // 1 = duplicate found, 0 = no duplicate
                    Message = exists ? "Duplicate" : "OK"
                };
            }
            catch (Exception ex)
            {
                Log.Error("Error in HasSameNameSummary in SummaryService: " + ex.Message);
                return new IdReply
                {
                    Id = -1,
                    Message = "Error"
                };
            }
        }

        public async Task<IdReply> BatchRecalculateSummaries(List<SummaryRequest> summaryRequests)
        {
            try
            {
                var sampleUnitIds = summaryRequests
                   .Select(r => r.SampleUnitId)
                   .Distinct()
                   .ToList();

                var sampleUnits = _context.SampleUnit
                    .Where(su => sampleUnitIds.Contains(su.Id))
                    .ToDictionary(su => su.Id, su => su.Coordinates);

                // Populate CoordinateString for each SummaryRequest
                foreach (var req in summaryRequests)
                {
                    if (sampleUnits.TryGetValue(req.SampleUnitId, out var coords))
                    {
                        req.CoordinateString = coords;   // inject coordinates here
                    }
                }

                var tasks = summaryRequests.Select(async summaryRequest =>
                {
                    var response = await _segmentService.GetNumericValueWithinBoundary(summaryRequest);

                    if (response != null && response.Count > 0 && summaryRequest.SummaryId != null)
                    {
                        var summaryDefects = response.Select(item => new SummaryDefect 
                        { 
                            SummaryId = summaryRequest.SummaryId.Value, 
                            NumericField = item.NumericField, 
                            Operation = item.Operation, 
                            TableName = item.TableName, 
                            Value = item.NumericValue, 
                        }).ToList();

                        return summaryDefects;
                    }

                    return new List<SummaryDefect>();
                });

                var allDefectLists = await Task.WhenAll(tasks);

                // Flatten the list of lists
                var allDefects = allDefectLists.SelectMany(defects => defects).ToList();

                // Handle update or insert logic in batch
                foreach (var summaryDefect in allDefects)
                {
                    var existingDefect = _context.SummaryDefect.FirstOrDefault(x =>
                        x.TableName == summaryDefect.TableName &&
                        x.NumericField == summaryDefect.NumericField &&
                        x.Operation == summaryDefect.Operation &&
                        x.SummaryId == summaryDefect.SummaryId);

                    if (existingDefect != null)
                    {
                        existingDefect.Value = summaryDefect.Value;
                    }
                    else
                    {
                        _context.SummaryDefect.Add(summaryDefect);
                    }
                }

                await _context.SaveChangesAsync();
                return new IdReply
                {
                    Id = 0,
                    Message = "Saved"
                };
            }
            catch (Exception ex)
            {
                Log.Error("Error in DeleteSummaryDefect" + ex.Message);
                return new IdReply
                {
                    Id = -1,
                    Message = "Error"
                };
            }
        }

        public async Task<IdReply> BatchCreateSummaries(List<SummaryRequest> summaryRequests)
        {
            try
            {
                foreach (var summaryRequest in summaryRequests)
                {
                    var response = await _segmentService.GetNumericValueWithinBoundary(summaryRequest);
                    if (response.Count > 0)
                    {
                        summaryRequest.SummaryItems = response;
                        await CreateSummary(summaryRequest);
                    }
                }
                return new IdReply { Id = 0, Message = "Created" };
            }
            catch (Exception ex)
            {
                Log.Error("Error in BatchCreateSummaries" + ex.Message);
                return new IdReply
                {
                    Id = -1,
                    Message = "Error"
                };
            }  
        }

        public async Task<List<string>> GetSummaryNameBySurvey(SurveyIdRequest request)
        {
            try
            {
                var summaryNames = _context.Summary.Where(x => x.SurveyId == request.SurveyExternalId).Select(x => x.Name).Distinct().ToList();
                if (summaryNames != null && summaryNames.Count > 0)
                {
                    return summaryNames;
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error in GetSummaryNameBySurvey" + ex.Message);
            }
            return new List<string>();
        }
    }
}
