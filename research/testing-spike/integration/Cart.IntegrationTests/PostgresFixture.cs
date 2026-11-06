using Testcontainers.PostgreSql;
using Testcontainers.Xunit;
using DotNet.Testcontainers.Builders;
using Xunit.Sdk;

[assembly: AssemblyFixture(typeof(Cart.IntegrationTests.PostgresFixture))]

namespace Cart.IntegrationTests;

/// One PostgreSQL container for the whole test assembly (xUnit v3 assembly fixture).
public sealed class PostgresFixture(IMessageSink sink)
    : ContainerFixture<PostgreSqlBuilder, PostgreSqlContainer>(sink)
{
    protected override PostgreSqlBuilder Configure() =>
        new PostgreSqlBuilder("postgres:17-alpine");
}
