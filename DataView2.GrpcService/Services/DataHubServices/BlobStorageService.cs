using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using DataView2.Core.Models.DataHub;

namespace DataView2.GrpcService.Services.DataHubServices
{
    public class BlobStorageService : IBlobStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _containerName = "surveyimg";

        public BlobStorageService(BlobServiceClient blobServiceClient)
        {
            _blobServiceClient = blobServiceClient;
        }

        public async Task<string> UploadImageAsync(Stream imageStream, string fileName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobHttpHeaders = new BlobHttpHeaders { ContentType = "image/jpeg" };

            var blobClient = containerClient.GetBlobClient(fileName);

            await blobClient.UploadAsync(imageStream, new BlobUploadOptions { HttpHeaders = blobHttpHeaders }); 
            return blobClient.Uri.ToString(); // Return the URL of the uploaded image

        }
    }
}
