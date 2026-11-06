# Chapter 9. Unit Testing the Domain

A driver reserves connector 1 at the bus depot at 07:52. Traffic is bad, and at 08:10, three minutes after the reservation ran out, she taps *Reserve* again in the driver app. The answer is "nothing changed", so she drives on, sure that the connector is hers. At 08:11 a public driver reserves the same connector, and the charge point, seeing an expired reservation, gives it to him.

None of the 662 tests at `ch08-s07` failed, because none of them asked. They run in seconds and test the decision tables row by row, but in the developers' language: `Decide(With("reserved"), ...)` over a state built by hand. The operations lead who owns the reservation rules cannot check whether her sentence, "a lapsed reservation is gone, whoever asks", is in there, and two test projects have each grown their own helper for checking a decision.

Chapter 7 put the rules in pure functions so that they could be tested as values. This chapter builds the tests that make that pay: a harness that speaks in events, tables the experts can read, properties that hold whatever happens, a clock the test controls, and the habits that make domain tests lie.

> **In this chapter you will:**
>
> - place every kind of Ohmline test on a layer of the pyramid and run the fast loop with xUnit v3 on Microsoft.Testing.Platform;
> - test any decider as Given events, When a command, Then events or a refusal, with a failure message that prints the whole scenario;
> - turn an expert's examples into a `TheoryData` table and a CSV file that the tests read;
> - check an aggregate's invariant after every decision of a thousand random command scripts, and read a shrunk counterexample;
> - test expiry, grace periods and time-of-use boundaries across midnight and daylight-saving changes with a `FakeTimeProvider`;
> - test a translator and every policy as tables, and recognize three DDD test smells.

> **Starting point:** `ch08-s07`, the three deciders, the price calculator and the policies of Chapters 7 and 8.
>
> ```bash
> git checkout ch08-s07
> ```

## 9.1 The test pyramid for a functional core and an actor shell

From Chapter 10 on, every decider lives inside an actor, and it will be tempting to test a reservation rule by sending `ReserveConnector` to a `ChargePointActor` and waiting for the reply. That test would need an actor system, a journal and a timeout to check one row of a switch expression. Before the shell exists, the sample needs a rule for which test goes where.

### The idea: the shape of the test suite follows the shape of the code

Mike Cohn's **test pyramid** ([*Succeeding with Agile*](https://www.informit.com/store/succeeding-with-agile-software-development-using-scrum-9780321579362), 2009, ch. 16 "Quality", section "Automate at Different Levels", pp. 311–313) asks for many fast, isolated tests at the bottom and fewer slow, integrated ones at the top. Khononov ([*Learning Domain-Driven Design*](https://www.oreilly.com/library/view/learning-domain-driven-design/9781098100124/), 2021, ch. 10 "Design Heuristics", section "Testing Strategy") ties the shape to the business-logic pattern of section 4.8: a domain model gets a pyramid because its rules can be tested in isolation, while a transaction script pushes the tests upwards. Ohmline's core is an event-sourced domain model of pure functions, so its pyramid has a wide base: a domain test calls a function with values and compares the values that come back.

Tag `ch09-s01` writes the strategy down in `docs/testing-strategy.md`. Its layer table, condensed:

| Layer | What it proves | Tools | From |
|---|---|---|---|
| Domain | The rules: decisions, evolution, policies, translations, prices | xUnit v3, Shouldly, CsCheck, `FakeTimeProvider` | Chapter 6 |
| Actor | The shell's protocol: replies, timers, supervision | Akka.TestKit.Xunit | Chapter 10 |
| Persistence | Recovery, failed writes, snapshots | Akka.Persistence.TestKit.Xunit | Chapter 10 |
| Hosted | Registration and sharding on one node | Akka.Hosting.TestKit | Chapter 12 |
| Integration | HTTP, Akka.NET and a real PostgreSQL journal | Testcontainers | Chapter 12 |
| System | The whole application, end to end | Aspire.Hosting.Testing | Chapter 16 |

The rows run from the bottom of the pyramid to the top: each layer is slower to run than the one before and has fewer tests. This chapter fills the bottom layer; Chapters 10 to 16 add the others. Multi-process cluster tests with `Akka.MultiNode.TestAdapter` are an advanced topic the sample leaves out. The strategy gives three rules for choosing a layer:

*File: docs/testing-strategy.md @ ch09-s01*

```text
1. **Test a rule where it lives.** A rule in `Decide` is tested by calling `Decide`. Testing it
   through an actor proves the same rule more slowly, and a failure no longer says whether the
   rule or the shell is wrong.
2. **Go one layer up only for what the lower layer cannot show.** An actor test that a rejected
   command writes nothing is worth having; an actor test for every row of a decision table is not.
3. **A bug found in a higher layer gets a test in the lowest layer that can show it**, and the
   higher test stays only if it proves something else as well.
```

### xUnit v3 on Microsoft.Testing.Platform

The test projects use xUnit v3 4.0.2 on Microsoft.Testing.Platform (MTP), the runner that `global.json` selected in section 1.7. Four consequences matter:

- **A test project is a program.** `tests/Directory.Build.props` sets `OutputType` to `Exe`, and xUnit generates `Main`. Top-level statements in a test project become the entry point instead (CS7022), and no test runs.
- **`global.json` selects the runner.** Without its `test` section, `dotnet test` on the .NET 11 SDK refuses xUnit v3 4.x projects.
- **`dotnet test` needs a target**, `--solution` or `--project`; options after `--` go to the test programs, and filters are xUnit's own.
- **`dotnet new xunit` still creates an xUnit v2 project for VSTest.** Run `dotnet new install xunit.v3.templates`, or copy an existing test project.

> **Version note:** These points hold for SDK 11.0.100-rc.3 and xUnit v3 4.0.2 at the time of writing (November 2026). Without the `global.json` setting, the build fails with "[Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10 SDK and later](https://github.com/microsoft/testfx/blob/df9b675dd7b49b6bbd18775d013e05bc5af8a89a/src/Platform/Microsoft.Testing.Platform.MSBuild/buildMultiTargeting/Microsoft.Testing.Platform.MSBuild.targets#L395)", an error defined in Microsoft.Testing.Platform's MSBuild targets.

The fast loop leaves out the two traits that need containers:

