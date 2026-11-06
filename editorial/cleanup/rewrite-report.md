# Sample history rewrite: report (2026-11-05)

The author approved this rewrite (queued-jobs.md job 1). Each of the 14 fixes is now in the commit
that introduced the bug. Every later commit, all 175 annotated tags and all 13 branches were
rebuilt on top. Nothing was pushed.

## Backup (left in place for the author to remove)

- Bundle: `editorial/cleanup/sample-before-cleanup.bundle`, `--all`, 1.8 MB. `git bundle verify`
  passed.
- Refs: `refs/backup/pre-cleanup/heads/*` (13) and `refs/backup/pre-cleanup/tags/*` (175) in
  `sample/`. Each is a copy of the original ref.
- Tag map: `editorial/cleanup/tag-map.tsv`. Columns are tag, old tag object, old commit, new tag
  object, new commit.

To restore the old history (not needed now):

```
git for-each-ref --format='%(refname)' refs/backup/pre-cleanup/ | while read r; do
  git update-ref "refs/${r#refs/backup/pre-cleanup/}" "$r"; done
```

Restore tags with `git update-ref refs/tags/<t> refs/backup/pre-cleanup/tags/<t>`.

## Method

- **No worktree for the replay.** For each commit of `git rev-list --reverse main`, the new tree
  is `git merge-tree --write-tree --merge-base=<old parent> <new parent> <old commit>`. When the
  new parent's tree equals the old parent's tree, the original tree is reused unchanged.
- **Fixes.** At the commit an introducing tag points to, the fix patch is applied with
  `git apply --cached` on a temporary index and becomes part of that commit's tree.
- **Commits** are created with `git commit-tree`. The original raw message is reused verbatim,
  and author and committer name, email and raw date all come from the original commit.
- **Conflicts** stopped the script. Each was resolved by hand, and the resolved file was fed back
  in.
- **Side branches.** Each single commit was replayed onto the new commit of its base tag the same
  way. `upgrade/akka-1.6` is based on the new `ch21-s09`.
- **Tags.** All 175 were re-created with `git tag -f -a -F <original message>` on the mapped
  commit, using the original tagger identity and raw date. `ch18-s01` got the corrected message
  `18.1 Domain metrics with a cardinality budget`.
- **Scripts** are in the session scratchpad, `.../scratchpad/rewrite/` (`replay.sh`, `check.py`,
  `smoke.sh`).

## Fixes applied (14 patches plus 1 tag message)

| id | amended into | files |
| --- | --- | --- |
| ch01-dod-exemption | ch01-s07 | README.md, tools/verify-tags.sh |
| logger-race | ch02-s09 | learn/Ch02.Hosted.Tests/ChargerAppTests.cs |
| sqlite-node-ask | ch10-s07 | tests/Ohmline.Charging.Actors.Tests/Sqlite/SqliteNode.cs |
| two-incarnations-ask | ch10-s09 | .../JournalTour/TwoIncarnationsTests.cs |
| state-sourcing-ask | ch11-s07 | .../JournalTour/StateSourcingTourTests.cs |
| edge-testkit-ask | ch12-s05 | tests/Ohmline.Charging.Api.Tests/EdgeTestKit.cs, ChargePointEndpointsTests.cs |
| journal-node-ask | ch13-s03 | tests/Ohmline.Charging.ReadModels.Tests/JournalNode.cs |
| fixture-ensures-schema | ch13-s08 | tests/Ohmline.IntegrationTests/PostgresFixture.cs |
| simulator-retries | ch16-s07 | tools/Ohmline.Simulator/Driver.cs, SimulatedCharger.cs |
| grid-signal-poller | ch16-s08 | tests/Ohmline.SiteEnergy.Acl.GridOperator.Tests/GridSignalPollerTests.cs |
| link-tests | ch17-s07 | tests/Ohmline.Telemetry.Tests/LinkTests.cs |
| traced-node-ask | ch17-s08 | tests/Ohmline.Telemetry.Tests/TracedNode.cs |
| maui-cs0200 | ch19-s04 | mobile/Directory.Build.props |
| gateway-drain | ch21-s07 | tests/Ohmline.DeviceGateway.Tests/GatewayDrainTests.cs |
| ch18-s01-tag-message | tag ch18-s01 | message only, no tree change |

None was skipped.

## Conflicts and how they were resolved

All of them matched the diagnoses' predictions.

