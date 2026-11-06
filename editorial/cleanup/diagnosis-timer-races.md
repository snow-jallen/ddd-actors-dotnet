# Diagnosis: timer and timing races (LinkTests, GridSignalPollerTests, GatewayDrainTests)

All three fixes are test-only. They add no tests and remove none, so every test total the book
quotes stays the same. No `src/` file changes, and no file the book quotes with a `// File:`
header changes. No later commit touches any of the three test files. `git rev-parse <tag>:<file>`
gives the same blob at every tag that contains the introducing tag, `upgrade/akka-1.6`
(`ch21-s10`) and `v1.0.0` included. So each patch replays onto every later commit without a
conflict.

Patches are in `editorial/cleanup/patches/`:

| id | Patch | Introducing tag | File |
| --- | --- | --- | --- |
| link-tests | `timer-races-link-tests.patch` | `ch17-s07` (ded01ac) | `tests/Ohmline.Telemetry.Tests/LinkTests.cs` |
| grid-signal-poller | `timer-races-grid-signal-poller.patch` | `ch16-s08` (47c44a6) | `tests/Ohmline.SiteEnergy.Acl.GridOperator.Tests/GridSignalPollerTests.cs` |
| gateway-drain | `timer-races-gateway-drain.patch` | `ch21-s07` (0566ea5) | `tests/Ohmline.DeviceGateway.Tests/GatewayDrainTests.cs` |

Each patch was checked as follows:
- `git apply --check` passes at its introducing tag.
- `dotnet build <test project> -warnaserror` gives 0 warnings and 0 errors at the introducing
  tag, at `ch21-s09` (main HEAD) and at `ch21-s10` (Akka.NET 1.6), with all three patches applied.
- Each affected class passed 5 of 5 runs at `ch21-s09` and at `ch21-s10`.

