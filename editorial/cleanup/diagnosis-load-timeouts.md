# Diagnosis: load-only timeouts (cleanup family `load-timeouts`)

Agent: diagnose-and-patch, 2026-11-05/06. The sample repo was not modified. All work was done in
detached worktrees under the session scratchpad and in a private clone (`.../cleanup-load-timeouts/replay`)
used to simulate the history rewrite. Patches are in `editorial/cleanup/patches/load-timeouts-*.patch`.

## How the flakes were reproduced

The flakes appear only when the whole suite runs at once (load 15 to 40 on 16 cores). To reproduce
them without loading the machine for the other agents, the test process was pinned with `taskset`
to one or two CPUs that also ran 3 to 4 busy loops each (`burn.sh <cpu> <n> <seconds>`). Under that
contention every family member failed reliably on the unpatched code (numbers below), and passed on
the patched code.

## Common root cause

Every test in this family asks a **cold** node or actor system for its first answer with a fixed
production-sized (5 s) or test-sized (500 ms) deadline. The first answer includes one-time work
that has nothing to do with the behaviour under test and whose duration scales with CPU
starvation:

- SQLite table creation (`autoInitialize`), and from ch12 the cluster self-join, the shard
  coordinator singleton starting, the region registering with it (Akka.NET retries the
  registration every 2 s, so the latency is quantized), shard allocation and the first recovery.
  Instrumented at ch18-s03 under contention: every ask after the first answered in under 20 ms
  (p90 18 ms over 672 asks), while the first ask on a fresh node took 0.4 s to 5.4 s.
