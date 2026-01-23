using DataView2.Core.Models;
using DataView2.Core.Models.DataHub;
using DataView2.GrpcService.Data;
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;
using static DataView2.Core.Helper.XMLParser;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Net;

namespace DataView2.GrpcService.Services.DataHubServices
{
    public class FOD_Data_ReportService : IFOD_Data_ReportService
    {
        private readonly HttpClient _httpClient;
        private readonly AppDbContextProjectData _context;
        IDbContextFactory<AppDbContextProjectData> _dbContextFactory;
        private readonly TokenHandler _tokenHandler;
        private readonly ISettingsService _settingsService;
        private readonly ILogger<FOD_Data_Report> _logger;

        public FOD_Data_ReportService(HttpClient httpClient, IDbContextFactory<AppDbContextProjectData> dbContextFactor, TokenHandler tokenHandler, ISettingsService settingsService, ILogger<FOD_Data_Report> logger)
        {
            _httpClient = httpClient;
            _dbContextFactory = dbContextFactor;
            _context = _dbContextFactory.CreateDbContext();
            _tokenHandler = tokenHandler;
            _settingsService = settingsService;
            _logger = logger;

        }

        public async  Task SendFODDataToApiAsync(List<FOD_Data_Report> fodData)
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

            foreach (var fods in fodData)
            {

                var jsonPayload = JsonSerializer.Serialize(fods);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, $"{exportUrl}/api/FOD_Data_Report")
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
                    _logger.LogWarning($"FOD id : {fods.FodID} already exists. Skipping...");
                    continue;
                }

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Failed to send report to API. Status Code: {response.StatusCode}, Reason: {response.ReasonPhrase}, Content: {responseContent}");
                }
            }
        }

        public async Task GetAndSendAllFODDataAsync(Empty empty)
        {
            using var context = _dbContextFactory.CreateDbContext();
            var fods = await context.LCMS_FOD.ToListAsync();

            var fodReports = fods.Select(fod => new FOD_Data_Report
            {
                
                FodID = $"{fod.SurveyId}-{fod.SegmentId}-{fod.FODID}", //unique id 
                InitFodDateTime = fod.DetectionDate,
                AoaID = fod.SurveyName,
                OperatorID = fod.Operator, 
                FodImage = fod.ImageFile,
                FodWidth = (ulong)fod.FODWidth_mm,
                //FodWidth = (ulong)fod.AverageHeight,
                FodLength = (ulong)fod.FODLength_mm,
                //FodLength = (ulong)fod.Area,
                FodLat = fod.GPSLatitude,
                FodLong = fod.GPSLongitude,
                UpdateFodDateTime = fod.DetectionDate.ToString(),
                StatusFodAlert = fod.Status, //need to create from fod.csv
                Note = fod.Comments,  
                FodCharacter = fod.FODDescription, 
                FodSource = "NA"
            }) .ToList();


            await SendFODDataToApiAsync(fodReports);
        }
    }
}
