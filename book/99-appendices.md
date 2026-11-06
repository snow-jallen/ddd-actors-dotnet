# Appendix A. Versions and the Version Matrix

Every version below was read from the sample: `global.json` and `Directory.Packages.props` at the last section tag on `main`, `ch21-s09` (unchanged at the release commit tagged `v1.0.0`), workflow and compose files at the same tag, and the toolchain the book was verified on. The "First tag" column is the first section tag at which the item appears; the sample adds each `PackageVersion` in the same commit as the first project that references it.

## A.1 SDK, language and build

| Item | Version | First tag | Notes |
|---|---|---|---|
| .NET SDK | 11.0.100-rc.3.26522.148 | `ch01-s07` | `global.json`: `rollForward: latestFeature`, `allowPrerelease: true` |
| Target framework | `net11.0` | `ch01-s07` | Set once in `Directory.Build.props`; the MAUI head sets its own `TargetFrameworks` |
| C# | 15 | `ch01-s07` | The default for `net11.0`; no project sets `LangVersion` |
| Test platform | Microsoft.Testing.Platform | `ch01-s07` | `global.json` `test.runner`; `dotnet test` takes `--solution` or `--project` |
| Solution format | `.slnx` | `ch01-s07` | `Ohmline.slnx` at the root; `mobile/Ohmline.Mobile.slnx` from `ch19-s04` |
| Central Package Management | on | `ch01-s07` | No `PackageReference` carries a `Version` attribute |
| Package lock files | on | `ch21-s04` | `RestorePackagesWithLockFile`; CI restores in locked mode |
| MSBuild SDKs | `Microsoft.NET.Sdk`, `Microsoft.NET.Sdk.Web` | `ch02-s01` | Libraries and tests; web hosts |
| Aspire AppHost SDK | `Aspire.AppHost.Sdk/13.6.4` | `ch12-s10` | Brings `Aspire.Hosting.AppHost` implicitly (see A.3) |

## A.2 NuGet packages

All `Akka.*` packages share one property, `<AkkaVersion>1.5.73</AkkaVersion>`, so an Akka.NET upgrade is a one-line change (section 21.10).

### Akka.NET

| Package | Version | First tag | Used for |
|---|---|---|---|
| `Akka` | 1.5.73 | `ch02-s05` | Actors, the core runtime |
| `Akka.TestKit.Xunit` | 1.5.73 | `ch02-s05` | The TestKit for xUnit v3 |
| `Akka.Hosting` | 1.5.73 | `ch02-s09` | Actor systems in the Generic Host, `IRequiredActor<T>` |
| `Akka.Persistence.Hosting` | 1.5.73 | `ch10-s01` | Persistent actors, journal and snapshot configuration |
| `Akka.Persistence.TestKit.Xunit` | 1.5.73 | `ch10-s04` | The scriptable test journal and snapshot store |
| `Akka.Persistence.Sql.Hosting` | 1.5.73 | `ch10-s07` | Journal, snapshots and read journal on SQLite and PostgreSQL |
| `Akka.Cluster.Hosting` | 1.5.73 | `ch12-s01` | Cluster, sharding, singletons, DistributedPubSub |
| `Akka.Hosting.TestKit` | 1.5.73 | `ch12-s09` | A real host with DI in actor tests |
| `Akka.Persistence.Query.InMemory` | 1.5.73 | `ch14-s07` | Fast projection tests on the in-memory journal |
| `Akka.Management` | 1.5.73 | `ch21-s06` | Cluster bootstrap on Kubernetes |
| `Akka.Discovery.KubernetesApi` | 1.5.73 | `ch21-s06` | Contact points from the Kubernetes API |
| `Akka.Coordination.KubernetesApi` | 1.5.73 | `ch21-s06` | The lease behind the lease-majority split-brain resolver |

### Hosting, data and Aspire

| Package | Version | First tag | Used for |
|---|---|---|---|
| `Microsoft.Data.Sqlite` | 10.0.14 | `ch10-s07` | The local journal (server) and, from `ch19-s06`, the phone's outbox and cache |
| `Aspire.Hosting.PostgreSQL` | 13.6.4 | `ch12-s10` | PostgreSQL 17 as an AppHost resource |
| `Microsoft.Extensions.ServiceDiscovery` | 10.11.0 | `ch12-s10` | Service discovery in `Ohmline.ServiceDefaults` |
| `Microsoft.Extensions.Http.Resilience` | 10.11.0 | `ch12-s10` | Resilience handlers; TallyPay client from `ch16-s04` |
| `Npgsql` | 10.0.3 | `ch12-s10` | The PostgreSQL driver for the journal, read models and Billing |
| `dbup-postgresql` | 7.0.1 | `ch20-s08` | `Ohmline.Migrator`, which owns every schema |
| `Microsoft.FeatureManagement.AspNetCore` | 4.8.0 | `ch21-s08` | Feature flags, read by the host and never by an actor |
| `MinVer` | 8.0.0 | `ch21-s02` | Versions from `v`-prefixed git tags |

### Telemetry and operations

| Package | Version | First tag | Used for |
|---|---|---|---|
| `OpenTelemetry.Extensions.Hosting` | 1.20.1 | `ch17-s02` | The OpenTelemetry SDK in `Ohmline.ServiceDefaults`; the MAUI head from `ch19-s08` |
| `OpenTelemetry.Exporter.OpenTelemetryProtocol` | 1.20.1 | `ch17-s02` | OTLP export; the MAUI head from `ch19-s08` |
| `OpenTelemetry.Instrumentation.AspNetCore` | 1.20.1 | `ch17-s02` | Server spans for HTTP |
| `OpenTelemetry.Instrumentation.Http` | 1.20.1 | `ch17-s02` | Client spans for `HttpClient`; the MAUI head from `ch19-s08` |
| `OpenTelemetry.Instrumentation.Runtime` | 1.20.1 | `ch17-s02` | Runtime metrics |
| `Petabridge.Cmd.Host`, `.Cluster`, `.Cluster.Sharding` | 1.5.2 | `ch18-s08` | `pbm` commands for a running cluster, bound to loopback |

### Mobile

| Package | Version | First tag | Used for |
|---|---|---|---|
| `CommunityToolkit.Mvvm` | 8.4.2 | `ch19-s04` | View-model source generators (partial properties, commands) |
| `Microsoft.Maui.Controls` | 11.0.0-rc.2.26505.5 | `ch19-s04` | The MAUI head only; the workload's `$(MauiVersion)` |
| `Microsoft.Extensions.Logging.Debug` | 11.0.0-rc.3.26522.148 | `ch19-s04` | The MAUI head only; the template floats `11.0.0-*`, pinned here |
| `Microsoft.AspNetCore.SignalR.Client` | 11.0.0-rc.3.26522.148 | `ch14-s05` | Integration tests from `ch14-s05`; the phone from `ch19-s07` |

### Testing

| Package | Version | First tag | Used for |
|---|---|---|---|
| `xunit.v3` | 4.0.2 | `ch02-s01` | The test framework, on Microsoft.Testing.Platform |
| `Shouldly` | 4.3.0 | `ch02-s01` | Assertions |
| `Microsoft.Extensions.TimeProvider.Testing` | 10.11.0 | `ch03-s01` | `FakeTimeProvider` |
| `CsCheck` | 5.0.1 | `ch06-s08` | Property-based and model-based tests |
| `Microsoft.AspNetCore.Mvc.Testing` | 11.0.0-rc.3.26522.148 | `ch12-s11` | `WebApplicationFactory` for the node |
| `Testcontainers.PostgreSql` | 4.16.0 | `ch12-s11` | PostgreSQL 17 in a container |
| `Testcontainers.XunitV3` | 4.16.0 | `ch12-s11` | Container fixtures for xUnit v3 |
| `Aspire.Hosting.Testing` | 13.6.4 | `ch16-s09` | System tests that start the whole AppHost |
| `OpenTelemetry.Exporter.InMemory` | 1.20.1 | `ch17-s08` | Assertions on whole traces |
| `Microsoft.Extensions.Diagnostics.Testing` | 10.11.0 | `ch18-s03` | `MetricCollector<T>` for metric tests |

## A.3 Implicit and branch-only versions

- **`Aspire.Hosting.AppHost`** is not in `Directory.Packages.props`. `Aspire.AppHost.Sdk/13.6.4` references it implicitly at its own version, and NuGet's [NU1009](https://learn.microsoft.com/en-us/nuget/reference/errors-and-warnings/nu1009) forbids a `PackageVersion` for an implicit reference.
- **Branch `upgrade/akka-1.6`** (tag `ch21-s10`) changes `AkkaVersion` to 1.6.0-beta5 for every `Akka.*` package and adds `Akka.Serialization.V2` 1.6.0-beta5, used only in `learn/`.

## A.4 Tools, images and services

| Tool or image | Version | First tag | Notes |
|---|---|---|---|
| Aspire CLI | 13.6.x | `ch12-s10` | `aspire run --project src/Ohmline.AppHost` |
| PostgreSQL | 17 | `ch12-s10` | Aspire `AddPostgres("postgres").WithImageTag("17")`; Testcontainers `postgres:17-alpine` from `ch12-s11` |
| Container engine | Podman (rootless) or Docker | `ch12-s10` | Verified with rootless Podman and `DOCKER_HOST` set to its socket |
| `otel/opentelemetry-collector-contrib` | 0.142.0 | `ch18-s07` | `deploy/observability/compose.yaml` |
| `grafana/otel-lgtm` | 0.39.0 | `ch18-s07` | Grafana, Prometheus 3 with its OTLP receiver, Tempo and Loki in one image |
| Container base image | `mcr.microsoft.com/dotnet/aspnet:11.0.0-rc.3` | `ch21-s05` | Chosen by SDK container publishing to match the runtime; no Dockerfile |
| `kubectl` (`kustomize`) | any recent | `ch21-s06` | Renders `deploy/k8s/overlays/{dev,prod}`; no cluster needed |
| `actions/checkout` | v7 | `ch21-s04` | GitHub Actions |
| `actions/setup-dotnet` | v6.1.0 | `ch21-s04` | Reads the SDK from `global.json` |
| `googleapis/release-please-action` | v5.0.0 | `ch21-s03` | Release PRs, `CHANGELOG.md` and `vX.Y.Z` tags |
| `docker/login-action` | v4.7.0 | `ch21-s05` | Image pushes to GHCR from CI |

### The mobile toolchain (Chapter 19)

| Item | Version | Notes |
|---|---|---|
| Workload | `maui-android` | Installed into the SDK; the only workload the sample needs |
| Workload set | 11.0.100-rc.3.26524.4 | The workload set that matches the RC3 SDK |
| .NET MAUI | 11.0.0-rc.2.26505.5 | There is no MAUI rc.3; the RC3 SDK ships MAUI rc.2 |
| .NET for Android SDK pack | 37.2.0-rc.2.84 | Android API 37 bindings |
| Android SDK (verification machine) | platform `android-37.0`, build-tools 37.0.0 | Installed with `InstallAndroidDependencies` or Android Studio |
| JDK (verification machine) | Microsoft Build of OpenJDK 21.0.8 | Any full JDK that .NET for Android supports |
| Minimum Android version | `SupportedOSPlatformVersion` 24.0 | The .NET 11 minimum |

The MAUI head was built for `net11.0-android` with zero warnings at every tag from `ch19-s04` on. iOS and Mac Catalyst need a Mac with Xcode and were not built by the book's tooling.

## A.5 Packages deliberately not used

| Package | Why not |
|---|---|
| FluentAssertions 8 | Commercial licence; Shouldly is used instead |
| Verify.* | Its SC021 exemption expires and would break old tags; golden files are hand-rolled (sections 10.3 and 20.5) |
| FsCheck | Its `Gen` clashes with CsCheck's; one property library is enough |
| Microsoft.NET.Test.Sdk, xunit.runner.visualstudio, coverlet.collector | VSTest-era packages, not needed on Microsoft.Testing.Platform |
| OpenTelemetry.Exporter.Prometheus.* | Still beta; OTLP feeds Prometheus 3's OTLP receiver instead |
| Akka.HealthCheck* | Deprecated; Akka.Hosting has health checks built in |
| Akka.Persistence.PostgreSql, .Sqlite, .SqlServer | Deprecated in favour of Akka.Persistence.Sql |
| Akka.Serialization.Hyperion | Not needed; every context has its own System.Text.Json serializer |
| Akka.Aspire, Akka.Aspire.Hosting | Its cluster bootstrap needs Redis or Azure Table discovery; the AppHost uses a static seed (ADR 0010) |
| Aspire.Hosting.Maui, Aspire.Hosting.DevTunnels | Preview, and not verified on the book's machine; `docs/observability.md` explains how to point the app at a collector instead |
| EF Core | Billing uses Npgsql directly |
| Any `Akka.*` package in a mobile project | The domain and the actors stay on the server (section 19.10) |
| sqlite-net-pcl | Reflection-based and risky under full trimming; `Microsoft.Data.Sqlite` keeps the outbox SQL visible |
| CommunityToolkit.Maui | Not needed for the sample's screens |

## A.6 The GA re-pin checklist

The sample is pinned to the .NET 11 RC3 go-live SDK. When .NET 11 reaches general availability, the re-pin is one commit on `main`, after `v1.0.0`. Section tags are never moved: they keep their RC pins so that every listing and Try it box in the book still matches its tag.