Final confirmation, with all three patches applied at `ch21-s10`:
- `dotnet build Ohmline.slnx -warnaserror`: 0 warnings, 0 errors.
- `dotnet test --solution Ohmline.slnx`: total 2676 (the book's figure), 1 failed.
- The failure was `ChargeToReceiptTests`, the Aspire system test. It timed out in the
  simulator's `CallAsync` at load average about 55. That is a known load flake outside this
  family.
- `LinkTests`, `GridSignalPollerTests` and `GatewayDrainTests` passed in that run.

How the stress runs were done: the xUnit v3 test executable was run directly
(`<exe> -class <class>`), pinned with `taskset` to a single CPU, with N busy-loop "hogs" on the
same CPU. Load average from other agents on the machine was 10 to 55 at the time.

## 1. LinkTests (`ch17-s07`): two races, not one

Test: `Given_a_timer_started_in_a_trace_then_it_fires_in_a_new_trace_linked_to_it`.

**Race A, the warm-up timer (the one in the errata).**
- The untraced warm-up `Arm` also starts the 50 ms `ring` timer.
- The traced `Arm` re-arms the same key, which cancels the warm-up's timer. But it only does so
  if it is handled within 50 ms of the warm-up.
- On a busy machine it is handled later. The warm-up's untraced `Rang` then reaches `TestActor`
  first, and the test fails with `Expected a message of type LinkTests+Armed, but received
  {Rang { ActivityId = }}`.

**Race B, a span that has not stopped yet (new, not in the errata).**
- `Alarm` tells `Rang` from inside its handler, so the `process Alarm` span for the ring stops
  only after the handler returns.
- The test then read `recorder.All.Single(span => span.Id == rang.ActivityId)` without waiting.
  Under load the span had not stopped yet, and the test failed with `InvalidOperationException:
  Sequence contains no matching element`.
- `SpanRecorder.SingleAsync` exists for exactly this case: its doc comment says "a reply can reach
  the test before the span it was sent from has ended". The test just did not use it here.

**Reproduction.**

| Run | Result |
| --- | --- |
| Unpatched, 1 CPU, 12 hogs | 1 of 15 failed (B) |
| Unpatched, 1 CPU, 24 hogs | 6 of 25 failed (1 A, 5 B) |
| Unpatched, 1 CPU, 16 hogs, machine load about 45 | 20 of 20 failed (A) |
| Deterministic A: `await Task.Delay(100, Ct)` between the warm-up and the traced `Arm` (the book's Exercise 2 criterion) | 3 of 3 failed with the exact errata message |
| Deterministic B: `Thread.Sleep(200)` after `listener.Tell` in the `Ring` handler | 3 of 3 failed with `Sequence contains no matching element` |

**Fix (+4 / -1 lines).**
1. After the warm-up ask, the test now waits for the warm-up's own ring:
   `await ExpectMsgAsync<Rang>(cancellationToken: Ct); // Its ring: no stale one is left.`
   The traced `Arm` can then only be followed by its own `Armed` and `Rang`.
2. The ring span is now awaited by its trace id, the same way the arm span already was:
   `await recorder.SingleAsync(ActivityContext.Parse(rang.ActivityId!, null).TraceId,
   ActorShellDiagnostics.Name)`, followed by `ring.Id.ShouldBe(rang.ActivityId)`.

Nothing changes in `Alarm` or `Bell`.

**Verification.**

| Run | Result |
| --- | --- |
| Fixed, same 16-hog A/B run as the 20/20 failures above | 20 of 20 passed |
| Fixed, with each deterministic injection | 3 of 3 passed for each |
| Fixed, 1 CPU, 24 hogs | 24 of 25 passed |

The one failure in the 24-hog run was the other test, `Bell`. Its default 3 s `ExpectMsgAsync`
ran out at roughly 1/25 of a CPU. That is the job of `OHMLINE_TIMEFACTOR`, not a race.

## 2. GridSignalPollerTests (`ch16-s08`): an answer that is late counts as no answer

Test: `Given_a_revision_that_cancels_the_event_then_the_site_is_restored`.

**Root cause.** The test gives the poller `AskTimeout = 500 ms`, and the probe's autopilot
answers each command. On a busy machine the answer can take more than 500 ms. When it does:
1. The ask fails and the poller gets `NotTold`.
2. By design, the next 100 ms poll tells the site the *same* `ApplyCurtailment` again. This is
   the behaviour `Given_a_site_that_did_not_answer_then_it_is_told_again` tests.
3. The revision test expects the next message to be `EndCurtailment` and gets the repeated
   `ApplyCurtailment`.

`Given_an_event_in_force_..._curtailed_once_...` has the same weakness: its
`ExpectNoMsgAsync(500 ms)` receives the retry.

So the poller is correct and the timeout is the issue. A test whose budget answers must not
treat a slow answer as a missing one.

**Reproduction.**

| Run | Result |
| --- | --- |
| Deterministic: the autopilot sleeps 700 ms before its first answer | 3 of 3 runs failed both tests, with the exact errata message (`Expected ... EndCurtailment, but received {ApplyCurtailment ...}`) and `Expected no messages during 00:00:00.5 ...` |
| Load: 1 CPU, 12 hogs | 10 of 15 runs failed the revision test |

**Fix (+7 / -3 lines).** `Poller(feed, budgets, TimeSpan? askTimeout = null)`.
- By default the ask timeout is now `Dilated(TimeSpan.FromSeconds(3))`. That is 3 s ×
  `akka.test.timefactor`, which is `OHMLINE_TIMEFACTOR`. It matches the 3 s the `EdgeTestKit`
  fix chose at `ch21-s08`.
- `Given_a_site_that_did_not_answer_then_it_is_told_again` passes
  `askTimeout: TimeSpan.FromMilliseconds(500)`. Its budget never answers, so it keeps the short
  ask, and its 5 s wait for the retry still holds at any time factor.

**Verification.**

| Run | Result |
| --- | --- |
| Fixed, with the 700 ms injection | 3 of 3 passed |
| Fixed, 1 CPU, 12 hogs (A/B with the 10/15 failures above) | 15 of 15 passed |
| Fixed, 1 CPU, 8 hogs | 20 of 20 passed |

## 3. GatewayDrainTests (`ch21-s07`): stopping before the gateway has recorded the connection

Test: `Given_connected_chargers_when_the_gateway_stops_then_each_is_told_to_return`. The final
report saw it fail once in a full-suite run with `[(NormalClosure, Bye)]`.

**Root cause.** The final report suggested two possible causes: the 1 s `DrainWindow` running out,
or the host's shutdown overtaking the drain. Neither is it.
- `(NormalClosure, "Bye")` is the close that `ChargerSockets.ConnectAsync` sends when a socket
  ends *without* a restart. So that charger was never in the drain's plan.
- `client.ConnectAsync` returns once the 101 handshake is done, inside the server's
  `AcceptWebSocketAsync`. The server records the connection with `open.Add(id, link)` only after
  that, on a continuation.
- If the test calls `_gateway.DisposeAsync()` in that gap, `ConnectionDrain` snapshots
  `connections.Current` without that charger. The charger is then closed at the end of shutdown
  with a normal "Bye".

**Reproduction.** A temporary `await Task.Delay(300)` before `open.Add` for `OHM-SLC-0043` failed
3 of 3 runs with exactly `[(NormalClosure, Bye)]`.

Two checks rule out the suggested causes:
- With `DrainWindow` raised to 6 s, the test passed 3 of 3 runs, taking 4.6 s each. The host does
  wait for the drain, so neither a wider window nor a TimeFactor-scaled window would change
  anything.
- The natural flake is rare. It was not seen in 15 runs pinned to 1 CPU with 12 hogs, or in 48
  runs as 6 concurrent loops.

**Fix (+19 lines).** Before stopping the gateway, the test now waits until the gateway's
`OpenConnections` holds the three connections. This is a bounded `RecordedAsync(expected)` that
polls `_gateway.Services.GetRequiredService<OpenConnections>().Current.Count` every 10 ms, with a
10 s patience, the same idiom as `SpanRecorder`. The test then asserts
`(await RecordedAsync(chargers.Length)).ShouldBe(chargers.Length)`.

**Verification.**

| Run | Result |
| --- | --- |
| Fixed, with the 300 ms injection | 3 of 3 passed |
| Fixed, 1 CPU, 8 hogs | 20 of 20 passed |
| Fixed, 1 CPU, 12 hogs | 15 of 15 passed |

**Product note (not fixed: outside this family, editor's call).** The same gap exists in
production, but it is tiny. If a charger passes the `open.Accepting` check, and the drain then
snapshots before `open.Add`, that charger is missed. It gets a plain "Bye" when the host stops,
with no 1012 hint and no jitter.

A source fix would go in `ChargerSockets.ConnectAsync`: re-check `open.Accepting` after
`open.Add`, and restart the link at once if the drain has begun. That would also need
`ConnectionDrain` to wait for late connections. The book does not quote `ChargerSockets.cs` or
`ConnectionDrain.cs` (only `DrainPlan.cs` and `DrainExtensions.cs` at `ch21-s07`), but section
21.7's prose describes the drain.

## Book impact

- **`ch17-tracing-through-the-mailbox.md`, line 737** (the 17.8 Note): "On a loaded machine,
  `LinkTests` from section 17.7 can see an untraced warm-up timer ring before the traced one and
  fail; Exercise 2 fixes the test." This is no longer true at any tag after the replay. Drop the
  sentence.
- **Same chapter, line 798** (Exercise 2): it asks readers to make the test immune to timing,
  with "a 100 ms delay" as the check. The repo now already passes that check. Either rewrite the
  exercise or drop it. One possible rewrite: "explain why the test waits for the warm-up's ring,
  and what `SingleAsync` waits for".
- **`review-log.md`, line 368** refers to the same Note.
- **`GridSignalPollerTests.cs`** is not quoted or named in the book. The 16.8 prose ("every 5 s",
  "never while an earlier command to it is in flight") is about the production options and is
  unaffected.
- **`GatewayDrainTests.cs`** is not quoted. The ch21 "Try it" (line 799: "three test WebSockets
  each receive status 1012 and a hint between 5 and 30 s") stays true.
- The test totals per tag are unchanged.

## Replay notes

Apply each patch to its introducing commit during the history rewrite. No later commit touches
these files, so there is nothing to resolve. Nothing was fixed forward later either:
- `ch21-s08` raised `EdgeTestKit`'s ask to 3 s.
- `ch18-s04` scaled the SQLite asks.

Neither change touches these files. The errata entries to close are:
- "ch17-s07 (observed, not fixed): LinkTests can see the warm-up timer's ring first".
- The GridSignalPoller load-flake mentions (ch17 testing pass, ch19 verification).
- The final report's item 7 (`GatewayDrainTests`).
