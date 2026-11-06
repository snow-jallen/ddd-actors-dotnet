# Re-verification after the history rewrite: ch15-* to ch21-*, v1.0.0, side branches (2026-11-05/06)

## Method

- **Refs:** 63 tags (`ch15-s01` to `ch21-s10`; `ch21-s10` is on `upgrade/akka-1.6`), `v1.0.0`, and the
  tips of `alt/ch20-s04-adapter-only` and all 10 `wrong/*` branches, in that order.
  - Every tag resolves to the `new_commit` in `tag-map.tsv`.
- **Per ref:**
  - A fresh detached worktree at `.../scratchpad/reverify-ch15-21/<ref>`, with `/` replaced by `__`
    for branches.
  - `dotnet build Ohmline.slnx -warnaserror`.
  - From `ch19-s04` on, `dotnet build mobile/Ohmline.Mobile.slnx -warnaserror` (net11.0-android).
  - `dotnet test --solution Ohmline.slnx`, then the worktree was removed.
- **Toolchain:** the `tools/env.sh` SDK, with rootless Podman through `DOCKER_HOST`. Only one
  `dotnet test` ran at a time.
- **Run rule:**
  - These tags needed **3 consecutive green full runs**: ch15-s05..s07, ch16-s07..s10,
    ch17-s07..s09, ch18-s01, ch18-s03, ch18-s07..s09, ch19-s04, ch19-s06..s08, ch20-s02,
    ch20-s08..s10 and ch21-s07..s10.
  - Every other tag needed one green run.
  - After a failure the counter reset, up to 7 runs (3 for one-run tags).
  - A side branch that failed was run a second time to show the failure is deterministic.
- **Load:** the other two re-verification agents ran at the same time.
  - The 1-minute load was 10 to 55 during ch15, about 5 to 15 during ch16 and ch17, and about 3 to
    10 from ch18 on.
- **No refs were changed** and nothing was committed. `git worktree list` shows only the main
  checkout.
- **Raw data:**
  - `results.tsv`, per-run logs, `run.sh` and the job lists are in
    `/tmp/claude-1000/-home-jonathan-code-scienceNight/45f474fc-e89c-490e-8b5f-ec3ffd186198/scratchpad/reverify-ch15-21-logs/`.
  - The first ch15-s05 runs are in `phase1/` there.

## Result

- **Builds:** all 75 refs built `Ohmline.slnx` with **0 warnings and 0 errors**.
- **MAUI head:** all 28 refs from `ch19-s04` on built with **0 warnings and 0 errors**: 27 tags,
  `v1.0.0`, `alt/ch20-s04-adapter-only` and `wrong/ch21-s02-minver-appversion`.
  - **Same-path check, beyond the brief:** one worktree path ran
    `git clean -ffdxq` and then a head build at each step of ch19-s04 → s05 → s07 → s04 → s08 → s05 →
    ch20-s01 → ch21-s09. That is the setup that used to trigger CS0200.
  - All 8 builds gave 0 warnings, 0 errors and no CS0200.
- **Test totals:** **63 of 63 tags match progress.md** exactly, with no differences.
  - `alt/ch20-s04-adapter-only` gives 2,433, the book's figure (ch20 line 445).
- **Green runs:**
  - Every tag has a green full run.
  - Every 3-run tag has 3 consecutive green runs. ch15-s05 got them only in a second pass at load
    about 2.5.
  - In all, 132 full runs at the tags and v1.0.0 had 8 failed runs, each with 1 or 2 failed tests.
