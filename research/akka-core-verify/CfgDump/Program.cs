using Microsoft.Extensions.Logging;
using Akka.Actor;
using Akka.Cluster.Hosting;
using Akka.Cluster.Sharding;
using Akka.Hosting;
using Akka.Remote.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var b = Host.CreateApplicationBuilder();
b.Logging.ClearProviders();
b.Services.AddAkka("cfg", (akka, _) =>
{
    akka.WithRemoting("localhost", 8111)
        .WithClustering(new ClusterOptions { SeedNodes = ["akka.tcp://cfg@localhost:8111"] })
        .WithShardRegion<Dummy>("dummy", id => Props.Create(() => new Dummy()), new HashCodeMessageExtractorImpl(10), new ShardOptions());
});
using var host = b.Build();
await host.StartAsync();
var sys = host.Services.GetRequiredService<ActorSystem>();
var c = sys.Settings.Config;
string[] keys = [
 "akka.actor.provider","akka.actor.serialize-messages","akka.actor.ask-timeout",
 "akka.actor.serialization-bindings","akka.actor.serialization-settings.allow-unregistered-types",
 "akka.cluster.downing-provider-class","akka.cluster.split-brain-resolver.active-strategy","akka.cluster.split-brain-resolver.stable-after","akka.cluster.split-brain-resolver.down-all-when-unstable",
 "akka.cluster.sharding.state-store-mode","akka.cluster.sharding.remember-entities","akka.cluster.sharding.remember-entities-store","akka.cluster.sharding.passivate-idle-entity-after",
 "akka.cluster.sharding.least-shard-allocation-strategy.rebalance-absolute-limit","akka.cluster.sharding.least-shard-allocation-strategy.rebalance-relative-limit",
 "akka.cluster.run-coordinated-shutdown-when-down","akka.coordinated-shutdown.run-by-clr-shutdown-hook","akka.coordinated-shutdown.exit-clr","akka.coordinated-shutdown.default-phase-timeout",
 "akka.cluster.app-version","akka.persistence.journal.plugin","akka.persistence.snapshot-store.plugin","akka.cluster.sharding.verbose-debug-logging",
 "akka.cluster.sharding.journal-plugin-id","akka.cluster.sharding.distributed-data.durable.keys"];
foreach (var k in keys) { try { Console.WriteLine($"{k} = {(c.HasPath(k) ? c.GetValue(k).ToString() : "<missing>")}"); } catch (Exception e) { Console.WriteLine($"{k} = ERR {e.Message}"); } }
Console.WriteLine("phases:");
var phases = c.GetConfig("akka.coordinated-shutdown.phases");
foreach (var p in phases.AsEnumerable().Select(kv => kv.Key.Split('.')[0]).Distinct())
{
    var pc = phases.GetConfig(p);
    Console.WriteLine($"  {p}: timeout={(pc.HasPath("timeout") ? pc.GetString("timeout") : "default")} depends-on=[{(pc.HasPath("depends-on") ? string.Join(",", pc.GetStringList("depends-on")) : "")}]");
}
var ra = host.Services.GetRequiredService<IRequiredActor<Dummy>>();
Console.WriteLine("IRequiredActor props: " + string.Join(",", ra.GetType().GetInterfaces()[0].GetProperties().Select(p => p.Name)));
await host.StopAsync();

public sealed class Dummy : ReceiveActor { public Dummy() { ReceiveAny(_ => { }); } }
public sealed class HashCodeMessageExtractorImpl(int n) : HashCodeMessageExtractor(n) { public override string EntityId(object m) => m.ToString()!; }
