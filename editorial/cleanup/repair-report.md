# Sample history repair after re-verification: report (2026-11-06)

The re-verification of the rewritten history (`reverify-ch08-14.md`, `reverify-ch15-21.md`) left
12 failure items. Each one was diagnosed. Eight test-only fixes were then amended into the commits
that introduced the defects, using the rewrite's replay method. Every later commit, every tag and
every branch was rebuilt on top. Nothing was pushed, and no book file was changed.

## Backup

- `refs/backup/pre-repair/heads/*` (13) and `refs/backup/pre-repair/tags/*` (175) in `sample/`
  hold the state before this repair (main was `9a203cb`). `refs/backup/pre-cleanup/*` from the
  rewrite is untouched.
- Tag map: `editorial/cleanup/repair-tag-map.tsv`, with columns tag, old tag object, old commit,
  new tag object and new commit. "Old" means pre-repair.
- To restore the pre-repair state:

  ```
  git for-each-ref --format='%(refname)' refs/backup/pre-repair/ | while read r; do
    git update-ref "refs/${r#refs/backup/pre-repair/}" "$r"; done
  ```

## Diagnosis of each item

| # | item | verdict | action |
| --- | --- | --- | --- |
| 1 | ch12-s06 `ChargePointListingActorTests` snapshot test | Real test race, introduced at ch11-s07 and fixed forward at ch13-s03 (`SettledAsync`). The test kit's in-memory snapshot store can lose a snapshot that is saved while an older one is being deleted. | **Fixed at ch11-s07** (`listing-snapshot-settled`) |
| 2 | ch12-s11 `SitePowerBudgetActorTests`: AskTimeout 3 s in `PersistenceShellSpec.SeedAsync` | Cold start. The seeder's ask is the first write of a fresh actor system, so it pays for the journal plugin starting, under a fixed 3 s deadline. | **Fixed at ch11-s08** (`persistence-shell-seed`) |
| 3 | ch14-s05 `SiteEnergyEndpointsTests`, 500 ms ask | The `edge-testkit-ask` pattern. The probe's reply came after the 500 ms `AskTimeout` and went to dead letters. Introduced at ch12-s05. | **Fixed at ch12-s05** (`site-energy-api-ask`) |
| 4 | ch14-s06 `DriverAppTests.ChargeAsync`, Accepted instead of OK | Real read-your-writes race on a freshly started node. Introduced at ch14-s06, fixed forward at ch15-s01. | **Fixed at ch14-s06** (`driver-app-charge-waits`, the ch15-s01 change verbatim) |
| 4b | (same race, not observed) `ReplayTests.ChargeAndStopAsync` | Introduced at ch14-s07, fixed forward at ch15-s06. | **Fixed at ch14-s07** (`replay-charge-and-stop-waits`, the ch15-s06 change verbatim) |
| 5 | ch19-s04 and ch19-s06 `TraceShapeTests` journey test | Real test race from ch17-s08. `SpansAsync` returned as soon as the child span `process IdTokenActor` was exported. Its parent `persisted ChargingJourneyActor` ends only after `onComplete()` returns, so it can be exported later. | **Fixed at ch17-s08** (`trace-shape-ancestors`), without touching the line the book quotes |
| 6a | ch15-s05 and ch15-s06 `SiteEnergyEndpointsTests` and `DriverAccountsEndpointsTests`, 500 ms asks | Same as item 3. `DriverAccountsEndpointsTests` was introduced at ch12-s07. | SiteEnergy as item 3. **Fixed at ch12-s07** (`driver-accounts-api-ask`) |
| 6b | ch15-s05 and ch15-s06 `SnapshotFailureTests` and `ChargingSessionActorTests`, AskTimeout 3 s in `SeedAsync` | Same as item 2. | Item 2 fix |
| 6c | ch16-s07 `ChargingSessionActorTests.Given_no_session_...`, "Timeout 00:00:03 while waiting for a message" | Same cold start, in the other branch of the code. With an empty history `SeedAsync` returned at once, so the actor under test started the journal itself, inside the 3 s `ExpectMsgAsync`. | Item 2 fix: the seeder now runs for an empty history too |
| 7 | `wrong/ch12-s04-context-stop`: 4 failures where the book says three | **Not a repo bug.** The branch fails the same 4 tests before the rewrite, after it and after this repair. The book's count is wrong. | Book fix for the editor (below) |
| 8 to 11 | `wrong/ch10-s06`, `wrong/ch10-s10`, `wrong/ch13-s04` and `wrong/ch16-s01` | Expected anti-pattern failures, as the book says. | None |
| 12 | `wrong/ch21-s02-minver-appversion`, 63 failed | By design: the startup crash section 21.2 describes. | None |
| new | ch21-s10 `JournalTraceTests.Given_traced_events_in_the_journal_then_a_new_incarnation_recovers_them`: `InvalidActorNameException: Actor name "counter-2" is not unique!` | **Found during this repair's verification** (ch21-s10 run 1, load 5). Real test race from ch17-s07. The test stops the incarnation, waits for `Terminated` and starts the next one under the same name. The user guardian can still hold the name (`TerminatingChildrenContainer.Reserve` in the stack), because the watcher's `Terminated` and the parent's removal of the child are not ordered. | **Fixed at ch17-s07** (`journal-trace-unnamed`) |