- **Fixed tags:** these failed every run before the rewrite and are now green 3 of 3 each:
  - **ch15-s07**: before, 4 of 4 failed on the read-model race.
  - **ch17-s07**: before, 4 of 4 failed on the `LinkTests` race.
  - **ch18-s03**: before, 3 of 3 failed.
  - **ch20-s08**: before, 5 of 5 failed.
  - ch20-s02 was green 3 of 3 too.
  - No `QueryApiTests`/`ReadYourWritesTests` 42P01, `LinkTests`, `GatewayDrainTests`,
    `GridSignalPollerTests`, Ch02.Hosted, SQLite AskTimeout, `SessionEndpointsTests`,
    `NodeMetricTests`/`TapToJournalTraceTests` or `ChargeToReceiptTests` failure was seen anywhere
    in this range.

| tag | total | runs (P pass, Fn n failed; * = second pass) |
| --- | --- | --- |
| ch15-s01..s04 | 1669, 1699, 1830, 1838 | P each |
| ch15-s05 | 1859 | P F1 P F1 F1 F2 P, then P* P* P* |
| ch15-s06 | 1871 | P F1 P P P |
| ch15-s07 | 1905 | P P P |
| ch16-s01..s06 | 1914, 1923, 1951, 1981, 1991, 2031 | P each |
| ch16-s07 | 2070 | F1 P P P |
| ch16-s08..s10 | 2090, 2092, 2111 | P P P each |
| ch17-s01..s06 | 2121, 2126, 2129, 2148, 2156, 2159 | P each |
| ch17-s07..s09 | 2169, 2172, 2172 | P P P each |
| ch18-s01 | 2180 | P P P |
| ch18-s02 | 2185 | P |
| ch18-s03 | 2191 | P P P |
| ch18-s04..s06 | 2195, 2197, 2202 | P each |
| ch18-s07..s09 | 2202, 2203, 2206 | P P P each |
| ch19-s01..s03 | 2206, 2264, 2283 | P each |
| ch19-s04 | 2294 | P F1 P P P |
| ch19-s05 | 2335 | P |
| ch19-s06 | 2361 | P P F1 P P P |
| ch19-s07, s08 | 2377, 2384 | P P P each |
| ch20-s01 | 2394 | P |
| ch20-s02 | 2402 | P P P |
| ch20-s03..s07 | 2412, 2428, 2550, 2557, 2561 | P each |
| ch20-s08..s10 | 2569, 2585, 2597 | P P P each |
| ch21-s01..s06 | 2599, 2613, 2613, 2615, 2623, 2632 | P each |
| ch21-s07..s10 | 2651, 2661, 2664, 2676 | P P P each |
| v1.0.0 | 2664 | P (progress.md has no figure; the same as ch21-s09, which adds docs only) |

## Failures at tags, explained

### 1. `TraceShapeTests` journey race (ch19-s04 run 2, ch19-s06 run 3): a real test race, not load

- **Failing test:**
  `Ohmline.Telemetry.Tests.TraceShapeTests.Given_a_journey_then_what_it_tells_other_actors_stays_in_the_request_trace`.
  - **Message:** `System.InvalidOperationException : Sequence contains no matching element` at
    `TraceShapeTests.cs:90`, which is `spans.Single(span => span.DisplayName == "persisted ChargingJourneyActor")`.
  - The load was only about 4 both times.
- **Cause:** a test-only race, present since the test was added at **ch17-s08** and unchanged
  through main and ch21-s10.
  - `SpansAsync` waits until the span named `last` has been exported. Here that span is
    `process IdTokenActor`.
  - Its parent, `persisted ChargingJourneyActor`, is created in `TracedPersistentActor` (line 153 at
    ch21-s09) as `using var completing = write?.Completing(); onComplete();`. It ends only when
    `onComplete` returns.
  - The `IdTokenActor` that `onComplete` tells can process the message and export its child span
    first. A poll in that window sees the child without its parent.
  - The other two tests wait for a span that ends last, so they are not affected.
- **Reproduced at ch21-s09 (main):**
  - Running the class alone, uncontended, passed 20 of 20.
  - Pinned to one CPU with 3 busy loops (`taskset -c 3`), **3 of 12 runs failed with the same
    exception at line 90**.
