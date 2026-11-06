using Akka.Actor;
using Akka.Configuration;
using Akka.TestKit;
using Shouldly;
using XunitTestKit = Akka.TestKit.Xunit.TestKit;

namespace Cart.Tests;

public sealed class CartActorSpecs(ITestOutputHelper output) : XunitTestKit(output: output)
{
    private CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Add_item_replies_with_new_state()
    {
        var cart = Sys.ActorOf(Props.Create(() => new CartActor("c1")));
        cart.Tell(new AddItem("c1", new Sku("apple"), new Quantity(2)), TestActor);

        var state = await ExpectMsgAsync<CartState>(cancellationToken: Ct);
        state.Lines[new Sku("APPLE")].Value.ShouldBe(2);
    }

    [Fact]
    public async Task Business_rule_violation_is_a_reply_not_a_crash_and_is_logged()
    {
        var cart = Sys.ActorOf(Props.Create(() => new CartActor("c2")));
        var probe = CreateTestProbe();

        await EventFilter.Warning(contains: "Cannot check out an empty cart")
            .ExpectOneAsync(async () =>
            {
                cart.Tell(new CheckOut("c2"), probe.Ref);
                (await probe.ExpectMsgAsync<Rejected>(cancellationToken: Ct)).Reason.ShouldContain("empty");
            }, Ct);
    }

    [Fact]
    public async Task State_survives_restart_via_recovery_and_AwaitAssert()
    {
        var cart = Sys.ActorOf(Props.Create(() => new CartActor("c3")), "cart-c3");
        cart.Tell(new AddItem("c3", new Sku("kiwi"), new Quantity(4)), TestActor);
        await ExpectMsgAsync<CartState>(cancellationToken: Ct);

        Watch(cart);
        cart.Tell(PoisonPill.Instance);
        await ExpectTerminatedAsync(cart, cancellationToken: Ct);

        // Name may not be free immediately; AwaitAssert retries until it is.
        IActorRef revived = ActorRefs.Nobody;
        await AwaitAssertAsync(() => revived = Sys.ActorOf(Props.Create(() => new CartActor("c3")), "cart-c3"),
            TimeSpan.FromSeconds(3), TimeSpan.FromMilliseconds(50), Ct);

        revived.Tell(new GetCart("c3"), TestActor);
        (await ExpectMsgAsync<CartState>(cancellationToken: Ct)).Lines[new Sku("KIWI")].Value.ShouldBe(4);
    }
}

/// Virtual time: swap in TestScheduler so timers fire only when the test advances the clock.
public sealed class ReservationSpecs(ITestOutputHelper output)
    : XunitTestKit(ConfigurationFactory.ParseString(
        """akka.scheduler.implementation = "Akka.TestKit.TestScheduler, Akka.TestKit" """), output: output)
{
    private TestScheduler Scheduler => (TestScheduler)Sys.Scheduler;

    [Fact]
    public async Task Reservation_expires_after_15_virtual_minutes()
    {
        var ct = TestContext.Current.CancellationToken;
        var probe = CreateTestProbe();
        var actor = Sys.ActorOf(Props.Create(() => new ReservationActor(probe.Ref)));

        actor.Tell(new StartReservation("APPLE"));
        // make sure the actor has processed the start message before advancing time
        await actor.Ask<ActorIdentity>(new Identify(1), TimeSpan.FromSeconds(3), ct);

        Scheduler.Advance(TimeSpan.FromMinutes(14));
        await probe.ExpectNoMsgAsync(TimeSpan.FromMilliseconds(200), ct);

        Scheduler.Advance(TimeSpan.FromMinutes(1));
        (await probe.ExpectMsgAsync<ReservationExpired>(cancellationToken: ct)).Sku.ShouldBe("APPLE");
    }
}
