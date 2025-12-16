using SqlVersioningService.Models;
using SqlVersioningService.Repositories;
using SqlVersioningService.Tests.Infrastructure;
using Xunit;

public class QueryRepositoryIntegrationTests
{
    [Fact]
    public async Task CreateAndFetchQuery_ById()
    {
        // Arrange
        var db = TestDatabaseFactory.Create(); // wraps connection string
        var repo = new QueryRepository(db);

        var query = new Query
        {
            Id = Guid.NewGuid(),
            Name = "Integration Test",
            OrganizationId = Guid.NewGuid(),
            OwnerUserId = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            IsDeleted = false,
        };

        // Act
        await repo.CreateAsync(query);
        var fetched = await repo.GetByIdAsync(query.Id);

        // Assert
        Assert.NotNull(fetched);
        Assert.Equal(query.Name, fetched!.Name);
        Assert.Equal(query.OrganizationId, fetched.OrganizationId);
    }

    [Fact]
    public async Task Versions_FormCorrectParentChain()
    {
        var db = TestDatabaseFactory.Create();
        var queryRepo = new QueryRepository(db);
        var versionRepo = new VersionRepository(db);

        var query = new Query
        {
            Id = Guid.NewGuid(),
            Name = "Version Chain",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        await queryRepo.CreateAsync(query);

        var v1 = new QueryVersion
        {
            Id = Guid.NewGuid(),
            QueryId = query.Id,
            BlobHash = "hash1",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        await versionRepo.CreateAsync(v1);
        await queryRepo.UpdateHeadVersionAsync(query.Id, v1.Id);

        var v2 = new QueryVersion
        {
            Id = Guid.NewGuid(),
            QueryId = query.Id,
            ParentVersionId = v1.Id,
            BlobHash = "hash2",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        await versionRepo.CreateAsync(v2);

        var versions = await versionRepo.GetAllVersionsAsync(query.Id);

        Assert.Equal(2, versions.Count());
        Assert.Contains(versions, v => v.ParentVersionId == v1.Id);
    }
}