- For WebApplicationFactory and Aspire nodes, also the first PostgreSQL connection and the first
  write path (the seed's `tariff-book` took 4.8 s and 8.7 s just to recover under contention).
- For `EdgeTestKit`, the round trip through a fresh TestKit system with `serialize-messages = on`
  (two serializations, JIT on first use) measured about 1 s under contention against a 500 ms
  deadline.

So the deadline really is the defect: it measures warm-up, not the behaviour. The fix is to give
cold starts a test-sized patience (scaled by `OHMLINE_TIMEFACTOR` where the value is not asserted),
and for the system test, to make the simulator behave as the protocols already tell clients to.

## Fixes (one patch per introducing tag)

| id | introducing tag | file(s) | change |
| --- | --- | --- | --- |
| sqlite-node-ask | ch10-s07 | `tests/Ohmline.Charging.Actors.Tests/Sqlite/SqliteNode.cs` | `AskTimeout` 5 s -> `20 * ActorTestConfig.TimeFactor` s (backport of ch18-s04) |
| two-incarnations-ask | ch10-s09 | `tests/Ohmline.Charging.Actors.Tests/JournalTour/TwoIncarnationsTests.cs` | same |
| state-sourcing-ask | ch11-s07 | `tests/Ohmline.Charging.Actors.Tests/JournalTour/StateSourcingTourTests.cs` | same |
| edge-testkit-ask | ch12-s05 | `tests/Ohmline.Charging.Api.Tests/EdgeTestKit.cs`, `ChargePointEndpointsTests.cs` | 500 ms -> 3 s, 504 detail "No answer within 3 s." (backport of ch21-s08) |
| journal-node-ask | ch13-s03 | `tests/Ohmline.Charging.ReadModels.Tests/JournalNode.cs` | `AskTimeout` 5 s -> `20 * TimeFactor` s (never fixed forward; cause of the `ReadJournalTourTests` flake) |
| simulator-retries | ch16-s07 | `tools/Ohmline.Simulator/Driver.cs`, `SimulatedCharger.cs` | driver sends an `Idempotency-Key` and resends on 504 (3 attempts, 1 s apart); charger resends a call answered `InternalError` (3 attempts). Cause of the `ChargeToReceiptTests` flake. |
| traced-node-ask | ch17-s08 | `tests/Ohmline.Telemetry.Tests/TracedNode.cs` | `Ohmline:{Charging,SiteEnergy,DriverAccounts}:AskTimeout` = 20 s for the in-memory node. Cause of the `NodeMetricTests` / `TapToJournalTraceTests` 504s. |

Test counts: unchanged by every patch (no test added or removed).

### Notes per fix

- **Errata corrections.** The 500 ms `EdgeTestKit` timeout dates from `ch12-s05`, not `ch12-s08`. The
  504-detail line that later carries "send it again with the same Idempotency-Key" was changed at
  `ch12-s06`, not `ch15-s06`.
- **ReadJournalTourTests.** This is the same cold-start ask as the SQLite one, in a second copy of the
  helper (`JournalNode`, ch13-s03) that ch18-s04 did not touch. At ch21-s09 it still asks with 5 s.
  Unpatched at ch18-s03, with the project on one contended CPU beside Charging.Actors.Tests, it failed
  5 of 5 runs with `AskTimeoutException : Timeout after 5.00 seconds` at `JournalNode.AskAsync`.
- **ChargeToReceiptTests.** At ch21-s07, with the AppHost on 2 contended CPUs, it failed 3 of 3 runs:
  twice a 504 on the driver's first `POST /tariffs` and once `The gateway answered InternalError:
  Charging did not answer.` Both answers mean "not done in time; send it again". Charging's 504
  detail says so, and the gateway undoes its context for a resend ("the charger sends the call
  again"). The simulator sent each request once and threw. The fix keeps the production timeouts and
  the book-quoted test unchanged. The book never quotes the simulator's code.
  The retried HTTP commands are deduplicated by the `Idempotency-Key` (every command endpoint accepts
  one from ch12-s06, before the simulator exists). A resent OCPP call gets a new message id, as a real
  charger's retry would. Charging is idempotent by state for status and meter calls.
- **TapToJournalTraceTests / NodeMetricTests.** Both fail on the node's first command (504 after its
  5 s ask). The "ClusterSingletonProxy failed to find tariff-book after 30 seconds" warning in the
  errata is a red herring: it is logged within about 40 ms of the node starting, and the singleton is
  identified right after. Reproduced 3 of 3 at ch21-s07 on contended CPUs; see the evidence below.

## Replay (how the rewrite should carry these forward)

The replay was simulated in a private clone: every commit from ch10-s07 to `main` was
cherry-picked, and each patch was applied at its tag. Six conflicts came up, all trivial:

| at | file | resolution |
| --- | --- | --- |
| ch12-s02 | SqliteNode.cs (usings) | union, sorted: `Ohmline.ActorShell.Clustering`, `Ohmline.Charging.Contracts.ChargePoints`, `Ohmline.Testing` (ch12-s02 drops `Ohmline.Charging.Actors.ChargePoints`) |
| ch12-s06 | ChargePointEndpointsTests.cs | take ch12-s06's two-line detail with `0.5 s` -> `3 s` |
| ch18-s01 | JournalNode.cs (usings) | keep both: `...Actors.Diagnostics`, then `Ohmline.Testing` |
| ch18-s04 | SqliteNode.cs, TwoIncarnationsTests.cs, StateSourcingTourTests.cs | comment-only conflict; take ch18-s04's (later, fuller) comment. ch18-s04 then changes only those comments; its commit message line "SQLite tests wait longer" becomes stale. |
| ch21-s07 | SimulatedCharger.cs (end of file) | keep both types: `CloseFrame`, then `GatewayCallError` |
| ch21-s08 | EdgeTestKit.cs | comment-only; take ch21-s08's comment (or drop its parenthetical "500 ms timed out now and then from Chapter 21's test count on", which is no longer true after the backport). ChargePointEndpointsTests merges cleanly (identical change). |

The replayed tip differs from `main` only in JournalNode.cs, TracedNode.cs and the two simulator
files. `dotnet build Ohmline.slnx -warnaserror` gives 0 warnings and 0 errors there. ch21-s10
(`upgrade/akka-1.6`) touches none of these files except lock files and needs a plain rebase.
`v1.0.0` (31a0d8b) must also be re-pointed.

## Book impact

- `TwoIncarnationsTests.cs` is quoted at ch10-s09 (section 10.9), but only the test body. The
  changed field and using lines are outside the excerpt, so the listing is unchanged.
- **`TracedNode.cs` @ ch17-s08 is quoted in section 17.8** (`ConfigureWebHost`, ch17 lines 696-707).
  The listing must gain the comment and the three `UseSetting(...AskTimeout...)` lines. A sentence
  such as "and gives the edge 20 s instead of production's 5 s for a node that has just started"
  could follow it.
- Not quoted: SqliteNode.cs, StateSourcingTourTests.cs, JournalNode.cs, EdgeTestKit.cs,
  ChargePointEndpointsTests.cs, Driver.cs, SimulatedCharger.cs. Section 16.7's simulator output
  is unchanged.
- **Quoted `--stat` totals change.** The book quotes each of these totals. They were measured on
  the simulated replay, with the conflicts resolved as in the table above:

  | tag | book quotes | after replay |
  | --- | --- | --- |
  | ch10-s07 (10.7) | 9 files changed, 150 insertions(+), 3 deletions(-) | 9 files, 156 (+), 3 (-) |
  | ch10-s09 (10.9) | 2 files changed, 150 insertions(+) | 2 files, 156 (+) |
  | ch11-s07 (11.7) | 27 files changed, 930 insertions(+), 3 deletions(-) | 27 files, 935 (+), 3 (-) |
  | ch12-s05 (12.5) | 56 files changed, 1922 insertions(+), 10 deletions(-) | 56 files, 1926 (+), 10 (-) |
  | ch13-s03 (13.3) | 10 files changed, 376 insertions(+), 4 deletions(-) | 10 files, 382 (+), 4 (-) |
  | ch16-s07 (16.7) | 53 files changed, 2000 insertions(+), 226 deletions(-) | 53 files, 2055 (+), 226 (-) |
  | ch17-s08 (17.8) | 5 files changed, 290 insertions(+), 2 deletions(-) | 5 files, 295 (+), 2 (-) |
  | ch18-s04 (18.4) | 15 files changed, 411 insertions(+), 73 deletions(-) | 15 files, 397 (+), 76 (-) (keeping ch18-s04's comments) |
  | ch21-s08 (21.8) | 39 files changed, 733 insertions(+), 42 deletions(-) | 38 files, 729 (+), 42 (-) (keeping ch21-s08's comment) |

  The other tags' totals are unchanged (checked at ch12-s02, ch12-s06, ch18-s01 and ch21-s07, where
  the conflicts were).

## Not fixed

- **Podman rootlessport port clash** (`HealthTests`, ch19-s08 run 1: `rootlessport listen tcp
  0.0.0.0:38513: bind: address already in use`). The test already uses a dynamic port:
  Testcontainers asks for a random host port. Podman picks the port when the container is created
  and rootlessport binds it only at start. The port lies in the kernel's ephemeral range
  (32768-60999), which the suite's hundreds of port-0 sockets (Akka remoting, Kestrel, clients)
  draw from too. That is a check-then-bind race inside rootless Podman, so "use dynamic ports"
  cannot fix it. The options are a retry-on-bind-failure wrapper around `StartAsync` in
  Ohmline.Testing, or a host setting. Both are environmental, and it was seen once.
- **Fixed ports in the AppHost** (5180-5182, 5185, 5190, remoting 4053-4055/4060, pbm 9110-9112/9120).
  Two full-suite runs on one machine (several agents at once) clash on them. Section 16.9 accepts
  this ("a system test owns its machine"). Also seen: every in-memory node in Development binds pbm
  to 127.0.0.1:9110 and logs `FATAL ... failed to bind` when another holds it. This is harmless
  (pbm only), but noisy.
- **System trait / separate CI step.** The trait already exists: `[Trait("Category", "System")]`
  on `ChargeToReceiptTests` since ch16-s09, described in sections 9.1, 16.9 and 21.4. ci.yml (ch21-s04)
  already runs the fast loop in its own job; Integration and System share one job, which section 21.4
  quotes and explains. Splitting them, or splitting `tools/verify-tags.sh` (quoted at ch01-s07), would
  change quoted text and is left to the editor/tooling family. No test count changes either way.
- **Latent, same pattern, not observed failing:** `SiteEnergy.Api.Tests` and `DriverAccounts.Api.Tests`
  test kits also use a 500 ms ask, and IntegrationTests' `OhmlineNode` uses the production 5 s.
  `GridSignalPollerTests` (500 ms ask with a 100 ms poll) belongs to the timer-races family.
- **ChargeToReceiptTests under extreme contention** (12 busy loops on 2 CPUs): 1 run in 5 still
  failed. The split-brain resolver downed the starved seed node, and a journey's charger-command
  port timed out. That is a cluster partition, not a timeout, and is outside this family.

## Evidence (contention = `taskset -c <cpu>` plus 3 or 4 busy loops on the same CPU)

| fix | before | after |
| --- | --- | --- |
| SQLite (SqliteNode, TwoIncarnations, StateSourcing) | ch18-s03 Charging.Actors.Tests on 1 contended CPU: `NodeRestartTests` `AskTimeoutException : Timeout after 5.00 seconds` (1 of 3 runs). Instrumented: first ask up to 5.4 s, all later asks < 1 s. | ch18-s03 with the fix: 8 of 8 recorded runs green under heavier contention (two sets of loops), beside ReadModels. At the intro tags: ch10-s07 3/3 (121 tests), ch10-s09 3/3 (124), ch11-s07 10/10 (245), all on a contended CPU. `-warnaserror` clean. |
| journal-node-ask | ch18-s03 ReadModels (non-Integration) beside Charging.Actors on 1 CPU + 5 loops: `ReadJournalTourTests` AskTimeout 5/5 | the same setup with the fix: 10/10 green. ch13-s03: 10/10 (4 tests), build clean |
| edge-testkit-ask | ch21-s07 Charging.Api.Tests on 1 CPU + 4 loops: `SessionEndpointsTests.Given_a_start_...` 5/5 failed | ch21-s07 + 3 s: 10/10. Replayed ch20-s08 (the tag that failed 5 of 5 in verification): 10/10 (63 tests). ch12-s05: 10/10 (21), build clean |
| traced-node-ask | ch21-s07 Telemetry.Tests beside Charging.Actors on 2 CPUs + 8 loops: `NodeMetricTests` and `TapToJournalTraceTests` 504, 3/3 | the same setup with the fix: 10/10 with no 504. Runs 5 and 9 failed only `LinkTests` (the warm-up timer race, timer-races family). ch17-s08 build clean |
| simulator-retries | ch21-s07 SystemTests on 2 CPUs + 6 loops: 3/3 failed (504 twice, InternalError once) | the same setup with the fix: replayed ch21-s09 5/5 green (1m44s to 2m06s). With 12 loops on 2 CPUs: 4/5 (the failure was a split-brain downing, see "Not fixed"). ch16-s07 build clean |

Every patch passes `git apply --check` at its introducing tag. The full replay (ch10-s06..main) built
`Ohmline.slnx` with `-warnaserror` at the tip: 0 warnings, 0 errors. The final confirmation run
`dotnet test --solution Ohmline.slnx` at the replayed ch21-s09 (machine load 26 to 33 from other
agents) gave **total 2664, failed 0**, the book's figure for ch21-s09.