## Fixes applied (8 patches)

The patches are in `editorial/cleanup/patches/repair-*.patch`. All are test-only: no `src/` file
changes and no test is added or removed.

| id | amended into | file | change |
| --- | --- | --- | --- |
| listing-snapshot-settled | ch11-s07 | `tests/Ohmline.Charging.Actors.Tests/Listings/ChargePointListingActorTests.cs` | Backport of ch13-s03's `SettledAsync`, verbatim (+26/-4) |
| persistence-shell-seed | ch11-s08 | `tests/Ohmline.Testing/PersistenceShellSpec.cs` | The seeding patience changes from 3 s to `20 * TimeFactor` s, the same value as `sqlite-node-ask`. It is also passed to the seeder's `ExpectTerminatedAsync`. The seeder now runs for an empty history too: it answers once it has recovered, so the journal and snapshot store are started before the actor under test gets its message (+14/-7) |
| site-energy-api-ask | ch12-s05 | `tests/Ohmline.SiteEnergy.Api.Tests/SiteEnergyEndpointsTests.cs` | `AskTimeout` 500 ms -> 3 s. One line; no test waits for this timeout to expire |
| driver-accounts-api-ask | ch12-s07 | `tests/Ohmline.DriverAccounts.Api.Tests/DriverAccountsEndpointsTests.cs` | Same change, one line |
| driver-app-charge-waits | ch14-s06 | `tests/Ohmline.IntegrationTests/DriverAppTests.cs` | Backport of ch15-s01, verbatim (+6/-2) |
| replay-charge-and-stop-waits | ch14-s07 | `tests/Ohmline.IntegrationTests/ReplayTests.cs` | Backport of ch15-s06, verbatim (+7/-1) |
| journal-trace-unnamed | ch17-s07 | `tests/Ohmline.Telemetry.Tests/JournalTraceTests.cs` | `StartedAsync` starts the counter unnamed. The persistence id is still passed to the actor. Line-neutral (+2/-2) |
| trace-shape-ancestors | ch17-s08 | `tests/Ohmline.Telemetry.Tests/TraceShapeTests.cs` | `SpansAsync` now waits until the span named `last` **and every ancestor up to the server span (or the root)** have been exported. This is a new private helper, `EndedWithItsAncestors`. The call `SpansAsync(node, trace, "process IdTokenActor")` is unchanged (+18/-1) |