- **Suggested test-only fix:** wait for both spans. For example, pass
  `"persisted ChargingJourneyActor"` too and wait until every named span has been exported. Or
  collect after `process IdTokenActor` and poll until the `persisted` span appears.
- **Book check:**
  - errata.md mentions `TraceShapeTests` only as a load failure at ch18-s03 (line 589).
  - **Section 17.8 quotes the exact call** (ch17 line 718, in the `TraceShapeTests.cs @ ch17-s08`
    listing starting at line 715): `var spans = await SpansAsync(node, trace, "process IdTokenActor");`.
    A fix at ch17-s08 would change that listing and the ch17-s07..s08 stat line (ch17:748), so it
    needs an editor decision, as the Ch02.Hosted fix did.
  - No test total changes.

### 2. Cold-start asks under heavy load (ch15-s05, ch15-s06, ch16-s07; load 15 to 55): load-timeouts family

- **`SiteEnergyEndpointsTests.Given_a_site_then_its_budget_is_configured_with_every_connector`**
  (ch15-s05 run 2) and **`DriverAccountsEndpointsTests.Given_a_card_reported_lost_then_the_token_is_blocked`**
  (ch15-s05 run 6).
  - **Message:** `(await answer).Result should be of type Accepted<CommandAcceptedResponse> but was ProblemHttpResult`.
  - **Cause:** both test kits ask with `AskTimeout = TimeSpan.FromMilliseconds(500)`
    (SiteEnergyEndpointsTests.cs:34, DriverAccountsEndpointsTests.cs:27).
  - `diagnosis-load-timeouts.md` listed these under "Latent, same pattern, not observed failing".
    **They have now been observed failing.**
  - The fix would be the same as `edge-testkit-ask`: 3 s.
- **`Ohmline.Charging.Actors.Tests.SnapshotFailureTests.Given_a_snapshot_due_that_cannot_be_saved_then_the_reply_is_unchanged`**
  (ch15-s05 runs 4, 5 and 6) and
  **`Sessions.ChargingSessionActorTests.Given_an_ended_session_in_the_journal_then_the_new_incarnation_remembers`**
  (ch15-s06 run 2).
  - **Message:** `Akka.Actor.AskTimeoutException : Timeout after 3.00 seconds` at
    `Ohmline.Testing.PersistenceShellSpec.Scenario.SeedAsync()` (PersistenceShellSpec.cs:112).
  - **Cause:** the seeding ask uses `3 * ActorTestConfig.TimeFactor` seconds, which is 3 s by default.
- **`Sessions.ChargingSessionActorTests.Given_no_session_when_it_starts_then_its_terms_are_recorded_at_version_1`**
  (ch16-s07 run 1).
  - **Message:** `Timeout 00:00:03 while waiting for a message of type System.Object`. This is the
    TestKit default expect.
- **Not in errata.md.** These failures appeared only at load 18 to 55, when three full suites were
  sharing the machine.
  - ch15-s05 then passed 3 of 3 at load 2.5, and the other two tags passed their next 3 runs.
  - The `PersistenceShellSpec` and TestKit 3 s waits scale with `OHMLINE_TIMEFACTOR`. They can be
    left alone or raised, the editor's call.

## Side branches

All built clean. Their totals are the base tag's total plus the branch's added test. Each failing
branch failed the same tests in both runs.

