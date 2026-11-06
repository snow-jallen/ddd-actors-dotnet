# Research notes: testing DDD + Akka.NET on .NET 11 (state as of 2026-11-04)

Author: testing research agent. Audience: chapter writers and the sample-repo builder.

Everything marked **VERIFIED** was compiled and run on this machine with the local .NET 11 SDK
(`11.0.100-rc.3.26522.148`, sourced via `tools/env.sh`) against the exact package versions listed.
Anything not run locally is marked **UNVERIFIED**.

The full working spike code is kept next to these notes so writers can copy from it:

- `research/testing-spike/unit/`: xUnit v3 + Akka.TestKit + Persistence.TestKit + Hosting.TestKit (local sharding) + Shouldly/AwesomeAssertions + FsCheck/CsCheck + Verify. **15/15 tests pass, run 4 times, about 2 s per run.**
- `research/testing-spike/integration/`: ASP.NET minimal API + Akka.Hosting + Akka.Persistence.Sql on PostgreSQL. It has WebApplicationFactory + Testcontainers tests (2/2 pass, about 11 s) and an Aspire AppHost + `Aspire.Hosting.Testing` test (1/1 pass), all against rootless Podman.

---

## 1. Package matrix (exact versions, checked on NuGet on 2026-11-04)

Versions come from `https://api.nuget.org/v3-flatcontainer/<id>/index.json`. Dependencies come from each package's `.nuspec`.

| Purpose | Package | Latest stable | Notes |
|---|---|---|---|
| Akka core | `Akka` | **1.5.73** (2026-10-05) | `1.6.0-beta5` was published 2026-11-04 and targets **net10.0 only** |
| TestKit core | `Akka.TestKit` | 1.5.73 | Framework-agnostic: `TestKitBase`, `TestProbe`, `TestScheduler`, `EventFilter` |
| **TestKit for xUnit v3** | **`Akka.TestKit.Xunit`** | 1.5.73 | Depends on `xunit.v3.assert` and `xunit.v3.extensibility.core` **3.2.2** (net10.0 and netstandard2.0 groups). It was switched to xUnit v3 in **Akka 1.5.41 (May 6, 2025)**. |
| TestKit for xUnit v2 | `Akka.TestKit.Xunit2` | 1.5.73 | Depends on `xunit` 2.8.1. This is the legacy path. |
| `Akka.TestKit.Xunit3` | **does not exist** | n/a | Do not invent it. The v3 package is the un-suffixed `Akka.TestKit.Xunit`. |
| Persistence TestKit core | `Akka.Persistence.TestKit` | 1.5.73 | `TestJournal`, `TestSnapshotStore`, interceptors |
| Persistence TestKit for xUnit v3 | **`Akka.Persistence.TestKit.Xunit`** | 1.5.73 | First published 1.5.63. Depends on `Akka.TestKit.Xunit`. |
| Persistence TestKit for xUnit v2 | `Akka.Persistence.TestKit.Xunit2` | 1.5.73 | Legacy |
| Hosting TestKit (xUnit v3) | **`Akka.Hosting.TestKit`** | 1.5.73 | **Breaking in Akka.Hosting 1.5.64 (2026-03-31):** it now targets xUnit v3 and depends on `Akka.TestKit.Xunit` |
| Hosting TestKit (xUnit v2) | `Akka.Hosting.TestKit.Xunit2` | 1.5.73 | Created in 1.5.64 for people who stay on v2 |
| Multi-node | `Akka.MultiNode.TestAdapter` + `Akka.Remote.TestKit` / `Akka.Cluster.TestKit` | 1.5.73 | The TestAdapter depends on `xunit.v3.extensibility.core` 3.2.2. The old `Akka.MultiNodeTestRunner` stopped at 1.4.51 and is dead. |
| NUnit / MSTest TestKits | `Akka.TestKit.NUnit`, `Akka.TestKit.MSTest` | **1.5.67** | Community repos that lag the main release train. Avoid them in the book. |
| xUnit v3 | `xunit.v3` | **4.0.2** (2026-10-09) | 4.0.0 shipped 2026-08-14. It pulls `xunit.v3.mtp-v2`, so MTP v2 is the default. |
| xUnit v3 templates | `xunit.v3.templates` | 4.0.2 | `dotnet new install xunit.v3.templates` |
| MTP | `Microsoft.Testing.Platform` | 2.5.2 | xunit.v3 4.0.2 resolved 2.5.1 transitively |
| Assertions | `Shouldly` | 4.3.0 (Jan 2025) | 5.0.0-preview.2 exists. 4.3.0 works fine on net11.0. |
| Assertions | `AwesomeAssertions` | 9.6.0 | Apache-2.0 fork of FluentAssertions |
| Assertions | `FluentAssertions` | 8.11.0 | **Commercial (Xceed) license from v8.** Avoid. |
| Snapshot | `Verify.XunitV3` | 33.3.4 | **The maintenance fee is enforced at build time from v33.** See section 8. |
| Property-based | `FsCheck` / `FsCheck.Xunit.v3` | 3.6.0 | `FsCheck.Xunit.v3` requires `xunit.v3.extensibility.core [4.0.0, 5.0.0)`, so it **needs xunit.v3 4.x** |
| Property-based | `CsCheck` | 5.0.1 (2026-11-03) | Targets net10.0 and net11.0. No attribute needed. |
| Containers | `Testcontainers`, `Testcontainers.PostgreSql`, `Testcontainers.XunitV3` | 4.16.0 | `Testcontainers.XunitV3` declares extensibility.core 3.2.2 but works with 4.0.2 (VERIFIED) |
| ASP.NET integration | `Microsoft.AspNetCore.Mvc.Testing` | 10.0.14 stable; **11.0.0-rc.3.26522.148** matches the SDK | Use the RC to match net11.0 |
| Aspire testing | `Aspire.Hosting.Testing`, `Aspire.AppHost.Sdk`, `Aspire.Hosting.PostgreSQL` | 13.6.4 | Local Aspire CLI is 13.6.3 |
| Time abstraction | `Microsoft.Extensions.TimeProvider.Testing` | 10.11.0 | `FakeTimeProvider` for pure-domain code |
| SQL journal | `Akka.Persistence.Sql.Hosting` | 1.5.73 | Uses linq2db 6.5.1. Use `Npgsql` 10.0.3 with it. |
| Other frameworks | `TUnit` 1.80.0, `NUnit` **5.0.0** (2026-09-27), `MSTest`/`MSTest.Sdk` 4.5.2 | n/a | See section 3 |