| at | file | resolution |
| --- | --- | --- |
| ch12-s02 | SqliteNode.cs (usings) | Union, sorted: `ActorShell.Clustering`, `Charging.Contracts.ChargePoints`, `Testing`. The result is ch12-s02's file plus exactly the fix. |
| ch12-s06 | ChargePointEndpointsTests.cs | ch12-s06's two-line detail, with `0.5 s` changed to `3 s`. This is identical to the text ch21-s08 later wrote. |
| ch18-s01 | JournalNode.cs (usings) | Both kept: `Charging.Actors.Diagnostics`, then `Testing`. |
| ch18-s04 | SqliteNode.cs, TwoIncarnationsTests.cs, StateSourcingTourTests.cs | Took ch18-s04's comment (the forward fix). All three files are now byte-identical to ch18-s04's originals. |
| ch20-s08 | PostgresFixture.cs | Took ch20-s08's file verbatim, since the migrator is the fuller fix and `ReadModelSchema` is gone. |
| ch21-s07 | SimulatedCharger.cs (end of file) | Both kept: `CloseFrame`, then `GatewayCallError`. |
| ch21-s08 | EdgeTestKit.cs | Took ch21-s08's comment verbatim (the forward fix). The file is byte-identical to ch21-s08's original. |

`tools/verify-tags.sh` at ch19-s04 merged cleanly in three ways. It keeps ch19-s04's mobile
header lines and gains the Chapter 1 exemption comment.

## Verification

### Every rewritten commit (188: 176 on main, 12 side commits), checked by check.py

- The message, author and committer lines (identity and date) are byte-identical to the
  original. Each parent maps to the parent's rewritten commit.
- The tree differs from the original only in fix files, and only at or after that fix's
  introducing commit.
- For each file, the added and removed lines (old to new) equal the patch's lines.
- The one expected variant is ChargePointEndpointsTests.cs from ch12-s06 to ch21-s07. There, the
  delta is the uniform swap of `0.5 s` for `3 s` in the two-line message.
- The fix delta is gone exactly where a later commit fixed the same thing forward:
  - SqliteNode, TwoIncarnations and StateSourcing from ch18-s04 (39 commits);
  - PostgresFixture from ch20-s08 (17 commits);
  - EdgeTestKit and ChargePointEndpointsTests from ch21-s08 (6 commits).
- Problems found: 0.

### Every tag: `git diff refs/backup/pre-cleanup/tags/<t> <t>`

- ch01-s02, s03, s04 and s06 are unchanged. Their tag objects are byte-identical to the
  originals.
- Every other tag differs only in the fix files listed above whose introducing tag is at or
  before it.
- Unexpected diffs: none.
- Every tag is annotated, with tagger `Ohmline Book <book@ohmline.example>` and the original raw
  date.
- Every message is byte-identical to the original, except ch18-s01, which is now
  `18.1 Domain metrics with a cardinality budget`.
- `git tag -l --format='%(refname:short) %(contents:subject)'` now shows the `NN.M <title>` form
  for every ch tag.

### Branches

- `main`: 176 commits, linear, no merges. 82baae1 is now 9a203cb (`ch21-s09`). `v1.0.0` is now on
  1ee93fb.
- `upgrade/akka-1.6`: f9ed698 is now 16250af. Tag `ch21-s10` stays on this branch.
- `alt/ch20-s04-adapter-only` and the 10 `wrong/*` branches: each is still one commit on its
  rewritten base tag.
- The checkout in `sample/` is on `main`, with a clean status. `git worktree list` shows only the
  main checkout; the smoke worktree has been removed.

### Smoke tests (quiet machine, load about 2 to 3)

Every build used `dotnet build Ohmline.slnx -warnaserror`, from a clean checkout of each tag in
one scratch worktree. All of them gave 0 warnings and 0 errors.

| tag | tests (all green) |
| --- | --- |
| ch01-s07 | `bash -n` OK. `verify-tags.sh 'ch01-*'` gave `0 passed, 5 skipped, 0 failed` (output unchanged). `verify-tags.sh ch02-s01` gave `1 passed`. |
| ch02-s09 | Ch02.Hosted.Tests 4/4, run 3 times |
| ch10-s07 | Charging.Actors.Tests 121 |
| ch10-s09 | Charging.Actors.Tests 124 |
| ch11-s07 | Charging.Actors.Tests 245 |
| ch12-s05 | Charging.Api.Tests 21 |
| ch13-s03 | ReadJournalTourTests 4 |
| ch13-s08 | IntegrationTests 11 (Podman) |
| ch16-s07 | build only (simulator; SystemTests left to the next phase) |
| ch16-s08 | GridOperator.Tests 16 |
| ch17-s07 | LinkTests 2 |
| ch17-s08 | Telemetry.Tests 22 |
| ch19-s04 | `mobile/Ohmline.Mobile.slnx -warnaserror` 0/0 |
| ch21-s07 | GatewayDrainTests 1 |
| ch21-s09 (main) | mobile 0/0. Ch02.Hosted 4, Charging.Actors 601, Charging.Api 67, ReadModels 90, IntegrationTests 42, GridOperator 16, Telemetry 40, DeviceGateway 17 |
| ch21-s10 (Akka 1.6) | same projects and totals as ch21-s09, mobile 0/0 |

The mobile head was built at ch19-s04 and then at ch21-s09 in the same worktree path. That is the
setup that used to trigger CS0200, and it built clean. Full-solution runs, repeated runs at the
formerly flaky tags and the SystemTests are for the next phase.

## Book impact (for the editor; the book was not changed here)

### Diff --stat lines the book quotes that are now different

