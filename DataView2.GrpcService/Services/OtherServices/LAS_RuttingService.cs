using CsvHelper.Configuration;
using CsvHelper;
using DataView2.Core.Models;
using DataView2.Core.Models.LCMS_Data_Tables;
using DataView2.Core.Models.Other;
using DataView2.GrpcService.Data;
using DataView2.GrpcService.Interfaces;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using NPOI.SS.Formula.Functions;
using ProtoBuf.Grpc;
using System.Globalization;

namespace DataView2.GrpcService.Services.OtherServices
{
    public class LAS_RuttingService : ILAS_RuttingService
    {
        private readonly ILogger<LAS_RuttingService> logger;

        private readonly IRepository<LAS_Rutting> repository;
        private readonly AppDbContextProjectData context;
        private readonly SurveyService _surveyService;


        public LAS_RuttingService(ILogger<LAS_RuttingService> _logger,
            AppDbContextProjectData appDbContextProject, IRepository<LAS_Rutting> _repository, SurveyService surveyService
)
        {
            logger = _logger;
            context = appDbContextProject;
            repository = _repository;
            _surveyService = surveyService;

        }

        public async Task<IdReply> Create(LAS_Rutting rutting)
        {
            try
            {
                await repository.CreateAsync(rutting);
                return new IdReply
                {
                    Id = rutting.Id,
                    Message = "rutting created succesfully"
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            return new IdReply
            {
                Id = -1,
                Message = "Failed to create new rutting"
            };
        }

        public async Task<List<LAS_Rutting>> GetAll(Empty empty)
        {
            try
            {
                var lasRuttings = context.LAS_Rutting.ToList();
                return lasRuttings;
            }
            catch (Exception ex)
            {
                logger.LogError($"Error retrieving all LAS files: {ex.Message}");
                throw new RpcException(new Status(StatusCode.Internal, $"Failed to retrieve LAS files: {ex.Message}"));
            }
        }


        public async Task<List<LAS_Rutting>> GetBySurvey(SurveyRequest request)
        {
            try
            {
                var entities = repository.Query()
                    .Where(x => x.SurveyId == request.SurveyId).ToList();

                return entities;
            }
            catch (Exception ex)
            {
                logger.LogError($"Error in BaseService Loading an object type by survey Id: {ex.Message}");
                Console.WriteLine($"Error: {ex.Message}");
                return new List<LAS_Rutting>();
            }
        }

        public async Task<IdReply> HasData(Empty empty, CallContext context = default)
        {

            var hasData = await repository.AnyAsync();
            if (hasData)
            {
                return new IdReply
                {
                    Id = 1
                };
            }
            else
            {
                return new IdReply
                {
                    Id = 0
                };
            }
        }

        public async Task<IdReply> HasDataBySurvey(string surveyId, CallContext context = default)
        {
            var hasServiceData = await repository.AnyAsync();
            if (!hasServiceData)
            {
                return new IdReply { Id = 0 };
            }

            var surveyData = await repository.FirstOrDefaultAsync(x => x.SurveyId == surveyId);

            if (surveyData != null)
            {
                return new IdReply { Id = 1 };
            }
            else
            {
                return new IdReply { Id = 0 };
            }
        }

        public async Task<IdReply> DeleteById(IdRequest id)
        {
            var reply = new IdReply();

            try
            {
                var lasRut = context.LAS_Rutting.FirstOrDefault(l => l.Id == id.Id);

                if (lasRut == null)
                {
                    reply.Id = 0;
                    reply.Message = $"LAS rut with id '{id}' not found";
                    return reply;
                }

                // Remove the LAS file itself
                context.LAS_Rutting.Remove(lasRut);

                await context.SaveChangesAsync();

                reply.Id = lasRut.Id;
                reply.Message = $"LAS rut '{id}' was deleted successfully";
            }
            catch (Exception ex)
            {
                logger.LogError($"Error deleting LAS rut: {id}, Exception: {ex.Message}");
                reply.Id = 0;
                reply.Message = $"Failed: {ex.Message}";
            }

            return await Task.FromResult(reply);
        }


        public async Task<IdReply> ProcessLASRuttingFiles(List<LASfileRequest> requests)
        {
            DateTime start = DateTime.Now;
            logger.LogInformation($"===>> Start Time: {start}");

            var replies = new List<IdReply>();
            int passCount = 0;
            int alreadyProcessedCount = 0;
            int failCount = 0;

            var existingLASfiles = (List<LAS_Rutting>)await repository.GetAllAsync();

            foreach (var request in requests)
            {
                try
                {
                    var reply = await ProcessSingleLASRuttingSFile(request);
                    switch (reply.Id)
                    {
                        case 0: // Failed
                            replies.Add(reply);
                            failCount++;
                            break;

                        case -1: // Already processed
                            alreadyProcessedCount++;
                            break;

                        default: // Success
                            passCount++;
                            break;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError($"Error while processing LAS file: {request.LASfileName}, Exception: {ex.Message}");
                    replies.Add(new IdReply { Id = 0, Message = $"Failed to process LAS file: {ex.Message}" });
                    failCount++;
                }
            }

            logger.LogInformation($"===>> ProcessLASfilesOptimized Total time (sec): {(DateTime.Now - start).TotalSeconds}");

            // Builds las import message
            string combinedMessage = "";
            if (replies.Any()) // Error Path
            {
                if (passCount > 0)
                    combinedMessage = $"{passCount} LAS file(s) processed successfully. <br /><br />";

                if (alreadyProcessedCount > 0)
                    combinedMessage += $"{alreadyProcessedCount} selected LAS file(s) have already been processed.<br /><br />";

                if (failCount > 0)
                    combinedMessage += $"{failCount} LAS file(s) failed to process<br />Please check they are valid and try again: <br />";

                combinedMessage += string.Join("<br />", replies.Select(r => "• " + r.Message));
                return new IdReply { Id = 0, Message = combinedMessage };
            }
            if (passCount == 0)
            {
                return new IdReply { Id = -1, Message = "" };
            }

            // Success Path
            if (alreadyProcessedCount == 0)
                combinedMessage = $"All LAS file(s) processed successfully.";

            else if (passCount > 0)
                combinedMessage += $"{passCount} LAS file(s) processed successfully.<br /><br />";

            if (alreadyProcessedCount > 0)
                combinedMessage += $"{alreadyProcessedCount} selected LAS file(s) have already been processed.";

            return new IdReply { Id = 1, Message = combinedMessage };

        }

        private async Task<IdReply> ProcessSingleLASRuttingSFile(LASfileRequest request)
        {
            //Get survey from request
            Survey survey = await _surveyService.GetSurveyEntityByExternalId(request.SurveyId);

            if (!File.Exists(request.FilePath))
            {
                return new IdReply { Id = 0, Message = $"LAS file not found: {request.LASfileName} " };
            }

            using (var reader = new StreamReader(request.FilePath))
            using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                PrepareHeaderForMatch = args => args.Header.ToLower(),
                MissingFieldFound = null
            }))
            {
                try
                {
                    csv.Read();
                    csv.ReadHeader();

                    var headerRow = csv.HeaderRecord;

                    List<LAS_Rutting> records;

                    // Check if the old format is present
                    if (headerRow.Contains("TableName")) // Old format check
                    {
                        records = csv.GetRecords<OldFormatLAS_Rutting>().Select(record => new LAS_Rutting
                        {
                            Id = 0,
                            SurveyId = request.SurveyId,
                            RutDepth_mm = record.RutDepth_mm,
                            RutWidth_m = record.RutWidth_mm,
                            GPSLatitude = record.GPSLatitude,
                            GPSLongitude = record.GPSLongitude,
                            GeoJSON = record.GeoJSON
                        }).ToList();
                    }
                    else // New format
                    {
                        records = csv.GetRecords<LAS_Rutting>().ToList();
                        foreach (var record in records)
                        {
                            record.Id = 0; // Ensure a new record is created
                            record.SurveyId = request.SurveyId;
                        }
                    }

                    if (records.Count > 0 && survey.GPSLatitude == 0.0)
                        
                    {
                        await UpdateSurveyCoordinatesIfNeeded(survey, records.FirstOrDefault().GPSLongitude, records.FirstOrDefault().GPSLatitude);
                    }

                        foreach (var record in records)
                    {
                        record.Id = 0; // Ensure a new record is created
                        record.SurveyId = request.SurveyId;

                        var reply = await Create(record);
                    }

                    return new IdReply
                    {
                        Id = 1,
                        Message = $"LAS rutting file '{request.LASfileName}' processed successfully with {records.Count} records."
                    };
                }
                catch (Exception ex)
                {
                    logger.LogError($"Error processing LAS rutting file: {request.LASfileName}, Exception: {ex.Message}");
                    return new IdReply
                    {
                        Id = 0,
                        Message = $"Failed to process LAS rutting file '{request.LASfileName}': {ex.Message}"
                    };
                }
            }
        }