### Version-skew observation (VERIFIED)

`Akka.TestKit.Xunit` 1.5.73 is compiled against xUnit v3 **3.2.2**. Referencing `xunit.v3` **4.0.2** next to it lifts the whole `xunit.v3.*` family to 4.0.2. It builds with **0 warnings** and every TestKit test ran fine. xUnit 4.0 removed and changed runner-internal extensibility APIs (orderers, runners, `XunitTestCaseRunner` constructors). The Akka TestKit only uses `ITestOutputHelper` and the assertion and lifetime interfaces, so it is not affected. **Recommendation:** use `xunit.v3` 4.0.2 with Akka 1.5.73. You need 4.x anyway for `FsCheck.Xunit.v3` 3.6.0 and `Verify.XunitV3` 33.x, which both require extensibility.core 4.x.

### Akka 1.6 note (for the "operations / upgrades" chapter)

1.6.0-beta1 (2026-10-02) lists "TestKit `IAsyncLifetime`" as a breaking change (PR #8545). `TestKit` gets `virtual InitializeAsync()` and `virtual DisposeAsync()`, and derived classes that declared their own must now `override` and chain to the base. Akka.Hosting.TestKit moves into the main repo and versions in lockstep with Akka. **All 1.6 packages target net10.0 only.** A net11.0 project can reference them because net10 assets are compatible (UNVERIFIED with 1.6 betas). The book should pin **1.5.73**. A "migrating to 1.6" sidebar is optional. (Sources: akka.net RELEASE_NOTES.md; PR https://github.com/akkadotnet/akka.net/pull/8545.)

---

## 2. Microsoft.Testing.Platform (MTP) and `dotnet test` on .NET 10/11

### The gotcha that will bite every reader (VERIFIED)

xUnit v3 **4.x dropped MTP v1 and VSTest-via-MTP support on the .NET 10+ SDK**. With the .NET 11 SDK and no `test` section in `global.json`, `dotnet test` **fails at build time**:

```
error : Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10 SDK and later.
If you use dotnet test, you should opt-in to the new dotnet test experience.
For more information, see https://aka.ms/dotnet-test-mtp-error
```

The fix is to add the MTP runner to `global.json` at the repo root:

```json
{
  "sdk": { "version": "11.0.100-rc.3.26522.148", "rollForward": "latestFeature", "allowPrerelease": true },
  "test": { "runner": "Microsoft.Testing.Platform" }
}
```

From .NET 11 Preview 6 you can set `DOTNET_TEST_RUNNER=Microsoft.Testing.Platform` instead. It overrides `global.json` for the current process. Source: https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-with-dotnet-test (updated 2026-09-14).

### CLI differences in MTP mode (VERIFIED where marked)

- A bare `dotnet test` in a folder with no project or solution fails with "Specify a project or solution file" (VERIFIED). Use `dotnet test --solution Cart.slnx` or `dotnet test --project Probe.Tests` (both VERIFIED). Positional `dotnet test MyProject.csproj` becomes `--project`, and a dll path becomes `--test-modules`.
- Arguments are passed straight to the test app, so the extra `--` is optional. xUnit filters look like `dotnet test --solution Cart.slnx -- --filter-class "*ReservationSpecs"` (VERIFIED, ran 1 test). xUnit's own filter options (`--filter-class`, `--filter-method`, `--filter-trait`, `--filter-query`) differ from MSTest's `--filter`.
- `--report-trx` only works when `Microsoft.Testing.Extensions.TrxReport` is referenced. For coverage, use `Microsoft.Testing.Extensions.CodeCoverage` (18.12.0) or `coverlet.MTP` (10.1.0). `coverlet.collector` is VSTest-only. (UNVERIFIED locally.)
- xUnit v3 test projects are **executables**: `<OutputType>Exe</OutputType>`, and `dotnet run` runs the tests too (VERIFIED). xUnit generates the entry point. **Do not put top-level statements in a test project.** It produces CS7022 and your code becomes the entry point instead of the test runner (VERIFIED in a reflection probe).
- The built-in `dotnet new xunit` template in SDK 11.0.100-rc.3 **still produces xUnit v2 + VSTest** (`xunit` 2.9.3, `Microsoft.NET.Test.Sdk` 17.14.1, `coverlet.collector`) (VERIFIED). Tell readers to run `dotnet new install xunit.v3.templates` (4.0.2) or to hand-write the csproj below.

### Minimal csproj that worked (VERIFIED, 1 TestProbe test via `dotnet test`)

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net11.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <OutputType>Exe</OutputType>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="xunit.v3" Version="4.0.2" />
    <PackageReference Include="Akka.TestKit.Xunit" Version="1.5.73" />
  </ItemGroup>
  <ItemGroup>
    <Using Include="Xunit" />
  </ItemGroup>
</Project>
```

The resolved transitive packages were Akka 1.5.73, Akka.TestKit 1.5.73, Akka.Analyzers 0.3.3, xunit.v3.{assert,common,core.mtp-v2,extensibility.core,mtp-v2,runner.common,runner.inproc.console} 4.0.2, Microsoft.Testing.Platform(+.MSBuild) 2.5.1 and xunit.analyzers 2.2.0. **No `Microsoft.NET.Test.Sdk` and no `xunit.runner.visualstudio` are needed.** Add `xunit.runner.visualstudio` 4.0.1 only if you need VS Test Explorer in VSTest mode. VS and Rider both run MTP directly.

```csharp
using Akka.Actor;
using Akka.TestKit.Xunit;

namespace Probe.Tests;

public sealed record Greet(string Name, IActorRef ReplyTo);
public sealed record Greeted(string Message);

public sealed class Greeter : ReceiveActor
{
    public Greeter() => Receive<Greet>(g => g.ReplyTo.Tell(new Greeted($"Hello, {g.Name}")));
}

public sealed class GreeterSpecs(ITestOutputHelper output) : TestKit(output: output)
{
    [Fact]
    public async Task Greeter_replies_to_the_probe()
    {
        var probe = CreateTestProbe();
        var greeter = Sys.ActorOf(Props.Create<Greeter>(), "greeter");

        greeter.Tell(new Greet("Ada", probe.Ref));

        var reply = await probe.ExpectMsgAsync<Greeted>(TimeSpan.FromSeconds(3),
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal("Hello, Ada", reply.Message);
        await probe.ExpectNoMsgAsync(TimeSpan.FromMilliseconds(100), TestContext.Current.CancellationToken);
    }
}
```

Note: in xUnit v3, `ITestOutputHelper` lives in namespace `Xunit` (it was `Xunit.Abstractions` in v2), and `TestContext.Current.CancellationToken` is the cancellation token to use. xUnit v3 analyzers nudge you to pass it.

---

## 3. Choosing a test framework in late 2026

| Framework | Status (Nov 2026) | Akka TestKit support | Verdict for this book |
|---|---|---|---|
| **xUnit v3 (4.0.x)** | Active. 4.0.0 (Aug 2026) added full parallelization modes, AOT, fixture lifecycle notifications and assembly fixtures; MTP v2 is the default. | **First-class.** Akka's own test suite uses it, and Akka.TestKit.Xunit, Persistence.TestKit.Xunit, Hosting.TestKit and MultiNode.TestAdapter all target it. | **Use this.** |
| xUnit v2 (2.9.3) | Maintenance only | `*.Xunit2` packages | Mention as legacy only |
| NUnit 5.0.0 | Released 2026-09-27; net8.0, net10.0, net462 | `Akka.TestKit.NUnit` is community-maintained, stuck at 1.5.67 and built on NUnit 4.3.2 | Not recommended with Akka |
| MSTest 4.5.2 / MSTest.Sdk | Active, MTP-native, MSTest.Sdk project SDK | `Akka.TestKit.MSTest` is at 1.5.67 | Not recommended with Akka |
| TUnit 1.80.0 | Very active (published 2026-11-04). Source-generated, MTP-only, parallel by default, AOT-friendly. Telemetry on by default since 1.12.x (`TESTINGPLATFORM_TELEMETRY_OPTOUT=1`). | **No Akka TestKit package.** You can build one on `TestKitBase` with a custom `ITestKitAssertions` (UNVERIFIED). | Sidebar only |

Rationale: the Akka TestKit is not framework-neutral in practice. The base class owns `Sys`, `TestActor` and lifecycle, and the official adapters are xUnit-only and released in lockstep with Akka.

---

## 4. Akka.TestKit essentials (APIs verified by reflection on 1.5.73)

`Akka.TestKit.Xunit.TestKit : TestKitBase` implements `IDisposable`, **`IAsyncLifetime`** and **`IAsyncDisposable`**. Its constructors are:

- `(ActorSystem system, ITestOutputHelper output)`
- `(ActorSystemSetup config, string actorSystemName, ITestOutputHelper output)`
- `(Config config, string actorSystemName, ITestOutputHelper output)`
- `(string config, ITestOutputHelper output)`

All parameters are optional and named, so `: TestKit(output: output)` works.

**Async APIs on `TestKitBase`** (use these in every test; the sync forms block a thread-pool thread):
`AwaitAssertAsync, AwaitConditionAsync, AwaitConditionNoThrowAsync, ChildActorOfAsync, ExpectMsgAllOfAsync, ExpectMsgAllOfMatchingPredicatesAsync, ExpectMsgAnyOfAsync, ExpectMsgAsync, ExpectMsgFromAsync, ExpectNoMsgAsync, ExpectTerminatedAsync, FishForMessageAsync, FishUntilMessageAsync, PeekOneAsync, ReceiveNAsync (returns IAsyncEnumerable), ReceiveOneAsync, ReceiveWhileAsync, ShutdownAsync, TryPeekOneAsync, TryReceiveOneAsync, UnwatchAsync, WaitForRadioSilenceAsync, WatchAsync, WithinAsync`.

### Important behaviours and gotchas

1. **A new ActorSystem for every test.** xUnit makes a new class instance per test, and the TestKit constructor and `InitializeAsync` build a fresh `ActorSystem` named `test`. That is isolation for free, but heavy setups such as clustering cost startup time for every test.
2. **Prefer `async Task` tests and `*Async` assertions.** Akka PR #8545 (Sept 2026) traced CI flakiness to blocking `.GetAwaiter().GetResult()` and sync probe calls inside async tests on 2-vCPU agents, which starves the thread pool. The maintainers' rule is that touched tests migrate to the async API. The book should teach only the async forms.
3. **xUnit v3 calls `DisposeAsync()` in preference to `Dispose()`.** If you override `DisposeAsync`, chain to `base.DisposeAsync()`. Otherwise the ActorSystem leaks (Akka issue #8191). On 1.5.73 `DisposeAsync` is not virtual on the TestKit; it becomes virtual in 1.6 (PR #8545).
4. **Config keys** (`akka.test.*` in `Akka.TestKit/Internal/Reference.conf`): `timefactor = 1.0` (scales all `Dilated` timeouts; set 2–3 on CI), `single-expect-default = 3s`, `expect-no-message-default = 3s`, `filter-leeway = 3s`, `default-timeout = 5s`, `startup-timeout = 5s`. Akka 1.5.59 raised the effective TestKit startup timeout default from 10 s to 30 s for Hosting.TestKit. The TestKit also installs `TestEventListener` as the logger, and `EventFilter` needs it.
5. `TestActorRef<T>` and `CallingThreadDispatcher` exist, but the official docs say: "It is highly recommended to stick to traditional behavioral testing … instead of using `TestActorRef` whenever possible." The book's position: test the **pure aggregate** directly (no actor), and test the **actor** only through messages.

### EventFilter (VERIFIED)

```csharp
await EventFilter.Warning(contains: "Cannot check out an empty cart")
    .ExpectOneAsync(async () =>
    {
        cart.Tell(new CheckOut("c2"), probe.Ref);
        (await probe.ExpectMsgAsync<Rejected>(cancellationToken: Ct)).Reason.ShouldContain("empty");
    }, Ct);
```

Other filters: `EventFilter.Error/Info/Debug/Exception<T>()/DeadLetter<T>()/Custom(...)` with `ExpectOneAsync`, `ExpectAsync(n, ...)` and `MuteAsync`. **Gotcha (VERIFIED):** in 1.5.73 a journal *rejection* is logged at **ERROR**, not WARNING ("Rejected to persist event type [...]"). Our first test used `EventFilter.Warning` and timed out.

### Virtual time with TestScheduler (VERIFIED)

```csharp
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
        // barrier: make sure the timer has been scheduled before advancing the clock
        await actor.Ask<ActorIdentity>(new Identify(1), TimeSpan.FromSeconds(3), ct);

        Scheduler.Advance(TimeSpan.FromMinutes(14));
        await probe.ExpectNoMsgAsync(TimeSpan.FromMilliseconds(200), ct);

        Scheduler.Advance(TimeSpan.FromMinutes(1));
        (await probe.ExpectMsgAsync<ReservationExpired>(cancellationToken: ct)).Sku.ShouldBe("APPLE");
    }
}
```

The actor uses `IWithTimers` / `Timers.StartSingleTimer(...)`, and timers go through `Sys.Scheduler`. `TestScheduler` exposes `Advance(TimeSpan)` and `AdvanceTo(DateTimeOffset)`. Gotchas:

- You need a **barrier** (`Identify` / Ask) so the actor has processed the message before you advance time. Otherwise `Advance` runs before the timer exists.
- The TestScheduler affects **everything** that uses the scheduler (Ask timeouts on some paths, Persistence recovery timeouts, cluster gossip), so use it only in focused unit tests and never with clustering.
- For pure domain code, inject `TimeProvider` and use `FakeTimeProvider` (`Microsoft.Extensions.TimeProvider.Testing` 10.11.0) instead.

### AwaitAssert (VERIFIED)

```csharp
IActorRef revived = ActorRefs.Nobody;
await AwaitAssertAsync(() => revived = Sys.ActorOf(Props.Create(() => new CartActor("c3")), "cart-c3"),
    TimeSpan.FromSeconds(3), TimeSpan.FromMilliseconds(50), Ct);
