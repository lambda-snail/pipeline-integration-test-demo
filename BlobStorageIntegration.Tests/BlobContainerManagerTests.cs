namespace BlobStorageIntegration.Tests;

using AutoFixture;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage;
using AutoFixture.Kernel;

public class BlobContainerManagerTests
{
    Fixture _fixture;

    public BlobContainerManagerTests()
    {
        _fixture = new();
    }

    internal class ConnectionStringContainer
    {
        public string ConnectionString { get; set; }
    }

    [Fact]
    public async Task SaveBlobAsync_ShouldSaveTextToStorage()
    {
        string blobName = "testblob";
        string blobContent = "testcontent";

        // Arrange
        string containerName = GetRandomContainerName();
        string connectionString = "UseDevelopmentStorage=true";
        BlobContainerManager containerManager = new(connectionString);

        // Act
        await containerManager.InitBlobContainerManager(containerName);
        await containerManager.SaveBlobAsync(blobName, blobContent);

        // Assert
        CloudStorageAccount.TryParse(connectionString, out CloudStorageAccount stAccount); // Should be in arrange?
        CloudBlobClient? client = stAccount.CreateCloudBlobClient();
        CloudBlobContainer container = client.GetContainerReference(containerName);

        CloudBlockBlob blob = container.GetBlockBlobReference(blobName);
        string savedContent = await blob.DownloadTextAsync();

        Assert.Equal(blobContent, savedContent);
    }

    private string GetRandomContainerName()
    {
        var pattern = @"[a-z][a-z][a-z][a-z][a-z]";
        var connectionStringGenerator =
            new SpecimenContext(_fixture).Resolve(
                new RegularExpressionRequest(pattern));
        _fixture.Customize<ConnectionStringContainer>(c => c.With(x => x.ConnectionString, connectionStringGenerator));
        string containerName = _fixture.Create<ConnectionStringContainer>().ConnectionString;
        return containerName;
    }
}