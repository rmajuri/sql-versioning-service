using Moq;
using SqlVersioningService.Models;
using SqlVersioningService.Services;
using Xunit;

namespace SqlVersioningService.UnitTests.Services;

public class QueryCreationServiceTests
{
    [Fact]
    public async Task CreateQueryAsync_CreatesQuery_AndInitialVersion()
    {
        // Arrange
        var queryService = new Mock<QueryService>(null!);
        var versionService = new Mock<QueryVersioningService>(null!, null!, null!, null!, null!);

        queryService.Setup(q => q.CreateAsync(It.IsAny<Query>())).ReturnsAsync(Guid.NewGuid());

        versionService
            .Setup(v =>
                v.CreateVersionAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string?>())
            )
            .ReturnsAsync(new QueryVersion { Id = Guid.NewGuid() });

        var creationService = new QueryCreationService(queryService.Object, versionService.Object);

        // Act
        var result = await creationService.CreateQueryAsync(
            "Test Query",
            Guid.NewGuid(),
            Guid.NewGuid(),
            "SELECT 1;",
            "init"
        );

        // Assert
        queryService.Verify(q => q.CreateAsync(It.IsAny<Query>()), Times.Once);
        versionService.Verify(
            v => v.CreateVersionAsync(It.IsAny<Guid>(), "SELECT 1;", "init"),
            Times.Once
        );

        Assert.NotNull(result.Query);
        Assert.NotNull(result.Version);
    }
}
