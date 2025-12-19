using Moq;
using SqlVersioningService.Models;
using SqlVersioningService.Repositories;
using SqlVersioningService.Services;
using Xunit;

namespace SqlVersioningService.UnitTests.Services;

public class QueryVersioningServiceTests
{
    [Fact]
    public async Task CreateVersionAsync_UploadsBlobAndCreatesMetadata_WhenBlobDoesNotExist()
    {
        // Arrange
        var queryId = Guid.NewGuid();
        var sql = "SELECT * FROM widgets;";
        var hashingService = new HashingService();
        var hash = hashingService.ComputeHash(sql);

        var queryRepo = new Mock<IQueryRepository>();
        var versionRepo = new Mock<IVersionRepository>();
        var blobRepo = new Mock<ISqlBlobRepository>();
        var blobStorage = new Mock<IBlobStorageService>();

        blobRepo.Setup(r => r.ExistsAsync(hash)).ReturnsAsync(false);

        versionRepo.Setup(r => r.GetHeadVersionIdAsync(queryId)).ReturnsAsync((Guid?)null);

        var service = new QueryVersioningService(
            queryRepo.Object,
            versionRepo.Object,
            blobRepo.Object,
            blobStorage.Object,
            hashingService
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

        versionRepo.Verify(
            r =>
                r.CreateAsync(
                    It.Is<QueryVersion>(v =>
                        v.QueryId == queryId && v.BlobHash == hash && v.ParentVersionId == null
                    )
                ),
            Times.Once
        );

        queryRepo.Verify(r => r.UpdateHeadVersionAsync(queryId, version.Id), Times.Once);
    }

    [Fact]
    public async Task CreateVersionAsync_DoesNotUploadBlobOrCreateMetadata_WhenBlobAlreadyExists()
    {
        // Arrange
        var queryId = Guid.NewGuid();
        var sql = "SELECT * FROM widgets;";
        var hashingService = new HashingService();
        var hash = hashingService.ComputeHash(sql);
        var existingHeadVersionId = Guid.NewGuid();

        var queryRepo = new Mock<IQueryRepository>();
        var versionRepo = new Mock<IVersionRepository>();
        var blobRepo = new Mock<ISqlBlobRepository>();
        var blobStorage = new Mock<IBlobStorageService>();

        blobRepo.Setup(r => r.ExistsAsync(hash)).ReturnsAsync(true);

        versionRepo
            .Setup(r => r.GetHeadVersionIdAsync(queryId))
            .ReturnsAsync(existingHeadVersionId);

        var service = new QueryVersioningService(
            queryRepo.Object,
            versionRepo.Object,
            blobRepo.Object,
            blobStorage.Object,
            hashingService
        );

        // Act
        await service.CreateVersionAsync(queryId, sql, null);

        // Assert
        blobStorage.Verify(b => b.UploadAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);

        blobRepo.Verify(r => r.CreateIfNotExistsAsync(It.IsAny<SqlBlob>()), Times.Never);

        versionRepo.Verify(
            r =>
                r.CreateAsync(
                    It.Is<QueryVersion>(v =>
                        v.QueryId == queryId
                        && v.BlobHash == hash
                        && v.ParentVersionId == existingHeadVersionId
                    )
                ),
            Times.Once
        );

        queryRepo.Verify(r => r.UpdateHeadVersionAsync(queryId, It.IsAny<Guid>()), Times.Once);
    }
}
