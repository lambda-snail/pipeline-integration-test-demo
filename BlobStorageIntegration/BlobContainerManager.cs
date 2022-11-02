using System;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

#nullable enable

namespace BlobStorageIntegration;

/// <summary>
/// Encapsulates operations and functionality related to blob storage in Azure.
/// </summary>
public class BlobContainerManager
{
    string? _connectionString;
    CloudBlobContainer? _container;

    public bool IsInitialized => _container is not null;

    public BlobContainerManager(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException("Error: Connection string must not be null.");
    }

    public async Task InitBlobContainerManager(string containerName)
    {
        CloudStorageAccount? stAccount = null;
        if (CloudStorageAccount.TryParse(_connectionString, out stAccount))
        {
            CloudBlobClient client = stAccount.CreateCloudBlobClient();
            _container = client.GetContainerReference(containerName);

            await _container.CreateIfNotExistsAsync();
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
