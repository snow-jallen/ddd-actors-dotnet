# Research notes: C# 15 / .NET 11 features for DDD on Akka.NET

Researched 2026-11-04. Every code block labelled **VERIFIED** was compiled and run on the local
toolchain listed below. Anything not compiled is marked **UNVERIFIED**. The scratch projects
are in
`/tmp/claude-1000/-home-jonathan-code-scienceNight/45f474fc-e89c-490e-8b5f-ec3ffd186198/scratchpad/cs15-research/`
(`u1` to `u9`, plus `closedlib`).

---

## 0. TL;DR for authors

1. **Toolchain.** The local SDK is `11.0.100-rc.3.26522.148`, with runtime `11.0.0-rc.3.26522.148` and
   Roslyn `5.12.0-1.26522.148`. The default `LangVersion` for `net11.0` is **15.0** (checked with
   `#error version`). Unions, closed hierarchies, extension indexers, collection-expression
   `with(...)` and labeled `break`/`continue` need **no preview flag**. Only the "memory safety"
   feature needs `LangVersion=preview`, and the book doesn't use it.
2. **C# 15 has two "closed set" features, and the book should use both:**
   - `union` declares a **struct** that holds one of an arbitrary list of *existing* types (`Value` is
     `object?`). It's best for **command results, replies and error sets**, where the cases are
     unrelated types or reused types.
   - The `closed` modifier on a class or record makes a **closed hierarchy**: derived types are
     only allowed in the same assembly, and switches over it are exhaustive. It's best for
     **actor message protocols and persisted domain events.** Every case *is-a* base type, so
     Akka's `Tell(object)` and `Receive<TBase>` work with no casting. The default Akka
     (Newtonsoft) serializer round-trips it, and System.Text.Json gives it a `$type`
     discriminator.
3. **Gotcha (verified): a union is not its case.** `Tell(new PlaceOrder(...))` sends a
   `PlaceOrder`, not an `OrderCommand` union, because `Tell` takes `object` and no union
   conversion happens. A `Receive<OrderCommand>` handler then **doesn't match**. Callers must
   cast with `(OrderCommand)` or go through a typed helper. Also, a boxed union `is OrderEvent`
   but `is not OrderPlaced`.
4. **Gotcha (verified): Akka's default serializer cannot rehydrate a union.** The
   Newtonsoft round-trip gives an `OrderCommand` whose `Value` is **null**. Never put a
   compiler-generated union on the wire or in a journal without a custom serializer.
5. **Gotcha (verified): `default(MyUnion)` has `Value == null`.** A switch with no `null` arm then
   throws `SwitchExpressionException`, and the compiler does **not** warn when every case type
   is non-nullable. Add a `null =>` arm whenever a default can reach the switch, for example in
   actor state fields or arrays.
6. **System.Text.Json (.NET 11) serializes unions as the bare case value, with no discriminator.**
   Object-shaped cases are ambiguous on read (`JsonException`) unless you add
   `[JsonUnion(TypeClassifier = typeof(JsonUnionTypeStructuralClassifier))]`. The source
   generator warns with **SYSLIB1227**. For events, use a `closed record` with
   `[JsonPolymorphic(InferClosedTypePolymorphism = true)]` or explicit `[JsonDerivedType]`
   discriminators. Nested closed levels work in RC3.
7. **Treat `CS8509` and `CS8655` as errors** in `Directory.Build.props`. Adding a new event or
   command then breaks the build at every switch that doesn't handle it. That's the payoff of
   these features for DDD.
8. Value objects: `readonly record struct` plus a C# 14 **`field`-backed `init` validator**
   puts both the constructor and `with` through the guard. `default(T)` still bypasses it,
   which is a struct limitation.
9. Package versions on NuGet as of 2026-11-04: Akka **1.5.73** stable (1.6.0-beta5 prerelease);
   Microsoft.Extensions.* **11.0.0-rc.3.26522.148** (stable 10.0.14);
   Microsoft.Extensions.TimeProvider.Testing **10.11.0**;
   Microsoft.Extensions.Diagnostics.Testing **10.11.0**; OpenTelemetry **1.20.1** stable
   (1.21.0-rc.1); Aspire.Hosting.AppHost **13.6.4**.

---

## 1. Toolchain facts (verified)

```bash
source /home/jonathan/code/ddd-actors-dotnet/tools/env.sh
dotnet --version            # 11.0.100-rc.3.26522.148
```

`dotnet new console` produces:

```xml
<TargetFramework>net11.0</TargetFramework>
<ImplicitUsings>enable</ImplicitUsings>
<Nullable>enable</Nullable>
```

`#error version` printed:
`Compiler version: '5.12.0-1.26522.148 (5254d8bc...)'. Language version: 15.0.`

The C# 15 features, per the official "What's new in C# 15" page (updated 2026-10-23):
collection expression arguments, union types, closed hierarchies, extension indexers, labeled
`break`/`continue`, non-virtual static interface members on older runtimes, and memory safety
(preview only). The page says: "Except for memory safety, these features don't require
`<LangVersion>preview</LangVersion>`."

Package versions, from `https://api.nuget.org/v3-flatcontainer/<id>/index.json` on 2026-11-04:

| Package | Latest | Latest stable |
|---|---|---|
| Akka / Akka.Hosting | 1.6.0-beta5 | **1.5.73** |
| System.Text.Json | 11.0.0-rc.3.26522.148 | 10.0.14 (inbox in net11.0 anyway) |
| Microsoft.Extensions.Hosting / .Diagnostics | 11.0.0-rc.3.26522.148 | 10.0.14 |
| Microsoft.Extensions.TimeProvider.Testing | 10.11.0 | 10.11.0 |
| Microsoft.Extensions.Diagnostics.Testing | 10.11.0 | 10.11.0 |
| OpenTelemetry | 1.21.0-rc.1 | 1.20.1 |
| Aspire.Hosting.AppHost | 13.6.4 | 13.6.4 |

Recommendation: target `net11.0` and use the `11.0.0-rc.3.*` Microsoft.Extensions packages to
match the SDK (they are "go-live"). Plan an errata pass once 11.0.0 GA ships, which is
expected in Nov 2026.

---

## 2. Union types (C# 15)

### 2.1 Declaration syntax (as shipped in RC3)

The grammar is quoted from the feature spec:

