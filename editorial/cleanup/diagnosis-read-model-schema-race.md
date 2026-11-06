# Diagnosis: read-model schema race

## Symptom

`QueryApiTests.Given_a_site_with_no_connectors_on_the_board_then_404` (from ch13-s08) and
`ReadYourWritesTests.Given_a_session_the_history_has_never_seen_then_404_without_min_version`
(from ch14-s01) get 500 instead of 404. The node logs `42P01: relation "read_models.site_live_board"`
(or `session_history`) `does not exist`. This was seen at ch14-s01, ch15-s07, ch18-s03 and ch20-s02.

## Root cause

Each `ProjectionDaemon` runs `ReadModelSchema.EnsureAsync` in `PreStart` (section 13.6). This
happens asynchronously, a moment after the node starts. The two 404 tests start an `OhmlineNode`
and query at once. When one of them is the first test in the assembly to start a node on the
fresh container, there is no table yet, so the query throws and the endpoint returns 500. The
other integration tests wait for rows, and `SessionHistoryTests` and `ProjectionStatusTests`
already tolerate 42P01, so they never hit this. Whether a 404 test runs first depends on xUnit's
test order. That order follows test ids, and test ids depend on the worktree path, which is why
some paths fail every time and others never fail.

Deterministic repro: run either 404 test on its own, for example
`dotnet test --project tests/Ohmline.IntegrationTests -- --filter-method "*Given_a_site_with_no_connectors_on_the_board_then_404"`.
It failed 1/1 at ch13-s08, ch14-s01, ch15-s07, ch18-s03, ch20-s02 and ch20-s07. The
ReadYourWrites test failed alone at ch14-s01, ch18-s03, ch20-s02 and ch20-s07 (it passed once at
ch15-s07 on timing).

## Introducing tag: ch13-s08

ch13-s08 adds `QueryApiTests` with the immediate-read 404 test. At ch13-s07, the only read-model
endpoint (`/projections/status`) catches 42P01, and the tests that read tables poll through it.

## Fix (test fixture only)

`tests/Ohmline.IntegrationTests/PostgresFixture.cs` gets an `InitializeAsync` override that, after
the container starts, runs `ReadModelSchema.EnsureAsync` once, before any node starts. This is the
same call `Ohmline.Charging.ReadModels.Tests` already makes per test, and it is the shape ch20-s08
gives this fixture with the migrator. Production code is unchanged. The daemons still run their
own idempotent `EnsureAsync`.

Patch: `editorial/cleanup/patches/read-model-schema-race-fixture-ensures-schema.patch`, applied
at ch13-s08 with 15 insertions.

## Verification

- `git apply --check` succeeds at ch13-s08, ch14-s01, ch15-s07, ch18-s03, ch20-s02 and ch20-s07.
  It fails, as expected, at ch20-s08.
- `-warnaserror` builds have 0 warnings and 0 errors (the IntegrationTests project at all six
  tags, and the full solution at ch13-s08 and ch14-s01).
- Results with the patch, running the 404 test alone:
  - ch13-s08: 10/10 green.
  - ch14-s01: 5/5 green for each 404 test.
  - ch15-s07, ch18-s03 and ch20-s02: 3/3 green for each.
  - ch20-s07: 2/2 green for each.
- The whole IntegrationTests project was green 3/3 at ch13-s08 (11 tests), 2/2 at ch14-s01 (14)
  and 2/2 at ch20-s07 (39).
- Full solution, with the patch, at machine load 49 to 56:
  - ch14-s01: total 1607. The one failure is the Ch02.Hosted shutdown race.
  - ch13-s08: total 1594. The two failures are the Charging.Api 500 ms ask load flakes.
  - All integration tests passed in both runs, and the totals are unchanged.

## Carrying it forward

- No commit between ch13-s08 and ch20-s07 touches `PostgresFixture.cs`. `ReadModelSchema.EnsureAsync`
  keeps the same signature until it is deleted at ch20-s08, so the replay applies cleanly through ch20-s07.
- ch20-s08 conflicts on `PostgresFixture.cs`. Take ch20-s08's version verbatim: its `InitializeAsync`
  runs `Migrations.Apply`, and `ReadModelSchema` no longer exists at that tag. This is the
  later, fuller form of the same fix.
- The side branches `wrong/ch16-s01-dual-write` and `alt/ch20-s04-adapter-only` do not touch the
  file and rebase cleanly.

## Book impact

The book quotes `PostgresFixture.cs` only at ch12-s11 (ch12 around line 1051), and that is unaffected. Two
diff --stat sentences change:

- ch13 line 796 (`git diff ch13-s07 ch13-s08 --stat`): `25 files changed, 866 insertions(+), 12 deletions(-)`
  becomes `26 files changed, 881 insertions(+), 12 deletions(-)`.
- ch20 line 768 (`git diff ch20-s07 ch20-s08 --stat`): `48 files changed, 908 insertions(+), 300 deletions(-)`
  becomes `48 files changed, 903 insertions(+), 310 deletions(-)`.

Test counts do not change. Errata entries for ch14-s01, ch15-s07 and ch18-s03, the ch20-s02
note, and final-report item 2 can be closed once the replay lands.
