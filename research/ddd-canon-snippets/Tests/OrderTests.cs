using Akka.Actor;
using Akka.TestKit.Xunit;
using Microsoft.Extensions.Time.Testing;
using Sales.Domain;
using Sales.Shell;

namespace Sales.Tests;

// 1) Functional core: Given events / When command / Then events. No actors needed.
public class OrderDecideTests
{
    private static readonly OrderId Id = OrderId.New();
    private static readonly FakeTimeProvider Clock = new(new DateTimeOffset(2026, 11, 4, 12, 0, 0, TimeSpan.Zero));

    private static OrderState Given(params OrderEvent[] history) => history.Aggregate(Order.Initial, Order.Evolve);

    private static Money Usd(decimal a) => Money.Create(a, "usd") switch
    {
        Money m => m,
        DomainError.InvalidValue e => throw new InvalidOperationException(e.ToString()),
    };

    [Fact]
    public void Submitting_an_empty_order_violates_an_invariant()
    {
        var state = Given(new OrderPlaced(Id, "c-1", Clock.GetUtcNow()));
        var decision = Order.Decide(state, new SubmitOrder(Id), Clock);
        Assert.True(decision is OrderError { Value: DomainError.InvariantViolated });
    }

    [Fact]
    public void Submitting_a_draft_with_lines_emits_OrderSubmitted_with_total()
    {
        var line = new OrderLine(new Sku("ABC"), 2, Usd(5m));
        var state = Given(new OrderPlaced(Id, "c-1", Clock.GetUtcNow()), new LineAdded(Id, line));
        var decision = Order.Decide(state, new SubmitOrder(Id), Clock);
        var events = Assert.IsType<OrderEvent[]>(decision.Value);
        var submitted = Assert.IsType<OrderSubmitted>(Assert.Single(events).Value);
        Assert.Equal(10m, submitted.Total.Amount);
        Assert.Equal("USD", submitted.Total.Currency);
    }

    [Fact]
    public void Cancelling_twice_is_idempotent()
    {
        var state = Given(new OrderPlaced(Id, "c-1", Clock.GetUtcNow()), new OrderCancelled(Id, "x"));
        var decision = Order.Decide(state, new CancelOrder(Id, "again"), Clock);
        Assert.Empty(Assert.IsType<OrderEvent[]>(decision.Value));
    }
}

// 2) Actor shell: persistence + protocol, using TestKit with in-memory journal.
public class OrderActorTests : TestKit
{
    [Fact]
    public void Actor_persists_and_recovers_state()
    {
        var id = Guid.NewGuid().ToString("N");
        var oid = new OrderId(Guid.Parse(id));
        var actor = Sys.ActorOf(OrderActor.PropsFor(id, TimeProvider.System));

        actor.Tell(new OrderEnvelope(id, new PlaceOrder(oid, "c-1")));
        ExpectMsg<CommandAccepted>(a => a.EventCount == 1);

        // Wrong-state command is rejected with a typed error
        actor.Tell(new OrderEnvelope(id, new PlaceOrder(oid, "c-1")));
        var rejected = ExpectMsg<CommandRejected>();
        Assert.True(rejected.Error is { Value: DomainError.WrongState });

        // Kill and recreate: state comes back from the journal
        Watch(actor); actor.Tell(PoisonPill.Instance); ExpectTerminated(actor);
        var revived = Sys.ActorOf(OrderActor.PropsFor(id, TimeProvider.System));
        revived.Tell(new GetOrderState(id));
        var state = ExpectMsg<OrderState>();
        Assert.True(state is { Value: DraftOrder });
    }

    [Fact]
    public void Sending_a_union_boxes_the_wrapper_not_the_case()
    {
        OrderCommand cmd = new SubmitOrder(OrderId.New());
        TestActor.Tell(cmd);
        var msg = ExpectMsg<object>();
        Assert.IsType<OrderCommand>(msg);      // the struct wrapper arrives, boxed
        Assert.IsNotType<SubmitOrder>(msg);    // so Receive<SubmitOrder> would NOT match
    }
}
