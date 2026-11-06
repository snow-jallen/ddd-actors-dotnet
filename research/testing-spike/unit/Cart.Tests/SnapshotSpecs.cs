namespace Cart.Tests;

public sealed class EventSnapshotSpecs
{
    [Fact]
    public Task Event_stream_shape_is_stable()
    {
        var events = CartScenario.Given("c1")
            .When(new AddItem("c1", new Sku("pear"), new Quantity(3)))
            .Then()
            .Select(e => e.Value);   // snapshot the case records, not the union wrapper
        return Verify(events);
    }
}