```antlr
union_declaration
    : attributes? struct_modifier* 'partial'? 'union' identifier type_parameter_list?
      '(' case_types ')'  struct_interfaces? type_parameter_constraints_clause*
      (`{` struct_member_declaration* `}` | ';')
    ;
case_types : type (',' type)* ;
```

There is **no `case` keyword and no inline case declaration** such as `union Shape { case Circle(double R); }`.
The cases are existing types that you declare separately, usually `sealed record`s. There's no
`record union` either. Tested: `public record union R(A, B);` is a syntax error (CS1513/CS1514).
The case types don't need to be related, and they can be classes, structs, interfaces, type
parameters, nullable types, or other unions.

I compiled each of these declaration forms. All of them build cleanly (**VERIFIED**):

```csharp
public partial union R(A, B);  public partial union R { public int X => 1; }   // partial OK
public readonly union R(A, B);                                                // readonly modifier OK
public interface IMsg { }  public union R(A, B) : IMsg;                       // implements interfaces OK
public union R(A, B) : IEquatable<R> { public bool Equals(R o) => Equals(Value, o.Value); }
public interface IA; public interface IB; public union R(IA, IB);            // interface cases OK
public union R<T>(A, T) where T : notnull;                                   // generic + constraints OK
public union Inner(A, B);  public union Outer(Inner, C);                     // union as a case OK
public union R(A, B) { public R(A a, int n) : this(a) { } }                  // extra ctor delegating via this(...) OK
public union R(A, B) { public static R Make() => new A();
                       public override string ToString() => Value?.ToString() ?? "<empty>"; }
```

These forms are rejected (**VERIFIED**):

| Code | Result |
|---|---|
| `union R(A,B) { private readonly int _x; }` or `{ public int P { get; set; } }` | `CS9373: Instance fields, auto-properties or field-like events are not permitted in a 'union' declaration.` |
| `union Dup(string, string)` | `CS0111` duplicate constructor |
| `Either<int,int> e = 1;` (generic union where the type args collide) | `CS0457: Ambiguous user defined conversions` |

### 2.2 How it lowers (verified with reflection)

The spec says `public union Pet(Cat, Dog){...}` becomes:

```csharp
[Union] public struct Pet : IUnion
{
    public Pet(Cat value) => Value = value;
    public Pet(Dog value) => Value = value;
    public object? Value { get; }
    ... // original body
}
```

Reflection on `union OrderEvent(OrderPlaced, OrderLineAdded, OrderShipped, OrderCancelled)` printed:

```
is struct: True
implements IUnion: True
has [Union]: True
ctors: OrderPlaced, OrderLineAdded, OrderShipped, OrderCancelled
members: get_Value, Equals, GetHashCode, ToString, GetType, .ctor, Value
```

`System.Runtime.CompilerServices.UnionAttribute` and `IUnion` ship in the .NET 11 runtime.
Consequences that matter for the book:

- It's **always a struct** holding one `object?`, so value-type cases are **boxed**. A
  hand-written "custom union" can avoid that (section 2.8).
- **Equality**: no record-style equality is generated. `ValueType.Equals` still compares the
  single field, so two unions that wrap record-equal values are `Equals`. Verified: `True`.
- **`ToString()` prints only the type name.** Verified: `OrderEvent`. Override it in the body if
  you log unions, which you will in actors.

### 2.3 Conversions

There is an implicit conversion from each case type. It calls the generated constructor. A
user-defined implicit operator takes priority over it.

**Conversions are not transitive (VERIFIED).** Take
`union PlaceOrderReply(OrderAccepted, PlaceOrderError)`, where
`PlaceOrderError` is `union(OutOfStock, CustomerBlocked, InvalidQuantity)`. Then:

```csharp
PlaceOrderReply r = new CustomerBlocked("x");           // CS0029: Cannot implicitly convert
PlaceOrderReply r = (PlaceOrderError)new CustomerBlocked("x"); // OK: one explicit hop
```

### 2.4 Pattern matching rules

From the language reference, verified where noted:

| Pattern on a union value | Tests |
|---|---|
| `Dog`, `Dog d` | the **contents** (`Value`) |
| `Dog { Breed: "Lab" }` | contents, then nested |
| `{ Name: "Rex" }` (untyped) | the **union's own** members |
| `var x`, `_` | the union instance itself, without unwrapping |
| `null` | contents are null (or the union reference/nullable is null) |
| `pet is Pet` | **CS8121 compile error**, because no case is a `Pet` |

Patterns don't reach into a nested union either. `reply is OutOfStock` on `PlaceOrderReply`
gives `CS8121` (**VERIFIED**). Write `reply is PlaceOrderError and OutOfStock` instead, which works.

### 2.5 Exhaustiveness and nullability (verified diagnostics)

```csharp
public sealed record OrderPlaced(string Id);
public sealed record OrderShipped(string Id);
public sealed record OrderCancelled(string Id, string Reason);
public union OrderEvent(OrderPlaced, OrderShipped, OrderCancelled);

public sealed record NullableCaseA(string X);
public union MaybeNull(NullableCaseA?, OrderPlaced);

public static string Missing(OrderEvent e) => e switch
{
    OrderPlaced p => "placed",
    OrderShipped s => "shipped",
};   // warning CS8509: ... For example, the pattern 'OrderCancelled' is not covered.

public static string NoNullArm(MaybeNull m) => m switch
{
    NullableCaseA a => a.X,
    OrderPlaced p => p.Id,
};   // warning CS8655: ... the pattern 'null' is not covered.

public static void Stmt(OrderEvent e)
{
    switch (e) { case OrderPlaced: break; case OrderShipped: break; } // NO warning (statements are never exhaustiveness-checked)
}
```

- Only switch **expressions** get exhaustiveness warnings. In the book, prefer `x switch { ... }`
  over `switch (x) { case ... }` for protocol handling.
- **The `default` trap (VERIFIED).** `OrderEvent dflt = default;` passed to a switch with no
  `null` arm throws `SwitchExpressionException` at runtime with **no compile-time warning**,
  because all case types are non-nullable. `dflt is null` is `true`. Uninitialized actor
  fields and array slots are exactly how a default union gets in. Advice: add `null => ...`
  (throw or ignore) rather than `_ =>`. A discard silences the CS8509 you *want* when a new
  case is added.
- When `when` guards are involved, add a final unguarded arm per case or an
  `A or B or C =>` catch-all. Guards don't count toward exhaustiveness.

To make the build fail (**VERIFIED**: produces `error CS8509` and `error CS8655`):