None of the 8 fixes caused a conflict:
- ch13-s03, ch15-s01 and ch15-s06 make the same change as the backports, so they merged cleanly. Those commits no longer touch those files.
- ch15-s03's `PersistenceShellSpec` hunk and ch19-s08's `private` -> `internal` change in `TraceShapeTests` do not overlap the fixes.
- No later commit touches the SiteEnergy/DriverAccounts option lines or `JournalTraceTests`.

## Method

- This is the rewrite's `replay.sh`, adapted (`.../scratchpad/repair/replay.sh`):
  - The source history is `refs/backup/pre-repair/heads/main`.
  - Each new tree is made with `git merge-tree --write-tree --merge-base=<old parent> <new parent> <old commit>`.
  - Each patch is applied with `git apply --cached` to the commit that its introducing tag points to.
  - Commits are made with `git commit-tree`, using the original message, author, committer and raw dates.
- The replay ran in two passes:
  - Pass 1 had 7 patches.
  - Pass 2 started again from `pre-repair` with all 8, after `JournalTraceTests` failed in verification.
  - Only the pass-2 result is on the refs.
- **Side branches:** each single commit was replayed onto its rebuilt base. The three `wrong/ch10-*` branches before ch11-s07 are unchanged (same SHAs).
- **Tags:** every tag whose commit moved got a new tag object made with `git mktag`. It is byte-identical to the old object except for its `object` line, so the message, tagger and raw date are unchanged. 84 tags (ch01-s02 to ch11-s06) keep their old tag objects.
- **Main checkout:** `sample/` is on `main` (`3566d59`) with a clean status. `git worktree list` shows only the main checkout.

### Checks (`.../scratchpad/repair/check.py`, the rewrite's check.py adapted)

- 188 commits were mapped: 88 unchanged and 100 rewritten.
- For every rewritten commit:
  - Message, author and committer are byte-identical.
  - Parents map correctly.
  - The tree differs from the pre-repair commit only in the fix files, only at or after each fix's introducing commit, and by exactly the patch's lines.
- The fix delta is gone where the same fix already existed upstream:
  - `ChargePointListingActorTests` from ch13-s03 (84 commits);
  - `DriverAppTests` from ch15-s01 (70);
  - `ReplayTests` from ch15-s06 (65).
- **Problems: 0.**
- Tag check, `git diff refs/backup/pre-repair/tags/<t> <t>`:
  - 84 tags have no difference.
  - Every other tag differs only in fix files.
  - All 175 tag bodies are identical apart from the `object` line.

| ref | pre-repair | now |
| --- | --- | --- |
| main = ch21-s09 | 9a203cb | 3566d59 |
| upgrade/akka-1.6 = ch21-s10 | 16250af | 7946730 |
| v1.0.0 | 1ee93fb | b7859b9 |
| ch11-s07 / ch11-s08 | b6961a9 / 0e09c82 | e763a46 / acf306d |
| ch12-s05 / ch12-s07 | 6177a55 / f73e0cd | ce92467 / a1e281f |
| ch14-s06 / ch14-s07 | 26bfa71 / 7084367 | f14eba0 / 2ba0cb3 |
| ch17-s07 / ch17-s08 | e1d0c85 / f361086 | 1c2ea28 / 841b072 |
| alt/ch20-s04-adapter-only | 08ca54f | d77ee4e |
| wrong/ch12-s02 / ch12-s04 / ch13-s04 / ch16-s01 / ch18-s01 / ch21-s02 | 3b6acd2 / 509524b / 6b4c279 / 9b26085 / 65f6ff7 / d4524ae | 60d1cf6 / 335e97a / fb9bcfb / 443d52c / 8764a84 / e53133d |

## Evidence for the fixes

