# Re-verification after the history rewrite: ch08-* to ch14-* (2026-11-05/06)

## Method

- **Tags:** 55, `ch08-s01` to `ch14-s07`, in order. Each tag got a fresh detached worktree at
  `.../scratchpad/reverify-ch08-14/<tag>`. Each ran `dotnet build Ohmline.slnx -warnaserror`,
  then `dotnet test --solution Ohmline.slnx`, and the worktree was removed afterwards.
- **Toolchain:** `tools/env.sh` SDK, with rootless Podman through `DOCKER_HOST`.
- **Run rule:**
  - Most tags needed one green full run.
  - The tags with a fix or a past flake, plus the last 3 tags of each chapter, needed **3
    consecutive green full runs**. Those are ch10-s07, ch10-s09, ch11-s07, ch12-s05, ch13-s03,
    ch13-s08, ch14-s01, plus ch08-s05..s07, ch09-s04..s06, ch10-s08..s10, ch11-s06..s08,
    ch12-s09..s11, ch13-s06..s08 and ch14-s05..s07.
  - After a failure, the run counter went back to zero, with at most 3 extra runs per tag.
- **Load:** the machine was **not** quiet. Two other verification agents ran at the same time, and
  the 1-minute load average at the start of each run was 6 to 60. It was about 35 to 60 during
  ch10-s10 to ch12-s06, and about 10 to 25 from ch12-s07 on.
- **No refs were changed** and nothing was committed. `git worktree list` shows no ch08-14
  worktree left.
- **Raw data:**
  - `results.tsv`, `repro.tsv`, `contend.tsv` and `logs/` in
    `/tmp/claude-1000/-home-jonathan-code-scienceNight/45f474fc-e89c-490e-8b5f-ec3ffd186198/scratchpad/reverify-ch08-14/`.
  - The scripts there are `run.sh`, `repro.sh` and `contend.sh`.

## Result

- **Builds:** all 55 tags built with **0 warnings and 0 errors**.
- **Test totals:** every tag's total matches progress.md, **55 of 55**, with no differences.
- **Green runs:** every tag has a green full run, and every tag that needed 3 has 3 consecutive
  green runs.
- **Failures:** 113 full runs in total, 4 failed, and each failure was a single test.

| tag | expected = got | runs (P = pass, F = fail) |
| --- | --- | --- |
| ch08-s01..s04 | 573, 601, 616, 636 | P each |
| ch08-s05, s06, s07 | 643, 654, 662 | PPP each |
| ch09-s01..s03 | 662, 668, 708 | P each |
| ch09-s04, s05, s06 | 723, 746, 809 | PPP each |
| ch10-s01..s06 | 809, 822, 934, 944, 945, 952 | P each |
| ch10-s07, s08, s09, s10 | 953, 955, 956, 1093 | PPP each |
| ch11-s01..s04 | 1096, 1116, 1119, 1124 | P each |
| ch11-s06, s07, s08 | 1128, 1166, 1170 | PPP each |
| ch12-s01..s04 | 1170, 1235, 1236, 1242 | P each |
| ch12-s05 | 1279 | PPP |
| **ch12-s06** | 1293 | **F**P |
| ch12-s07, s08 | 1408, 1469 | P each |
| ch12-s09, s10 | 1476, 1476 | PPP each |
| **ch12-s11** | 1481 | PP**F**PPP |
| ch13-s02, s04, s05 | 1545, 1552, 1564 | P each |
| ch13-s03, s06, s07, s08 | 1549, 1567, 1573, 1594 | PPP each |
| ch14-s01 | 1607 | PPP |
| ch14-s02, s03, s04 | 1607, 1614, 1626 | P each |
| **ch14-s05** | 1628 | PP**F**PPP |
| **ch14-s06** | 1655 | **F**PPP |
| ch14-s07 | 1661 | PPP |

## Compared with the pre-rewrite verification in progress.md

- **Totals:** identical at all 55 tags.
- **The rewrite fixes held:**
  - **Ch02.Hosted shutdown race:** no failures in 113 runs. Before the rewrite it hit first runs
    at ch08-s01, ch08-s07, ch09-s01, ch11-s01, ch12-s09, ch13-s03 and ch13-s07/s08.
  - **ch14-s01:** green 3 of 3. It failed 2 of 2 before the rewrite (read-model schema race, now
    `fixture-ensures-schema` at ch13-s08).
  - **SQLite AskTimeouts:** gone. Before, they hit ch14-s03 and the first run of ch13-s07.
  - **ch10-s07, ch10-s09, ch11-s07, ch12-s05, ch13-s03 and ch13-s08:** green 3 of 3, which
    confirms each fix in place.

## The 4 failures, explained

None of the four reproduced when its project ran alone. They were also run pinned to 2 CPUs, and
on 1 CPU with 3 busy loops on the same CPU:

| tag | project | alone | 2 CPUs | 1 CPU + 3 busy loops |
| --- | --- | --- | --- | --- |
| ch12-s06 | Charging.Actors.Tests (300) | 4/4 green | 4/4 green | 2/2 green |
| ch12-s11 | SiteEnergy.Actors.Tests (106) | 5/5 green | 5/5 green | 3/3 green |
| ch14-s05 | SiteEnergy.Api.Tests (4) | 5/5 green | 5/5 green | 3/3 green |
| ch14-s06 | IntegrationTests (24) | 3/3 green | 3/3 green | (not run) |