```

This is a typical use: an actor name is not free immediately after `Terminated`, so we retry until it is.

### TestProbe patterns worth teaching

`CreateTestProbe("name")`, `probe.Ref` passed as a collaborator (an "outbound port" in DDD terms), `probe.Reply(...)` and `probe.Forward(...)`, `probe.SetAutoPilot(new DelegateAutoPilot(...))` to fake a downstream bounded context, and `probe.Watch(actor)` + `ExpectTerminatedAsync`. The official docs warn about a CallingThreadDispatcher deadlock risk with probes. Source: `docs/articles/actors/testing-actor-systems.md` in akkadotnet/akka.net.

---

## 5. Testing the aggregate without actors: Given-When-Then with C# unions (VERIFIED)

C# 15 `union` declarations compile on this SDK. Reflection shows `public union CartEvent(ItemAdded, CheckedOut);` lowers to a **value type** with constructors per case and an `object Value` property. `default(CartEvent).Value` is `null`. Implicit conversion from each case type works, and `switch` over the union is exhaustive with no discard arm.

This is the domain model the tests exercise. Decide and evolve are pure functions, and commands and events are unions:

```csharp
public sealed record AddItem(string CartId, Sku Sku, Quantity Qty);
public sealed record CheckOut(string CartId);
public union CartCommand(AddItem, CheckOut);

