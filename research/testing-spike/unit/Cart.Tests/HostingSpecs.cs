using Akka.Actor;
using Akka.Cluster;
using Akka.Cluster.Hosting;
using Akka.Cluster.Sharding;
using Akka.Hosting;
using Akka.Remote.Hosting;
using Shouldly;
using HostingPersistenceTestKit = Akka.Hosting.TestKit.PersistenceTestKit;

namespace Cart.Tests;

public sealed class CartRegionKey;

/// Akka.Hosting.TestKit: real IHost + ActorRegistry; here a single-node cluster running sharding locally.
public sealed class ShardedCartSpecs(ITestOutputHelper output) : HostingPersistenceTestKit(output: output)
{
    protected override void ConfigureAkka(AkkaConfigurationBuilder builder, IServiceProvider provider)
    {
        base.ConfigureAkka(builder, provider);     // wires TestJournal + TestSnapshotStore
        builder
            .WithRemoting("127.0.0.1", 0)
            .WithClustering()
            .WithShardRegion<CartRegionKey>("cart",
                entityId => Props.Create(() => new CartActor(entityId)),
                new CartMessageExtractor(),
                new ShardOptions { StateStoreMode = StateStoreMode.DData, RememberEntities = false })
            .AddStartup((system, _) =>
            {
                var cluster = Cluster.Get(system);
                cluster.Join(cluster.SelfAddress);   // single-node cluster for tests
            });
    }

    [Fact]
    public async Task Messages_route_through_the_shard_region()
    {
        var ct = TestContext.Current.CancellationToken;
        await AwaitAssertAsync(() => Cluster.Get(Sys).SelfMember.Status.ShouldBe(MemberStatus.Up),
            TimeSpan.FromSeconds(10), cancellationToken: ct);

        var region = await ActorRegistry.GetAsync<CartRegionKey>(ct);
        region.Tell(new AddItem("s1", new Sku("apple"), new Quantity(2)), TestActor);
        var state = await ExpectMsgAsync<CartState>(TimeSpan.FromSeconds(10), cancellationToken: ct);
        state.CartId.ShouldBe("s1");
    }
}

public sealed class CartMessageExtractor() : HashCodeMessageExtractor(maxNumberOfShards: 10)
{
    public override string? EntityId(object message) => message switch
    {
        AddItem m => m.CartId,
        CheckOut m => m.CartId,
        GetCart m => m.CartId,
        _ => null
    };
}
