## Status after cleanup job 1 (book resync, 2026-11-06)

Cleanup job 1 (`queued-jobs.md`) amended 22 test-only or tooling fixes into the commits that
introduced each defect and re-pointed every tag (`cleanup/rewrite-report.md`,
`cleanup/repair-report.md`). Every `chNN-sMM` tag from `ch02-s01` on, `v1.0.0`, `main` and `upgrade/akka-1.6` now
builds with 0 warnings and passes its full suite; the tags that used to flake passed 3 consecutive
full runs. Every entry below carries a **Status** line. Fix ids and the tags they were amended into:

| fix id | amended into | closes |
| --- | --- | --- |
| `ch01-dod-exemption` | `ch01-s07` | Chapter 1 tags and the definition of done |
| `logger-race` | `ch02-s09` | Ch02.Hosted shutdown-log race |
| `sqlite-node-ask` | `ch10-s07` | SQLite `AskTimeout` 5 s load flakes |
| `two-incarnations-ask` | `ch10-s09` | same, `TwoIncarnationsTests` |
| `listing-snapshot-settled` | `ch11-s07` | `ChargePointListingActorTests` snapshot race |
| `state-sourcing-ask` | `ch11-s07` | same as `sqlite-node-ask`, `StateSourcingTourTests` |
| `persistence-shell-seed` | `ch11-s08` | `PersistenceShellSpec` cold-start seeding (3 s) |
| `edge-testkit-ask` | `ch12-s05` | `EdgeTestKit` 500 ms ask (`SessionEndpointsTests`, `TariffEndpointsTests`) |
| `site-energy-api-ask` | `ch12-s05` | `SiteEnergyEndpointsTests` 500 ms ask |
| `driver-accounts-api-ask` | `ch12-s07` | `DriverAccountsEndpointsTests` 500 ms ask |
| `journal-node-ask` | `ch13-s03` | `ReadJournalTourTests` 5 s ask |
| `fixture-ensures-schema` | `ch13-s08` | read-model schema race (42P01) at ch14-s01, ch15-s07, ch18-s03, ch20-s02 |
| `driver-app-charge-waits` | `ch14-s06` | `DriverAppTests.ChargeAsync` read-your-writes race |
| `replay-charge-and-stop-waits` | `ch14-s07` | `ReplayTests.ChargeAndStopAsync` race |
| `simulator-retries` | `ch16-s07` | `ChargeToReceiptTests` (Aspire system test) load flake |
| `grid-signal-poller` | `ch16-s08` | `GridSignalPollerTests` revision race |
| `link-tests` | `ch17-s07` | `LinkTests` warm-up timer race |
| `journal-trace-unnamed` | `ch17-s07` | `JournalTraceTests` actor-name race |
| `traced-node-ask` | `ch17-s08` | `NodeMetricTests`/`TapToJournalTraceTests` 504s |
| `trace-shape-ancestors` | `ch17-s08` | `TraceShapeTests` journey span race |
| `maui-cs0200` | `ch19-s04` | MAUI head CS0200 after `AppShell.xaml` changes |
| `gateway-drain` | `ch21-s07` | `GatewayDrainTests` drain-window flake |
| (tag message) | tag `ch18-s01` | message now `18.1 Domain metrics with a cardinality budget` |

No test total changed at any tag. Book changes made by the resync: 22 `--stat` totals, the
`TracedNode.cs @ ch17-s08` listing, the 17.8 Note and Exercise 2, the 18.4 Note, the 19.4 prose
and 19.5 Version note, the 12.4 side-branch count (four tests, not three), and the 21.9 commit
hashes (see `final-report.md`, "Cleanup (job 1)").

### Still open

1. **Podman rootlessport port clash** (`HealthTests`, seen once at `ch19-s08` before the cleanup).
   Environmental: rootless Podman binds the random host port only at container start, inside the
   ephemeral range the suite's own sockets use. A retry-on-bind wrapper around `StartAsync` would
   be the only test-side remedy. Not seen in any re-verification run.
2. **`ChargePointActorTests.Given_accepted_commands_then_each_version_counts_the_events_persisted`**
   (from `ch11-s08`): under extreme contention (1 CPU with 16 busy loops) it failed 1 of 9 runs,
   because a recovered, lapsed reservation's timer can be decided before the test's `Tell`. Never
   seen in a full-suite run. The book quotes the test and `ThenAsync` at `ch11-s08` (section 11.8),
   so a fix needs an editor decision.
3. **`PersistenceShellSpec.ThenAsync`'s 3 s TestKit expects** (book-quoted at `ch11-s08`) scale only
   with `OHMLINE_TIMEFACTOR`. After `persistence-shell-seed` they cover only a warm actor's reply;
   no failure seen since. Editor's call whether to raise them.
4. **`RegionTests.Given_a_failed_write_then_the_next_message_starts_the_entity_again`** is fixed
   forward at `ch13-s02` only, so it stays latent at `ch12-s09`..`ch12-s11`. Not seen in any
   re-verification run; the book's 12.9 Tip already tells readers a rerun passes. Could be
   backported to `ch12-s09` if every tag must be deterministic.
5. **Stale commit hash in the repo's `CHANGELOG.md`** (from `v1.0.0`, also at `ch21-s09` and
   `ch21-s10`): `* **charging:** idle fee cap per tariff (da32c99)` names the feature commit's
   pre-rewrite id; it is now `67228e8`. The book (21.9) no longer quotes the hash. Fixing the file
   means amending the `v1.0.0` commit and replaying `ch21-s09`'s two later commits, the
   `upgrade/akka-1.6` commit and the tags `v1.0.0`, `ch21-s09`, `ch21-s10`.
6. **Leftover wording in the repo** (commit messages are never edited, and later commits' own
   text wins): the `ch18-s04` message still ends "SQLite tests wait longer"; the `ch13-s03` message
   still says it fixes the listing snapshot race; `ch21-s08`'s `EdgeTestKit` comment still says
   "(500 ms timed out now and then from Chapter 21's test count on)". None is quoted in the book.
7. **`sample-architecture.md` §10** still documents tag messages as `chNN-sMM: <title>`; every tag
   now uses `NN.M <title>` (the commit subjects keep the `chNN-sMM:` form).
8. **Backups** to remove once the author is satisfied: `refs/backup/pre-cleanup/*` and
   `refs/backup/pre-repair/*` in `sample/`, and `editorial/cleanup/sample-before-cleanup.bundle`.

## ch01 (testing agent, 2026-11-04): Chapter 1 tags do not pass the definition of done

**Status (cleanup job 1): RESOLVED** by `ch01-dod-exemption`, amended into `ch01-s07`. The exemption is accepted: `README.md` and the header of `tools/verify-tags.sh` now say the Chapter 1 tags are skipped and every tag from `ch02-s01` on must pass. Output of the script is unchanged; book 1.7 needed only its `--stat` total (now `267 insertions(+)`).

The sample repo is unchanged. Tested in detached worktrees with SDK 11.0.100-rc.3.26522.148.

- `ch01-s02`, `ch01-s03`, `ch01-s04`, `ch01-s06`: these tags have no `global.json` and no
  `Ohmline.slnx`.
  - `dotnet build Ohmline.slnx -warnaserror` fails with `MSBUILD : error MSB1009: Project file does not exist.`
  - `dotnet test --solution Ohmline.slnx` fails with `MSB1009` (it falls back to VSTest because there is no `global.json`).
- `ch01-s07`: `Ohmline.slnx` is an empty `<Solution>`.
  - The build fails with `NuGet.targets(200,5): error : Unable to find a project to restore! [Ohmline.slnx]` (a warning, promoted by `-warnaserror`).
  - The test fails with `Unhandled exception: The solution configuration '|' is invalid.`
- This is the known, already-flagged deviation 1 in `code-notes/ch01.md`. Both the chapter text (section 1.7 and exercise 5) and
  `tools/verify-tags.sh` treat these tags as skipped. `tools/verify-tags.sh 'ch01-*'` prints
  `0 passed, 5 skipped, 0 failed` and exits 0.
- Editor decision needed: either accept that Chapter 1 tags are exempt from §11, or add a placeholder project at `ch01-s07`.
  No book change is needed while the exemption stands.

## ch03 (testing agent, 2026-11-04): flaky Chapter 2 hosted test seen at `ch03-s04`

**Status (cleanup job 1): RESOLVED.** Ch02.Hosted shutdown-log race: RESOLVED by `logger-race`, amended into `ch02-s09`. A `before-actor-system-terminate` task waits (at most 5 s) until the drain line has reached the captured logs, so the logger actor cannot stop first. Test-only; the section 2.9 listings are unchanged. 0 failures in 75+ runs from `ch02-s09` on (reverify-ch01-07).

The sample repo is unchanged. Tested in detached worktrees with SDK 11.0.100-rc.3.

- Every ch03 tag passes `dotnet build Ohmline.slnx -warnaserror` with 0 warnings. `dotnet test --solution Ohmline.slnx`
  passes at every tag except one run at `ch03-s04`: `total: 75`, `failed: 1`, `succeeded: 74`, exit code 2.
- The failing test was `Ch02.Hosted.Tests.ChargerAppTests.Given_a_running_host_when_it_stops_then_coordinated_shutdown_drains_first`:
  ```
  Shouldly.ShouldAssertException : _logs.Messages
      should contain an element satisfying the condition
  m.Contains("draining charger twins")
      but does not
    at Ch02.Hosted.Tests.ChargerAppTests...drains_first() in learn/Ch02.Hosted.Tests/ChargerAppTests.cs:69
  ```
- This file has not changed since `ch02-s09`, and 6 reruns in a row at `ch03-s04` all passed. That makes it a race, not a
  Chapter 3 regression. The test checks the captured log right after `_app.StopAsync`, but Akka.NET logs through its
  asynchronous logger actor. In the failed run the "Host is stopping: draining charger twins" line reached the log
  after the assertion had already run. The machine was busy because other agents were building at the same time.
- Suggested fix for whoever owns Chapter 2 code: have the test poll for the message, using `AwaitAssert` or a short
  retry loop with a timeout, or have the shutdown task flush or await the log before it returns. Every later tag inherits
  this test, so it can fail any `verify-tags.sh` run.

## ch04 (testing agent, 2026-11-04): same Chapter 2 hosted test race, now failing about half the time

**Status (cleanup job 1): RESOLVED.** Ch02.Hosted shutdown-log race: RESOLVED by `logger-race`, amended into `ch02-s09` (see the ch03 entry).

The sample repo is unchanged. Tested in detached worktrees with SDK 11.0.100-rc.3.

- Every ch04 tag (`ch04-s01` to `ch04-s08`) passes `dotnet build Ohmline.slnx -warnaserror` with 0 warnings and 0 errors.
  `dotnet test --solution Ohmline.slnx` reports `total: 137`, `failed: 0` at `ch04-s02` to `ch04-s08` on the first run.
