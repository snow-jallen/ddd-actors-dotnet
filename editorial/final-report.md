# Final testing report (2026-11-05)

Toolchain: local SDK 11.0.100-rc.3.26522.148 (`tools/env.sh`), local Android SDK/JDK, rootless
Podman via `DOCKER_HOST`. The sample repo was left unchanged.

## 1. Last tag: build, test, MAUI head

- **Last tag:** `ch21-s10` (f9ed698, created 18:54). It is on branch `upgrade/akka-1.6` (Akka.NET
  1.6), not on `main`. `main` HEAD is `ch21-s09` (82baae1). `v1.0.0` is an older tag (18:39).
- **Worktree:** a detached worktree at `.../scratchpad/final-verify`, removed afterwards.
  `git worktree list` now shows only the main checkout.
- **`dotnet build Ohmline.slnx -c Release`:** succeeded with 0 warnings and 0 errors (18 s).
- **`dotnet test --solution Ohmline.slnx`:**
  - Run 1: total 2676, failed 1, succeeded 2675 (exit code 2). The failing test was
    `Ohmline.DeviceGateway.Tests.GatewayDrainTests.Given_connected_chargers_when_the_gateway_stops_then_each_is_told_to_return`
    (GatewayDrainTests.cs:38). The test expected close status 1012 (ServiceRestart) but got
    `[(NormalClosure, Bye)]`.
  - That test project then passed 5 of 5 runs on its own (17/17).
  - Run 2 of the full suite: **total 2676, failed 0, succeeded 2676** (exit code 0, load average
    about 4 to 7).
  - The total of 2676 matches the book's figure for `ch21-s10`.
- **This flake is new.** It is not in `errata.md` yet. Under full-suite load, one of the three
  chargers sometimes gets a normal close instead of the drain's reconnect hint. The likely cause
  is that the 1 s `DrainWindow` set in the test runs out, or the host's shutdown overtakes the
  drain. A test-only fix could widen the window or scale it with `ActorTestConfig.TimeFactor`. It
  should be added to the errata and to the flake list in `queued-jobs.md` job 1.
- **MAUI head:** `dotnet build mobile/Ohmline.Mobile/Ohmline.Mobile.csproj -f net11.0-android -c Release`
  succeeded with 0 warnings and 0 errors (53 s).

## 2. Tags compared with `outline.json`

- `outline.json` has 21 chapters and 185 sections. 174 of those sections have a `codeTag`.
- `git tag -l 'ch*'` lists 174 tags.
- **Missing: none. Extra: none.** The two sets are identical.
- Non-`ch*` tags: `v1.0.0`. The outline does not list it, but Chapter 21 and the ch21
  verification reference it.

## 3. Book build

- `tools/build-book.sh` produced `build/book.md`: 23 files, 185,428 words.
- No `pandoc` is on PATH, so the script was rerun with
  `PANDOC=.../scratchpad/pandoc`. That run also produced `build/book.html` and `build/book.epub`.
- `wc -w book/*.md`: **185,380 words in total.**
  - Chapters: 168,380 words. The outline's target was 154,000, so the chapters are about 109% of
    target.
  - Front matter: 2,737 words. Appendices: 14,263 words.
  - Longest chapters: ch21 (9,898 words) and ch16 (9,896). Shortest: ch01 (6,046).
- **Estimated length: about 618 pages** at 300 words per page, for prose with code.

## 4. Editorial status

### progress.md (21 rows, one per chapter; all chapters drafted and verified)

- The chapter word counts in the file add up to 168,400, which matches the 168,380 measured now.
- **164 of 174 tags were verified green.** The 10 that were not:
  - `ch01-s02`..`s07` (5 tags): these have no solution file or an empty one. They are exempt by
    design but still need an editor decision.
  - `ch14-s01` and `ch15-s07`: the read-model schema race.
  - `ch17-s07`: the `LinkTests` timer race.
  - `ch18-s03`: the read-model schema race again. It depends on the worktree path.
  - `ch20-s08`: 5 of 5 runs failed on known load flakes. The cause was fixed forward at `ch21-s08`.
- **441 file-headed listings were checked against their tags, and 1 book fix was needed.** It
  was a Mermaid parse error in the section 21.8 sequence diagram (`;` changed to `,`).