- **persistence-shell-seed.** At ch11-s08, `Charging.Actors.Tests` was pinned to one CPU with 16 busy loops on it, unpatched and patched side by side:
  - Unpatched: **4 of 9 runs failed** with exactly the ch16-s07 message, `ChargingSessionActorTests.Given_no_session_when_it_starts_then_its_terms_are_recorded_at_version_1`: "Timeout 00:00:03 while waiting for a message of type System.Object".
  - Patched: 0 of 9 runs failed on seeding or cold start.
  - With 4 busy loops, both were green 4 of 4.
  - One patched run failed in an unrelated test (see "Not fixed").
- **trace-shape-ancestors.** This was a deterministic injection: `Thread.Sleep(300)` after `onComplete()` in `TracedPersistentActor`, so the parent span ends 300 ms after its child.
  - Unpatched ch17-s08: the journey test failed **3 of 3** with `Sequence contains no matching element`, the reported failure.
  - Patched: 3 of 3 green.
  - Without the injection, patched: 3 of 3 green.
- **listing, driver-app and replay backports.** These are the forward fixes' own changes, already verified at ch13-s03 (3 of 5 failing before, 0 of 3 after), ch15-s01 and ch15-s06 (see errata).
- **The 500 ms -> 3 s asks.** The same change and evidence as `edge-testkit-ask`. The failure log for item 3 shows the reply going to dead letters after the ask expired.
- **journal-trace-unnamed.** The stack trace (`TerminatingChildrenContainer.Reserve`) identifies the race. `JournalTraceTests` was green 3 of 3 at ch17-s07 after the fix.

## Verification of the repaired refs

Each ref was checked in a fresh detached worktree (`.../scratchpad/repair/verify/`):
- `dotnet build Ohmline.slnx -warnaserror`, then `dotnet test --solution Ohmline.slnx`.
- Tags needed **3 consecutive green full runs**, with the counter reset after a failure.
- Side branches got one run.
- Only one suite ran at a time. The load average was 3 to 12.

Raw results: `.../scratchpad/repair/verify-logs/results.tsv`.

**Every build had 0 warnings and 0 errors.** Every total matches progress.md and the book.

| ref (commit) | total | runs | note |
| --- | --- | --- | --- |
| ch11-s07 (e763a46) | 1166 | P P P | listing fix |
| ch11-s08 (acf306d) | 1170 | P P P | seeding fix |
| ch12-s05 (ce92467) | 1279 | P P P | SiteEnergy ask |
| ch12-s06 (beab219) | 1293 | P P P | item 1 |
| ch12-s07 (a1e281f) | 1408 | P P P | DriverAccounts ask |
| ch12-s11 (e7e8494) | 1481 | P P P | item 2 |
| ch14-s05 (2cc523e) | 1628 | P P P | item 3 |
| ch14-s06 (f14eba0) | 1655 | P P P | item 4 |
| ch14-s07 (2ba0cb3) | 1661 | P P P | ReplayTests backport |
| ch15-s05 (fc3cf04) | 1859 | P P P | item 6 (4 of 7 failed before) |
| ch15-s06 (070869d) | 1871 | P P P | item 6 |
| ch16-s07 (18abde8) | 2070 | P P P | item 6c |
| ch17-s07 (1c2ea28) | 2169 | P P P | JournalTraceTests fix (pass 2) |
| ch17-s08 (841b072) | 2172 | P P P | TraceShape fix (pass 2; pass 1 was also P P P) |
| ch19-s04 (6750e26) | 2294 | P P P | item 5 (pass 2; pass 1 was also P P P) |
| ch19-s06 (c2bbbc5) | 2361 | P P P | item 5 (pass 2; pass 1 was also P P P) |
| ch21-s09 = main (3566d59) | 2664 | P P P | pass 2; pass 1 was also P P P |
| ch21-s10 = upgrade/akka-1.6 (7946730) | 2676 | P P P | pass 2. Pass 1 was F P P P: the run-1 failure was `JournalTraceTests`, which led to the pass-2 fix |
| v1.0.0 (b7859b9) | 2664 | P | |
| alt/ch20-s04-adapter-only (d77ee4e) | 2433 | P | matches the book's 2,433 |
| wrong/ch12-s02-actorref-as-identity (60d1cf6) | 1236 | P | as the book says |
| wrong/ch12-s04-context-stop (335e97a) | 1243 | 4 failed | the same 4 as before the rewrite; see below |
| wrong/ch13-s04-offset-before-handler (fb9bcfb) | 1553 | 2 failed | as the book says |
| wrong/ch16-s01-dual-write (443d52c) | 1915 | 2 failed | as the book says |
| wrong/ch18-s01-high-cardinality-tags (8764a84) | 2180 | P | as the book says |
| wrong/ch21-s02-minver-appversion (e53133d) | 2614 | 63 failed | by design, the same 63 |