1. **Install the GA SDK and update `global.json`** to its exact version. Keep `rollForward: latestFeature`; `allowPrerelease` can become `false` once no pinned package is a prerelease.
2. **Run the learning tests first**: `dotnet test --project learn/Ch03.LanguageLab.Tests`. They pin the C# 15 behaviour the book relies on (unions, closed hierarchies, exhaustiveness diagnostics, union serialization), and a change in the language shows up there before it shows up in production code (section 3.1).
3. **Move the RC-versioned packages** in `Directory.Packages.props` to their GA versions: `Microsoft.AspNetCore.Mvc.Testing`, `Microsoft.AspNetCore.SignalR.Client` and `Microsoft.Extensions.Logging.Debug` (all 11.0.0-rc.3.26522.148 today).
4. **Update the MAUI workload** to the GA workload set, then move `Microsoft.Maui.Controls` to the workload's `$(MauiVersion)`. Build `mobile/Ohmline.Mobile.slnx -warnaserror` without `MSBuildEnableWorkloadResolver=false`.
5. **Review the 10.x pins** that have 11.0 counterparts: `Microsoft.Data.Sqlite` (10.0.14), `Microsoft.Extensions.ServiceDiscovery`, `Microsoft.Extensions.Http.Resilience`, `Microsoft.Extensions.TimeProvider.Testing` and `Microsoft.Extensions.Diagnostics.Testing` (10.11.0). Move them only if a GA release exists and the full test run stays green; `Microsoft.Data.Sqlite` must still build `Ohmline.Mobile.Core` with `IsAotCompatible` and zero warnings.
6. **Regenerate the lock files** (`dotnet restore --force-evaluate` for both solutions), because CI restores in locked mode from `ch21-s04` on.
7. **Re-run the version-sensitive tests**: `AddTracingTests` (section 18.6, the .NET 11 `AddTracing` and `ActivitySourceFactory` APIs), the SignalR client tests (sections 14.5 and 19.7) and the golden-file tests (`Ohmline.ContractTests`), which catch any change in System.Text.Json output.
8. **Rebuild the container images.** The SDK picks the base image that matches the runtime, so `aspnet:11.0.0-rc.3` becomes `aspnet:11.0.x` without a file change (section 21.5). Check that `ContainerTests` still pass.
9. **Run everything**: `dotnet build Ohmline.slnx -warnaserror`, `dotnet test --solution Ohmline.slnx` (including the Integration and System traits), and the mobile build. Commit with a Conventional Commit such as `build(deps): re-pin to .NET 11 GA` so that release-please records it.
10. **Check the Akka.NET side separately.** The re-pin does not change `AkkaVersion`. If Akka.NET 1.6.0 ships as a stable release, branch `upgrade/akka-1.6` is where it is tried first (section 21.10).

# Appendix B. Glossary

## B.1 Ohmline's ubiquitous language

These terms come from the sample's `docs/glossary.md` at the last tag. Each bounded context has its own language; the same word can mean different things in two contexts, and section B.2 lists the words that must always be qualified.

### Used in every context

| Term | Meaning | In code |
|---|---|---|
| Ohmline | The fictional charge point operator whose software this is | `Ohmline.*` |
| Site | A location with one grid connection and therefore one power budget | `SiteId` (`slc-depot`) |
| Charge point (charger) | A physical charging device with one or more connectors | `ChargePointId` (`OHM-SLC-0042`) |
| Connector | A numbered plug on a charge point; hosts at most one session at a time | `ConnectorId` (`OHM-SLC-0042/1`) |
| Id token (token) | An RFID card or app credential that authorizes charging | `TokenUid` |
| Driver | The person charging; never stored by name in events | `DriverRef` (opaque) |
| Power | An instantaneous rate, in kW; never confuse it with energy | `Power` |
| Energy | An amount delivered over time, in kWh | `Energy` |
| Current / voltage | Electrical current in A; electrical potential in V | `Current`, `Voltage` |
| Money | An amount in one currency; amounts in two currencies are never added | `Money`, `Currency` |
| Version (of an aggregate) | The aggregate's sequence number after a command; returned to the caller and stored with read-model rows | `Version`, `last_sequence_nr` |
| Command id | The id the sender gives a command, a version 7 Guid; sending the command again with the same id is safe | `CommandId` |

### Charging