All were measured on the rewritten history.

| book line | range | old | new |
| --- | --- | --- | --- |
| ch01:492 | ch01-s06..s07 | 9 files changed, 261 insertions(+) | 9 files changed, 267 insertions(+) |
| ch02:1025 | ch02-s08..s09 | 10 files changed, 242 insertions(+) | 10 files changed, 258 insertions(+) |
| ch10:728 | ch10-s06..s07 | 9 files changed, 150 insertions(+), 3 deletions(-) | 9 files changed, 156 insertions(+), 3 deletions(-) |
| ch10:889 | ch10-s08..s09 | 2 files changed, 150 insertions(+) | 2 files changed, 156 insertions(+) |
| ch11:732 | ch11-s06..s07 | 27 files changed, 930 insertions(+), 3 deletions(-) | 27 files changed, 935 insertions(+), 3 deletions(-) |
| ch12:530 | ch12-s04..s05 | 56 files changed, 1922 insertions(+), 10 deletions(-) | 56 files changed, 1926 insertions(+), 10 deletions(-) |
| ch13:239 | ch13-s02..s03 | 10 files changed, 376 insertions(+), 4 deletions(-) | 10 files changed, 382 insertions(+), 4 deletions(-) |
| ch13:796 | ch13-s07..s08 | 25 files changed, 866 insertions(+), 12 deletions(-) | 26 files changed, 881 insertions(+), 12 deletions(-) |
| ch16:771 | ch16-s06..s07 | 53 files changed, 2000 insertions(+), 226 deletions(-) | 53 files changed, 2055 insertions(+), 226 deletions(-) |
| **ch16:880** | ch16-s07..s08 | 31 files changed, 1078 insertions(+), 5 deletions(-) | 31 files changed, 1082 insertions(+), 5 deletions(-) |
| **ch17:688** | ch17-s06..s07 | 22 files changed, 800 insertions(+), 46 deletions(-) | 22 files changed, 803 insertions(+), 46 deletions(-) |
| ch17:748 | ch17-s07..s08 | 5 files changed, 290 insertions(+), 2 deletions(-) | 5 files changed, 295 insertions(+), 2 deletions(-) |
| ch18:427 | ch18-s03..s04 | 15 files changed, 411 insertions(+), 73 deletions(-) | 15 files changed, 397 insertions(+), 76 deletions(-) |
| ch19:347 | ch19-s03..s04, filtered | 30 files changed, 761 insertions(+) | 30 files changed, 768 insertions(+) |
| ch20:768 | ch20-s07..s08 | 48 files changed, 908 insertions(+), 300 deletions(-) | 48 files changed, 903 insertions(+), 310 deletions(-) |
| **ch21:799** | ch21-s06..s07 | 25 files changed, 912 insertions(+), 5 deletions(-) | 25 files changed, 931 insertions(+), 5 deletions(-) |
| ch21:886 | ch21-s07..s08 | 39 files changed, 733 insertions(+), 42 deletions(-) | 38 files changed, 729 insertions(+), 42 deletions(-) |

The three rows in bold were **not** in any diagnosis: the timer-race diagnoses reported "no book
impact" but missed their sections' stat lines. ch19-s04's unfiltered stat changes from 56/1621 to
56/1628, but the book does not quote it.

The stats at ch12-s02, ch12-s06, ch18-s01 and every other tag are unchanged. So is the output of
`git diff ch02-s08 ch02-s09 -- learn/Ch02.Hosted`, which has the same md5.

### Other text to update (from the diagnoses)

- **ch17 §17.8 TracedNode.cs listing** (about lines 696-707, @ ch17-s08). It must gain the
  2-line comment and the three `UseSetting("Ohmline:...:AskTimeout", "00:00:20")` lines.
- **ch17 line 737** (Note on the LinkTests warm-up race) and **line 798** (Exercise 2): the repo
  now already contains the fix. Drop the Note and rewrite or drop the exercise.
  `editorial/review-log.md:368` refers to the same Note.
- **ch19 line 463** (19.5 Version note about `dotnet build-server shutdown` after CS0200): replace
  it with the fact that `mobile/Directory.Build.props` sets `EnableMauiIncrementalHotReload=false`.
  Line 295 could mention the property (optional).
- **errata.md**: close the entries for the fixes above.
- **editorial/sample-architecture.md §10** still documents the tag message as
  `chNN-sMM: <title>`. It should be `NN.M <title>`, the form every tag now uses.
- **Test totals**: no test total changed at any tag.

### Leftover wording in the repo (kept, because the later commit's version wins)

- The ch18-s04 commit message still ends "SQLite tests wait longer". After the rewrite, ch18-s04
  changes only the doc comments of the three SQLite helpers. The original commit message was kept
  as instructed.
- ch21-s08's EdgeTestKit comment keeps its parenthetical "(500 ms timed out now and then from
  Chapter 21's test count on)", although 3 s has been in place since ch12-s05. Removing it would
  mean changing ch21-s08's own content, so it was left for the editor.
