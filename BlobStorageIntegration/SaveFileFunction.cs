using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

#nullable enable

namespace BlobStorageIntegration
{
    public class SaveFileFunction
    {
        private readonly ILogger<SaveFileFunction> _logger;
        private readonly BlobContainerManager _blobManager;

        public SaveFileFunction(ILogger<SaveFileFunction> log)
        {
            _logger = log;
            _blobManager = new();
        }

        [FunctionName("SaveFile")]
        [OpenApiOperation(operationId: "Run", tags: new[] { "blob" })]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiParameter(name: "containerName", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "The name of the blob storage to save in.")]
        [OpenApiParameter(name: "blobName", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "The name of the blob to store.")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(string), Description = "If the file was saved successfully.")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "containers/{containerName}/blobs/{blobName}")] HttpRequest req,
            [FromRoute] string containerName,
            [FromRoute] string blobName,
            [FromBody] string content
            )
        {
            _logger.LogInformation("Save {bob} to {storage}", blobName, containerName);

            if(string.IsNullOrWhiteSpace(content))
            {
                return new BadRequestResult();
            }

            await _blobManager.InitBlobContainerManager(containerName);
            await _blobManager.SaveBlobAsync(blobName, content);

            return new OkResult();
        }
    }

    /// <summary>
    /// Encapsulates operations and functionality related to blob storage in Azure.
    /// </summary>
    public class BlobContainerManager
    {
        CloudBlobContainer? _container;

        public bool IsInitialized => _container is not null;

        public async Task InitBlobContainerManager(string containerName)
        {
            CloudStorageAccount? stAccount = null;
            if(CloudStorageAccount.TryParse(Environment.GetEnvironmentVariable("StorageAccountConnectionString"), out stAccount))
            {
                CloudBlobClient client = stAccount.CreateCloudBlobClient();
                CloudBlobContainer container = client.GetContainerReference(containerName);

                await container.CreateIfNotExistsAsync();
            }
        }

        public Task SaveBlobAsync(string blobName, string content)
        {
            EnsureInitialized();
            CloudBlockBlob blob = _container!.GetBlockBlobReference(blobName);
            return blob.UploadTextAsync(content);
        }

        public Task<string> ReadBlobAsync(string blobName)
        {
            EnsureInitialized();
            CloudBlockBlob blob = _container!.GetBlockBlobReference(blobName);
            return blob.DownloadTextAsync();
        }

        private void EnsureInitialized()
        {
            if (!IsInitialized) 
            { 
                throw new InvalidOperationException("Error: Attempt to operate on an uninitialized blob container manager."); 
            }
        }
    }
}

