using Akka;
using Akka.Event;
using Akka.Actor;
using Akka.Cluster.Hosting;
using Akka.Cluster.Sharding;
using Akka.Hosting;
using Akka.Persistence.Hosting;
using Akka.Persistence.Query;
using Akka.Persistence.Sql.Hosting;
using Akka.Persistence.Sql.Query;
using Akka.Remote.Hosting;
using Akka.Streams;
using Akka.Streams.Dsl;
using LinqToDB;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shop.Actors;
using Shop.Domain;

var dbPath = Path.Combine(AppContext.BaseDirectory, "shop.db");
if (args is ["--fresh", ..] && File.Exists(dbPath)) File.Delete(dbPath);
var connStr = $"Data Source={dbPath}";

IHost BuildHost()
{
    var builder = Host.CreateApplicationBuilder();
    builder.Services.AddAkka("shop", (akka, sp) =>
    {
        akka
            .ConfigureLoggers(l => l.AddLoggerFactory())
            .WithRemoting(hostname: "localhost", port: 8110)
            .WithClustering(new ClusterOptions
            {
                Roles = ["orders"],
                SeedNodes = ["akka.tcp://shop@localhost:8110"],
            })
            .WithSqlPersistence(
                connectionString: connStr,
                providerName: ProviderName.SQLiteMS,
                mode: PersistenceMode.Both,
                autoInitialize: true,
                journalBuilder: j => j.AddWriteEventAdapter<OrderTaggingAdapter>("order-tagger", [typeof(IOrderEvent)]))
            .WithShardRegion<OrderRegion>(
                typeName: "order",
                entityPropsFactory: entityId => Props.Create(() => new OrderActor(entityId)),
                messageExtractor: new OrderMessageExtractor(maxShards: 50),
                shardOptions: new ShardOptions
                {
                    Role = "orders",
                    StateStoreMode = StateStoreMode.DData,
                    RememberEntities = false,
                    PassivateIdleEntityAfter = TimeSpan.FromMinutes(2),
                })
            .WithActors((system, registry, resolver) =>
            {
                var cache = system.ActorOf(BackoffExample.Wrap(Props.Create(() => new PriceCacheActor())), "price-cache-supervisor");
                registry.Register<PriceCacheActor>(cache);
            })
            .AddStartup((system, registry) =>
            {
                CoordinatedShutdown.Get(system).AddTask(CoordinatedShutdown.PhaseBeforeServiceUnbind, "log-goodbye", () =>
                {
                    system.Log.Info("before-service-unbind: draining");
                    return Task.FromResult(Done.Instance);
                });
            });
    });
    return builder.Build();
}

// ---- Run 1: write ----
using (var host = BuildHost())
{
    await host.StartAsync();
    var region = await host.Services.GetRequiredService<IRequiredActor<OrderRegion>>().GetAsync();
    var t = TimeSpan.FromSeconds(10);
    var id = "o-1";
    Console.WriteLine(await region.Ask<object>(new PlaceOrder(id, "c-42"), t));
    Console.WriteLine(await region.Ask<object>(new AddItem(id, "sku-1", 2, 9.99m), t));
    Console.WriteLine(await region.Ask<object>(new AddItem(id, "sku-2", 0, 1m), t));
    Console.WriteLine(await region.Ask<object>(new AddItem(id, "sku-2", 1, 5m), t));
    Console.WriteLine(await region.Ask<object>(new SubmitOrder(id), t));
    Console.WriteLine(await region.Ask<object>(new GetOrder(id), t));

    var cache = await host.Services.GetRequiredService<IRequiredActor<PriceCacheActor>>().GetAsync();
    Console.WriteLine(await cache.Ask<PriceCacheActor.Price>(new PriceCacheActor.GetPrice("sku-1"), t));

    // ---- Persistence Query: read side ----
    var system = host.Services.GetRequiredService<ActorSystem>();
    var readJournal = PersistenceQuery.Get(system).ReadJournalFor<SqlReadJournal>(SqlReadJournal.Identifier);
    var events = await readJournal
        .CurrentEventsByTag("order", Offset.NoOffset())
        .Select(env => $"{env.PersistenceId}#{env.SequenceNr} offset={((Sequence)env.Offset).Value} {env.Event.GetType().Name}")
        .RunWith(Sink.Seq<string>(), system.Materializer());
    foreach (var e in events) Console.WriteLine("  [tag:order] " + e);

    var byPid = await readJournal
        .CurrentEventsByPersistenceId("order-" + id, 0, long.MaxValue)
        .RunWith(Sink.Seq<EventEnvelope>(), system.Materializer());
    Console.WriteLine($"  events for order-{id}: {byPid.Count}");
    var from2 = await readJournal.CurrentEventsByTag("order", Offset.Sequence(2)).Select(e => ((Sequence)e.Offset).Value).RunWith(Sink.Seq<long>(), system.Materializer());
    Console.WriteLine($"  CurrentEventsByTag from Sequence(2): [{string.Join(",", from2)}]");

    var store = new Shop.Projections.InMemoryOffsetStore();
    var seen = 0;
    var (ks, _) = Shop.Projections.OrderTotalsProjection.Run(system, store, _ => Interlocked.Increment(ref seen));
    await Task.Delay(TimeSpan.FromSeconds(3));
    Console.WriteLine($"  projection saw {seen} events, offset={await store.LoadAsync("order-totals")}");
    await region.Ask<object>(new PlaceOrder("o-2", "c-7"), t);
    await Task.Delay(TimeSpan.FromSeconds(3));
    Console.WriteLine($"  after one more event: projection saw {seen}, offset={await store.LoadAsync("order-totals")}");
    ks.Shutdown();

    await host.StopAsync();
}

// ---- Run 2: recover from the SQLite journal + snapshot ----
using (var host = BuildHost())
{
    await host.StartAsync();
    var region = await host.Services.GetRequiredService<IRequiredActor<OrderRegion>>().GetAsync();
    Console.WriteLine("after restart: " + await region.Ask<object>(new GetOrder("o-1"), TimeSpan.FromSeconds(10)));
    await host.StopAsync();
}