- Every chapter's diff --stat lines, checkout/diff refs and test totals match the repo.
- From ch19 on, the mobile head builds with `-warnaserror` at every tag.

### errata.md (42 entries, 759 lines)

There are no open errors in the book text. All open items concern the tests or the tooling.

**Fixed forward:**

| Tag | Fix |
| --- | --- |
| `ch11-s04` | Second-incarnation sequence race |
| `ch12-s06` | Command ids |
| `ch13-s02`, `ch13-s03` | `AskAsync` timeout and snapshot test |
| `ch16-s02` | Projection schema deadlock |
| `ch16-s05` | Feed `Type` serialized twice |
| `ch16-s09` | Feed returned 500 before the outbox existed; it now returns 503 with `Retry-After` |
| `ch17-s07` | Timer envelope not unwrapped |
| `ch18-s04` | SQLite `AskTimeout` raised from 5 s to 20 s × TimeFactor |
| `ch20-s08` | The migrator removes the read-model schema race |
| `ch21-s08` | `EdgeTestKit` ask raised from 500 ms to 3 s |

Several coder entries also record listing and glossary changes that were fixed forward for
ch04 to ch12. Earlier tags keep their old behaviour.

**Still open:**

1. **Ch02.Hosted shutdown-log race** (`ChargerAppTests ... coordinated_shutdown_drains_first`).
   - It is the most frequent flake, seen at almost every chapter. At ch04-s01 it failed about
     half the runs.
   - Fixing it changes code that section 2.9 quotes, so it needs an editor decision.
2. **Read-model schema race** at `ch14-s01`, `ch15-s07`, `ch18-s03` and `ch20-s02`.
   - The test order depends on the worktree path, so the failure appears in some paths and not
     others.
   - It is latent until `ch20-s08`. The "Try it" sections at those tags promise `failed: 0`.
3. **`LinkTests` warm-up timer race**, from `ch17-s07`. A test-only fix has been suggested.
4. **Load-only flakes** with no fix recorded:
   - `GridSignalPollerTests`: 500 ms ask with a 100 ms poll.
   - `ChargeToReceiptTests`, the Aspire system test.
   - `TapToJournalTraceTests` and `NodeMetricTests`: 504 from the singleton proxy.
   - `ReadJournalTourTests`.
   - A Podman rootlessport port clash.
   - Running the System trait as its own CI step has been suggested.
5. **MAUI CS0200 build error** (`ch19-s05`/`s07`, `AppShell.xaml.uc.xsg.cs`).
   - It needs the shared compiler server to have built the same project path with a different
     `AppShell.xaml`.
   - Workarounds: `EnableMauiIncrementalHotReload=false`, `UseSharedCompilation=false`, or
     `dotnet build-server shutdown` before the build. None of these is applied yet.
   - It does not reproduce in per-tag worktrees.
6. **Chapter 1 tags:** the editor must decide whether they are exempt from the definition of done.
7. **New today:** the `GatewayDrainTests` drain-window flake described in section 1 above.

`queued-jobs.md` job 1 was approved by the author. It plans to fix items 1 to 4 at the commits
that introduced them and to re-point the tags. Item 7 and the MAUI item should be added to that
job.

## Verdict

- The final tag builds clean.
- The full suite is green (2676/2676) on a rerun, after one new load flake.
- The MAUI Android head builds.
- The tags match the outline exactly.
- The book is about 185k words, roughly 618 pages.
- What remains is test flakiness, already queued for job 1, plus two editor decisions: the
  Ch02.Hosted fix and the Chapter 1 DoD exemption.

# Cleanup (job 1)

Done 2026-11-05/06, as approved by the author (`queued-jobs.md` job 1). Details are in
`cleanup/rewrite-report.md`, `cleanup/reverify-ch01-07.md`, `cleanup/reverify-ch08-14.md`,
`cleanup/reverify-ch15-21.md`, `cleanup/repair-report.md` and the `cleanup/diagnosis-*.md` files;
patches are in `cleanup/patches/`. Nothing was pushed.

## Fixes

