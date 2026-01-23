using DataView2.Core.Models;
using DataView2.Core.Models.DataHub;
using DataView2.GrpcService.Data;
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace DataView2.GrpcService.Services.DataHubServices
{
    public class AoaDataService : IAoaDataService
    {
        private readonly HttpClient _httpClient;
        private readonly AppDbContextProjectData _context;
        IDbContextFactory<AppDbContextProjectData> _dbContextFactory;
        private readonly TokenHandler _tokenHandler;
        private readonly ISettingsService _settingsService;
        private readonly ILogger<AoaDataService> _logger;
        private readonly IBlobStorageService _blobStorageService;

        public AoaDataService(HttpClient httpClient,
            IDbContextFactory<AppDbContextProjectData> dbContextFactory, 
            TokenHandler tokenHandler,
            ISettingsService settingsService, 
            ILogger<AoaDataService> logger , 
            IBlobStorageService blobStorageService)
        {
            _httpClient = httpClient;
            _dbContextFactory = dbContextFactory;
            _context = _dbContextFactory.CreateDbContext();
            _tokenHandler = tokenHandler;
            _settingsService = settingsService;
            _logger = logger;
            _blobStorageService = blobStorageService;

        }

        public async Task SendAoaDataToApiAsync(List<AoaData> aoaDataList)
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

            foreach (var aoaData in aoaDataList)
            {
                var jsonPayload = JsonSerializer.Serialize(aoaData);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, $"{exportUrl}/Aoas")
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
                    _logger.LogWarning($"Aoa ID: {aoaData.AoaId} already exists. Skipping...");
                    continue;
                }

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Failed to send report to API. Status Code: {response.StatusCode}, Reason: {response.ReasonPhrase}, Content: {responseContent}");
                }
            }
        }

        public async Task GetAndSendAllAoaDataAsync(Empty empty)
        {
            using var context = _dbContextFactory.CreateDbContext();
            var boundaries = await context.Boundary.ToListAsync();
            List<AoaData> aoaList = new List<AoaData>();

            foreach (var boundary in boundaries)
            {
                try
                {
                    // Deserialize coordinates
                    List<(double, double)> coordinates = Newtonsoft.Json.JsonConvert.DeserializeObject<List<(double, double)>>(boundary.Coordinates);

                    // Ensure there are enough coordinates (at least 4 points)
                    if (coordinates.Count < 4)
                    {
                        _logger.LogWarning($"Boundary {boundary.SurveyName} does not contain enough coordinates (expected 4, found {coordinates.Count}). Skipping.");
                        continue;
                    }

                    // Map the coordinates into rings (if needed)
                    List<Double[]> rings = coordinates.Select(coord => new Double[] { coord.Item1, coord.Item2 }).ToList();

                    // Create AoaData from coordinates
                    var newAoa = new AoaData
                    {
                        AoaId = boundary.SurveyName,
                        Lat1 = coordinates[0].Item2,  
                        Long1 = coordinates[0].Item1,
                        Lat2 = coordinates[1].Item2, 
                        Long2 = coordinates[1].Item1,
                        Lat3 = coordinates[2].Item2, 
                        Long3 = coordinates[2].Item1,
                        Lat4 = coordinates[3].Item2,  
                        Long4 = coordinates[3].Item1,


                        Blocks = new List<Block>() // Initialize Blocks list

                    };

                    // Fetch associated segments (blocks) for the boundary
                    var segments = await context.LCMS_Segment
                                                .Where(s => s.SurveyId == boundary.SurveyId) // Assuming BoundaryId is the link
                                                .ToListAsync();


                    foreach (var segment in segments)
                    {
                        // Deserialize segment coordinates
                        dynamic segmentJson = Newtonsoft.Json.JsonConvert.DeserializeObject(segment.GeoJSON);
                        // Extract the first four coordinates from the "coordinates" field
                        var segmentCoords = segmentJson.geometry.coordinates[0]; // First ring of coordinates in GeoJSON Polygon

                        if (segmentCoords.Count < 4)
                        {
                            _logger.LogWarning($"Segment {segment.SegmentId} does not contain enough coordinates. Skipping.");
                            continue;
                        }

                        // Check if the segment has cracks in the LCMS_Cracking_Raw table
                        var hasCracks = await context.LCMS_Cracking_Raw
                            .AnyAsync(c => c.SegmentId == segment.SegmentId);


                        // Map the GeoJSON coordinates to Block properties
                        var block = new Block
                        {
                            BlockID = "block" + segment.Id.ToString(),
                            BlockNoSeverity = hasCracks ? 80 : 0, // Set severity as 80 if cracks are found
                            BlockLat1 = (double)segmentCoords[0][1],  
                            BlockLong1 = (double)segmentCoords[0][0], 
                            BlockLat2 = (double)segmentCoords[1][1], 
                            BlockLong2 = (double)segmentCoords[1][0], 
                            BlockLat3 = (double)segmentCoords[2][1],  
                            BlockLong3 = (double)segmentCoords[2][0], 
                            BlockLat4 = (double)segmentCoords[3][1],  
                            BlockLong4 = (double)segmentCoords[3][0]  
                        };
                       
                        newAoa.Blocks.Add(block); 
                    }

                    aoaList.Add(newAoa); 
              
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error processing boundary {boundary.SurveyName}: {ex.Message}");
                    continue;
                }
            }

            if (aoaList.Any())
            {
                await SendAoaDataToApiAsync(aoaList);
                _logger.LogInformation($"{aoaList.Count} AOA data records successfully sent.");
            }
            else
            {
                _logger.LogWarning("No AOA data was sent to the API.");
            }
        }
    }
}