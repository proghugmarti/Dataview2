using DataView2.Core;
using DataView2.Core.Helper;
using DataView2.Core.Models;
using DataView2.Core.Models.Positioning;
using DataView2.GrpcService.Data;
using DataView2.GrpcService.Interfaces;
using DataView2.GrpcService.Protos;
using DataView2.GrpcService.Services.OtherServices;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using ProtoBuf.Grpc;
using Serilog;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using static DataView2.Core.Models.ExportTemplate.ExportPCIToXml;

namespace DataView2.GrpcService.Services
{
    public class KeyCodeService : IKeycodeService
    {
        private readonly AppDbContextProjectData _context;
        private readonly IRepository<Keycode> _repository;
        private readonly SurveyService _surveyService;
        private readonly GPSProcessedService _gpsProcessedService;


        public KeyCodeService(AppDbContextProjectData context, IRepository<Keycode> repository, SurveyService surveyService, GPSProcessedService gpsProcessedService)
        {
            _context = context;
            _repository = repository;
            _surveyService = surveyService;
            _gpsProcessedService = gpsProcessedService;
        }

        public async Task ProcessKeycode(string filePath, SurveyIdRequest surveyIdRequest)
        {
            var keycodeList = new List<Keycode>();

            Survey survey = await _surveyService.GetById(surveyIdRequest);

            var gpsData = await _context.GPS_Processed
                .Where(g => g.SurveyId == survey.Id)
                .OrderBy(g => g.Chainage)
                .ToListAsync();

            // Prepare the lists for interpolation
            List<double> baseChainages = gpsData.Select(g => g.Chainage).ToList();
            List<double> baseLatitudes = gpsData.Select(g => g.Latitude).ToList();
            List<double> baseLongitudes = gpsData.Select(g => g.Longitude).ToList();
            List<double> baseTrackAngles = gpsData.Select(g => g.Heading).ToList();
            List<double> baseTimes = gpsData.Select(g => g.Time).ToList();

            try
            {
                string jsonString;
                using (FileStream openStream = File.OpenRead(filePath))
                {
                    using var reader = new StreamReader(openStream);
                    jsonString = await reader.ReadToEndAsync();
                }

                // Check if the JSON needs to be wrapped and/or validated
                if (!jsonString.TrimStart().StartsWith("["))
                {
                    // Ensure proper JSON structure and fix it
                    jsonString = await FixJsonFormat(jsonString, filePath);

                }



                using var jsonDocument = JsonDocument.Parse(jsonString);
                foreach (var element in jsonDocument.RootElement.EnumerateArray())
                {
                    double targetChainage = element.GetProperty("Chainage").GetDouble();
                    double interpolatedLatitude = CoordinateHelper.LinearInterpolateSingle(targetChainage, baseChainages, baseLatitudes);
                    double interpolatedLongitude = CoordinateHelper.LinearInterpolateSingle(targetChainage, baseChainages, baseLongitudes);
                    double interpolatedTrackAngle = CoordinateHelper.LinearInterpolateSingle(targetChainage, baseChainages, baseTrackAngles);

                    var keycode = new Keycode
                    {
                        SurveyId = surveyIdRequest.SurveyId,
                        SurveyName = survey.SurveyName,
                        Chainage = targetChainage,
                        GPSLatitude = interpolatedLatitude,
                        GPSLongitude = interpolatedLongitude,
                        GPSTrackAngle = interpolatedTrackAngle,
                        GeoJSON = string.Empty,
                        Time = element.GetProperty("Time").GetDouble().ToString(),
                        Speed = element.GetProperty("Speed").GetDouble(),
                        Description = element.GetProperty("Description").GetString(),
                        Key = int.Parse(element.GetProperty("Key").GetString()),
                        EventKeyType = element.GetProperty("EventKeyType").GetString(),
                        ContinuousStatus = element.TryGetProperty("ContinuousStatus", out var status) ? status.GetString() : null
                    };

                    keycodeList.Add(keycode);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing keycodes :\n{ex.Message}");
            }

            _context.Keycode.AddRange(keycodeList);
            await _context.SaveChangesAsync();
        }

        private async Task<string> FixJsonFormat(string jsonString, string filePath)
        {
            // Trim whitespace from start and end
            jsonString = jsonString.Trim();

            // Check if the JSON needs to be wrapped in brackets
            bool needsWrapping = !jsonString.StartsWith("[");

            // Ensure proper comma placement and fix the JSON format
            var fixedJson = new StringBuilder();
            var lines = jsonString.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            int lineCount = 0;

            foreach (var line in lines)
            {
                // Strip leading/trailing whitespace
                var trimmedLine = line.Trim();

                // Add comma if not the last line and if it ends with "}"
                if (trimmedLine.EndsWith("}") && lineCount < lines.Length - 1)
                {
                    fixedJson.AppendLine(trimmedLine + ",");
                }
                else
                {
                    fixedJson.AppendLine(trimmedLine);
                }
                lineCount++;
            }

            // Prepare final JSON format
            string finalJson = fixedJson.ToString();

            // Remove any trailing newlines and spaces
            finalJson = finalJson.TrimEnd(',', '\n', ' ');

            // If wrapping is needed, wrap the JSON
            if (needsWrapping)
            {
                finalJson = "[" + finalJson + "]";
            }
            else
            {
                finalJson += "]"; // Append closing bracket if already wrapped
            }

            // Save the fixed JSON back to the file
            await File.WriteAllTextAsync(filePath, finalJson);

            return finalJson;
        }

        public async Task<List<Keycode>> GetAll(Empty empty, CallContext context = default)
        {
            try
            {
                var entities = await _repository.GetAllAsync();
                return entities.ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new List<Keycode>();
            }

        }

        public async Task<List<Keycode>> GetByDescription(string keycodeDescription, CallContext context = default)
        {
            try
            {
                var entities = await _repository.GetAllAsync();
                if (entities != null)
                {
                    var matchingEntities = entities.Where(x => x.Description == keycodeDescription).ToList();
                    if (matchingEntities != null)
                    {
                        return matchingEntities;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return new List<Keycode>();
        }

        public async Task<List<Keycode>> GetBySurveyId(SurveyIdRequest idRequest)
        {
            try
            {
                var entities = await _repository.GetAllAsync();
                if (entities != null)
                {
                    var matchingEntities = entities.Where(x => x.SurveyId == idRequest.SurveyId).ToList();
                    if (matchingEntities != null)
                    {
                        return matchingEntities;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return new List<Keycode>();
        }
        public async Task<List<Keycode>> GetBySurvey(string surveyId, CallContext context = default)
        {
            var keycodes = new List<Keycode>();
            try
            {
                var survey = _context.Survey.FirstOrDefault(x => x.SurveyIdExternal == surveyId);
                if (survey != null)
                {
                    var entities = await _repository.Query().Where(x => x.SurveyId == survey.Id).ToListAsync();
                    if (entities != null && entities.Count > 0)
                    {
                        keycodes.AddRange(entities);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in GetBySurvey" + ex.Message);
            }
            return keycodes;
        }

        public async Task<List<Keycode>> GetBySurveyExternalIdList(List<string> listExternalIds)
        {
            try
            {
                // Retrieve the survey IDs first in a single database call
                var surveyIds = await _context.Survey
                    .Where(x => listExternalIds.Contains(x.SurveyIdExternal))
                    .Select(s => s.Id)
                    .ToListAsync();

                // Now retrieve the keycodes that match the survey IDs in another single call
                var keycodes = await _repository.Query()
                    .Where(x => surveyIds.Contains(x.SurveyId))
                    .ToListAsync();

                return keycodes;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in GetBySurveyExternalIdList: " + ex.Message);
                return new List<Keycode>(); // Return an empty list on error
            }
        }

        public async Task<List<string>> GetKeycodeDescriptionsBySurvey (SurveyIdRequest request)
        {
            try
            {
                var entities = _context.Keycode
                        .Where(x => x.SurveyId == request.SurveyId)
                        .Select(x => x.Description)
                        .Distinct()
                        .ToList();
                    if (entities != null && entities.Count > 0)
                    {
                        return entities;
                    }
                
            }
            catch (Exception ex)
            {
                Log.Error("Error in GetSummaryNameBySurvey" + ex.Message);
            }
            return new List<string>();
        }

        public async Task<List<Keycode>> HasData(Empty empty, CallContext context = default)
        {
            var hasData = await _repository.AnyAsync();
            if (hasData)
            {
                //if there is a video data, return descriptions
                var keycodes = await _repository.GetAllAsync();

                // Use Distinct() with a proper equality comparer if necessary
                var distinctKeycodes = keycodes
                    .GroupBy(k => new { k.Id, k.Description })
                    .Select(g => g.First())
                    .ToList();

                return distinctKeycodes;
            }
            else
            {
                return new List<Keycode>();
            }
        }

        public async Task<IdReply> DeleteObject(Keycode request, CallContext context = default)
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

       // public async Task<IEnumerable<Keycode>> QueryAsync(string predicate)
      //  {
           // try
           // {
            //    var sqlQuery = predicate;
               // var lstTables = await _context.FromSqlRaw(sqlQuery).ToListAsync();

               // return lstTables;
          //  }
           // catch (Exception ex)
           // {
            //    Utils.RegError($"Error when execute query: {ex.Message}");
           //     return new List<Keycode>();
           // }
//}
        public async Task<CountReply> GetCountAsync(string sqlQuery)
        {
            try
            {
                var count = await _context.VideoFrame.FromSqlRaw(sqlQuery).CountAsync();

                return new CountReply { Count = count };
            }
            catch (Exception ex)
            {
                Utils.RegError($"Error when execute query: {ex.Message}");
                return new CountReply { Count = 0 };
            }
        }
        public async Task<Keycode> EditValue(Keycode request, CallContext context = default)
        {
            return await _repository.UpdateAsync(request);
        }

        public async Task SaveListKeycodes(List<keycodeFromOtherSaveRequest> keycodeList)
        {
            try
            {
                Survey survey = await _surveyService.GetById(new SurveyIdRequest { SurveyId = keycodeList[0].exampleKeycode.SurveyId });
                var newKeycodes = new List<Keycode>();
                foreach (var keycodeRequest in keycodeList)
                {

                    var closestGPSPoint = await _gpsProcessedService.FindClosestChainageFromGPS(keycodeRequest.Latitude, keycodeRequest.Longitude, survey.Id);

                    if (closestGPSPoint != null)
                    {
                        keycodeRequest.exampleKeycode.Chainage = closestGPSPoint.Chainage;
                        keycodeRequest.exampleKeycode.Time = closestGPSPoint.Time.ToString();
                        keycodeRequest.exampleKeycode.GPSTrackAngle = closestGPSPoint.Heading;
                    }

                    // Create a new Keycode instance
                    var newKeycode = new Keycode
                    {
                        SurveyId = keycodeRequest.exampleKeycode.SurveyId,
                        SurveyName = keycodeRequest.exampleKeycode.SurveyName,
                        Chainage = keycodeRequest.exampleKeycode.Chainage,
                        GPSLatitude = keycodeRequest.Latitude,
                        GPSLongitude = keycodeRequest.Longitude,
                        GPSTrackAngle = keycodeRequest.exampleKeycode.GPSTrackAngle,
                        GeoJSON = keycodeRequest.exampleKeycode.GeoJSON,
                        Time = keycodeRequest.exampleKeycode.Time,
                        Speed = keycodeRequest.exampleKeycode.Speed,
                        Description = keycodeRequest.exampleKeycode.Description,
                        Key = keycodeRequest.exampleKeycode.Key,
                        EventKeyType = keycodeRequest.exampleKeycode.EventKeyType,
                        ContinuousStatus = !string.IsNullOrEmpty(keycodeRequest.ContinuousStatus)
                            ? keycodeRequest.ContinuousStatus
                            : keycodeRequest.exampleKeycode.ContinuousStatus
                    };

                    newKeycodes.Add(newKeycode);

                }

                // Add the new Keycodes to the context
                _context.Keycode.AddRange(newKeycodes);
                await _context.SaveChangesAsync();

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving keycode list: {ex.Message}");
            }
        }
    }
}
