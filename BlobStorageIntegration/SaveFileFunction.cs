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
            _blobManager = new(Environment.GetEnvironmentVariable("AzureWebJobsStorage")!);
        }

        [FunctionName("SaveFile")]
        [OpenApiOperation(operationId: "Run", tags: new[] { "blob" })]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiParameter(name: "containerName", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "The name of the blob storage to save in.")]
        [OpenApiParameter(name: "blobName", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "The name of the blob to store.")]
        [OpenApiRequestBody(contentType: "text/plain", bodyType: typeof(string), Description = "The content of the file.", Required = true)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "If the file was saved successfully.")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "containers/{containerName}/blobs/{blobName}")] HttpRequest req,
            [FromRoute] string containerName,
            [FromRoute] string blobName
            )
        {
            _logger.LogInformation("Save {blob} to {storage}", blobName, containerName);

            string content = await req.ReadAsStringAsync();
            if(string.IsNullOrWhiteSpace(content))
            {
                return new BadRequestResult();
            }

            await _blobManager.InitBlobContainerManager(containerName);
            await _blobManager.SaveBlobAsync(blobName, content);

            return new OkResult();
        }
    }
}
