# Re-verification after the history rewrite: ch01 to ch07 (2026-11-05)

Scope: every `ch01-*` to `ch07-*` tag on the rewritten history (56 tags). No refs were changed and
nothing was committed. Each tag was checked out as a detached worktree under
`.../scratchpad/reverify-ch01-07/<tag>` and removed afterwards. Toolchain: `tools/env.sh`
(SDK 11.0.100-rc.3), rootless Podman via `DOCKER_HOST`. One `dotnet test` ran at a time.
Two other re-verification agents ran at the same time, so the load average was high: 2 at the start,
then 13 to 61 (most of ch05 to ch07 ran at a load of 40 to 60 on 16 cores).

## Result: 50 of 50 testable tags green, 5 Chapter 1 tags skip cleanly

- **Every tag `ch02-s01` to `ch07-s08`:** `dotnet build Ohmline.slnx -warnaserror` gave 0 warnings
  and 0 errors. `dotnet test --solution Ohmline.slnx` gave exit code 0 and `failed: 0` on every run.
  No tag left untracked or modified files behind.
- **87 test runs, 0 failures.** That is 1 run at each of 32 tags plus 3 consecutive runs at each of
  18 tags: `ch02-s09` (the fix tag) and the last 3 tags of each chapter (`ch02-s07..s09`,
  `ch03-s07..s09`, `ch04-s06..s08`, `ch05-s07..s09`, `ch06-s06..s08`, `ch07-s06..s08`). Every one of
  them passed on runs 1, 2 and 3 with no retries.
- **Ch02.Hosted shutdown-log race (fixed at `ch02-s09`):** `Ch02.Hosted.Tests` passed 4/4 in all 75
  runs from `ch02-s09` on, many of them at a load of 40 to 60. Before the rewrite it failed on first
  runs at ch03-s04, ch04-s01, ch05-s01, ch06-s01, ch06-s04, ch07-s01, ch07-s02 and ch07-s08 at much
  lower load. `learn/Ch02.Hosted.Tests/ChargerAppTests.cs` has the same blob (e3aa609) at every
  chapter-end tag from `ch02-s09` to `ch07-s08`.
- **Tags vs. the tag map:** each `ch02-*` to `ch07-*` tag resolves to the `new_commit` in
  `tag-map.tsv`. `git diff refs/backup/pre-cleanup/tags/<t> <t>` at each chapter-end tag shows only
  the 3 fix files (README.md, ChargerAppTests.cs, tools/verify-tags.sh, 23+/1-). `main` is still
  9a203cb.

## Chapter 1 (exempt docs-only tags)

- `ch01-s02`, `-s03`, `-s04` and `-s06` have no `Ohmline.slnx`. At `ch01-s07` it lists no projects.
- `main`'s `tools/verify-tags.sh 'ch01-*'`: all 5 tags `skipped (Ohmline.slnx has no projects yet)`,
  then `0 passed, 5 skipped, 0 failed`, exit 0. No worktree was left behind.
- `ch01-s07`'s own `tools/verify-tags.sh`: `bash -n` OK, and the same output and exit 0.

## Totals vs. `editorial/progress.md`

| chapter | tags | totals measured (s01, s02, ...) | progress.md | difference |
| --- | --- | --- | --- | --- |
| ch02 | 9 | 3/7/12/17/22/29/33/42/46 | 3/7/12/17/22/29/33/42/46 | none |
| ch03 | 9 | 54/61/71/75/84/96/101/121/137 | 54/61/71/75/84/96/101/121/137 | none |
| ch04 | 8 | 137/137/137/137/137/137/137/137 | 137/137/137/137/137/137/137/137 | none |
| ch05 | 9 | 137/137/165/165/166/166/184/184/184 | 137/137/165/165/166/166/184/184/184 | none |
| ch06 | 8 | 201/216/243/292/302/306/381/393 | 201/216/243/292/302/306/381/393 | none |
| ch07 | 8 | 393/407/413/470/486/493/524/566 | 393/407/413/470/486/493/524/566 | none |

