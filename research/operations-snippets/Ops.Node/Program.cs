using System.Reflection;
using Akka.Cluster.Hosting;
using Akka.Cluster.Hosting.SBR;
using Akka.Coordination.KubernetesApi;
using Akka.Discovery.KubernetesApi;
using Akka.Hosting;
using Akka.Management;
using Akka.Management.Cluster.Bootstrap;
using Akka.Persistence.Sql.Hosting;
using Akka.Remote.Hosting;
using LinqToDB;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.FeatureManagement;

var builder = WebApplication.CreateBuilder(args);
var cfg = builder.Configuration;

// AppVersion: MAJOR.MINOR.PATCH only — Akka's AppVersion parser rejects dotted pre-release ids like "1.2.3-rc.1".
var appVersion = cfg["Akka:AppVersion"]
    ?? typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!
           .InformationalVersion.Split('+', '-')[0];
var inKubernetes = Environment.GetEnvironmentVariable("KUBERNETES_SERVICE_HOST") is not null;

builder.Services.AddFeatureManagement();
builder.Services.AddHealthChecks();
builder.Services.AddAkka("orders", (akka, sp) =>
{
    akka.WithRemoting(new RemoteOptions { HostName = "0.0.0.0", PublicHostName = cfg["POD_IP"] ?? "localhost", Port = 5055 })
        .WithClustering(new ClusterOptions
        {
            Roles = ["orders"],
            AppVersion = appVersion,
            SeedNodes = inKubernetes ? [] : ["akka.tcp://orders@localhost:5055"],
            SplitBrainResolver = inKubernetes
                ? new LeaseMajorityOption { LeaseImplementation = new KubernetesLeaseOption() }
                : SplitBrainResolverOption.Default,   // keep-majority
        })
        .WithActorSystemLivenessCheck()
        .WithAkkaClusterReadinessCheck()
        .WithSqlPersistence(cfg.GetConnectionString("journal")!, ProviderName.PostgreSQL15,
                            autoInitialize: false,               // schema owned by migrations, not by app start-up
                            journalBuilder: j => j.WithHealthCheck().WithConnectivityCheck(),
                            snapshotBuilder: s => s.WithHealthCheck());
    if (inKubernetes)
    {
        akka.WithAkkaManagement(port: 8558)
            .WithClusterBootstrap(o =>
            {
                o.ContactPointDiscovery.ServiceName = "orders";
                o.ContactPointDiscovery.RequiredContactPointsNr = 2;
                o.ContactPointDiscovery.ContactWithAllContactPoints = true;
                o.ContactPointDiscovery.StableMargin = TimeSpan.FromSeconds(5);
            }, autoStart: true)
            .WithKubernetesDiscovery();
    }
});

var app = builder.Build();
app.MapHealthChecks("/healthz/live", new HealthCheckOptions { Predicate = c => c.Tags.Contains("liveness") });
app.MapHealthChecks("/healthz/ready", new HealthCheckOptions { Predicate = c => c.Tags.Contains("ready") || c.Tags.Contains("persistence") });
app.MapGet("/version", () => new { appVersion });
app.MapGet("/pricing", async (IFeatureManager fm) => await fm.IsEnabledAsync("NewPricing") ? "v2" : "v1");
app.Run();
