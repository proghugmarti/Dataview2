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
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System;

namespace DataView2.GrpcService.Services
{
    public class SurveyService :ISurveyService
    {
        private readonly AppDbContextProjectData _context;
        private readonly IRepository<Survey> _repository;
        private readonly IServiceProvider _serviceProvider;
        public SurveyService(IRepository<Survey> repository, AppDbContextProjectData context, IServiceProvider serviceProvider)
        {
            _repository = repository;
            _context = context;
            _serviceProvider = serviceProvider;
        }

        public async Task<List<Survey>> GetAll(Empty empty, CallContext context = default)
        {
            var entities = await _repository.GetAllAsync();
            return entities.ToList();
        }

        public async Task<string> GetSurveyNameById(SurveyIdRequest surveyIdrequest, CallContext context = default)
        {
            var entities = await _repository.GetAllAsync();
            if (entities != null && entities.Any())
            {
                var matchingEntity = entities.FirstOrDefault(e => e.Id == surveyIdrequest.SurveyId);
                if (matchingEntity != null)
                {
                    return matchingEntity.SurveyName;
                }
            }

            return string.Empty;
        }

        public async Task<string> GetSurveyNameByExternalId(string externalSurveyId, CallContext context = default)
        {
            var entities = await _repository.GetAllAsync();
            if(entities != null && entities.Any())
            {
                var matchingEntity = entities.FirstOrDefault(e => e.SurveyIdExternal == externalSurveyId);
                if(matchingEntity != null)
                {
                    return matchingEntity.SurveyName;
                }
            }

            return string.Empty;
        }