- At `ch04-s01` the first run reported `total: 137`, `failed: 1`, `succeeded: 136`, exit code 2. Over 13 runs at that
  tag, 7 failed and 6 passed, and every failure was the same test as in the ch03 entry above:
  `Ch02.Hosted.Tests.ChargerAppTests.Given_a_running_host_when_it_stops_then_coordinated_shutdown_drains_first`
  (`_logs.Messages should contain an element satisfying the condition m.Contains("draining charger twins") but does not`,
  `learn/Ch02.Hosted.Tests/ChargerAppTests.cs:69`). Load average was about 1.2 to 1.6, with other agents building.
- `ch04-s01` adds only three Markdown files (`git diff ch03-s09 ch04-s01 -- ':!docs'` is empty), so this is not a Chapter 4
  regression. But a failure rate near 50% means `verify-tags.sh` will fail on random tags. The fix suggested above
  (poll with `AwaitAssert` or a retry loop, or flush the log before `StopAsync` returns) is now urgent.
- No book change is needed: Chapter 4 says `total: 137` and `failed: 0`, which is what a passing run reports.

## ch05 (testing agent, 2026-11-04): the Chapter 2 hosted test race again, at `ch05-s01`

**Status (cleanup job 1): RESOLVED.** Ch02.Hosted shutdown-log race: RESOLVED by `logger-race`, amended into `ch02-s09` (see the ch03 entry).

The sample repo is unchanged. Tested in detached worktrees with SDK 11.0.100-rc.3.

- Every ch05 tag (`ch05-s01` to `ch05-s09`) passes `dotnet build Ohmline.slnx -warnaserror` with 0 warnings and 0 errors.
  On the first run, `dotnet test --solution Ohmline.slnx` passes at `ch05-s02` to `ch05-s09`, with totals of
  137/165/165/166/166/184/184/184.
- At `ch05-s01` the first run reported `total: 137`, `failed: 1`, `succeeded: 136`, exit code 2. In 5 reruns with
  `--no-build`, 1 failed and 4 passed. Every failure was the same test as in the ch03 and ch04 entries:
  `Ch02.Hosted.Tests.ChargerAppTests.Given_a_running_host_when_it_stops_then_coordinated_shutdown_drains_first`
  (`_logs.Messages should contain an element satisfying the condition m.Contains("draining charger twins") but does not`,
  `learn/Ch02.Hosted.Tests/ChargerAppTests.cs:69`). Load average was about 1.8 to 3.2, with other agents building.
- `ch05-s01` adds only the six canvases under `docs/canvases/`, so this is not a Chapter 5 regression. The test still
  needs the fix described in the ch03 entry. No book change is needed.

## ch06 (coder, 2026-11-04): `SiteId` format tightened in `docs/naming-rules.md` (from ch04-s05)

**Status:** no action needed (documentation fix forward; not part of job 1).

- `ch06-s04` changes the `SiteId` row of `docs/naming-rules.md` from "lower-case kebab case" to "lower-case kebab case,
  starting with a three-letter area code". `SiteId` validates `^[a-z]{3}(-[a-z0-9]+)+$`, which is the shape
  `docs/acl/assettrack.md` (ch05-s06, rule 2) already demanded and the shape every site in the sample has (`slc-depot`,
  `ogd-library`, ...). Fixed forward; no tagged commit was amended.
- Book impact: none found. Chapter 4's text does not quote the row; anyone quoting `docs/naming-rules.md` at a ch04
  or ch05 tag still gets the old wording, which is correct for that tag.
- Also in `ch06-s02`: `src/Ohmline.SharedKernel/GOVERNANCE.md` gains version rows for 6.1 (1.1.0) and 6.2 (2.0.0);
  6.1 forgot its own row. Chapter 5's "The kernel has its own semantic version (1.0.0)" stays true at `ch05-s05`.

## ch06 (testing agent, 2026-11-04): the Chapter 2 hosted test race again, at `ch06-s01` and `ch06-s04`

**Status (cleanup job 1): RESOLVED.** Ch02.Hosted shutdown-log race: RESOLVED by `logger-race`, amended into `ch02-s09` (see the ch03 entry).

The sample repo is unchanged. Tested in detached worktrees with SDK 11.0.100-rc.3.

- Every ch06 tag (`ch06-s01` to `ch06-s08`) passes `dotnet build Ohmline.slnx -warnaserror` with 0 warnings and 0 errors.
  On the first run, `dotnet test --solution Ohmline.slnx` passes at `ch06-s02`, `-s03` and `-s05` to `-s08`, with totals of
  216/243/302/306/381/393.
- On the first run, `ch06-s01` reported `total: 201`, `failed: 1` and `ch06-s04` reported `total: 292`, `failed: 1`, both
  with exit code 2. Both failures were the same test as in the ch03, ch04 and ch05 entries:
  `Ch02.Hosted.Tests.ChargerAppTests.Given_a_running_host_when_it_stops_then_coordinated_shutdown_drains_first`
  (`_logs.Messages should contain an element satisfying the condition m.Contains("draining charger twins") but does not`,
  `learn/Ch02.Hosted.Tests/ChargerAppTests.cs:69`). Load average was about 10 to 15, with other agents building.
  3 reruns at each of the two tags all passed (201/0 and 292/0).
- Neither tag touches `learn/`, so this is not a Chapter 6 regression. The test still needs the fix described in the
  ch03 entry. No book change is needed.

## ch07 (coder, 2026-11-04): fixes forward to Chapter 4 to 6 material

**Status:** the fixes forward stand (not part of job 1). The Chapter 2 race in the last bullet: RESOLVED by `logger-race`, amended into `ch02-s09`.

- **`PriorityClass` moved** from `src/SiteEnergy/Ohmline.SiteEnergy.Domain/Sites/PriorityClass.cs` (namespace
  `Ohmline.SiteEnergy.Domain.Sites`, added at `ch06-s07`) to `src/SiteEnergy/Ohmline.SiteEnergy.Contracts/Budgets/PriorityClass.cs`
  (namespace `Ohmline.SiteEnergy.Contracts.Budgets`) in `ch07-s08` (`git mv`, so `git log --follow` keeps its history).
  Reason: `ConfigureSite` is a command and lives in `Ohmline.SiteEnergy.Contracts`, which may not reference the Domain
  (architecture §4), and it must carry each connector's priority class. The type itself is unchanged (still a closed
  record with `Fleet` and `Public`); its summary gained one sentence saying why it lives in the contracts.
  `ConnectorConfiguration.cs`, `AssetTrackTranslator.cs` and three test files gained a `using`. `docs/highlighted-core.md`
  names the new location. Book impact: Chapter 6 quotes at `ch06-s07` stay correct for that tag; any later chapter must
  use the new path and namespace.
- **`tests/Ohmline.Charging.Domain.Tests/ChargePoints/ConnectorTests.cs`** (from `ch06-s06`): its private field
  `Available` became `Free` in `ch07-s03`, because the new state case type `Available` would otherwise be hidden by the
  field. `ConnectorState.Available` / `new ConnectorState("Occupied")` became `new Available()` / `new Occupied(...)`.
  Book impact: none for Chapter 6, which quotes the file at `ch06-s06`.
- **`docs/eventstorming/design-level.md`** (from `ch04-s04`) and **`docs/canvases/bc-charging.md`** (from `ch05-s01`)
  gained the two operator commands `MakeConnectorUnavailable` / `MakeConnectorAvailable` (events
  `ConnectorMadeUnavailable` / `ConnectorMadeAvailable`) in `ch07-s02`. Without them the glossary's connector status
  *Unavailable* could never be reached. Book impact: Chapters 4 and 5 quote those files at their own tags.
- **The Chapter 2 hosted-test race is still there.** `tools/verify-tags.sh 'ch07-*'` failed `ch07-s02` twice in a row with
  `Ch02.Hosted.Tests.ChargerAppTests.Given_a_running_host_when_it_stops_then_coordinated_shutdown_drains_first`
  (load average 7.6 to 8.0, other agents building), then passed three times in a row; every other ch07 tag passed on the
  first run. Same cause as the ch03 to ch05 entries. It was not fixed in Chapter 7 because the fix belongs to Chapter 2's
  learning project and would be unrelated churn in a Chapter 7 diff.

## ch07 (testing agent, 2026-11-05): the Chapter 2 hosted test race again, at `ch07-s01` and `ch07-s08`

**Status (cleanup job 1): RESOLVED.** Ch02.Hosted shutdown-log race: RESOLVED by `logger-race`, amended into `ch02-s09` (see the ch03 entry).

- Every ch07 tag builds with `dotnet build Ohmline.slnx -warnaserror` (0 warnings, 0 errors). On the first
  `dotnet test --solution Ohmline.slnx` run, `ch07-s01` (393 tests) and `ch07-s08` (566 tests) each had one failure, the same
  test as in the ch03 to ch06 entries:
  `Ch02.Hosted.Tests.ChargerAppTests.Given_a_running_host_when_it_stops_then_coordinated_shutdown_drains_first`
  (`_logs.Messages should contain an element satisfying the condition m.Contains("draining charger twins") but does not`,
  `learn/Ch02.Hosted.Tests/ChargerAppTests.cs:69`). Load average was about 6 to 8.
- Reruns: `ch07-s01` passed 3 of 3 (393/0). At `ch07-s08` the first rerun failed again on the same test (566/1), and the next 2
  passed (566/0). The other six tags passed on the first run.
- No ch07 tag touches `learn/`, so this is not a Chapter 7 regression. The test still needs the fix described in the ch03
  entry. No book change is needed.

## ch08 (coder, 2026-11-05): changes that touch Chapter 5 to 7 material

**Status:** no action needed for job 1. The Chapter 2 race in the last bullet: RESOLVED by `logger-race` (`ch02-s09`). The suggested amendment to `sample-architecture.md` §4 is still the editor's call.

