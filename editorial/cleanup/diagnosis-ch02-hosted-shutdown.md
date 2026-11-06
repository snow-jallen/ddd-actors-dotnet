# Diagnosis: Ch02.Hosted coordinated-shutdown log race

Test: `Ch02.Hosted.Tests.ChargerAppTests.Given_a_running_host_when_it_stops_then_coordinated_shutdown_drains_first`
Patch: `editorial/cleanup/patches/ch02-hosted-shutdown-logger-race.patch` (test-only, 1 file, +16 lines)
Introducing tag: `ch02-s09` (33664ef). The test and app files never change again on `main`, and no side branch changes them (`upgrade/akka-1.6` touches only their `packages.lock.json` files).

## Root cause

The suggestion in the errata was to poll the log or use `AwaitAssert`. That would not fix this, because in a failing run the line never reaches Microsoft.Extensions.Logging at all.

1. The drain task runs in `before-service-unbind` and calls `system.Log.Info(...)`. That puts a `LogEvent` in the mailbox of Akka.Hosting's `LoggerFactoryLogger`, which is an actor on the default dispatcher.
2. `AkkaHostedService.StopAsync` awaits `CoordinatedShutdown.Run(ClrExitReason)` to the end. Every later phase is empty in this host, so the run reaches `actor-system-terminate` within milliseconds. That last phase stops the system guardian, and the guardian stops the logger actors (`LoggingBus.StopDefaultLoggers`).
3. Under load, the logger's mailbox has not been scheduled by then. The stop is a system message, so it is handled before the pending `LogEvent`. `LoggerMailbox.CleanUp` then sends the leftover events to `Settings.StdoutLogger`.
4. The line is printed to stdout as a bare `[INFO][...] Host is stopping: draining charger twins`, without the `info: Akka.Actor.ActorSystem[0]` prefix. It never reaches the `CapturingLoggerProvider`, so the assertion fails.

The stress output shows exactly this. In every failing run, the drain line (and sometimes the `CoordinatedShutdown invoked ... ClrExitReason` line) appears as a bare stdout `[INFO]` line.

## Fix (test only; no quoted listing changes)

Before calling `StopAsync`, the test adds a `before-actor-system-terminate` task. That task waits, bounded at 5 s with `SpinWait.SpinUntil`, until the drain line has reached the captured logs. The actor system, and with it the logger actor, cannot stop before the line has gone through the logging bridge.

The fix keeps everything section 2.9 says:
- The app still logs through `system.Log`.
- The SIGTERM output block is unchanged.
- The test still proves "both the shutdown path and the logging bridge".

If the drain task is removed, the test still fails with the same Shouldly message after about 5 s. The default phase timeout is 5 s with `recover = on`, so the run cannot hang.

An app-side alternative was rejected: log the drain line through an MEL `ILogger` resolved from DI, as `ConnectionDrain` does at `ch21-s07`. It needs `WithActors((system, _, resolver) => ...)` in place of `AddStartup`, because `AddStartup` has no resolver overload. That means rewriting the 2.9 listing, the `AddStartup` bullet, the SIGTERM output and the "logging bridge" sentence, and the test would no longer check the bridge.

## Evidence

Stress runs: N copies of the test binary at once, on 16 cores.

| Build | Load | Failing runs |
| --- | --- | --- |
| `ch02-s09` baseline | 24 at once × 3 | 5 of 72 |
| `ch02-s09` baseline | 32 at once × 8 | 9 of 256 |
| `ch02-s09` with patch | 24 at once × 4 | 0 of 96 |
| `ch02-s09` with patch | 32 at once × 8 | 0 of 256 |
| `ch04-s01` with patch | 32 at once × 4 | 0 of 128 |
| `ch21-s10` (Akka 1.6.0-beta5) with patch | 32 at once × 4 | 0 of 128 |

Other checks:
- `dotnet test --project learn/Ch02.Hosted.Tests -- --filter-class Ch02.Hosted.Tests.ChargerAppTests`: 12 of 12 runs green (total 4).
- Full solution at `ch02-s09`: 3 of 3 runs give total 46, failed 0.
- Full solution at `ch04-s01`: 5 of 5 runs give total 137, failed 0, at a load average of about 55 to 70.
- `dotnet build -warnaserror`: 0 warnings at `ch02-s09`, `ch04-s01` and `ch21-s10`.
- `git apply --check` is clean at `ch02-s09`. The patch also applies cleanly at `ch04-s01` and `ch21-s10`.

## Replay

- Apply the patch on the `ch02-s09` commit.
- No later commit touches `learn/Ch02.Hosted.Tests/ChargerAppTests.cs`. `git diff ch02-s09 <tag> -- <file>` is empty for every tag up to `v1.0.0` and `ch21-s10`. The fix therefore carries through the rebase with no conflicts.
- Nothing fixed this forward; the file is the same at every tag.
- No package references change, so the `packages.lock.json` files (from `ch21`) are unaffected.

## Book impact

- No `// File:` listing quotes `ChargerAppTests.cs`.
- `book/ch02-actors-from-first-principles.md:1025`: "The diff shows `10 files changed, 242 insertions(+)`" must become **`10 files changed, 258 insertions(+)`**.
- The output of `git diff ch02-s08 ch02-s09 -- learn/Ch02.Hosted` is byte-identical.
- The prose at line 1014 stays true.
- Test counts do not change (Ch02.Hosted.Tests 4, `ch02-s09` total 46).
