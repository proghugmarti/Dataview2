using Azure.Storage.Blobs;
using DataView2.Core.Models;
using DataView2.Core.Models.DataHub;
using DataView2.Core.Models.LCMS_Data_Tables;
using DataView2.GrpcService.Data;
using DotSpatial.Projections.ProjectedCategories;
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;
using RTools_NTS.Util;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using static DataView2.Core.Helper.XMLParser;

namespace DataView2.GrpcService.Services.DataHubServices
{
    public class PMS_Data_ReportService : IPMS_Data_ReportService
    {
        private readonly HttpClient _httpClient;
        private readonly AppDbContextProjectData _context;
        IDbContextFactory<AppDbContextProjectData> _dbContextFactory;
        private readonly TokenHandler _tokenHandler;
        private readonly ISettingsService _settingsService;
        private readonly ILogger<PMS_Data_ReportService> _logger;
        private readonly IBlobStorageService _blobStorageService;



        public PMS_Data_ReportService(HttpClient httpClient, 
            IDbContextFactory<AppDbContextProjectData> dbContextFactor, 
            TokenHandler tokenHandler, ISettingsService settingsService, 
            ILogger<PMS_Data_ReportService> logger,
            IBlobStorageService blobStorageService)
        {
            _httpClient = httpClient;
            _dbContextFactory = dbContextFactor;
            _context = _dbContextFactory.CreateDbContext();
            _tokenHandler = tokenHandler;
            _settingsService = settingsService;
            _logger = logger;
            _blobStorageService = blobStorageService;

        }

        public async Task SendPMS_Data_ReportsToApiAsync(List<PMS_Data_Report> cracksList)
        {
            var token = _tokenHandler.GetToken();
            if (string.IsNullOrEmpty(token))
            {
                throw new Exception("No token available. Please authenticate first.");
            }
            // Retrieve the ExportURL setting
            var exportUrlSetting = await _settingsService.GetByName(new SettingName { name = "ExportURL" });
            var exportUrl = exportUrlSetting?.FirstOrDefault()?.Value;

            if (string.IsNullOrEmpty(exportUrl))
            {
                throw new Exception("Export URL not configured. Please check your settings.");
            }

            foreach (var cracks in cracksList)
            {
            
                var jsonPayload = JsonSerializer.Serialize(cracks);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, $"{exportUrl}/Pds")
                {
                    Content = content
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();


                _logger.LogInformation($"Response Status: {response.StatusCode}");
                _logger.LogInformation($"Response Content: {responseContent}");
               
                if (response.StatusCode == HttpStatusCode.Conflict)
                {
                    _logger.LogWarning($"Crack ID: {cracks.PdID} already exists. Skipping...");
                    continue;
                }

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Failed to send report to API. Status Code: {response.StatusCode}, Reason: {response.ReasonPhrase}, Content: {responseContent}");
                }
            }
        }

        public async Task GetAndSendWholeCracksAsync(Empty empty)
        {
            using var context = _dbContextFactory.CreateDbContext();
            string basePath = @"C:\Users\vanes\Documents\DCLlocal\Data View\26-APR-2024 02-29-47 PM\ImageResult";


            // Get all cracks
            var nodes = await context.LCMS_Cracking_Raw.ToListAsync();

            // Get the related surveys for the cracks
            var surveyIds = nodes.Select(n => n.SurveyId).Distinct().ToList();

            // Fetch the surveys from the Survey table based on SurveyIdExternal
            var surveys = await context.Survey
                .Where(s => surveyIds.Contains(s.SurveyIdExternal))
                .ToDictionaryAsync(s => s.SurveyIdExternal, s => s.SurveyName);

            // Group the cracks and create PMS_Data_Report objects
            var wholeCracks = new List<PMS_Data_Report>();

            foreach (var group in nodes.GroupBy(node => new { node.SurveyId, node.SegmentId, node.CrackId }))
            {
                var firstNode = group.First();
                string pdImageUrl = null;

                // Upload the image to blob storage if ImageFileIndex contains a valid path
                if (!string.IsNullOrEmpty(firstNode.ImageFileIndex))
                {
                    try
                    {
                        // Remove the .jpg extension from the ImageFileIndex before adding the _Overlay.jpg suffix
                        string fileIndexWithoutExtension = Path.GetFileNameWithoutExtension(group.First().ImageFileIndex);

                        // Construct the full image path with Overlay
                        string localImagePath = $"C:\\Users\\vanes\\Documents\\DCLlocal\\Data View\\26-APR-2024 02-29-47 PM\\ImageResult\\{fileIndexWithoutExtension}_Overlay.jpg";


                        if (File.Exists(localImagePath))
                        {
                            _logger.LogInformation($"Uploading image {localImagePath}...");

                            using var stream = new FileStream(localImagePath, FileMode.Open);
                            pdImageUrl = await _blobStorageService.UploadImageAsync(stream, Path.GetFileName(localImagePath)); // Upload and get URL

                            _logger.LogInformation($"Image {localImagePath} uploaded successfully.");

                        }
                        else
                        {
                            _logger.LogWarning($"Image file {localImagePath} not found.");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error uploading image {firstNode.ImageFileIndex}: {ex.Message}");
                    }
                }

                var pmsReport = new PMS_Data_Report
                {
                    PdID = $"{group.Key.SurveyId}-{group.Key.SegmentId}-{group.Key.CrackId}", // crack pdid unique id
                    PdDateTime = firstNode.SurveyDate,
                    AoaID = surveys.ContainsKey(group.Key.SurveyId) ? surveys[group.Key.SurveyId] : group.Key.SurveyId.ToString(), // Use SurveyName or SurveyId as fallback
                    OperatorID = "SampleOperatorID",
                    PdImage = pdImageUrl, // Assign the URL of the uploaded image
                    Classification = firstNode.PavementType == "Asphalt" ? 1 : firstNode.PavementType == "Concrete" ? 20 : 1, // classification: asphalt or concrete
                    PdLat = firstNode.GPSLatitude,
                    PdLong = firstNode.GPSLongitude,
                    Note = "Sample Note",
                    Width = group.Average(node => node.NodeWidth_mm) ?? 0,
                    Length = group.Average(node => node.NodeLength_mm) ?? 0,
                    Depth = group.Average(node => node.NodeDepth_mm) ?? 0,
                    BlockID = firstNode.SegmentId.ToString(), // segment ID as temporary block
                    PdSeverity = GetSeverityLevel(group)
                };

                wholeCracks.Add(pmsReport);
            }

            // Send the PMS_Data_Reports to the API
            await SendPMS_Data_ReportsToApiAsync(wholeCracks);
        }

        private long GetSeverityLevel(IGrouping<dynamic, LCMS_Cracking_Raw> group)
        {

            var severityMapping = new Dictionary<string, long>
            {
                { "Very High", 100},
                { "High", 75},
                { "Med", 50 },
                { "Low", 25 },
                { "Very Low", 0 }
            };

            return group
             .Select(node => node.Severity)
             .Where(severity => !string.IsNullOrEmpty(severity)) // Ensure severity is not null or empty
             .Select(severity =>
             {
                 // Try to get severity level from dictionary; default to 30 ("Low") if not found
                 return severityMapping.TryGetValue(severity, out var mappedSeverity) ? mappedSeverity : 30;
             })
             .DefaultIfEmpty(30) // Default to "Low" severity if none are found
             .Max();
        }
    }
}
