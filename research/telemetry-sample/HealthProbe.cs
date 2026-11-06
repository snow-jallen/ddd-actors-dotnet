using Akka.Cluster.Hosting;
using Akka.Hosting;
using Akka.Persistence.Hosting;
using Akka.Persistence.Sql.Hosting;
using LinqToDB;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace OtelAkka;

public static class ClusterHealthWiring
{
    // Compile-only: the shape of production health wiring for a clustered, persistent node.
    public static AkkaConfigurationBuilder AddProductionHealthChecks(this AkkaConfigurationBuilder akka, string connStr) => akka
        .WithClustering(new ClusterOptions { Roles = ["ordering"] })
        .WithActorSystemLivenessCheck(tags: ["live"])                          // Unhealthy once the ActorSystem terminates
        .WithAkkaClusterReadinessCheck()                                       // Unhealthy until this node has joined; tagged "ready"
        .WithSqlPersistence(
            connectionString: connStr,
            providerName: ProviderName.PostgreSQL15,
            journalBuilder: journal =>
            {
                journal.WithHealthCheck(HealthStatus.Degraded);   // tags: akka, persistence, journal
                journal.WithConnectivityCheck();                  // actively pings the DB even when idle
            },
            snapshotBuilder: snapshot => snapshot.WithHealthCheck(HealthStatus.Degraded))
        .WithHealthCheck("order-registry", async (system, registry, ct) =>
            await registry.GetAsync<OrderActor>(ct) is not null
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("OrderActor not registered"),
            HealthStatus.Unhealthy, ["ready"]);
}
