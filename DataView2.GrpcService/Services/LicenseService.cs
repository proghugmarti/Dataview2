namespace DataView2.GrpcService.Services
{
    public class LicenseService
    {
        private readonly IConfiguration _configuration;
        public LicenseService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GetLicenseFilePath()
        {
            return _configuration["LicenseSettings:LicenseFilePath"];
        }
    }
}