Pass-1 runs at refs before ch17-s07 count as verification of the final refs: pass 2 left those
commits byte-identical, and the SHAs above are the current ones. Across both passes there were
70 full-suite runs at `chNN-sMM` tags, plus 2 at v1.0.0. The only unexpected failure was
ch21-s10's pass-1 run 1.

Not repeated: the mobile head builds. No file under `mobile/` or `src/` changed, so the MAUI head
is the same as in the re-verification, which was 0/0 at all 28 refs.

## Tags that remain red

**None of the `chNN-sMM` tags, `v1.0.0`, `main` or `upgrade/akka-1.6`.** The only refs that fail
are the anti-pattern branches, which fail by design. Each fails exactly the tests the book names,
except `wrong/ch12-s04-context-stop`:

- `wrong/ch10-s06-persist-async-decider` (953, 1 failed), `wrong/ch10-s10-logic-in-receive` (1094, 2 failed): unchanged SHAs, so not re-run. Their results are in reverify-ch15-21.
- `wrong/ch13-s04-offset-before-handler`: 1553, 2 failed (the two `ProjectionRunnerTests`).
- `wrong/ch16-s01-dual-write`: 1915, 2 failed (both `IntegrationEventRelayTests`).
- `wrong/ch21-s02-minver-appversion`: 2614, 63 failed. These are the same 63 as before: 2 `AppVersionTests`, 60 node-starting tests with `ArgumentOutOfRangeException` at `AppVersion.Parse`, and `ChargeToReceiptTests`.
- `wrong/ch12-s04-context-stop`: 1243, **4 failed**:
  - `PassivationTests.Given_a_session_that_ends_then_it_asks_its_shard_to_passivate_it`
  - `PassivationTests.Given_a_command_behind_the_last_write_then_it_is_answered_before_the_stop`
  - `ChargingSessionActorTests.Given_a_metered_session_when_stopped_then_it_ends_with_the_energy_delivered`
  - `Sqlite.ShardRegionTests.Given_an_ended_session_then_its_shard_stops_it_until_the_next_command`

  All four follow from the anti-pattern, and the result was the same before the rewrite.

## Not fixed (for the editor)

- **`ChargePointActorTests.Given_accepted_commands_then_each_version_counts_the_events_persisted`.**
  - **What happened:** under extreme contention (1 CPU shared with 16 busy loops) it failed once in 9 patched runs. The `ReservationExpired` carried the timer's command id instead of the `Reserve` command's.
  - **Cause:** this is a separate race and was there before this repair. On recovery, `ChargePointActor` restarts a lapsed reservation's timer as due at once. If the test thread is starved between `ActorOf` and `Tell`, the timer's `ExpireReservation` is decided first.
  - **Frequency:** it was not seen in any full-suite run, before or after the rewrite.
  - **Why it is left:** the book quotes the test and `ThenAsync` at ch11-s08 (ch11 lines 749 and 780), so any fix needs an editor decision. It is not amended.
- **The 3 s TestKit expects** in `PersistenceShellSpec.ThenAsync` (book-quoted) still scale only with `OHMLINE_TIMEFACTOR`. After the seeding fix they cover only a warm actor's reply.

## Book impact (the book was not changed)

### Listings

