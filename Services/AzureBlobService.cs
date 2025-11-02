using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Rently.Api.Services
{
    public interface IBlobService
    {
        Task<string> UploadAsync(string containerName, Stream fileStream, string fileName);
        string GenerateReadSasUrl(string containerName, string fileName, int expireMinutes = 10);
    }

    public class AzureBlobService : IBlobService
    {
        private readonly BlobServiceClient _blobServiceClient;

        public AzureBlobService(IConfiguration config)
        {
            _blobServiceClient = new BlobServiceClient(config["AzureBlob:ConnectionString"]);
        }

        public async Task<string> UploadAsync(string containerName, Stream fileStream, string fileName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync();
            await containerClient.UploadBlobAsync(fileName, fileStream);

            return $"{containerClient.Uri}/{fileName}";
        }

        public string GenerateReadSasUrl(string containerName, string fileName, int expireMinutes = 10)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(fileName);

            if (!blobClient.CanGenerateSasUri)
                throw new InvalidOperationException("Blob client cannot generate SAS URI. Use a connection string with account key.");

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = containerName,
                BlobName = fileName,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(expireMinutes)
            };
            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            var sasUri = blobClient.GenerateSasUri(sasBuilder);
            return sasUri.ToString();
        }
    }
}
