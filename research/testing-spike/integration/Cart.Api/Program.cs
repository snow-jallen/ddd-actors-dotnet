using Akka.Actor;
using Akka.Hosting;
using Akka.Persistence.Sql.Hosting;
using Cart.Api;
using LinqToDB;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAkka("cart-system", (akka, sp) =>
{
    var cs = builder.Configuration.GetConnectionString("carts")
             ?? throw new InvalidOperationException("ConnectionStrings:carts missing");
    akka.WithSqlPersistence(
            connectionString: cs,
            providerName: ProviderName.PostgreSQL15,
            autoInitialize: true)
        .WithActors((system, registry, resolver) =>
            registry.Register<CartManager>(system.ActorOf(Props.Create(() => new CartManager()), "carts")));
});

var app = builder.Build();
{
    app.MapPost("/carts/{id}/items", async (string id, AddItemRequest body, IRequiredActor<CartManager> carts, CancellationToken ct) =>
    {
        var manager = await carts.GetAsync(ct);
        var reply = await manager.Ask<object>(new AddItem(id, new Sku(body.Sku), new Quantity(body.Qty)), TimeSpan.FromSeconds(5), ct);
        return reply switch
        {
            CartState s => Results.Ok(new { s.CartId, Lines = s.Lines.ToDictionary(kv => kv.Key.Value, kv => kv.Value.Value) }),
            Rejected r => Results.Problem(r.Reason, statusCode: 409),
            _ => Results.StatusCode(500)
        };
    });
    app.MapGet("/carts/{id}", async (string id, IRequiredActor<CartManager> carts, CancellationToken ct) =>
    {
        var s = await (await carts.GetAsync(ct)).Ask<CartState>(new GetCart(id), TimeSpan.FromSeconds(5), ct);
        return Results.Ok(new { s.CartId, Lines = s.Lines.ToDictionary(kv => kv.Key.Value, kv => kv.Value.Value) });
    });
    app.Run();
}

public sealed record AddItemRequest(string Sku, int Qty);

/// Child-per-aggregate parent (stand-in for sharding in this sample).
public sealed class CartManager : ReceiveActor
{
    public CartManager()
    {
        Receive<AddItem>(m => Child(m.CartId).Forward(m));
        Receive<CheckOut>(m => Child(m.CartId).Forward(m));
        Receive<GetCart>(m => Child(m.CartId).Forward(m));
    }
    private IActorRef Child(string id) =>
        Context.Child(id) is var c && !c.IsNobody() ? c : Context.ActorOf(Props.Create(() => new CartActor(id)), id);
}

public partial class Program;
