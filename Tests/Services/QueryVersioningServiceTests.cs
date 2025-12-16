using Moq;
using SqlVersioningService.Models;
using SqlVersioningService.Repositories;
using SqlVersioningService.Services;
using Xunit;

public class QueryVersioningServiceTests
{
    [Fact]
    public async Task CreateVersionAsync_UploadsBlob_WhenBlobDoesNotExist()
    {
        // Arrange
        var queryId = Guid.NewGuid();
        var sql = "SELECT * FROM users;";
        var hash = new HashingService().ComputeHash(sql);

        var queryRepo = new Mock<QueryRepository>(null!);
        var versionRepo = new Mock<VersionRepository>(null!);
        var blobRepo = new Mock<SqlBlobRepository>(null!);
        var blobStorage = new Mock<IBlobStorageService>();

        blobRepo.Setup(r => r.ExistsAsync(hash)).ReturnsAsync(false);

        versionRepo.Setup(r => r.GetHeadVersionIdAsync(queryId)).ReturnsAsync((Guid?)null);

        var service = new QueryVersioningService(
            queryRepo.Object,
            versionRepo.Object,
            blobRepo.Object,
            blobStorage.Object,
            new HashingService()
        );

        // Act
        var version = await service.CreateVersionAsync(queryId, sql, "initial");

        // Assert
        blobStorage.Verify(b => b.UploadAsync(hash, sql), Times.Once);

        blobRepo.Verify(
            r =>
                r.CreateIfNotExistsAsync(
                    It.Is<SqlBlob>(b => b.Hash == hash && b.BytesSize == sql.Length)
                ),
            Times.Once
        );

        versionRepo.Verify(r => r.CreateAsync(It.IsAny<QueryVersion>()), Times.Once);

        queryRepo.Verify(r => r.UpdateHeadVersionAsync(queryId, version.Id), Times.Once);
    }

    [Fact]
    public async Task CreateVersionAsync_DoesNotUploadBlob_WhenBlobExists()
    {
        // Arrange
        var queryId = Guid.NewGuid();
        var sql = "SELECT * FROM users;";
        var hash = new HashingService().ComputeHash(sql);

        var queryRepo = new Mock<QueryRepository>(null!);
        var versionRepo = new Mock<VersionRepository>(null!);
        var blobRepo = new Mock<SqlBlobRepository>(null!);
        var blobStorage = new Mock<IBlobStorageService>();

        blobRepo.Setup(r => r.ExistsAsync(hash)).ReturnsAsync(true);

        versionRepo.Setup(r => r.GetHeadVersionIdAsync(queryId)).ReturnsAsync(Guid.NewGuid());

        var service = new QueryVersioningService(
            queryRepo.Object,
            versionRepo.Object,
            blobRepo.Object,
            blobStorage.Object,
            new HashingService()
        );

        // Act
        await service.CreateVersionAsync(queryId, sql, null);

        // Assert
        blobStorage.Verify(b => b.UploadAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);

        blobRepo.Verify(r => r.CreateIfNotExistsAsync(It.IsAny<SqlBlob>()), Times.Never);
    }
}
