using Xunit;

namespace SqlVersioningService.IntegrationTests.Infrastructure;

/// <summary>
/// Defines the "Postgres" collection for integration tests.
/// All tests in this collection share a single PostgresFixture instance,
/// meaning schema is created once per test run.
/// </summary>
[CollectionDefinition("Postgres")]
public class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    // This class has no code and is never instantiated.
    // Its purpose is to define the test collection and associate it with PostgresFixture.
}
