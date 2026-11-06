using Akka.Actor;
using Akka.Event;
using Akka.Pattern;

namespace Shop.Actors;

// ReceiveActor + timers + stash: a read-through cache that stashes while loading.
public sealed class PriceCacheActor : ReceiveActor, IWithTimers, IWithStash
{
    public sealed record GetPrice(string Sku);
    public sealed record Price(string Sku, decimal Amount);
    private sealed record Loaded(IReadOnlyDictionary<string, decimal> Prices);
    private sealed record Refresh
    {
        public static readonly Refresh Instance = new();
    }

    private IReadOnlyDictionary<string, decimal> _prices = new Dictionary<string, decimal>();
    public ITimerScheduler Timers { get; set; } = null!;
    public IStash Stash { get; set; } = null!;

    public PriceCacheActor()
    {
        Loading();
    }

    protected override void PreStart()
    {
        Timers.StartPeriodicTimer("refresh", Refresh.Instance, TimeSpan.FromMinutes(5));
        Self.Tell(Refresh.Instance);
    }

    private void Loading()
    {
        Receive<Refresh>(_ => LoadAsync().PipeTo(Self));
        Receive<Loaded>(l => { _prices = l.Prices; Stash.UnstashAll(); Become(Ready); });
        ReceiveAny(_ => Stash.Stash());
    }

    private void Ready()
    {
        Receive<GetPrice>(q => Sender.Tell(new Price(q.Sku, _prices.GetValueOrDefault(q.Sku))));
        Receive<Refresh>(_ => { Become(Loading); Self.Tell(Refresh.Instance); });
    }

    private static async Task<Loaded> LoadAsync()
    {
        await Task.Delay(10);
        return new Loaded(new Dictionary<string, decimal> { ["sku-1"] = 9.99m });
    }

    protected override SupervisorStrategy SupervisorStrategy() =>
        new OneForOneStrategy(maxNrOfRetries: 3, withinTimeRange: TimeSpan.FromMinutes(1), localOnlyDecider: ex => ex switch
        {
            ArgumentException => Directive.Resume,
            TimeoutException => Directive.Restart,
            _ => Directive.Escalate
        });
}

public static class BackoffExample
{
    public static Props Wrap(Props child) =>
        BackoffSupervisor.Props(
            Backoff.OnFailure(child, "price-cache",
                    minBackoff: TimeSpan.FromSeconds(1),
                    maxBackoff: TimeSpan.FromSeconds(30),
                    randomFactor: 0.2,
                    maxNrOfRetries: -1)
                .WithAutoReset(TimeSpan.FromSeconds(10)));
}