All four appear only in full-solution runs on a loaded machine. Each one is explained below by the
test code, the failure message and the existing errata.

1. **ch12-s06, run 1 (load 56):** a known flake, fixed forward at ch13-s03.
   - **Test:** `Ohmline.Charging.Actors.Tests.Listings.ChargePointListingActorTests.Given_revisions_when_a_new_incarnation_starts_then_the_snapshot_is_enough`.
   - **Message:** "Timeout (00:00:03) while waiting for messages. Only received 0/1 messages that
     matched filter [Info when Message contains ": snapshot at 3, 0 events replayed"]". The new
     incarnation logged "recovered to sequence number 3 ... no snapshot, 0 events replayed".
   - **Cause:** this is the errata's in-memory snapshot store race. A snapshot saved while an older
     one is being deleted gets lost. The test dates from ch11-s07 and was fixed forward at
     ch13-s03 with `SettledAsync`. The rewrite did not amend it.
   - Before the rewrite it showed at ch12-s02, s08 and s09, and it can show at any tag from
     ch11-s07 to ch13-s02.
   - **Not a regression.** It is a candidate for the same kind of backport as the other fixes, at
     ch11-s07, if the author wants every tag to be deterministic.
2. **ch12-s11, run 3 (load 21):** a load flake that no diagnosis covers.
   - **Test:** `Ohmline.SiteEnergy.Actors.Tests.Budgets.SitePowerBudgetActorTests.Given_a_busy_site_when_the_grid_curtails_then_versions_count_every_event`.
   - **Message:** `Akka.Actor.AskTimeoutException : Timeout after 3.00 seconds` at
     `Ohmline.Testing.PersistenceShellSpec.Scenario.SeedAsync()` (PersistenceShellSpec.cs:102).
   - **Cause:** the seeder's `Ask<Seeded>` has a fixed `3 * TimeFactor` second deadline, from
     ch11-s08. The test kit's journal had only just auto-started, and the seeder did not answer
     within 3 s on the loaded machine.
   - This is the same family as `diagnosis-load-timeouts.md`, but `PersistenceShellSpec` is not
     in that diagnosis. The spec is used by the ChargePoint, ChargingSession and SitePowerBudget
     actor tests from ch11-s08 to the head.
3. **ch14-s05, run 3 (load 10):** a load flake that the diagnosis listed as "latent, not observed
   failing". It has now been observed.
   - **Test:** `Ohmline.SiteEnergy.Api.Tests.SiteEnergyEndpointsTests.Given_a_site_then_its_budget_is_configured_with_every_connector`.
   - **Message:** "(await answer).Result should be of type Accepted<CommandAcceptedResponse> but was
     ProblemHttpResult" (SiteEnergyEndpointsTests.cs:72).
   - **Cause:** the test log shows a dead letter, `[BudgetCommandAccepted] from [akka://test/temp/c]
     to [deadLetters]`. The probe's reply arrived after the endpoint's 500 ms `AskTimeout` had
     expired.
   - This is the pattern that `edge-testkit-ask` fixed for Charging.Api.Tests (500 ms to 3 s at
     ch12-s05). The 500 ms option in SiteEnergyEndpointsTests also dates from ch12-s05, and it is
     still present at `ch21-s09`.
   - The same backport, `AskTimeout` 3 s in `SiteEnergyEndpointsTests.Options` from ch12-s05,
     would close it. The diagnosis says DriverAccounts.Api.Tests has the same latent 500 ms.
4. **ch14-s06, run 1 (load 12):** a known race, fixed forward at ch15-s01.
   - **Test:** `Ohmline.IntegrationTests.DriverAppTests.Given_an_app_in_its_tokens_group_when_the_session_stops_then_it_hears_it`.
   - **Message:** "(await http.GetAsync(started.Headers.Location, Ct)).StatusCode should be
     HttpStatusCode.OK but was HttpStatusCode.Accepted", in `DriverAppTests.ChargeAsync`
     (DriverAppTests.cs:133).
   - **Cause:** this is the errata's "ch15-s01: fix forward in `DriverAppTests.ChargeAsync`".
     Read-your-writes waits 2 s, and on a freshly started node the projection daemons can still
     be starting. The test dates from ch14-s06, and the rewrite did not amend it.
   - `ReplayTests.ChargeAndStopAsync` at ch14-s07 has the same race, fixed at ch15-s06. It did not
     fail here (3 of 3 green).

## Suggestions for the editor (nothing was changed)

- Close the progress.md and errata items for ch14-s01, the ch14-s03 SQLite AskTimeouts, and the
  Ch02.Hosted race in this range.
- If every tag should be deterministic under load, consider three more backports:
  - `ChargePointListingActorTests` `SettledAsync` at ch11-s07;
  - SiteEnergy.Api (and DriverAccounts.Api) test `AskTimeout` 500 ms to 3 s at ch12-s05 and
    their introducing tags;
  - `DriverAppTests.ChargeAsync` waiting at ch14-s06 and `ReplayTests` at ch14-s07.
- The `PersistenceShellSpec` seeding ask (3 s) is a new item for `diagnosis-load-timeouts.md`.
