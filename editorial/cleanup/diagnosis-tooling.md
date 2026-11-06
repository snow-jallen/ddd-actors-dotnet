# Diagnosis: tooling items (MAUI CS0200, ch18-s01 tag message, Chapter 1 DoD exemption)

The sample repo was not changed. All work was done in detached worktrees and in a throwaway clone
under the session scratchpad, and they were removed afterwards. The patches are in
`editorial/cleanup/patches/`.

## (a) MAUI head CS0200 in `AppShell.xaml.uc.xsg.cs`: `tooling-maui-cs0200.patch`

- **Reproduced** in one worktree path. The steps: build the head at `ch19-s04`, check out
  `ch19-s05`, `git clean -ffdxq`, build again. The build fails with two errors, both
  `AppShell.xaml.uc.xsg.cs(45,3)/(47,3): error CS0200: Property or indexer 'ShellItem.Items' / 'Shell.Items' cannot be assigned to -- it is read only`.
- **Root cause.** This is an upstream bug in MAUI 11 rc.2 (11.0.0-rc.2.26505.5), not in the
  sample's code.
  - `Microsoft.Maui.Controls.targets` turns XAML Incremental Hot Reload on for Debug:
    `EnableMauiIncrementalHotReload` defaults to `true` when it is empty and the configuration is
    Debug.
  - With it on, the XamlGenerator emits `UpdateComponent` code in `*.xaml.uc.xsg.cs`. It builds that
    code from the previous XAML, which the long-lived shared compiler server (VBCSCompiler) keeps
    for the same project path.
  - After a checkout changes `AppShell.xaml`, the generator emits an "update" that assigns
    `Shell.Items` and `ShellItem.Items`. Both are read-only collections, so the build fails.
  - Deleting `obj/` does not help, because the stale state lives in the compiler server.
  - Fresh worktree paths never hit it, which is why the per-tag verification never saw it. A reader
    who checks out tags in order in one clone does hit it, and so does `tools/verify-tags.sh`,
    which reuses one worktree.
- **Fix:** set `<EnableMauiIncrementalHotReload>false</EnableMauiIncrementalHotReload>` in
  `mobile/Directory.Build.props`, with a comment that explains why.
  - It goes in at `ch19-s04`, where the head first appears.
  - The SDK default is guarded by `'$(EnableMauiIncrementalHotReload)' == ''`, so the override
    takes effect.
  - It switches off only the buggy generator path. Classic XAML hot reload and the compiler server
    stay, and Release builds already default to `false`.
  - This file holds only the head, and the book does not quote it under a `File:` header. The head's
    `.csproj` is quoted at `ch19-s04`, so it was avoided.
  - `UseSharedCompilation=false` would also work, but it slows every head build and hides the bug
    instead of switching off its cause.
- **Verified:**
  - The patch passes `git apply --check` at `ch19-s04`.
  - The stress loop used the same worktree path with `git clean -ffdxq` before each build. It ran 13
    head builds over the transitions s04 → s05 → s07 → s04 → s08 → s05 → s06 → s07 → s04 → s05 →
    ch20-s01 → s05 → ch21-s09.
  - **Result: 13 of 13 builds had 0 errors and 0 warnings.**
  - **Control without the fix:** in the same path, `s05` → `s07` failed again with CS0200
    (`ShellItem.Items`).
  - At `ch19-s04` with the patch applied, `dotnet build mobile/Ohmline.Mobile.slnx -warnaserror`
    and `dotnet build Ohmline.slnx -warnaserror` both gave 0 warnings and 0 errors. Afterwards,
    `git status` showed only the patched file.
- **Carry forward.** `mobile/Directory.Build.props` never changes after `ch19-s04`.
  - The patch applies unchanged at every later tag, at `ch21-s10` (`upgrade/akka-1.6`), at
    `alt/ch20-s04-adapter-only` and at `wrong/ch21-s02-minver-appversion`.
  - A scripted rebase of `main` in a scratch clone replayed with no conflicts.
  - No later commit fixes this forward.
