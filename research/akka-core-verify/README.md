Verified spikes backing research/akka-core.md (built and run on 2026-11-04 with .NET SDK 11.0.100-rc.3.26522.148).
Run `source tools/env.sh` first. Each folder is a standalone project (bin/obj omitted).

- Verify/     Akka 1.5.73: domain w/ C# unions, ReceivePersistentActor, tagging adapter, sharding, Akka.Persistence.Sql (SQLite), Persistence Query, RestartSource projection, timers/stash/backoff, CoordinatedShutdown. `dotnet run -- --fresh`
- Verify16/   Same sources compiled+run unchanged against 1.6.0-beta5 (Verify16.csproj only differs in versions).
- Tests/      xunit v3 + Akka.Hosting.TestKit 1.5.73 (links ../Verify sources). `dotnet test`
- SerV2/      1.6.0-beta5 Akka.Serialization.V2 source-generated serializer incl. a C# union field.
- StjSer/     1.5.73 SerializerWithStringManifest on System.Text.Json + STJ/.NET 11 union probe.
- UnionAkka/  Shows union boxing vs Receive<T>, and Newtonsoft (Akka default) silently dropping union values.
- AsyncProbe/ ReceiveAsync suspends the mailbox; Sender stable across await.
- CfgDump/    Prints effective defaults (SBR, sharding, coordinated-shutdown phases, serializer bindings).