```bash
dotnet test --solution Ohmline.slnx -- --filter-not-trait "Category=Integration" \
                                       --filter-not-trait "Category=System"
```

At this tag it runs all 662 tests, because nothing carries either trait yet. A test program in which no test runs fails with exit code 8:

```text
$ dotnet test --project tests/Ohmline.Tariffs.Domain.Tests -- --filter-trait "Category=Integration"
Test run summary: Zero tests ran
  total: 0
...
Test run completed with non-success exit code: 8. No tests ran. ...
```

Over a whole solution, an empty project does not fail the run as long as some test ran elsewhere; for a single project, pass `--ignore-exit-code 8`. Tests that wait, from Chapter 10 on, pass `TestContext.Current.CancellationToken` to every API that takes a token, so that a cancelled run stops at once, and the xUnit analyzers flag calls that do not; domain tests never wait.

### Names and the things not to test

Test names are sentences in glossary words: `Given_<state>_when_<command>_then_<outcome>` for deciders and services, `Whenever_<event>_then_<intents>` for policies, as the lilac stickies of section 4.3 read, and a plain sentence for a property. If a test cannot be named in the language, it is probably testing the implementation. The "What not to test" list rules out the framework, private helpers, the wall clock, and three habits section 9.6 returns to. Every new test starts with one question: which layer is the lowest one that can show this?

> **Try it**
>
> ```bash
> git checkout ch09-s01
> git diff ch08-s07 ch09-s01 --stat
> dotnet test --solution Ohmline.slnx -- --filter-not-trait "Category=Integration" \
>                                        --filter-not-trait "Category=System"
> dotnet test --project tests/Ohmline.Tariffs.Domain.Tests -- --filter-trait "Category=Integration"
> ```
>
> The diff shows `1 file changed, 114 insertions(+)`. The fast loop reports `total: 662`, `failed: 0`; the second run reports `Zero tests ran` and exit code 8.

## 9.2 A Given-When-Then harness for deciders

At `ch09-s01`, `ChargePointDecideTests` writes the state down: `With("reserved")` builds a `Registered` record with a `Reserved` connector inside it. That test knows the aggregate's private shape and must change whenever the shape does, even if no rule changed. Charging and Site Energy also have a `DecisionAssertions` helper each, and a failing one prints little more than "the decision was a rejection".

### The idea: the history is the arrangement

