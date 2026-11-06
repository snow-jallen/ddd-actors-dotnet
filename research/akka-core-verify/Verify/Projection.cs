using Akka;
using Akka.Actor;
using Akka.Persistence.Query;
using Akka.Persistence.Sql.Query;
using Akka.Streams;
using Akka.Streams.Dsl;
using Shop.Domain;

namespace Shop.Projections;

public interface IOffsetStore
{
    Task<long> LoadAsync(string projection);
    Task SaveAsync(string projection, long offset);
}

public sealed class InMemoryOffsetStore : IOffsetStore
{
    private readonly Dictionary<string, long> _d = new();
    public Task<long> LoadAsync(string p) => Task.FromResult(_d.GetValueOrDefault(p));
    public Task SaveAsync(string p, long o) { _d[p] = o; return Task.CompletedTask; }
}

public static class OrderTotalsProjection
{
    public const string Name = "order-totals";

    /// Live, resumable, restart-with-backoff projection over the "order" tag.
    public static (UniqueKillSwitch, Task) Run(ActorSystem system, IOffsetStore offsets, Action<IOrderEvent> apply)
    {
        var readJournal = PersistenceQuery.Get(system).ReadJournalFor<SqlReadJournal>(SqlReadJournal.Identifier);

        var restarting = RestartSource.OnFailuresWithBackoff(
            () => Source.FromTask(offsets.LoadAsync(Name))
                .ConcatMany(last => readJournal.EventsByTag("order", Offset.Sequence(last)))  // offset is exclusive in Akka.Persistence.Sql
                .SelectAsync(1, async env =>
                {
                    if (env.Event is IOrderEvent e) apply(e);           // idempotent upsert in real code
                    await offsets.SaveAsync(Name, ((Sequence)env.Offset).Value);
                    return NotUsed.Instance;
                }),
            RestartSettings.Create(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30), 0.2));

        var (killSwitch, done) = restarting
            .ViaMaterialized(KillSwitches.Single<NotUsed>(), Keep.Right)
            .ToMaterialized(Sink.Ignore<NotUsed>(), Keep.Both)
            .Run(system.Materializer());
        return (killSwitch, done);
    }
}
