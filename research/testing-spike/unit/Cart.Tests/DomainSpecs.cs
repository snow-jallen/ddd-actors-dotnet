using AwesomeAssertions;
using Shouldly;

namespace Cart.Tests;

/// Given-When-Then over the pure aggregate functions. No actors involved.
public sealed class CartScenario
{
    private CartState _state;
    private CartCommand _when;
    private CartScenario(string id) => _state = CartState.Empty(id);

    public static CartScenario Given(string cartId, params CartEvent[] history)
    {
        var s = new CartScenario(cartId);
        foreach (var e in history) s._state = Cart.Evolve(s._state, e);
        return s;
    }
    public CartScenario When(CartCommand command) { _when = command; return this; }
    public IReadOnlyList<CartEvent> Then() => Cart.Decide(_state, _when);
    public CartRuleViolation ThenRejected() => Should.Throw<CartRuleViolation>(() => Cart.Decide(_state, _when));
}

public sealed class CartDecisionSpecs
{
    private static readonly Sku Apple = new("apple");

    [Fact]
    public void Adding_an_item_to_an_open_cart_emits_ItemAdded()
    {
        var events = CartScenario.Given("c1")
            .When(new AddItem("c1", Apple, new Quantity(2)))
            .Then();

        // Union equality: struct equality delegates to the boxed record's Equals.
        events.ShouldBe([new CartEvent(new ItemAdded("c1", Apple, new Quantity(2)))]);
        events.Single().Value.Should().BeOfType<ItemAdded>();   // AwesomeAssertions
    }

    [Fact]
    public void Checking_out_an_empty_cart_is_rejected() =>
        CartScenario.Given("c1").When(new CheckOut("c1")).ThenRejected()
            .Message.ShouldContain("empty");

    [Fact]
    public void Checking_out_twice_is_idempotent() =>
        CartScenario.Given("c1",
                new ItemAdded("c1", Apple, new Quantity(1)),
                new CheckedOut("c1", 1))
            .When(new CheckOut("c1"))
            .Then()
            .ShouldBeEmpty();

    [Fact]
    public void Exhaustive_switch_over_union_needs_no_default()
    {
        CartEvent e = new CheckedOut("c1", 3);
        var label = e switch { ItemAdded => "added", CheckedOut => "checked-out" };
        label.ShouldBe("checked-out");
    }
}