22 fixes, each amended into the commit that introduced the defect, then every later commit,
all 175 annotated tags and all 13 branches rebuilt on top (commit messages, authors and dates
byte-identical). All are test-only except `ch01-dod-exemption` (README and a comment in
`tools/verify-tags.sh`), `maui-cs0200` (`mobile/Directory.Build.props`) and `simulator-retries`
(`tools/Ohmline.Simulator`). No `src/` file changed and no test was added or removed.

| pass | fix ids (amended into) |
| --- | --- |
| rewrite (14 + 1 tag message) | `ch01-dod-exemption` (ch01-s07), `logger-race` (ch02-s09), `sqlite-node-ask` (ch10-s07), `two-incarnations-ask` (ch10-s09), `state-sourcing-ask` (ch11-s07), `edge-testkit-ask` (ch12-s05), `journal-node-ask` (ch13-s03), `fixture-ensures-schema` (ch13-s08), `simulator-retries` (ch16-s07), `grid-signal-poller` (ch16-s08), `link-tests` (ch17-s07), `traced-node-ask` (ch17-s08), `maui-cs0200` (ch19-s04), `gateway-drain` (ch21-s07); tag `ch18-s01` message now `18.1 Domain metrics with a cardinality budget` |
| repair after re-verification (8) | `listing-snapshot-settled` (ch11-s07), `persistence-shell-seed` (ch11-s08), `site-energy-api-ask` (ch12-s05), `driver-accounts-api-ask` (ch12-s07), `driver-app-charge-waits` (ch14-s06), `replay-charge-and-stop-waits` (ch14-s07), `journal-trace-unnamed` (ch17-s07), `trace-shape-ancestors` (ch17-s08) |

Where a later commit had already fixed the same thing forward (ch13-s03, ch15-s01, ch15-s06,
ch18-s04, ch20-s08, ch21-s08), that commit now carries only its remaining changes; its message
was kept. Key refs: `main` = `ch21-s09` 82baae1 -> **3566d59**; `upgrade/akka-1.6` = `ch21-s10`
f9ed698 -> **7946730**; `v1.0.0` 31a0d8b -> **b7859b9**.

## Backup

- `editorial/cleanup/sample-before-cleanup.bundle` (`--all`, 1.8 MB, verified).
- In `sample/`: `refs/backup/pre-cleanup/{heads,tags}/*` (13 + 175, the original history) and
  `refs/backup/pre-repair/{heads,tags}/*` (13 + 175, the state between rewrite and repair).
- Tag maps: `cleanup/tag-map.tsv` (original -> rewrite) and `cleanup/repair-tag-map.tsv`
  (rewrite -> final). Restore commands are in the two reports. Remove the backups once the author
  is satisfied.

## Re-verification

Fresh detached worktree per ref, `dotnet build Ohmline.slnx -warnaserror`, then
`dotnet test --solution Ohmline.slnx`; from `ch19-s04` on also the MAUI head
(`mobile/Ohmline.Mobile.slnx -warnaserror`, net11.0-android).

- **Builds:** every ref, 0 warnings and 0 errors. MAUI head 0/0 at all 28 refs from `ch19-s04`,
  including a same-path build sequence (s04 -> s05 -> s07 -> s04 -> s08 -> s05 -> ch20-s01 ->
  ch21-s09) that used to hit CS0200.
- **Chapter 1:** the 5 tags are skipped by design (`0 passed, 5 skipped, 0 failed`), now documented
  as exempt.
- **ch02 to ch07:** 50 of 50 tags green, 87 full runs, 0 failures (load up to 60).
- **ch08 to ch21, v1.0.0:** every total matches `progress.md`. The first re-verification had 12
  failed runs in 245 full runs (load up to 55), each one or two tests hitting a race or cold-start
  timeout the rewrite had not covered; all were diagnosed and closed by the 8 repair fixes above.
  The side branches failed exactly as the book says, except `wrong/ch12-s04-context-stop`, which
  fails 4 tests, not 3 (a book count, wrong before the rewrite too).
- **After the repair:** 18 refs (every repaired tag, `main`, `ch21-s10` and the formerly failing
  tags) passed **3 consecutive full runs** each; `v1.0.0` (2,664), `alt/ch20-s04-adapter-only`
  (2,433) and the `wrong/*` branches gave exactly what the book says. 70 full runs at `ch*` tags,
  one unexpected failure (`JournalTraceTests` at ch21-s10), fixed as `journal-trace-unnamed` and
  re-run green.