No test total differs from progress.md or from the book. This matches the rewrite report ("no test
total changed at any tag").

## Per-tag log (commit, build, runs as exit code / total / failed / load at start)

```
ch02-s01  f33a21b build 0W/0E  r1:ec=0,total=3,failed=0,load=2.24;
ch02-s02  84d5baf build 0W/0E  r1:ec=0,total=7,failed=0,load=2.12;
ch02-s03  ffe5556 build 0W/0E  r1:ec=0,total=12,failed=0,load=2.12;
ch02-s04  52c2dc8 build 0W/0E  r1:ec=0,total=17,failed=0,load=2.35;
ch02-s05  bb02825 build 0W/0E  r1:ec=0,total=22,failed=0,load=2.35;
ch02-s06  2ac47c5 build 0W/0E  r1:ec=0,total=29,failed=0,load=4.24;
ch02-s07  4f101a7 build 0W/0E  r1:ec=0,total=33,failed=0,load=6.31; r2:ec=0,total=33,failed=0,load=6.20; r3:ec=0,total=33,failed=0,load=6.88;
ch02-s08  0a72cb9 build 0W/0E  r1:ec=0,total=42,failed=0,load=8.65; r2:ec=0,total=42,failed=0,load=9.80; r3:ec=0,total=42,failed=0,load=20.34;
ch02-s09  8c98d14 build 0W/0E  r1:ec=0,total=46,failed=0,load=19.30; r2:ec=0,total=46,failed=0,load=17.99; r3:ec=0,total=46,failed=0,load=19.11;
ch03-s01  b5887fb build 0W/0E  r1:ec=0,total=54,failed=0,load=19.10;
ch03-s02  517269b build 0W/0E  r1:ec=0,total=61,failed=0,load=20.29;
ch03-s03  f6918dc build 0W/0E  r1:ec=0,total=71,failed=0,load=18.57;
ch03-s04  183f7e6 build 0W/0E  r1:ec=0,total=75,failed=0,load=17.33;
ch03-s05  9091478 build 0W/0E  r1:ec=0,total=84,failed=0,load=16.10;
ch03-s06  c5e7eb7 build 0W/0E  r1:ec=0,total=96,failed=0,load=16.33;
ch03-s07  a2b6c7a build 0W/0E  r1:ec=0,total=101,failed=0,load=15.18; r2:ec=0,total=101,failed=0,load=14.20; r3:ec=0,total=101,failed=0,load=13.01;
ch03-s08  8642214 build 0W/0E  r1:ec=0,total=121,failed=0,load=12.77; r2:ec=0,total=121,failed=0,load=13.51; r3:ec=0,total=121,failed=0,load=12.99;
ch03-s09  4638e7c build 0W/0E  r1:ec=0,total=137,failed=0,load=27.01; r2:ec=0,total=137,failed=0,load=29.57; r3:ec=0,total=137,failed=0,load=31.32;
ch04-s01  e856848 build 0W/0E  r1:ec=0,total=137,failed=0,load=32.98;
ch04-s02  5e29b45 build 0W/0E  r1:ec=0,total=137,failed=0,load=32.07;
ch04-s03  8914457 build 0W/0E  r1:ec=0,total=137,failed=0,load=31.34;
ch04-s04  f7a25ca build 0W/0E  r1:ec=0,total=137,failed=0,load=29.63;
ch04-s05  892a22c build 0W/0E  r1:ec=0,total=137,failed=0,load=27.43;
ch04-s06  71cbeb1 build 0W/0E  r1:ec=0,total=137,failed=0,load=26.20; r2:ec=0,total=137,failed=0,load=24.34; r3:ec=0,total=137,failed=0,load=24.55;
ch04-s07  5471f13 build 0W/0E  r1:ec=0,total=137,failed=0,load=24.02; r2:ec=0,total=137,failed=0,load=25.38; r3:ec=0,total=137,failed=0,load=29.65;
ch04-s08  3f37518 build 0W/0E  r1:ec=0,total=137,failed=0,load=30.10; r2:ec=0,total=137,failed=0,load=35.29; r3:ec=0,total=137,failed=0,load=38.58;
ch05-s01  0eac228 build 0W/0E  r1:ec=0,total=137,failed=0,load=40.06;
ch05-s02  5fc67e2 build 0W/0E  r1:ec=0,total=137,failed=0,load=39.74;
ch05-s03  610b99f build 0W/0E  r1:ec=0,total=165,failed=0,load=40.11;
ch05-s04  2ba263a build 0W/0E  r1:ec=0,total=165,failed=0,load=36.08;
ch05-s05  886d6d0 build 0W/0E  r1:ec=0,total=166,failed=0,load=37.83;
ch05-s06  185de35 build 0W/0E  r1:ec=0,total=166,failed=0,load=36.17;
ch05-s07  d8d6aa9 build 0W/0E  r1:ec=0,total=184,failed=0,load=40.48; r2:ec=0,total=184,failed=0,load=53.50; r3:ec=0,total=184,failed=0,load=52.58;
ch05-s08  b6b37d8 build 0W/0E  r1:ec=0,total=184,failed=0,load=45.75; r2:ec=0,total=184,failed=0,load=43.99; r3:ec=0,total=184,failed=0,load=43.09;
ch05-s09  0af0e3f build 0W/0E  r1:ec=0,total=184,failed=0,load=43.02; r2:ec=0,total=184,failed=0,load=38.66; r3:ec=0,total=184,failed=0,load=38.93;
ch06-s01  1620804 build 0W/0E  r1:ec=0,total=201,failed=0,load=38.14;
ch06-s02  c6949bc build 0W/0E  r1:ec=0,total=216,failed=0,load=46.01;
ch06-s03  80315d1 build 0W/0E  r1:ec=0,total=243,failed=0,load=53.09;
ch06-s04  48cca0e build 0W/0E  r1:ec=0,total=292,failed=0,load=53.05;
ch06-s05  bce0578 build 0W/0E  r1:ec=0,total=302,failed=0,load=43.28;
ch06-s06  f5a4930 build 0W/0E  r1:ec=0,total=306,failed=0,load=45.48; r2:ec=0,total=306,failed=0,load=48.41; r3:ec=0,total=306,failed=0,load=58.39;
ch06-s07  1f616d1 build 0W/0E  r1:ec=0,total=381,failed=0,load=56.11; r2:ec=0,total=381,failed=0,load=57.66; r3:ec=0,total=381,failed=0,load=56.00;
ch06-s08  bc105d7 build 0W/0E  r1:ec=0,total=393,failed=0,load=55.64; r2:ec=0,total=393,failed=0,load=60.75; r3:ec=0,total=393,failed=0,load=57.18;
ch07-s01  38c5814 build 0W/0E  r1:ec=0,total=393,failed=0,load=51.94;
ch07-s02  43d789a build 0W/0E  r1:ec=0,total=407,failed=0,load=47.35;
ch07-s03  1e9f9f1 build 0W/0E  r1:ec=0,total=413,failed=0,load=59.52;
ch07-s04  8bfd575 build 0W/0E  r1:ec=0,total=470,failed=0,load=56.56;
ch07-s05  d5564bf build 0W/0E  r1:ec=0,total=486,failed=0,load=50.55;
ch07-s06  3ec8018 build 0W/0E  r1:ec=0,total=493,failed=0,load=56.03; r2:ec=0,total=493,failed=0,load=60.94; r3:ec=0,total=493,failed=0,load=57.75;
ch07-s07  bd745a9 build 0W/0E  r1:ec=0,total=524,failed=0,load=52.16; r2:ec=0,total=524,failed=0,load=48.50; r3:ec=0,total=524,failed=0,load=45.67;
ch07-s08  45a0724 build 0W/0E  r1:ec=0,total=566,failed=0,load=50.39; r2:ec=0,total=566,failed=0,load=53.51; r3:ec=0,total=566,failed=0,load=54.35;
```

## Open items

None in this range. The book's ch02 diff --stat line (ch02:1025, now `10 files changed, 258
insertions(+)`) and ch01:492 (`267 insertions(+)`) are already listed in the rewrite report's
"Book impact" table. `git diff --stat ch02-s08 ch02-s09` confirms `10 files changed, 258 insertions(+)`.