- **Unchanged.** The `TraceShapeTests.cs @ ch17-s08` listing (ch17 line 715) still matches the tag line for line, including `var spans = await SpansAsync(node, trace, "process IdTokenActor");`. So does `PersistenceShellSpec.cs @ ch11-s08` (ch11 line 749).
- The book does not quote any other changed file.

### Test totals

No test total changed at any tag or branch.

### `--stat` totals that change

All 174 `git diff ... --stat` commands in the book were re-run on the new refs. "Book" is what the
text says now (pre-cleanup), "after rewrite" is the rewrite report's figure, and "now" is after
this repair. Every other quoted stat is unchanged by this repair, including the side-branch stats
at ch20:441 (7/68/75) and ch21:237 (3/19/9).

| book line | range | book | after rewrite | now |
| --- | --- | --- | --- | --- |
| ch11:732 | ch11-s06..s07 | 27 files changed, 930 insertions(+), 3 deletions(-) | 27 / 935 / 3 | **27 files changed, 957 insertions(+), 3 deletions(-)** |
| ch11:831 | ch11-s07..s08 | 11 files changed, 489 insertions(+), 245 deletions(-) | same | **11 files changed, 496 insertions(+), 245 deletions(-)** |
| ch13:239 | ch13-s02..s03 | 10 files changed, 376 insertions(+), 4 deletions(-) | 10 / 382 / 4 | **9 files changed, 356 insertions(+)** |
| ch14:742 | ch14-s05..s06 | 28 files changed, 1183 insertions(+), 7 deletions(-) | same | **28 files changed, 1187 insertions(+), 7 deletions(-)** |
| ch14:795 | ch14-s06..s07 | 9 files changed, 468 insertions(+) | same | **9 files changed, 474 insertions(+)** |
| ch15:132 | ch14-s07..ch15-s01 | 12 files changed, 366 insertions(+), 3 deletions(-) | same | **11 files changed, 360 insertions(+), 1 deletion(-)** |
| ch15:683 | ch15-s05..s06 | 23 files changed, 593 insertions(+), 130 deletions(-) | same | **22 files changed, 586 insertions(+), 129 deletions(-)** |
| ch17:748 | ch17-s07..s08 | 5 files changed, 290 insertions(+), 2 deletions(-) | 5 / 295 / 2 | **5 files changed, 312 insertions(+), 2 deletions(-)** |

Line numbers are those of the sentence that quotes the stat. Each stat comes from a command 5
lines above it: ch11:727, ch11:826, ch13:234, ch14:737, ch14:790, ch15:127, ch15:678 and ch17:743.

### Text

- **ch12 line 386:** "three tests from main fail" should be "four tests from main fail". The four are listed above. `PassivationTests.Given_a_session_that_ends_then_it_asks_its_shard_to_passivate_it` with `Timeout 00:00:03 while waiting for a message of type ...CommandAccepted` is still among them, so the rest of the sentence holds.
- **errata.md:**
  - The ch12 "Observed flake" and ch13 entries for `ChargePointListingActorTests` are now amended at ch11-s07.
  - The ch15-s01 `DriverAppTests` and ch15-s06 `ReplayTests` "fix forward" entries are now amended at ch14-s06 and ch14-s07. The ch15-s01 and ch15-s06 commits no longer touch those files, although their commit messages are unchanged.
  - The ch18-s03 `TraceShapeTests` mention is now fixed at ch17-s08.
  - Add a closed entry for `JournalTraceTests`, ch17-s07.

### Leftover wording in the repo (kept, because commit messages are never edited)

- The ch13-s03 commit message still says it "Fixes forward a snapshot race in a listing actor test". After the repair, ch13-s03 no longer touches that test. The ch15-s01 and ch15-s06 messages do not mention their test fixes.
- The backported `SettledAsync` doc comment cites "(errata, Chapter 13)", as ch13-s03 wrote it. Keeping the text verbatim is what let ch13-s03 merge cleanly.