```xml
<!-- Directory.Build.props -->
<Project>
  <PropertyGroup>
    <WarningsAsErrors>$(WarningsAsErrors);CS8509;CS8655;nullable</WarningsAsErrors>
  </PropertyGroup>
</Project>
```

### 2.6 Domain events as a union protocol (VERIFIED, `u1`)

```csharp
public readonly record struct OrderId(Guid Value)
{
    public static OrderId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString("N")[..8];
}

public sealed record OrderPlaced(OrderId Id, string Customer, decimal Total);
public sealed record OrderLineAdded(OrderId Id, string Sku, int Qty);
public sealed record OrderShipped(OrderId Id, DateTimeOffset At);
public sealed record OrderCancelled(OrderId Id, string Reason);

public union OrderEvent(OrderPlaced, OrderLineAdded, OrderShipped, OrderCancelled);

public static string Describe(OrderEvent e) => e switch
{
    OrderPlaced p => $"placed {p.Id} for {p.Customer} total {p.Total}",
    OrderLineAdded l => $"line {l.Sku} x{l.Qty}",
    OrderShipped s => $"shipped at {s.At:O}",
    OrderCancelled c => $"cancelled: {c.Reason}",
};

// collection expression + implicit union conversions
OrderEvent[] history =
[
    new OrderPlaced(id, "ada", 10m),
    new OrderLineAdded(id, "SKU-1", 2),
    new OrderShipped(id, DateTimeOffset.UnixEpoch),
];

object boxed = (OrderEvent)new OrderPlaced(id, "ada", 10m);
// boxed.GetType().Name == "OrderEvent"; boxed is OrderPlaced == false; boxed is OrderEvent == true
```

### 2.7 Generic unions and a union with a body (VERIFIED)

```csharp
public sealed record None { public static readonly None Instance = new(); }
public union Option<T>(None, T);

Option<int> some = 42;
Option<int> none = None.Instance;
some switch { int i => $"some {i}", None => "none" };   // "some 42"

public union Money(decimal, long)
{
    public decimal Amount => this switch
    {
        decimal d => d,
        long cents => cents / 100m,
        null => 0m,
    };
}
Money m = 1234L;   // m.Amount == 12.34
```

### 2.8 Custom (hand-written) unions (from docs, UNVERIFIED locally)

Any class or struct with `[System.Runtime.CompilerServices.Union]` is a union to the compiler
if it has one or more public single-parameter constructors (one per case) and a public
`object? Value { get; }`. On top of that, you can add:
- the **non-boxing access pattern**: `bool HasValue` plus one `bool TryGetValue(out TCase)` per case;
- a **union member provider**: a nested public `IUnionMembers` interface with static `Create(...)`
  factories, which allows a private constructor and factory validation.

Class-based unions are allowed: `[Union] public class Result<T> : IUnion { ... }`.
The docs' examples are long and the book probably only needs a sidebar on this. The only
real use case for us would be a non-boxing `Result` of value types, so it's low priority.

### 2.9 Other limitations

- There's no `[JsonPolymorphic]` support for unions, so you can't get a discriminator (section 5).
- Union structs are not serializable by Newtonsoft round-trip (section 4.3).
- The pattern `x is TheUnionType` is a compile error. Test `x.Value is not null` or `x is not null` instead.
- Union conversions don't apply when the target is `object`, which matters for Akka (section 4).

---

## 3. Closed hierarchies (C# 15)

`closed` is a contextual modifier on a class or record. Direct derived types are only
allowed **in the same assembly**, so the compiler knows all of them and switch expressions over
them are exhaustive. A closed class is **implicitly abstract**.

### 3.1 Verified behaviour (`closedlib` + `u4`)

```csharp
// ClosedLib assembly
public closed record OrderEvent(string OrderId);
public sealed record OrderPlaced(string OrderId, decimal Total) : OrderEvent(OrderId);
public sealed record OrderShipped(string OrderId) : OrderEvent(OrderId);
public closed record OrderClosed(string OrderId) : OrderEvent(OrderId);   // intermediate closed
public sealed record OrderCancelled(string OrderId, string Reason) : OrderClosed(OrderId);
public sealed record OrderCompleted(string OrderId) : OrderClosed(OrderId);
public record OpenLeaf(string OrderId) : OrderEvent(OrderId);              // non-sealed leaf
```

In a consuming assembly:

```csharp
public static string All(OrderEvent e) => e switch
{
    OrderPlaced p => "placed",
    OrderShipped => "shipped",
    OrderCancelled c => "cancelled",
    OrderCompleted => "completed",
    OpenLeaf => "open",
};  // no warning, no discard needed, works ACROSS assemblies

// omit the two OrderClosed leaves:
// warning CS8509: ... For example, the pattern 'ClosedLib.OrderCancelled' is not covered.
```

Diagnostics (all **VERIFIED**):

| Code | Diagnostic |
|---|---|
| `public sealed record Hack(string OrderId) : OrderEvent(OrderId);` in another assembly | `CS9382: 'Hack': cannot use a closed type 'OrderEvent' from another assembly as a base type.` |
| `public closed sealed record Bad1;` | `CS9381: a closed type cannot be sealed or static` |
| `public abstract closed record Bad2;` | `CS9384: a closed type cannot be marked abstract because it is always implicitly abstract.` |
| `public closed interface INope { }` | `CS0106: The modifier 'closed' is not valid for this item` |
| `new OrderEvent("x")` | `CS0144: Cannot create an instance of the abstract type` |
| A plain `abstract record` base with two sealed leaves | still `CS8509 ... pattern '_' is not covered` |

The last row is why `closed` matters. Before C# 15, a DDD event hierarchy always needed a `_ =>` arm.

Derivation is **not transitive**. `Hack2 : OpenLeaf` in another assembly compiles, and
it's still covered by the `OpenLeaf =>` arm. Recommend making every leaf `sealed` and every
intermediate level `closed`.

Generic closed hierarchies work (**VERIFIED**):

```csharp
public closed record E<T>(T X);
public sealed record E1<T>(T X) : E<T>(X);
public sealed record E2<T>(T X) : E<T>(X);
public static int F(E<int> e) => e switch { E1<int> => 1, E2<int> => 2 };   // no warning
```

Closed plain classes work too:
`public closed class Shape; public sealed class Sq : Shape; public sealed class Ci : Shape;`.

