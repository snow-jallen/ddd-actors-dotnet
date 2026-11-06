using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using System.Net;
using System.Net.Http.Json;

namespace Cart.AspireTests;

public sealed class AppHostSpecs
{
    [Fact]
    public async Task Whole_app_boots_and_accepts_a_command()
    {
        var ct = TestContext.Current.CancellationToken;
        var timeout = TimeSpan.FromMinutes(2);

        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Cart_AppHost>(ct);
        await using var app = await appHost.BuildAsync(ct).WaitAsync(timeout, ct);
        await app.StartAsync(ct).WaitAsync(timeout, ct);

        await app.ResourceNotifications.WaitForResourceAsync("cart-api", KnownResourceStates.Running, ct).WaitAsync(timeout, ct);

        using var http = app.CreateHttpClient("cart-api");
        var resp = await http.PostAsJsonAsync("/carts/a1/items", new { sku = "apple", qty = 1 }, ct);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }
}