public sealed record ItemAdded(string CartId, Sku Sku, Quantity Qty);
public sealed record CheckedOut(string CartId, int LineCount);
public union CartEvent(ItemAdded, CheckedOut);

public static class Cart
{
    public const int MaxLines = 10;

    public static IReadOnlyList<CartEvent> Decide(CartState state, CartCommand command) => command switch
    {
        AddItem _ when state.IsCheckedOut => throw new CartRuleViolation("Cart already checked out"),
        AddItem a when !state.Lines.ContainsKey(a.Sku) && state.Lines.Count >= MaxLines
            => throw new CartRuleViolation($"Cart cannot exceed {MaxLines} lines"),
        AddItem a => [new ItemAdded(a.CartId, a.Sku, a.Qty)],
        CheckOut _ when state.IsCheckedOut => [],               // idempotent
        CheckOut _ when state.Lines.Count == 0 => throw new CartRuleViolation("Cannot check out an empty cart"),
        CheckOut c => [new CheckedOut(c.CartId, state.Lines.Count)],
    };

    public static CartState Evolve(CartState state, CartEvent @event) => @event switch
    {
        ItemAdded e => state with { Lines = /* merge qty */ ... },
        CheckedOut => state with { IsCheckedOut = true },
    };
}
```

(The full code is in `testing-spike/unit/Cart.Tests/Domain.cs`. Collection expressions `[new ItemAdded(...)]` convert to `CartEvent` elements implicitly, and this compiled.)

A tiny Given-When-Then harness:

```csharp
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

[Fact]
public void Adding_an_item_to_an_open_cart_emits_ItemAdded()
{
    var events = CartScenario.Given("c1")
        .When(new AddItem("c1", Apple, new Quantity(2)))
        .Then();

    events.ShouldBe([new CartEvent(new ItemAdded("c1", Apple, new Quantity(2)))]);   // Shouldly
    events.Single().Value.Should().BeOfType<ItemAdded>();                            // AwesomeAssertions
}

[Fact]
public void Checking_out_twice_is_idempotent() =>
    CartScenario.Given("c1", new ItemAdded("c1", Apple, new Quantity(1)), new CheckedOut("c1", 1))
        .When(new CheckOut("c1"))
        .Then()
        .ShouldBeEmpty();
