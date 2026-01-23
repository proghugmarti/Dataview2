using DataView2.Core.Models;
using DataView2.Core.Models.DataHub;
using DataView2.GrpcService.Data;
using Microsoft.EntityFrameworkCore;
using RTools_NTS.Util;
using System.Text;
using System.Text.Json;

namespace DataView2.GrpcService.Services.DataHubServices
{
    public class AuthDataHubService : IAuthDataHubService
    {
        private readonly HttpClient _httpClient;
        private readonly TokenHandler _tokenHandler;
        private readonly AppDbContextProjectData _context;
        IDbContextFactory<AppDbContextProjectData> _dbContextFactory;
        private readonly ISettingsService _settingsService;


        public AuthDataHubService(HttpClient httpClient, IDbContextFactory<AppDbContextProjectData> dbContextFactor , TokenHandler tokenHandler, ISettingsService settingsService)
        {
            _httpClient = httpClient;
            _dbContextFactory = dbContextFactor;
            _context = _dbContextFactory.CreateDbContext();
            _tokenHandler = tokenHandler;
            _settingsService = settingsService;
        }

        public async Task<LoginResponseToken> AuthenticateAsync(AuthDataHub authDataRequest)
        {
            
            var content = new StringContent(JsonSerializer.Serialize(authDataRequest), Encoding.UTF8, "application/json");

            // Retrieve the ExportURL setting
           var exportUrlSetting = await _settingsService.GetByName(new SettingName { name = "ExportURL" });
            var exportUrl = exportUrlSetting?.FirstOrDefault()?.Value;

            var response = await _httpClient.PostAsync($"{exportUrl}/users/login", content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var jsonResponse = JsonSerializer.Deserialize<Dictionary<string, string>>(responseContent);

                if (jsonResponse != null && jsonResponse.TryGetValue("token", out var token))
                {
                    // Save the token in the TokenHandler
                    _tokenHandler.Token = token;

                    return new LoginResponseToken
                    {
                        Token = token
                    };
                }
            }

            throw new Exception("Failed to authenticate");
        }
    }
}