        // Updates survey coordinates if this is the first file or they need updating
        private async Task UpdateSurveyCoordinatesIfNeeded(Survey surveyToUpdate, double gpslongitude, double gpslatitude)
        {
            
            if (surveyToUpdate.GPSLatitude == 0.0)
            {
                surveyToUpdate.GPSLatitude = gpslatitude;
            }

            if (surveyToUpdate.GPSLongitude == 0.0)
            {
                surveyToUpdate.GPSLongitude = gpslongitude;
            }

            surveyToUpdate = await _surveyService.EditValue(surveyToUpdate);
            
        }

        public async Task<IdReply> UpdateRecalculatedById (LasRuttingRecalculateRequest idRequest)
        {
            LAS_Rutting existingRutting = null;
            try
            {
                existingRutting = context.LAS_Rutting.FirstOrDefault(l => l.Id == idRequest.Id);
                if (existingRutting == null)
                {
                    return new IdReply
                    {
                        Id = 0,
                        Message = $"LAS Rutting with id '{idRequest.Id}' not found"
                    };
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Error updating LAS Rutting recalculated flag: {idRequest.Id}, Exception: {ex.Message}");
                return new IdReply
                {
                    Id = 0,
                    Message = $"Failed to update LAS Rutting recalculated flag: {ex.Message}"
                };
            }
            //Update existingRutting 
            existingRutting.RutDepth_mm = idRequest.NewDepthFactor;
            context.LAS_Rutting.Update(existingRutting);
            await context.SaveChangesAsync();

            return new IdReply
            {
                Id = existingRutting.Id,
                Message = $"LAS Rutting recalculated flag updated successfully for id '{idRequest.Id}'"
            };
        }
    }
}