### 3.2 Union or closed hierarchy? A decision table for the book

| Concern | `union` | `closed record` hierarchy |
|---|---|---|
| Cases are pre-existing or unrelated types (`int`, `Exception`, another BC's types) | yes | no (cases must derive) |
| Shared data on every case (e.g. `OrderId`) | no (no instance fields) | yes (base positional params) |
| Akka `Tell(object)` / `Receive<TBase>` without casts | **no** (needs a cast) | **yes** |
| Akka default (Newtonsoft) serialization round-trip | **broken** (Value lost) | works |
| STJ with a discriminator | not supported | `InferClosedTypePolymorphism` / `[JsonDerivedType]` |
| Allocation | struct wrapper plus boxed case | a single reference |
| Exhaustive switch | yes | yes |
| Can be extended outside the assembly | never | only below non-closed, non-sealed leaves |

**Recommendation:**
- Use **closed records** for Commands, Events and Queries that cross the actor boundary,
  get persisted, or go over remoting/cluster sharding.
- Use **unions** for in-process algebraic return types: `Result<T,E>`, decision outputs
  (`Decision = union(Accepted, Rejected)`), error sets that combine errors from several
  modules, `Option<T>`, and pure domain functions (`Decide(state, cmd) -> union`).
  Before a union crosses an actor boundary, unwrap it, or use a closed hierarchy for the reply.

---

## 4. Unions and closed hierarchies inside Akka.NET 1.5.73 (VERIFIED, `u6`)

### 4.1 A union as an actor protocol

```csharp
using Akka.Actor;

public sealed record PlaceOrder(string OrderId, decimal Total);
public sealed record ShipOrder(string OrderId);
public sealed record CancelOrder(string OrderId, string Reason);
public union OrderCommand(PlaceOrder, ShipOrder, CancelOrder);

public sealed record Accepted(string OrderId);
public sealed record Rejected(string OrderId, string Reason);
public union CommandReply(Accepted, Rejected);

public sealed class OrderActor : ReceiveActor
{
    private string _state = "new";
    public OrderActor()
    {
        Receive<OrderCommand>(cmd => Sender.Tell(Handle(cmd)));
        ReceiveAny(m => Sender.Tell((CommandReply)new Rejected("?", $"unhandled {m.GetType().Name}")));
    }

    private CommandReply Handle(OrderCommand cmd) => cmd switch
    {
        PlaceOrder p when _state == "new" => Accept(p.OrderId, "placed"),
        ShipOrder s when _state == "placed" => Accept(s.OrderId, "shipped"),
        CancelOrder c when _state != "shipped" => Accept(c.OrderId, "cancelled"),
        PlaceOrder or ShipOrder or CancelOrder => new Rejected("?", $"invalid in state {_state}"),
        null => new Rejected("?", "empty command"),
    };

    private CommandReply Accept(string id, string next) { _state = next; return new Accepted(id); }
}

// C# 14 extension block gives a typed "Send" that forces the union conversion at the call site
public static class OrderActorRefExtensions
{
    extension(IActorRef actor)
    {
        public Task<CommandReply> Send(OrderCommand cmd, TimeSpan? timeout = null) =>
            actor.Ask<CommandReply>(cmd, timeout ?? TimeSpan.FromSeconds(3));
    }
}
```

Output:

```
order.Send(new PlaceOrder("o1", 10m))                       -> Accepted o1
order.Send(new PlaceOrder("o1", 10m))                       -> Rejected: invalid in state placed
order.Ask<CommandReply>(new ShipOrder("o1"))                -> Rejected: unhandled ShipOrder   <-- GOTCHA
order.Ask<CommandReply>((OrderCommand)new ShipOrder("o1"))  -> Accepted o1
```

`Ask<CommandReply>` works fine with a union **reply**. The reply is boxed as the union, and
`Ask<T>` casts back to it.

### 4.2 The same protocol as a closed hierarchy (preferred for actors)

```csharp
public closed record CartCommand(string CartId);
public sealed record AddItem(string CartId, string Sku, int Qty) : CartCommand(CartId);
public sealed record RemoveItem(string CartId, string Sku) : CartCommand(CartId);
public sealed record Checkout(string CartId) : CartCommand(CartId);

public sealed class CartActor : ReceiveActor
{
    private readonly Dictionary<string, int> _items = [];
    public CartActor() => Receive<CartCommand>(cmd => Sender.Tell(Handle(cmd)));

    private CommandReply Handle(CartCommand cmd) => cmd switch
    {
        AddItem { Qty: <= 0 } a => new Rejected(a.CartId, "qty must be positive"),
        AddItem a => Add(a),
        RemoveItem r when !_items.ContainsKey(r.Sku) => new Rejected(r.CartId, $"no {r.Sku}"),
        RemoveItem r => Remove(r),
        Checkout c when _items.Count == 0 => new Rejected(c.CartId, "empty cart"),
        Checkout c => new Accepted(c.CartId),
    };   // exhaustive, no discard, no null arm needed (reference type, non-null)
    private CommandReply Add(AddItem a) { _items[a.Sku] = _items.GetValueOrDefault(a.Sku) + a.Qty; return new Accepted(a.CartId); }
    private CommandReply Remove(RemoveItem r) { _items.Remove(r.Sku); return new Accepted(r.CartId); }
}
// cart.Ask<CommandReply>(new AddItem("c1","A",2)) works with no cast.
```

### 4.3 Akka serialization (default serializer in 1.5.73 = `NewtonSoftJsonSerializer`)

```
union:  {"$id":"1","$type":"OrderCommand, U6","Value":{"$type":"CancelOrder, U6","OrderId":"o1","Reason":"test"}}
        FromBinary -> OrderCommand with Value == null        <-- SILENT DATA LOSS
closed: FromBinary -> AddItem { CartId = c1, Sku = B, Qty = 3 }   <-- correct
```

So for remoting, sharding or persistence, never send or persist a generated union. The
serialization chapter should still recommend a proper serializer (STJ- or protobuf-based) over
Newtonsoft. That's for the Akka serialization researcher to confirm.

---

## 5. System.Text.Json and unions/closed hierarchies (.NET 11 RC3, VERIFIED `u5`, `u9`)

The docs page "Serialize union types with System.Text.Json" (2026-09-24) says unions are
written as the **active case value only**, and read back by **JSON token type**.

```csharp
public sealed record OrderPlaced(string OrderId, string Customer, decimal Total);
public sealed record OrderShipped(string OrderId, DateTimeOffset At);
public sealed record OrderCancelled(string OrderId, string Reason);

public union OrderEvent(OrderPlaced, OrderShipped, OrderCancelled);

[JsonUnion(TypeClassifier = typeof(JsonUnionTypeStructuralClassifier))]
public union OrderEventS(OrderPlaced, OrderShipped, OrderCancelled);

public union Reading(int, string, OrderShipped);

[JsonPolymorphic(InferClosedTypePolymorphism = true)]
public closed record CartEvent(string CartId);
public sealed record ItemAdded(string CartId, string Sku, int Qty) : CartEvent(CartId);
public sealed record ItemRemoved(string CartId, string Sku) : CartEvent(CartId);

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$event")]
[JsonDerivedType(typeof(Opened), "account.opened.v1")]
[JsonDerivedType(typeof(Deposited), "account.deposited.v1")]
public closed record AccountEvent(string AccountId);
public sealed record Opened(string AccountId, string Owner) : AccountEvent(AccountId);
public sealed record Deposited(string AccountId, decimal Amount) : AccountEvent(AccountId);

[JsonSerializable(typeof(OrderEventS))]
[JsonSerializable(typeof(Reading))]
[JsonSerializable(typeof(CartEvent))]
[JsonSerializable(typeof(AccountEvent))]
public partial class AppJson : JsonSerializerContext;
```

Actual output:

```
1 serialize:  {"OrderId":"o1","Customer":"ada","Total":9.5}
1 roundtrip:  THREW JsonException: JSON value type 'Object' is ambiguous for union type 'OrderEvent'
              because multiple case types can use this value type. Specify a custom type classifier...
2 serialize:  {"OrderId":"o1","Reason":"changed mind"}
2 roundtrip:  OrderCancelled { OrderId = o1, Reason = changed mind }
2 shipped rt: OrderShipped { OrderId = o2, At = 11/4/2026 ... }
2 only OrderId: THREW JsonException: The custom type classifier for union type 'OrderEventS'
              returned null for JSON token type 'StartObject'.
2 srcgen roundtrip: OrderCancelled { ... }                     (source-gen works)
3 Int32: 42 -> 42
3 String: "hi" -> hi
3 OrderShipped: {...} -> OrderShipped {...}
3 default union: null
3 Web options string: THREW JsonException: JSON value type 'String' is ambiguous for union type 'Reading'
4 serialize:  {"$type":"ItemAdded","Sku":"SKU","Qty":2,"CartId":"c1"}
4 roundtrip:  ItemAdded { CartId = c1, Sku = SKU, Qty = 2 }
4 srcgen:     {"$type":"ItemAdded",...}
5 serialize:  {"$event":"account.deposited.v1","Amount":100,"AccountId":"a1"}
5 roundtrip:  Deposited { AccountId = a1, Amount = 100 }
```

Adding `[JsonSerializable(typeof(OrderEvent))]` (the union without a classifier) to the
source-gen context gives:

```
warning SYSLIB1227: Union type 'OrderEvent': case types 'OrderPlaced', 'OrderShipped', 'OrderCancelled'
all serialize as JSON value type 'Object'. Set JsonUnionAttribute.TypeClassifier to provide a custom
classifier that can disambiguate.
```

More verified findings (`u9`):
- **Nested closed levels work in RC3.** `closed record OrderEvent` → `closed record Terminal` →
  `sealed record Cancelled`, with `InferClosedTypePolymorphism = true` on the root, round-trips
  `{"$type":"Cancelled","Reason":"x","OrderId":"o"}`. Andrew Lock reported this failing in
  Preview 7. It's fixed now.
- **Silent data loss without the attribute.** A `closed record NoAttr(string Id)` holding a
  `NoAttrLeaf(Id, Extra)` serializes as `{"Id":"a"}`. Setting
  `new JsonSerializerOptions { InferClosedTypePolymorphism = true }` at the options level gives
  `{"$type":"NoAttrLeaf","Extra":5,"Id":"a"}`. **Book advice:** set it on the options or the
  source-gen context globally, not per type.
- The structural classifier only looks at root-level property **names**. Cases need a unique
  property, nested unions aren't supported, and `ReferenceHandler.Preserve` isn't supported.
  It's brittle for evolving event schemas.
- **Versioning advice (for the operations chapter):** use explicit, stable discriminator
  strings (`"account.deposited.v1"`) through `[JsonDerivedType]`. Inferred discriminators use
  the type name, so a rename breaks your journal.

Other .NET 11 STJ additions listed in "What's new in .NET 11 libraries" (UNVERIFIED locally):
`JsonNamingPolicy.PascalCase`, `[JsonNamingPolicy]` attribute, type-level `[JsonIgnore]`,
open-generic `[JsonDerivedType]`, `JsonSerializer.SerializeAsyncEnumerable(..., topLevelValues: true)`
(JSON Lines), `GetTypeInfo<T>()`, `Utf8JsonWriter.Reset(...)`, `IReadOnlySet<T>` support.

---

## 6. Result types: a `OneOf` replacement (VERIFIED, `u3`)

```csharp
public readonly record struct Ok<T>(T Value);
public readonly record struct Err<E>(E Error);

public union Result<T, E>(Ok<T>, Err<E>)
{
    public bool IsOk => this is Ok<T>;

    public Result<U, E> Map<U>(Func<T, U> f) => this switch
    {
        Ok<T> ok => new Ok<U>(f(ok.Value)),
        Err<E> err => err,
        null => throw new InvalidOperationException("default Result"),
    };

    public Result<U, E> Bind<U>(Func<T, Result<U, E>> f) => this switch
    {
        Ok<T> ok => f(ok.Value),
        Err<E> err => err,
        null => throw new InvalidOperationException("default Result"),
    };
}

// Domain errors as a union, nested in a command-reply union
public sealed record OutOfStock(string Sku, int Requested, int Available);
public sealed record CustomerBlocked(string CustomerId);
public sealed record InvalidQuantity(int Qty);
public union PlaceOrderError(OutOfStock, CustomerBlocked, InvalidQuantity);

public sealed record OrderAccepted(string OrderId);
public union PlaceOrderReply(OrderAccepted, PlaceOrderError);

public static Result<int, PlaceOrderError> ValidateQty(int qty) =>
    qty > 0 ? new Ok<int>(qty) : new Err<PlaceOrderError>(new InvalidQuantity(qty));

public static Result<int, PlaceOrderError> Reserve(string sku, int qty, int stock) =>
    qty <= stock ? new Ok<int>(stock - qty) : new Err<PlaceOrderError>(new OutOfStock(sku, qty, stock));

public static PlaceOrderReply Place(string customer, string sku, int qty, int stock)
{
    if (customer == "mallory") return (PlaceOrderError)new CustomerBlocked(customer);
    if (qty <= 0) return (PlaceOrderError)new InvalidQuantity(qty);
    if (qty > stock) return (PlaceOrderError)new OutOfStock(sku, qty, stock);
    return new OrderAccepted($"ord-{sku}-{qty}");
}

public static string Explain(PlaceOrderError e) => e switch
{
    OutOfStock o => $"only {o.Available} of {o.Sku} left",
    CustomerBlocked c => $"customer {c.CustomerId} blocked",
    InvalidQuantity q => $"bad qty {q.Qty}",
};

public static string Render(PlaceOrderReply r) => r switch
{
    OrderAccepted a => $"OK {a.OrderId}",
    PlaceOrderError e => $"REJECTED: {Explain(e)}",
};

public static bool IsStock(PlaceOrderReply r) => r is PlaceOrderError and OutOfStock;
```

Output: `only 2 of SKU left` / `10` / `OK ord-A-1` /
`REJECTED: customer mallory blocked` / `REJECTED: only 5 of A left  stock? True`.

Design notes:
- `union Either<TL,TR>(TL, TR)` works (`Either<int,string> e = 5;`), but it's **ambiguous
  when TL == TR** (CS0457). The wrapper cases `Ok<T>`/`Err<E>` avoid that and read better.
  Recommend them.
- Each `Err<PlaceOrderError>(...)` needs an explicit `new`, because conversions aren't
  transitive. Static helpers such as `Result.Fail(...)` cut down the noise.
- This replaces `OneOf<T0..Tn>`, `ErrorOr`, `FluentResults` and similar libraries with no
  dependency, and gives exhaustiveness for free.

---

## 7. Value objects and entities: C# 12-14 features (VERIFIED, `u7`)

### 7.1 `readonly record struct` + `field` keyword validator

```csharp
public readonly record struct Quantity
{
    // field keyword (C# 14): the init accessor validates, so ctor AND `with` both go through the guard
    public int Value
    {
        get;
        init => field = value > 0 ? value : throw new ArgumentOutOfRangeException(nameof(Value), value, "must be > 0");
    }
    public Quantity(int value) => Value = value;
    public static Quantity operator +(Quantity a, Quantity b) => new(a.Value + b.Value);
}
```

Output: `new Quantity(0)` throws (`ParamName = Value`). `new Quantity(5) with { Value = -1 }`
**throws** ("with validated"). `default(Quantity).Value = 0` **bypasses validation**.

What went wrong first (worth teaching): the classic
`readonly record struct Quantity(int Value) { public int Value { get; } = Value > 0 ? ... }`
compiles, but then `with { Value = 6 }` fails with `CS0200: ... cannot be assigned to -- it is read only`.
And if you make it `{ get; init; }` instead, `with` skips the check. The `field` + `init` form
is the clean answer.

Use a `sealed record` (reference type) for value objects where `default` must never exist,
such as `Email`. Use a `readonly record struct` for small numeric or ID types where `default` is
acceptable or checked at the boundary.

### 7.2 Static abstract interface members: a generic value-object contract

```csharp
public interface IValueObject<TSelf, TValue> where TSelf : IValueObject<TSelf, TValue>
{
    TValue Value { get; }
    static abstract bool TryCreate(TValue raw, [NotNullWhen(true)] out TSelf? result, out string? error);
    static virtual TSelf Create(TValue raw) =>
        TSelf.TryCreate(raw, out var r, out var err) ? r : throw new ArgumentException(err);
}

public sealed record Email : IValueObject<Email, string>
{
    public string Value { get; }
    private Email(string value) => Value = value;
    public static bool TryCreate(string raw, [NotNullWhen(true)] out Email? result, out string? error)
    {
        raw = raw.Trim();
        if (raw.IndexOf('@') is > 0 and var at && at < raw.Length - 1)
        { result = new(raw.ToLowerInvariant()); error = null; return true; }
        result = null; error = $"'{raw}' is not an email"; return false;
    }
    public override string ToString() => Value;
}

public static TSelf Parse<TSelf, TValue>(TValue raw) where TSelf : IValueObject<TSelf, TValue> => TSelf.Create(raw);
// Parse<Email,string>("  Ada@Example.COM ") -> ada@example.com
```

(A possible variant: `TryCreate` returning a `union` `Result<TSelf, ValidationError>`.)

### 7.3 Generic math operator interfaces for Money

```csharp
public readonly record struct Money(decimal Amount, string Currency)
    : IAdditionOperators<Money, Money, Money>, IComparisonOperators<Money, Money, bool>
{
    public static Money operator +(Money a, Money b) =>
        a.Currency == b.Currency ? new(a.Amount + b.Amount, a.Currency) : throw new InvalidOperationException("currency mismatch");
    public static bool operator <(Money a, Money b) => a.Amount < b.Amount;
    public static bool operator >(Money a, Money b) => a.Amount > b.Amount;
    public static bool operator <=(Money a, Money b) => a.Amount <= b.Amount;
    public static bool operator >=(Money a, Money b) => a.Amount >= b.Amount;
    public static Money Zero(string ccy) => new(0, ccy);
}
public static T Sum<T>(IEnumerable<T> xs, T zero) where T : IAdditionOperators<T, T, T>
{ var acc = zero; foreach (var x in xs) acc += x; return acc; }
// Sum([new(10,"USD"), new(2.5m,"USD")], Money.Zero("USD")) -> Money { Amount = 12.5, Currency = USD }
```

### 7.4 `required` + `field` on entity properties

```csharp
public sealed class Customer
{
    public required string Name
    {
        get;
        set => field = string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("name required") : value.Trim();
    }
    public int CreditLimit { get; init => field = value >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(value)); }
}
// new Customer { Name = "  Grace ", CreditLimit = 5 }.Name == "Grace"
```

### 7.5 Primary constructors on classes (C# 12) for services and actors

```csharp
public sealed class PricingService(TimeProvider clock, decimal weekendSurcharge = 0.1m)
{
    public decimal Price(decimal basePrice) =>
        clock.GetUtcNow().DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday ? basePrice * (1 + weekendSurcharge) : basePrice;
}
```

Note: class primary-constructor parameters are **captured, mutable, and not properties**. For
actors (`sealed class OrderActor(IActorRef inventory, TimeProvider clock) : ReceiveActor`)
this is neat DI. The usual style warning applies, though: assign to `readonly` fields if you
need immutability guarantees. Actor-specific use with `Props.Create` / Akka.DependencyInjection
is UNVERIFIED here.

---

## 8. Extension members (C# 14) and extension indexers (C# 15) (VERIFIED)

```csharp
public static class DomainExtensions
{
    extension(Quantity q)
    {
        public bool IsBulk => q.Value >= 100;                          // extension property
        public Quantity Double() => new(q.Value * 2);                   // extension method
    }
    extension(Quantity)                                                 // receiver-less = static members
    {
        public static Quantity One => new(1);                           // static extension property
        public static Quantity operator *(Quantity a, int k) => new(a.Value * k);   // extension operator
    }
    extension<T>(IReadOnlyList<T> list)
    {
        public T this[Index i, T fallback] =>                           // C# 15 extension indexer
            i.GetOffset(list.Count) is var o && o >= 0 && o < list.Count ? list[o] : fallback;
        public bool IsEmpty => list.Count == 0;
    }
}
// q.IsBulk, q.Double(), Quantity.One, q * 3, skus[^1, "none"], skus[5, "none"], skus.IsEmpty  all work
```

Plus the `IActorRef` `Send` helper in section 4.1. DDD uses:
- Typed `Tell`/`Ask` helpers per protocol (`extension(IActorRef<...>)` or `extension(IActorRef)`).
- Domain-query properties on value objects that live in another layer, like `money.IsZero`.
- Test DSL helpers on `TestKit` probes.

---

## 9. Other language features worth using (VERIFIED unless noted)

**Collection expressions (C# 12) with C# 15 `with(...)` arguments:**

```csharp
HashSet<string> tags = [with(StringComparer.OrdinalIgnoreCase), "Gold", "GOLD", "silver"]; // Count == 2
List<int> big = [with(capacity: 64), 1, 2, .. new[] { 3, 4 }];                            // Count 4, Capacity 64
private readonly Dictionary<string, int> _items = [];                                     // empty-collection init
```

**params collections (C# 13):**

```csharp
public static string Join(params ReadOnlySpan<string> parts) => string.Join("|", parts);
public static int Count(params IEnumerable<int> xs) => xs.Count();
```

**List patterns over event histories:**

```csharp
object[] history = ["Placed", "LineAdded", "LineAdded", "Shipped"];
history switch
{
    [] => "empty",
    ["Placed", .., "Shipped"] => "placed...shipped",
    ["Placed", ..] => "open",
    _ => "corrupt",
};  // "placed...shipped"
```

Gotcha: `raw is [.., '@', ..]` (two slices) gives `CS8980: Slice patterns may only be used once and directly inside a list pattern.`

**File-scoped types** (`file sealed class Helper`) work. Use them for test fakes and
source-local helpers.

**`System.Threading.Lock` (.NET 9 / C# 13):**

```csharp
private readonly Lock _gate = new();
public int Next() { lock (_gate) { return ++_n; } }                  // compiler uses Lock.EnterScope
public int NextScoped() { using (_gate.EnterScope()) { return ++_n; } }
```

(Inside actors you never need locks. This only belongs in a sidebar on non-actor shared services.)

**Null-conditional assignment (C# 14):** `real?.Name = "Hopper";` works.
**`nameof` unbound generic (C# 14):** `nameof(List<>)` gives `"List"`.
**Labeled break (C# 15):** `outer: foreach (...) foreach (...) { if (v < 0) break outer; }` works.

UNVERIFIED (not tested, low relevance): C# 14 partial constructors and events, user-defined
compound assignment operators, first-class `Span` conversions, and `scoped`/`ref` modifiers on
simple lambda parameters. C# 15 non-virtual static interface members on older runtimes don't
matter on net11.0.

---

## 10. TimeProvider and FakeTimeProvider (VERIFIED, `u8`)

Package: `Microsoft.Extensions.TimeProvider.Testing` **10.11.0**. It works on net11.0, and no
11.x build exists yet.

```csharp
using Microsoft.Extensions.Time.Testing;

public sealed class OrderTimeout(TimeProvider clock)
{
    public DateTimeOffset PlacedAt { get; } = clock.GetUtcNow();
    public bool Expired => clock.GetUtcNow() - PlacedAt > TimeSpan.FromMinutes(30);
    public Guid NewId() => Guid.CreateVersion7(clock.GetUtcNow());
}

var fake = new FakeTimeProvider(new DateTimeOffset(2026, 11, 4, 9, 0, 0, TimeSpan.Zero));
var t = new OrderTimeout(fake);
t.Expired;                         // False
fake.Advance(TimeSpan.FromMinutes(31));
t.Expired;                         // True
t.NewId().Version;                 // 7  (time-ordered UUIDs: good for event/aggregate ids)

var fired = 0;
using var timer = fake.CreateTimer(_ => fired++, null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));
fake.Advance(TimeSpan.FromSeconds(35));   // fired == 3

var delay = Task.Delay(TimeSpan.FromHours(1), fake);
fake.Advance(TimeSpan.FromHours(1));
await delay;                               // completes
```

A hand-rolled fake is just as easy: `sealed class FixedClock(DateTimeOffset now) : TimeProvider { public override DateTimeOffset GetUtcNow() => now; }`.

**Book advice:** inject `TimeProvider` into aggregates and domain services, not
`DateTime.UtcNow`. Akka has its own scheduler. Akka.TestKit's `TestScheduler` is the right
tool for actor timers, and whether Akka 1.5.73 can be driven by a `TimeProvider` is
**UNVERIFIED**, so check with the Akka testing researcher.

---

## 11. Diagnostics: metrics and tracing APIs (VERIFIED, `u8`)

`IMeterFactory` + `MetricCollector<T>` (`Microsoft.Extensions.Diagnostics.Testing` 10.11.0):

```csharp
public sealed class OrderMetrics
{
    public const string MeterName = "Shop.Orders";
    private readonly Counter<long> _placed;
    private readonly Histogram<double> _value;
    public OrderMetrics(IMeterFactory factory)
    {
        var meter = factory.Create(MeterName, version: "1.0.0");
        _placed = meter.CreateCounter<long>("shop.orders.placed", unit: "{order}", description: "Orders placed");
        _value = meter.CreateHistogram<double>("shop.order.value", unit: "USD",
            advice: new InstrumentAdvice<double> { HistogramBucketBoundaries = [10, 50, 100, 500] });
    }
    public void Placed(string channel, decimal total)
    {
        _placed.Add(1, new KeyValuePair<string, object?>("shop.channel", channel));
        _value.Record((double)total, new KeyValuePair<string, object?>("shop.channel", channel));
    }
}

var services = new ServiceCollection().AddMetrics().AddSingleton<OrderMetrics>().BuildServiceProvider();
using var collector = new MetricCollector<long>(services.GetRequiredService<IMeterFactory>(), OrderMetrics.MeterName, "shop.orders.placed");
// after 3 Placed() calls: collector.GetMeasurementSnapshot().Count == 3; web sum == 2
```

`ActivitySource` with a plain `ActivityListener` prints
`activity order.place kind=Internal tags=order.id=o1 status=Ok`.

**New in .NET 11: `AddTracing` (Microsoft.Extensions.Diagnostics 11.0.0-rc.3).** This mirrors
`AddMetrics` with rule-based enable and disable of ActivitySources:

```csharp
using Microsoft.Extensions.Diagnostics.Tracing;   // REQUIRED for EnableTracing/AddListener (they're extension methods here)

var builder = Host.CreateApplicationBuilder();
builder.Services.AddTracing(tracing =>
{
    tracing.EnableTracing(sourceName: "Shop.Orders");
    tracing.DisableTracing(sourceName: "Shop.Orders", operationName: "order.healthcheck");
    tracing.AddListener("console", l => l.ActivityStopped = a => Console.WriteLine($"[host listener] {a.OperationName}"));
});
```

Verified: `order.ship` reached the host listener, and `order.healthcheck` was filtered out. The
API surface found by reflection is `TracingBuilderExtensions.{AddListener, ClearListeners,
EnableTracing, DisableTracing}`, `TracingBuilderConfigurationExtensions.AddConfiguration(IConfiguration)`,
`ActivityListenerBuilder{Sample, SampleUsingParentId, ActivityStarted, ActivityStopped, ExceptionRecorder}`,
`TracingOptions.Rules`, `TracingRule`, and `ActivitySourceScopes`. The docs snippet in
"What's new in .NET 11 libraries" leaves out the `using` and won't compile as shown.
OpenTelemetry's `AddOpenTelemetry().WithTracing(...)` is still the main path for exporting.
The relationship between `AddTracing` and OTel (does OTel's SDK respect these rules?) is
**UNVERIFIED**, so hand it to the telemetry researcher.

Other .NET 11 diagnostics items from the docs (UNVERIFIED): `ActivitySourceFactory`, an
unsealed `ActivitySource`, and MemoryCache metrics (`TrackStatistics = true`, meter
`Microsoft.Extensions.Caching.Memory.MemoryCache`).

---

## 12. Other .NET 11 library additions that might be useful (from docs, UNVERIFIED)

From "What's new in .NET 11 libraries" (last updated for RC2):
- `System.Diagnostics.Process.RunAndCaptureTextAsync("git", ["status","--porcelain"])`, which is
  handy for a release or versioning tooling chapter (for example, reading git tags in a build
  script).
- `EqualityComparer<T>.Create(p => p.Name)`, which makes key-based comparers for entities by Id trivial.
- LINQ `LeftJoin`/`RightJoin`/`FullJoin`.
- `OptionsBuilder.Validate<TValidator>()`, `IAsyncStartupValidator`, async DataAnnotations
  validation (`Validator.ValidateObjectAsync`), and `[ConfigurationIgnore]`, which are useful for
  validating Akka.Hosting options at startup.
- `Type.GetNullableUnderlyingType()`, `Base64.EncodeToString`, and `Zstandard` in `System.IO.Compression`.

---

## 13. Recommended project defaults for the sample repo

```xml
<!-- Directory.Build.props -->
<Project>
  <PropertyGroup>
    <TargetFramework>net11.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <!-- LangVersion omitted: 15.0 is the default for net11.0 -->
    <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
    <WarningsAsErrors>$(WarningsAsErrors);CS8509;CS8655;nullable</WarningsAsErrors>
  </PropertyGroup>
</Project>
```

Also pin the SDK with a `global.json` (`"version": "11.0.100-rc.3.26522.148", "rollForward": "latestFeature"`),
and move to GA when it ships.

House style for the book's code:
1. Leaf messages and events are `sealed record`. Protocol roots are `closed record`. Middle levels are `closed record`.
2. Decision and return types use `union` with wrapper records. Always include a `null =>` arm when the value could be `default`.
3. Never `Tell` a generated union. Unwrap it or reply with a closed-hierarchy type.
4. Value objects are a `readonly record struct` with a `field`-validated `init`, or a `sealed record` with a private ctor and a static `TryCreate` behind an `IValueObject<TSelf,TValue>` contract.
5. Inject `TimeProvider`. Use `Guid.CreateVersion7(clock.GetUtcNow())` for ids.
6. Use `IMeterFactory` for meters and `MetricCollector<T>` in tests.

---

## 14. Sources

- What's new in C# 15: https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-15 (updated 2026-10-23)
- Union types reference: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/union
- Unions feature spec: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-15.0/unions (csharplang `proposals/csharp-15.0/unions.md`)
- Closed hierarchies spec: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-15.0/closed-hierarchies (linked from What's new; not read in full)
- STJ union types: https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/union-types (2026-09-24)
- What's new in .NET 11 libraries: https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/libraries
- SYSLIB1227: https://learn.microsoft.com/dotnet/fundamentals/syslib-diagnostics/syslib1227
- Andrew Lock, "the pain of serializing unions and closed class hierarchies" (Preview 7): https://andrewlock.net/exploring-the-dotnet-11-preview-7-the-pain-of-serializing-unions-and-closed-class-hierarchies-with-system-text-json/ (its nested-closed-hierarchy issue is fixed in RC3, verified above)
- NuGet flat container: https://api.nuget.org/v3-flatcontainer/{akka,system.text.json,microsoft.extensions.timeprovider.testing,microsoft.extensions.diagnostics.testing,opentelemetry,aspire.hosting.apphost}/index.json