```

**Union equality (VERIFIED):** `ShouldBe` over `CartEvent[]` compares correctly. Struct equality on the union delegates to the boxed record's value equality.

**Persisting unions:** the actor persists the **case objects** (`e.Value!`), not the union struct, so journals and serializers see ordinary CLR record types. On recovery, `Recover<ItemAdded>` / `Recover<CheckedOut>` rewrap them. Recommend this pattern to the persistence and serialization chapter writers. The Akka 1.6 `SerializerV2` source generator mentions "union inference" in beta5, so that writer should check how 1.6 treats C# unions (UNVERIFIED).

Literature for GWT on event-sourced aggregates: Greg Young's original "Given events, When command, Then events/exception", and the Decider pattern by Jérémie Chassaing (decide/evolve/initialState). The book can name the Decider explicitly.

---

## 6. Akka.Persistence.TestKit: failure injection (VERIFIED)

Package: **`Akka.Persistence.TestKit.Xunit` 1.5.73** for xUnit v3. The docs page (`docs/articles/persistence/persistence-testing.md`) still says `Akka.Persistence.TestKit.Xunit2`, which is out of date. Its code sample is also buggy ("dec" does `value++`) and has a missing `break`, so don't copy it.

`PersistenceTestKit : TestKit` is pre-configured with `akka.persistence.journal.test` (`TestJournal`, based on MemoryJournal) and `akka.persistence.snapshot-store.test`. Constructors: `(string actorSystemName = null, ITestOutputHelper output = null)` and overloads taking Config, ActorSystemSetup or an ActorSystem.

Scoped behaviour switches (all return `Task` and **must be awaited**). Behaviour resets to `Pass` after the block:

- `WithJournalWrite(Func<JournalWriteBehavior,Task>, Func<Task> | Action)`
- `WithJournalRecovery(Func<JournalRecoveryBehavior,Task>, ...)`
- `WithSnapshotSave / WithSnapshotLoad / WithSnapshotDelete(...)`

Behaviours found by reflection on 1.5.73:

- **JournalWriteBehavior:** `Pass, PassWithDelay, Fail, FailWithDelay, FailIf, FailIfWithDelay, FailUnless, FailUnlessWithDelay, FailOnType, FailOnTypeWithDelay, Reject, RejectWithDelay, RejectIf, RejectIfWithDelay, RejectUnless, RejectUnlessWithDelay, RejectOnType, RejectOnTypeWithDelay, SetInterceptorAsync`
- **JournalRecoveryBehavior:** the Pass/Fail* family plus `SetInterceptorAsync` (there is no Reject on recovery)
- **SnapshotStoreSaveBehavior** (and Load/Delete): the Pass/Fail* family plus `SetInterceptorAsync`

You can also set behaviour directly: `Journal.OnWrite.SetInterceptorAsync(new MyCollectingInterceptor())`, which takes `IJournalInterceptor { Task InterceptAsync(IPersistentRepresentation message); }`. A collecting interceptor is a neat way to assert exactly what got persisted. Snapshot interceptors implement `ISnapshotStoreInterceptor { Task InterceptAsync(string persistenceId, SnapshotSelectionCriteria criteria); }`.

Semantics demonstrated by the spike (all VERIFIED):

```csharp
public sealed class CartPersistenceSpecs(ITestOutputHelper output) : PersistenceTestKit(output: output)
{
    private CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact] // Fail => OnPersistFailure => actor is STOPPED (not restarted)
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

    [Fact] // Reject => OnPersistRejected => logged at ERROR, actor keeps running, handler NOT invoked
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
        var replies = await ReceiveNAsync(2, TimeSpan.FromSeconds(3), Ct).ToListAsync(Ct);
        replies.OfType<CartState>().Last().Lines.ShouldBeEmpty();
    }

    [Fact] // recovery failure => actor stopped
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
```

Teaching points for the book:

- **Persist failure means stop.** That is why aggregates should live under a parent with `BackoffSupervisor`, or under sharding, which restarts on the next message.
- Rejection usually means a serialization or validation problem. The actor must handle it (override `OnPersistRejected`) and reply to the caller. Our naive actor's `DeferAsync` still fired after the rejection. That is a real design smell the book can use as a teaching moment.
- After `Fail`, a `DeferAsync` reply ends up in **dead letters**, which you can see in the logs.

---

## 7. Akka.Hosting.TestKit + cluster sharding locally (VERIFIED)

`Akka.Hosting.TestKit` 1.5.73 (xUnit v3) gives you a real `IHost` with DI, logging, `ActorRegistry` and `IRequiredActor<T>`, and it starts in `InitializeAsync`. Members found in the source (`src/Akka.Hosting.TestKit/TestKit.Shared.cs`):

- Abstract `ConfigureAkka(AkkaConfigurationBuilder, IServiceProvider)`
- Virtual `ConfigureServices`, `ConfigureLogging`, `ConfigureHostConfiguration`, `ConfigureAppConfiguration`, `ConfigureHostBuilder`, `BeforeTestStart()` and `AfterAllAsync()`
- `Host` and `ActorRegistry` properties
- A constructor `(string? actorSystemName = null, ITestOutputHelper? output = null, TimeSpan? startupTimeout = null /*30s*/, LogLevel logLevel = Information)`

**`Akka.Hosting.TestKit.PersistenceTestKit`** layers `TestJournal` and `TestSnapshotStore` on top and exposes the same `WithJournalWrite` / ... methods. Call `base.ConfigureAkka(...)` in your override.

A single-node cluster running real sharding inside a unit test (passes in about 1 s):

```csharp
using HostingPersistenceTestKit = Akka.Hosting.TestKit.PersistenceTestKit;

public sealed class CartRegionKey;   // registry key type

