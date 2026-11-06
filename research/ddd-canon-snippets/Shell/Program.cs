using Akka.Cluster.Hosting;
using Akka.Hosting;
using Akka.Persistence.Hosting;
using Akka.Remote.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Sales.Shell;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddAkka("sales", (akka, sp) =>
{
    akka.WithRemoting("localhost", 0)
        .WithClustering(new ClusterOptions { SeedNodes = ["akka.tcp://sales@localhost:0"], Roles = ["sales"] })
        .WithInMemoryJournal()
        .WithInMemorySnapshotStore()
        .WithShardRegion<OrderActor>(
            "order",
            (_, _, resolver) => id => OrderActor.PropsFor(id, sp.GetRequiredService<TimeProvider>()),
            new OrderMessageExtractor(),
            new ShardOptions { StateStoreMode = Akka.Cluster.Sharding.StateStoreMode.DData, Role = "sales" });
});
// await builder.Build().RunAsync();
