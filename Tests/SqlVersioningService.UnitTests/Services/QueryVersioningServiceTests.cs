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

    [Fact]
    public async Task GetVersionsForQueryAsync_ReturnsList_WhenVersionsExist()
    {
        // Arrange
        var queryId = Guid.NewGuid();
        var versions = new List<QueryVersion>
        {
            new QueryVersion
            {
                Id = Guid.NewGuid(),
                QueryId = queryId,
                BlobHash = "hash1",
            },
            new QueryVersion
            {
                Id = Guid.NewGuid(),
                QueryId = queryId,
                BlobHash = "hash2",
            },
        };

        var queryRepo = new Mock<IQueryRepository>();
        var versionRepo = new Mock<IVersionRepository>();
        var blobRepo = new Mock<ISqlBlobRepository>();
        var blobStorage = new Mock<IBlobStorageService>();
        var hashingService = new HashingService();

        versionRepo.Setup(r => r.GetAllVersionsAsync(queryId)).ReturnsAsync(versions);

        var service = new QueryVersioningService(
            queryRepo.Object,
            versionRepo.Object,
            blobRepo.Object,
            blobStorage.Object,
            hashingService
        );

        // Act
        var result = await service.GetVersionsForQueryAsync(queryId);

        // Assert
        Assert.Equal(2, result.Count());
        versionRepo.Verify(r => r.GetAllVersionsAsync(queryId), Times.Once);
    }

    [Fact]
    public async Task GetVersionByIdAsync_ReturnsVersion_WhenFound()
    {
        // Arrange
        var versionId = Guid.NewGuid();
        var expectedVersion = new QueryVersion
        {
            Id = versionId,
            QueryId = Guid.NewGuid(),
            BlobHash = "somehash",
        };

        var queryRepo = new Mock<IQueryRepository>();
        var versionRepo = new Mock<IVersionRepository>();
        var blobRepo = new Mock<ISqlBlobRepository>();
        var blobStorage = new Mock<IBlobStorageService>();
        var hashingService = new HashingService();

        versionRepo.Setup(r => r.GetByIdAsync(versionId)).ReturnsAsync(expectedVersion);

        var service = new QueryVersioningService(
            queryRepo.Object,
            versionRepo.Object,
            blobRepo.Object,
            blobStorage.Object,
            hashingService
        );

        // Act
        var result = await service.GetVersionByIdAsync(versionId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(versionId, result.Id);
        versionRepo.Verify(r => r.GetByIdAsync(versionId), Times.Once);
    }

    [Fact]
    public async Task GetVersionByIdAsync_ReturnsNull_WhenNotFound()
    {
        // Arrange
        var versionId = Guid.NewGuid();

        var queryRepo = new Mock<IQueryRepository>();
        var versionRepo = new Mock<IVersionRepository>();
        var blobRepo = new Mock<ISqlBlobRepository>();
        var blobStorage = new Mock<IBlobStorageService>();
        var hashingService = new HashingService();

        versionRepo.Setup(r => r.GetByIdAsync(versionId)).ReturnsAsync((QueryVersion?)null);

        var service = new QueryVersioningService(
            queryRepo.Object,
            versionRepo.Object,
            blobRepo.Object,
            blobStorage.Object,
            hashingService
        );

        // Act
        var result = await service.GetVersionByIdAsync(versionId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetSqlForVersionAsync_ReturnsNull_WhenVersionNotFound()
    {
        // Arrange
        var versionId = Guid.NewGuid();

        var queryRepo = new Mock<IQueryRepository>();
        var versionRepo = new Mock<IVersionRepository>();
        var blobRepo = new Mock<ISqlBlobRepository>();
        var blobStorage = new Mock<IBlobStorageService>();
        var hashingService = new HashingService();

        versionRepo.Setup(r => r.GetByIdAsync(versionId)).ReturnsAsync((QueryVersion?)null);

        var service = new QueryVersioningService(
            queryRepo.Object,
            versionRepo.Object,
            blobRepo.Object,
            blobStorage.Object,
            hashingService
        );

        // Act
        var result = await service.GetSqlForVersionAsync(versionId);

        // Assert
        Assert.Null(result);
        blobStorage.Verify(b => b.DownloadAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetSqlForVersionAsync_DownloadsFromBlobStorage_WhenVersionFound()
    {
        // Arrange
        var versionId = Guid.NewGuid();
        var blobHash = "somehash";
        var expectedSql = "SELECT * FROM orders;";
        var version = new QueryVersion
        {
            Id = versionId,
            QueryId = Guid.NewGuid(),
            BlobHash = blobHash,
        };

        var queryRepo = new Mock<IQueryRepository>();
        var versionRepo = new Mock<IVersionRepository>();
        var blobRepo = new Mock<ISqlBlobRepository>();
        var blobStorage = new Mock<IBlobStorageService>();
        var hashingService = new HashingService();

        versionRepo.Setup(r => r.GetByIdAsync(versionId)).ReturnsAsync(version);
        blobStorage.Setup(b => b.DownloadAsync(blobHash)).ReturnsAsync(expectedSql);

        var service = new QueryVersioningService(
            queryRepo.Object,
            versionRepo.Object,
            blobRepo.Object,
            blobStorage.Object,
            hashingService
        );

        // Act
        var result = await service.GetSqlForVersionAsync(versionId);

        // Assert
        Assert.Equal(expectedSql, result);
        versionRepo.Verify(r => r.GetByIdAsync(versionId), Times.Once);
        blobStorage.Verify(b => b.DownloadAsync(blobHash), Times.Once);
    }
}
