using CsCheck;
using Gen = CsCheck.Gen;
using FsCheck;

using FsCheck.Xunit;
using Shouldly;

namespace Cart.Tests;

public sealed class QuantityProperties
{
    // FsCheck 3 + xunit v3 integration: [Property] attribute from FsCheck.Xunit.v3
    [Property]
    public bool Quantity_addition_never_exceeds_99(PositiveInt a, PositiveInt b)
    {
        var x = (a.Get - 1) % 99 + 1;
        var y = (b.Get - 1) % 99 + 1;
        return (new Quantity(x) + new Quantity(y)).Value <= 99;
    }

    // CsCheck: generator-first, shrinks automatically, no attribute needed
    [Fact]
    public void Cart_never_exceeds_max_lines_whatever_the_command_sequence()
    {
        var genSku = Gen.Char['a', 'z'].Array[1, 6].Select(cs => new Sku(new string(cs)));
        var genCmd = Gen.OneOf(
            Gen.Select(genSku, Gen.Int[1, 5], (s, q) => new CartCommand(new AddItem("c", s, new Quantity(q)))),
            Gen.Const(new CartCommand(new CheckOut("c"))));

        genCmd.Array[0, 60].Sample(cmds =>
        {
            var state = CartState.Empty("c");
            foreach (var cmd in cmds)
            {
                try { foreach (var e in Cart.Decide(state, cmd)) state = Cart.Evolve(state, e); }
                catch (CartRuleViolation) { /* rejected commands leave state unchanged */ }
            }
            state.Lines.Count.ShouldBeLessThanOrEqualTo(Cart.MaxLines);
        });
    }
}
