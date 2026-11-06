using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;

namespace Cart.IntegrationTests;

public sealed class CartApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.UseSetting("ConnectionStrings:carts", connectionString);
}

public sealed class CartApiSpecs(PostgresFixture pg)
{
    private CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Cart_state_survives_an_application_restart()
    {
        var id = Guid.NewGuid().ToString("N");

        await using (var first = new CartApiFactory(pg.Container.GetConnectionString()))
        {
            var client = first.CreateClient();
            var resp = await client.PostAsJsonAsync($"/carts/{id}/items", new { sku = "apple", qty = 2 }, Ct);
            resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        // brand-new host + ActorSystem; state must come back from PostgreSQL via recovery
        await using var second = new CartApiFactory(pg.Container.GetConnectionString());
        var cart = await second.CreateClient().GetFromJsonAsync<CartDto>($"/carts/{id}", Ct);
        cart!.Lines["APPLE"].ShouldBe(2);
    }

    [Fact]
    public async Task Business_rule_violation_maps_to_409()
    {
        await using var app = new CartApiFactory(pg.Container.GetConnectionString());
        var client = app.CreateClient();
        var id = Guid.NewGuid().ToString("N");
        for (var i = 0; i < 10; i++)
            (await client.PostAsJsonAsync($"/carts/{id}/items", new { sku = $"sku{i}", qty = 1 }, Ct)).EnsureSuccessStatusCode();

        var resp = await client.PostAsJsonAsync($"/carts/{id}/items", new { sku = "one-too-many", qty = 1 }, Ct);
        resp.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    private sealed record CartDto(string CartId, Dictionary<string, int> Lines);
}