| Term | Meaning | In code |
|---|---|---|
| Charging session (session) | One charge on one connector, from start to end. OCPP 1.6 calls it a "transaction" (see `ConcurrentTx` in the Open Charge Alliance's [*Using ISO 15118 Plug & Charge with OCPP 1.6*](https://openchargealliance.org/wp-content/uploads/2023/11/ocpp_1_6_ISO_15118_v10.pdf)); inside Charging it is always a session | `SessionId`, `ChargingSession` |
| Connector status | Available, Reserved, Occupied, Out of order or Unavailable | `ConnectorState` |
| Reservation | A claim on a connector for one token until it expires; not a payment hold | `ReserveConnector`, `ConnectorReserved`, `ReservationExpired` |
| Occupied | A car is plugged into the connector | `OccupyConnector`, `ConnectorOccupied` |
| Out of order | The charge point reports that a connector cannot be used. Called "faulted" until section 20.7; stored and published names keep the old word | `ReportConnectorOutOfOrder`, `ConnectorOutOfOrder` |
| Repaired | An out-of-order connector works again | `ReportConnectorRepaired`, `ConnectorRepaired` |
| Unavailable | Operators took a working connector out of use | `MakeConnectorUnavailable`, `ConnectorMadeUnavailable` |
| Meter reading | The charge point's cumulative energy register (Wh) with a timestamp; it never decreases | `MeterReading` |
| Meter anomaly | A reading that breaks the rules, such as a register that went backwards; recorded, never rejected | `MeterAnomalyDetected` |
| Energy delivered | Energy between two meter readings, or over a whole session | `Energy` |
| Active (session) | A session that is running and not suspended | `Active` |
| Suspended by EV / by charger | Charging paused while the car stays plugged in: by the car (idle fee may apply) or by the charge point at Ohmline's request (no idle fee). Split in section 8.6 | `SuspendedByEv`, `SuspendedByCharger` |
| Spend cap | The most the driver pre-authorized; reaching it stops charging | `SpendCapReached` |
| Tariff snapshot | The tariff version copied into a session when it starts; later tariff changes never affect it | `TariffSnapshot` |
| Pricing model | How a session's fees apply, chosen by a feature flag at start and recorded in the start event (section 21.8) | `PricingModel` |
| Charging journey | The process from "start charging" to a settled payment, across several contexts | `ChargingJourney`, `JourneyId` |
| Charge point listing | The public description of a charge point drivers see; only the latest revision matters (section 11.7) | `ChargePointListing` |
| Session completed | The integration event other contexts consume after a session is priced | `ohmline.charging.session-completed.v1` |
| Projection lag | The time between an event entering the journal and a read model reflecting it | `GET /projections/status` |

### Site Energy

| Term | Meaning | In code |
|---|---|---|
| Grid connection limit | The maximum power the site may draw from the grid | `Power` |
| Safety margin | Headroom Ohmline keeps below the grid connection limit | `Power` |
| Curtailment | A temporary reduction of the site's capacity, requested by the grid operator | `ApplyCurtailment`, `CurtailmentApplied` |
| Effective capacity | Grid connection limit minus active curtailment minus safety margin | computed in `SitePowerBudget` |
| Power allocation | Power granted to one connector's session, between the connector's minimum and maximum | `PowerAllocation` |
| Priority class | Fleet or Public; fleet requests are served first | `PriorityClass` |
| Fair-share allocation | Sharing the effective capacity among all requests, up to each one's maximum (water-filling) | `FairShareAllocator` |
| Rebalance (allocation) | Recomputing every allocation at a site after a change; not shard rebalancing | `AllocationsRebalanced` |

### Tariffs

| Term | Meaning | In code |
|---|---|---|
| Tariff | Versioned pricing rules for a site: energy rate, time rate, session fee, idle fee, time-of-use bands, price cap | `Tariff`, `TariffComponent` |
| Time-of-use (TOU) band | A rate that applies in a window of site-local time | `TimeOfUseBand` |
| Site-local time | The time on the site's wall clock; derived from an instant, never the other way round | `SiteLocalTime` |
| Idle fee / grace period | A per-minute fee once charging has stopped and the car is still plugged in, after a grace period | `IdleFee` |
| Price breakdown | The line items and total for one session | `PriceBreakdown` |
| Rounding policy | How amounts are rounded, per currency and tariff | `RoundingPolicy` |

### Driver Accounts

| Term | Meaning | In code |
|---|---|---|
| Token lock | An id token held by one running session; a locked token cannot start another | `LockForSession`, `TokenLockedForSession` |
| Blocked token | A token that may not start any session, for example a card reported lost | `BlockToken`, `TokenBlocked` |

### Billing

| Term | Meaning | In code |
|---|---|---|
| Payment | A money movement at TallyPay: a hold, a capture or a void; never called a "transaction" | Billing tables |
| Hold (pre-authorization) | Money reserved on the driver's card before charging; not a connector reservation | `PlaceHold`, `HoldPlaced` |
| Capture | Taking the final amount from a hold; the rest is released | `CapturePayment`, `PaymentCaptured` |
| Void | Releasing a hold without taking any money | `VoidHold` |
| Debt | An amount owed after a capture failed for good, or after the price exceeded the hold | `DebtRecorded` |
| Receipt | The driver-facing record of a settled session | Billing tables |

### Device Gateway

These words belong to the charger protocol, a simplified subset in the style of OCPP 1.6J, and stop at the gateway.

| Term | Meaning | Inside Ohmline it becomes |
|---|---|---|
| Transaction (OCPP) | The charge point's word for a charging session | Charging session |
| StartTransaction / StopTransaction | The charge point reports that a session started or stopped | `StartSession`, `StopSession` |
| Authorize | The charge point asks whether an id token may charge | `LockForSession` |
| Charging profile | A power limit sent to a charge point; `SetChargingProfile` keeps its external name | `SetChargingProfile` |
| Remote start / remote stop | Ohmline asks a charge point to start or stop | `RemoteStart`, `RemoteStop` |
| Heartbeat / boot notification | A liveness ping / the first message after a charge point connects | — |
| `Faulted` (status) | The charge point's word for a connector that cannot be used | `ReportConnectorOutOfOrder` |

### Driver App

| Term | Meaning | In code |
|---|---|---|
| Driver app | Ohmline's .NET MAUI app for drivers; its own bounded context downstream of Charging | `Ohmline.Mobile`, `Ohmline.Mobile.Core` |
| Pending command | A command the driver gave that the server has not yet accepted or rejected | `CommandStatus` cases `Queued`, `Sending` |
| Outbox (the app's) | The phone's durable list of commands to send, written before any network call | `SqliteOutbox` |
| Syncing | Sending pending commands, or catching the cache up after the app was away | `OutboxDispatcher`, `FeedCatchUp` |
| Failed to sync | A pending command that could not be sent within its maximum age of 24 hours; never sent again | `CommandStatus` case `FailedToSync` |
| Offline | The phone has no usable connection; a hint that makes the app wait, never proof | `IConnectivity.IsOnline`, `ScreenState` case `Offline` |
| Read-model cache | The session cards the server last sent, kept on the phone; disposable | `SqliteReadModelCache` |

### External systems

| Term | Meaning | In the sample |
|---|---|---|
| TallyPay | The fictional payment provider (pre-authorize, capture, void) | `tools/Ohmline.FakePsp` |
| Northgate Grid | The fictional grid operator; publishes demand-response signals | `tools/Ohmline.FakeGridOperator` |
| Demand-response signal | Northgate Grid's request to reduce a site's draw; Ohmline turns it into a curtailment | The grid operator ACL |
| AssetTrack | The fictional legacy asset registry; a messy CSV export | `tools/legacy-assets/` |

## B.2 Words with more than one meaning

| Word | Meanings | Rule |
|---|---|---|
| session | A charging session; an HTTP session | A bare "session" is always a charging session |
| transaction | OCPP's name for a session; a payment; a database transaction | Say "OCPP transaction", "payment" or "database transaction" |
| hold / reservation | A payment hold (Billing); a connector reservation (Charging) | "Hold" is money, "reservation" is a connector |
| actor | An Akka.NET actor; a role in a domain story | Say "story actor" in domain stories |
| rebalance | Allocation rebalancing (Site Energy); shard rebalancing (Akka.NET) | Always qualify |
| event | Domain event; integration event; EventStorming sticky; .NET event; Northgate Grid's signal | Qualify whenever two meanings are in play |
| version | Aggregate version; manifest version (`.v2`); tariff version; app or release version | Always qualify |
| capacity | The grid connection limit; the effective capacity | A bare "capacity" is the effective capacity |
| pending | A pending command (Driver App); a pending write in a persist cycle | In the app, "pending" is a command the server has not answered |
| outbox | The journal as outbox (16.1); Billing's transactional outbox (16.3); the app's command outbox (19.6) | Always qualify |
| mailbox | An Akka.NET actor's mailbox; the phone's single-reader `Channel` (19.6) | Akka.NET never runs on the phone |
| projection | A read-model projection; a LINQ projection | Means a read-model projection |
| repository | A DDD repository; a git repository | Say "sample repo" for git |

## B.3 Domain-Driven Design terms

| Term | Definition | First in |
|---|---|---|
| Domain | The sphere of knowledge and activity the software serves: for Ohmline, public EV charging | 1.1 |
| Subdomain (core, supporting, generic) | A part of the domain classified by its business value: core differentiates, supporting is needed but not distinctive, generic is bought | 4.7 |
| Ubiquitous language | The language shared by experts and developers within one bounded context, used in speech, documents and code | 1.3 |
| Model-Driven Design | Keeping the code and the domain model in one-to-one correspondence, so a change to one is a change to the other | 1.3 |
| Refactoring toward deeper insight | Changing the model when the team learns something about the domain, such as splitting "suspended" in section 8.6 | 1.3 |
| Domain Storytelling | A workshop in which domain experts tell concrete stories with pictographs of story actors, work objects and activities | 4.1 |
| EventStorming | A workshop that models a domain as a timeline of domain events on stickies, at Big Picture, Process and Design levels | 4.2 |
| Bounded context | The boundary within which one model and its language are consistent | 5.1 |
| Bounded Context Canvas | The [ddd-crew template (V5)](https://github.com/ddd-crew/bounded-context-canvas) that describes a context's purpose, roles, language and communication | 5.1 |
| Context map | The relationships between bounded contexts and between the teams that own them | 5.4 |
| Partnership | Two contexts whose teams succeed or fail together and coordinate changes | 5.4 |
| Customer/Supplier | An upstream context that plans its changes around a downstream customer's needs | 5.4 |
| Conformist | A downstream context that adopts the upstream model as it is | 5.4 |
| Shared Kernel | A small, explicitly shared part of the model, changed only by agreement | 5.5 |
| Open Host Service (OHS) | A protocol a context offers to all consumers, instead of one integration per consumer | 5.5 |
| Published Language | A documented, versioned language for integration, such as Ohmline's `ohmline.*.v1` events | 5.5 |
| Anticorruption Layer (ACL) | A translating layer that keeps a foreign model from leaking into yours | 5.6 |
| Separate Ways | Deciding not to integrate two contexts at all | 5.6 |
| Big Ball of Mud | A system without a coherent model; quarantined behind an ACL rather than rewritten | 5.6 |
| Distillation | Separating the core domain from everything else so effort goes where it pays; includes Domain Vision Statement, Highlighted Core, Cohesive Mechanism, Segregated Core and Abstract Core | 5.8 |
| Cohesive Mechanism | A well-defined algorithm extracted from the model and called by it, such as the fair-share allocator | 5.8 |
| Large-scale structure | A system-wide pattern of responsibility, such as Responsibility Layers or a Knowledge Level | 5.9 |
| Knowledge Level | A part of the model that describes how another part behaves, edited as data: Ohmline's tariffs | 8.2 |
| Value object | An immutable, self-validating value defined only by its attributes: `Power`, `Money`, `MeterReading` | 6.1 |
| Smart constructor | A factory that returns either a valid value or an error, never an invalid value | 6.2 |
| Entity | An object defined by its identity over time, such as a connector within a charge point | 6.4 |
| Aggregate | A consistency boundary: one decider and the shell that runs it, changed by one command at a time | 7.1 |
| Invariant | A rule that must hold after every command, inside one aggregate | 7.1 |
| Aggregate Design Canvas | The [ddd-crew template](https://github.com/ddd-crew/aggregate-design-canvas) for an aggregate's invariants, commands, events and size | 7.1 |
| Decider | An aggregate written as three pure parts: an initial state, `Decide` (state and command to events or an error) and `Evolve` (state and event to state) | 1.6, 7.6 |
| Decision table | `Decide` written as one exhaustive switch expression over state and command | 7.4 |
| Fold | Rebuilding state by applying `Evolve` to every event in order, as recovery does | 7.5 |
| Domain event | A fact domain experts care about, named in the past tense and stored forever | 8.1 |
| Integration event | A fact published to other contexts in the Published Language, versioned separately from domain events | 16.1 |
| Domain service | A stateless operation of the domain that belongs to no entity, such as the price calculator | 8.3 |
| Specification | A predicate over domain objects that has a name in the ubiquitous language | 8.4 |
| Policy | A reaction "whenever X, then Y", written as a pure function from an event to commands | 8.5 |
| Supple design | [Evans's patterns](https://www.informit.com/store/domain-driven-design-tackling-complexity-in-the-heart-9780321125217) (*Domain-Driven Design*, ch. 10 "Supple Design") for code that stays safe to change: Intention-Revealing Interfaces, Side-Effect-Free Functions, Assertions, Conceptual Contours, Standalone Classes, Closure of Operations, Declarative Design, Draw on Established Formalisms, When You Can | 8.8 |
| Factory | Code that creates a valid aggregate or object; in Ohmline, the decider's first arm and Props factories | 7.4, 12.3 |
| Repository | The illusion of an in-memory collection of aggregates, in the words of Evans's [*Domain-Driven Design Reference*](https://domainlanguage.com/wp-content/uploads/2016/05/DDD_Reference_2015-03.pdf) ("Repositories", p. 17); in Ohmline, the shard region | 12.2 |
| Application service | A thin layer that receives a request, sends a command to an aggregate and translates the reply | 12.5 |
| Module | A named group of model elements; in the sample, folders and projects | 5.2 |
| Process manager | A stateful coordinator that reacts to events from several sources and decides the next command: the charging journey | 15.1 |
| Saga | A long-running sequence of local steps with compensating actions instead of a distributed transaction | 15.1 |
| Event sourcing | Storing an aggregate as the sequence of its events and rebuilding its state by folding them | 10.4 |
| CQRS | Command Query Responsibility Segregation: separate models for writing and reading | 13.1 |
| Read model / projection | A query-shaped view built from events, disposable and rebuildable | 13.2 |
| Eventual consistency | A read model reflects a write after a delay, not at once | 14.1 |
| Read-your-writes | A read that waits until the read model has reached the version a command returned | 14.1 |
| Idempotency | A receiver that handles the same message twice with the effect of handling it once | 12.6 |
| Upcasting | Translating a stored event from an old schema to the current one while reading it | 20.3 |
| Tolerant reader | A reader that uses only the fields it needs and ignores the rest | 20.2 |
| Expand/contract | Changing a schema in compatible steps: add the new shape, migrate readers, remove the old | 20.2 |

## B.4 Akka.NET terms

| Term | Definition | First in |
|---|---|---|
| Actor | State and behaviour behind a mailbox, processing one message at a time | 2.2 |
| Mailbox | The queue in front of an actor; the only way in | 2.2 |
| `IActorRef` | An address of one actor incarnation; not an identity | 2.5 |
| `Props` | The recipe for creating an actor | 2.5 |
| Tell / Ask | Send without waiting / send and await one reply with a timeout | 2.3 |
| `Become` / `Stash` | Replace an actor's behaviour / set messages aside until the behaviour changes | 2.6 |
| Timers | Scheduled messages owned by an actor (`IWithTimers`), cancelled when it stops | 2.7 |
| Supervision | A parent's decision to resume, restart or stop a failing child; `BackoffSupervisor` restarts with growing delays | 2.8 |
| `PipeTo` | Turning a task's result into a message to an actor, instead of awaiting inside a handler | 2.8 |
| Akka.Hosting | Configuring the actor system in the .NET Generic Host; `IRequiredActor<T>` resolves actors from DI | 2.9 |
| Coordinated shutdown | Ordered phases that stop an actor system and leave a cluster cleanly | 2.9 |
| Persistent actor | An actor whose state is rebuilt from its journal; `ReceivePersistentActor` in Akka.NET | 10.4 |
| Persistence id | The key of one persistent actor's events: `{shardTypeName}-{entityId}`, such as `charge-point-OHM-SLC-0042` | 10.4 |
| Sequence number | The position of an event in its persistence id's stream; the aggregate version | 10.8 |
| Journal | The append-only store of events, here Akka.Persistence.Sql on SQLite and PostgreSQL | 10.7 |
| `Persist`, `PersistAll`, `PersistAsync`, `DeferAsync` | Ways to write events; deciders use `PersistAll`, which stashes new commands until the write completes | 10.6 |
| Internal stash | Where a persistent actor keeps commands that arrive while a write is in flight | 10.5 |
| Serializer, serializer id, manifest | The code that turns messages into bytes, its permanent number (9101 for Charging) and the permanent name of each type (`charging.connector-reserved.v1`) | 10.3 |
| Recovery | Replaying a snapshot and events through `Evolve` before the first command | 11.1 |
| Snapshot / snapshot store | A saved state that shortens recovery; a cache, never the truth | 11.2 |
| Journal circuit breaker | Akka.Persistence's breaker that fails writes fast while the journal is unreachable | 11.3 |
| Poison event | A stored event that cannot be read or applied, which stops recovery | 11.3 |
| Passivation | Stopping an idle sharded entity safely through its shard, to be recovered on the next message | 11.5, 12.4 |
| Durable State | The [JVM Akka feature](https://doc.akka.io/libraries/akka-core/current/typed/durable-state/persistence.html) that stores only the latest state; Akka.NET 1.5.73 has no Durable State store | 11.7 |
| State sourcing | Ohmline's closest equivalent: a journal whose events are whole state revisions, used for the charge point listing | 11.7 |
| Cluster, role, seed node | Nodes forming one actor system; roles (`charging`, `gateway`) select where actors run; seeds are the first contact points | 12.1 |
| Split-brain resolver (SBR) | The strategy that downs one side of a network partition so that each entity keeps one writer | 12.1 |
| Cluster sharding | Distributing entities across nodes by id; one live instance of each entity in the cluster | 12.2 |
| Shard region, shard, entity | The local gateway to a sharded type / a group of entities moved together / one aggregate instance | 12.2 |
| Message extractor | The function that reads an entity id and shard id from a message | 12.2 |
| `maxNumberOfShards` | The fixed number of shards per type (100 in Ohmline); never changed in production | 12.2 |
| Remember-entities | Restarting entities after a rebalance or crash without a message; on only for journeys | 12.4 |
| Shard rebalancing | Moving shards between nodes; not allocation rebalancing | 12.4 |
| Cluster singleton | Exactly one instance of an actor in the cluster, such as `tariff-book` | 12.8 |
| Event adapter, tagger | A write adapter that tags events for queries; a read adapter that translates old events | 13.2, 20.4 |
| Persistence Query, read journal | The read side of the journal: `EventsByTag`, `EventsByPersistenceId` and their `Current*` variants | 13.3 |
| Offset | A position in an event stream that a projection stores to resume after a restart | 13.4 |
| Sharded daemon process | A fixed number of long-running workers kept alive across the cluster: Ohmline's `projections` | 13.6 |
| DistributedPubSub | Topic-based publish and subscribe across a cluster: `session-summaries`, `driver-app-cards` | 14.4 |
| Reliable delivery (Akka.Delivery) | Producer and consumer controllers with acknowledgements; considered and declined in ADR 0011 | 15.6 |
| Akka.Management, discovery, lease | Cluster bootstrap over HTTP, contact points from Kubernetes, and a lease for the lease-majority SBR | 21.6 |
| `AppVersion` | The application version a node advertises to the cluster; MinVer's full version is not one Akka.NET can parse | 21.2 |
| TestKit, test probe | Akka.NET's test harness and a stand-in actor that records messages | 2.5 |
| Phobos, Petabridge.Cmd | Petabridge's commercial [tracing for Akka.NET](https://phobos.petabridge.com/); its [command-line tool](https://cmd.petabridge.com/) for running clusters (`pbm`) | 17.9, 18.8 |

## B.5 C#, .NET and operations terms

| Term | Definition | First in |
|---|---|---|
| Union (C# 15) | A type whose value is exactly one of a closed set of existing types, checked for exhaustiveness in switch expressions | 3.3 |
| Closed hierarchy (C# 15) | A `closed` base record whose derived types are all known to the compiler | 3.4 |
| `field` keyword | Access to a property's compiler-generated backing field, used for validation in `init` | 3.2 |
| Static abstract members | Interface members implemented by the type itself, behind `IValueObject<TSelf,TValue>` and `IDecider` | 3.6 |
| Extension members | C# 14 extension blocks (properties, operators, static members) and C# 15 extension indexers | 3.7 |
| `TimeProvider` / `FakeTimeProvider` | The .NET clock abstraction / its controllable test double | 3.8 |
| Guid v7 | A time-ordered Guid from `Guid.CreateVersion7` | 3.8 |
| Microsoft.Testing.Platform (MTP) | The test runner `dotnet test` uses with xUnit v3 | 1.7 |
| Golden file, golden corpus | A committed JSON file that a serialization test compares against; every manifest version ever written, kept forever | 10.3, 20.5 |
| `ActivitySource`, `Meter` | The BCL APIs for spans and metrics, used by the domain-near code; the OpenTelemetry SDK lives only in hosts | 17.1 |
| W3C trace context | The `traceparent` format the trace envelope carries through mailboxes and the journal | 17.4 |
| OTLP | The OpenTelemetry protocol used to export traces, metrics and logs | 17.2 |
| Server-Sent Events (SSE) | A one-way HTTP stream of events from server to browser | 14.4 |
| SignalR | ASP.NET Core's real-time messaging, used for live updates to app clients | 14.5 |
| SLI / SLO / error budget | A measured indicator, its target, and the failures the target allows | 18.9 |
| MinVer | A package that sets versions from `v`-prefixed git tags | 21.2 |
| Conventional Commits, release-please | A commit-message format, and the tool that turns it into release PRs, changelogs and tags | 21.3 |
| Rolling update / blue-green | Replacing nodes one at a time in a running cluster / switching traffic between two complete deployments | 21.7, 21.8 |

# Appendix C. Tag Index and Anti-Pattern Catalogue

## C.1 Tag index

This index is generated from `git tag -n --sort=version:refname` in the sample. Each tag is the state of the sample at the end of its section; the "What it adds" column comes from the sample's `README.md`. There are 174 section tags: 173 on `main` and `ch21-s10` on branch `upgrade/akka-1.6`. The release commit of section 21.9 also carries the version tag `v1.0.0`, the only tag MinVer reads.

### Part I: First Principles

#### Chapter 1. Taming Complexity: Why Domain-Driven Design Meets the Actor Model

| Tag | Section | What it adds |
|---|---|---|
| `ch01-s02` | 1.2 Meet Ohmline: one charge, told end to end | This README, the licence, git settings, `docs/ohmline-primer.md` |
| `ch01-s03` | 1.3 Models, language and the people who know the business | `docs/glossary.md`, the ADR template, ADR 0000 |
| `ch01-s04` | 1.4 Consistency without distributed transactions | ADR 0001: one writer per aggregate |
| `ch01-s06` | 1.6 Functional core, actor shell | ADR 0002, a first-draft `docs/vision.md` |
| `ch01-s07` | 1.7 Setting up the workbench | `global.json`, `Directory.*.props`, `NuGet.config`, `.editorconfig`, an empty `Ohmline.slnx`, `tools/verify-tags.sh` |

No tag (prose only): 1.1 Essential versus accidental complexity; 1.5 Actors as the runtime for consistency boundaries.

#### Chapter 2. Actors from First Principles

| Tag | Section | What it adds |
|---|---|---|
| `ch02-s01` | 2.1 Shared mutable state: double-booking a connector | `learn/`, `RacyConnector`, `LockedConnector`, the first xUnit v3 tests |
| `ch02-s02` | 2.2 A mailbox in sixty lines | `Mailbox<TMessage>` on a single-reader `Channel`, `ConnectorActor` |
| `ch02-s03` | 2.3 Tell, don't ask: reply addresses and futures | `ReplyTo<TReply>`, `ICanTell<TMessage>`, `AskAsync` with a mandatory timeout |
| `ch02-s04` | 2.4 Let it crash: supervision and addresses by hand | `Supervisor`, `ActorDirectory<TActor>` |
| `ch02-s05` | 2.5 Hello, Akka.NET | Akka.NET 1.5.73, `Ch02.AkkaBasics.Tests`, `AkkaConnectorActor`, the TestKit |
| `ch02-s06` | 2.6 Behaviour as state: Become and Stash | `ChargerTwin` with `Become` and `IWithStash` |
| `ch02-s07` | 2.7 Time inside actors | The `ChargerTwin` heartbeat watchdog on `IWithTimers` |
| `ch02-s08` | 2.8 Supervision, backoff and the async question | `FirmwareLookupActor` (`PipeTo`), `ReceiveAsync` contrast, `BackoffSupervisor` |
| `ch02-s09` | 2.9 Actors in the Generic Host | `Ch02.Hosted`: Akka.Hosting, `IRequiredActor<T>`, coordinated shutdown |

No tag (prose only): 2.10 What actors do not give you: a model.

#### Chapter 3. The C# 15 Modeling Toolkit

| Tag | Section | What it adds |
|---|---|---|
| `ch03-s01` | 3.1 Learning tests: pinning down a language | `Ch03.LanguageLab.Tests`, record equality and with-expressions |
| `ch03-s02` | 3.2 Records, with, and validation with the field keyword | `Kilowatts` with a `field`-validated `init` |
| `ch03-s03` | 3.3 Union types: closed sets of existing types | `ConnectorStatus` union; CS8509 and CS8655 as build errors |
| `ch03-s04` | 3.4 Closed hierarchies and the union-versus-closed decision | `Ch03.ClosedLib`, `ChargerCommand` closed hierarchy, ADR 0003 |
| `ch03-s05` | 3.5 Errors as values: a Result union | `Ohmline.SharedKernel` with `Result<T, E>` and `Unit`; `tests/` |
| `ch03-s06` | 3.6 Static abstract members and generic math | `IValueObject<TSelf, TValue>`, `ValidationError`, operator interfaces |
| `ch03-s07` | 3.7 Extension members and extension indexers | Extension blocks on `decimal`, `Kilowatts` and a meter history |
| `ch03-s08` | 3.8 Patterns that read like rules; time and identity | List and property patterns, labeled `break`, `FakeTimeProvider`, `GuidV7` |
| `ch03-s09` | 3.9 Serialization reality check: unions and closed records on the wire and in the mailbox | System.Text.Json and Akka.NET tests, `docs/house-style.md` |

### Part II: Strategic Design

#### Chapter 4. Discovering the Domain: Storytelling and EventStorming

| Tag | Section | What it adds |
|---|---|---|
| `ch04-s01` | 4.1 Domain Storytelling with Ohmline's people | Three domain stories in `docs/domain-stories/` |
| `ch04-s02` | 4.2 Big Picture EventStorming | `docs/eventstorming/big-picture.md`: timeline, pivotal events, hotspots |
| `ch04-s03` | 4.3 Process-Level EventStorming | Process boards for starting charging and settling a session |
| `ch04-s04` | 4.4 Design-Level EventStorming and the sticky-to-code map | Aggregates and the sticky-to-code map in `docs/eventstorming/design-level.md` |
| `ch04-s05` | 4.5 Ubiquitous language: glossary, naming rules and durable vocabulary | The glossary per context, `docs/naming-rules.md` |
| `ch04-s06` | 4.6 From stickies to types: the vocabulary compiles | `Ohmline.Charging.Contracts` and `.Domain` with empty `ChargePointCommand` and `ChargePointEvent` hierarchies |
| `ch04-s07` | 4.7 Subdomains: core, supporting, generic | `docs/subdomains.md` |
| `ch04-s08` | 4.8 Choosing a business-logic pattern per subdomain | ADR 0004: an implementation style per subdomain |

#### Chapter 5. Bounded Contexts, Context Maps and Distillation

| Tag | Section | What it adds |
|---|---|---|
| `ch05-s01` | 5.1 Bounded contexts and the Bounded Context Canvas | A Bounded Context Canvas per context in `docs/canvases/` |
| `ch05-s02` | 5.2 Modules made physical: architecture styles and the solution layout | Empty projects for every context, Billing, the Device Gateway and `Ohmline.ActorShell` |
| `ch05-s03` | 5.3 Keeping the core pure, enforced | `build/DomainPurity.targets`, `tests/Ohmline.ArchitectureTests` |
| `ch05-s04` | 5.4 Context mapping I: teams and flows | `docs/context-map.md`: teams, the map, every relationship |
| `ch05-s05` | 5.5 Context mapping II: Shared Kernel, Open Host Service and Published Language | Shared kernel governance, `.github/CODEOWNERS`, the Published Language rules |
| `ch05-s06` | 5.6 Context mapping III: Anticorruption Layer, Separate Ways and Big Ball of Mud | The AssetTrack export and its translation rules in `docs/acl/` |
| `ch05-s07` | 5.7 The executable context map and deployment topology | An artifact per relationship, ADR 0005, `ContextBoundaryTests` |
| `ch05-s08` | 5.8 Distillation | The final `docs/vision.md`, `docs/highlighted-core.md` |
| `ch05-s09` | 5.9 Large-scale structure | ADR 0006: responsibility layers and supervision |

### Part III: The Functional Core

#### Chapter 6. Value Objects and Entities

| Tag | Section | What it adds |
|---|---|---|
| `ch06-s01` | 6.1 Primitive obsession in charging: kW versus kWh versus A | `Energy`, `Power`, `Current`, `Voltage` and the `Dimensions` operators |
| `ch06-s02` | 6.2 Smart constructors that return unions | `union ValidationError(OutOfRange, Malformed, Missing)`; `TryCreate` on every unit |
| `ch06-s03` | 6.3 Money, currency and rounding | `Currency`, `Money`, `CurrencyMismatch`, `RoundingPolicy` |
| `ch06-s04` | 6.4 Identity: strongly typed identifiers | `SiteId`, `ChargePointId`, `ConnectorId`, `SessionId`, `JourneyId`, `TokenUid` |
| `ch06-s05` | 6.5 Meter readings: monotonic registers and time | `MeterReading`, `MeterError`, `MeterHistory`; `Ohmline.Charging.Domain.Tests` |
| `ch06-s06` | 6.6 Entities inside an aggregate: a connector is a record, not an actor | `Connector` and `ConnectorComparer.ById` |
| `ch06-s07` | 6.7 Value objects at the boundary: parsing a Big Ball of Mud | `SiteConfiguration`, the AssetTrack anticorruption layer and its tests |
| `ch06-s08` | 6.8 Property-based tests for value objects | CsCheck 5.0.1, generators and property tests for units and money |

#### Chapter 7. Aggregates and the Decider

| Tag | Section | What it adds |
|---|---|---|
| `ch07-s01` | 7.1 Finding the true invariants: the Aggregate Design Canvas | An Aggregate Design Canvas for each of the four aggregates in `docs/canvases/` |
| `ch07-s02` | 7.2 Commands and events as closed hierarchies | The charge point's commands and events with their data; `PersistedShapeTests` |
| `ch07-s03` | 7.3 State as a union: making illegal states unrepresentable | `union ConnectorState(...)`, `union ChargePointState(Unregistered, Registered)` |
| `ch07-s04` | 7.4 Decide: business rules as an exhaustive decision table | `DecisionContext`, `ChargePoint.Decide`, `union ChargePointError(...)` |
| `ch07-s05` | 7.5 Evolve: total, deterministic, never failing | `ChargePoint.Evolve` and `ChargePoint.Fold` |
| `ch07-s06` | 7.6 The Decider abstraction | `IDecider<TState, TCommand, TEvent, TError>`, `Decider.Fold` |
| `ch07-s07` | 7.7 The ChargingSession aggregate | Session commands, events, `union SessionState(...)` and the `ChargingSession` decider |
| `ch07-s08` | 7.8 The SitePowerBudget aggregate and the fair-share allocator | `FairShareAllocator`, `union AllocationDecision(...)` and the `SitePowerBudget` decider |

No tag (prose only): 7.9 Vernon's four rules in Ohmline (the rules are from Vaughn Vernon's [*Effective Aggregate Design*](https://www.dddcommunity.org/library/vernon_2011/), Parts I and II).

#### Chapter 8. Domain Events, Services, Policies and Supple Design

| Tag | Section | What it adds |
|---|---|---|
| `ch08-s01` | 8.1 Domain events as facts experts care about | `CommandId` on every command; `CommandId` and `OccurredAt` stamped on every event |
| `ch08-s02` | 8.2 Tariffs as a Knowledge Level | `Tariff`, `TariffComponent`, `TimeOfUseBand`; `TariffSnapshot` locked into `SessionStarted` |
| `ch08-s03` | 8.3 Domain services: the price calculator | `PriceCalculator`, `SessionUsage`, `PriceBreakdown` |
| `ch08-s04` | 8.4 Specifications: as classes and as patterns | `ISpecification<T>` with `And`/`Or`/`Not`; `IdleFeeApplies`, `RequiresPreAuthorization` |
| `ch08-s05` | 8.5 Policies: 'whenever X then Y' as pure reactions | `ISessionPolicy`, `ReleaseOnSessionEnded`, `SpendCapPolicy`, `IdleFeePolicy`, `ChargingIntent` |
| `ch08-s06` | 8.6 Refactoring toward deeper insight: who suspended the charge? | `SuspendedByEv` and `SuspendedByCharger` in place of `Suspended` |
| `ch08-s07` | 8.7 Time in the domain: site-local time, time-of-use and DST | `SiteLocalTime`; pricing across Denver's daylight-saving changes |

No tag (prose only): 8.8 The supple design checklist.

#### Chapter 9. Unit Testing the Domain

| Tag | Section | What it adds |
|---|---|---|
| `ch09-s01` | 9.1 The test pyramid for a functional core and an actor shell | `docs/testing-strategy.md`: the layers, naming and what not to test |
| `ch09-s02` | 9.2 A Given-When-Then harness for deciders | `tests/Ohmline.Testing` with `DeciderSpec`; the decider tests rewritten on it |
| `ch09-s03` | 9.3 Specification by example: scenario tables in the ubiquitous language | Reservation and session-stop tables, `pricing-examples.csv`; a reservation fix |
| `ch09-s04` | 9.4 Model-based property tests for invariants | `DeciderProperties`, `AllocationsFit`, property tests for the three deciders |
| `ch09-s05` | 9.5 Testing time | `ReservationExpiryTests`, `IdleFeeGraceTests`, `TouBoundaryTests` on a fake clock |
| `ch09-s06` | 9.6 Testing translators and policies; DDD test smells | Table tests for the AssetTrack translator and the policies, `tests/README.md` |

### Part IV: The Actor Shell

#### Chapter 10. The Aggregate as a Persistent Actor: Commands, Events and the Journal

| Tag | Section | What it adds |
|---|---|---|
| `ch10-s01` | 10.1 What the shell owns; a composition root with Akka.Hosting | `src/Ohmline.Node` (`/alive`, `TimeProvider.System`), `AddChargingContext()` |
| `ch10-s02` | 10.2 Protocol: commands in, replies out, and the union trap | `CommandAccepted`/`CommandRejected`, `GetChargePointStatus`, `ErrorMapping`, `SendExtensions.Send` |
| `ch10-s03` | 10.3 Serialization before the first persisted event | `ManifestJsonSerializer`, `ChargingSerializer` (9101), `Golden`, `tests/golden/`, ADR 0007 |
| `ch10-s04` | 10.4 ReceivePersistentActor: Decide, PersistAll, Evolve, reply | `ChargePointActor`, `ChargePointDirectory`, `ActorTestConfig`, the first shell tests |
| `ch10-s05` | 10.5 While the write is in flight: the internal stash | `InFlightWriteTests` with a held journal write |
| `ch10-s06` | 10.6 Persist, PersistAsync, PersistAll and DeferAsync | `learn/Ch10.PersistenceLab.Tests` |
| `ch10-s07` | 10.7 Real storage: Akka.Persistence.Sql on SQLite | `WithSqlPersistence` on SQLite in the node, `NodeRestartTests` |
| `ch10-s08` | 10.8 A guided tour of the journal | `JournalTourTests`, `JournalTourQueries`, `docs/journal-tour.md` |
| `ch10-s09` | 10.9 One writer per persistence id: sequence numbers instead of locks | `TwoIncarnationsTests`; "Two writers" in `docs/journal-tour.md` |
| `ch10-s10` | 10.10 A reusable DeciderActor and the other aggregates | `DeciderActor`, `ChargingSessionActor`, `SitePowerBudgetActor`, `SiteEnergySerializer` (9102) |

#### Chapter 11. Recovery, Snapshots, Durable State and Failure

| Tag | Section | What it adds |
|---|---|---|
| `ch11-s01` | 11.1 Recovery in depth | The recovery INFO line in `DeciderActor`, `RecoveryTests`, `JournalProbe` |
| `ch11-s02` | 11.2 Snapshots: a cache with a cost model | Snapshot DTOs and manifests for the three aggregates, `SnapshotEvery`, the snapshot row in `docs/journal-tour.md` |
| `ch11-s03` | 11.3 Recovery at scale: throttling, storms and the poison event | `max-concurrent-recoveries` in the node, `OnRecoveryFailure`, `PoisonEventTests`, `docs/runbooks/poison-event.md` |
| `ch11-s04` | 11.4 Timers derived from state: reservation expiry | Reservation timers in `ChargePointActor`, `ReservationExpiryTests` on virtual time |
| `ch11-s06` | 11.6 Failure semantics: errors versus failures | `OnPersistFailure`/`OnPersistRejected`, `persistence-rejected`, a backoff supervisor, ADR 0008 |
| `ch11-s07` | 11.7 Durable State: the non-event-sourced alternative | `ChargePointListing`, `StateSourcedDeciderActor`, `docs/persistence-choices.md` |
| `ch11-s08` | 11.8 Testing the shell with Akka.Persistence.TestKit | `PersistenceShellSpec`; the actor tests rewritten on it; `SnapshotFailureTests` |

No tag (prose only): 11.5 Passivation and rehydration: an entity's life.

#### Chapter 12. Cluster Sharding: The Shard Region as Repository

| Tag | Section | What it adds |
|---|---|---|
| `ch12-s01` | 12.1 From one node to a cluster | Remoting and clustering in the node (`WithOhmlineCluster`), the `charging` role, keep-majority set explicitly |
| `ch12-s02` | 12.2 The shard region is the repository | `EntityMessageExtractor` (100 shards), `EntityShardOptions`, a region per aggregate, ADR 0009 |
| `ch12-s03` | 12.3 Factories: domain versus actor | Entities made by the container (`resolver.Props<T>`), the clock injected |
| `ch12-s04` | 12.4 Passivation and remember-entities | The `Stop` hand-off message, idle passivation, an ended session passivates itself |
| `ch12-s05` | 12.5 Application services at the HTTP edge | `Ohmline.Charging.Api` and `Ohmline.SiteEnergy.Api` endpoints, validated options, development scaffolding |
| `ch12-s06` | 12.6 Idempotency keys and read-your-writes | `RecentCommands` in `DeciderActor` and every snapshot, the `Idempotency-Key` header |
| `ch12-s07` | 12.7 Uniqueness across aggregates: the IdToken aggregate | Driver Accounts: `IdToken`, `DriverAccountsSerializer` (9103), the `id-token` region, `/tokens` |
| `ch12-s08` | 12.8 The Knowledge Level in the cluster: the tariff book singleton | `TariffBook`, `TariffsSerializer` (9104), the `tariff-book` singleton and its proxy, `/tariffs` |
| `ch12-s09` | 12.9 Testing sharding locally with Akka.Hosting.TestKit | `Ohmline.Node.Tests`: the node's wiring on a cluster of one |
| `ch12-s10` | 12.10 Postgres and a multi-node cluster on your laptop with Aspire | `Ohmline.AppHost` (PostgreSQL 17, three nodes), `Ohmline.ServiceDefaults`, ADR 0010 |
| `ch12-s11` | 12.11 Integration tests against real Postgres | `Ohmline.IntegrationTests`: `WebApplicationFactory`, Testcontainers, state that survives a restart |

### Part V: CQRS, Processes and Integration

#### Chapter 13. CQRS I: From Journal to Read Model

| Tag | Section | What it adds |
|---|---|---|
| `ch13-s02` | 13.2 Tagging events | `ChargingTagger`, `SiteEnergyTagger`: one tag per event; the tags rows in the journal tour |
| `ch13-s03` | 13.3 Persistence Query: the read side of the journal | `ReadJournalAccess`; `Ohmline.Charging.ReadModels.Tests` with `ReadJournalTourTests` |
| `ch13-s04` | 13.4 A resumable projection runner: offsets and at-least-once | `ProjectionRunner`, `NpgsqlOffsetStore`, `ReadModelSchema`; branch `wrong/ch13-s04-offset-before-handler` |
| `ch13-s05` | 13.5 Idempotent read models: upserts and the offset in one transaction | `SessionHistoryProjection`, the pure `SessionHistoryRow.Apply`, the offset in the row's transaction |
| `ch13-s06` | 13.6 Running exactly one projector | The sharded daemon process `projections` (`ProjectionDaemon`, `AddChargingReadModels()`) |
| `ch13-s07` | 13.7 When a projection fails: restarts, poison events and lag | `ProjectionFailurePolicy`, `read_models.projection_failures`, `GET /projections/status`, a runbook |
| `ch13-s08` | 13.8 Read models people use: a site live board and a query API | `SiteLiveBoardProjection`, `ISiteAllocationEvent`, `GET /sites/{id}/live-board`, `GET /tokens/{uid}/sessions` |

No tag (prose only): 13.1 Why the write model cannot answer your questions.

#### Chapter 14. CQRS II: Living with Eventual Consistency

| Tag | Section | What it adds |
|---|---|---|
| `ch14-s01` | 14.1 Read-your-writes with the version a command returns | `GET /sessions/{id}?minVersion=`, `ReadYourWrites`, `Ohmline:ReadModels:ReadYourWritesTimeout`; session commands' `Location` asks for their version |
| `ch14-s02` | 14.2 Consistency choices per screen | `docs/consistency-per-screen.md` |
| `ch14-s03` | 14.3 Tuning and measuring projection lag | `Ohmline:Projections:RefreshInterval` and `QueryDelay` (100 ms), `last_event_at` and lag in `GET /projections/status` |
| `ch14-s04` | 14.4 Live push with Server-Sent Events | `LiveUpdate`/`SessionSummaryChanged` published after the commit on DistributedPubSub, `GET /sessions/{id}/events` |
| `ch14-s05` | 14.5 Live push to app clients with SignalR | `ChargingHub` at `/hubs/charging`, `LiveUpdatesBridge` |
| `ch14-s06` | 14.6 Many read models from one stream, including one for the driver app | `DriverAppProjection` (`driver-app-v1`), the driver app cards and feed, `GET /app/v1/tokens/{uid}/cards` and `/feed`, `driverCardChanged` |
| `ch14-s07` | 14.7 Testing projections | `ProjectionRulesTests` and `InMemoryRunnerTests` on Akka.Persistence.Query.InMemory, `ReplayTests` |

No tag (prose only): 14.8 Rebuilding a read model: a preview.

#### Chapter 15. Process Managers and Sagas

| Tag | Section | What it adds |
|---|---|---|
| `ch15-s01` | 15.1 Saga or process manager? Choreography first | `SessionEndedSubscriber` (`session-ended-subscriber-v1`): a session's end releases its allocation and unlocks its token; `CommandId.Derive` |
| `ch15-s02` | 15.2 The charging journey as a pure state machine | `ChargingJourney.Next` over the `JourneyInput` union, `JourneyState`, `JourneyEffect`, the journey's events; Site Energy's `PowerAllocated` and `QueuedForPower` |
| `ch15-s03` | 15.3 The journey actor: persistent, sharded, with timers | `ChargingJourneyActor` in the `charging-journey` region (remember-entities on), journey manifests and tag, `POST /journeys`, `GET /journeys/{id}` |
| `ch15-s04` | 15.4 Ports to other contexts | `IPaymentPort`, `IChargerCommandPort`, `InMemoryPaymentPort` and `LoggingChargerCommandPort` in Development, `JourneySessionRelay` (`journey-session-relay-v1`), fake contexts with autopilots |
| `ch15-s05` | 15.5 Timeouts and compensation | The energy-flow timeout gives everything back; capture retries with backoff, then `DebtRecorded` |
| `ch15-s06` | 15.6 Delivery guarantees and idempotency keys | `IdempotencyKey` on every request, reply timers, idempotent ports and receivers, ADR 0011 |
| `ch15-s07` | 15.7 Testing time-driven processes | `JourneyTransitionTableTests`, `ChargingJourneyActorTests` on the `TestScheduler` with autopilots |

No tag (prose only): 15.8 Hot and huge aggregates.

#### Chapter 16. Integrating Bounded Contexts: Anticorruption Layers, Open Host and the Outbox

| Tag | Section | What it adds |
|---|---|---|
| `ch16-s01` | 16.1 Domain events versus integration events: the journal is the outbox | `ChargingIntegrationEvent`/`SessionCompleted` (`ohmline.charging.session-completed.v1`), `IntegrationEventRelay` (`integration-relay-v1`) into `read_models.integration_outbox`; branch `wrong/ch16-s01-dual-write` |
| `ch16-s02` | 16.2 Open Host Service: a notification feed | `GET /feeds/charging?after=&limit=` with next link and ETag, `docs/feeds.md` |
| `ch16-s03` | 16.3 Billing, deliberately simple | `Ohmline.Billing`: hold endpoints, `ChargingFeedConsumer` with an inbox position, invoices and receipts with a transactional outbox; `Ohmline.Billing.Contracts` |
| `ch16-s04` | 16.4 The payment provider ACL | `tools/Ohmline.FakePsp` (TallyPay, `FLAKY_RATE`), `TallyPayClient` with its own resilience handler, `TallyPayTranslator` to `PaymentOutcome` |
| `ch16-s05` | 16.5 Charging to Billing: Customer/Supplier | `BillingPaymentPort` behind the journey's payment port; the AppHost adds Billing and the TallyPay stand-in |
| `ch16-s06` | 16.6 The Device Gateway: OCPP frames to domain commands | `OcppFrameParser`, the `OcppFrame` and `Translation` unions, `OcppTranslator`, recorded frames from two vendors |
| `ch16-s07` | 16.7 Connection actors and the charger simulator | `ChargerConnectionActor` at `/ocpp/{chargePointId}`, shard-region proxies, `GatewayChargerCommandPort` on `charger-commands-{id}` topics, `tools/Ohmline.Simulator`; development scaffolding deleted |
| `ch16-s08` | 16.8 The grid operator ACL and a projection-fed dispatcher | `tools/Ohmline.FakeGridOperator`, `GridSignalTranslator` and the `grid-signal-poller` singleton, `ChargingProfileDispatcher` (`charging-profile-dispatcher-v1`) |
| `ch16-s09` | 16.9 System tests with Aspire | `Ohmline.SystemTests`: the AppHost under Aspire.Hosting.Testing, plug in, charge, get a receipt; launch profiles for Billing and the gateway |
| `ch16-s10` | 16.10 Contract tests for the Published Language | `Ohmline.ContractTests`: golden JSON in `tests/golden/published-language/` and `tests/golden/billing/`, Billing's reader over Charging's feed |

### Part VI: Instrumentation and Telemetry

#### Chapter 17. Tracing Through the Mailbox

| Tag | Section | What it adds |
|---|---|---|
| `ch17-s01` | 17.1 Signals in .NET and observability as a port | `ChargingDiagnostics` and one diagnostics class per context (`Ohmline.{Context}` ActivitySource and meter name, `CreateMeter` over `IMeterFactory`), `ActorShellDiagnostics` |
| `ch17-s02` | 17.2 Wiring OpenTelemetry into ServiceDefaults and the Aspire dashboard | OpenTelemetry 1.20.1 in `Ohmline.ServiceDefaults` only: `Ohmline.*` sources and meters, ASP.NET Core, HttpClient and runtime instrumentation, `service.version`, `deployment.environment.name`, `UseOtlpExporter` when `OTEL_EXPORTER_OTLP_ENDPOINT` is set |
| `ch17-s03` | 17.3 Why traces stop at the mailbox | `tests/Ohmline.Telemetry.Tests`, `SpanRecorder`, `MailboxLosesTraceTests` |
| `ch17-s04` | 17.4 A W3C trace envelope | `TraceEnvelope`, `TraceEnvelopeSerializer` (9100, `ohmline.trace-envelope.v1`), `TellTraced`/`AskTraced`; the shard extractor looks through envelopes; the edge, the ports and the projections send traced |
| `ch17-s05` | 17.5 Consumer spans in AroundReceive | `TracedPersistentActor` and `TracedReceiveActor`: `process {ActorType}` CONSUMER spans; `DeciderActor`, `ChargingJourneyActor` and `ChargerConnectionActor` adopt them |
| `ch17-s06` | 17.6 Stash, persist and the double count | `stash` spans and `OnMessageHandled` once per handled message; `PersistTraced`/`PersistAllTraced` with `persist`, `apply` and `persisted` spans |
| `ch17-s07` | 17.7 Parents versus links | Traced events stored in envelopes and tagged by `TracedEventTagger`; projection, OCPP frame and journey timer spans linked to their origin; `messaging.message.conversation_id` |
| `ch17-s08` | 17.8 Testing traces | OpenTelemetry.Exporter.InMemory; `TraceShapeTests` on the node as it runs: HTTP to actor to journal in one trace, a projection's link, a journey's Tell |
| `ch17-s09` | 17.9 Buy versus build: Phobos | ADR 0012: manual tracing, its known gaps, and the criteria for buying Phobos |

#### Chapter 18. Metrics, Logs, Health Checks and Dashboards

| Tag | Section | What it adds |
|---|---|---|
| `ch18-s01` | 18.1 Domain metrics with a cardinality budget | `ChargingMetrics` (commands, refusals by error case, sessions started, kWh delivered) and `SiteEnergyMetrics` (fullest site's allocation), on the host's `IMeterFactory`; `DeciderActor.OnDecided`; branch `wrong/ch18-s01-high-cardinality-tags` |
| `ch18-s02` | 18.2 Actor and cluster metrics | `Ohmline.ActorShell/Telemetry`: `ActorShellMetrics` extension, `DeadLetterMetricsListener`, `LifecycleMetricsListener`, `ShardStatsPoller`, recovery time in `DeciderActor`; `WithShellMetrics()`, `WithShardStats()` |
| `ch18-s03` | 18.3 Testing metrics | Microsoft.Extensions.Diagnostics.Testing; `MetricTests` and `NodeMetricTests` with `MetricCollector<T>` |
| `ch18-s04` | 18.4 Logs that correlate | `AddAkkaTraceCorrelation()` in the node and the gateway; `ChargingLog` and `GatewayLog` (`[LoggerMessage]`); `LogCorrelationTests` |
| `ch18-s05` | 18.5 Health checks that tell the truth | `/health/live` and `/health/ready` in every environment; `HealthTags`; SQL connectivity checks under `ready`; `HealthTests` stops PostgreSQL |
| `ch18-s06` | 18.6 .NET 11 AddTracing and ActivitySourceFactory | ADR 0013 and `AddTracingTests`: what the factory and its rules do beside the OpenTelemetry SDK |
| `ch18-s07` | 18.7 Production-shaped telemetry locally | `deploy/observability`: Collector 0.142.0, grafana/otel-lgtm 0.39.0, the *Ohmline: Charging* dashboard; `docs/observability.md` |
| `ch18-s08` | 18.8 Petabridge.Cmd, bound safely | Petabridge.Cmd 1.5.2 on `127.0.0.1` in the node and the gateway; `OperatorCommandsTests`; `docs/runbooks/operator-commands.md` |
| `ch18-s09` | 18.9 SLOs from the domain | `ohmline.charging.journey.time_to_energy_flow` from the journey's events; `deploy/observability/alerts/ohmline-slo.yaml`; SLO panels |

### Part VII: The Mobile Edge

#### Chapter 19. The Mobile Edge: An Offline-First .NET MAUI Driver App

| Tag | Section | What it adds |
|---|---|---|
| `ch19-s01` | 19.1 Where the domain lives: the driver app as its own bounded context | The Driver App on the context map; `docs/canvases/bc-driver-app.md`; ADR 0014; the app's words in the glossary |
| `ch19-s02` | 19.2 A Published Language for client apps | `Ohmline.Charging.Client.Contracts` (references nothing): requests with a `CommandId`, `CommandAcceptedDto`, `RejectionCodes`, read-model and notification DTOs, `ChargingClientJsonContext`; the `/app/v1` commands and listing; golden files in `tests/golden/client` |
| `ch19-s03` | 19.3 Commands you can send twice | The journey answers its own start again and refuses another command's (`journey-exists`); the body's `CommandId` and a matching `Idempotency-Key`; `docs/idempotency-contract.md` |
| `ch19-s04` | 19.4 A plain core library and a thin MAUI head | `src/Mobile/Ohmline.Mobile.Core` and its tests in `Ohmline.slnx`; the .NET MAUI head and `mobile/Ohmline.Mobile.slnx`; `verify-tags.sh` builds it for Android |
| `ch19-s05` | 19.5 The ubiquitous language on the phone: value objects and unions | `ConnectorCode`, `SpendCap`, `ParseResult<T>`, the `ScreenState` and `CommandStatus` unions, `RejectionText`; the start-charging page |
| `ch19-s06` | 19.6 Offline first: a durable outbox drained by a mailbox | `SqliteOutbox`, `OutboxDispatcher` (a single-reader `Channel`), `SendOutcome`, `HttpCommandSender`; the window lifecycle in the head |
| `ch19-s07` | 19.7 Read models on the device: a cache, live updates and optimistic UI | `SqliteReadModelCache`, `FeedCatchUp`, `LiveUpdatesClient`, `SessionCardViewModel` with its pending overlay; the session page |
| `ch19-s08` | 19.8 One trace from tap to journal | `MobileDiagnostics` (`Ohmline.Mobile`); the tap's traceparent in the outbox; the projection's in `DriverCardChanged`; OpenTelemetry in the head; `TapToJournalTraceTests` |

No tag (prose only): 19.9 Persistence and CQRS: the server versus the phone; 19.10 Should the phone run actors? Local-first as the exception.

### Part VIII: Operations, Releases and Versioning

#### Chapter 20. Events Are Forever: Evolving Schemas, Snapshots and Read Models

| Tag | Section | What it adds |
|---|---|---|
| `ch20-s01` | 20.1 What is really in your journal, and the contracts that never die | `tools/Ohmline.JournalInspect` (PostgreSQL or SQLite), `docs/contracts-that-never-die.md` |
| `ch20-s02` | 20.2 Tolerant readers and expand/contract | An optional `PhaseCount` on `SessionStarted`, the old shape frozen in `tests/golden/corpus/`, ADR 0015 |
| `ch20-s03` | 20.3 Upcasting inside the serializer | `MeterValueRecorded` v2 (kWh, phase), an `Upcaster` from v1's Wh in `ChargingSerializer` |
| `ch20-s04` | 20.4 Upcasting with read event adapters | `LegacyEventsReadAdapter` (one event split into two, one dropped); branch `alt/ch20-s04-adapter-only` |
| `ch20-s05` | 20.5 The golden corpus | `tests/golden/corpus/` for every manifest ever written, `CorpusTests` |
| `ch20-s06` | 20.6 Snapshots are a cache | `ChargePointSnapshot` v2, `snapshot-is-optional` on, `DeciderActor`'s whole-history check |
| `ch20-s07` | 20.7 Renaming a concept when events are forever | "Out of order" instead of "fault": `ConnectorOutOfOrder` under a new manifest, the old one an alias |
| `ch20-s08` | 20.8 Owning the journal schema: migrations | `src/Ohmline.Migrator` (DbUp), least-privilege roles, `WaitForCompletion(migrator)` |
| `ch20-s09` | 20.9 Rebuilding read models and versioning the Published Language | `session-history-v2` beside v1, `SessionHistoryVersion`, `SessionCompletedV2` beside v1 |
| `ch20-s10` | 20.10 Retention, privacy and backups | `docs/runbooks/retention.md`, `docs/runbooks/backup-restore.md`, `PersonalDataTests` |

#### Chapter 21. Versioning, Releasing and Running the Cluster

| Tag | Section | What it adds |
|---|---|---|
| `ch21-s01` | 21.1 SemVer at three layers | ADR 0016; the published contracts packable with package validation |
| `ch21-s02` | 21.2 Versions from git with MinVer, and the AppVersion trap | MinVer 8.0.0, `OhmlineAppVersion` (MAJOR.MINOR.PATCH for the cluster); branch `wrong/ch21-s02-minver-appversion` |
| `ch21-s03` | 21.3 Conventional Commits and release-please | `release-please.yml`, `release-please-config.json`, `.release-please-manifest.json` (0.9.0), `docs/contributing.md` |
| `ch21-s04` | 21.4 CI that builds every tag | `packages.lock.json` everywhere, `.github/workflows/ci.yml` (build, tests, Android, audit, `verify-tags.sh`) |
| `ch21-s05` | 21.5 Container images from the SDK | `build/Containers.targets`: `ohmline/*` images, MinVer tags, user 1654; ghcr.io on release |
| `ch21-s06` | 21.6 Kubernetes topology for an Akka cluster | `ClusterFormation`, Akka.Management bootstrap and a lease-majority SBR on Kubernetes, `deploy/k8s` |
| `ch21-s07` | 21.7 Anatomy of a rolling update | `ShutdownBudget`, the gateway's `ConnectionDrain` with reconnect hints, `docs/runbooks/rolling-update.md`, `reconnect-storm` |
| `ch21-s08` | 21.8 Feature flags without breaking replay; blue/green done right | `DecisionContext.Features`, `SessionStarted.PricingModel`, ADR 0017, `docs/runbooks/blue-green.md` |
| `ch21-s09` | 21.9 Release day, end to end | `feat(charging): idle fee cap per tariff`, `CHANGELOG.md`, tag `v1.0.0`, the prod overlay at 1.0.0 |
| `ch21-s10` | 21.10 Upgrading the platform: Akka.NET 1.5 to 1.6 | On branch `upgrade/akka-1.6` only: Akka.NET 1.6.0-beta5, pinned serializer bindings, `docs/upgrade-akka-1.6.md` |

`ch21-s10` is on branch `upgrade/akka-1.6`, not on `main`.

## C.2 Side branches

Each `wrong/` branch forks from the named tag, adds one commit and builds. A test whose name starts with `Demonstrates_` shows the failure, either by failing or by asserting the bad behaviour. No side branch is ever merged.

| Branch | Forks from | What it shows | Test |
|---|---|---|---|
| `wrong/ch10-s02-union-on-the-wire` | `ch10-s02` | A union told as a reply arrives with `Value == null` | `Demonstrates_an_error_union_replied_as_it_is_arrives_empty` |
| `wrong/ch10-s03-default-serializer` | `ch10-s03` | The default serializer stores type names, and a rename makes events unreadable | `Demonstrates_renaming_the_event_type_makes_stored_events_unreadable` |
| `wrong/ch10-s06-persist-async-decider` | `ch10-s06` | `PersistAllAsync` lets two drivers occupy one connector | `Demonstrates_two_drivers_both_occupy_one_connector` |
| `wrong/ch10-s10-logic-in-receive` | `ch10-s10` | A rule in a `Receive` handler forgets that reservations lapse | `Demonstrates_a_lapsed_reservation_still_turns_the_next_driver_away` |
| `wrong/ch12-s02-actorref-as-identity` | `ch12-s02` | A remembered `IActorRef` is dead after its entity stops | `Demonstrates_the_remembered_site_actor_is_gone_when_the_session_ends` |
| `wrong/ch12-s04-context-stop` | `ch12-s04` | `Context.Stop(Self)` loses the reply and the next command | `Demonstrates_the_session_stops_before_it_answers_and_loses_the_next_command` |
| `wrong/ch13-s04-offset-before-handler` | `ch13-s04` | Saving the offset first loses an event for good | `Demonstrates_a_crash_after_the_offset_save_loses_the_event_for_good` |
| `wrong/ch16-s01-dual-write` | `ch16-s01` | Publishing beside the offset write publishes twice after a crash | `Demonstrates_a_crash_after_publishing_publishes_the_event_twice` |
| `wrong/ch18-s01-high-cardinality-tags` | `ch18-s01` | Entity ids as metric tags make one series per charge point | `Demonstrates_entity_ids_as_tags_make_a_series_per_charge_point` |
| `wrong/ch21-s02-minver-appversion` | `ch21-s02` | MinVer's whole version is not an `AppVersion` Akka.NET can parse | `Demonstrates_minver_s_whole_version_is_not_an_app_version_akka_can_parse` |
| `alt/ch20-s04-adapter-only` | `ch20-s04` | The meter reading's upcast done only by a read event adapter | — |
| `upgrade/akka-1.6` | `ch21-s09` | Akka.NET 1.6.0-beta5 (tag `ch21-s10`) | — |

## C.3 Anti-pattern catalogue

Each entry names the mistake as the chapters' **Anti-pattern** callouts do, says what goes wrong, gives the cure and points to where the book shows it.

### Anemic actor

An actor whose protocol is a setter, such as `SetConnectorStatus(Status)`, has no rules: the sender decided, and the actor stores what it is told. It is CRUD with a mailbox. **Cure:** messages are intentions (`OccupyConnector`) that a decider accepts or refuses where the state lives. **See:** sections 1.5, 4.5, 7.9 and 10.10.

### Logic in Receive handlers

The mirror image of the anemic actor: rules written inside `Receive<T>` lambdas, between `Persist`, `Sender.Tell` and logging, where only an actor system can test them and the next handler forgets a check. **Cure:** a decider with one decision table, run by `DeciderActor`, whose `Command<TCommand>` registration comes first so a subclass cannot add a competing handler. **See:** sections 7.9 and 10.10; branch `wrong/ch10-s10-logic-in-receive`.

### One actor per database row

Entities and value objects inside an aggregate made into actors. An actor per connector turns the charge point's rules into a conversation of asks, and two commands interleave between them. **Cure:** the connector is a record inside the charge point's state, and the rule is a pattern match. **See:** sections 1.5, 6.6 and 7.9.

### Ask chains

An actor that asks another aggregate and awaits the answer during its own turn stalls its mailbox; its own askers time out in turn, and under load the chain collapses into timeouts. **Cure:** tell between actors, keep asks at the edges with a timeout, and coordinate with a process manager. **See:** sections 1.5, 2.3 and Chapter 15.

### Mutable messages

A record that carries a `List<T>` shares the list between sender and receiver across threads. **Cure:** message fields are strings, numbers, other records and immutable collections. **See:** section 2.2.

### A god supervisor

A supervisor that inspects failures and decides what the business does next is a decider hiding in the operations layer, testable only with an actor system. **Cure:** business reactions go to a decider or the journey; supervision strategies stay short (ADR 0006). **See:** section 5.9.

### Persisting or telling a union wrapper

A C# 15 union told, asked, replied with or persisted arrives in the wrong handler, or empty after serialization. **Cure:** send `.Value`, a closed-record case, through the typed `Send` extension; persist the closed-record events `Decide` returns. **See:** sections 3.9 and 10.2; branch `wrong/ch10-s02-union-on-the-wire`.

### The default Newtonsoft serializer in the journal

It works on the first day and fails on the first rename, because each stored event carries its assembly-qualified type name. **Cure:** one `ManifestJsonSerializer` per context with permanent manifests and serializer ids, in place before the first persisted event. **See:** section 10.3; branch `wrong/ch10-s03-default-serializer`.

### `PersistAsync` in a decider shell

Commands are no longer stashed while a write is in flight, so the second command is decided against stale state and two drivers can both be told yes for one connector. **Cure:** `PersistAll`. **See:** section 10.6; branch `wrong/ch10-s06-persist-async-decider`.

### A non-deterministic `Evolve`

An `Evolve` that validates, throws on an unexpected pair or reads the clock turns a replay into a different history, or into a recovery failure. **Cure:** every decision, including time, is made in `Decide` and recorded in the event; `Evolve` is total and pure. **See:** sections 7.5 and 11.1.

### Editing journal rows to fix a poison event

Projections have read the row and their offsets point past it, a deleted row leaves an unexplained gap, and an edit changes history without an event to say so. **Cure:** a correcting command through the decider once the aggregate recovers. **See:** section 11.3.

### `Context.Stop(Self)` or PoisonPill in sharded or persistent actors

`PoisonPill` does not wait behind a write in flight, so stashed commands are lost; `Context.Stop(Self)` leaves the shard believing the entity is alive. **Cure:** passivation through the shard with a custom stop message. **See:** sections 11.5 and 12.4; branch `wrong/ch12-s04-context-stop`.

### Using the in-memory journal, which hides serialization bugs

The test journal stores objects, not bytes, so an event the serializer cannot write passes every in-memory test. **Cure:** `akka.actor.serialize-messages = on` in every actor test through `ActorTestConfig`, plus SQLite-backed tests with real rows. **See:** sections 10.4 and 11.8.

### Changing `maxNumberOfShards`, PersistenceId formats or serializer IDs

A changed persistence id recovers every aggregate as new, with its history still in the journal and nobody reading it. A new shard count moves ids while old nodes still run. A serializer id is in every journal row. **Cure:** treat them as durable names that never change (`docs/contracts-that-never-die.md`). **See:** sections 4.5, 12.2 and 20.7.

### `IActorRef` used as identity

An `IActorRef` names one incarnation on one node; after passivation, a shard rebalance or a restart, whatever is told to it becomes a dead letter. **Cure:** store the id and send to the shard region. **See:** section 12.2; branch `wrong/ch12-s02-actorref-as-identity`.

### Saving a projection offset before the read-model write

A crash between the two loses the event for the read model, and nothing logs its absence. **Cure:** write the read model and the offset in one database transaction. **See:** section 13.4; branch `wrong/ch13-s04-offset-before-handler`.

### Dual writes

Publishing to a broker outside the transaction that stores the offset publishes twice after a crash, or not at all in the other order. **Cure:** the journal is the outbox, read by a projection (section 16.1), or a transactional outbox table (section 16.3). **See:** section 16.1; branch `wrong/ch16-s01-dual-write`.

### High-cardinality metric tags

An entity id as a metric tag makes one time series per charge point, session or listing, kept until retention expires. **Cure:** a cardinality budget with bounded tag values; ids belong in traces and logs. **See:** section 18.1; branch `wrong/ch18-s01-high-cardinality-tags`.

### Feature flags evaluated during replay

A flag read in `Evolve`, a recovery handler or a projection makes history depend on today's configuration. **Cure:** read the flag in the shell, pass it to `Decide` and record the outcome in the event. **See:** section 21.8.

# Appendix D. DDD Patterns Mapped to Akka.NET

This table is the reference version of the book's central mapping: which DDD pattern becomes which pure type or Akka.NET mechanism in Ohmline, and where the book builds it.

## D.1 Strategic patterns

| DDD pattern | In Ohmline | Mechanism | Sections |
|---|---|---|---|
| Bounded context | Charging, Site Energy, Tariffs, Driver Accounts, Billing, Device Gateway, Driver App | A project set per context (`Contracts`, `Domain`, `Actors`, `Api`), its own serializer id, manifest prefix and event tags; cluster roles `charging` and `gateway` | 5.1, 5.2, 5.7, 19.1 |
| Context map | `docs/context-map.md` | Project references checked by `Ohmline.ArchitectureTests` and `build/DomainPurity.targets` | 5.3–5.7 |
| Shared Kernel | `Ohmline.SharedKernel`: units, `Money`, identifiers, `Result` | A BCL-only library; the phone deliberately does not share it | 5.5, 6.1–6.4, 19.2 |
| Open Host Service | The notification feed; the `/app/v1` client API | ASP.NET Core endpoints over the read side | 5.5, 16.2, 19.2 |
| Published Language | `ohmline.charging.session-completed.v1`; `Ohmline.Charging.Client.Contracts` | Versioned JSON contracts, golden files and contract tests; a source-generated `JsonSerializerContext` for clients | 5.5, 16.1, 16.10, 19.2, 20.9 |
| Customer/Supplier | Charging upstream of Billing | A feed consumer with an inbox in Billing | 5.4, 16.3, 16.5 |
| Partnership | Charging and the Device Gateway | The gateway joins the cluster in role `gateway` and talks to shard region proxies | 5.4, 16.7 |
| Conformist | The roaming hub (documentation only); the journey conforms to upstream contracts | — | 5.4, 15.2 |
| Anticorruption Layer | TallyPay, Northgate Grid, AssetTrack, the charger protocol | Pure translators; `ChargerConnectionActor` per connection; `GridSignalPoller` as cluster singleton `grid-signal-poller` | 5.6, 6.7, 16.4, 16.6–16.8 |
| Big Ball of Mud | The AssetTrack CSV export | A pure parser into value objects, `AssetTrackTranslator` | 5.6, 6.7 |
| Cohesive Mechanism, Highlighted Core | `FairShareAllocator` | A pure function in `Ohmline.SiteEnergy.Domain`, called by the `SitePowerBudget` decider | 5.8, 7.8 |
| Knowledge Level | Tariffs | `TariffBookActor`, a persistent cluster singleton named `tariff-book` | 8.2, 12.8 |

## D.2 Tactical patterns

| DDD pattern | In Ohmline | Mechanism | Sections |
|---|---|---|---|
| Value object | `Power`, `Energy`, `Money`, `MeterReading`, `SiteId` | `readonly record struct` or `sealed record` with `TryCreate`; JSON converters in `Ohmline.ActorShell` | 6.1–6.5 |
| Entity inside an aggregate | A connector | A record in the charge point's state, not an actor | 6.4, 6.6 |
| Aggregate | `ChargePoint`, `ChargingSession`, `SitePowerBudget`, `IdToken` | A pure decider run by a `DeciderActor` subclass, one persistent sharded entity per id | 7.1–7.8, 10.4, 10.10 |
| Invariant and consistency boundary | One session per connector | Single writer: one mailbox per entity, one live entity per id through sharding and the split-brain resolver | 1.4, 10.9, 12.1 |
| Aggregate identity | `ChargePointId` | Entity id from `EntityMessageExtractor`; persistence id `charge-point-OHM-SLC-0042`; never an `IActorRef` | 12.2 |
| Command | `ReserveConnector` | A `closed record` sent to the shard region; the reply carries the aggregate version | 7.2, 10.2 |
| Domain event | `ConnectorReserved` | A `closed record` persisted with `PersistAll` through the context's manifest serializer | 8.1, 10.3, 10.4 |
| Repository | — | The shard region: sending is loading, recovery is rehydration, the journal is saving | 12.2 |
| Factory | The decider's first arm; Props factories | `Decide` from `Initial`; `Props` built with DI | 7.4, 12.3 |
| Domain service | `PriceCalculator` | A pure function, called by deciders and the journey | 8.3 |
| Specification | Session specifications | Classes and C# property patterns | 8.4 |
| Policy | `ReleaseOnSessionEnded` | A pure function from event to commands, run by `SessionEndedSubscriber` on the read side | 8.5, 15.1 |
| Application service | HTTP endpoints | Minimal API endpoints that ask the region with a timeout and translate replies to HTTP | 12.5, 12.6 |
| Uniqueness across aggregates | `IdToken` | A reservation aggregate keyed by the unique value, sharded as `id-token` | 12.7 |
| Module | Folders and projects | Namespaces follow folders; layering enforced in the build | 5.2 |

## D.3 Architectural and integration patterns

| Pattern | In Ohmline | Mechanism | Sections |
|---|---|---|---|
| Event sourcing | Every aggregate | Akka.Persistence journal on Akka.Persistence.Sql (SQLite, then PostgreSQL) | 10.4–10.9, 11.1–11.3 |
| Snapshots | Aggregates run by `DeciderActor`, such as the charge point | `SaveSnapshot` with versioned snapshot DTOs (`charging.charge-point-snapshot.v2`); an unreadable snapshot falls back to replay (`OptionalSnapshots`) | 11.2, 20.6 |
| State-stored alternative | `ChargePointListing`; Billing | `StateSourcedDeciderActor` on the same journal; Billing on plain Npgsql tables | 11.7, 16.3 |
| Process manager | `ChargingJourney` | A pure state machine over a union of inputs, in a persistent sharded actor with timers and remember-entities | 15.2–15.5 |
| Saga and compensation | Voiding a hold when charging never starts | Compensating commands sent by the journey | 15.1, 15.5 |
| CQRS read model | Session history, site live board, driver app cards | Persistence Query `EventsByTag`, `ProjectionRunner`, offsets in the read model's transaction, a sharded daemon process named `projections` | 13.2–13.8, 14.6 |
| Read-your-writes | `?minVersion=` | Queries wait until the read model reaches the version the command returned | 14.1 |
| Live push | Browser and app updates | Server-Sent Events and SignalR, fed through DistributedPubSub topics | 14.4, 14.5 |
| Domain event to integration event | `session-completed.v1` | The journal as outbox: `IntegrationEventRelay`, a projection | 16.1 |
| Transactional outbox | Billing | Outbox tables and an `OutboxDispatcher` on Npgsql | 16.3 |
| Idempotent receiver | Commands, projections, Billing's inbox | `RecentCommands` window of command ids in the shell; upserts; Billing's inbox table | 12.6, 13.5, 15.6, 16.3 |
| Guaranteed delivery | Journey to other contexts | At-least-once with idempotency keys; Akka.Delivery declined (ADR 0011) | 15.6 |
| Upcasting | Renamed and reshaped events | An `Upcaster` inside the manifest serializer; `LegacyEventsReadAdapter` as a read event adapter | 20.3, 20.4, 20.7 |
| Client command outbox | The driver app | A SQLite outbox drained by a single-reader `Channel`, not Akka.NET | 19.6 |
| Tracing across messages | One charge as one trace | `TraceEnvelope` with serializer id 9100; consumer spans in `AroundReceive` | 17.4–17.7 |

## D.4 Where the mapping is awkward

| Problem | Why actors struggle | What Ohmline does | Section |
|---|---|---|---|
| Uniqueness across aggregates | No actor sees the whole set | A reservation aggregate keyed by the unique value | 12.7 |
| Hot and huge aggregates | One mailbox is a throughput ceiling; long streams slow recovery | Split by time or bucket, snapshots, relaxed invariants with corrective policies | 15.8 |
| Ad hoc queries | Asking thousands of actors is slow and couples readers to the write model | CQRS read models | 13.1 |
| One cluster for several contexts | Every actor is reachable from every other, and deploys couple the contexts | ADR 0005 puts four contexts in one cluster with roles and keeps Billing outside it | 5.7 |
| No repository to mock | Persistence is inside the actor | Decider tests for rules, TestKit for the shell, no fake repository | 9.6 |

# Appendix E. Further Reading

This is a short, annotated reading list. The complete bibliography, with every source the book cites and the sections that cite it, is References (`book/98-references.md`): Part 1 indexes the citations chapter by chapter and Part 2 lists each source once, with full publication details. Every entry below also appears there. Primary sources come first in each group.

## E.1 Domain-Driven Design

- Eric Evans, [*Domain-Driven Design: Tackling Complexity in the Heart of Software*](https://www.informit.com/store/domain-driven-design-tackling-complexity-in-the-heart-9780321125217) (Addison-Wesley, 2003); the author's resources are at [domainlanguage.com](https://www.domainlanguage.com/ddd/). The original catalogue of tactical patterns (Part II, "The Building Blocks of a Model-Driven Design") and strategic patterns (Part IV, "Strategic Design").
- Eric Evans, [*Domain-Driven Design Reference*](https://domainlanguage.com/wp-content/uploads/2016/05/DDD_Reference_2015-03.pdf) (2015). Short definitions of every pattern, including those named after 2003.
- Vaughn Vernon, [*Implementing Domain-Driven Design*](https://www.informit.com/store/implementing-domain-driven-design-9780321834577) (Addison-Wesley, 2013). Context maps, aggregates, domain events and integration in practice: chapters 3 "Context Maps", 10 "Aggregates", 8 "Domain Events" and 13 "Integrating Bounded Contexts".
- Vaughn Vernon, [*Effective Aggregate Design*](https://www.dddcommunity.org/library/vernon_2011/), Parts I to III (2011). The four rules of aggregate design, set out in Parts I and II.
- Vlad Khononov, [*Learning Domain-Driven Design*](https://www.oreilly.com/library/view/learning-domain-driven-design/9781098100124/) (O'Reilly, 2021). Subdomain heuristics (ch. 1 "Analyzing Business Domains"), business-logic patterns (chs. 5–7) and integration patterns (ch. 4 "Integrating Bounded Contexts", ch. 9 "Communication Patterns").
- Scott Wlaschin, [*Domain Modeling Made Functional*](https://pragprog.com/titles/swdddf/domain-modeling-made-functional/) (Pragmatic Bookshelf, 2018). Choice types, constrained types and workflows as pure functions: the chapters "Understanding Types", "Integrity and Consistency in the Domain" and "Modeling Workflows as Pipelines".
- Martin Fowler, [*Analysis Patterns: Reusable Object Models*](https://www.informit.com/store/analysis-patterns-reusable-object-models-9780201895421) (Addison-Wesley, 1997); author's page at [martinfowler.com](https://martinfowler.com/books/ap.html). The origin of the knowledge and operational levels (ch. 2 "Accountability", section "Accountability Knowledge Level").
- Martin Fowler, [*Patterns of Enterprise Application Architecture*](https://www.informit.com/store/patterns-of-enterprise-application-architecture-9780321127426) (Addison-Wesley, 2002); author's page at [martinfowler.com](https://martinfowler.com/books/eaa.html). Transaction Script and Domain Model (ch. 9 "Domain Logic Patterns") and Active Record (ch. 10 "Data Source Architectural Patterns").
- Martin Fowler, [*Money*](https://martinfowler.com/eaaCatalog/money.html) (2002). The pattern behind section 6.3.
- Eric Evans and Martin Fowler, [*Specifications*](https://martinfowler.com/apsupp/spec.pdf) ([PLoP '97](https://www.hillside.net/plop/plop97/abstracts/evans.txt)). Specifications, including composites.
- Ward Cunningham, [*The CHECKS Pattern Language of Information Integrity*](http://c2.com/ppr/checks.html) (1994). Whole Value, the origin of treating a number and its unit as one value: the pattern asks for objects that "capture the whole quantity with all its implications beyond merely magnitude".
- Brian Foote and Joseph Yoder, [*Big Ball of Mud*](http://www.laputan.org/mud/) (PLoP '97, 1997). Why such systems exist.

## E.2 Discovery and collaborative modeling

- Alberto Brandolini, [*Introducing EventStorming*](https://leanpub.com/introducing_eventstorming) (Leanpub, in progress), with [eventstorming.com](https://www.eventstorming.com/). All three levels, from the method's author: ch. 4 "Running a Big Picture Workshop", chs. 13–14 on Process Modeling and ch. 17 "Running a Design-Level EventStorming".
- Stefan Hofer and Henning Schwentner, [*Domain Storytelling*](https://www.informit.com/store/domain-storytelling-a-collaborative-visual-and-agile-9780137458912) (Addison-Wesley, 2021), with [domainstorytelling.org](https://domainstorytelling.org/). The pictographic language and scope factors (ch. 4 "Scope").
- ddd-crew, [*Bounded Context Canvas*](https://github.com/ddd-crew/bounded-context-canvas) (V5). The canvas used in sections 5.1 and 19.1.
- ddd-crew, [*Context Mapping*](https://github.com/ddd-crew/context-mapping). The patterns and team relationships.
- ddd-crew, [*Aggregate Design Canvas*](https://github.com/ddd-crew/aggregate-design-canvas). The canvas used in section 7.1.
- Gojko Adzic, [*Specification by Example*](https://www.manning.com/books/specification-by-example) (Manning, 2011); author's page at [gojko.net](https://gojko.net/books/specification-by-example/). Turning experts' examples into living documentation.

## E.3 Consistency, distribution and actors

- Carl Hewitt, Peter Bishop and Richard Steiger, [*A Universal Modular ACTOR Formalism for Artificial Intelligence*](https://www.ijcai.org/Proceedings/73/Papers/027B.pdf) (IJCAI 1973). Where actors began.
- Joe Armstrong, [*Making reliable distributed systems in the presence of software errors*](https://erlang.org/download/armstrong_thesis_2003.pdf) (2003). The case for letting processes crash under supervision (section 4.4 "Let it crash" and section 5.2 "Supervision hierarchies").
- Pat Helland, [*Life Beyond Distributed Transactions: An Apostate's Opinion*](https://www.cidrdb.org/cidr2007/papers/cidr07p15.pdf) (CIDR 2007); revised in [*ACM Queue* 14(5) (2016)](https://queue.acm.org/detail.cfm?id=3025012). Entities with one writer, activities and idempotence.
- Pat Helland, [*Idempotence Is Not a Medical Condition*](https://queue.acm.org/detail.cfm?id=2187821) (*ACM Queue* 10(4), 2012). Why receivers must tolerate retries.
- Pat Helland, [*Immutability Changes Everything*](https://www.cidrdb.org/cidr2015/Papers/CIDR15_Paper16.pdf) (CIDR 2015). The log is the truth; everything else is a cache.
- Pat Helland and Dave Campbell, [*Building on Quicksand*](https://arxiv.org/abs/0909.1788) (2009). Guesses and apologies in systems that answer before they know.
- Werner Vogels, [*Eventually Consistent*](https://www.allthingsdistributed.com/2008/12/eventually_consistent.html) (*ACM Queue* 6(6), 2008, [doi:10.1145/1466443.1466448](https://doi.org/10.1145/1466443.1466448); the revised text is on All Things Distributed as "Eventually Consistent - Revisited", December 2008). The client's view of eventual consistency (section "Client-side Consistency").
- Hector Garcia-Molina and Kenneth Salem, [*Sagas*](https://dl.acm.org/doi/10.1145/38713.38742) (SIGMOD 1987, pp. 249–259). The original saga.
- Vaughn Vernon, [*Reactive Messaging Patterns with the Actor Model*](https://www.informit.com/store/reactive-messaging-patterns-with-the-actor-model-applications-9780133846836) (Addison-Wesley, 2015). Messaging patterns implemented with actors.
- Gregor Hohpe and Bobby Woolf, [*Enterprise Integration Patterns*](https://www.informit.com/store/enterprise-integration-patterns-designing-building-9780321200686) (Addison-Wesley, 2003); pattern catalogue at [enterpriseintegrationpatterns.com](https://www.enterpriseintegrationpatterns.com/). Process Manager, Idempotent Receiver, Return Address, Envelope Wrapper and Correlation Identifier.
- Chris Richardson, [*Pattern: Transactional outbox*](https://microservices.io/patterns/data/transactional-outbox.html) (microservices.io). The classic outbox.
- Dimitri Bertsekas and Robert Gallager, [*Data Networks*](https://web.mit.edu/dimitrib/www/datanets.html), 2nd ed. (Prentice Hall, 1992). Max-min fairness, the idea behind the water-filling allocator (section 6.5.2 "Max-Min Flow Control").
- Martin Kleppmann, Adam Wiggins, Peter van Hardenberg and Mark McGranaghan, [*Local-first software*](https://www.inkandswitch.com/essay/local-first/) (Ink & Switch, 2019). The case where the device owns its data.

## E.4 Functional core and event sourcing

- Gary Bernhardt, [*Functional Core, Imperative Shell*](https://www.destroyallsoftware.com/screencasts/catalog/functional-core-imperative-shell) (2012). The screencast that names the split.
- Jérémie Chassaing, [*Functional Event Sourcing Decider*](https://thinkbeforecoding.com/post/2021/12/17/functional-event-sourcing-decider) (2021). The `Decide`, `Evolve` and initial-state triple.
- Alexis King, [*Parse, don't validate*](https://lexi-lambda.github.io/blog/2019/11/05/parse-don-t-validate/) (2019). Why a check should return a more precise type.
- Greg Young, [*CQRS Documents*](https://cqrs.files.wordpress.com/2010/11/cqrs_documents.pdf) (2010). CQRS from its originator.
- Udi Dahan, [*Clarified CQRS*](https://udidahan.com/2009/12/09/clarified-cqrs/) (2009). Why data on screen is always stale.
- Greg Young, [*Versioning in an Event Sourced System*](https://leanpub.com/esversioning) (Leanpub, 2017). Upcasting, weak schemas and copy-transform.
- Martin Fowler, [*TolerantReader*](https://martinfowler.com/bliki/TolerantReader.html) (2011). Read only what you need.
- Danilo Sato, [*ParallelChange*](https://martinfowler.com/bliki/ParallelChange.html) (2014). Expand, migrate, contract.
- Ian Robinson, [*Consumer-Driven Contracts: A Service Evolution Pattern*](https://martinfowler.com/articles/consumerDrivenContracts.html) (2006). Consumers state what a contract must keep.
- Alistair Cockburn, [*Hexagonal Architecture*](https://alistair.cockburn.us/hexagonal-architecture/) (2005). Ports and adapters.
- Frederick P. Brooks, Jr., [*No Silver Bullet: Essence and Accidents of Software Engineering*](https://doi.org/10.1109/MC.1987.1663532) (*IEEE Computer* 20(4), 1987). Essential versus accidental complexity.

## E.5 Akka.NET

- [Akka.NET documentation](https://getakka.net/). In particular [*Event Sourcing*](https://getakka.net/articles/persistence/event-sourcing.html), [*Snapshots*](https://getakka.net/articles/persistence/snapshots.html), [*Persistence Query*](https://getakka.net/articles/persistence/persistence-query.html), [*Event Adapters*](https://getakka.net/articles/persistence/event-adapters.html), [*Serialization*](https://getakka.net/articles/serialization/serialization.html), [*Cluster Sharding*](https://getakka.net/articles/clustering/cluster-sharding.html), [*Split Brain Resolver*](https://getakka.net/articles/clustering/split-brain-resolver.html), [*Distributed Publish Subscribe in Cluster*](https://getakka.net/articles/clustering/distributed-publish-subscribe.html), [*Reliable Delivery*](https://getakka.net/articles/actors/reliable-delivery.html), [*Coordinated Shutdown*](https://getakka.net/articles/actors/coordinated-shutdown.html), [*Testing Actor Systems*](https://getakka.net/articles/actors/testing-actor-systems.html) and [*Persistence Testing*](https://getakka.net/articles/persistence/persistence-testing.html).
- Akka.NET, [*Akka.Hosting*](https://github.com/akkadotnet/Akka.Hosting). Hosting, DI and the hosting test kit (`Akka.Hosting.TestKit`).
- Akka.NET, [*Akka.Persistence.Sql*](https://github.com/akkadotnet/Akka.Persistence.Sql). Providers, tag-table mode and the published DDL.
- Akka.NET, [`BREAKING_CHANGES_V1.6.md`](https://github.com/akkadotnet/akka.net/blob/dev/BREAKING_CHANGES_V1.6.md). Every change behind section 21.10.
- Akka, [*Durable State*](https://doc.akka.io/libraries/akka-core/current/typed/durable-state/persistence.html) (JVM). The feature section 11.7 compares against.
- Aaron Stannard, [*Migrating from CRUD to CQRS and Event-Sourcing with Akka.Persistence*](https://petabridge.com/blog/crud-to-cqrs-akka-persistence/) (Petabridge, 2025). `PersistAll` overloads.
- Aaron Stannard, [*You Don't Need to Use Akka.HealthChecks Anymore*](https://petabridge.com/blog/you-dont-need-akka-healthchecks-anymore/) (Petabridge, 2025). Health checks built into Akka.Hosting.
- Aaron Stannard, [*Akka.NET + Kubernetes: Everything You Need to Know*](https://petabridge.com/blog/akkadotnet-guide-to-kubernetes/) (Petabridge, 2025). Deployments, bootstrap and the headless service.
- Petabridge, [*Phobos release notes*](https://phobos.petabridge.com/articles/releases/RELEASE_NOTES.html) (2026). The commercial tracing alternative.

## E.6 C# 15 and .NET 11

- Microsoft, [*What's new in C# 15*](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-15) (2026). The official feature list.
- Microsoft, [*Union types (C# reference)*](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/union) (2026). Declaration, conversions and patterns.
- .NET team, [*Unions* feature specification](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-15.0/unions) (2026). The exact rules.
- Microsoft, [*Serialize union types with System.Text.Json*](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/union-types) (2026). Type classifiers and their limits.
- Andrew Lock, [*The pain of serializing unions and closed class hierarchies with System.Text.Json*](https://andrewlock.net/exploring-the-dotnet-11-preview-7-the-pain-of-serializing-unions-and-closed-class-hierarchies-with-system-text-json/) (2026). A practitioner's tour of the same problem.
- Microsoft, [*What's new in .NET 11 libraries*](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/libraries) (2026). `AddTracing` and `ActivitySourceFactory`.
- Microsoft, [*Channels*](https://learn.microsoft.com/en-us/dotnet/core/extensions/channels) (2026). The options behind a hand-built mailbox.
- Microsoft, [*Testing with `dotnet test`*](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-with-dotnet-test) (2026). Microsoft.Testing.Platform mode and the `global.json` runner setting.
- Microsoft, [*ASP.NET Core SignalR .NET client*](https://learn.microsoft.com/aspnet/core/signalr/dotnet-client) (2026). Automatic reconnect and handling a lost connection.
- Microsoft, [*App lifecycle*](https://learn.microsoft.com/dotnet/maui/fundamentals/app-lifecycle) (.NET MAUI, 2026). What `Stopped` and `Resumed` promise.
- WHATWG, [*HTML Living Standard: Server-sent events*](https://html.spec.whatwg.org/multipage/server-sent-events.html) (2026). The event-stream format and `Last-Event-ID`.
- IANA, [*Time Zone Database*](https://www.iana.org/time-zones). The rules behind site-local time.

## E.7 Testing

- Kent Beck, [*Test Driven Development: By Example*](https://www.informit.com/store/test-driven-development-by-example-9780321146533) (Addison-Wesley, 2002). The Learning Test pattern (ch. 26 "Red Bar Patterns").
- Gerard Meszaros, [*xUnit Test Patterns: Refactoring Test Code*](https://www.informit.com/store/xunit-test-patterns-refactoring-test-code-9780131495050) (Addison-Wesley, 2007); author's site at [xunitpatterns.com](http://xunitpatterns.com/). Test smells (ch. 2 "Test Smells" and Part II, "The Test Smells", chs. 15–17).
- Koen Claessen and John Hughes, [*QuickCheck: A Lightweight Tool for Random Testing of Haskell Programs*](https://doi.org/10.1145/351240.351266) (ICFP 2000, pp. 268–279). Where property-based testing began.
- Anthony Lloyd, [*CsCheck*](https://github.com/AnthonyLloyd/CsCheck). Generators, shrinking and seeds.
- [Testcontainers for .NET](https://dotnet.testcontainers.org/) (2026). Containers as test fixtures.
- Aspire, [*Write your first test*](https://aspire.dev/testing/write-your-first-test/) (2026). `DistributedApplicationTestingBuilder`.

## E.8 Observability and operations

- Benjamin H. Sigelman et al., [*Dapper, a Large-Scale Distributed Systems Tracing Infrastructure*](https://research.google/pubs/dapper-a-large-scale-distributed-systems-tracing-infrastructure/) (2010). The paper behind spans and traces.
- W3C, [*Trace Context*](https://www.w3.org/TR/trace-context/) (Recommendation, 2021). The `traceparent` format.
- OpenTelemetry, [*OpenTelemetry .NET*](https://opentelemetry.io/docs/languages/dotnet/) (2026); [*Collector*](https://opentelemetry.io/docs/collector/) (2026); [*Semantic conventions for messaging spans*](https://opentelemetry.io/docs/specs/semconv/messaging/messaging-spans/) (2026).
- Prometheus, [*Using Prometheus as your OpenTelemetry backend*](https://prometheus.io/docs/guides/opentelemetry/) (2026). The OTLP receiver.
- Betsy Beyer, Chris Jones, Jennifer Petoff and Niall Richard Murphy (eds.), *Site Reliability Engineering* (O'Reilly, 2016), ch. 4 ["Service Level Objectives"](https://sre.google/sre-book/service-level-objectives/). SLIs, SLOs and error budgets.
- Betsy Beyer, Niall Richard Murphy, David K. Rensin, Kent Kawahara and Stephen Thorne (eds.), *The Site Reliability Workbook* (O'Reilly, 2018), ch. 5 ["Alerting on SLOs"](https://sre.google/workbook/alerting-on-slos/). Multiwindow, multi-burn-rate alerts.
- Michael T. Nygard, [*Release It!*](https://pragprog.com/titles/mnee2/release-it-second-edition/), 2nd ed. (Pragmatic Bookshelf, 2018). Circuit breakers, timeouts and stability patterns (the chapter "Stability Patterns").
- Tom Preston-Werner, [*Semantic Versioning 2.0.0*](https://semver.org/). The public-API rule and pre-release grammar.
- [*Conventional Commits 1.0.0*](https://www.conventionalcommits.org/en/v1.0.0/). The commit format release-please reads.
- Pete Hodgson, [*Feature Toggles (aka Feature Flags)*](https://martinfowler.com/articles/feature-toggles.html) (2017). Kinds of flags and where to decide them.