        public async Task<Survey> ReadSurveyDetailsFile (string filePath, string dataViewVersion)
        {
            try
            {
                using FileStream openStream = File.OpenRead(filePath);
                JsonNode? root = await JsonNode.ParseAsync(openStream);

                if (root is null)
                    throw new Exception("Invalid JSON");

                string surveyName = root["SurveyUid"]?.ToString();
                var operatorName = root["Operator"]?.ToString();
                var surveyFolder = root["SurveyFolder"]?.ToString();
                var SurveyIdExt = root["LcmsSurveyId"]?.ToString();

                // Try to extract date from folder name or filename
                string sourceText = surveyFolder ?? Path.GetFileNameWithoutExtension(filePath);


                // Match 12-digit date format yyyyMMddHHmm
                Match match = Regex.Match(sourceText.Split('_').Last(), @"(\d{12})");
                if (!match.Success)
                    throw new Exception("Survey date (yyyyMMddHHmm) not found in folder name or file name.");


                DateTime surveyDate = DateTime.ParseExact(match.Value, "yyyyMMddHHmm", null);

                // Check if survey already exists by external ID
                var existingSurvey = await FindByNameAndDayAsync(surveyName, surveyDate.Date);
                if (existingSurvey != null)
                {
                    return existingSurvey;
                }

                // Create a new Survey object
                var newSurvey = new Survey
                {
                    SurveyIdExternal = string.IsNullOrEmpty(SurveyIdExt) ? $"{surveyDate:yyyyMMddHHmm}" : SurveyIdExt, // or any unique external key
                    //SurveyIdExternal = $"{surveyDate:yyyyMMddHHmm}", // or any unique external key
                    SurveyName = surveyName ?? throw new Exception("Survey name missing"),
                    // OperatorName = operatorName ?? throw new Exception("Operator missing"),
                    // = surveyFolder ?? throw new Exception("Survey folder missing"),
                    SurveyDate = surveyDate,
                    DataviewVersion = dataViewVersion,
                    Operator = operatorName
                    // Add other fields if needed
                };

                // Call your existing Create method
                IdReply reply = await Create(newSurvey);

                if (reply.Id > 0)
                {
                    newSurvey.Id = reply.Id;

                    Console.WriteLine($"Survey created/updated with ID {reply.Id}");

                    return newSurvey;
                }
                else
                {
                    Console.WriteLine($"Error creating survey: {reply.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            return new Survey();
        }


        public async Task<Survey?> FindByNameAndDayAsync(string name, DateTime dateOnly)
        {
            return _context.Survey.FirstOrDefault(s =>
                    s.SurveyName == name &&
                    s.SurveyDate.Date == dateOnly.Date);
        }


        public async Task<IdReply> Create(Survey survey, CallContext context = default)
        {
            try
            {
                //Save the survey only if it doesn't exist in the db

                // Find existing survey by name and date (date-only, no time)
                //var existingSurvey = await FindByNameAndDayAsync(survey.SurveyName, survey.SurveyDate.Date);
                // Find existing survey by surveyIdExternal
                var existingSurvey = await GetSurveyEntityByExternalId(survey.SurveyIdExternal);

                if (existingSurvey.SurveyIdExternal == null)
                {
                    await _repository.CreateAsync(survey);
                    return new IdReply
                    {
                        Id = survey.Id,
                        Message = "Survey created successfully."
                    };
                }
                else
                {
                    if (existingSurvey.ImageFolderPath != survey.ImageFolderPath)
                    {
                        existingSurvey.ImageFolderPath = survey.ImageFolderPath;
                    }

                    if (existingSurvey.GPSLatitude != survey.GPSLatitude || existingSurvey.GPSLongitude != survey.GPSLongitude)
                    {
                        if (survey.GPSLatitude != 0 && survey.GPSLongitude != 0)
                        {
                            existingSurvey.GPSLatitude = survey.GPSLatitude;
                            existingSurvey.GPSLongitude = survey.GPSLongitude;
                        }
                    }

                    if (existingSurvey.StartChainage == null || survey.StartChainage < existingSurvey.StartChainage) //get lower value
                        existingSurvey.StartChainage = survey.StartChainage;

                    if (existingSurvey.EndChainage == null || survey.EndChainage > existingSurvey.EndChainage) // get higher value
                        existingSurvey.EndChainage = survey.EndChainage;

                    _context.Survey.Update(existingSurvey);
                    await _context.SaveChangesAsync();

                    return new IdReply
                    {
                        Id = existingSurvey.Id,
                        Message = "Survey updated successfully."
                    };
                }
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

        public async Task<Survey> EditValue(Survey request, CallContext context = default)
        {
            return await _repository.UpdateAsync(request);
        }
        
        public async Task<Survey> GetById(SurveyIdRequest surveyIdrequest)
        {
            var surveys = await _repository.GetAllAsync();
            if(surveys != null)
            {
                var matchingSurvey = surveys.FirstOrDefault(s => s.Id == surveyIdrequest.SurveyId);
                if (matchingSurvey != null)
                {
                    return matchingSurvey;
                }
            }
            return new Survey();
        }

        public async Task<string> GetSurveyIdBySurveyName(string surveyName)
        {
            var surveys = await _repository.GetAllAsync();
            if (surveys != null)
            {
                var matchingSurvey = surveys.FirstOrDefault(s => s.SurveyName == surveyName);
                if (matchingSurvey != null)
                {
                    return matchingSurvey.SurveyIdExternal;
                }
            }
            return string.Empty;
        }

        public async Task<Survey> GetSurveyEntityByName (string surveyName)
        {
            var surveys = await _repository.GetAllAsync();
            if (surveys != null)
            {
                var matchingSurvey = surveys.FirstOrDefault(s => s.SurveyName == surveyName);
                if (matchingSurvey != null)
                {
                    return matchingSurvey;
                }
            }
            return new Survey();
        } 

        public async Task<Survey> GetSurveyEntityByExternalId (string externalId)
        {

            var surveys = await _repository.GetAllAsync();
            if (surveys != null)
            {
                var matchingSurvey = surveys.FirstOrDefault(s => s.SurveyIdExternal == externalId);
                if (matchingSurvey != null)
                {
                    return matchingSurvey;
                }
            }
            return new Survey();
        }

        public async Task<IdReply> DeleteObject(Survey request, CallContext context = default)
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

        public async Task<CountReply> GetRecordCount(Empty empty, CallContext context = default)
        {
            var iCount = await _repository.GetRecordCountAsync();

            return new CountReply { Count = iCount };
        }

        public async Task<IEnumerable<Survey>> QueryAsync(string predicate)
        {
            try
            {
                var sqlQuery = predicate;
                var lstTables = await _context.Survey.FromSqlRaw(sqlQuery).ToListAsync();

                return lstTables;
            }
            catch (Exception ex)
            {
                Utils.RegError($"Error when execute query: {ex.Message}");
                return new List<Survey>();
            }
        }
        public async Task<CountReply> GetCountAsync(string sqlQuery)
        {
            try
            {
                var count = await _context.Survey.FromSqlRaw(sqlQuery).CountAsync();

                return new CountReply { Count = count };
            }
            catch (Exception ex)
            {
                Utils.RegError($"Error when execute query: {ex.Message}");
                return new CountReply { Count = 0 };
            }
        }

        public async Task<string> InvokeServiceMethod(string layerName, string serviceName, string methodName, object[] parameters)
        {
            string namespacePrefix = "DataView2.GrpcService.Services.";
            var fullServiceName = namespacePrefix + serviceName;
            var serviceType = System.Type.GetType(fullServiceName);

            if (serviceType != null)
            {
                var serviceInstance = _serviceProvider.GetService(serviceType);
                if (serviceInstance == null)
                {
                    Log.Error($"Service instance for '{fullServiceName}' is null.");
                    return null;
                }
                var hasDataMethod = serviceType.GetMethod(methodName);

                if (hasDataMethod != null)
                {
                    var result = hasDataMethod.Invoke(serviceInstance, parameters);

                    if (result != null && result is Task<IdReply> idReplyTask)
                    {
                        var idReply = await idReplyTask;
                        if (idReply?.Id == 1)
                        {
                            return layerName;
                        }
                    }
                    //else if ( result != null && result is Task<List<string>> stringListTask)
                    //{
                    //    var stringList = await stringListTask;
                    //    if(stringList != null)
                    //    {
                    //        tableNames.AddRange(stringList);
                    //    }
                    //}
                }
            }
            return null;
        }
        public async Task<List<string>> FetchLCMSTables(Empty empty, CallContext context = default)
        {
            try
            {
                var tableNames = new List<string>();
                var parameters = new object[] { empty, context };
                foreach(var tableNameMap in TableNameHelper.TableNameMappings)
                {
                    var layerName = tableNameMap.LayerName;
                    var serviceName = tableNameMap.ServiceName;
                    // Skip INS Geometry layer, this is NOT LCMS table
                    if (layerName == "INS Geometry" || layerName == "LasRutting" || layerName == "LasPoints")
                        continue;
                    var tableName = await InvokeServiceMethod(layerName, serviceName, "HasData", parameters);
                    if(tableName != null)
                    {
                        tableNames.Add(tableName);
                    }
                }

                return tableNames;
            }
            catch (Exception ex) 
            { 
                Log.Error($"Error in fetching lcms layer names from table services : {ex.Message}");
                return new List<string>();
            }
        }

        public async Task<List<string>> FetchLCMSTablesBySurvey(SurveyIdRequest surveyIdrequest, CallContext context = default)
        {
            try
            {
                var tableNames = new List<string>();
                var parameters = new object[] { surveyIdrequest.SurveyExternalId, context };
                
                foreach (var tableNameMap in TableNameHelper.TableNameMappings)
                {
                    var layerName = tableNameMap.LayerName;
                    var serviceName = tableNameMap.ServiceName;
                    var tableName = await InvokeServiceMethod(layerName, serviceName, "HasDataBySurvey", parameters);
                    if (tableName != null)
                    {
                        tableNames.Add(tableName);
                    }
                }

                return tableNames;
            }
            catch (Exception ex)
            {
                Log.Error($"Error in fetching layer names by survey from table services : {ex.Message}");
                return new List<string>();
            }
        } 
        public async Task<List<string>> FetchLCMSTablesByExternalSurveyId(string surveyId, CallContext context = default)
        {
            try
            {
                var tableNames = new List<string>();
                var parameters = new object[] { surveyId, context };

                foreach (var tableNameMap in TableNameHelper.TableNameMappings)
                {
                    var layerName = tableNameMap.LayerName;
                    var serviceName = tableNameMap.ServiceName;
                    var tableName = await InvokeServiceMethod(layerName, serviceName, "HasDataBySurvey", parameters);
                    if (tableName != null)
                    {
                        tableNames.Add(tableName);
                    }
                }

                return tableNames;
            }
            catch (Exception ex)
            {
                Log.Error($"Error in fetching layer names by survey from table services : {ex.Message}");
                return new List<string>();
            }
        }

        public async Task<string> GetImageFolderPath(string surveyId)
        {
            var surveys = await _repository.GetAllAsync();
            if (surveys != null)
            {
                var matchingSurvey = surveys.FirstOrDefault(s => s.SurveyIdExternal == surveyId);
                if (matchingSurvey != null)
                {
                    return matchingSurvey.ImageFolderPath ?? string.Empty;
                }
            }
            return string.Empty;
        }

        public async Task UpdateSurveyFolderPath(ImageFolderChangeRequest request, CallContext context = default)
        {
            var surveyId = request.surveyId;
            var newFolderPath = request.ImageFolderPath;
            var survey = await _repository.FirstOrDefaultAsync(s => s.SurveyIdExternal == surveyId);

            if (survey != null)
            {
                survey.ImageFolderPath = newFolderPath;
                await EditValue(survey, context);
                Log.Information($"Updated Survey {surveyId} with new folder path: {newFolderPath}");
            }
            else
            {
                Log.Error($"Survey {surveyId} not found!");
            }
        }

        public async Task<Survey> UpdateGenericData(string fieldsToUpdateSerialized)
        {
            var entity = new Survey();
            Dictionary<string, object> fieldsToUpdate = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(fieldsToUpdateSerialized);
            entity.Id = Convert.ToInt32(fieldsToUpdate["Id"]);
            return await _repository.UpdateEntityAsync(entity, fieldsToUpdate, entity.Id);
        }

        public async Task<Survey> CreateSegmentedSurveyFromOther(SegmentationData request, CallContext context = default)
        {
            try
            {
                // Get the original survey by external ID
                var originalSurvey = await GetSurveyEntityByExternalId(request.SurveyId);
                if (originalSurvey == null || originalSurvey.Id == 0)
                {
                    throw new Exception($"Original survey with external ID {request.SurveyId} not found.");
                }

                string[] surveyData = request.NewSurveyId.Split('_');
                originalSurvey.Id = 0;
                originalSurvey.SurveyIdExternal = surveyData[1];
                string[] updatedArray = surveyData.Where((val, idx) => idx != 1).ToArray();
                originalSurvey.SurveyName = string.Join('_', updatedArray);
                originalSurvey.Direction = updatedArray[1] == "Inc" ? "Increment" : "Decrement";
//                originalSurvey.StartChainage = request.Chainage;
  //              originalSurvey.EndChainage = request.EndChainage;

                // Save the new segmented survey to the database
                var reply = await Create(originalSurvey);
                if (reply.Id > 0)
                {
                    originalSurvey.Id = reply.Id;
                    Console.WriteLine($"Segmented survey created with ID {reply.Id}");
                    return originalSurvey;
                }
                else
                {
                    throw new Exception($"Failed to create segmented survey: {reply.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return null;
            }
        }

    }
}