Dan North's behaviour-driven development ("[Introducing BDD](https://dannorth.net/blog/introducing-bdd/#atm-example)", 2006, section "BDD provides a 'ubiquitous language' for analysis") gave tests the sentence shape *Given* a context, *When* something happens, *Then* this outcome. For an event-sourced aggregate each part has an exact meaning: *Given* is the events the aggregate has recorded, *When* is one command, and *Then* is the events it records or the reason it refuses. [Greg Young's event-sourcing tests](https://gist.github.com/gregoryyoung/adcf117cbd809ffe08c365f1043b5c69) used this shape, and [Chassaing's Decider](https://thinkbeforecoding.com/post/2021/12/17/functional-event-sourcing-decider#:~:text=rebuilding%20the%20current%20State) (2021, section "Fold") explains why it fits: the state is a fold of the events, so giving events arranges the aggregate the way recovery will (Chapter 11). Such a test never names a state, and because every decider implements `IDecider` (section 7.6), one harness serves them all.

### The code: `DeciderSpec`

Tag `ch09-s02` adds `tests/Ohmline.Testing`, a class library that test projects share. `tests/Directory.Build.props` makes every project under `tests/` an xUnit test program, so this one takes that back:

```xml
<!-- File: tests/Ohmline.Testing/Ohmline.Testing.csproj @ ch09-s02 -->
  <PropertyGroup>
    <OutputType>Library</OutputType>
    <IsTestProject>false</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Remove="xunit.v3" />
    <Using Remove="Xunit" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../../src/Ohmline.SharedKernel/Ohmline.SharedKernel.csproj" />
  </ItemGroup>
```

It references the shared kernel and no bounded context, and keeps Shouldly from the nested props, so a failure is a `ShouldAssertException` that any test framework reports. The harness:

```csharp
// File: tests/Ohmline.Testing/DeciderSpec.cs @ ch09-s02
public sealed class DeciderSpec<TDecider, TState, TCommand, TEvent, TError>
    where TDecider : IDecider<TState, TCommand, TEvent, TError>
{
    // ...
    /// <summary>The events recorded so far, oldest first; none for a new aggregate.</summary>
    public static DeciderSpec<TDecider, TState, TCommand, TEvent, TError> Given(
        params IEnumerable<TEvent> history) => new([.. history], null);

    /// <summary>When the decision is made. There is no default time.</summary>
    public DeciderSpec<TDecider, TState, TCommand, TEvent, TError> At(DateTimeOffset now) =>
        With(new DecisionContext(now));

    /// <summary>Everything the decider is told about the world: a fake clock's reading.</summary>
    public DeciderSpec<TDecider, TState, TCommand, TEvent, TError> With(
        DecisionContext decisionContext) => new(history, decisionContext);

    public Decision When(TCommand command)
    {
        var decisionContext = context ?? throw new InvalidOperationException(
            "Say when the decision is made: call At(...) or With(...) before When(...).");
        var state = Decider.Fold<TDecider, TState, TCommand, TEvent, TError>(history);

        return new Decision(
            history, command, decisionContext, TDecider.Decide(state, command, decisionContext));
    }
```

`When` folds the history with section 7.6's `Decider.Fold` and calls `Decide`. Between `Given` and `When` sits a step the classic shape lacks: every decision needs a moment, because it stamps its events (section 8.1) and reservations expire relative to it. There is no default time, because a hidden default would let a test pass by accident.

The nested `Decision` holds the answer and the assertions on it:

```csharp
// File: tests/Ohmline.Testing/DeciderSpec.cs @ ch09-s02
        public ImmutableArray<TEvent> Then(params TEvent[] expected)
        {
            if (result is Ok<ImmutableArray<TEvent>> accepted
                && accepted.Value.SequenceEqual(expected))
            {
                return accepted.Value;
            }

            throw Failure(
                "Then it should record", Show.Events(expected), FirstDifference(expected));
        }

        /// <summary>Accepted with no events: the command asked for what is already true.</summary>
        public void ThenNothingChanges() => Then();
        // ...
        public TReason ThenRejects<TReason>()
        {
            if (result is Err<TError> refused && Show.CaseOf(refused.Error) is TReason reason)
            {
                return reason;
            }

            throw Failure("Then it should refuse", $"    with a {typeof(TReason).Name}");
        }
```

`Then` demands exactly these events in this order. `ThenRejects(TError)` compares a whole error, and `ThenRejects<TReason>()` accepts any refusal of one case and returns it, so a test can check one detail. Both compare the case inside the union: the internal `Show.CaseOf` unwraps `IUnion.Value`, recursively, because `SitePowerBudgetError` holds the kernel's `ValidationError` union. A union's own `ToString` prints only the union's name, so `Show` also prints the case for failure messages, with list items and times to the tick.

### Closing the harness over each decider

Each test project closes the five type arguments once with global using aliases (fully qualified, as aliases require):

```csharp
// File: tests/Ohmline.Charging.Domain.Tests/Specs.cs @ ch09-s02
// The Given-When-Then harness, closed over each of Charging's deciders once for every test.
global using ChargePointSpec = Ohmline.Testing.DeciderSpec<
    Ohmline.Charging.Domain.ChargePoints.ChargePoint,
    Ohmline.Charging.Domain.ChargePoints.ChargePointState,
    Ohmline.Charging.Contracts.ChargePoints.ChargePointCommand,
    Ohmline.Charging.Domain.ChargePoints.ChargePointEvent,
    Ohmline.Charging.Domain.ChargePoints.ChargePointError>;
// ...
```

The charge point tests now put connector 1 in a state with the event that puts it there:

```csharp
// File: tests/Ohmline.Charging.Domain.Tests/ChargePoints/ChargePointDecideTests.cs @ ch09-s02
    [Fact]
    public void Given_someone_elses_reservation_when_reserved_then_the_driver_learns_until_when()
    {
        Given("reserved")
            .When(new ReserveConnector(Command, Charger, 1, SomeoneElsesCard))
            .ThenRejects<ChargePointError.ReservedForAnotherToken>()
            .Until.ShouldBe(Expiry);
    }
    // ...
    /// <summary>A registered charge point whose connector 1 is in the named state.</summary>
    private static ChargePointSpec Given(string connectorState)
    {
        ChargePointEvent[] connector = connectorState switch
        {
            "available" => [],
            "reserved" => [new ConnectorReserved(Charger, 1, DriversCard, Expiry)],
            "occupied" => [new ConnectorOccupied(Charger, 1, Session, DriversCard)],
            "faulted" => [new ConnectorFaulted(Charger, 1, "GroundFailure")],
            "unavailable" => [new ConnectorMadeUnavailable(Charger, 1)],
            _ => throw new ArgumentOutOfRangeException(nameof(connectorState)),
        };

        return Given([Registration, .. connector]);
    }
```

A second private `Given` adds `.At(Morning)`, so a test states its moment only when it matters. A failure prints the whole scenario; here one test was changed on purpose to expect an occupation:

```text
Shouldly.ShouldAssertException :
Given
    ChargePointRegistered { ChargePointId = OHM-SLC-0042, CommandId = 00000000-..., ... }
    ConnectorReserved { ..., TokenUid = 04A2B3C4D5E6F7, ExpiresAt = 2026-11-04T08:07:00... }
When, at 2026-11-04T07:52:00.0000000+00:00
    OccupyConnector { CommandId = 01a1a58b-..., ..., TokenUid = 04FFEEDDCC }
Then it should record
    ConnectorOccupied { ..., OccurredAt = 2026-11-04T07:52:00.0000000+00:00, ... }
But it refused
    ReservedForAnotherToken { ConnectorNumber = 1, Until = 2026-11-04T08:07:00.0000000+00:00 }
```

The given events carry a zero `CommandId` and the year 1 as `OccurredAt`, because the charge point's `Evolve` never reads the stamp. Expected events must be stamped, with `.At(Morning)` from section 8.1's `Stamps.cs`, because the stamp is part of the fact. The harness cannot stamp for the test: the stamp lives on each context's own event base record, and the shared kernel has no common interface for it.

Site Energy's private `Given` takes commands instead and decides them into a `Journal`, because writing `AllocationsRebalanced` tables by hand would repeat the allocator's arithmetic in the test. Its `Then`s are still exact events: four cars charging, a fifth request, and an allocation rebalance that gives all five 38 kW of the depot's 190 kW.

### Consequences

`DecisionAssertions` is gone. Tests of `Evolve` and `Fold` still assert state, because state is what they produce. The harness is code other tests trust, so `DeciderSpecTests` tests it on Chapter 7's `EnergyTally` and pins the whole failure message. Its price is one more project and a generic signature that only the aliases make readable.

> **Try it**
>
> ```bash
> git checkout ch09-s02
> git diff ch09-s01 ch09-s02 --stat
> git diff ch09-s01 ch09-s02 -- tests/Ohmline.Charging.Domain.Tests/ChargePoints
> dotnet test --solution Ohmline.slnx
> ```
>
> The diff shows `15 files changed, 679 insertions(+), 297 deletions(-)`. The test run reports `total: 668`, `failed: 0`; the six new tests are in `DeciderSpecTests`.

## 9.3 Specification by example: scenario tables in the ubiquitous language

The operations lead explains reservations with examples: "Someone else asks at 08:06, they're told it's held until 08:07. They ask at 08:07, it's theirs until 08:22." Each sentence is a test, but `ChargePointDecideTests` scatters her rules among its 37 test methods, and Chapter 7's Exercise 1 left one open: is "nothing changes" right for a driver who reserves again after their own reservation lapsed?

### The idea: the expert's examples are the specification

Gojko Adzic calls the practice **specification by example** ([*Specification by Example*](https://www.manning.com/books/specification-by-example), 2011; ch. 3 "Living documentation"): the team and the experts agree on concrete examples, which, automated, become both the tests and the documentation. Ward Cunningham's [FIT framework](http://fit.c2.com/wiki.cgi?IntroductionToFit) (2002) ran such examples as tables that business people wrote. A table reads across, one row per situation, in the glossary's words (section 4.5), and it makes gaps visible: a long list of methods hides a missing case, while a grid invites the question about the row that is not there.

### The code: a reservation table

Tag `ch09-s03` adds `ReservationScenarioTests`. Its rows are strings, aligned as a table:

```csharp
// File: tests/Ohmline.Charging.Domain.Tests/ChargePoints/ReservationScenarioTests.cs @ ch09-s03
    public static readonly TheoryData<string, string, string> Reservations = new()
    {
        // Connector      What happens                Outcome
        { "available",   "driver reserves at 07:52", "reserved for driver until 08:07" },
        { "reserved",    "driver reserves at 07:55", "nothing changes" },
        { "reserved",    "other reserves at 07:55",  "refused: reserved until 08:07" },
        { "reserved",    "other reserves at 08:06",  "refused: reserved until 08:07" },
        { "reserved",    "other reserves at 08:07",  "expired; reserved for other until 08:22" },
        { "reserved",    "driver reserves at 08:10", "expired; reserved for driver until 08:25" },
        // ...
        { "reserved",    "timer fires at 08:06",     "nothing changes" },
        { "reserved",    "timer fires at 08:07",     "expired" },
        // ...
        { "reserved",    "driver plugs in at 08:10", "occupied by driver" },
        { "reserved",    "other plugs in at 08:00",  "refused: reserved until 08:07" },
        { "reserved",    "other plugs in at 08:10",  "expired; occupied by other" },
        // ...
    };

    [Theory]
    [MemberData(nameof(Reservations))]
    public void A_reservation_holds_a_connector_for_one_token_until_it_expires(
        string connector, string happening, string outcome)
    {
        var (command, at) = Happening(happening);

        var decision = ChargePointSpec.Given(History(connector)).At(at).When(command);

        Describe(decision.Result).ShouldBe(outcome);
    }
```

Private helpers translate both ways: `History` gives the events behind each connector state, `Happening` parses "other plugs in at 08:10" into a command and a moment, and `Describe` turns the decision into the table's words, using the `Result` property that `DeciderSpec.Decision` gains at this tag.

### The table finds a rule in the wrong order

The row "driver reserves at 08:10" is the operations lead's answer to the open question: a lapsed reservation is gone, and asking again starts a new one. Its first run failed:

```text
failed ...A_reservation_holds_a_connector_for_one_token_until_it_expires(...)
  Shouldly.ShouldAssertException : Describe(decision.Result)
      should be
  "expired; reserved for driver until 08:25"
      but was
  "nothing changes"
```

This is the bug from the chapter's opening. Since `ch07-s04` the decision table checked "same token" before "expired", so the holder of a lapsed reservation got `NoChange` and the lapsed reservation stayed, ready to be taken by the next driver who asked. Both rows match the same patterns and only their `when` guards differ, so the compiler cannot help: only an example finds a row in the wrong place. The fix swaps them:

```diff
# git diff ch09-s02 ch09-s03 -- src/Charging/Ohmline.Charging.Domain/ChargePoints/ChargePoint.cs
@@ -57,10 +57,10 @@ public sealed class ChargePoint
         (state, command) switch
         {
             (Available, ReserveConnector reserve) => Accept(Reservation(reserve, context)),
-            (Reserved held, ReserveConnector reserve) when held.TokenUid == reserve.TokenUid =>
-                NoChange,
             (Reserved held, ReserveConnector reserve) when held.HasExpiredAt(context.Now) =>
                 Accept(Expiry(reserve, held), Reservation(reserve, context)),
+            (Reserved held, ReserveConnector reserve) when held.TokenUid == reserve.TokenUid =>
+                NoChange,
             (Reserved held, ReserveConnector) => Reject(HeldForAnother(command, held)),
             (Occupied or Unavailable, ReserveConnector) => Reject(Unusable(command)),
             (Faulted fault, ReserveConnector) => Reject(Broken(command, fault)),
```

Section 7.4's listing shows the old order. For plugging in, the table records the opposite decision: a driver whose reservation lapsed may still plug in while nobody else has taken the connector. A second table, `SessionStopScenarioTests`, states in seven rows that a session ends once, with the energy it delivered, whatever it was doing.

### The pricing analyst's file

The pricing analyst's twelve examples stay in her spreadsheet's format, a CSV file beside the tests:

*File: tests/Ohmline.Tariffs.Domain.Tests/pricing-examples.csv @ ch09-s03*

```text
example,tariff,plugged_in,charging_stopped,energy_kwh,unplugged,price_usd,breakdown
...
Unplugged within the grace period,standard,09:00,09:30,10,09:44,5.20,energy 4.20; session fee 1.00
Unplugged as the grace period ends,standard,09:00,09:30,10,09:45,5.20,energy 4.20; session fee 1.00
...
Fractions of a cent,standard,09:00,09:20,7.123,09:20,3.99,energy 2.99; session fee 1.00
...
A charge inside the peak,peak,17:00,18:00,30,18:10,19.00,energy (peak) 18.00; session fee 1.00
...
```

Times are the depot's wall clock on 2026-11-04; a time earlier than the one before it is on the next day. The test project copies the file to its output, and `PriceCalculatorExampleTests` reads it:

```csharp
// File: tests/Ohmline.Tariffs.Domain.Tests/Pricing/PriceCalculatorExampleTests.cs @ ch09-s03
    public static TheoryData<string> Names => new(Examples.Select(example => example.Name));

    [Theory]
    [MemberData(nameof(Names))]
    public void Given_an_example_from_the_pricing_analyst_then_the_calculator_agrees(string name)
    {
        var example = Examples.Single(example => example.Name == name);

        var price = PriceCalculator.Price(Tariffs[example.Tariff], example.Usage);

        Describe(price).ShouldBe(example.Breakdown);
        price.Total.ShouldBe(Usd(example.PriceUsd));
    }
```

The theory's data is the example's name, so the runner lists "Fractions of a cent" as its own test, and the calculator must agree with the analyst line by line, not only on the total. A second test checks that every tariff the file names is defined. Daylight-saving nights are left to section 9.5.

### Consequences

String tables trade compile-time checking for readability: a typo in "driver resrves" fails the test, not the build, and the parsers need maintaining. In return an expert can review the table in a pull request, and each serializable `TheoryData` row reports as its own test case. The CSV costs a parser and earns a file the analyst extends without touching C#.

> **Try it**
>
> ```bash
> git checkout ch09-s03
> git diff ch09-s02 ch09-s03 --stat
> git diff ch09-s02 ch09-s03 -- src
> dotnet test --solution Ohmline.slnx
> ```
>
> The diff shows `9 files changed, 380 insertions(+), 3 deletions(-)`, and the `src` diff is the four-line swap above. The full run reports `total: 708`, `failed: 0`: 19 reservation rows, 7 stop rows, 12 pricing examples and two new facts.

## 9.4 Model-based property tests for invariants

Section 7.8 ended with a test that runs one busy morning at the depot, seventeen commands, and checks after each decision that the allocations fit. That morning is one ordering out of an astronomical number, and the allocator's bugs live in the orderings nobody writes: a curtailment between a release and a request, a latecomer asking for a held connector, a request for 0 kW.

### The idea: generate the history, check the rule after every step

Section 6.8 used CsCheck 5.0.1 to check laws of values. The idea scales to aggregates: a generator writes a random **script** of commands, the test runs it through `Decide` and `Evolve` as the shell will, and checks a property after every decision. This is the stateful, or model-based, style that John Hughes and colleagues developed with QuickCheck, which they [extended "with state machine formalisms"](https://www.cs.utexas.edu/~hunt/FMCAD/fmcad11/papers/inv8.pdf) (Hughes, "Specification Based Testing with QuickCheck", FMCAD 2011). In its full form a small model predicts every outcome; here the model is lighter, statements true of every state, which is what an aggregate's invariants are. When a script breaks a property, the library **shrinks** it, removing and simplifying commands while it still fails, until a short script remains that a person can read.

Four properties apply to every decider:

- **Evolve is total**: it accepts every event `Decide` produces, in every state a script reaches (section 7.5).
- **Decide never records an event that Evolve ignores**: an event that changes nothing is either a fact kept only for the journal or a bug in `Evolve`.
- **The invariant holds** after every decision.
- **A measure is monotonic**: the energy a session has delivered never goes down.

The third property needs the invariant written as code. Evans's supple-design pattern **Assertions** reads, in the [*DDD Reference*](https://domainlanguage.com/wp-content/uploads/2016/05/DDD_Reference_2015-03.pdf) (2015, Part III "Supple Design", "Assertions", p. 22): "State post-conditions of operations and invariants of classes and aggregates. If assertions cannot be coded directly in your programming language, write automated unit tests for them." Ohmline does both: the invariant becomes a named member of the state, and the property tests evaluate it.

### The code: the shell's loop without the actor

Tag `ch09-s04` adds `DeciderProperties` to `Ohmline.Testing`, which now references CsCheck too. Its heart is `Run`, the actor shell's loop of Chapter 10 without the actor:

```csharp
// File: tests/Ohmline.Testing/DeciderProperties.cs @ ch09-s04
    public static ImmutableArray<Step> Run(DateTimeOffset start, IEnumerable<Timed> script)
    {
        var steps = ImmutableArray.CreateBuilder<Step>();
        var state = TDecider.Initial;
        var now = start;

        foreach (var (wait, command) in script)
        {
            now += wait;
            var decision = TDecider.Decide(state, command, new DecisionContext(now));
            var after = decision is Ok<ImmutableArray<TEvent>> accepted
                ? Decider.Fold<TDecider, TState, TCommand, TEvent, TError>(state, accepted.Value)
                : state;

            steps.Add(new Step(state, command, now, decision, after));
            state = after;
        }

        return steps.ToImmutable();
    }
```

Each `Timed` pairs a command with how long the clock runs before it, so reservations can expire mid-script. Every property builds on one method that samples scripts and names the failing decision:

```csharp
// File: tests/Ohmline.Testing/DeciderProperties.cs @ ch09-s04
    private const long ScriptsPerProperty = 1_000;
    // ...
    public static void AfterEveryDecision(
        Gen<ImmutableArray<Timed>> scripts, DateTimeOffset start, Action<Step> property) =>
        scripts.Sample(
            script =>
            {
                var steps = Run(start, script);
                foreach (var (index, step) in steps.Index())
                {
                    try
                    {
                        property(step);
                    }
                    catch (Exception broken)
                    {
                        throw new ShouldAssertException(
                            $"After decision {index + 1} of {steps.Length}, "
                            + $"{Show.Value(step.Command)}: {broken.Message}",
                            broken);
                    }
                }
            },
            iter: ScriptsPerProperty,
            print: Print);
```

`EvolveIsTotal` passes a property that does nothing, because running the script is the test. `EveryEventChangesTheState` fails if `Equals(next, state)`, which works on the state unions because a union compares the case it holds. `Invariant` takes a predicate and a sentence to print, and `Monotonic` takes any measure with comparison operators, such as `Energy` (section 3.6).

CsCheck keeps sampling while it shrinks, so more scripts give a shorter counterexample: with its [default of 100 iterations](https://github.com/AnthonyLloyd/CsCheck#:~:text=The%20default%20sample%20size%20is%20100%20iterations) a planted bug shrank to 15 decisions, with 1,000 to 5. Hence `ScriptsPerProperty`.

### The invariant as an assertion

The only production change in this section is the budget's invariant, stated once on the `Configured` state:

```csharp
// File: src/SiteEnergy/Ohmline.SiteEnergy.Domain/Budgets/SitePowerBudgetState.cs @ ch09-s04
    /// <summary>
    /// The budget's invariant, stated once as an assertion: the allocations add up to no more
    /// than the effective capacity, and each belongs to one open request and lies between its
    /// connector's minimum and what the request may draw. Every decision keeps it true, and the
    /// property tests check it after every decision of many random scripts (section 9.4).
    /// </summary>
    public bool AllocationsFit =>
        Allocated <= EffectiveCapacity
        && Allocations.Select(allocation => allocation.ConnectorId).Distinct().Count()
            == Allocations.Length
        && Allocations.All(allocation =>
            AllocationRequests.SingleOrDefault(
                    request => request.ConnectorId == allocation.ConnectorId) is { } request
            && allocation.Power >= request.MinPower
            && allocation.Power <= request.MaxPower);
```

The property test's `Commands` generator picks requests of 0 to 60 kW, releases, a latecomer's request for a held connector, curtailments of 0 to 200 kW and their end. Scripts configure the depot with up to six fleet connectors first:

```csharp
// File: tests/Ohmline.SiteEnergy.Domain.Tests/Budgets/SitePowerBudgetProperties.cs @ ch09-s04
    /// <summary>The depot, with 0 to 6 fleet connectors, then 60 commands at most.</summary>
    private static readonly Gen<ImmutableArray<SitePowerBudgetChecks.Timed>> Scripts =
        Gen.Int[0, 6].SelectMany(fleet => SitePowerBudgetChecks.Scripts(
            Commands, Gen.Const(TimeSpan.FromMinutes(1)), 60, Configure(fleet)));

    [Fact]
    public void Allocations_never_exceed_the_effective_capacity()
    {
        SitePowerBudgetChecks.Invariant(
            Scripts,
            Morning,
            "The allocations fit the capacity and the connectors",
            state => state.Value is not Configured site || site.AllocationsFit);
    }
```

`SitePowerBudgetChecks` is another alias in `Specs.cs`. To see a failure, `Evolve` was made to ignore `AllocationsRebalanced`, and CsCheck shrank a failing script to five commands:

```text
CsCheck.CsCheckException : Set seed: "bxNWyR17oznb" ... (7 shrinks, 946 skipped, 1,000 total).

    after 0:00:00: ConfigureSite { ... }
    after 0:01:00: ReleaseAllocation { ... ConnectorId = OHM-SLC-0042/1, ... }
    after 0:01:00: ApplyCurtailment { ... Reduction = 159 kW }
    after 0:01:00: RequestAllocation { ... ConnectorId = OHM-SLC-0042/1, ... Requested = 16.176 kW }
    after 0:01:00: RequestAllocation { ... ConnectorId = OHM-SLC-0042/2, ... Requested = 58.974 kW }
---- Shouldly.ShouldAssertException : After decision 5 of 5, RequestAllocation { ... }: ...
```

The message ends `AllocationsRebalanced { ... } changed nothing in Configured { ... }`: the second request forced an allocation rebalance under a 159 kW curtailment, and the state never heard of it. CsCheck also prints a seed for replaying the run, which the sample has not tried under MTP.

### Consequences

`ChargingSessionProperties` generates readings that go backwards and arrive late and checks `The_energy_delivered_never_goes_down`; its "every event changes the state" property exempts the journal-only `MeterAnomalyDetected` and `SpendCapReached`. `ChargePointProperties` checks that a reservation never holds a connector longer than `ChargePoint.ReservationHold`.

A property is only as good as its generator: a throwaway probe confirmed that 1,000 scripts reach every Charging event type. An assertion that is always true makes every property pass, so `Given_allocations_that_break_the_rules_then_the_assertion_is_false` feeds `AllocationsFit` four wrong states and one right one. And a second planted bug, rounding shares up, never reached `AllocationsFit`: `Power`'s own guard from section 6.2 threw "Power is never negative" first, because value objects are assertions too. The eight Charging properties take about 2 s, using every core.

> **Try it**
>
> ```bash
> git checkout ch09-s04
> git diff ch09-s03 ch09-s04 --stat
> git diff ch09-s03 ch09-s04 -- src
> dotnet test --solution Ohmline.slnx
> ```
>
> The diff shows `11 files changed, 548 insertions(+), 4 deletions(-)`; the `src` diff is `AllocationsFit`. The test run reports `total: 723`, `failed: 0`, with four new tests in each decider's `...Properties` class and three for `DeciderProperties` itself.

## 9.5 Testing time

A reservation lasts fifteen minutes, an idle fee starts fifteen minutes after the car stops charging, and a night band runs from 22:00 to 06:00 on a wall clock that jumps twice a year. A test that reads `DateTime.UtcNow` passes at 10:00, fails at 23:55, and never reaches the night the clocks go back.

### The idea: the test plays the shell's clock

No decider reads a clock: the shell reads its `TimeProvider` once per command and passes the reading in a `DecisionContext` (section 7.4). A test plays that part with a `FakeTimeProvider` from `Microsoft.Extensions.TimeProvider.Testing`, building each context with `DecisionContext.From(clock)` and moving the clock with `Advance`. The instants worth testing are the boundaries: the last second before a deadline, the deadline, midnight, and the hour that happens twice or never.

### The code: fifteen real minutes

Tag `ch09-s05` adds `ReservationExpiryTests`, whose private `Given` builds the context from the fake clock:

```csharp
// File: tests/Ohmline.Charging.Domain.Tests/ChargePoints/ReservationExpiryTests.cs @ ch09-s05
    [Fact]
    public void Given_a_reservation_as_the_clocks_go_back_then_it_lasts_fifteen_real_minutes()
    {
        // 01:50 MDT on 1 November 2026 at the depot; ten minutes later it is 01:00 MST.
        clock = new FakeTimeProvider(new DateTimeOffset(2026, 11, 1, 7, 50, 0, TimeSpan.Zero));
        var reserved = Reserve(DriversCard);
        var expiry = reserved.OfType<ConnectorReserved>().Single().ExpiresAt;

        var wallClock = SiteLocalTime.Of(expiry, "America/Denver");
        (wallClock.TimeOfDay, wallClock.IsDaylightSavingTime).ShouldBe((new TimeOnly(1, 5), false));

        clock.Advance(TimeSpan.FromMinutes(15));
        Given(reserved)
            .When(new ExpireReservation(Command, Charger, 1))
            .Then(new ReservationExpired(Charger, 1, DriversCard).At(expiry));
    }
    // ...
    private ChargePointSpec Given(params IEnumerable<ChargePointEvent> history) =>
        ChargePointSpec.Given([Registration, .. history]).With(DecisionContext.From(clock));
```

A driver who reserves at 01:50 daylight time holds the connector until 01:05 standard time: earlier on the wall, and exactly fifteen real minutes later. The decider works in instants, and the wall clock is derived from them (section 8.7), so the rule needs no special case. The class's other tests walk the plain boundaries: another driver is refused at 14 minutes 59 seconds and succeeds at 15 minutes, an early timer changes nothing, and a replay a day later keeps the recorded expiry.

The test starts a new clock rather than setting the old one, because `FakeTimeProvider.SetUtcNow` [refuses to go backwards](https://github.com/dotnet/extensions/blob/5635a6ed7499739e2411f8288e1bd2b340c2c0e2/src/Libraries/Microsoft.Extensions.TimeProvider.Testing/FakeTimeProvider.cs#L104) with an `ArgumentOutOfRangeException`: "Cannot go back in time. Current time is 11/4/2026 7:52:00 AM +00:00."

`IdleFeeGraceTests` does the same for sessions: section 8.4's `IdleFeeApplies` is false 14 minutes 59 seconds after the car stops charging and true at 15, never applies when the charge point paused the charge, and agrees to the tick with the moment `IdleFeePolicy`'s warning names.

### Time-of-use boundaries

Pricing has no decider, so `TouBoundaryTests` uses the fake clock to time meter readings from a 60 kW charge point, 1 kWh a minute. A ten-row theory checks single minutes either side of 16:00, 21:00, 22:00, midnight and 06:00, and three facts check whole nights:

```csharp
// File: tests/Ohmline.Tariffs.Domain.Tests/Pricing/TouBoundaryTests.cs @ ch09-s05
    [Fact]
    public void Given_the_night_the_clocks_go_back_then_the_night_band_lasts_nine_real_hours()
    {
        // 21:00 MDT on 31 October to 07:00 MST on 1 November: eleven real hours.
        var price = PriceCalculator.Price(
            DayAndNight,
            Charge(
                new DateTimeOffset(2026, 11, 1, 3, 0, 0, TimeSpan.Zero), TimeSpan.FromHours(11)));

        EnergyIn(price, band: null).ShouldBe(new Energy(120m));
        EnergyIn(price, Night).ShouldBe(new Energy(540m));
    }
```

From 22:00 daylight time to 06:00 standard time is nine real hours, 540 kWh; on the March night the clocks go forward, the same band lasts seven real hours, 420 kWh.

### Consequences

The expected numbers are worked out by hand, not by the code under test. A fixed-instant test cannot find a bug on a date nobody named, which is why the DST nights are explicit, and these tests depend on the time zone data of the machine that runs them.

> **Try it**
>
> ```bash
> git checkout ch09-s05
> git diff ch09-s04 ch09-s05 --stat
> dotnet test --solution Ohmline.slnx
> ```
>
> The diff shows `4 files changed, 344 insertions(+)`. The test run reports `total: 746`, `failed: 0`, among them `Given_the_night_the_clocks_go_forward_then_the_night_band_lasts_seven_real_hours`.

## 9.6 Testing translators and policies; DDD test smells

Two kinds of pure function remain: the AssetTrack translator of section 6.7, an Anticorruption Layer (ACL) that turns a messy legacy CSV row into a site configuration or a rejection, and the policies of section 8.5. Both have many small cases that are easiest to review side by side.

### The idea: a mapping is best tested as a mapping

A translator has no history and no moment: it maps one language to another, so its specification is a dictionary from what the foreign system says to what the model reads. A policy maps an event to intents, so its specification is a grid of every policy against every event. As tables, both put the odd unit, the misspelling and the event a policy must ignore where a missing row shows.

### The code: the ACL as a table

Tag `ch09-s06` adds `AssetTrackTranslationTableTests`. The left column speaks AssetTrack, with its own column names, and the right column speaks Site Energy:

```csharp
// File: tests/Ohmline.SiteEnergy.Acl.AssetRegistry.Tests/AssetTrackTranslationTableTests.cs @ ch09-s06
    public static readonly TheoryData<string, string, string> Values = new()
    {
        // Column            AssetTrack says            Site Energy reads
        { "STATUS",         " active ",                "a connector in use" },
        { "STATUS",         "Retired",                 "skipped by Status" },
        // ...
        { "GRID_CONN",      "0.2MW",                   "200 kW" },
        { "GRID_CONN",      "1,000 kW",                "rejected by PowerValue" },
        // ...
        { "MIN_PWR",        "11000",                   "11 kW" },
        { "MIN_PWR",        "11",                      "rejected by Plausibility" },
        // ...
        { "TZ",             "US/Mountain",             "America/Denver" },
        { "TZ",             "Mountain Standard Time",  "rejected by TimeZone" },
        // ...
        { "PRIO",           "Flet",                    "rejected by PriorityClass" },
    };
```

Each row changes one column of a clean row, so the table reads as the ACL's rules: a bare `11` is 11 W, too small for a connector's minimum; a Windows time zone id is not an IANA one; a misspelt priority is refused, not guessed. All 37 rows passed on their first run, so the translator matches `docs/acl/assettrack.md`.

`PolicyTableTests` crosses every policy with every kind of session event, 24 rows of *Policy*, *Whenever* and *Then*. Most cells say "nothing", which is the point: a policy reacts only to its own event. Such a table goes stale as the model grows, so a guard finds the policies and events by reflection:

```csharp
// File: tests/Ohmline.Charging.Domain.Tests/Policies/PolicyTableTests.cs @ ch09-s06
    [Fact]
    public void Given_the_table_then_it_has_every_policy_and_every_kind_of_session_event()
    {
        var policies = typeof(ISessionPolicy).Assembly.GetTypes()
            .Where(type => type.IsAssignableTo(typeof(ISessionPolicy)) && !type.IsInterface
                           && type != typeof(SessionPolicies));
        var events = typeof(SessionEvent).Assembly.GetTypes()
            .Where(type => type.IsSubclassOf(typeof(SessionEvent)) && !type.IsAbstract);
        var rows = Reactions.Select(row => (Policy: row.Data.Item1, Event: row.Data.Item2))
            .ToList();

        rows.Select(row => Policy(row.Policy).GetType()).Distinct()
            .ShouldBe(policies, ignoreOrder: true);
        rows.Select(row => Event(row.Event).GetType()).Distinct()
            .ShouldBe(events, ignoreOrder: true);
        rows.Count.ShouldBe(policies.Count() * events.Count());
    }
```

In xUnit v3 a `TheoryData` enumerates `TheoryDataRow` values whose `Data` is a tuple, hence `row.Data.Item1`. When a later chapter adds a policy, this guard fails until its row is written.

### Smells

Gerard Meszaros [cataloged **test smells**](http://xunitpatterns.com/Test%20Smells.html) in [*xUnit Test Patterns*](https://www.informit.com/store/xunit-test-patterns-refactoring-test-code-9780131495050) (2007, Part II "The Test Smells", chapters 15 to 17): tests that break when nothing is wrong, his [Fragile Test](http://xunitpatterns.com/Fragile%20Test.html) (ch. 16 "Behavior Smells", p. 239), or let bugs through, his [Production Bugs](http://xunitpatterns.com/Production%20Bugs.html) (ch. 17 "Project Smells", p. 268). The new `tests/README.md` lists three that a functional core in an actor shell invites:

- **Asserting state instead of events.** A test that checks a field of the folded state passes when `Decide` records the wrong event but `Evolve` lands in the same place, and breaks when the state is refactored. Assert events with `Then`; assert state only in tests of `Evolve`.
- **Mocking a repository that does not exist.** An `IChargePointRepository` mock tests a design Ohmline does not have: an aggregate is the fold of its events, and the shard region of Chapter 12 plays the repository's role. Give the history as events.
- **Testing through actors what belongs in the core.** An actor test per decision-table row needs an actor system and a timeout to check a pure function, and its failure does not say whether the rule or the shell is wrong.

Four smaller ones follow: an expected value computed with the production formula (write `16.59`, not `27.6m * 0.42m + ...`), the wall clock, names in the code's language, and a property that cannot fail.

Two of the three larger smells arrive with the shell: once Chapters 10 and 12 put deciders in actors behind a shard region, a mock and an actor test will both look natural, and those chapters' tests cover only the protocol around a rule. The takeaway for every new test: name it in the glossary's words, put it on the lowest layer that can show the rule, and expect a value an expert wrote down.

> **Try it**
>
> ```bash
> git checkout ch09-s06
> git diff ch09-s05 ch09-s06 --stat
> dotnet test --solution Ohmline.slnx
> ```
>
> The diff shows `6 files changed, 319 insertions(+), 1 deletion(-)`. The test run reports `total: 809`, `failed: 0`: 37 translation rows and 24 policy rows, each with its guard.

## Summary

- A functional core puts most tests at the bottom of the pyramid: rules are tested where they live, as function calls on values, and each layer above tests only what the layer below cannot show.
- A decider test gives a history of events, one command and a moment, and expects events or a refusal; it never writes a state down, so it survives any refactoring that keeps the events.
- An expert's examples, kept as a table or a data file in the glossary's words, are tests the expert can review, and they find gaps that method-per-case tests hide, such as two guarded rows in the wrong order.
- An invariant written once as an assertion on the state can be checked after every decision of a thousand random scripts; shrinking turns a failure into a short story, and the assertion needs a test that makes it false.
- Time is an input: a fake clock read once per command reaches every boundary, including the hour that happens twice, and the expected numbers are worked out by hand.
- Translators and policies read best as tables, guarded by tests that fail when the model grows a case the table lacks.

## DDD canon covered

Ubiquitous Language; Invariants; Supple design: Assertions; Supple design: Side-Effect-Free Functions; Repositories; ACL; Making implicit concepts explicit (constraints, processes, policies/strategy); Functional core / actor shell.

## Exercises

1. **Reflect.** `ReservationScenarioTests` writes its rows as strings and parses them, while `ChargePointDecideTests` builds commands and events in C#. Weigh who can review each, what a typo costs, how much parsing code each needs, and which you would hand to the operations lead.
2. **Extend.** Starting from `ch09-s04`, add `An_ended_session_records_nothing_more` to `ChargingSessionProperties`: use `SessionChecks.AfterEveryDecision`, and when `step.Before` holds an `Ended` state, require `step.Events` to be empty. Expect it to pass and the total to rise to 724; then explain from section 7.7 why it passes.
3. **Extend.** Starting from `ch09-s03`, add a row to `pricing-examples.csv` for a car unplugged two minutes after the grace period: standard tariff, 09:00 to 09:30, 10 kWh, unplugged at 09:47. Work out the breakdown by hand first. Expect `energy 4.20; session fee 1.00; idle fee 0.80` and USD 6.00, and one more test case in the runner.
4. **Break it.** At `ch09-s03`, swap the two `ReserveConnector` rows of `ChargePoint.Decide` back to their Chapter 7 order and run the Charging tests. Expect the reservation row "driver reserves at 08:10" to fail with `"nothing changes"`, together with `Given_the_drivers_expired_reservation_when_reserved_again_then_a_new_one_starts`.
5. **Break it.** At `ch09-s04`, make `AllocationsFit` return `true` and run the Site Energy tests. Expect `Allocations_never_exceed_the_effective_capacity` to stay green and `Given_allocations_that_break_the_rules_then_the_assertion_is_false` to fail.

## Further reading

- Eric Evans, [*Domain-Driven Design Reference*](https://domainlanguage.com/wp-content/uploads/2016/05/DDD_Reference_2015-03.pdf) (2015). Part III "Supple Design", "Assertions" (p. 22): the definition that section 9.4 turns into code.
- Jérémie Chassaing, ["Functional Event Sourcing Decider"](https://thinkbeforecoding.com/post/2021/12/17/functional-event-sourcing-decider) (2021). The "Fold" section shows why a test can give events instead of a state.
- Microsoft, [*Testing with 'dotnet test'*](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-with-dotnet-test) (2026). The section "MTP mode of `dotnet test`" covers the Microsoft.Testing.Platform mode and the `global.json` runner setting.
- Vlad Khononov, [*Learning Domain-Driven Design*](https://www.oreilly.com/library/view/learning-domain-driven-design/9781098100124/) (O'Reilly, 2021). Chapter 10 "Design Heuristics", section "Testing Strategy", matches the shape of the test suite to the business-logic pattern.
- Gojko Adzic, [*Specification by Example*](https://gojko.net/books/specification-by-example/) (Manning, 2011). Chapter 3 "Living documentation": how teams turn experts' examples into living documentation.
- Gerard Meszaros, [*xUnit Test Patterns*](http://xunitpatterns.com/) (Addison-Wesley, 2007). Part II "The Test Smells", chapters 15 to 17: the catalog of test smells that section 9.6 extends.
- Anthony Lloyd, [*CsCheck*](https://github.com/AnthonyLloyd/CsCheck). Generators, shrinking and seeds, in the library the sample uses.
