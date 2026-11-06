using System.Reflection;
using Akka.Actor;
using Akka.Cluster;
using Akka.Cluster.Hosting;
using Akka.Hosting;
using Akka.Persistence;
using Akka.Persistence.Hosting;
using Akka.Persistence.Sql.Hosting;
using Akka.Remote.Hosting;
using LinqToDB;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.FeatureManagement;
using Ops.Verify;

if (args.Contains("--cmp"))
{
    string[] vs = ["1.4.2", "1.4.3-alpha.0.1", "1.4.3-alpha", "1.4.3-rc1", "1.4.3", "1.10.0", "2.0.0-rc.1", "2.0.0", "1.4.2+5-abc123", "1.4.2+5.sha.abc", "1.4.3-preview-0001"];
    foreach (var x in vs)
    {
        try { Akka.Util.AppVersion.Create(x); Console.WriteLine($"OK    {x}"); }
        catch (Exception ex) { Console.WriteLine($"THROW {x}: {ex.Message}"); }
    }
    (string, string)[] pairs = [("1.4.3", "1.4.2"), ("1.10.0", "1.4.3"), ("1.4.3-rc1", "1.4.3"), ("1.4.3-alpha", "1.4.2"), ("1.4.2+5-abc123", "1.4.2"), ("1.4.3-preview-0001","1.4.3")];
    foreach (var (x, y) in pairs) Console.WriteLine($"{x} cmp {y} = {Akka.Util.AppVersion.Create(x).CompareTo(Akka.Util.AppVersion.Create(y))}");
    return;
}
if (args.Contains("--union"))
{
    using var s = ActorSystem.Create("u");
    OrderEvent u = new OrderPlaced("o-9", 5m, "USD");
    var ser = s.Serialization.FindSerializerFor(u);
    var bytes = ser.ToBinary(u);
    Console.WriteLine($"union boxed type={u.GetType().Name} serializer={ser.GetType().Name} json={System.Text.Encoding.UTF8.GetString(bytes)}");
    try { var back = s.Serialization.Deserialize(bytes, ser.Identifier, typeof(OrderEvent)); Console.WriteLine($"roundtrip -> {back.GetType().Name}: Value={((OrderEvent)back).Value ?? "null"}"); }
    catch (Exception ex) { Console.WriteLine($"roundtrip failed: {ex.GetType().Name}: {ex.Message}"); }
    return;
}
var asm = Assembly.GetEntryAssembly()!;
var info = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;
Console.WriteLine($"AssemblyVersion={asm.GetName().Version} InformationalVersion={info}");

var dbFile = Path.Combine(Path.GetTempPath(), $"ops-verify-{Guid.NewGuid():N}.db");
var conn = $"Data Source={dbFile}";
var explicitAppVersion = args.Contains("--explicit-appversion") ? info.Split('+')[0] : null;

var builder = Host.CreateApplicationBuilder();
builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["FeatureManagement:NewPricing"] = "true" });
builder.Services.AddFeatureManagement();
builder.Services.AddAkka("ops", (b, sp) =>
{
    b.WithRemoting("localhost", 0)
     .WithClustering(new ClusterOptions { AppVersion = explicitAppVersion })
     .WithCustomSerializer("order-events", [typeof(OrderPlaced), typeof(OrderCancelled)],
                           sys => new OrderEventSerializer(sys))
     .WithSqlPersistence(conn, ProviderName.SQLiteMS,
        journalBuilder: j => j.AddReadEventAdapter<OrderPlacedUpcaster>("upcast-order-placed", [typeof(OrderPlacedV1)]));
});
using var host = builder.Build();
await host.StartAsync();
var sys = host.Services.GetRequiredService<ActorSystem>();
var cluster = Cluster.Get(sys);
Console.WriteLine($"Cluster AppVersion={cluster.Settings.AppVersion}");
var fm = host.Services.GetRequiredService<IFeatureManager>();
Console.WriteLine($"Feature NewPricing={await fm.IsEnabledAsync("NewPricing")}");

var a = sys.ActorOf(Props.Create(() => new OrderActor("order-1")));
await a.Ask<string>(new OrderPlacedV1("order-1", 10m), TimeSpan.FromSeconds(10)); // persists legacy type (json)
await a.Ask<string>(new OrderPlaced("order-1", 20m, "EUR"), TimeSpan.FromSeconds(10));
await a.Ask<string>(new OrderCancelled("order-1", null), TimeSpan.FromSeconds(10));
await a.GracefulStop(TimeSpan.FromSeconds(5));
// recover in a fresh incarnation and show what the actor sees
var b2 = sys.ActorOf(Props.Create(() => new OrderActor("order-1")));
Console.WriteLine(await b2.Ask<string>("dump", TimeSpan.FromSeconds(10)));
await host.StopAsync();

using var db = new SqliteConnection(conn);
db.Open();
using var cmd = db.CreateCommand();
cmd.CommandText = "select sequence_number, identifier, manifest, length(message), substr(cast(message as text),1,90) from journal order by ordering";
using var r = cmd.ExecuteReader();
while (r.Read()) Console.WriteLine($"row seq={r.GetInt64(0)} serializerId={r.GetValue(1)} manifest='{r.GetValue(2)}' bytes={r.GetInt64(3)} payload={r.GetValue(4)}");

public sealed class OrderActor(string id) : UntypedPersistentActor
{
    public override string PersistenceId => $"order-{id}";
    private readonly List<string> _seen = [];
    protected override void OnCommand(object message)
    {
        switch (message)
        {
            case "dump": Sender.Tell(string.Join("; ", _seen)); break;
            case var evt: Persist(evt, e => { Track(e); Sender.Tell("ok"); }); break;
        }
    }
    protected override void OnRecover(object message)
    {
        if (message is RecoveryCompleted) return;
        Track(message);
    }
    private void Track(object e) => _seen.Add(e switch
    {
        OrderPlaced p => OrderState.Describe(p),        // implicit conversion case -> union
        OrderCancelled c => OrderState.Describe(c),
        _ => $"raw {e.GetType().Name}",
    });
}
// c