- **No test total changed at any tag or branch.** Every `chNN-sMM` tag from `ch02-s01` on is now
  green, including the four that had no green run before (`ch15-s07`, `ch17-s07`, `ch18-s03`,
  `ch20-s08`).

## Book resync (2026-11-06)

Checked against the rewritten repo:

- **Listings:** all 426 code blocks that carry a `File: <path> @ <tag>` header (385 with the
  header inside the block, 41 with an italic header above it; the earlier figure of 441 was a
  per-chapter tally, and this scan of `book/*.md` finds 426 headed blocks), compared whitespace-normalized with
  `git show <tag>:<path>`, `// ...` elisions allowed. 1 mismatch, fixed: `TracedNode.cs @ ch17-s08`
  (17.8) gains the two-line comment and the three `UseSetting("Ohmline:...:AskTimeout", "00:00:20")`
  lines. The **section 2.9 listings did not change**: `logger-race` touches only
  `ChargerAppTests.cs`, which the book does not quote. The 12 `git diff` listings give the same
  output as before.
- **`--stat` totals:** all 176 `--stat` commands re-run; 22 totals updated (ch01 1.7; ch02 2.9; ch10
  10.7, 10.9; ch11 11.7, 11.8; ch12 12.5; ch13 13.3, 13.8; ch14 14.6, 14.7; ch15 15.1, 15.6; ch16
  16.7, 16.8; ch17 17.7, 17.8; ch18 18.4; ch19 19.4 (filtered); ch20 20.8; ch21 21.7, 21.8).
- **Test totals and outputs:** every quoted total matches the re-verified totals; no change.
  No quoted test output references a changed file.
- **Git commands:** all 530 `git checkout/diff/show/log` commands quoted in the book resolve on
  the new refs (path-limited diffs non-empty). The 21.9 `git log ch21-s08..ch21-s09` listing and
  the prose now use the new hashes (3566d59, e074715, b7859b9, 67228e8; MinVer `1.0.0+b7859b9...`).
  The repo's `CHANGELOG.md` still cites the old feature hash `da32c99`, so 21.9 no longer quotes
  that hash (open item in `errata.md`).
- **Prose:** 17.8 Note no longer mentions the `LinkTests` race, and Exercise 2 became a Reflect
  exercise on the fix; 17.8 "What to assert" explains why `SpansAsync` waits for the ancestors;
  the 18.4 Note says `ch18-s04` now only rewords the SQLite helpers' comments (the 20 s timeouts
  date from Chapters 10 and 11); the 19.5 Version note describes the
  `EnableMauiIncrementalHotReload=false` setting instead of `dotnet build-server shutdown`, and
  19.4's prose mentions it; 12.4's side-branch result says four tests fail, not three.
- **Front matter and appendices:** neither states a test count or describes flakiness; no change.
- `tools/build-book.sh` rebuilt `build/book.md`, `.html` and `.epub` (185,565 words assembled;
  `book/*.md` 185,517).

`errata.md` now opens with a status table and the list of what is still open; every entry has a
Status line.

# References (job 2)

Done 2026-11-06, as requested by the author (`queued-jobs.md` job 2). Fresh agents built one
ledger per book file (`references/*.json`, 23 files), an independent adversarial pass checked
every link and locator, and every finding was resolved (`references/fix-report.md`). The index
`book/98-references.md` is generated from the ledgers by `references/compiler/compile.py`
(details in `references/compile-notes.md`), so the index and the ledgers cannot drift apart.

## Counts (from the ledgers)

- **640 ledger entries** across front matter, ch01 to ch21 and the appendices: 324 claims,
  75 quotations, 241 further-reading lines. Every entry records a URL, a locator, how it was
  verified and the evidence text found at the source.