| branch | total | result | matches the book? |
| --- | --- | --- | --- |
| alt/ch20-s04-adapter-only | 2433 | green | yes, "2,433 tests passing" (ch20) |
| wrong/ch10-s02-union-on-the-wire | 823 | green | yes, Demonstrates passes |
| wrong/ch10-s03-default-serializer | 936 | green | yes, both Demonstrates pass |
| wrong/ch10-s06-persist-async-decider | 953 | 1 failed: `InFlightWriteTests.Given_a_write_in_flight_when_more_messages_arrive_then_they_wait_in_order` | yes (ch10 line 686) |
| wrong/ch10-s10-logic-in-receive | 1094 | 2 failed: `ChargePointActorTests.Given_accepted_commands_then_each_version_counts_the_events_persisted`, `TwoIncarnationsTests.Given_two_writers_when_both_write_the_next_event_then_the_first_one_wins` | yes |
| wrong/ch12-s02-actorref-as-identity | 1236 | green | yes |
| wrong/ch12-s04-context-stop | 1243 | **4 failed** (see below) | **no: the book says three** |
| wrong/ch13-s04-offset-before-handler | 1553 | 2 failed: `ProjectionRunnerTests` fails-once and crash tests | yes |
| wrong/ch16-s01-dual-write | 1915 | 2 failed: both `IntegrationEventRelayTests` | yes |
| wrong/ch18-s01-high-cardinality-tags | 2180 | green | yes, all tests pass |
| wrong/ch21-s02-minver-appversion | 2614 | 63 failed, by design (see below) | consistent |

### wrong/ch12-s04-context-stop: four tests fail, the book says three

- **Book:** ch12 line 386 says "three tests from main fail, among them
  `PassivationTests.Given_a_session_that_ends_then_it_asks_its_shard_to_passivate_it`".
- **What fails, in both runs:**
  - `PassivationTests.Given_a_session_that_ends_then_it_asks_its_shard_to_passivate_it`:
    `Timeout 00:00:03 while waiting for a message of type ...CommandAccepted`, as the book says.
  - `PassivationTests.Given_a_command_behind_the_last_write_then_it_is_answered_before_the_stop`.
  - `ChargingSessionActorTests.Given_a_metered_session_when_stopped_then_it_ends_with_the_energy_delivered`.
  - `Sqlite.ShardRegionTests.Given_an_ended_session_then_its_shard_stops_it_until_the_next_command`:
    `AskTimeoutException : Timeout after 20.00 seconds` at `SqliteNode.AskAsync`.
- **The rewrite did not cause this.** The pre-rewrite branch
  (`refs/backup/pre-cleanup/heads/wrong/ch12-s04-context-stop`, abf75bf) fails the same 4 tests in
  2 of 2 runs, with 1243 tests.
  - The only difference is that the SQLite test now gives up after 20 s instead of 5 s. The
    `sqlite-node-ask` fix makes a branch run about 15 s longer.
- **Suggested book fix:** "four tests from main fail".

### wrong/ch21-s02-minver-appversion

- **The `Demonstrates_` test fails on purpose.** Its doc comment says "Fails on this branch".
  - **Message:** `AppVersion.Create(appVersion) should not throw but threw ArgumentOutOfRangeException ... [0.0.0-alpha.0.166]`.
  - `The_build_writes_the_release_s_major_minor_patch_and_akka_parses_it` fails with
    `should be "0.0.0" but was "0.0.0-alpha.0.166"`.
- **60 node-starting tests fail** with `ArgumentOutOfRangeException` at `Akka.Util.AppVersion.Parse()`.
  They are in Node.Tests, IntegrationTests and Telemetry.Tests.
- `ChargeToReceiptTests` times out.
- This is the startup failure section 21.2 describes. The book does not claim a test result for
  the branch.

## For the editor

1. **New, real race:** the `TraceShapeTests` journey test, from ch17-s08 on. It has a test-only fix,
   but the fix touches a line quoted in section 17.8. It should go into errata and the cleanup
   list.
2. **Load-only flakes that were latent and are now observed:** the `SiteEnergy.Api.Tests` and
   `DriverAccounts.Api.Tests` 500 ms asks. Also new: the `PersistenceShellSpec` 3 s seed ask and the
   3 s TestKit expects, seen only at load 18 to 55.
3. **Book, ch12 line 386:** "three tests from main fail" should be "four". This was wrong before the
   rewrite too.
4. **Test totals:** no total changed anywhere in ch15 to ch21, v1.0.0 or the side branches. No
   book test count needs updating.
