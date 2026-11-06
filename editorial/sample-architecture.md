# Sample Repository Contract: `Ohmline`

*Editor-in-chief · 2026-11-04 (revision 2: persistence and CQRS chapters split, mobile chapter added, chapters renumbered) · binding on every coder, tester, writer and reviewer agent*

The sample lives at `/home/jonathan/code/ddd-actors-dotnet/sample/`. It is the source of truth for every line of code that appears in the book. This file defines its shape: what goes where, which versions are allowed, how it is built and tested, and how history is recorded. `editorial/outline.md` and `editorial/outline.json` define *what* each tagged section adds. If this file conflicts with an outline section's `codeChange`, raise the conflict with the editor; do not silently pick one.

---

## 1. Identity

| Item | Value |
|---|---|
| Solution file | `Ohmline.slnx`, the only solution file **at the repo root**. A second `.slnx` at the root makes a bare `dotnet build` fail with MSB1011. **The one exception** is `mobile/Ohmline.Mobile.slnx` (from `ch19-s04`), which lives in the `mobile/` subfolder and is the only solution that contains the .NET MAUI head. It never sits at the root, and the root solution never contains the MAUI head, so `dotnet build` at the root needs no MAUI workload (see §14). |
| Root namespace | `Ohmline` (namespace = project name + folders, file-scoped) |
| Target framework | `net11.0`, set once in `Directory.Build.props`. The only exception is the MAUI head `mobile/Ohmline.Mobile`, which sets `TargetFrameworks` itself (§14). |
| ActorSystem name | `ohmline` (the same on every node and in every test that joins a cluster) |
| Cluster roles | `charging` (Node), `gateway` (DeviceGateway) |
| Default branch | `main`, with linear history |
| License | MIT |

## 2. Toolchain

- **SDK:** `11.0.100-rc.3.26522.148` (go-live RC; GA is expected in November 2026, and Appendix A holds the re-pin checklist). C# language version 15 is the default. **Do not set `LangVersion`.**
- **Agents** run `source /home/jonathan/code/ddd-actors-dotnet/tools/env.sh` before any `dotnet` command. The system `dotnet` (10.0) must never be used. Readers install the SDK named in `global.json`.
- **If a build fails with MSB4242** (workload manifests), export `MSBuildEnableWorkloadResolver=false` for that shell and report it to the editor. **Never set it when building `mobile/Ohmline.Mobile.slnx`**: it switches off the MAUI workload, and the build then fails with NETSDK1208.
- **.NET MAUI (Chapter 19):** the `maui-android` workload is installed into the book's local SDK (`/home/jonathan/code/ddd-actors-dotnet/.dotnet`; workload set 11.0.100-rc.3.26524.4, MAUI manifest 11.0.0-rc.2.26505.5; there is no MAUI rc.3, and the rc.3 SDK ships MAUI rc.2). An Android SDK and a JDK are installed locally in the book folder; `tools/env.sh` exports `ANDROID_HOME`/`AndroidSdkDirectory` and `JAVA_HOME`/`JavaSdkDirectory` when they exist. Agents must not install or update workloads, SDK packages or JDKs without asking the editor. iOS and Mac Catalyst cannot be built on this Linux machine; Windows targets only on Windows.
- **Containers:** Podman (rootless) or Docker. For Testcontainers and Aspire with Podman:
  ```bash
  export DOCKER_HOST=unix:///run/user/$(id -u)/podman/podman.sock
  ```
- **Aspire CLI** 13.6.x. The AppHost uses `Aspire.AppHost.Sdk/13.6.4`.

### 2.1 `global.json` (exact, from `ch01-s07`)

```json
{
  "sdk": {
    "version": "11.0.100-rc.3.26522.148",
    "rollForward": "latestFeature",
    "allowPrerelease": true
  },
  "test": {
    "runner": "Microsoft.Testing.Platform"
  }
}
```

Without the `test` section, `dotnet test` on the .NET 11 SDK fails with xUnit v3 4.x. In MTP mode you must pass `--solution` or `--project`.

### 2.2 `Directory.Build.props` (root, as of `ch01-s07`; `ch03-s03` adds the `WarningsAsErrors` line)

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net11.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <!-- ch03-s03: exhaustiveness is non-negotiable even if someone turns TWAE off locally -->
    <!-- <WarningsAsErrors>$(WarningsAsErrors);CS8509;CS8655;nullable</WarningsAsErrors> -->
    <IsPackable>false</IsPackable>
    <Deterministic>true</Deterministic>
    <!-- Reproducible old tags: vulnerability audit runs as a separate CI job (ch21-s04), not in every build -->
    <NuGetAudit>false</NuGetAudit>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <PropertyGroup Condition="'$(GITHUB_ACTIONS)' == 'true'">
    <ContinuousIntegrationBuild>true</ContinuousIntegrationBuild>
  </PropertyGroup>
</Project>
```

**Additions over the course of the book:**

| Tag | Addition |
|---|---|
| `ch05-s03` | `Directory.Build.targets` imports `build/DomainPurity.targets` |
| `ch19-s04` | `mobile/Directory.Build.props` and `mobile/Directory.Build.targets` import the root files (MSBuild only picks up the nearest one of each) and clear `TargetFramework` for the MAUI head (§14) |
| `ch21-s02` | MinVer (`PrivateAssets="all"`, `MinVerTagPrefix` = `v`, so `chNN-sMM` tags are ignored) |
| `ch21-s04` | `RestorePackagesWithLockFile` (the MAUI head gets lock files too, restored in `--locked-mode` in CI) |

- **Analyzers:** do not raise `AnalysisLevel` or turn on `EnforceCodeStyleInBuild`. The default analyzer set plus Akka.Analyzers, all treated as errors, is the bar.
- **Suppressions:** `#pragma warning disable` and `NoWarn` are allowed only with a comment naming the reason and the book section that explains it.