- **Outcome per entry** (the `action` field):
  - linked as written, source confirmed: **524** (+2 `no-change` quotes and 1 `kept-unlinked`
    quote of the book's own sample code / compiler output: **527**)
  - wording corrected in the book to match the source: **93**
  - turned into a clearly marked paraphrase: **11**
  - removed: **2** (`ch05-...-10`, a Khononov attribution replaced by the verified Robert C.
    Martin statement; `ch20-...-13`, an unverified Vernon locator)
  - added during the fix pass for citations the first pass had missed: **7**
- **Quotations (75):** 47 confirmed verbatim as first written (including 5 added and 3 that quote
  the sample code or compiler output), 27 corrected to the source's verified wording, 1 turned
  into a paraphrase (Cunningham, Appendix E.1, now replaced by a verified excerpt). After the fix
  pass every quotation was re-checked verbatim against the source.
- **`book/98-references.md`:** Part 1 index of 639 entries (the one removed attribution is left
  out), Part 2 bibliography of 132 sources (26 books, 28 papers/articles, 5 talks, 50
  documentation/specification entries, 23 blog posts). Books link to the publisher or bookstore
  page with a chapter/section locator; page numbers are given only where verified for a named
  edition (e.g. the 2015-03 DDD Reference, IDDD and Beck via publisher index PDFs).
- Inline links were added in the chapters and appendices; all 103 Appendix E links are in Part 2.

## Adversarial check

- 86 finding rows in `fix-report.md` (some rows cover several entries); all resolved. 25 ledger
  entries record a 2026-11-06 change in their `action`, and 352 stale ledger `text` fields were
  regenerated from the current chapter text; a mechanical check confirms every Part 1 excerpt
  occurs in the chapter it indexes.
- Notable fixes: an unverifiable Wlaschin quotation (ch. 4) removed and replaced by a verified
  preface quotation; unverified page numbers removed (Khononov pp. 164-165 in ch. 9/11, p. 57 in
  ch. 21); an unsupported "new phase" gloss on pivotal events removed (ch. 4, ch. 18); Brandolini's
  Big Picture phases, the Phobos release-notes claims (ch. 17), the `AddTracing` description, the
  Young CQRS attribution (ch. 13) and the Vogels link (now All Things Distributed + DOI) corrected;
  7 entries added. Verified page numbers were kept with their evidence (Beck pp. 136-137, IDDD
  pp. 52 and 92).
- All 94 URLs touched by the fixes were re-fetched: 87 returned 200; the 7 that return 403 to
  curl (O'Reilly, ACM DL, ACM DOI, MIT Press DOI, Khononov landing page) were opened in a browser.

## Not resolved / caveats

- ch08-07: a strict text check flags it only because the book escapes `\[Event\]` inside a link;
  the wording matches slide 62.
- Two ch11 index excerpts contain bracketed editorial insertions on purpose.
- ch12-18: the paper's curly quotes around "Entities" were not reproduced (cosmetic).
- The ch08 supple-design table uses the DDD Reference's heading "Drawing on Established
  Formalisms" (correct for the Reference it cites), which differs from the book's heading.

# Summary for the author (jobs 1 and 2)

- **Job 1, test cleanup:** 22 fixes amended at the commits that introduced each defect (14 in the
  rewrite, 8 in the repair pass), all 175 tags and 13 branches re-pointed with identical names,
  messages, authors and dates. Every `chNN-sMM` tag from `ch02-s01` on is green (Chapter 1 tags
  are exempt by design and documented); no test total changed. Backups: `refs/backup/pre-cleanup/*`
  and `refs/backup/pre-repair/*` in `sample/`, plus `editorial/cleanup/sample-before-cleanup.bundle`.
  Remove them once you are satisfied. Nothing was pushed.
- **Book resync:** 1 listing fixed (`TracedNode.cs @ ch17-s08`), 22 `--stat` totals updated, the
  21.9 hashes updated, prose updated in 12.4, 17.8, 18.4, 19.4 and 19.5.
- **Job 2, references:** 640 citations checked; 93 corrected, 11 paraphrased, 2 removed, 7 added;
  `book/98-references.md` built; adversarial findings all resolved (four cosmetic caveats above).
- **Still open (your decisions):** see `errata.md` "Still open": the Podman port clash, the
  book-quoted `ch11-s08` test and `ThenAsync` timeouts, the `RegionTests` latency at
  `ch12-s09..s11`, the stale `da32c99` hash in the repo's `CHANGELOG.md`, and leftover commit
  wording.
- **Length:** `wc -w book/*.md` = **218,731 words** (chapters 173,005; front matter 2,757;
  appendices 14,552; references 28,417).
