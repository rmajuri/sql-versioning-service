using SqlVersioningService.IntegrationTests.Infrastructure;
using SqlVersioningService.Models;
using SqlVersioningService.Repositories;
using Xunit;

namespace SqlVersioningService.IntegrationTests.Repositories;

[Collection("Postgres")]
[Trait("Category", "Integration")]
public class QueryRepositoryIntegrationTests
{
    private readonly PostgresFixture _fixture;

    public QueryRepositoryIntegrationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CreateAndFetchQuery_ById()
    {
        // Arrange
        var db = _fixture.CreateDatabaseContext();
        var repo = new QueryRepository(db);

        var query = new Query
        {
            Id = Guid.NewGuid(),
            Name = "Integration Test",
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
    }

    [Fact]
    public async Task Versions_FormCorrectParentChain()
    {
        var db = _fixture.CreateDatabaseContext();
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
