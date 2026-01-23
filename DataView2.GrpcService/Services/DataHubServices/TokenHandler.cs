namespace DataView2.GrpcService.Services.DataHubServices
{
    public class TokenHandler
    {
        private string _token;

        public string Token
        {
            get => _token;
            set => _token = value;
        }

        public string GetToken() => _token;

        public void SetToken(string token)
        {
            _token = token;
        }
    }
}