public sealed class ShardedCartSpecs(ITestOutputHelper output) : HostingPersistenceTestKit(output: output)
{
    protected override void ConfigureAkka(AkkaConfigurationBuilder builder, IServiceProvider provider)
    {
        base.ConfigureAkka(builder, provider);     // wires TestJournal + TestSnapshotStore
        builder
            .WithRemoting("127.0.0.1", 0)          // port 0 => parallel-safe
            .WithClustering()
            .WithShardRegion<CartRegionKey>("cart",
                entityId => Props.Create(() => new CartActor(entityId)),
                new CartMessageExtractor(),
                new ShardOptions { StateStoreMode = StateStoreMode.DData, RememberEntities = false })
            .AddStartup((system, _) =>
            {
                var cluster = Cluster.Get(system);
                cluster.Join(cluster.SelfAddress);   // single-node cluster
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
        AddItem m => m.CartId, CheckOut m => m.CartId, GetCart m => m.CartId, _ => null
    };
}
```

Packages: `Akka.Hosting.TestKit` and `Akka.Cluster.Hosting` (which brings Akka.Cluster.Sharding and Akka.Remote.Hosting), both 1.5.73. Name clash: `Akka.TestKit.Xunit.TestKit` and `Akka.Hosting.TestKit.TestKit` are both called `TestKit`, and the two `PersistenceTestKit`s also share a name. Use `using` aliases as above.

Akka.Hosting release-note fixes relevant to parallel xUnit v3 runs:

- 1.5.68 (May 2026) fixed an implicit-sender leak under parallel xUnit v3 execution, a SynchronizationContext leak and a TestActor startup race.
- 1.5.59 fixed a startup race in `InitializeAsync()` that could crash the test host.

**Use ≥1.5.68.** Source: https://github.com/akkadotnet/Akka.Hosting/blob/dev/RELEASE_NOTES.md.

**MultiNode (real multi-process cluster tests):** use `Akka.MultiNode.TestAdapter` 1.5.73 (xUnit-v3-based) with `Akka.Cluster.TestKit`. You define a `MultiNodeConfig` with `RoleName`s, `CommonConfig` and `NodeConfig`, then write a `MultiNodeClusterSpec`. Barriers (`EnterBarrier`) and `TestConductor` let you blackhole or throttle links. **Breaking in 1.6:** throttle and blackhole require `TestTransport = true` (PR #8378). Docs: `docs/articles/testing/multi-node-testing.md`. Status: it works, but the setup is heavy and mostly used by Akka itself. **UNVERIFIED locally.** Recommend it as an "advanced / further reading" box. For the book's sample, use the single-node Hosting.TestKit sharding test above, plus an Aspire run of 2–3 nodes for system tests.

---

## 8. Assertion libraries and snapshot testing

**FluentAssertions licensing.** FluentAssertions v8 (Jan 2025) moved to a proprietary Xceed license, priced at $130 per developer per year for commercial use. 7.x stays Apache-2.0. Current FA is 8.11.0.
Sources: https://www.infoq.com/news/2025/01/fluent-assertions-v8-license, https://www.devclass.com/development/2025/01/16/another-open-source-project-shifts-to-restrictive-license-fluent-assertions-following-xceed-partnership/1621343

**AwesomeAssertions 9.6.0** is the community Apache-2.0 fork. It keeps the `FluentAssertions` namespaces for API compatibility, so a package swap is usually the whole migration. However, the spike's `using AwesomeAssertions;` compiled, so **9.x uses an `AwesomeAssertions` namespace**. Writers should check against the package before claiming namespace parity.

**Shouldly 4.3.0** is MIT and simple (`x.ShouldBe(y)`, `Should.Throw<T>(...)`, `ShouldContain`). Both VERIFIED working on net11.0 side by side. **Recommendation for the book: Shouldly.** It is smallest, has no licensing questions, and reads well in print. Mention AwesomeAssertions as a drop-in for teams coming from FA.

**Verify (snapshot testing) 33.3.4, with a big 2026 gotcha (VERIFIED).** Verify versions released after 2026-09-01 carry a **maintenance fee**, and from v33 a **SponsorCheck** runs at build time. A project referencing `Verify.XunitV3` 33.x **fails to build** with `SC021: No license specified` until one of these is declared:

```xml
<!-- Directory.Build.props: this is a non-revenue open-source sample -->
<Project>
  <PropertyGroup>
    <Verify_SponsorshipExemption>OpenSource</Verify_SponsorshipExemption>
    <Verify_SponsorshipExemptionUntil>2027-10</Verify_SponsorshipExemptionUntil>  <!-- max 12 months out -->
  </PropertyGroup>
</Project>
```

The other options are:

- `Verify_GitHubSponsorAccount`, for paying sponsors ($5–$100/month by organization size)
- `SmallRevenue` (under US$10k a year), or `MaintainerConsulting` (6 months)
- `Verify_SponsorshipLicensedUntil`
- `Verify_SponsorshipLicenseIgnored=true`, which emits warning SC023 on every build and so breaks `-warnaserror`

Exemptions expire, and the build fails again when the end month passes. **Ops chapter note:** this is a time bomb for a book sample repo. Either pin Verify ≤ a pre-Sept-2026 version (UNVERIFIED which one; check the release dates of 31.x/32.x) or document the property and its renewal.
Source: https://github.com/VerifyTests/Verify/blob/main/docs/maintenance-fee.md

Verify also warns when it can't find a `.sln`/`.slnx` within two directories up. Create a `.slnx` (`dotnet new sln --format slnx`). Usage (VERIFIED):

```csharp
[Fact]
public Task Event_stream_shape_is_stable()
{
    var events = CartScenario.Given("c1")
        .When(new AddItem("c1", new Sku("pear"), new Quantity(3)))
        .Then()
        .Select(e => e.Value);   // snapshot the case records, not the union wrapper
    return Verify(events);
}
```

The first run fails and writes `*.received.txt`. Accept it by renaming to `*.verified.txt` and committing it. The produced snapshot:

```
[
  {
    CartId: c1,
    Sku: { Value: PEAR },
    Qty: { Value: 3 }
  }
]
```

Good use in the book: snapshot **event schemas and serialized payloads** to catch accidental breaking changes. This ties directly into the versioning chapter.

---

## 9. Property-based testing for value objects and aggregates (VERIFIED)

**FsCheck 3.6.0 + FsCheck.Xunit.v3 3.6.0** need xunit.v3 4.x. Use the `[Property]` attribute with built-in arbitraries such as `PositiveInt`, `NonEmptyString` and `NonNegativeInt`:

```csharp
[Property]
public bool Quantity_addition_never_exceeds_99(PositiveInt a, PositiveInt b)
{
    var x = (a.Get - 1) % 99 + 1;
    var y = (b.Get - 1) % 99 + 1;
    return (new Quantity(x) + new Quantity(y)).Value <= 99;
}
```

**CsCheck 5.0.1** is C#-native, generator-first, shrinks automatically and works inside a plain `[Fact]`. It is the better fit for **model-based and stateful testing of aggregates**: generate random command sequences and assert invariants.

```csharp
using Gen = CsCheck.Gen;   // REQUIRED if FsCheck is also referenced: FsCheck.Fluent.Gen clashes (CS0104)

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
```

CsCheck also has `SampleModelBased` / `SampleParallel` for linearizability checks (UNVERIFIED here). **Recommendation:** use CsCheck in the book for aggregate invariants, because it is idiomatic C# and has no F# dependency. Mention FsCheck as the alternative.

---

## 10. Integration tests: WebApplicationFactory + Akka.Hosting + Testcontainers PostgreSQL on Podman (VERIFIED)

Setup: a minimal API with `AddAkka(...)`, `WithSqlPersistence(connectionString, ProviderName.PostgreSQL15, autoInitialize: true)` and a `CartManager` child-per-entity parent. The endpoints call `IRequiredActor<CartManager>` and use `Ask`. The program ends with `public partial class Program;` so `WebApplicationFactory<Program>` can see it.

```xml
<!-- Cart.IntegrationTests.csproj -->
<PackageReference Include="xunit.v3" Version="4.0.2" />
<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="11.0.0-rc.3.26522.148" />
<PackageReference Include="Testcontainers.PostgreSql" Version="4.16.0" />
<PackageReference Include="Testcontainers.XunitV3" Version="4.16.0" />
<PackageReference Include="Shouldly" Version="4.3.0" />
<!-- API project: Akka.Hosting 1.5.73, Akka.Persistence.Sql.Hosting 1.5.73, Npgsql 10.0.3 -->
```

```csharp
[assembly: AssemblyFixture(typeof(Cart.IntegrationTests.PostgresFixture))]   // xUnit v3 assembly fixture

public sealed class PostgresFixture(IMessageSink sink)
    : ContainerFixture<PostgreSqlBuilder, PostgreSqlContainer>(sink)     // Testcontainers.Xunit namespace
{
    protected override PostgreSqlBuilder Configure() => new PostgreSqlBuilder("postgres:17-alpine");
}

public sealed class CartApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.UseSetting("ConnectionStrings:carts", connectionString);
}

public sealed class CartApiSpecs(PostgresFixture pg)   // assembly fixtures inject via ctor
{
    private CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Cart_state_survives_an_application_restart()
    {
        var id = Guid.NewGuid().ToString("N");
        await using (var first = new CartApiFactory(pg.Container.GetConnectionString()))
        {
            var resp = await first.CreateClient()
                .PostAsJsonAsync($"/carts/{id}/items", new { sku = "apple", qty = 2 }, Ct);
            resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        }
        // brand-new host + ActorSystem; state must come back from PostgreSQL via recovery
        await using var second = new CartApiFactory(pg.Container.GetConnectionString());
        var cart = await second.CreateClient().GetFromJsonAsync<CartDto>($"/carts/{id}", Ct);
        cart!.Lines["APPLE"].ShouldBe(2);
    }
}
```

Notes:

- The `PostgreSqlBuilder("image")` constructor that takes an image is the current API. Pin the image tag; don't use `latest`.
- Each `WebApplicationFactory` gets its own `ActorSystem`. Disposing it runs CoordinatedShutdown. Persisting in one factory and recovering in a second is a great "event sourcing actually works" test.
- Use unique entity IDs (GUIDs) per test because the container is shared across the assembly.
- `ContainerTest<,>` gives a container per test; `ContainerFixture<,>` gives one per class or per assembly.

### Podman specifics (VERIFIED on Fedora 44, Podman 5.8.9, rootless)

- The only thing required was `DOCKER_HOST=unix:///run/user/1000/podman/podman.sock`, with `systemctl --user enable --now podman.socket`. This machine already sets it in `~/.config/environment.d/podman-docker.conf` (`DOCKER_HOST=unix://${XDG_RUNTIME_DIR}/podman/podman.sock`) and in `~/.bashrc`.
- **Ryuk worked rootless.** The `testcontainers/ryuk` image was pulled, and the test container was reaped after the run. `TESTCONTAINERS_RYUK_DISABLED=true` was **not** needed here.
- The usual fallbacks for readers whose Ryuk fails (SELinux denials mounting the socket, or rootless without a privileged option) are `TESTCONTAINERS_RYUK_DISABLED=true` (then containers are only cleaned up on dispose), or `TESTCONTAINERS_RYUK_CONTAINER_PRIVILEGED=true` plus `TESTCONTAINERS_DOCKER_SOCKET_OVERRIDE=/run/user/$UID/podman/podman.sock`. These env vars exist per https://dotnet.testcontainers.org/custom_configuration/, but the docs page says **nothing** about Podman. The fallback advice is community knowledge and is UNVERIFIED here.
- macOS / Windows Podman Desktop: Podman machine exposes a Docker-compatible socket ("Docker compatibility" toggle). UNVERIFIED.

---

## 11. Aspire testing (VERIFIED, with caveats)

The AppHost (`Aspire.AppHost.Sdk/13.6.4`, net11.0) wires up Postgres and the API:

```csharp
var builder = DistributedApplication.CreateBuilder(args);
var db = builder.AddPostgres("pg").AddDatabase("carts");
builder.AddProject<Projects.Cart_Api>("cart-api").WithReference(db).WaitFor(db);
builder.Build().Run();
```

The test project uses `xunit.v3` 4.0.2 and `Aspire.Hosting.Testing` 13.6.4, plus a ProjectReference to the AppHost:

```csharp
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;   // KnownResourceStates lives here

[Fact]
public async Task Whole_app_boots_and_accepts_a_command()
{
    var ct = TestContext.Current.CancellationToken;
    var timeout = TimeSpan.FromMinutes(2);

    var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Cart_AppHost>(ct);
    await using var app = await appHost.BuildAsync(ct).WaitAsync(timeout, ct);
    await app.StartAsync(ct).WaitAsync(timeout, ct);
    await app.ResourceNotifications.WaitForResourceAsync("cart-api", KnownResourceStates.Running, ct)
             .WaitAsync(timeout, ct);

    using var http = app.CreateHttpClient("cart-api");
    var resp = await http.PostAsJsonAsync("/carts/a1/items", new { sku = "apple", qty = 1 }, ct);
    Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
}
```

Gotchas hit while verifying:

1. `CreateHttpClient("cart-api")` threw `Endpoint '' for resource 'cart-api' not found` until the API project had a `Properties/launchSettings.json` with an `http` profile, because Aspire derives project endpoints from launch profiles. Alternatively, call `.WithHttpEndpoint()` on the resource (UNVERIFIED).
2. Without `.WaitFor(db)`, you get `database "carts" does not exist`. With it, startup logs still show transient Npgsql health-check failures while Postgres boots, and that is normal.
3. Building the AppHost warns `ASPIRE010 ... AspireUseCliBundle=false`. It is harmless for tests; suppress it or set `AspireUseCliBundle=true`.
4. The Aspire docs' test templates (`dotnet new aspire-xunit`) still use **xUnit v2** (`xunit` 2.9.3 + `Microsoft.NET.Test.Sdk`) and target net10.0. Our hand-written xUnit v3 + MTP project works. Source: https://aspire.dev/testing/write-your-first-test/
5. Prefer `WaitForResourceHealthyAsync` once the API exposes Akka health checks (`Akka.Hosting` health checks plus `MapHealthChecks`). This ties into the instrumentation chapter.

Use Aspire tests sparingly, as a few end-to-end "smoke" tests. Each one takes about 10–15 s because it starts real containers and processes.

---

## 12. Recommended test pyramid for the book

| Layer | What | Tooling | Speed |
|---|---|---|---|
| 1. Domain (most tests) | Value objects, Decide/Evolve, policies, invariants | Plain xUnit v3 + Shouldly; GWT harness; CsCheck properties; Verify for event shapes | microseconds |
| 2. Actor unit | Message protocol, replies, timers, supervision | `Akka.TestKit.Xunit` with TestProbe, EventFilter and TestScheduler | ms |
| 3. Persistence behaviour | Recovery, persist failure/rejection, snapshots | `Akka.Persistence.TestKit.Xunit` | ms |
| 4. Hosted component | DI wiring, ActorRegistry, sharding on one node | `Akka.Hosting.TestKit` (`PersistenceTestKit`) | ~1 s |
| 5. Integration | HTTP + Akka + real Postgres journal; schema and serialization | WebApplicationFactory + Testcontainers.PostgreSql | ~5–10 s per class (container shared) |
| 6. System / smoke | Whole distributed app | Aspire.Hosting.Testing | ~10–15 s each |
| (Advanced) | Network partitions, multi-process clusters | Akka.MultiNode.TestAdapter | slow; optional |

Repo-level settings to put in the sample repo at chapter "unit testing", section 1:

- `global.json` with `"test": { "runner": "Microsoft.Testing.Platform" }`
- `Directory.Build.props` with the Verify exemption, if Verify is used
- `Directory.Packages.props` (central package management) pinning the versions in section 1
- A `.slnx` solution

CI should run `dotnet test --solution X.slnx`, optionally with `--report-trx` (needs the TrxReport extension). It should also set `akka.test.timefactor` higher, either through config or an env-driven HOCON fallback.

---

## 13. Open questions / UNVERIFIED list

- Behaviour of Akka **1.6.0-beta5** TestKit packages on net11.0 (they target net10.0 only). Not tested.
- `Akka.MultiNode.TestAdapter` 1.5.73 with xunit.v3 4.0.2 (its extensibility.core is 3.2.2, and xUnit 4.0 changed runner internals that a *test adapter* may use). **Highest risk of breakage. Test it before putting it in the book.**
- AwesomeAssertions namespace in 9.x vs claims of `FluentAssertions` namespace parity. The spike used `using AwesomeAssertions;` successfully.
- TUnit + Akka via a custom `ITestKitAssertions` adapter.
- Coverage via `Microsoft.Testing.Extensions.CodeCoverage` / `coverlet.MTP` under MTP mode.
- Podman Ryuk fallbacks (`TESTCONTAINERS_RYUK_*`) on systems where Ryuk fails.
- Which Verify version is the last one free of the build-time check (released ≤ 2026-09-01).

## 14. Sources

- Akka.NET testing docs (source of getakka.net; the getakka.net URLs `/articles/testing/testkit.html` and `/articles/persistence/testing.html` returned 404 on 2026-11-04, so I read the markdown in the repo):
  - https://github.com/akkadotnet/akka.net/blob/dev/docs/articles/actors/testing-actor-systems.md
  - https://github.com/akkadotnet/akka.net/blob/dev/docs/articles/persistence/persistence-testing.md
  - https://github.com/akkadotnet/akka.net/blob/dev/docs/articles/testing/multi-node-testing.md
  - https://github.com/akkadotnet/akka.net/blob/dev/src/core/Akka.TestKit/Internal/Reference.conf and `Configs/TestScheduler.conf`
- Akka.NET release notes: https://github.com/akkadotnet/akka.net/blob/dev/RELEASE_NOTES.md (1.5.41 xUnit v3 TestKit; 1.6.0-beta1..5)
- Akka.NET 1.5.41 summary: https://newreleases.io/project/github/akkadotnet/akka.net/release/1.5.41
- PR #8545, TestKit async lifecycle: https://github.com/akkadotnet/akka.net/pull/8545
- Akka.Hosting release notes and TestKit source: https://github.com/akkadotnet/Akka.Hosting/blob/dev/RELEASE_NOTES.md, https://github.com/akkadotnet/Akka.Hosting/tree/dev/src/Akka.Hosting.TestKit
- Akka.Persistence.Sql README: https://github.com/akkadotnet/Akka.Persistence.Sql
- Petabridge bootcamp, Akka.Hosting.TestKit: https://petabridge.com/bootcamp/lessons/unit-1/akka-hosting-testkit/
- dotnet test / MTP: https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-with-dotnet-test
- xUnit v3 4.0.0 release notes: https://xunit.net/releases/v3/4.0.0; release list: https://xunit.net/releases/
- Verify maintenance fee: https://github.com/VerifyTests/Verify/blob/main/docs/maintenance-fee.md
- FluentAssertions license change: https://www.infoq.com/news/2025/01/fluent-assertions-v8-license
- Testcontainers config: https://dotnet.testcontainers.org/custom_configuration/; xUnit integration: https://dotnet.testcontainers.org/test_frameworks/xunit_net/
- Aspire testing: https://aspire.dev/testing/write-your-first-test/
- MTP adoption by frameworks (TUnit etc.): https://devblogs.microsoft.com/dotnet/mtp-adoption-frameworks/
- NuGet flat-container and registration APIs for every version and date in section 1.