- **Book impact (Chapter 19):**
  - Line 347, the 19.4 Try-it: `30 files changed, 761 insertions(+)` becomes
    **`30 files changed, 768 insertions(+)`** (computed on the replayed history).
  - Line 463, the 19.5 Version note: this note tells readers to run `dotnet build-server shutdown`,
    and that workaround is no longer needed. Rewrite it to say the head turns off XAML incremental
    hot reload in `mobile/Directory.Build.props` because of this rc.2 bug. Optionally, add half a
    sentence to the line 295 prose, which describes what `mobile/Directory.Build.props` does.
  - The errata entry "ch19-s05, ch19-s07 (found by the coder, not fixed)" can be closed.
- Tests: no change in test count.

## (b) `ch18-s01` tag annotation

- **Current:** `ch18-s01: Domain metrics with a cardinality budget`.
- **Correct:** `18.1 Domain metrics with a cardinality budget`, matching all 173 other `ch*` tags
  and the outline title.
- No tree change is needed, and there is no patch.
- **Replay:** create the tag as an annotated tag with tagger `Ohmline Book <book@ohmline.example>`,
  pointing at the rewritten ch18-s01 commit:
  `git tag -a -f ch18-s01 -m "18.1 Domain metrics with a cardinality budget" <commit>`.
- **Book impact:** none needed. Appendix line 689 (the tag index, which says it is generated from
  `git tag -n`) already shows `18.1 Domain metrics with a cardinality budget`, so the fix makes the
  repo match the book.
- `editorial/sample-architecture.md` §10 still documents the `chNN-sMM: <title>` form for tag
  messages. That is the commit-subject form, but the tags use `NN.M <title>`, so the editor may
  want to correct §10.

## (c) Chapter 1 tags exempt from the definition of done: `tooling-ch01-dod-exemption.patch`

- **Finding.** `tools/verify-tags.sh` already skips these tags, and it has done so since it was
  added at `ch01-s07`.
  - `has_projects` skips a tag when `Ohmline.slnx` is missing (`ch01-s02` to `s06`) or lists no
    `<Project` (`ch01-s07`).
  - It prints `skipped (Ohmline.slnx has no projects yet)` and ends with
    `0 passed, 5 skipped, 0 failed`, exit 0.
  - Both lines are quoted verbatim in the 1.7 Try-it (ch01 line 492), so the printed output was
    left unchanged.
- **Patch at `ch01-s07`:**
  - `tools/verify-tags.sh`: the header comment now says that skipped tags are the Chapter 1
    documents and setup tags, that they are exempt from the build-and-test check, and that every
    tag from `ch02-s01` on must pass. Only the comment changes. The book's quote of the script
    elides the header with `# ...`.
  - `README.md` "Building and testing": a new paragraph records the exemption, and says that from
    `ch02-s01` on every tag must build with `-warnaserror`, pass all of its tests and leave no files
    behind. This is the README section that describes the definition of done. No README version
    uses the literal words "definition of done".
- **Verified:**
  - The patch passes `git apply --check` at `ch01-s07`.
  - `bash -n` passes.
  - Run from the patched tree, `tools/verify-tags.sh 'ch01-*'` gives the same 5 skips, `0 passed,
    5 skipped, 0 failed` and exit 0.
  - `tools/verify-tags.sh 'ch02-s01'` gives `1 passed`.
- **Carry forward.** `ch19-s04` adds a header line right above the changed comment line, so a plain
  `git apply` fails from `ch19-s04` on.
  - The 3-way merge (rebase) is clean. `git merge-file` at `ch19-s04` and at `main` gives no
    conflict, and the scratch-clone rebase of all of `main` gave no conflict.
  - The README paragraph sits in text that no later commit changes.
  - A replay should let the rebase merge carry the change. It should not re-apply the patch text at
    later tags.
- **Book impact (Chapter 1):**
  - Line 492: `9 files changed, 261 insertions(+)` becomes **`9 files changed, 267 insertions(+)`**.
  - The 1.7 prose (line 485) already explains the skip and that every tag from section 2.1 must
    pass, so it needs no change.
  - `git diff ch01-s07 ch02-s01` (ch02 line 167) is unchanged.
- Tests: no change in test count.