### 2.3 Nested props

- **`tests/Directory.Build.props`** (from `ch03-s05`) imports the root props and sets:
  - `OutputType` = `Exe` (required by xUnit v3) and `IsTestProject` = `true`;
  - global `Using` for `Xunit` and `Shouldly`;
  - `PackageReference`s to `xunit.v3` and `Shouldly`.

  The non-test library `tests/Ohmline.Testing` overrides `OutputType` back to `Library` and sets `IsTestProject` to `false`.
- **`learn/Directory.Build.props`** (from `ch02-s01`) does the same for the learning projects. Learning test projects follow the same rules as `tests/`.
- **Test projects must not contain top-level statements.**

### 2.4 `Directory.Packages.props`: the complete allowed package set

Central Package Management is on. **No `Version` attribute ever appears on a `PackageReference`.**

- Add a `PackageVersion` in the **same commit that first references the package**, so the diff of each tag shows the dependency arriving.
- All `Akka.*` entries use `Version="$(AkkaVersion)"`, with `<AkkaVersion>1.5.73</AkkaVersion>` set in a `PropertyGroup` of this file.
- A package not in this table needs editor approval.

| Package | Version | First used |
|---|---|---|
| Akka | $(AkkaVersion) = 1.5.73 | ch02-s05 |
| Akka.TestKit.Xunit (this is the xUnit **v3** TestKit) | 1.5.73 | ch02-s05 |
| Akka.Hosting | 1.5.73 | ch02-s09 |
| Akka.Persistence.Hosting | 1.5.73 | ch10-s01 |
| Akka.Persistence.TestKit.Xunit | 1.5.73 | ch10-s04 |
| Akka.Persistence.Sql.Hosting (**not** the deprecated Akka.Persistence.PostgreSql/.Sqlite/.SqlServer) | 1.5.73 | ch10-s07 |
| Akka.Cluster.Hosting (brings Cluster, Sharding, Tools) | 1.5.73 | ch12-s01 |
| Akka.Hosting.TestKit | 1.5.73 | ch12-s09 |
| Akka.Aspire / Akka.Aspire.Hosting (only if 12.10 verification succeeds) | 1.5.73 | ch12-s10 |
| Akka.Persistence.Query.InMemory | 1.5.73 | ch14-s07 |
| Akka.Management | 1.5.73 | ch21-s06 |
| Akka.Discovery.KubernetesApi | 1.5.73 | ch21-s06 |
| Akka.Coordination.KubernetesApi | 1.5.73 | ch21-s06 |
| xunit.v3 | 4.0.2 | ch02-s01 |
| Shouldly | 4.3.0 | ch02-s01 |
| Microsoft.Extensions.TimeProvider.Testing | 10.11.0 | ch03-s01 |
| CsCheck | 5.0.1 | ch06-s08 |
| Microsoft.Data.Sqlite (10.0.0 pulls a vulnerable SQLitePCLRaw). One version for the server and the phone; `research/mobile.md` verified 11.0.0-rc.3.26522.148 (SQLitePCLRaw.bundle_e_sqlite3 3.0.5) for the client library. If 10.0.14 fails `IsAotCompatible` or the Android head at ch19-s06, that commit moves the whole repo to 11.0.0-rc.3.26522.148 and tells the editor. | 10.0.14 | ch10-s07 (server), ch19-s06 (phone) |
| Aspire.Hosting.AppHost (implicit via the SDK; pinned so CPM is satisfied) | 13.6.4 | ch12-s10 |
| Aspire.Hosting.PostgreSQL | 13.6.4 | ch12-s10 |
| Microsoft.Extensions.ServiceDiscovery | 10.11.0 | ch12-s10 |
| Microsoft.Extensions.Http.Resilience | 10.11.0 | ch12-s10 (ServiceDefaults), ch16-s04 |
| Testcontainers.PostgreSql | 4.16.0 | ch12-s11 |
| Testcontainers.XunitV3 | 4.16.0 | ch12-s11 |
| Microsoft.AspNetCore.Mvc.Testing | 11.0.0-rc.3.26522.148 | ch12-s11 |
| Npgsql | 10.0.3 | ch13-s04 |
| Microsoft.AspNetCore.SignalR.Client (the server hub needs no package: it is in the ASP.NET Core shared framework) | 11.0.0-rc.3.26522.148 | ch14-s05 (tests), ch19-s07 (phone) |
| Aspire.Hosting.Testing | 13.6.4 | ch16-s09 |
| OpenTelemetry.Extensions.Hosting | 1.20.1 | ch17-s02 (server), ch19-s08 (MAUI head) |
| OpenTelemetry.Exporter.OpenTelemetryProtocol | 1.20.1 | ch17-s02 (server), ch19-s08 (MAUI head) |
| OpenTelemetry.Instrumentation.AspNetCore | 1.20.1 | ch17-s02 |
| OpenTelemetry.Instrumentation.Http | 1.20.1 | ch17-s02 (server), ch19-s08 (MAUI head) |
| OpenTelemetry.Instrumentation.Runtime | 1.20.1 | ch17-s02 |
| OpenTelemetry.Exporter.InMemory | 1.20.1 | ch17-s08 |
| Microsoft.Extensions.Diagnostics.Testing | 10.11.0 | ch18-s03 |
| Petabridge.Cmd.Host / .Cluster / .Cluster.Sharding | 1.5.2 | ch18-s08 |
| CommunityToolkit.Mvvm (source generators; C# partial properties) | 8.4.2 | ch19-s04 |
| Microsoft.Maui.Controls (`$(MauiVersion)` of the installed workload; under CPM the head's `PackageReference` has no `Version` attribute) | 11.0.0-rc.2.26505.5 | ch19-s04 (MAUI head only) |
| Microsoft.Extensions.Logging.Debug (the MAUI template floats `11.0.0-*`; pinned here) | 11.0.0-rc.3.26522.148 | ch19-s04 (MAUI head only) |
| Aspire.Hosting.Maui (preview; only if 19.8 verification succeeds, behind `IncludeMobile=true`) | 13.6.4-preview.1.26523.11 | ch19-s08 (AppHost) |
| Aspire.Hosting.DevTunnels (only together with Aspire.Hosting.Maui) | 13.6.4 | ch19-s08 (AppHost) |
| dbup-postgresql | 7.0.1 | ch20-s08 |
| MinVer | 8.0.0 | ch21-s02 |
| Microsoft.FeatureManagement.AspNetCore | 4.8.0 | ch21-s08 |
| *(branch `upgrade/akka-1.6` only)* Akka.* | 1.6.0-beta5 | ch21-s10 |
| *(branch `upgrade/akka-1.6` only)* Akka.Serialization.V2 | 1.6.0-beta5 | ch21-s10 |

**Packages that are deliberately not used:**

| Package | Why not |
|---|---|
| FluentAssertions 8 | Commercial licence |
| Verify.* | The SC021 exemption expires and would break old tags |
| FsCheck | Its `Gen` clashes with CsCheck's; one property library is enough |
| Microsoft.NET.Test.Sdk, xunit.runner.visualstudio, coverlet.collector | VSTest-era packages, not needed with MTP |
| OpenTelemetry.Exporter.Prometheus.* | Still beta |
| Akka.HealthCheck* | Deprecated, and can crash startup |
| Akka.Serialization.Hyperion | Not needed |
| EF Core | Billing uses Npgsql directly |
| Any `Akka.*` package in a mobile project | The domain and the actors stay on the server (section 19.10). Akka.NET 1.5.73 fails under full trimming and NativeAOT, and Akka.Hosting needs hosted services that MAUI never starts |
| Akka.Hosting.Maui | Stale (1.5.8, net6.0), and Akka is not used on the phone anyway |
| sqlite-net-pcl | Reflection-based mini-ORM, risky under full trimming; Microsoft.Data.Sqlite makes the outbox SQL visible |
| CommunityToolkit.Maui | Not needed for the sample's screens |
| Proto.Actor, Microsoft.Orleans.Client | Not on a device; see section 19.10 |

## 3. Folder layout (final state at `ch21-s09`)

```
sample/
  Ohmline.slnx  global.json  NuGet.config  .editorconfig  .gitignore  .gitattributes  LICENSE  README.md
  Directory.Build.props  Directory.Build.targets  Directory.Packages.props
  build/DomainPurity.targets
  docs/            vision.md glossary.md naming-rules.md house-style.md ohmline-primer.md subdomains.md
                   highlighted-core.md context-map.md testing-strategy.md feeds.md observability.md
                   journal-tour.md persistence-choices.md consistency-per-screen.md idempotency-contract.md
                   adr/NNNN-*.md  canvases/  domain-stories/  eventstorming/  acl/  runbooks/
  learn/           Ch02.ActorsByHand  Ch02.ActorsByHand.Tests  Ch02.AkkaBasics.Tests  Ch02.Hosted
                   Ch03.LanguageLab.Tests  Ch03.ClosedLib  Ch10.PersistenceLab.Tests
  src/
    Ohmline.SharedKernel/          pure: units, Money, identifiers, Result, ValidationError, IValueObject,
                                   IDecider, DecisionContext, specifications, SiteLocalTime, GuidV7
    Ohmline.ActorShell/            Akka: DeciderActor, StateSourcedDeciderActor, ManifestJsonSerializer,
                                   EntityMessageExtractor, Stop, Send/TellTraced extensions,
                                   TraceEnvelope(+Serializer), TracedPersistentActor, telemetry helpers
    Charging/
      Ohmline.Charging.Contracts/  commands, replies, queries (closed records); Integration/ (published language)
      Ohmline.Charging.Client.Contracts/  client-facing published language: plain DTOs, RejectionCodes,
                                   ChargingClientJsonContext (source-generated); no references at all
      Ohmline.Charging.Domain/     ChargePoints/ Sessions/ Journeys/ Listings/ Metering/ Policies/  (pure)
      Ohmline.Charging.Actors/     actors, ChargingSerializer, tagger, ports, AddChargingContext()
      Ohmline.Charging.ReadModels/ projections (incl. DriverApp/), offset store, outbox relay, subscribers,
                                   dispatcher
      Ohmline.Charging.Api/        MapChargingEndpoints(), /app/v1 endpoints, feed and query endpoints,
                                   Live/ (ChargingHub, LiveUpdatesBridge)
    SiteEnergy/
      Ohmline.SiteEnergy.Contracts/  Ohmline.SiteEnergy.Domain/  Ohmline.SiteEnergy.Actors/
      Ohmline.SiteEnergy.Api/  Ohmline.SiteEnergy.Acl.AssetRegistry/  Ohmline.SiteEnergy.Acl.GridOperator/
    Tariffs/
      Ohmline.Tariffs.Contracts/  Ohmline.Tariffs.Domain/  Ohmline.Tariffs.Actors/
    DriverAccounts/
      Ohmline.DriverAccounts.Contracts/  Ohmline.DriverAccounts.Domain/  Ohmline.DriverAccounts.Actors/
      Ohmline.DriverAccounts.Api/
    Billing/
      Ohmline.Billing.Contracts/   hold/capture/void request+response DTOs
      Ohmline.Billing/             Web service, Npgsql, inbox/outbox, TallyPay ACL (no Akka)
    DeviceGateway/
      Ohmline.DeviceGateway.Ocpp/  pure frame parser + translator (no Akka)
      Ohmline.DeviceGateway/       Web host, cluster role gateway, ChargerConnectionActor
    Mobile/
      Ohmline.Mobile.Core/         plain net11.0 library for the driver app: view models (CommunityToolkit.Mvvm),
                                   ChargingApiClient, Outbox/ (SqliteOutbox, OutboxDispatcher), ReadModels/
                                   (SqliteReadModelCache, LiveUpdatesClient), Diagnostics/, Platform/ interfaces
    Ohmline.Node/                  Web host, cluster role charging; composes all Akka contexts + Api projects
    Ohmline.ServiceDefaults/       OTel, health endpoints, service discovery, resilience
    Ohmline.AppHost/               Aspire AppHost
    Ohmline.Migrator/              DbUp console (journal, read models, billing)
  mobile/                          everything that needs the MAUI workload; NOT in Ohmline.slnx
    Ohmline.Mobile.slnx            Client.Contracts + Mobile.Core + Mobile.Core.Tests + the head (paths ../src/...)
    Directory.Build.props  Directory.Build.targets   import the root files; clear TargetFramework
    README.md                      workload, Android SDK and JDK setup; how to run on the emulator
    Ohmline.Mobile/                thin .NET MAUI head: MauiProgram, AppShell, Pages/*.xaml, platform adapters
                                   (IConnectivity, IAppDispatcher, DB path, window lifecycle), Platforms/
  tools/
    verify-tags.sh  legacy-assets/assettrack-export.csv
    Ohmline.Simulator/  Ohmline.FakePsp/  Ohmline.FakeGridOperator/  Ohmline.JournalInspect/
  tests/
    Directory.Build.props
    Ohmline.Testing/               (library) DeciderSpec, DeciderProperties, Golden, ActorTestConfig,
                                   PersistenceShellSpec
    golden/                        golden JSON files; golden/corpus/ (every manifest version ever written)
    Ohmline.<Project>.Tests/       one per production project with logic (mirrors the src name),
                                   including Ohmline.Mobile.Core.Tests
    Ohmline.ArchitectureTests/  Ohmline.Node.Tests/  Ohmline.IntegrationTests/  Ohmline.SystemTests/
    Ohmline.ContractTests/  Ohmline.Telemetry.Tests/
  deploy/
    observability/  (compose.yaml, collector-config.yaml, dashboards/, alerts/)
    k8s/base/  k8s/overlays/{dev,prod}/
  .github/workflows/  ci.yml  release-please.yml      .github/CODEOWNERS
```

Solution folders in `Ohmline.slnx` follow the physical folders: `/learn/`, `/src/`, `/src/Charging/`, …, `/src/Mobile/`, `/tools/`, `/tests/`. Nothing under `mobile/` is ever added to `Ohmline.slnx`.

## 4. Layers and dependency rules

| Layer (suffix) | SDK | May reference | Must not reference |
|---|---|---|---|
| `Ohmline.SharedKernel` | Microsoft.NET.Sdk | BCL only | any package |
| `*.Contracts` | Microsoft.NET.Sdk | SharedKernel | any package; any Domain/Actors project |
| `Ohmline.Charging.Client.Contracts` (the client-facing published language, from `ch19-s02`) | Microsoft.NET.Sdk, `IsAotCompatible` | nothing (BCL and `System.Text.Json` source generation only) | SharedKernel, any project, any package |
| `*.Domain` | Microsoft.NET.Sdk | SharedKernel, its own Contracts, other contexts' Contracts (Conformist/Customer side) | any package at all (BCL only: `System.Collections.Immutable`, `TimeProvider`, `System.Text.Json` attributes are **not** used here either); any Actors/Api/ReadModels project |
| `*.Acl.*` (pure translators) | Microsoft.NET.Sdk | SharedKernel, Contracts, Domain of its context | Akka (the GridOperator ACL's poller actor is the one exception and lives in `Ohmline.SiteEnergy.Acl.GridOperator`, which may reference Akka and ActorShell) |
| `Ohmline.ActorShell` | Microsoft.NET.Sdk | SharedKernel, Akka.*, System.Text.Json | any context project |
| `*.Actors` | Microsoft.NET.Sdk | its Domain + Contracts, other contexts' Contracts, ActorShell, Akka.* | other contexts' Domain or Actors |
| `*.ReadModels` | Microsoft.NET.Sdk | its Domain + Contracts (including Client.Contracts from `ch19-s02`), ActorShell, Akka.*, Npgsql | other contexts' Domain (consume their Contracts or events via tags only) |
| `*.Api` | Microsoft.NET.Sdk (with `<FrameworkReference Include="Microsoft.AspNetCore.App" />`, which also brings the SignalR hub) | its Contracts and Client.Contracts, ActorShell, Akka.Hosting | Domain internals that are not public API |
| `Ohmline.Mobile.Core` (from `ch19-s04`) | Microsoft.NET.Sdk, `net11.0`, `IsAotCompatible` | `Ohmline.Charging.Client.Contracts`; CommunityToolkit.Mvvm, Microsoft.Data.Sqlite, Microsoft.AspNetCore.SignalR.Client; `System.Diagnostics` (ActivitySource) from the BCL | SharedKernel, every other `src/` project, Akka.*, Microsoft.Maui.*, OpenTelemetry SDK packages, ASP.NET Core server assemblies |
| `mobile/Ohmline.Mobile` (MAUI head, from `ch19-s04`) | Microsoft.NET.Sdk with `UseMaui` | Mobile.Core, Client.Contracts, Microsoft.Maui.Controls, Microsoft.Extensions.Logging.Debug, OpenTelemetry SDK packages (from `ch19-s08`) | any server project; Akka.*. Never in `Ohmline.slnx`; never referenced by the AppHost as a `ProjectReference` |
| Hosts (`Ohmline.Node`, `Ohmline.DeviceGateway`, `Ohmline.Billing`, tools) | Microsoft.NET.Sdk.Web | anything they compose, plus ServiceDefaults | — |
| `Ohmline.AppHost` | Aspire.AppHost.Sdk/13.6.4 | host projects (as Aspire project resources); the MAUI head only by path through `AddMauiProject` (ch19-s08, if verified) | — |

**Enforcement:**

- `build/DomainPurity.targets` fails the build if a `*.Domain` project has a forbidden `PackageReference`.
- `tests/Ohmline.ArchitectureTests` checks referenced assemblies by reflection.
- Both arrive in `ch05-s03`. The rules for `Ohmline.Charging.Client.Contracts` arrive in `ch19-s02` and those for `Ohmline.Mobile.Core` in `ch19-s04`.

**What goes where:**

- **Commands and replies** that other contexts or the edge send live in `*.Contracts`.
- **HTTP DTOs for client apps** live in `Ohmline.Charging.Client.Contracts` (from `ch19-s02`; before that, in `Ohmline.Charging.Api/App/Dtos`). The Api translates them to and from internal commands; they never reach an actor.
- **Domain events, state, errors and deciders** live in `*.Domain`.
- **Snapshot DTOs** live in `*.Actors`.
- **The phone's own types** (client value objects, screen and command-status unions, outbox rows) live in `Ohmline.Mobile.Core` and are never shared with the server.

## 5. C# conventions

### 5.1 Unions vs closed records (ADR 0003, from `ch03-s04`)

| Kind | Construct |
|---|---|
| Commands, replies, queries, domain events, snapshot DTOs, integration events: anything told, asked, replied, persisted, or on HTTP/feed wire | `public closed record XxxCommand(...)` base with `public sealed record` leaves |
| Client contracts (`Ohmline.Charging.Client.Contracts`) | `public sealed record` DTOs with primitive fields, each listed in `ChargingClientJsonContext`; no hierarchies that need polymorphic JSON |
| Aggregate state, decisions, error sets, `Result<T,E>`, effects, parse/translation results, actor-side protocol unions, multi-context process-manager inputs | `public union Xxx(CaseA, CaseB, ...)` over existing (usually `sealed record`) case types |

**Rules:**

- **Never `Tell`, `Ask`, reply with, or `Persist` a union value.**
  - Send `.Value` through `SendExtensions.Send`.
  - Persist the case records returned by `Decide`, which are `closed record` events, never a union.
- **Every switch that a `default(union)` can reach has a `null => ...` arm.** Prefer a `null` arm over `_ =>` so that exhaustiveness still catches new cases.
- **Use switch *expressions*** for decision tables. Only switch expressions are checked for exhaustiveness.
- **Put a union and its case records in one file named after the union.** Put a closed hierarchy in one file named after the base type. Otherwise, one type per file.

### 5.2 Domain-model conventions

- **Value objects** are `readonly record struct`s with `field`-validated `init`, or `sealed record`s with a private constructor. Both implement `IValueObject<TSelf,TValue>`, and `TryCreate` returns `Result<TSelf, ValidationError>`. Do not throw exceptions for expected failures.
- **The decider shape** (from `ch07-s06`):
  ```csharp
  public interface IDecider<TState, TCommand, TEvent, TError>
  {
      static abstract TState Initial { get; }
      static abstract Result<ImmutableArray<TEvent>, TError> Decide(TState state, TCommand command, DecisionContext context);
      static abstract TState Evolve(TState state, TEvent @event);
  }
  ```
  - `DecisionContext` carries `Now` (taken from `TimeProvider`) and, from `ch21-s08`, feature-flag values.
  - `Evolve` is total and never throws for any event that `Decide` can produce.
  - Neither `Decide` nor `Evolve` reads a clock, generates random values, or does I/O.
- **Collections:** use `ImmutableArray<T>` and `ImmutableDictionary<TKey,TValue>` in state and events, and collection expressions where they read well.
- **IDs** are strongly typed: `SessionId` and `JourneyId` wrap a Guid v7 created from `TimeProvider`.
- **Code style:** classes are `sealed` by default; namespaces are file-scoped; there are no `#region`s, and no comments that restate the code.

### 5.3 Naming

- **Commands** are imperative (`ReserveConnector`); **events** are past tense (`ConnectorReserved`).
- **Banned names:** `Update*`, `Set*` (except the OCPP `SetChargingProfile`, which keeps its external name), `*Manager`, `*Helper`, `*Util`.
- **Test names:** `Given_<state>_when_<command>_then_<outcome>`, or a sentence in the ubiquitous language using `[Fact(DisplayName = ...)]`. Follow the style guide.

## 6. Durable names (never change after first commit; see `docs/contracts-that-never-die.md`)

| Kind | Values |
|---|---|
| Shard type names | `charge-point`, `charge-point-listing`, `charging-session`, `charging-journey`, `site-power-budget`, `id-token` (+ `charger-commands` if chosen in ch16-s07) |
| Singleton names | `tariff-book` |
| Sharded daemon / projection names | `projections`; projection ids `session-history-v1`, `site-live-board-v1`, `driver-app-v1`, `integration-relay-v1`, `session-ended-subscriber-v1`, `charging-profile-dispatcher-v1`, `session-history-v2` |
| PersistenceId format | `{shardTypeName}-{entityId}`, e.g. `charge-point-OHM-SLC-0042`, `charge-point-listing-OHM-SLC-0042`; singleton `tariff-book` |
| maxNumberOfShards | `100` for every region |
| Event tags | `charging`, `charging-session`, `charging-journey`, `site-energy`, `accounts`, `tariffs` |
| DistributedPubSub topics | `session-summaries` (ch14-s04), `driver-app-cards` (ch14-s06) |
| SignalR | hub path `/hubs/charging`; groups `session-{sessionId}`, `token-{tokenUid}`; client methods `sessionSummaryChanged`, `driverCardChanged` (ch14-s05) |
| Client API | route prefix `/app/v1` (ch14-s06); `RejectionCodes` string values in `Ohmline.Charging.Client.Contracts` (ch19-s02) are append-only |
| Phone storage | SQLite file `ohmline-driver.db3` in `FileSystem.AppDataDirectory`; tables `outbox`, `session_card`, `feed_position` (ch19-s06, ch19-s07). Not a journal: the cache may be dropped and refilled, the outbox must survive app upgrades |
| Serializer IDs | 9100 `TraceEnvelopeSerializer` (ch17-s04), 9101 `ChargingSerializer`, 9102 `SiteEnergySerializer`, 9103 `DriverAccountsSerializer`, 9104 `TariffsSerializer`; 9105–9199 reserved |
| Manifest scheme (internal) | `{context}.{kebab-type-name}.v{N}`, where context is one of `charging`, `site-energy`, `accounts`, `tariffs`; snapshots `{context}.{aggregate}-snapshot.v{N}`; envelope `ohmline.trace-envelope.v1` |
| Manifest/discriminator scheme (Published Language) | `ohmline.{context}.{kebab-type-name}.v{N}`, e.g. `ohmline.charging.session-completed.v1` |
| Databases / schemas | Postgres 17 database `ohmline` (Akka.Persistence.Sql default journal/snapshot/tag tables in `public`; `read_models` schema); database `billing` (schema `billing`) |
| ActivitySource / Meter names | `Ohmline.Charging`, `Ohmline.SiteEnergy`, `Ohmline.DriverAccounts`, `Ohmline.Tariffs`, `Ohmline.DeviceGateway`, `Ohmline.Billing`, `Ohmline.ActorShell`, `Ohmline.Mobile` (phone, ch19-s08) |
| Metric names | `ohmline.{context}.{name}` (e.g. `ohmline.charging.energy.delivered`, unit `kWh`) |
| Aspire resource / service names | `postgres`, `ohmline` (db), `billingdb`, `migrator`, `node`, `gateway`, `billing`, `fakepsp`, `fakegrid`, `simulator`, `mobile` (ch19-s08, only with `IncludeMobile=true`) |

## 7. Serialization (from `ch10-s03`; the phone from `ch19-s02`)

- **Serializer per context:** one `ManifestJsonSerializer` subclass per context, built on System.Text.Json. Manifests come from an explicit `FrozenDictionary` registry and are never derived from type names at runtime. An unknown manifest throws `SerializationException`.
- **Registration:** each serializer is registered with `WithCustomSerializer(...)` and bound to the closed-record base types (commands, events, replies, queries and snapshot DTOs) of its context.
- **Shared converters:** value objects from SharedKernel use converters that live in `Ohmline.ActorShell`, so SharedKernel takes no serialization dependency.
- **Golden files:** every manifest has a golden JSON file under `tests/golden/`. Accept a changed or new golden file with `OHMLINE_ACCEPT_GOLDEN=1`. An unexpected diff fails the test.
- **The golden corpus** (from `ch20-s05`) keeps every version ever written, forever.
- **Actor test configuration:** every actor test sets `akka.actor.serialize-messages = on`, through the `ActorTestConfig` HOCON in `Ohmline.Testing` (from `ch10-s04`).
- **Client contracts** are serialized with the source-generated `ChargingClientJsonContext`, on the server (the `/app/v1` endpoints) and on the phone. Every client DTO has a golden file in `Ohmline.ContractTests` (from `ch19-s02`). The phone never uses reflection-based `JsonSerializer` overloads.

## 8. Persistence by stage

| Stage | Journal / snapshot store | Used by |
|---|---|---|
| Unit tests (domain) | none: pure functions | `*.Domain.Tests` |
| Actor tests | Akka in-memory journal via Akka.Persistence.TestKit (`serialize-messages = on`) | `*.Actors.Tests`, `Ohmline.Node.Tests` |
| Journal-tour and SQLite-backed actor tests (from ch10-s08) | Akka.Persistence.Sql, `ProviderName.SQLiteMS`, a temporary file per test, real serializers | `Ohmline.Charging.Actors.Tests` (`JournalTour/`, `PoisonEventTests`), `Ohmline.Charging.ReadModels.Tests` |
| Local single node (ch10-s07 → ch12-s09) | Akka.Persistence.Sql, `ProviderName.SQLiteMS`, `Data Source=ohmline.db`, `autoInitialize: true` | `Ohmline.Node` in Development |
| Local cluster (from ch12-s10) | Akka.Persistence.Sql, `ProviderName.PostgreSQL15`, Postgres 17 via Aspire, tag-table mode | AppHost |
| Integration tests (from ch12-s11) | Postgres 17 via Testcontainers.PostgreSql | `Ohmline.IntegrationTests` |
| From ch20-s08 | as above but `autoInitialize: false`; schema applied by `Ohmline.Migrator` (DbUp); AppHost `WaitForCompletion(migrator)` | all hosts |
| The phone (from ch19-s06) | **no journal and no snapshot store.** SQLite (`Microsoft.Data.Sqlite`, WAL) holds the command outbox and a disposable read-model cache | `Ohmline.Mobile.Core`, `Ohmline.Mobile.Core.Tests` (temporary files) |

**Akka.NET has no Durable State store** in 1.5.73 or 1.6.0-beta5. The state-sourced `ChargePointListing` (ch11-s07) uses the same journal and snapshot store as everything else; do not add a separate state-store plugin or table.

## 9. Testing

- **Framework and runner:**
  - xUnit v3 4.0.2 on Microsoft.Testing.Platform, with Shouldly for assertions and CsCheck for properties.
  - Akka.TestKit.Xunit, Akka.Persistence.TestKit.Xunit and Akka.Hosting.TestKit, all 1.5.73.
  - `FakeTimeProvider` for time, `MetricCollector<T>` for metrics, the OpenTelemetry InMemory exporter for traces, Testcontainers for Postgres, and Aspire.Hosting.Testing for system tests.
  - **Mobile:** all app logic is tested in `tests/Ohmline.Mobile.Core.Tests`, which is in `Ohmline.slnx` and runs on any machine without MAUI workloads (fake `HttpMessageHandler`, fake `IConnectivity`/`IAppDispatcher`, temporary SQLite files). The MAUI head has no tests of its own; it must build (§11). UI automation and device tests are out of scope.
- **Async and cancellation:**
  - Use only the `*Async` TestKit assertion methods.
  - Pass `TestContext.Current.CancellationToken` where an API accepts a token.
  - Never call `Thread.Sleep`. Use `AwaitAssertAsync`, a `TestScheduler`, or `FakeTimeProvider` instead.
- **Timing:** `akka.test.timefactor` is read from the environment variable `OHMLINE_TIMEFACTOR` (default 1; CI uses 3).
- **Traits:** tests that need containers carry `[Trait("Category", "Integration")]` (Testcontainers) or `[Trait("Category", "System")]` (Aspire). The fast loop is:
  ```bash
  dotnet test --solution Ohmline.slnx -- --filter-not-trait "Category=Integration" --filter-not-trait "Category=System"
  ```
  Verify the exact MTP filter syntax at ch09-s01 and fix this line if it differs.
- **Isolation:** tests must not depend on execution order, on wall-clock time, or on fixed ports. Use port 0 for Akka remoting in tests.

## 10. Git conventions

- **Branches:** `main` has linear history. There are no merge commits on `main`.
- **Commit messages:**
  - Subject: `chNN-sMM: <Section title exactly as in outline.json>`.
  - Body: two to six lines saying what changed and why, in plain prose.
  - Trailer: `Book-Section: chNN-sMM`.
  - Commit identity: `Ohmline Book <book@ohmline.example>` (set with `git config` inside `sample/`).
- **One commit per section, normally.** A section may contain several commits only when its narrative needs them (for example, ch21-s09 release day). The tag then points to the last commit.
- **Tags:**
  - Every section whose `codeTag` is not null gets exactly one **annotated** tag: `git tag -a chNN-sMM -m "chNN-sMM: <title>"`.
  - Sections with `codeTag: null` get no commit and no tag.
  - Version tags `vX.Y.Z` appear only from ch21-s09 on. MinVer reads only `v`-prefixed tags.
- **Side branches** (each forks from the named tag; see outline.md for the full list):
  - `wrong/<tag>-<slug>`: one commit, never merged. It must build. Its failure shows up as a test whose name starts with `Demonstrates_`; in the `wrong/` branch that test fails, or it asserts the bad behaviour. ch21-s02's branch may fail at startup by design.
  - `alt/<tag>-<slug>`: a design alternative.
  - `upgrade/akka-1.6`: holds tag `ch21-s10`.
- **Fixing an earlier section before the book is frozen:**
  1. Amend that section's commit with a scripted, non-interactive rebase (`git rebase --onto`, or `GIT_SEQUENCE_EDITOR` scripts; `-i` is not available to agents).
  2. Re-point every later tag to its rewritten commit.
  3. Rebase the side branches.
  4. Re-run `tools/verify-tags.sh`.
  5. Tell the editor, so that writers whose chapters quote the changed files can re-quote them.
- **Never** force-push a published remote without editor approval. Until the book is frozen, the repo is local only.

## 11. Definition of done for every tag

From a clean checkout of the tag, with `tools/env.sh` sourced and `DOCKER_HOST` set:

```bash
dotnet build Ohmline.slnx -warnaserror      # 0 warnings, 0 errors
dotnet test --solution Ohmline.slnx         # all green, including Integration/System traits
```

**Mobile tags.** At every tag where `mobile/Ohmline.Mobile.slnx` exists (every tag from `ch19-s04` on, in Chapter 19 and in every later chapter), the MAUI head must also build for Android:

```bash
dotnet build mobile/Ohmline.Mobile.slnx -warnaserror   # 0 warnings, 0 errors; on Linux the head's
                                                     #   only target framework is net11.0-android
```

- Run it with `tools/env.sh` sourced, so that `ANDROID_HOME`/`AndroidSdkDirectory` and `JAVA_HOME`/`JavaSdkDirectory` are set, and **without** `MSBuildEnableWorkloadResolver=false`.
- Do not pass `-f net11.0-android` to the solution build (it would force the plain `net11.0` projects onto Android). To build only the head, use `dotnet build mobile/Ohmline.Mobile/Ohmline.Mobile.csproj -f net11.0-android -warnaserror`.
- `tools/verify-tags.sh` runs this build at every tag where the file exists (from `ch19-s04`). Without `ANDROID_HOME` or `JAVA_HOME` it fails loudly; readers without an Android toolchain may set `OHMLINE_SKIP_MOBILE=1`, agents may not.
- The root `Ohmline.slnx` build and tests must not change behaviour because of `mobile/`: `MSBuildEnableWorkloadResolver=false dotnet build Ohmline.slnx` still succeeds at every tag.
- The coder records in the handoff notes the head's build result and, when the section makes a runtime claim, the result of an actual run on the Android emulator. Claims about running on iOS or a physical device are hedged.

Additional requirements:

- `git status` is clean after the build and test run. Generated files belong in `.gitignore`.
- New code has tests in the same commit, unless the section is docs-only. Every decider arm has a test. Every manifest has a golden file.
- Any new package is in the §2.4 table, and its `PackageVersion` arrives in this commit.
- Durable names follow §6.
- `tools/verify-tags.sh` passes for every tag up to this one before a chapter is handed to its writer.

## 12. How to run

```bash
source tools/env.sh                                   # agents only; readers use their installed SDK
dotnet build Ohmline.slnx
dotnet test --solution Ohmline.slnx                   # everything (needs Podman/Docker from ch12-s11)
dotnet run --project src/Ohmline.Node                 # ch10-s01 … ch12-s09: single node, SQLite
aspire run --project src/Ohmline.AppHost              # from ch12-s10: Postgres + cluster + dashboard
                                                      #   (or: dotnet run --project src/Ohmline.AppHost)
dotnet run --project tools/Ohmline.Simulator -- --chargers 5 --scenario plug-in-and-charge   # from ch16-s07
podman compose -f deploy/observability/compose.yaml up                                      # from ch18-s07
dotnet build mobile/Ohmline.Mobile.slnx                                                     # from ch19-s04
dotnet build mobile/Ohmline.Mobile/Ohmline.Mobile.csproj -f net11.0-android -t:Run          # emulator running
kubectl kustomize deploy/k8s/overlays/dev                                                   # from ch21-s06
tools/verify-tags.sh                                  # build + test every chNN-sMM tag in order
                                                      #   (+ the mobile solution where it exists)
```

## 13. Configuration keys

| Key | Meaning |
|---|---|
| `ConnectionStrings:ohmline` | Journal, snapshots and read models |
| `ConnectionStrings:billing` | Billing database |
| `Akka:Remote:Hostname`, `Akka:Remote:Port` | Remoting endpoint (0 in tests) |
| `Akka:Cluster:SeedNodes` | Static seeds (dev/Aspire fallback) |
| `Ohmline:Charging:AskTimeout` | Endpoint Ask timeout (default 5 s; validated on start) |
| `Ohmline:Projections:RefreshInterval`, `Ohmline:Projections:QueryDelay` | Persistence Query polling (default 100 ms each; ch14-s03) |
| `Ohmline:ReadModels:ReadYourWritesTimeout` | How long a `?minVersion=` read waits (default 2 s; ch14-s01) |
| `IncludeMobile=true` | AppHost adds the MAUI head as a resource (ch19-s08, only if Aspire.Hosting.Maui is verified) |
| `Ohmline:ApiBaseAddress` | The phone's API base address, in the head's embedded `appsettings.json` (`http://10.0.2.2:<port>` for the Android emulator; ch19-s04) |
| `OHMLINE_KUBERNETES=true` | Switch to Akka.Management bootstrap, Kubernetes discovery and the lease SBR (ch21-s06) |
| `OHMLINE_TIMEFACTOR` | TestKit time factor |
| `OHMLINE_ACCEPT_GOLDEN=1` | Accept golden-file changes |
| `OHMLINE_SKIP_MOBILE=1` | `tools/verify-tags.sh` skips the mobile solution (readers without an Android toolchain only) |
| `FLAKY_RATE` | FakePsp failure rate (0–1) |

## 14. The mobile app (Chapter 19)

**Why two solutions.** `Ohmline.slnx` at the root contains everything that builds with the plain SDK, including all of the app's logic (`src/Mobile/Ohmline.Mobile.Core`) and its tests. `mobile/Ohmline.Mobile.slnx` adds the .NET MAUI head, which needs the `maui-android` workload, an Android SDK and a JDK. Keeping the second solution in a subfolder keeps a bare `dotnet build` at the root working (MSB1011 otherwise), and a reader without MAUI only ever sees NETSDK1208 if they build `mobile/` on purpose. A `.slnf` filter cannot do this: it can only subtract from the root solution, which would then have to contain the head.

**`mobile/Directory.Build.props`** imports `../Directory.Build.props` and then empties `TargetFramework`, so the head's own `TargetFrameworks` apply. **`mobile/Directory.Build.targets`** imports `../Directory.Build.targets`. Everything else (Nullable, `TreatWarningsAsErrors`, CPM, CS8509/CS8655 as errors, and later MinVer and lock files) applies to the head unchanged. A `NoWarn` in the head needs the same justification comment as anywhere else.

**The head's project file** (shape; the coder of ch19-s04 owns the exact text):

- `<UseMaui>true</UseMaui>`, `<SingleProject>true</SingleProject>`, `<OutputType>Exe</OutputType>`;
- `<TargetFrameworks>net11.0-android</TargetFrameworks>`, plus `net11.0-ios;net11.0-maccatalyst` when `$([MSBuild]::IsOSPlatform('osx'))` and `net11.0-windows10.0.19041.0` when `$([MSBuild]::IsOSPlatform('windows'))`;
- `SupportedOSPlatformVersion` 24.0 for Android (the .NET 11 minimum), 17.0 for Mac Catalyst;
- `ApplicationId` `example.ohmline.driver`, `ApplicationTitle` `Ohmline`;
- `PackageReference`s without versions: `Microsoft.Maui.Controls`, `Microsoft.Extensions.Logging.Debug` (Debug only), OpenTelemetry packages from ch19-s08;
- `ProjectReference`s to `../../src/Mobile/Ohmline.Mobile.Core` and `../../src/Charging/Ohmline.Charging.Client.Contracts` only.

**The head stays thin.** It holds `MauiProgram` (DI registration of Core services, the typed `HttpClient`, the OpenTelemetry SDK), `AppShell`, XAML pages bound to Core view models, and adapters for platform services: `IConnectivity` over `Microsoft.Maui.Networking.Connectivity`, `IAppDispatcher` over `MainThread`, the database path from `FileSystem.AppDataDirectory`, and the window lifecycle (`Stopped` stops the dispatcher and the hub connection; `Resumed` restarts them, catches up and nudges). Logic that can be tested belongs in `Ohmline.Mobile.Core`.

**On the phone, Akka.NET is not used** (no `Akka.*` reference in any mobile project; section 19.10 explains why). The client's single writer is a `Channel` with `SingleReader = true`.

**Platforms.** Android is built at every mobile tag. iOS and Mac Catalyst need a Mac with Xcode and are not built by the book's tooling; the project file must still be correct for them, and the chapter hedges any iOS claim. Android needs the `ACCESS_NETWORK_STATE` permission for connectivity and cleartext HTTP to `10.0.2.2` in Debug only.