- **Contracts may now reference an upstream context's contracts** (`ch08-s02`). `StartSession`
  (in `Ohmline.Charging.Contracts`) must carry Tariffs' `TariffSnapshot`, and a contracts project could
  reference only the shared kernel. `tests/Ohmline.ArchitectureTests/ContractsPurityTests.cs` was
  renamed from `A_contracts_assembly_references_only_the_shared_kernel_and_runtime` to
  `A_contracts_assembly_references_only_kernel_contracts_and_runtime` and now also allows
  `Ohmline.*.Contracts`. `Ohmline.Charging.Contracts.csproj` references `Ohmline.Tariffs.Contracts`, and
  its comment and `src/Charging/Ohmline.Charging.Contracts/README.md` say so. Domains, actors and packages
  stay forbidden. Book impact: Chapter 5 (and `sample-architecture.md` §4, whose table lists only
  "SharedKernel" for `*.Contracts`) says contracts reference the shared kernel and nothing else; from
  `ch08-s02` on, a contracts project may also reference the contracts of a context it is downstream of
  (Charging is Tariffs' customer, `docs/context-map.md`). The editor may want to amend §4.
- **The session events lost their own timestamps** (`ch08-s01`): `SessionStarted.StartedAt`,
  `ChargingSuspended.SuspendedAt`, `ChargingResumed.ResumedAt` and `SessionEnded.EndedAt` became the
  base record's `OccurredAt`, which every event now has. `SessionStarted`/`StartSession` also lost
  `TariffId`, `TariffVersion` and `PricePerKwh` to `TariffSnapshot Tariff` (`ch08-s02`), and
  `ChargingSuspended` was split (`ch08-s06`). Every command gained `CommandId` as its first positional
  member. Book impact: none for Chapter 7, which quotes these files at `ch07-*` tags; any later chapter
  must quote from `ch08-*` or later.
- **Living docs updated for 8.6:** `docs/glossary.md` (row "Suspended" became "Suspended by EV / by
  charger", as the style guide's glossary already has it; new row "Site-local time" in `ch08-s07`),
  `docs/canvases/aggregate-charging-session.md` (state diagram, `SuspendCharging` row, created events, and
  the `IdleFeePolicy` corrective policy from `ch08-s05`), `docs/canvases/bc-device-gateway.md`
  (`SuspendedEVSE`), and one sentence in `docs/eventstorming/design-level.md` saying how 8.6 answered H2.
  Chapter 4, 5 and 7 quotes of these files at their own tags stay correct.
- **No tagged commit was amended.** `tools/verify-tags.sh 'ch08-*'` hit the known Chapter 2 hosted-test
  race once (`Ch02.Hosted.Tests.ChargerAppTests.Given_a_running_host_when_it_stops_then_coordinated_shutdown_drains_first`
  at `ch08-s02`; 600 of 601 passed); see the Verification section of `code-notes/ch08.md`.

## ch09 (coder, 2026-11-05): fixes forward to Chapter 7 material

**Status:** no action needed (fixes forward with their own book coverage; not part of job 1).

- **A driver who reserves again after their own reservation has expired now gets a new one**
  (`ch09-s03`, found by the reservation scenario table). From `ch07-s04` to `ch08-s07`,
  `ChargePoint.Decide` checked "same token" before "expired", so `ReserveConnector` from the token
  that held an expired reservation returned no change: the lapsed hold stayed in the state, and the
  next `ExpireReservation` freed the connector under the driver. The two rows of the connector
  decision table now run in the other order (expired first: `ReservationExpired`, then a new
  `ConnectorReserved` for 15 minutes from the decision). Book impact: Chapter 7's 7.4 listing of the
  decision table at `ch07-s04` shows the old order; the text there should not claim the rows are
  order-independent. 9.3 tells the story of the fix. `docs/canvases/aggregate-charge-point.md`'s
  `ReserveConnector` row now says so too. `OccupyConnector` was left as it was: the driver who holds
  the reservation may still plug in after it has lapsed, as long as nobody else has taken it.
- **`DecisionAssertions` (both test projects) are gone** (`ch09-s02`): `ShouldAccept`/`ShouldReject`
  became `DeciderSpec`'s `Then`/`ThenRejects`. Chapter 7 quotes at `ch07-*` tags are unaffected.

## ch08 testing (2026-11-05)

**Status (cleanup job 1): RESOLVED.** Ch02.Hosted shutdown-log race: RESOLVED by `logger-race`, amended into `ch02-s09`.

- All seven tags ch08-s01..s07 build with `-warnaserror` (0 warnings, 0 errors), and tests pass with totals of 573/601/616/636/643/654/662, which match the book. On the first runs of ch08-s01 and ch08-s07, the known flaky
  `Ch02.Hosted.Tests.ChargerAppTests.Given_a_running_host_when_it_stops_then_coordinated_shutdown_drains_first`
  failed (assertion that the logs contain "draining charger twins"; the message was logged just after the assertion). Each tag failed only that test, and each passed on its rerun (1/1). These are not ch08 regressions. The race is still open and now shows up on most chapters' first runs.

## ch10 (coder, 2026-11-05): fixes forward to Chapter 7 material

**Status:** no action needed (not part of job 1).

- **`RegisterChargePoint` now compares its connector list by value** (`ch10-s03`). From `ch07-s02`
  it was a plain positional record holding an `ImmutableArray<Power>`, so two equal commands were
  unequal (the array compares by reference), while its event `ChargePointRegistered` already had
  a value `Equals`. Found by the serializer's round-trip test. The command got the same
  `Equals`/`GetHashCode` pair as the event. Book impact: none for Chapter 7, whose 7.2 listing of
  `ChargePointCommand.cs` at `ch07-s02` stays correct for that tag; quotes from `ch10-s03` on show
  the two extra members.

## ch09 (tester, 2026-11-05): flaky Ch02.Hosted shutdown test again

**Status (cleanup job 1): RESOLVED.** Ch02.Hosted shutdown-log race: RESOLVED by `logger-race`, amended into `ch02-s09`. The `ConfigureSite` bullet needs no action.

- **`ch09-s01` first run: 661/662.** In a clean worktree, `dotnet build Ohmline.slnx -warnaserror`
  gave 0 warnings and 0 errors. `dotnet test --solution Ohmline.slnx` then failed only
  `Ch02.Hosted.Tests.ChargerAppTests.Given_a_running_host_when_it_stops_then_coordinated_shutdown_drains_first`
  (`_logs.Messages should contain an element satisfying the condition m.Contains("draining charger twins") but does not`,
  ChargerAppTests.cs:69). It is the known shutdown-log race. Three reruns at the same tag gave
  fail, pass, pass, and the fast-loop and exit-code-8 commands from section 9.1 behaved as the book says. `ch09-s02`
  to `ch09-s06` passed on their first runs (668/708/723/746/809). This is not a ch09 regression,
  and the book needs no change.
- **`ConfigureSite` now compares its connector list by value** (`ch10-s10`), for the same reason
  as `RegisterChargePoint`: it holds an `ImmutableArray<ConnectorLimits>` and was unequal to an
  equal copy. Found by Site Energy's serializer round-trip test. Book impact: Chapter 7's 7.8
  quote of `SitePowerBudgetCommand.cs` at `ch07-s08` stays correct for that tag.

## ch10 testing note (coder, 2026-11-05)

**Status (cleanup job 1): RESOLVED.** Ch02.Hosted shutdown-log race: RESOLVED by `logger-race`, amended into `ch02-s09`.

- `tools/verify-tags.sh 'ch10-*'`: the known Chapter 2 hosted-test race failed `ch10-s02` and
  `ch10-s09` on the first run (one test each); both passed on rerun. No Chapter 10 change touches
  `learn/Ch02.*`.

## ch11 (coder, 2026-11-05): fixes forward to Chapter 10 material

**Status:** no action needed (fixed forward at `ch11-s04`; book 11.4 tells the story; not part of job 1).

- **`TwoIncarnationsTests` (`ch10-s09`) became racy once reservations expire by timer
  (`ch11-s04`).** Its second incarnation used a clock 16 minutes after the reservation, so on
  recovery its new reservation timer was already due and wrote `ReservationExpired` at once,
  racing the first incarnation for sequence number 3. Fixed forward in `ch11-s04`: the second
  incarnation's clock starts at +14 minutes (not lapsed when it recovers) and is advanced by two
  minutes just before it decides, so the failing write is again the `ReservationExpired` +
  `ConnectorReserved` batch at 3 and 4; the fresh incarnation uses the original clock. Book
  impact: none for section 10.9, which quotes the test at `ch10-s09`, where it is correct.

## ch11 (testing agent, 2026-11-05): flaky Ch02.Hosted shutdown test again, at `ch11-s01`

**Status (cleanup job 1): RESOLVED.** Ch02.Hosted shutdown-log race: RESOLVED by `logger-race`, amended into `ch02-s09`.

- **`ch11-s01` first run: 1095/1096.** In a clean worktree, `dotnet build Ohmline.slnx -warnaserror`
  gave 0 warnings and 0 errors. `dotnet test --solution Ohmline.slnx` then failed only
  `Ch02.Hosted.Tests.ChargerAppTests.Given_a_running_host_when_it_stops_then_coordinated_shutdown_drains_first`
  (`_logs.Messages should contain an element satisfying the condition m.Contains("draining charger twins")`).
  This is the known shutdown-log race. Two reruns in a fresh worktree at the same tag gave pass
  (1096/1096) and fail (the same test only). Load average was about 4 to 11, with other agents building.
  `ch11-s02` to `ch11-s08` passed on their first runs (1116/1119/1124/1128/1166/1170). No Chapter 11
  change touches `learn/Ch02.*`. This is not a ch11 regression, and the book needs no change.

## ch12 (coder, 2026-11-05): fixes forward to Chapters 10 and 11 material

**Status (cleanup job 1):** the listing flake ("Observed flake") is RESOLVED by `listing-snapshot-settled`, amended into `ch11-s07` (ch13-s03's `SettledAsync`, backported verbatim). The `RegionTests` flake stays fixed forward at `ch13-s02` only; it is latent at `ch12-s09`..`ch12-s11`, not seen in any re-verification run, and the book's 12.9 Tip tells readers about it. The other bullets need no action.

No tagged commit was amended. All of these arrive in Chapter 12 tags; quotes of earlier tags
stay correct for those tags.

- **`ChargePointDirectory` and its backoff supervisor are deleted at `ch12-s02`** (as planned
  in the ch11 notes). The shard region starts an entity again on its next message; the ch11-s06
  test `PersistenceFailureTests.Given_a_failed_write_then_the_supervisor_starts_the_charge_point_again`
  is gone with it, and its successor is
  `Ohmline.Node.Tests.RegionTests.Given_a_failed_write_then_the_next_message_starts_the_entity_again`
  (`ch12-s09`). `SqliteNode` (Charging actor tests) now starts a cluster of one and asks the
  `ChargePointRegion`. ADR 0008's "Until Chapter 12 ... from Chapter 12" wording stays true.
- **`DeciderActor`'s snapshot hooks change shape at `ch12-s06`**: `ToSnapshot(TState)` becomes
  `ToSnapshot(TState, ImmutableArray<RecentCommand>)` and `FromSnapshot(object)` returns
  `(TState State, ImmutableArray<RecentCommand> Recent)`; every actor also implements
  `CommandIdOf(TCommand)` and `CausationOf(TEvent)`. The four snapshot DTOs gain an optional
  `RecentCommands` property under the same `.v1` manifests (tolerant reader; the golden files of
  the four snapshot manifests changed, and
  `ChargePointSnapshotTests.Given_a_snapshot_stored_before_the_window_existed_then_it_reads_with_none`
  reads a ch11-shaped snapshot). Section 11.2's quotes at `ch11-s02` are unaffected.
- **`Known.Command` in `Ohmline.Charging.Actors.Tests` is a new id on every read from
  `ch12-s06`** (it was one fixed id for every command; the ch10/11 comment said "the shell's
  duplicate check is section 12.6"). With the duplicate check, a history stamped with the same id
  as the command under test made the shell answer without deciding. Expectations for the events
  a command causes now pass the command's id (`Stamped(..., by: reserve.CommandId)`,
  `Registered(by: ...)`). Same change in `SitePowerBudgetActorTests` (history stamps get fresh
  ids). Book impact: section 11.8's `ChargePointActorTests` quote at `ch11-s08` is unchanged.
- **Observed flake (not fixed):** `ChargePointListingActorTests.Given_revisions_when_a_new_incarnation_starts_then_the_snapshot_is_enough`
  failed once in about ten full runs at `ch12-s08` ("Only received 0/1 messages that matched
  filter [Info when Message contains ": snapshot at 3, 0 events replayed"]"; the new incarnation
  logged "no snapshot, 0 events replayed"). It passed 12 out of 12 reruns. Suspected: the
  persistence test kit's in-memory snapshot store running a save and an older delete
  concurrently. The test is from `ch11-s07`; worth a look by the Chapter 20 coder.
- **Known flake to fix forward in the next commit (Chapter 13):**
  `Ohmline.Node.Tests.RegionTests.Given_a_failed_write_then_the_next_message_starts_the_entity_again`
  (`ch12-s09`) failed once in `tools/verify-tags.sh 'ch12-*'` at `ch12-s10` ("AwaitAssert failed,
  timeout [00:00:10] is over after [1] attempts"), then passed on rerun and in 4 + 4 local runs.
  Cause: inside `AwaitAssertAsync(..., Patience)` it asks with `AskAsync`, whose Ask timeout is
  also `Patience` (10 s). When the first question reaches the stopping entity it becomes a dead
  letter, that one Ask waits all 10 s, and the assertion gets no second attempt. Fix: ask with a
  short timeout inside the loop (for example an `AskAsync<TKey, TReply>(message, TimeSpan
  timeout)` overload with 500 ms) and keep `Patience` for the `AwaitAssertAsync`. Not fixed in
  Chapter 12 because every Chapter 12 tag was already cut, and tags are never moved.

## ch13 (coder, 2026-11-05): fixes forward to Chapters 11 and 12 test code

**Status (cleanup job 1):** the `ChargePointListingActorTests` fix now lives at `ch11-s07` (`listing-snapshot-settled`); `ch13-s03` no longer touches that test, although its commit message still says so. The `RegionTests` fix stays at `ch13-s02`.

No tagged commit was amended. Both fixes are in Chapter 13 tags; quotes of earlier tags stay
correct for those tags.

- **`RegionTests.Given_a_failed_write_then_the_next_message_starts_the_entity_again`
  (`ch12-s09`), fixed at `ch13-s02`** as the ch12 errata proposed: `ShardedNode` gains
  `AskAsync<TKey, TReply>(object message, TimeSpan timeout)`, and the status asked inside
  `AwaitAssertAsync(..., Patience)` now uses a 500 ms Ask, so a question lost to the stopping
  entity leaves time for another attempt. At the same tag the `Begin()` session command moved
  from `ShardRegionTests` into `Known` (Charging actor tests), because the journal tour uses it
  too.
- **`ChargePointListingActorTests.Given_revisions_when_a_new_incarnation_starts_then_the_snapshot_is_enough`
  (`ch11-s07`), fixed at `ch13-s03`.** The observed flake from the ch12 errata became frequent
  once Chapter 13 added SQLite and Postgres tests to the run (it failed in 3 of 5 full runs at
  `ch13-s02`/`ch13-s03` before the fix; 0 of 3 after). Cause, as suspected: the persistence test
  kit's in-memory snapshot store can lose a snapshot that is saved while an older one is being
  deleted. The test now waits after each revision until its round is over (`SettledAsync`: the
  journal is empty and `LoadSnapshot` with `maxSequenceNr: version - 1` finds nothing) before it
  sends the next one. Production code is unchanged (the SQL snapshot store has no such race).
  Book impact: section 11.7 quotes the test at `ch11-s07`, where it is unchanged.

## ch12 (tester, 2026-11-05): tag verification

**Status (cleanup job 1): RESOLVED** by `listing-snapshot-settled`, amended into `ch11-s07`. The suggested 12.9 note is no longer needed.

All 11 tags (`ch12-s01`..`ch12-s11`) build with `-warnaserror` (0 warnings, 0 errors), and the test
totals match the book (1170/1235/1236/1242/1279/1293/1408/1469/1476/1476/1481). No repo change.

- **Flake seen more often than "once in about ten runs":** `ChargePointListingActorTests.Given_revisions_when_a_new_incarnation_starts_then_the_snapshot_is_enough`
  failed on the first runs of `ch12-s02` and `ch12-s09` (same message: "Only received 0/1 messages
  that matched filter [Info when Message contains ": snapshot at 3, 0 events replayed"]", new
  incarnation logged "recovered to sequence number 3 ... no snapshot, 0 events replayed").
  Reruns: `ch12-s02` passed 1/1; `ch12-s09` failed it again twice (once together with the known
  `Ch02.Hosted.Tests.ChargerAppTests.Given_a_running_host_when_it_stops_then_coordinated_shutdown_drains_first`
  shutdown race) and passed on the fourth run. So roughly 4 failures in 14 full runs on this
  machine. Already fixed forward at `ch13-s03` (see the ch13 entry above); a reader at a Chapter 12
  tag may hit it. The book's tip in section 12.9 mentions only the `RegionTests` flake; consider a
  one-line note that this listing test (from `ch11-s07`) can also fail now and then at these tags.

## ch13 (tester, 2026-11-05): tag verification

**Status (cleanup job 1): RESOLVED.** Chapter 2 race: `logger-race` (`ch02-s09`). SQLite 5 s asks: `sqlite-node-ask` (`ch10-s07`), `two-incarnations-ask` (`ch10-s09`), `state-sourcing-ask` (`ch11-s07`), all 20 s × TimeFactor. `TariffEndpointsTests` 500 ms ask: `edge-testkit-ask` (`ch12-s05`, 3 s).

All 7 tags (`ch13-s02`..`ch13-s08`) build with `-warnaserror` (0 warnings, 0 errors). The test totals match
the book (1545/1549/1552/1564/1567/1573/1594), and every tag has at least one fully green
`dotnet test --solution Ohmline.slnx` run. No repo change. Flakes seen while other agents loaded the machine
(load average about 10 to 11):

- **Known Chapter 2 shutdown race:**
  `Ch02.Hosted.Tests.ChargerAppTests.Given_a_running_host_when_it_stops_then_coordinated_shutdown_drains_first`
  ("_logs.Messages should contain an element satisfying the condition"). Seen at `ch13-s03` in runs
  1, 2, 3 and 5 of 5 (run 4 green), at `ch13-s07` in runs 2 and 4 of 5, and at `ch13-s08` in run 1
  of 2. It is the most frequent flake now; at `ch13-s03` it failed 4 of 5 runs.
- **Load-induced Ask timeouts, once:** the first full run at `ch13-s07` failed 5 tests. Four were
  `AskTimeoutException : Timeout after 5.00 seconds` in `Ohmline.Charging.Actors.Tests`:
  `StateSourcingTourTests.Given_a_listing_when_the_node_restarts_then_its_snapshot_is_enough`,
  `ShardRegionTests.Given_a_fake_clock_among_the_services_then_the_entities_decide_by_it`,
  `JournalTourTests.Given_a_session_event_then_its_tag_is_charging_session` and
  `NodeRestartTests.Given_a_reservation_when_the_node_restarts_then_the_reservation_is_kept`, all at
  about 9.3 s. The fifth was
  `Ohmline.Charging.Api.Tests.TariffEndpointsTests.Given_a_tariff_then_the_book_is_told_to_publish_it`
  (a `ProblemHttpResult` instead of `Accepted<CommandAcceptedResponse>`). None recurred in 4 more full
  runs or 3 runs of `Ohmline.Charging.Actors.Tests` alone (347/347). The likely cause is the fixed 5 s
  Ask in `SqliteNode.AskAsync` (tests/Ohmline.Charging.Actors.Tests/Sqlite/SqliteNode.cs:76) while a
  SQLite node starts under heavy CPU load. Consider a longer Ask there in a later fix forward.
- The book's fast-loop claim at `ch13-s08` (25 tests, no container) was confirmed: `total: 25`, `failed: 0`.

## ch14 (coder, 2026-11-05): known flake seen again

**Status (cleanup job 1): RESOLVED.** Ch02.Hosted shutdown-log race: RESOLVED by `logger-race`, amended into `ch02-s09`.

- `Ch02.Hosted.Tests.ChargerAppTests.Given_a_running_host_when_it_stops_then_coordinated_shutdown_drains_first`
  failed once each at `ch14-s02` and `ch14-s06` in the first `tools/verify-tags.sh 'ch14-*'` run
  (the other 5 tags passed); both tags passed on rerun. Same shutdown-log race as before, not related
  to Chapter 14. No code changed.

## ch15-s01 (coder, 2026-11-05): fix forward in `DriverAppTests.ChargeAsync`

**Status (cleanup job 1): RESOLVED** by `driver-app-charge-waits`, amended into `ch14-s06` (this ch15-s01 change, backported verbatim). The `ch15-s01` commit no longer touches `DriverAppTests.cs`; book 15.1's `--stat` total is now `11 files changed, 360 insertions(+), 1 deletion(-)`.

- `tests/Ohmline.IntegrationTests/DriverAppTests.cs` (from `ch14-s06`) asserted that
  `GET /sessions/{id}?minVersion=1` answers 200 right after `POST /sessions` on a node that had just
  started. Read-your-writes waits 2 s, and on a fresh node the projection daemons may all still be
  starting (schema step, then the stream). With a fifth stream (`session-ended-subscriber-v1`,
  ch15-s01) it failed 2 runs in 3 with `HttpStatusCode.Accepted` instead of `OK`
  (`Given_the_driver_app_then_its_cards_and_its_feed_show_the_session`,
  `Given_one_session_then_it_has_a_row_in_all_three_read_models`). The helper now checks the 202
  and waits (20 s patience) for the `session_history` row and then the `driver_app_cards` row, as
  `ProjectionLagTests` already does for its first session. 3 of 3 reruns green. Chapter 14's text
  does not quote `ChargeAsync`.

## ch14 (testing agent, 2026-11-05): `ch14-s01` fails one integration test, 2 runs of 2

**Status (cleanup job 1): RESOLVED.** The 42P01 race: `fixture-ensures-schema`, amended into `ch13-s08` (`PostgresFixture` creates the read-model schemas before any test node starts; superseded by the migrator at `ch20-s08`). `ch14-s01` green 3 of 3. The ch14-s03 SQLite asks: `sqlite-node-ask` (`ch10-s07`) and its siblings.

- `Ohmline.IntegrationTests.ReadYourWritesTests.Given_a_session_the_history_has_never_seen_then_404_without_min_version`
  failed in both full `dotnet test --solution Ohmline.slnx` runs at `ch14-s01` (fresh detached
  worktree, `total: 1607`, `failed: 1`): `read.StatusCode should be HttpStatusCode.NotFound but was
  HttpStatusCode.InternalServerError` (tests/Ohmline.IntegrationTests/ReadYourWritesTests.cs:71).
  The node's log shows the cause: `Npgsql.PostgresException 42P01: relation
  "read_models.session_history" does not exist` thrown from `SessionEndpoint.FindAsync`. The test
  starts an `OhmlineNode` and reads `GET /sessions/{new guid}` at once, without waiting for anything;
  when it is the first test of the `OneNodeAtATime` collection to touch a fresh container, the
  `ProjectionDaemon` has not yet run `EnsureSchemaExists`, so the table is missing and the endpoint
  answers 500. The same test passed at `ch14-s02`..`ch14-s07` in this run (the file is unchanged
  through `ch14-s07`, so the race is latent there too; test order differs per worktree path). It is
  a test-ordering race, not a Chapter 14 design fault, but the book's "Try it" at `ch14-s01` promises
  `failed: 0`. Suggested fix forward: have the test (or `OhmlineNode`) wait until the read-model
  schema exists, e.g. until `GET /projections/status` answers 200 or `session_history` exists,
  before the first read; alternatively map 42P01 in the query endpoints to 503 with `Retry-After`.
- `ch14-s03`, first run only: 6 `Ohmline.Charging.Actors.Tests` SQLite tests (`TwoIncarnationsTests`,
  `StateSourcingTourTests`, `NodeRestartTests`, `ShardRegionTests`, `JournalTourTests`,
  `PoisonEventTests`) failed with `Akka.Actor.AskTimeoutException : Timeout after 5.00 seconds`
  under load (load average about 9). The rerun was green (`total: 1614`, `failed: 0`). Same 5 s Ask
  under CPU load as noted for ch13-s07.
- All 7 tags built with `-warnaserror`, 0 warnings, 0 errors. Test totals 1607/1607/1614/1626/1628/1655/1661
  match the book; s02, s04, s05, s06 and s07 were green on the first run.

## ch15-s02 (coder, 2026-11-05): `PolicyTests` narrowed for the journey's references

**Status:** no action needed (not part of job 1).

- `tests/Ohmline.Charging.Domain.Tests/Policies/PolicyTests.cs`,
  `Given_a_policy_then_its_intents_are_charging_words_not_other_contexts_commands` (from `ch08-s05`)
  asserted that the whole `Ohmline.Charging.Domain` assembly references no `Ohmline.SiteEnergy*` or
  `Ohmline.DriverAccounts*` assembly. From `ch15-s02` the charging journey conforms to their
  Contracts (outline 15.2, architecture §4), so the assembly references them. The test now checks
  what it meant: no property of any `ChargingIntent` case has a type from those contexts. If 8.5
  says the assembly references neither context, that sentence holds only up to `ch14-s07`.

## ch15-s06 (coder, 2026-11-05): fix forward in `ReplayTests.ChargeAndStopAsync`

**Status (cleanup job 1): RESOLVED** by `replay-charge-and-stop-waits`, amended into `ch14-s07` (this ch15-s06 change, backported verbatim). The `ch15-s06` commit no longer touches `ReplayTests.cs`; book 15.6's `--stat` total is now `22 files changed, 586 insertions(+), 129 deletions(-)`.

- `tests/Ohmline.IntegrationTests/ReplayTests.cs` (from `ch14-s07`) had the same race as
  `DriverAppTests` (see ch15-s01 above): it asserted `GET /sessions/{id}?minVersion=3` answers 200
  right after stopping the first session on a freshly started node. With six projection streams
  (ch15-s01 and ch15-s04 each add one) it failed 2 full-solution runs in 6 (`Accepted` instead of
  `OK` after about 3.3 s), never when the project ran alone. The helper now checks the 202, and the
  test waits (30 s patience) for the `session_history` row with status `ended` before reading the
  rows it compares. Chapter 14 quotes neither line.
- ADR 0009's durable-name table gains, in this commit, rows that belong to earlier tags of this
  chapter: the `charging-journey` tag (ch15-s03), the projection ids
  `session-ended-subscriber-v1` (ch15-s01) and `journey-session-relay-v1` (ch15-s04).

## ch16-s02 (fixed forward): read-model schema creation could deadlock with running projections

**Status:** closed (fixed in the `ch16-s02` commit itself; not part of job 1).

- **Where:** `src/Charging/Ohmline.Charging.ReadModels/ReadModelSchema.cs`, since `ch14-s03`
  (the `ALTER TABLE read_models.projection_offsets ADD COLUMN IF NOT EXISTS last_event_at`).
- **Symptom:** with the seventh projection stream (`integration-relay-v1`, ch16-s01), the node's
  logs in `FeedTests` showed `40P01: deadlock detected` in `NpgsqlOffsetStore.SaveAsync` (1 run
  in 3); the stream restarted and the tests still passed.
- **Cause:** every `ProjectionDaemon` instance runs `ReadModelSchema.EnsureAsync` when it starts.
  `ALTER TABLE ... ADD COLUMN IF NOT EXISTS` takes an ACCESS EXCLUSIVE lock on
  `projection_offsets` even when the column exists; `CREATE INDEX IF NOT EXISTS` then waits for a
  SHARE lock on a read-model table that a stream already running holds, while that stream waits
  to write its offset.
- **Fix (in the ch16-s02 commit):** the ALTER runs inside a `DO` block only when
  `information_schema.columns` says the column is missing. 3 of 3 integration runs afterwards had
  no deadlock. Chapter 20's migrator removes schema creation from the nodes altogether.

## ch15 (testing agent, 2026-11-05): `ch15-s07` fails one integration test in the verify worktree, 4 runs of 4

**Status (cleanup job 1): RESOLVED** by `fixture-ensures-schema`, amended into `ch13-s08`. `ch15-s07` green 3 of 3 (failed 4 of 4 before).

- `Ohmline.IntegrationTests.QueryApiTests.Given_a_site_with_no_connectors_on_the_board_then_404`
  failed in both full `dotnet test --solution Ohmline.slnx` runs at `ch15-s07` (fresh detached
  worktree at `.../scratchpad/verify-ch15/ch15-s07`, build `-warnaserror` 0 warnings, `total: 1905`,
  `failed: 1`) and in 2 of 2 runs of the integration project alone in that same path:
  `response.StatusCode should be HttpStatusCode.NotFound but was HttpStatusCode.InternalServerError`
  (tests/Ohmline.IntegrationTests/QueryApiTests.cs:82). The node's log shows
  `Npgsql.PostgresException 42P01: relation "read_models.site_live_board" does not exist`. The test
  starts an `OhmlineNode` and reads `GET /sites/slc-nowhere/live-board` at once; when it is the
  first test of the collection to touch a fresh container, no `ProjectionDaemon` has run
  `EnsureSchemaExists` yet. This is the same latent race as `ReadYourWritesTests` at `ch14-s01`
  (above), now on another endpoint. `ch15-s07` itself changes no integration test or node code
  (its diff touches only actor/domain tests and READMEs); the same tag in a worktree with a different
  directory name passed, and so did `ch15-s06`, because xUnit's order follows the test ids, which
  depend on the path. `ch15-s01`..`ch15-s06` were green on the first full run (1669/1699/1830/1838/
  1859/1871, matching the book). The book's "Try it" at `ch15-s07` promises `failed: 0`, which a
  reader gets only if this test is not first. Suggested fix forward: the same as for `ch14-s01`,
  have `OhmlineNode` (or the query tests) wait until the read-model schema exists before the first
  read, or map 42P01 in the query endpoints to 503 with `Retry-After`.

## ch16-s05 (fixed forward): the charging feed wrote each entry's type twice

**Status:** closed (fixed in the `ch16-s05` commit itself; not part of job 1).

- **Where:** `src/Charging/Ohmline.Charging.Contracts/Integration/ChargingFeedPage.cs`, since
  `ch16-s02`.
- **Symptom:** a real run through the AppHost showed every entry of `GET /feeds/charging` as
  `{"position":1,"event":{...},"type":"ohmline.charging.session-completed.v1"}`: the computed
  `ChargingFeedEntry.Type` property was serialized beside the event. Readers ignored it; the
  documented shape (`docs/feeds.md`) has no such member.
- **Fix (in the ch16-s05 commit):** `[JsonIgnore]` on `Type`. Quote `ChargingFeedPage.cs` from
  `ch16-s05` or later if the attribute matters to the text.

## ch16-s09 (fixed forward): the charging feed answered 500 before the outbox existed

**Status:** closed (fixed in the `ch16-s09` commit itself; the AppHost wait and the 503 stay as the book tells them in 16.9).

- **Where:** `src/Charging/Ohmline.Charging.Api/Feeds/FeedEndpoints.cs` (since `ch16-s02`) and
  `src/Ohmline.AppHost/AppHost.cs` (since `ch16-s05`).
- **Symptom:** the first system test run logged, in the seed node,
  `42P01: relation "read_models.integration_outbox" does not exist` with a 500 from
  `GET /feeds/charging`: Billing's feed consumer asked before the node's projection daemons had
  created the read-model schema. The consumer retried 5 s later and the test passed. The same
  start-up race as the `ch14-s01` read-your-writes entry.
- **Fix (in the ch16-s09 commit):** the feed answers 503 with `Retry-After: 1` while the outbox
  table is missing, and the AppHost starts Billing only once the seed node is healthy
  (`billing.WithReference(seed).WaitFor(seed)`). Chapter 20's migrator removes the race.

## ch16-s10 (observed, not fixed): SQLite actor tests time out while the system test runs

**Status (cleanup job 1): RESOLVED** by `sqlite-node-ask` (`ch10-s07`), `two-incarnations-ask` (`ch10-s09`) and `state-sourcing-ask` (`ch11-s07`). The CI split of the System trait was not adopted; every tag was green 3 of 3 with the DoD command unchanged.

- **Where:** `tests/Ohmline.Charging.Actors.Tests` (`Sqlite/NodeRestartTests`,
  `Sqlite/ShardRegionTests`, `JournalTour/JournalTourTests`), from `ch16-s09`.
- **Symptom:** 1 full run in 3 at `ch16-s10` failed four SQLite-backed actor tests after 10 s
  (Ask timeouts), the same load flake the `ch13-s07` and `ch14-s03` entries describe. From
  `ch16-s09` the full run also starts the whole AppHost (`Ohmline.SystemTests`: PostgreSQL and
  seven processes) beside every other test assembly, so the machine is busier than before.
  The next two full runs were green (2,111 tests).
- **Suggestion for the editor:** run the System trait in a step of its own in CI
  (`--filter-trait "Category=System"`) and the rest without it, or raise `OHMLINE_TIMEFACTOR`
  for the full run; the DoD command is unchanged here.

## ch16 (testing agent, 2026-11-05): known TariffEndpoints flake seen again at `ch16-s05`

**Status (cleanup job 1): RESOLVED** by `edge-testkit-ask`, amended into `ch12-s05` (`EdgeTestKit` ask 500 ms -> 3 s).

- First verify run of `ch16-s05` (fresh detached worktree, `dotnet test --solution Ohmline.slnx`):
  `total: 1991`, `failed: 1`:
  `Ohmline.Charging.Api.Tests.TariffEndpointsTests.Given_a_tariff_then_the_book_is_told_to_publish_it`
  (TariffEndpointsTests.cs:54, `(await answer).Result should be of type Accepted<CommandAcceptedResponse>
  but was ProblemHttpResult`; the probe's `TariffBookAccepted { Version = 2 }` went to deadLetters,
  so the endpoint's ask timed out). Same flake as the `ch13-s07` entry; unrelated to Chapter 16.
- Rerun of the same tag in a new worktree: `total: 1991`, `failed: 0`. All other ch16 tags were green
  on the first run (build `-warnaserror` 0 warnings/0 errors everywhere; totals 1914, 1923, 1951,
  1981, 1991, 2031, 2070, 2090, 2092, 2111, matching the book). No repo changes.

## ch17-s06 (fixed forward in ch17-s07): a timer's message reached the shell still wrapped

**Status:** closed (product fix in the `ch17-s07` commit itself; not part of job 1).

- **Where:** `src/Ohmline.ActorShell/Tracing/TracedPersistentActor.cs`, since `ch17-s06`
  (and `TracedReceiveActor.cs` since `ch17-s05`).
- **Symptom:** Akka.NET 1.5.73 hands `AroundReceive` a timer's message inside its own
  `Akka.Actor.Scheduler.TimerScheduler+TimerMsg` and takes the message out only further down,
  on the way to the handler. At `ch17-s06`, `OnMessageHandled` was therefore called with the
  `TimerMsg`, not with the timer's message (no override existed yet, so nothing visible broke);
  and a timer message in a `TraceEnvelope`, which `ch17-s07` sends for the journey's timers,
  reached the handlers unopened and was unhandled (found by `LinkTests` at `ch17-s07`).
- **Fix (in the ch17-s07 commit):** the shell opens an envelope that reaches the receive
  delegate (`TracedPersistentActor.Invoke`, `TracedReceiveActor.AroundReceive`), and the hook
  gets the message the handler got. Quote `TracedPersistentActor.cs` from `ch17-s07` or later
  when the text discusses timers or the hook's argument.

## ch17-s07 (observed, not fixed): LinkTests can see the warm-up timer's ring first

**Status (cleanup job 1): RESOLVED** by `link-tests`, amended into `ch17-s07`: the test now waits for the warm-up's own `Rang` before the traced `Arm`, and finds the ring's span with `SingleAsync` by its trace id. The 17.8 Note no longer mentions the race, and Exercise 2 is now a Reflect exercise about the fix. `review-log.md` line 368 refers to the old Note.

- **Where:** `tests/Ohmline.Telemetry.Tests/LinkTests.cs`,
  `Given_a_timer_started_in_a_trace_then_it_fires_in_a_new_trace_linked_to_it`, from `ch17-s07`.
- **Symptom:** in 1 of 2 `tools/verify-tags.sh` runs of `ch17-s08` (machine busy with the SQLite
  load flakes above), `Expected a message of type LinkTests+Armed, but received
  {Rang { ActivityId = }}`. The untraced warm-up `Arm` (sent so the persistent `Alarm` has
  recovered) also starts the 50 ms `ring` timer; when the traced `Arm` arrives more than 50 ms
  later, the warm-up's untraced `Rang` reaches `TestActor` first. Green in eight local runs.
- **Suggested fix forward (test only):** warm the alarm up with a message that starts no timer
  (a `WhatTimeIsIt`-style query answered by the `Alarm`), or make `Arm` carry a flag that the
  warm-up sets to `false`; nothing in `src/` changes.

## ch17 testing pass (2026-11-05): ch17-s07 has no green full run; load flakes at s02, s03, s08

**Status (cleanup job 1): RESOLVED.** `LinkTests`: `link-tests` (`ch17-s07`). `GridSignalPollerTests`: `grid-signal-poller` (`ch16-s08`). SQLite asks: `sqlite-node-ask` (`ch10-s07`) and siblings. Chapter 2 race: `logger-race` (`ch02-s09`). `ReadJournalTourTests`: `journal-node-ask` (`ch13-s03`, `JournalNode` ask 20 s × TimeFactor). `ch17-s07` green 3 of 3.

- **Scope:** fresh detached worktrees, `dotnet build Ohmline.slnx -warnaserror` (0 warnings/0
  errors at all nine tags) and `dotnet test --solution Ohmline.slnx`, with load average 10 to 20
  from other agents. Totals 2121, 2126, 2129, 2148, 2156, 2159, 2169, 2172, 2172 match the book.
  First runs were green at s01, s04, s05, s06 and s09. No mobile/ at any tag, so no MAUI build.
  No repo changes.
- **ch17-s07 (failed 4 of 4 full runs):** every run failed
  `LinkTests.Given_a_timer_started_in_a_trace_then_it_fires_in_a_new_trace_linked_to_it` with
  `Expected a message of type LinkTests+Armed, but received {Rang { ActivityId = }}`. This is the
  warm-up-timer race in the entry above, much more frequent under load than the coder's 1 in 2.
  `tests/Ohmline.Telemetry.Tests` alone at s07 passed 3 of 3. Runs 2 to 4 also hit
  `GridSignalPollerTests.Given_a_revision_that_cancels_the_event_then_the_site_is_restored`
  (twice): `Expected ... EndCurtailment, but received {ApplyCurtailment ...}`. That test dates
  from ch16-s08; its 500 ms `AskTimeout` with a 100 ms poll lets a retried `ApplyCurtailment` arrive
  before `EndCurtailment` under load, and on its own it passed 3 of 3. They also hit the SQLite
  AskTimeouts (5 to 7 tests), the Ch02.Hosted shutdown race and
  `ReadJournalTourTests.Given_a_live_query_then_it_delivers_an_event_stored_after_it_started`
  (once). The test-only fix in the entry above (a warm-up that starts no timer) would make s07
  verifiable. Section 17.8's note and Exercise 2 already tell readers about the race.
- **ch17-s02 (green on run 3) and ch17-s08 (green on run 3):** runs 1 and 2 each failed 3 (s02)
  or 4 (s08) SQLite `AskTimeoutException : Timeout after 5.00 seconds` tests in
  `Ohmline.Charging.Actors.Tests` (TwoIncarnations, ShardRegion, PoisonEvent, NodeRestart).
  `tests/Ohmline.Charging.Actors.Tests` alone was green at both tags (549 and 552).
- **ch17-s03 (green on run 2):** run 1 hit the Ch02.Hosted shutdown race
  (`m.Contains("draining charger twins")`).

## ch13-s07 … ch17 SQLite load flake (fixed forward in ch18-s04): Ask timeouts raised

**Status (cleanup job 1): RESOLVED at the introducing tags.** The ch18-s04 change is now in `ch10-s07` (`sqlite-node-ask`), `ch10-s09` (`two-incarnations-ask`) and `ch11-s07` (`state-sourcing-ask`); `ch18-s04` only rewords the three doc comments (its commit message still says "SQLite tests wait longer"). The 18.4 Note in the book was rewritten to say so, and 18.4's `--stat` total is now `15 files changed, 397 insertions(+), 76 deletions(-)`.

- **Where:** `tests/Ohmline.Charging.Actors.Tests/Sqlite/SqliteNode.cs`,
  `JournalTour/StateSourcingTourTests.cs`, `JournalTour/TwoIncarnationsTests.cs` (the `AskTimeout`
  of 5 s each), the flake the `ch13-s07`, `ch14-s03`, `ch16-s10` and ch17 entries describe.
- **Symptom:** from `ch18-s02` on, the full `dotnet test --solution Ohmline.slnx` failed the same
  four or five SQLite tests (`AskTimeoutException : Timeout after 5.00 seconds`) in 5 of 7 runs,
  also with `--filter-not-trait "Category=System"`; the project on its own passed every time
  (557). Chapter 18 adds more test hosts that run actor systems in parallel (Node, Telemetry), so
  a SQLite node that creates its tables and forms its cluster misses 5 s more often.
- **Fix forward (test only, in the `ch18-s04` commit):** the three `AskTimeout`s are now
  `TimeSpan.FromSeconds(20 * ActorTestConfig.TimeFactor)`. Nothing in `src/` changes. Two full
  runs green right after (2,195). Earlier tags keep the 5 s timeouts; `tools/verify-tags.sh` may
  still need a rerun for them.

## ch18 (testing agent, 2026-11-05): `ch18-s03` has no green full run; load flakes at s02, s04, s07, s09

**Status (cleanup job 1): RESOLVED.** `QueryApiTests` 42P01: `fixture-ensures-schema` (`ch13-s08`); `ch18-s03` green 3 of 3. SQLite asks: `sqlite-node-ask` and siblings. `SessionEndpointsTests`/`TariffEndpointsTests` 500 ms: `edge-testkit-ask` (`ch12-s05`). `ChargeToReceiptTests`: `simulator-retries` (`ch16-s07`). `TraceShapeTests`: `trace-shape-ancestors` (`ch17-s08`). `LinkTests`: `link-tests`. Chapter 2 race: `logger-race`.

- **ch18-s03 failed 3 of 3 full runs** in the verify worktree
  `.../scratchpad/verify-ch18/ch18-s03` (build `-warnaserror` 0 warnings, 0 errors, `total: 2191`).
  Every run failed
  `Ohmline.IntegrationTests.QueryApiTests.Given_a_site_with_no_connectors_on_the_board_then_404`:
  `response.StatusCode should be HttpStatusCode.NotFound but was HttpStatusCode.InternalServerError`.
  The integration project run alone in the same path also failed (1 of 30), and the node's log showed
  `42P01: relation "read_models.site_live_board" does not exist`. This is the latent
  schema-creation race from the `ch15-s07` entry above. Test order follows the worktree path, and
  `ch18-s03` changes no node or query code. Runs 2 and 3 also hit the SQLite
  `AskTimeoutException : Timeout after 5.00 seconds` flake, which `ch18-s04` fixes forward. In a
  worktree with a different name (`alt-ch18-s03`), `QueryApiTests` passed in all 4 full runs. Those
  runs still failed 4, 17, 4 and 5 tests: the 5 s SQLite AskTimeouts (TwoIncarnations, JournalTour,
  StateSourcingTour, PoisonEvent, ShardRegion, NodeRestart), with machine load between 15 and 25.
  Run 2 also hit wider load failures, including `TraceShapeTests`, `ReadJournalTourTests` and the
  Charging.Api 500 ms asks. `tests/Ohmline.Charging.Actors.Tests` alone at s03 passed (557/557). So
  s03's own tests are fine. The tag cannot be verified until the read-model race is fixed (see the
  `ch14-s01` and `ch15-s07` suggestions). The book's s03 "Try it" promises 2,191 tests and no
  failures, which a reader gets only when this test does not run first.
- **ch18-s02 (green on run 3):** run 1 hit the Ch02.Hosted shutdown race and the `LinkTests` timer
  race (errata ch17-s07). Run 2 hit the Ch02.Hosted race and 5 SQLite 5 s AskTimeouts.
- **ch18-s04 (green on run 3, 2,195):** both failed runs hit
  `Ohmline.Charging.Api.Tests.SessionEndpointsTests.Given_a_start_then_the_session_gets_the_tariffs_current_version`
  (`(await answer).Result should be of type Accepted<CommandAcceptedResponse> but was
  ProblemHttpResult`, SessionEndpointsTests.cs:50). That test is unchanged since `ch14-s01`.
  `EdgeTestKit` gives the endpoint a 500 ms `AskTimeout`, and under load the probe's answer arrives
  late, so it is a load flake that s04's diff does not touch. Run 1 also hit the Ch02.Hosted race. Run 2
  hit `ChargeToReceiptTests` (`The gateway answered InternalError: Charging did not answer.`), the
  Aspire system test under load. Suggested test-only fix: scale `EdgeTestKit`'s 500 ms with
  `ActorTestConfig.TimeFactor`, as `ch18-s04` did for SQLite.
- **ch18-s07 (green on run 2, 2,202):** run 1 hit the known
  `TariffEndpointsTests.Given_a_tariff_then_the_book_is_told_to_publish_it` flake (ch16-s05 entry).
- **ch18-s09 (green on run 3, 2,206):** runs 1 and 2 hit the `LinkTests` timer race, and run 1 also
  hit the Ch02.Hosted race.
- s01 (2,180), s05 (2,197), s06 (2,202) and s08 (2,203) were green on the first run. Every total
  matches the book.

## ch19 coder runs (2026-11-05): known flakes seen again, nothing fixed

**Status (cleanup job 1): RESOLVED.** `LinkTests`: `link-tests` (`ch17-s07`). Chapter 2 race: `logger-race` (`ch02-s09`). The MAUI item: see the next entry.

- **Scope:** full `dotnet test --solution Ohmline.slnx` in the working tree at each Chapter 19
  tag as it was committed. No repo change for these; no earlier tag was amended.
- **ch19-s07 (first run):** `LinkTests.Given_a_timer_started_in_a_trace_then_it_fires_in_a_new_
  trace_linked_to_it`, the warm-up-timer race of the ch17-s07 entry above. Rerun: 2,377 green.
- **ch19-s08 (first run):** `Ch02.Hosted.Tests.ChargerAppTests.Given_a_running_host_when_it_
  stops_then_coordinated_shutdown_drains_first`, the shutdown-log race logged since Chapter 1.
  Rerun: green.
- **Not a flake: `ch19-s05` and `ch19-s07` fail `tools/verify-tags.sh` in a run over the
  chapter (needs a fix forward; see the next entry).**

## ch19-s05, ch19-s07 (found by the coder, not fixed): the MAUI head fails after `AppShell.xaml` changes

**Status (cleanup job 1): RESOLVED** by `maui-cs0200`, amended into `ch19-s04`: `mobile/Directory.Build.props` sets `EnableMauiIncrementalHotReload` to `false`. Same-path builds over s04 -> s05 -> s07 -> s04 -> s08 -> s05 -> ch20-s01 -> ch21-s09 gave 0 warnings and no CS0200. The book's 19.5 Version note now describes the setting instead of `dotnet build-server shutdown`; 19.4's filtered `--stat` total is now `30 files changed, 768 insertions(+)`.

- **Where:** `mobile/Ohmline.Mobile` (the head's `AppShell.xaml` changes at `ch19-s04` (one
  `ShellContent`), `ch19-s05` (a `TabBar` with two) and `ch19-s07` (three)), MAUI
  11.0.0-rc.2.26505.5.
- **Symptom:** `tools/verify-tags.sh 'ch19-*'` (one worktree, tags in order): s01-s04, s06, s08
  passed; **s05 and s07 failed**, in the mobile build only (their tests were green: 2,335 and
  2,377), with `.../obj/Debug/net11.0-android/Microsoft.Maui.Controls.SourceGen/
  Microsoft.Maui.Controls.SourceGen.XamlGenerator/AppShell.xaml.uc.xsg.cs(45,3): error CS0200:
  Property or indexer 'ShellItem.Items' cannot be assigned to -- it is read only` (and
  `'Shell.Items'`).
- **Cause (reproduced):** XAML Incremental Hot Reload, on by default in Debug for .NET 11
  (`EnableMauiIncrementalHotReload`), generates `UpdateComponent` in `*.xaml.uc.xsg.cs`. Normally it
  is empty. When the shared compiler server (`VBCSCompiler`) has built the same project path with a
  different `AppShell.xaml`, the generator emits an update for the change, and for Shell items
  that code assigns a read-only collection. Deleting `obj/` does not help; the state is in the
  compiler server. In a worktree: build `ch19-s04`, check out `ch19-s05`, delete `obj`, build:
  fails; build again: passes. The same at s07 after s06, and at s04 after s08. A reader who checks
  out the tags in order and builds the head at each hits it too.
- **Verified workarounds** (each passed every transition s04→s05→s07→s04→s08): build with
  `-p:EnableMauiIncrementalHotReload=false`; or `-p:UseSharedCompilation=false`; or run
  `dotnet build-server shutdown` before the build.
- **Suggested fix forward (not made: the chapter's tags must not move, and an untagged commit
  after `ch19-s08` would land in `ch20-s01`'s diff):** in the next commit that touches the
  sample, either (a) add to `mobile/Ohmline.Mobile/Ohmline.Mobile.csproj`
  `<EnableMauiIncrementalHotReload>false</EnableMauiIncrementalHotReload>` with a comment naming
  this entry (fixes the head from then on, not the old tags), and (b) in `tools/verify-tags.sh`,
  run `dotnet build-server shutdown` before `dotnet build mobile/Ohmline.Mobile.slnx`, which makes
  every existing tag verifiable, since the script always runs from the current checkout; or have
  the editor amend `ch19-s04` with (a) and re-point the later tags (architecture §10). Until then,
  `mobile/README.md` should tell readers: after checking out another tag, run
  `dotnet build-server shutdown` before building the head.

## ch19 verification (testing agent): flakes only, every tag green

**Status (cleanup job 1): RESOLVED** except the Podman rootlessport port clash. `TariffEndpointsTests`/`SessionEndpointsTests`: `edge-testkit-ask` (`ch12-s05`). `GridSignalPollerTests`: `grid-signal-poller` (`ch16-s08`). `ChargeToReceiptTests`: `simulator-retries` (`ch16-s07`). `LinkTests`: `link-tests`. Chapter 2 race: `logger-race`. The port clash is still OPEN (see "Still open" at the top).

Every run used a fresh detached worktree per tag (`.../scratchpad/verify-ch19/<tag>`), on a
machine shared with other agents (load 5-12). `dotnet build Ohmline.slnx -warnaserror` and
`dotnet build mobile/Ohmline.Mobile.slnx -warnaserror` (from s04) gave 0 warnings and 0 errors at
every tag, and no tag left files behind. Totals match the book: 2,206 / 2,264 / 2,283 / 2,294 /
2,335 / 2,361 / 2,377 / 2,384.

- **The MAUI `AppShell.xaml.uc.xsg.cs` CS0200 entry above did not reproduce here.** The mobile
  head built at s04-s08 in a sequential run without `dotnet build-server shutdown`. Each tag had
  its own worktree path, which is consistent with the coder's diagnosis: the bug needs the compiler
  server to have built the *same project path* with a different `AppShell.xaml`. The fix forward
  in that entry still stands for `tools/verify-tags.sh`, which reuses a single path.
- **s01, s02, s03, s05:** green on the first run.
- **ch19-s04 (green on run 2):** run 1 hit the known
  `TariffEndpointsTests.Given_a_tariff_then_the_book_is_told_to_publish_it` flake (ch16-s05 entry).
- **ch19-s06 (green on run 3):** run 1 hit the Ch02.Hosted shutdown race. Run 2 hit
  `GridSignalPollerTests.Given_a_revision_that_cancels_the_event_then_the_site_is_restored`
  (received `ApplyCurtailment`, expected `EndCurtailment`) and `ChargeToReceiptTests` (504 from
  the simulator's driver), both load flakes seen in ch17/ch18.
- **ch19-s07 (green on run 2):** run 1 hit the `LinkTests` timer race (ch17-s07 entry).
- **ch19-s08 (green on run 4):** runs 1-3 all failed
  `SessionEndpointsTests.Given_a_start_then_the_session_gets_the_tariffs_current_version`
  (`ProblemHttpResult` instead of `Accepted<CommandAcceptedResponse>`, SessionEndpointsTests.cs:50),
  which is the ch18-s04 entry's 500 ms `EdgeTestKit` ask under load. s08 does not touch that test or
  `SessionEndpoints`, and `tests/Ohmline.Charging.Api.Tests` on its own passed 3 of 3 full runs
  (63/63) and 3 of 3 filtered runs (6/6). It failing 3 times in a row at s08, and never at s01-s07,
  suggests that s08's extra parallel Postgres work (`TapToJournalTraceTests`) makes the race more
  likely. That makes the suggested `TimeFactor` fix in the ch18-s04 entry more urgent. Run 1 also
  hit `HealthTests` with a Podman `rootlessport ... bind: address already in use` on container
  start (environmental: another agent's containers). Run 2 also hit `ChargeToReceiptTests`
  (WebSocket could not connect, under load).

## ch21-s08 (fixed forward): flaky Charging.Api edge tests

**Status (cleanup job 1): RESOLVED at the introducing tag** by `edge-testkit-ask`, amended into `ch12-s05` (the 500 ms ask dates from `ch12-s05`, not `ch12-s08`). `ch21-s08` now only rewords the comment, which still says "(500 ms timed out now and then from Chapter 21's test count on)" although 3 s is in place from `ch12-s05`; 21.8's `--stat` total is now `38 files changed, 729 insertions(+), 42 deletions(-)`. The Chapter 2 race in the last bullet: `logger-race` (`ch02-s09`), test-only, so section 2.9's listings did not change.

- **Where:** `tests/Ohmline.Charging.Api.Tests/EdgeTestKit.cs`, from `ch12-s08`.
- **Problem:** `Options` gave the endpoints an `AskTimeout` of 500 ms. With the whole suite
  running in parallel (about 2,650 tests by Chapter 21), a test probe's reply sometimes arrived
  later, and `SessionEndpointsTests.Given_a_start_then_the_session_gets_the_tariffs_current_version`
  or `TariffEndpointsTests.Given_a_tariff_then_the_book_is_told_to_publish_it` failed with a
  `ProblemHttpResult` (504) instead of `Accepted`. Seen twice in full runs during Chapter 21;
  always green when the project runs alone.
- **Fix (in ch21-s08's commit):** the timeout is 3 s. Only
  `Given_no_answer_in_time_then_504_and_the_command_may_still_happen` waits for it; its expected
  detail now reads "No answer within 3 s." (was 0.5 s). No book text
  quotes the 500 ms line as far as the coder knows; a writer who quotes `EdgeTestKit` at an earlier
  tag is unaffected.
- **Also seen, not fixed:** `Ch02.Hosted.Tests.ChargerAppTests.Given_a_running_host_when_it_stops_then_coordinated_shutdown_drains_first`
  (section 2.9) failed twice in about ten full runs: the "draining charger twins" line goes
  through Akka.NET's asynchronous logger and is sometimes not yet written when the host has
  stopped. It passes alone; fixing it would change code that section 2.9 quotes, so it is left
  for the editor to decide.

## ch20 verification (testing agent): flakes only; ch20-s08 not green in 5 full runs

**Status (cleanup job 1): RESOLVED.** `ch20-s02` 42P01: `fixture-ensures-schema` (`ch13-s08`). `ch20-s08`: `edge-testkit-ask` (`ch12-s05`), `logger-race` (`ch02-s09`), `link-tests` (`ch17-s07`); green 3 of 3. Chapter 2 race at `ch20-s04`: `logger-race`. `TariffEndpointsTests`: `edge-testkit-ask`.

Every run used a fresh detached worktree per tag (`.../scratchpad/verify-ch20/<tag>`), on a
machine shared with other agents (load 6-23). `dotnet build Ohmline.slnx -warnaserror` and
`dotnet build mobile/Ohmline.Mobile.slnx -warnaserror` gave 0 warnings and 0 errors at every tag,
and no tag left files behind. Totals match the book: 2,394 / 2,402 / 2,412 / 2,428 / 2,550 /
2,557 / 2,561 / 2,569 / 2,585 / 2,597. No book listing or repo fix is needed.

- First runs green at s01, s03, s05, s07, s09, s10.
- **ch20-s02** green on run 3. Runs 1 and 2 failed
  `QueryApiTests.Given_a_site_with_no_connectors_on_the_board_then_404` (500 instead of 404:
  `42P01: relation "read_models.site_live_board" does not exist`), the path-dependent
  ProjectionDaemon schema race logged at ch15-s07. The migrator at ch20-s08 removes its cause.
- **ch20-s04** green on run 2. Run 1 failed the known Ch02.Hosted shutdown race
  (`ChargerAppTests.Given_a_running_host_when_it_stops_then_coordinated_shutdown_drains_first`,
  "draining charger twins" missing from the logs).
- **ch20-s06** green on run 2. Run 1 failed the known `TariffEndpointsTests.Given_a_tariff_then_the_book_is_told_to_publish_it`
  (ProblemHttpResult, 500 ms ask) and the `LinkTests` timer race ("Expected ... Armed, but received ... Rang").
- **ch20-s08 failed 5 of 5 full runs** (2,569 total; 1 or 2 failures each), only with known
  load flakes: `SessionEndpointsTests.Given_a_start_then_the_session_gets_the_tariffs_current_version`
  in 4 of 5 (`ProblemHttpResult` instead of `Accepted<CommandAcceptedResponse>`,
  SessionEndpointsTests.cs:50; `EdgeTestKit` uses a 500 ms `AskTimeout`), the Ch02.Hosted shutdown
  race in 2, and `LinkTests` in 1. `tests/Ohmline.Charging.Api.Tests` alone at ch20-s08 is green
  3 of 3 (63/63) at load 16-17. ch20-s08 changes neither that project nor Charging.Api, so this is
  the machine-load flake recorded for ch18/ch19, not a ch20 defect; the 500 ms test ask timeout
  remains the thing to raise (or make configurable) if it keeps failing verification.

## ch21 verification (testing agent): flakes only, every tag green

**Status (cleanup job 1): RESOLVED.** `NodeMetricTests`/`TapToJournalTraceTests` 504: `traced-node-ask` (`ch17-s08`, the in-memory node's edge asks 20 s). `SessionEndpointsTests`: `edge-testkit-ask` (`ch12-s05`). `LinkTests`: `link-tests`. Chapter 2 race: `logger-race`. The 21.8 Mermaid fix stands.

Every run used a fresh detached worktree per tag (`.../scratchpad/verify-ch21/<tag>`), on a
machine shared with other agents (load 4-17). `dotnet build Ohmline.slnx -warnaserror` and
`dotnet build mobile/Ohmline.Mobile.slnx -warnaserror` gave 0 warnings and 0 errors at every tag,
and no worktree was left behind. Totals match the book: 2,599 / 2,613 / 2,613 / 2,615 / 2,623 /
2,632 / 2,651 / 2,661 / 2,664 / 2,676 (s10 on `upgrade/akka-1.6`, Akka.NET 1.6.0-beta5).

- First runs green at s01, s04, s05, s06, s08, s09, s10.
- **ch21-s02** green on run 4. Run 1 failed `NodeMetricTests.Given_a_command_over_HTTP_then_the_nodes_meters_count_it_and_time_recovery`
  and `TapToJournalTraceTests.Given_a_send_after_the_app_restarted_then_it_links_to_the_tap`
  (504 Gateway Timeout after 10 s; "ClusterSingletonProxy failed to find ... [tariff-book] ...
  after 30 seconds", load 17). Runs 2 and 3 failed the known
  `SessionEndpointsTests.Given_a_start_then_the_session_gets_the_tariffs_current_version`
  (500 ms `EdgeTestKit` ask, fixed forward at ch21-s08), run 3 also the `LinkTests` timer race.
  `tests/Ohmline.Charging.Api.Tests` (67/67) and `tests/Ohmline.Telemetry.Tests` (40/40) pass alone.
- **ch21-s03** green on run 3. Run 1 failed the known Ch02.Hosted shutdown race
  (`ChargerAppTests.Given_a_running_host_when_it_stops_then_coordinated_shutdown_drains_first`),
  run 2 the `LinkTests` timer race.
- **ch21-s07** green on run 2. Run 1 failed the Ch02.Hosted shutdown race.
- Book fix (not a repo issue): the blue/green `sequenceDiagram` in section 21.8 had a `;` in a
  message (`B->>J: last events; shards handed off, cluster left`); Mermaid ends a message's text at
  `;`, so the diagram would not parse. Changed to a comma.

## Cleanup job 1 (2026-11-05/06): items found during the cleanup, all resolved

- **`GatewayDrainTests.Given_connected_chargers_when_the_gateway_stops_then_each_is_told_to_return`**
  (final report item 7): one charger got `NormalClosure` instead of 1012 under full-suite load.
  **Status: RESOLVED** by `gateway-drain`, amended into `ch21-s07` (test-only; `GatewayDrainTests` is
  not quoted). 21.7's `--stat` total is now `25 files changed, 931 insertions(+), 5 deletions(-)`.
- **`TraceShapeTests.Given_a_journey_then_what_it_tells_other_actors_stays_in_the_request_trace`**
  (from `ch17-s08`): `SpansAsync` returned once the child span `process IdTokenActor` was exported,
  before its parent `persisted ChargingJourneyActor` had ended. **Status: RESOLVED** by
  `trace-shape-ancestors`, amended into `ch17-s08`: the helper waits for the named span and every
  ancestor up to the server span. The quoted 17.8 listing is unchanged; one sentence in "What to
  assert" now explains the wait. 17.8's `--stat` total is now `5 files changed, 312 insertions(+), 2 deletions(-)`.
- **`JournalTraceTests.Given_traced_events_in_the_journal_then_a_new_incarnation_recovers_them`**
  (from `ch17-s07`): `InvalidActorNameException: Actor name "counter-2" is not unique!`, because
  `Terminated` can reach the watcher before the guardian frees the name. **Status: RESOLVED** by
  `journal-trace-unnamed`, amended into `ch17-s07` (the counter starts unnamed).
- **`PersistenceShellSpec.Scenario.SeedAsync`** (from `ch11-s08`): `AskTimeoutException` after 3 s on
  a cold journal (ch12-s11, ch15-s05, ch15-s06), and the empty-history variant timing out in the
  actor under test (ch16-s07). **Status: RESOLVED** by `persistence-shell-seed`, amended into
  `ch11-s08` (20 s × TimeFactor for seeding; the seeder also runs for an empty history). The quoted
  11.8 listing is unchanged; 11.8's `--stat` total is now `11 files changed, 496 insertions(+), 245 deletions(-)`.
- **`SiteEnergyEndpointsTests` and `DriverAccountsEndpointsTests` 500 ms asks** (latent in the
  load-timeouts diagnosis, then observed at ch14-s05 and ch15-s05). **Status: RESOLVED** by
  `site-energy-api-ask` (`ch12-s05`) and `driver-accounts-api-ask` (`ch12-s07`), 3 s each.
- **`wrong/ch12-s04-context-stop` fails four tests, not three** (before and after the rewrite).
  **Status: RESOLVED in the book**: 12.4's Try-it now says "four tests from main fail".
- **`ch18-s01` tag annotation** read `ch18-s01: ...` unlike every other tag. **Status: RESOLVED**:
  re-created as `18.1 Domain metrics with a cardinality budget`, as Appendix C already showed.
