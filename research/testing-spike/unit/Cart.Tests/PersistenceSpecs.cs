using Akka.Actor;
using Akka.Persistence.TestKit;
using Shouldly;

namespace Cart.Tests;

/// Akka.Persistence.TestKit.Xunit: TestJournal / TestSnapshotStore with failure injection.
public sealed class CartPersistenceSpecs(ITestOutputHelper output) : PersistenceTestKit(output: output)
{
    private CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Journal_write_failure_stops_the_actor()
    {
        await WithJournalWrite(write => write.Fail(), async () =>
        {
            var cart = ActorOf(() => new CartActor("f1"));
            Watch(cart);
            cart.Tell(new AddItem("f1", new Sku("apple"), new Quantity(1)));
            await ExpectTerminatedAsync(cart, cancellationToken: Ct);
        });
    }

    [Fact]
    public async Task Journal_rejection_keeps_actor_alive_and_state_unchanged()
    {
        var cart = ActorOf(() => new CartActor("f2"));
        await WithJournalWrite(write => write.RejectOnType<ItemAdded>(), async () =>
        {
            await EventFilter.Error(contains: "Rejected to persist").ExpectOneAsync(() =>
            {
                cart.Tell(new AddItem("f2", new Sku("apple"), new Quantity(1)), TestActor);
                return Task.CompletedTask;
            }, Ct);
        });
        cart.Tell(new GetCart("f2"), TestActor);
        // the DeferAsync reply from the rejected command arrives first, then our GetCart reply
        var replies = await ReceiveNAsync(2, TimeSpan.FromSeconds(3), Ct).ToListAsync(Ct);
        replies.OfType<CartState>().Last().Lines.ShouldBeEmpty();
    }

    [Fact]
    public async Task Recovery_failure_stops_the_actor()
    {
        var cart = ActorOf(() => new CartActor("f3"));
        cart.Tell(new AddItem("f3", new Sku("apple"), new Quantity(1)), TestActor);
        await ExpectMsgAsync<CartState>(cancellationToken: Ct);
        Sys.Stop(cart);

        await WithJournalRecovery(recovery => recovery.Fail(), async () =>
        {
            var revived = ActorOf(() => new CartActor("f3"));
            Watch(revived);
            await ExpectTerminatedAsync(revived, cancellationToken: Ct);
        });
    }
}
