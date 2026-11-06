
## Chapter 1 (book/ch01-why-ddd-and-actors.md): chapter editor review, 2026-11-04

**State on arrival.** All seven sections, the hook, "In this chapter you will", Starting point, Summary, DDD canon covered, Exercises and Further reading were present. All 12 `File:`-headed blocks matched `git show <tag>:<path>` (checked with a script). Every Try-it result matched a real run (`git show`/`git diff --stat` at each tag, plus the coder's recorded `verify-tags.sh` output). 5999 words.

**Changes made**
- 1.2 Try it: the closing sentence now says git prints the annotated tag's header before the commit subject, which matches the real output. It does not quote the annotation text, so it stays correct whichever tag-message format is chosen.
- 1.3: Hands-on Modelers and Continuous Integration now follow Evans more closely: modelers touch code and coders take part in modeling; CI includes automated tests that flag fragmentation.
- 1.4: "join your transaction" now reads "take part in your two-phase commit", to keep transaction meanings apart (style guide §8.3). Removed the `git log` line from the Try it to save words; the README block in 1.2 still teaches it.
- 1.5: the four failure modes now carry their Appendix C names (anemic actor, logic in Receive, one actor per database row, Ask chains). Added one sentence saying that from Chapter 7 the book keeps lifecycles in the state union rather than in `Become` (ddd-canon §2). Dropped the extra "Read model" row from the mapping table so the table matches the outline's four rows.
- 1.6: quoted ADR 0002's "Lives in the core | Lives in the shell" table verbatim (outline: "what lives where"). It passes the extraction check.
- 1.7: corrected the xUnit 4.x wording ("no longer supports running through VSTest", per testing.md §2). Added a "Containers and the Aspire CLI" subsection: `systemctl --user enable --now podman.socket` plus `DOCKER_HOST` (VERIFIED in testing.md), and the Aspire CLI with an aspire.dev pointer; no install command is given because none is verified. Added a short "Following along" subsection (outline: "how to follow along with tags"). Replaced the empty-solution output block with prose that keeps the exact error strings. Removed a sentence that repeated the `NuGetAudit` XML comment. Removed `has_projects()` from the listing; prose now describes the skip rule.
- Trimmed throughout to fit the word budget (summary merged to 6 bullets). DDD canon covered now lists the Anti-pattern catalog preview.
- Final count: 6046 words (target 5500, +9.9%); about 900 words are code. No banned words, no exclamation marks, no code line over 100 characters.

**Open items for the editor-in-chief / coder**
1. **Primer arithmetic (needs a sample fix by rebase at ch01-s02):** 27.6 kWh cannot be delivered under the stated allocations. 38 kW from 07:52 to 08:15 (≈23 min) plus 30 kW from 08:15 to 08:40 (25 min) comes to at most ≈27.1 kWh, and the book teaches power versus energy. Suggested fix: 25.0 kWh, which gives USD 10.50 + 1.00 + 4.00 = USD 15.50. Change `docs/ohmline-primer.md` steps 9–10 and re-quote; the book's 1.2 prose (two sentences) must change in the same pass. The book is left consistent with the repo as it stands.
2. **Deviation 1 (empty `Ohmline.slnx` versus the DoD):** I accept keeping the solution empty, as the outline says. Chapter 1 tags are skipped by `verify-tags.sh` and the chapter explains why. Please record a §11 exception for tags before ch02-s01.
3. **Deviation 2:** sample-architecture §2.2 should show `ManagePackageVersionsCentrally` in `Directory.Packages.props`. The book quotes the repo.
4. **Deviation 4 (tag message format):** please choose one format. The chapter does not depend on the choice.
5. The ACM Queue URL for Helland (HTTP 403 to curl) is not cited; Further reading uses the CIDR 2007 PDF.

## Chapter 2 (book/ch02-actors-from-first-principles.md): chapter editor review, 2026-11-04

**State on arrival.** All ten sections, the hook, "In this chapter you will", Starting point (`ch01-s07`), Summary (7), DDD canon covered, Exercises (2 Reflect, 2 Extend, 2 Break it) and Further reading (8, primary sources first) were present, with headings exactly as in outline.json. A script checked all 29 `File:`-headed blocks against `git show <tag>:<path>`: all passed, and none has a line over 100 characters or more than 40 lines. All nine Try-it diff stats match `git diff --stat` between consecutive tags, and the test counts (3/7/12/17/22/29/33/42/46), curl output and log lines match code-notes/ch02.md. There are no banned words, no exclamation marks and no authorial "we". 7439 words.

**Changes made**
- 2.1: the MTP sentence now matches 1.7's wording ("must be told what to run", so the book uses `--solution`). The old wording claimed `--solution` is the only form.
- 2.2: added the *mutable messages* anti-pattern (Appendix C), which DDD canon covered already claimed: positional records are init-only, but the protection is shallow (a carried `List<T>` is shared).
- 2.7: the closing sentence now says what replaces real time where: `FakeTimeProvider` (3.8) for pure domain code, `TestScheduler` (15.7) for actor timers. The old text implied `FakeTimeProvider` drives actor timers.
- 2.8: the decider paragraph now explains where a strategy normally lives (override `SupervisorStrategy()`), the default OneForOne restart (research/akka-core.md §2), and why this sample passes it through `WithSupervisorStrategy` (coder deviation 5). Corrected Escalate: it fails the *supervisor itself* so that its parent decides. It does not "fail the parent".
- 2.9: corrected `AddStartup`. It runs a callback after the actors start, and the callback registers the coordinated-shutdown task; it does not itself add a shutdown task.
- Exercise 6: the expected failure now matches the real assertion (`started.Count(s => s).ShouldBe(1)` sees 2), not "two `true` values".
- Final count: 7601 words (target 7000, +8.6%).

**Open items for the editor-in-chief**
1. outline.md/json 2.8 says `BackoffOptions.OnFailure(...)`; the 1.5.73 API is `Backoff.OnFailure(...)` returning `BackoffOptions` (the chapter has a Version note). The 2.4 row still says `ConcurrentDictionary<string, ConnectorActor>`; the sample uses `ActorDirectory<TActor>` over `Lazy<TActor>`. Suggest updating the outline text.
2. sample-architecture §3 should list `learn/Ch02.Hosted.Tests` (coder deviation 7), and should note that learn test settings live in `learn/Directory.Build.props` (deviation 9).
3. Tell the ch03-s07 writer: a C# 14 generic extension block on RC3 does not accept partial explicit type arguments (CS1061; 2.3 Version note). Do not claim it does in 3.7.
4. Exercise 5 refers to AK1005, whose description is from the analyzer docs table and was not exercised in the sample. The exercise is hedged ("if your build lets it through").

## Chapter 3 (book/ch03-csharp-15-modeling-toolkit.md): chapter editor review, 2026-11-04

**State on arrival.** All nine sections, the hook, "In this chapter you will", Starting point (`ch02-s09`), Summary (7), DDD canon covered, Exercises (2 Reflect, 1 Extend, 2 Break it) and Further reading (8) were present, with headings exactly as in outline.json. A script checked all 24 `File:`-headed blocks (C#, the ADR and house-style text) against `git show <tag>:<path>`, and all passed; no code line is over 100 characters and no block is over 40 lines. All nine Try-it diff stats match `git diff --stat` between consecutive tags, and the test counts (54/61/71/75/84/96/101/121/137), the compiler errors and the JSON strings match code-notes/ch03.md. Technical claims were checked against csharp-dotnet11.md §2–§10 and akka-core.md (1.6 Serialization.V2 union round-trip VERIFIED on beta5). There are no banned words and no exclamation marks. 7629 words.

**Changes made**
- "In this chapter you will": the old bullet said generic math, extension members and list patterns live "in the shared kernel", but they are in LanguageLab. It now names what goes where, and the list is still six bullets.
- 3.1: softened an unverified claim that the docs "leave out" what a union's `ToString()` prints. Tightened the project-file sentence.
- 3.2: tightened the sentence that leads into the key test.
- 3.3: tightened the sentence about the missing warning.
- 3.6: the `Kilowatts` diff was hand-trimmed: it dropped hunk headers and context lines, and spliced the `<` operator directly after the hunk header. It is now the verbatim `git diff ch03-s05 ch03-s06 -- learn/Ch03.LanguageLab.Tests/Kilowatts.cs` (36 lines, without the four file-header lines). The following prose now says that `IComparisonOperators` requires `==`/`!=`, which the record struct generates.
- 3.7: shortened the preview paragraph, which repeated the code comment.
- 3.8: tightened the Guid v7 ordering sentence.
- 3.9: "a bare `Occupied` told to a `Receive<ConnectorStatus>`-only actor goes unhandled" is now stated as following from the runtime-type rule, because no sample test exercises it (the research spike u6 does).
- DDD canon covered: reduced to the §8 names, with only the necessary qualifiers.
- Further reading: the spec link now goes to the learn.microsoft.com unions specification, which is the URL in the research sources. It no longer points to a csharplang directory.
- Final count: 7686 words (target 7000, +9.8%), about 75% prose.

**Open items for the editor-in-chief**
1. The outline's 3.9 text, "Akka sends the case not the union (Receive<Case> versus Receive<Union>)", reads as the opposite of the verified behaviour: a told union arrives as the boxed union struct, and only `.Value` reaches `Receive<Case>`. The chapter follows the tests. Please reword outline.md/json.
2. Style guide §6.3: please document the nested `DECLARATION_ERRORS` symbol (coder deviation 2). Roslyn suppresses method-body diagnostics once a declaration is in error.
3. Tell the ch06 coder and writer two things. First, `IValueObject.Create` throws `ArgumentException(err.Error.Message)`, so this must change when 6.2 makes `ValidationError` a union. Second, a static virtual `Create` is not callable as `StateOfCharge.Create(...)` (CS1929). Units that want `Power.Create(...)` need their own public static member, or the generic helper.
4. Tell the ch10-s04 coder: `tests/Directory.Build.props` makes every project under `tests/` an xUnit executable, so `tests/Ohmline.Testing` must override `OutputType`/`IsTestProject`.
5. The chapter is at +9.8% of its word target. Any later addition needs an equal cut.

## Chapter 4 (book/ch04-discovering-the-domain.md): chapter editor review, 2026-11-04

**State on arrival.** All eight sections, the hook, "In this chapter you will" (5), Starting point (`ch03-s09`), Summary (6), DDD canon covered, Exercises (2 Reflect, 2 Extend, 2 Break it) and Further reading (7) were present, with headings exactly as in outline.json. A script checked all 13 `File:`-headed blocks (Mermaid and text quoted from `docs/`, C# and the csproj) against `git show <tag>:<path>`: all passed, and none has a line over 100 characters or more than 40 lines. All eight Try-it diff stats match `git diff --stat` between consecutive tags, and the 137-test count, the build lines and the compiler messages in Exercises 5–6 match code-notes/ch04.md. Every claim about the docs (votes, policies P1–P9, aggregates, the 11-row classification, ADR 0004) was checked against the files at their tags. 6584 words (wc -w).

**Changes made**
- 4.2: removed "Each phase is named after the pivotal event that closes it". The timeline contradicts it: Onboarding and Arrival have no pivotal event, and *Session ended* opens Ending. The sentence now says that the pivotal events cut the wall into phases. Replaced "Nobody in the room could answer H2", which the file does not support, and folded the H2 answer into the top-four list. "more easily" became "more readily", and "recognise" became "recognize".
- 4.3: corrected the board grammar. A policy issues a command; it does not cause an event directly. An event follows a command or comes from an external system. Added a short paragraph on pink external systems (outline 4.3): at this level they receive commands and can refuse them.
- 4.4: "Registers" became "Meter registers" in the aggregate table.
- 4.7: added that the quadrant chart plots Tariffs at [0.58, 0.4], inside the *generic* quadrant, and explained why the heuristics table (build versus buy), not the chart, keeps it supporting.
- 4.8: "the first two / the last two" was ambiguous against the bullet order and now names the patterns. Separated what ADR 0004 says (Billing's audit trail is its tables and outbox) from the book's own supporting point (the session events are already in Charging's journal). The Try it no longer repeats the reason.
- 4.1 Try it: "the chapter adds documents only" became "the section …", because 4.6 adds code.
- "actor" is now "Akka.NET actor" where it could be misread (style guide §8.1, chapter 4 rule).
- Exercise 5 now says `error CS8509`, because CS8509 is in `WarningsAsErrors`. Exercise 6 now tells the reader to remove `Sneaky` before the rename.
- Further reading: removed the unverified "in progress since 2015" from Brandolini and added eventstorming.com, the URL in research/ddd-canon.md §1. DDD canon covered now uses the §8 name "Aggregates and Vernon's 4 rules (discovery only)".
- Cut words to make room for the additions (hotspot list, settle/start board paragraphs, hook-adjacent prose). Final count: 6589 words (target 6000, +9.8%).

**Open items for the editor-in-chief / coder**
1. Style guide §8.2/§8.3: the ch04-s05 glossary adds Meter anomaly, Occupied, Fault, Token lock, Blocked token, Payment, Void, Grace period and Demand-response signal, plus ambiguity rows for "hold / reservation" and "capacity" (code-notes/ch04.md). Please mirror them; I did not edit the contract.
2. Coder, optional: `docs/subdomains.md` plots Tariffs (supporting) at x=0.58, which is in the generic quadrant. Either move it to x<0.5 or keep it and add a sentence to the file. The book now explains it either way, but if the point moves, the 4.7 sentence must go.
3. Coder, optional: `big-picture.md` says "The phase names came from the pivotal events that close each phase", which the timeline contradicts (see 4.2 above). The book no longer repeats it.
4. Coder deviation 2 (id token event names `TokenRegistered`, `TokenBlocked`, `TokenLockedForSession`, `TokenUnlocked`) is quoted in 4.5 as reserved by the glossary. Tell the 12.7 coder and writer.
5. The chapter is at +9.8% of its word target. Any later addition needs an equal cut.

## Chapter 5 (book/ch05-bounded-contexts-and-context-maps.md): chapter editor review, 2026-11-04

**State on arrival.** All nine sections, the hook, "In this chapter you will" (6), Starting point (`ch04-s08`), Summary (8), DDD canon covered, Exercises (2 Reflect, 2 Extend, 2 Break it) and Further reading (8) were present, with headings exactly as in outline.json. A script checked all 16 `File:`-headed blocks (canvas and README text, csproj, targets, C#, CODEOWNERS, three Mermaid diagrams, ADR text) against `git show <tag>:<path>`: all passed, and none has a line over 100 characters or more than 40 lines. All nine Try-it diff stats match `git diff --stat` between consecutive tags, and the test totals (137/137/165/165/166/166/184/184/184), the OHM0001 text, the `ContextBoundaryTests` failure text and the 19/5/16 AssetTrack outcome match code-notes/ch05.md and the files at their tags. 8199 words.

**Changes made**
- 5.3: the section called the build-enforced pure Domain projects "what Evans calls a Segregated Core", which 5.8 then says stretches Evans's meaning. The idea is now introduced as isolating the domain (Evans 2003, chapter 4), and the heading is "an isolated core the build defends". The Summary bullet no longer says "Segregated Core". The 5.8 table and its caveat stand as written.
- 5.3: the reflection check is now described precisely. It sees a package that arrives transitively through another project once code uses it, not "anything reached through a project reference".
- 5.2: explained what `internal` adds to `closed`: closed seals the case set, and internal hides the helpers that are not part of the language.
- 5.4: the ddd-crew guide "names" the three team relationships. The sample's map, not the guide, puts a team relationship on every line before choosing a pattern.
- 5.5: the Open Host Service with a single consumer is now presented honestly, as a cheap bet. Corrected the CODEOWNERS paragraph: GitHub accepts one owner's approval per path even when a line names two teams, so "one reviewer from each team" is a convention, not something GitHub enforces. This matches GOVERNANCE.md.
- 5.6: "The Device Gateway is that context's ACL" became "the ACL in front of them" (the charge points).
- 5.7: ADR 0005's options now note that a cluster per context is the common default and isolates best (research/ddd-canon.md §3.3, §6.4 recommend it). Added a paragraph on what a role is in Akka.NET: `ClusterOptions.Roles`, `ShardOptions.Role` and `WithShardRegionProxy` (akka-core.md API surface), with pointers to 12.1 and 16.7. "Those that exist at ch05-s07 … are already checked" claimed the AssetTrack fixture was checked. It now says only that their project references are checked.
- 5.8: "thirty projects and a map of fourteen systems" became "31 projects and a context map with 14 nodes". `Ohmline.slnx` lists 31 projects at `ch05-s07` once `Ohmline.ArchitectureTests` is counted.
- Trimmed throughout to pay for the additions. Final count: 8246 words (target 7500, +9.9%), about 85% prose.

**Open items for the editor-in-chief / coder**
1. Style guide §8.2: the coder suggests adding the team names (Sessions, Gateway, Energy, Pricing and Accounts, Finance IT) and the new fictional sites (`ogd-library`, `pvo-center`, `slc-airport-lot`, `slc-fleet-yard`). The chapter uses the team names.
2. Coder deviation 5 (the target blocks four families, and the reflection test enforces "no packages at all") is presented in the chapter as designed. If the editor-in-chief makes the target forbid every package, then 5.3's `Shouldly` sentence and Exercise 5 must change.
3. research/ddd-canon.md §6.4 recommends a cluster per core context; ADR 0005 and the outline choose one cluster with roles. The chapter now names the trade-off. No change needed unless the outline changes.
4. The chapter is at +9.9% of its word target. Any later addition needs an equal cut.

## Chapter 6 (book/ch06-value-objects-and-entities.md): chapter editor review, 2026-11-04

**State on arrival.** All eight sections, the hook, "In this chapter you will" (6), Starting point (`ch05-s09`), Summary (7), DDD canon covered, Exercises (2 Reflect, 2 Extend, 2 Break it) and Further reading (7, primary sources first) were present, with headings exactly as in outline.json. A script checked all 20 `File:`-headed blocks against `git show <tag>:<path>`: all passed, and none has a line over 100 characters or more than 40 lines. All eight Try-it diff stats match `git diff --shortstat` between consecutive tags; the test totals (201/216/243/292/302/306/381/393) and per-section deltas, the three CS0019/CS0029 demo errors, CS8602, the 19/5/16 AssetTrack outcome and the CsCheck counterexample match code-notes/ch06.md. No banned words, no exclamation marks, no authorial "we". 7109 words.

**Changes made**
- 6.1: the diagram no longer claims to show "every combination" (current and voltage also add to themselves). The ticks sentence now gives the real reason (avoiding `TotalHours`, a `double`) instead of "keeps the arithmetic exact", which 6.8's counterexample contradicts. Removed a reference to "the outline" from reader-facing prose.
- 6.2: 6.1 promised that 6.2 uses `V × I`, but the prose never showed it. Added the tests' `Supply` chain (Bind then Map, voltage error first). The "two doors" now include section 3.6's `Create`, which matches Chapter 3's forward promise.
- 6.3: "section 6.1's `Sum<T>`" did not exist in 6.1's prose. It now points to section 3.6's `GenericMath.Sum<T>`. "Section 8.2 gives each tariff its policy" became "Chapter 8", because outline 8.2 does not list a RoundingPolicy.
- 6.4: the Shared Kernel scope now matches Chapter 5, which allows small building blocks such as `Result`. The old text said "nothing else".
- 6.6: "section 7.5's tests" became "Chapter 7's tests". The Version note says the sample, not "this book", verified the API.
- 6.7: the power reader rejects a comma *before* the regex runs, but the text said the regex rejected it. Added that `TryFindSystemTimeZoneById` is case-sensitive only in a fresh process, because a filled cache hides it (code notes).
- 6.8: replaced a sentence that repeated the `Generators.cs` comment with the verified fact that the alias coexists with `using CsCheck;`. Hedged seed replay: the sample never ran a replay, so the Consequences and Exercise 5 now ask the reader to check it rather than promise that the same inputs come back. "compare `KilowattHours`" now reads "raw amounts such as", because the quoted listing compares `Kilowatts`.
- Trimmed throughout to pay for the additions. Final count: 7141 words (target 6500, +9.9%), about 74% prose.

**Open items for the editor-in-chief / coder**
1. Coder deviations 1 (`Monetary`), 3, 5, 6, 7, 10 and 13 are presented in the chapter as designed. Deviation 8 (7.8 must reuse `Sites/PriorityClass.cs`) and deviation 9 (`Ohmline.SiteEnergy.Domain.Tests` already exists) need to reach the 7.8 coder and writer.
2. Style guide §8.2: the glossary now has a `Voltage` row (ch06-s01), and `SiteConfiguration` exposes `ConfiguredCapacity`, which the glossary does not have. Please consider adding both.
3. outline 8.2 does not mention `RoundingPolicy` on `Tariff`, while the glossary says it is per tariff. 6.3 now says "Chapter 8". Make sure 8.2 or 8.3 attaches it.
4. Seed replay through `CsCheck_Seed` under MTP `dotnet test` is unverified. If a later coder confirms the exact command, 6.8 and Exercise 5 can state it.
5. The chapter is at +9.9% of its word target. Any later addition needs an equal cut.

## Chapter 7 (book/ch07-aggregates-and-the-decider.md): chapter editor review, 2026-11-05

**State on arrival.** All nine sections, the hook, "In this chapter you will" (6), Starting point (`ch06-s08`), Summary (7), DDD canon covered, Exercises (2 Reflect, 1 Extend, 2 Break it) and Further reading (7, primary sources first) were present, with headings exactly as in outline.json. A script checked all 27 `File:`-headed blocks against `git show <tag>:<path>`: all passed (two needed a manual look because of indentation), and none has a line over 100 characters or more than 40 lines. The `ChargePoint.cs` diff in 7.6 matches `git diff ch07-s05 ch07-s06`. All eight Try-it diff stats and test totals (393/407/413/470/486/493/524/566) and per-class counts match code-notes/ch07.md. The CS8509 demo is quoted at `ch07-s05` (deviation 8). The connector table's "32 rows" was recounted at `ch07-s04` (31 rows plus the `null` arm). The canvases' throughput, size and cadence figures match the 7.1 table, and hotspots H4/H7/H9 match `docs/eventstorming/big-picture.md`. No banned words, no exclamation marks, no authorial "we". 9226 words, 69% prose.

**Changes made**
- 7.1: Vernon's heuristic now matches his essay: it asks whose job the consistency is (the user issuing the command, or the system later), not only "ask the domain experts".
- 7.3, 7.5, 7.8 and DDD canon covered: the outline's §B index assigns Side-Effect-Free Functions (7.4, 7.5) and Drawing on Established Formalisms (7.3 state machine, 7.8 water-filling) to this chapter, but the prose never named them. One sentence each now does, and both are in the canon line.
- 7.6: "The second overload" referred to an overload the listing does not show. It now says which overload is shown (from a given state, for 11.2's snapshots) and that the other starts from `TDecider.Initial`. Removed a sentence that repeated the quoted `IDecider` summary.
- 7.7: the opening's "07:52 driver" contradicted the hook (that driver's reservation was taken by the van at 08:07) and the primer (07:52 is when the primer's driver arrives). It is now the van driver. "A table of processed commands" (12.6) became "a window of recent command ids", per outline 12.6. Consequences gained the cost of recording instead of refusing: anomalies live only in the journal, so querying them needs a read model (Chapter 13).
- 7.8: "the primer" is now "the domain primer of Chapter 1 (`docs/ohmline-primer.md`)". The min/max sentence explained only the minimum. It now gives both bounds and the raise-to-minimum rule, which matches canvas invariant 2 as corrected at `ch07-s08`. The "one-word change" experiment now says the unused label was removed as well (otherwise CS0164, per code notes).
- 7.9: "two to four connector states" had no source. It now reads "one state per connector".
- 7.2: removed a sentence that repeated the quoted `ChargePointRegistered` comment.
- Trimmed to pay for the additions. Final count: 9323 words (target 8500, +9.7%), about 69% prose. No code listing was changed.

**Open items for the editor-in-chief / coder**
1. Policy name: the sample's canvases (`ch07-s01`) and the 7.9 diagram use `ReleaseOnSessionEnded`, while outline 8.5 names `SessionEndedReleasesAllocation` and also mentions an unlock-the-token reaction. The 8.5 coder and writer should pick one name. If it is the outline's, the canvases and the 7.9 diagram need a fix-forward.
2. Exercise 4 (add `Emergency : PriorityClass`, expect CS8509 in `FairShareAllocator.Rank`) was not run. It follows from the closed hierarchy and `Rank`'s no-discard switch. Note that `tests/.../Sites/ConnectorConfigurationTests.cs` has a second switch over `PriorityClass`, which fails too if the Domain project builds.
3. Coder deviations 1–11 are presented in the chapter as designed (operator commands, `Active`, `PricePerKwh`, `PriorityClass` move, `ConnectorLimits`, nested error cases, `Known.Charger`, no `Decider.Run`). Style guide §8.2 should gain the glossary rows the coder added ("Unavailable" commands, "Active (session)").
4. For 10.3/10.10: `SiteConfigured` carries `SiteConfiguration`, which has a private constructor, so it needs a converter that does not re-validate on replay (code notes).
5. The chapter is at +9.7% of its word target. Any later addition needs an equal cut.

## Chapter 8 (book/ch08-events-services-policies-supple-design.md): chapter editor review, 2026-11-05

**State on arrival.** All eight sections, the hook, "In this chapter you will" (6), Starting point (`ch07-s08`), Summary (7), DDD canon covered, Exercises (2 Reflect, 1 Extend, 2 Break it) and Further reading (8, primary sources first) were present, with headings exactly as in outline.json. A script checked all 15 `File:`-headed blocks against `git show <tag>:<path>`: all passed, and none has a line over 100 characters or more than 40 lines. The `SessionState.cs` diff in 8.6 matches `git diff ch08-s05 ch08-s06` byte for byte. All seven Try-it diff stats match `git diff --shortstat` between consecutive tags, and the test totals (573/601/616/636/643/654/662), every named test (all exist at `ch08-s07`; Exercise 3's is the one the reader writes), the CS8509 demo (`SessionStateCompileErrors.cs(14,64)` at `ch08-s06`), the DST table (3 h/60/30/USD 1.80 and 4 h/30/90/USD 2.40, recomputed) and the 2026 Denver transition dates match code-notes/ch08.md. Policies P1/P5/P6/P7 and hotspots H2/H3/H9 match `docs/eventstorming/*` at the tag. No banned words, no exclamation marks, no authorial "we". 7106 words.

**Changes made**
- 8.2: removed the reader-facing "The outline called the component set a union"; the closed-hierarchy decision now stands on its own reason (ADR 0003, `PersistedShapeTests`). The relaxed rule now names `ContractsPurityTests`.
- 8.2: "an upper bound ... so a session stops before its hold runs out" was not true. `SessionTerms.CostOf` at `ch08-s07` counts energy at the highest rate plus the session fee, but leaves out time rates and idle fees. The text now says it estimates high, says what it leaves out, and names policy P6 and the debt (hotspot H3) as the backstop. Exercise 1 now says "a high estimate" to match.
- 8.3: added why the lines are rounded rather than only the total (so the printed lines add up to the printed total). Rewrote the god-object sentence to match research/ddd-canon.md §4.8.
- 8.4: the operations rule said "for longer than the grace period", but the code (`at >= since + grace`) and the theory (15 min applies) say "for the whole grace period". Fixed.
- 8.6: the first build step found "every decision table" was imprecise, because one of the two CS8509s is in `Evolve`. It now says "both switches over the state". Step 2's errors also hit the old rows in `ChargingSession.cs` (CS8121 at (44,14) in the code notes), and the text now says so.
- 8.7 and the hook: "Denver" became the depot's time zone, `America/Denver` (the depot is `slc-depot`). The Windows IANA-id claim is still hedged and now links the .NET documentation, as style guide §7 requires.
- 8.8: "closes his tactical patterns with supple design" was inaccurate, because chapters 11–13 follow chapter 10. It now gives Evans's definition. `Tariff.Revise` is out of the Closure of Operations row: it takes components, not a tariff, and returns a `Result`.
- Summary: rewrote the specification bullet, which listed "a pattern predicate, a plain static call" as two different forms, and added a supple-design bullet (8 in total).
- Exercise 4: the reader now builds `Ohmline.Charging.Domain` alone at `ch08-s04`. Otherwise the CS8509s in `Tariffs.Domain` hide the point that `RequiresPreAuthorization` still compiles.
- Further reading: the Brandolini entry now matches Chapter 4 (Leanpub URL).
- Trimmed throughout to pay for the additions. Final count: 7144 words (target 6500, +9.9%), about 76% prose. No code listing was changed.

**Open items for the editor-in-chief / coder**
1. sample-architecture.md §4 still lists only SharedKernel for `*.Contracts`. From `ch08-s02`, `Charging.Contracts` references `Tariffs.Contracts` (coder deviation 2; errata). The table needs a row change, or a decision to reverse it.
2. Chapter 6 (6.3, line ~326) says "section 8.3's price calculator rounds once, at the end", but the calculator rounds each line once and sums the rounded lines. Chapter 6 needs a one-line fix-forward ("rounds each line once").
3. `SessionTerms.CostOf` is not an upper bound for tariffs with a time rate or an idle fee, although its summary says "the most the energy delivered so far can cost", which is true only of energy. Consider a coder note or a summary tweak, and make sure 15/16 cover P6.
4. Coder deviations 1–11 are presented as designed. The outline's 8.2 ("union TariffComponent") and 8.5 (`SessionEndedReleasesAllocation`, two-field intents) wording differs from the sample. Consider updating outline.md so later writers do not reintroduce them.
5. Style guide §8.2: the sample's glossary gained "Site-local time" (`ch08-s07`). Please add the row. `CommandId`, `ChargingIntent` and "intent" are also candidates.
6. The chapter is at +9.9% of its word target. Any later addition needs an equal cut.

## Chapter 9 (book/ch09-unit-testing-the-domain.md): chapter editor review, 2026-11-05

**State on arrival.** All six sections, the hook, "In this chapter you will" (6), Starting point (`ch08-s07`), Summary (6), DDD canon covered, Exercises (1 Reflect, 2 Extend, 2 Break it) and Further reading were present, with headings exactly as in outline.json. A script checked all 17 `File:`-headed blocks against `git show <tag>:<path>`, and all passed. The 9.3 `ChargePoint.cs` diff matches `git diff ch09-s02 ch09-s03`. All six Try-it diff stats match `git diff --shortstat`, and the test totals (662/668/708/723/746/809) match code-notes/ch09.md. Spot checks against the tags: the Show/CaseOf behaviour, the 38 kW/190 kW rebalance test, `ChargePoint.ReservationHold`, the session's Ended arms (Exercise 2 holds), the CSV rows (Exercise 3's 0.40/min idle rate), and the DST arithmetic (01:50 MDT + 15 min = 01:05 MST; 540/420/120 kWh). The xUnit/MTP claims match research/testing.md §2 (all VERIFIED). No banned words and no exclamation marks. 6553 words on arrival.

**Changes made**
- Hook and 9.3: "hold" for a reservation became "reservation" (in the glossary, Hold is Billing's pre-authorization). "Each test project has grown its own helper" became "two test projects", because only Charging and Site Energy had `DecisionAssertions`.
- 9.1: added a Version note (SDK 11.0.100-rc.3 / xUnit v3 4.0.2, with the verified VSTest-target error text). This is the chapter's one "at the time of writing". Added why tests pass `TestContext.Current.CancellationToken` (a cancelled run stops; the analyzers flag missing tokens). Removed the pyramid Mermaid chain, which only repeated the table and had no explanation after it; the table's lead-out now says that.
- 9.2/9.4: qualified "rebalance" as "allocation rebalance" (style guide §8.3).
- 9.3: "thirty methods" became "its 37 test methods" (the count at `ch09-s02`).
- 9.4: one sentence on what "model-based" means here (invariants as a light model). Named CsCheck 5.0.1. Removed the sampling-loop Mermaid, which had no explanation after it and only repeated the prose. The Try-it now says "four new tests in each decider's `...Properties` class", because Site Energy's fourth is the assertion's own unit test, not a property.
- 9.6: added "The idea" (a translator is a dictionary, policies are a grid), which was missing from the section anatomy. Added a short closing link to Chapters 10 and 12, where the mock and actor-test smells become tempting.
- Further reading: added learn.microsoft.com "Testing with dotnet test", the primary source for MTP mode (in research/testing.md).
- Trimmed throughout to pay for the additions. Final count is about 6640 by `wc -w` (about 6560 words of prose plus code, target 6000, about +10%), with about 74% prose. No code listing was changed.

**Open items**
1. The `// File:` header of the 9.6 translator block is 103 characters because the path is long. The sample code itself is within 100. Accept, or shorten in a layout pass.
2. The chapter sits at the top of its tolerance. Any later addition needs an equal cut.
3. Chapter 7's 7.4 listing shows the pre-fix `ReserveConnector` row order (errata entry exists). 9.3 tells the reader so.

## Chapter 10 (book/ch10-aggregate-as-persistent-actor.md): chapter editor review, 2026-11-05

**State on arrival.** All ten sections, the hook, "In this chapter you will" (6), Starting point (`ch09-s06`), Summary (6), DDD canon covered, Exercises (1 Reflect, 2 Extend, 2 Break it) and Further reading (7) were present. Headings match outline.json. A script checked all 23 `File:`-headed blocks against `git show <tag>:<path>`, and all passed with no line over 100 characters. Both diffs match `git diff` (the `PersistAllAsync` side-branch diff and the `ChargePointActor` extraction diff at `ch10-s10`). All ten Try-it diff stats match `git diff --shortstat`, and the test totals (809/822/934/944/945/952/953/955/956/1093) and the side-branch results match code-notes/ch10.md. Both sequence diagrams (the persist cycle and the write in flight) match the message order in `ChargePointActorTests` and `InFlightWriteTests`, and so does the two-writers diagram in `TwoIncarnationsTests`. The coder deviations are already worked into the text: no inbound protocol union, `ChargePointDirectory`, no `tags` column, the internal-stash bound hedged, the test journal not being atomic, the two-event batch in 10.9, and the `Accepted`/`Rejected` hooks. Exercise 3's no-op `ReleaseConnector` matches `Given_a_command_that_changes_nothing...`. No banned words and no exclamation marks. 8784 words on arrival.

**Changes made**
- 10.3: "Akka.NET keeps 1 to 100 for itself" was not what research/akka-core.md §9 records. The 1.5 documentation reserves 0 to 40, and the 1.6 documentation says "ids 1-100 are reserved". The text now gives both. "Each verified against the sample" became "each observed in a real run", because the private-constructor failure came from a scratch run against `ch10-s03`, not from a test in the sample.
- 10.4: "Akka.Persistence documents that `Sender` inside a persist callback is still the command's sender" was unverified in research, and research/akka-core.md §13 advises capturing `Sender` for `onComplete`. It is now hedged per style guide §7: a linked documentation statement, not tested for the newer `onComplete` callback, with capturing as the house style.
- 10.9: the replay filter's "discards ... and logs a warning" is now "is documented to", because the sample never exercises it (code notes). Added a pointer to ADR 0001 as the single-writer decision from Chapter 1.
- 10.7: "Production uses PostgreSQL (section 12.10)" became "The cluster of section 12.10 runs on PostgreSQL". 12.10 is the laptop Aspire setup, not production.
- 10.2 and 10.6, glossary §8.3: "someone else holds it" became "it is reserved for someone else", because Hold is Billing's pre-authorization. "one transaction" became "one database transaction".
- Small trims in 10.1, 10.3, 10.5, 10.7, 10.9 and 10.10 pay for the additions. Final count is 8784 by `wc -w` (target 8000, +9.8%), with about 75% prose. No code listing was changed.

**Open items for the editor-in-chief / coder**
1. **Coder deviation 10 needs a decision before 12.5/12.6.** Charging replies `CommandReply` and Site Energy replies `BudgetCommandReply`, but the outline later assumes one shared `CommandReply` (`Ask<CommandReply>` and the `DeciderActor` LRU of `CommandId -> CommandReply`). 10.10 describes the code as it is, and Exercise 1 asks the reader to weigh the two options. If the decision is one shared reply, 10.10's paragraph on the hooks and Exercise 1 must change.
2. outline.md 10.8 still mentions "the tags column that stays empty in TagTable mode". That column does not exist in Akka.Persistence.Sql 1.5.73. Update the outline so that 13.x writers do not reintroduce it.
3. outline.md 10.2 still mentions "a protocol union inside the actor". The sample has none, and the chapter says why: dispatch is over closed records, and the exhaustive switches are over the domain's unions. Update the outline to match.
4. Coder deviation 11: `Ohmline.ActorShell` references `Akka.Persistence.Hosting` only to get `Akka.Persistence`. Decide whether to pin `Akka.Persistence` in sample-architecture.md §2.4.
5. 11.6 and 11.8 should pick up two points from the code notes: the stashed messages that become dead letters (shown in 10.9), and the `serialize-messages` gotchas (interceptors and predicates are copied, and `BoundTypeJsonConverter` exists).
6. The chapter is at the top of its tolerance. Any later addition needs an equal cut.

## Chapter 11 (book/ch11-recovery-snapshots-and-failure.md): chapter editor review, 2026-11-05

**State on arrival.** All eight sections (11.5 untagged), the hook, "In this chapter you will" (6), Starting point (`ch10-s10`), Summary (8), DDD canon covered, Exercises (2 Reflect, 2 Extend, 2 Break it) and Further reading (8) were present, with headings matching outline.json. A script checked all 18 `File:`-headed blocks against `git show <tag>:<path>`, and all passed with no line over 100 characters. All seven Try-it diff stats match `git diff --shortstat`, and the test totals (1096/1116/1119/1124/1128/1166/1170) match code-notes/ch11.md. The three sequence diagrams and the state diagram match the tests' message order (`RecoveryTests`, `PersistenceFailureTests`, the rejection test). Spot checks: `SnapshotSequenceNr` returning `LastSequenceNr` (verified in the 1.5.73 XML docs in the NuGet cache), the recovery defaults and circuit-breaker values (code notes), the backoff schedule in `ChargePointDirectory` at `ch11-s06`, `ReservationExpiryTests` (the one-second-early case), `SnapshotFailureTests` (it really builds a listing scenario, as Exercise 3 assumes), and `ChargePointActorTests` opening with the serialize-messages assertion. The further-reading URLs resolve (O'Reilly returns 403 to scripts, the same URL as Chapters 5 to 8). No banned words and no exclamation marks. 8605 words on arrival.

**Changes made**
- Removed editorial back-stage talk from the prose: "the outline's short TestKit intervals" (11.4), "The outline asked whether..." (11.7, 11.8), "which the coder found" (11.6), and "The editor checked ... the coder searched" (11.7, now an impersonal statement of the 2026-11-04/05 search). Also changed "a bug in our code" to "a bug in the code or the serializer".
- 11.2 and Exercise 1: the `SitePowerBudget` size said "thousands of events a day", which contradicts the canvas in 7.1 ("tens of thousands a year", snapshot "100, smaller at the depot"). Both now match Chapter 7.
- 11.2 and 11.3: "rebalance" is now "shard rebalance" (style guide §8.3).
- 11.4: the clock sticky now refers to Chapter 4's board (lilac with a clock).
- 11.5: the hand-off stop message was described as an ordinary message that queues behind a write. Akka.NET's default is `PoisonPill` (research/akka-core.md); the text now says so and that section 12.4 replaces it. The anti-pattern callout now describes what `wrong/ch12-s04-context-stop` actually shows (from its commit message): the unanswered command and the dead-lettered stashed command.
- 11.6: added one sentence: the `Backoff.OnStop` supervisor also restarts a charge point with a poison event, so its ERROR line repeats on the backoff schedule.
- 11.7: in the comparison table, the state-sourcing storage is now "one snapshot and one deleted row each" (observed). The deleted-row finding is tied back to section 10.8's "soft-delete flag". "No projection may be built ... the driver app asks the actor" became "no projection can rely on the listing's events", with a read model that needs the display name (section 14.6) taking it from the listing's view.
- 11.8: the in-memory journal paragraph is now the Appendix C anti-pattern callout "using the in-memory journal, which hides serialization bugs". `WithJournalWrite` variants now say that only Fail and Reject take predicates and types.
- Summary: "Akka.NET has no Durable State" became "Akka.NET 1.5.73 ships no Durable State store" (style guide §7). DDD canon covered gains Aggregate Design Canvas.
- Final count is 8773 by `wc -w` (target 8000, +9.7%), with about 79% prose. No code listing was changed.

**Open items for the editor-in-chief / coder**
1. **outline.md 14.6 conflicts with 11.7.** It denormalises "the charge point listing's display name from section 11.7" into the driver-app read model. The listing's events are deleted after each snapshot (code notes: "do not build projections on `ChargePointListingRevised`"). 14.6 must get the name from the listing's view (or from something else), not by projecting listing events. 11.7 now says so.
2. Chapter 10 (10.8 table, line ~801) calls `deleted` "a soft-delete flag". On SQLite with 1.5.73, `DeleteMessages` deletes rows physically and keeps only the highest row, marked deleted. 11.7 explains this, and 10.8 could add "(see section 11.7)".
3. `sample/docs/persistence-choices.md` still says "(the editor checked the assemblies ...)". This is reader-facing repo text with an editorial role in it. The coder could reword it in a later commit.
4. The poison-event-under-backoff sentence in 11.6 follows from `Backoff.OnStop` semantics, but no test at `ch11-s06` demonstrates it. If 12.x keeps a test of the poison event under sharding, consider asserting the repeat there.
5. Carried from the code notes: the 123-character comment in `tests/Ohmline.Testing/Ohmline.Testing.csproj` is to be fixed forward. The chapter does not quote it.
6. The chapter is near the top of its tolerance. Any later addition needs an equal cut.

## Chapter 12 (book/ch12-cluster-sharding-and-application-services.md): chapter editor review, 2026-11-05

**State on arrival.** All eleven sections, the hook, "In this chapter you will" (6), Starting point (`ch11-s08`), Summary (8), DDD canon covered (names from ddd-canon §8), Exercises (2 Reflect, 2 Extend, 2 Break it) and Further reading (8) were present, with headings matching outline.json. A script checked all 30 `File:`-headed blocks against `git show <tag>:<path>`: all passed, none over 100 characters or 40 lines. All eleven Try-it diff stats match `git diff --shortstat`, the test totals (1170/1235/1236/1242/1279/1293/1408/1469/1476/1476/1481) match code-notes/ch12.md, and both `wrong/ch12-*` branches exist. The passivation sequence diagram matches `PassivationTests` (Passivate is sent from `OnEventApplied` before the reply, to a different receiver). Exercise 5's arithmetic (shard 67 becomes 17 with 50 shards; 2 and 20 stay) and the test names in Exercises 3, 5 and 6 were checked against the tags. The coder deviations (named nodes instead of `WithReplicas(3)`, static seed, the version-not-reply window, the region keys in Contracts, `Npgsql` at 12.10, NU1009, ASPIRE010) were already in the text. No banned words and no exclamation marks. 9326 words on arrival.

**Changes made**
- 12.1: "on an even split, the side with the lowest address survives" is not in the research notes; it is now attributed to the documentation.
- 12.2: "the region drops it with a warning" was not verified. It now says the message reaches no entity, which `ChargingMessageExtractorsTests` does check. The sentence on renaming the type name became the Appendix C callout "Anti-pattern: changing `maxNumberOfShards`, PersistenceId formats or serializer IDs" (with the shard-count consequence and a pointer to Exercise 5). "Anti-pattern: IActorRef as identity" was renamed to the Appendix C name "`IActorRef` used as identity".
- Style guide §8.3: three bare "rebalance" (12.2 callout, 12.3, 12.4) became "shard rebalance". In 12.6 and Exercise 4, "hold" and "holds" for a reservation became "reservation", "had reserved" and "has", because Hold is Billing's pre-authorization.
- 12.8: "Charging conforms to the Tariffs model" implied the Conformist pattern. The context map in section 5.4 has Tariffs to Charging as Customer/Supplier, and the text now says so.
- 12.10: `WithAkkaClusterReadinessCheck()` was credited to ServiceDefaults; it is added by `WithOhmlineCluster` (`ClusterExtensions.cs` at `ch12-s10`). It now says that `/alive` runs the live checks and `/health` runs every check. "Linq2Db's most specific PostgreSQL dialect" was unverified for linq2db 6.5.1 and became "the PostgreSQL 15 dialect, which PostgreSQL 17 accepts". Added that `autoInitialize` and `TagMode.TagTable` carry over from 10.7, because the outline's tag-table mode was not mentioned.
- 12.5 Try-it: the box ran `curl .../budget` without configuring the site, which gives a 404. It now says so, and gives the recorded answer as following a `POST /sites`.
- 12.9: removed from the Tip the back-stage "failed once in a run of every tag in this chapter" ("fails now and then at the Chapter 12 tags; a rerun passes"). Removed the alias sentence from the Version note, because the listing does not show the alias.
- 12.11: "the fast loop" now points to section 9.1.
- Trims that pay for the additions: text that repeated code comments in 12.1, 12.5, 12.8, 12.9 and 12.10. Final count is 9360 by `wc -w` (target 8500, +10.1%), with about 74% prose. No code listing was changed.

**Open items for the editor-in-chief / coder**
1. The `RegionTests` flake (a 10 s ask inside a 10 s `AwaitAssertAsync`) is in errata for a fix forward. Once it is fixed, the 12.9 Tip's "fails now and then at the Chapter 12 tags" should name the tag that fixes it.
2. Outline 12.10 still says "three Node replicas" and "Akka.Aspire ... (verify)". Update it to the named resources and static seed (ADR 0010) so that 16.9 and 21.6 writers do not reintroduce `WithReplicas(3)`.
3. Outline 12.6 still says "LRU of (CommandId -> CommandReply)". The sample stores versions, and 12.6 explains why. Update the outline. Chapter 19.3 should build on versions and the reply hooks `Accepted(command, version)`.
4. The architecture's config-key table lacks `Ohmline:SiteEnergy:AskTimeout` and `Ohmline:DriverAccounts:AskTimeout` (coder deviation 12.5).
5. The chapter is at the top of its tolerance. Any later addition needs an equal cut.

## Chapter 13 (book/ch13-cqrs-from-journal-to-read-model.md): chapter editor review, 2026-11-05

**State on arrival.** All eight sections (13.1 untagged), the hook, "In this chapter you will" (6), Starting point (`ch12-s11`), Summary (8), DDD canon covered, Exercises (2 Reflect, 2 Extend, 2 Break it) and Further reading (8) were present, with headings matching outline.json. A script checked all 17 `File:`-headed blocks against `git show <tag>:<path>`, and all passed. No body line is over 100 characters or 40 lines; two `File:` header comments are 101 and 103 characters because of the path. The 13.5 diff block matches `git diff ch13-s04 ch13-s05`. All seven Try-it diff stats match `git diff --shortstat`, the test totals (1545/1549/1552/1564/1567/1573/1594) match code-notes/ch13.md, and `wrong/ch13-s04-offset-before-handler` exists. The 13.4 sequence diagram matches the crash test at `ch13-s04`, and the anti-pattern callout uses the Appendix C name. The polling defaults (`refresh-interval = 1s`, `query-delay = 1s`, `max-tries = 10`, 500 to 1,200 ms) were checked against the reference.conf embedded in the Akka.Persistence.Sql 1.5.73 DLL in the NuGet cache. `JournalSequenceActor` exists there. The cross-references (4.3 green read models, 5.4 Customer/Supplier, 5.7 `ContextBoundaryTests`, 7.7 `NoChange`, 11.3, 12.1, 12.4, 12.6 202 with version, 12.8, 20.8) and the five non-O'Reilly/InformIT URLs were checked. No banned words and no exclamation marks. 8535 words on arrival.

**Changes made**
- 13.2: ADR 0009 gets its tag rows at `ch13-s06`, not `ch13-s02`. The sentence now says "from tag `ch13-s06`".
- 13.3: removed "Persistence Query makes one read journal per system", which is not in the research. The `max-tries` gap rule now comes from the plugin's reference configuration and says that no sample test provokes a gap. Before, it was stated as fact with no test behind it.
- 13.4: "20 %" became "20%" (style guide §5).
- 13.5: "two incarnations ... may overlap during a hand-over" contradicted 13.6 ("never two writers"). It now says "could overlap if the cluster's single-writer assumptions broke (section 13.6)".
- DDD canon covered: "Event Sourcing" became the canon name "Event Sourcing (snapshots, upcasting)", as in Chapter 10.
- Final count is 8554 by `wc -w` (target 8000, +6.9%). No code listing was changed.

**Open items for the editor-in-chief / coder**
1. Outline 13.2 and 13.4 to 13.6 still describe registration inside `AddXxxContext`, `projection_offsets` without `tag`, "add Npgsql 10.0.3 to CPM" and "numberOfInstances: 1 per projection". The chapter follows the sample: journal-builder extensions, `(projection_id, tag)`, Npgsql in CPM since `ch12-s10`, and one instance per stream. Update the outline so that Chapter 14 and 20 writers follow the sample too.
2. Outline 14.6 says the driver-app projection reads `charging-session` and `charging`. The tags are disjoint, which 13.2 states, so no envelope is seen twice. Its denormalised display name must still come from the listing's view (see the Chapter 11 open item 1), because `charging` carries listing events that are deleted after snapshots.
3. No test moves a daemon between nodes. 13.6 hedges the claim. A multi-node test in Chapter 21 could show it.
4. `projection_failures` has no `tag` column, so the live board's two streams report each other's failures in `GET /projections/status`. Exercise 4 uses this as an exercise. Decide whether 14.x or 20.8 fixes it.

## Chapter 14 (book/ch14-living-with-eventual-consistency.md): chapter editor review, 2026-11-05

**State on arrival.** All eight sections (14.8 untagged), the hook, "In this chapter you will" (6), Starting point (`ch13-s08`), Summary (8), DDD canon covered (names from ddd-canon §8), Exercises (2 Reflect, 2 Extend, 2 Break it) and Further reading (7) were present, with headings matching outline.json. A script checked all 17 `File:`-headed blocks against `git show <tag>:<path>`, and all passed; none is over 100 characters or 40 lines. All seven Try-it diff stats match `git diff --shortstat`, and the test totals (1607/1607/1614/1626/1628/1655/1661, fast loop 45) match code-notes/ch14.md. The coder's deviations (LiveUpdate in Contracts, the runner publishing after commit, `charging-session` only with names from the listing's view through `RunningIn`, the 204 on SSE reconnect, the Integration-trait runner test) were already in the text. The reference.conf quotes ("Tuning only refresh-interval will NOT reduce latency", "Default (1s/1s) = 500-1200ms, Optimized (10ms/10ms) = 20-100ms") and `max-buffer-size = 500` were checked against the Akka.Persistence.Sql 1.5.73 DLL. `(Ended, StopSession) => NoChange` (the hook's second tap), `GET /tokens/{uid}/sessions`, the CS8509-as-error setting behind Exercise 6, and the single `LiveUpdate` switch were checked at the tags. No banned words and no exclamation marks. 8497 words on arrival.

**Changes made**
- Hook: "refreshes every five seconds" became "every few seconds". No console exists in the sample, and `docs/consistency-per-screen.md` says "polls it every few seconds".
- 14.1: the read-your-writes sequence diagram had the client calling `ReadYourWrites` directly. It now goes client to `SessionEndpoint` to `ReadYourWrites`, and the reply is `CaughtUp(row)` then 200 with `ETag "7"`. The text says which class owns HTTP and which owns the wait, and names `StillBehind(6)` for the 202 path.
- 14.3: the rhetorical "Why 100 ms rather than 10 ms?" became a statement.
- 14.4: added that `Select`/`Prepend` over `IAsyncEnumerable` are the BCL's own (.NET 10+). Code notes gotcha 8 covers this. Added "like Akka.NET messaging in general" to the at-most-once claim (ddd-canon §5.7).
- 14.5: "the test server cannot carry a WebSocket" became "a WebSocket cannot pass through the test server's HTTP handler". Added the package story the outline asks for: the hub needs no package (shared framework, which the csproj confirms), the client is in CPM for `Ohmline.IntegrationTests` and later the app. Added a Version note on the 11.0.0-rc.3.26522.148 pin and the Appendix A re-pin.
- 14.6: "Section 13.2 expected..." became "Section 13.2 named this projection as one that also reads...". "At most once per session start" was wrong, because `DriverAppProjection` asks again on later events while a timed-out question has left the card without a name. The sentence now says so.
- 14.7: the runner test's PostgreSQL now points at the Testcontainers fixture of section 12.11, which the outline names.
- 14.8: the misquoted id comment now reads "A new table shape gets a new id.", as in each projection at `ch14-s07`.
- Style guide §8.3: five bare "transaction" became "database transaction" (14.1, 14.3, 14.4, 14.6, 14.7).
- Final count is 8661 by `wc -w` (target 8000, +8.3%), with about 82% prose. No code listing was changed.

**Open items for the editor-in-chief / coder**
1. Architecture §4 lists only `Akka.Hosting` for `*.Api`. `Ohmline.Charging.Api` references `Akka.Cluster.Hosting` from `ch14-s04` (for `DistributedPubSub`). Amend §4 or ask for a change (the coder's deviation).
2. Chapter 13, line 113, still says section 14.6's driver-app read model reads both `charging-session` and `charging`. At `ch14-s06` it reads only `charging-session`. Reword 13.2's "Disjoint tags" sentence, for example to "so a projection that reads both never meets one event twice". Chapter 14 now explains the change.
3. Outline 14.6 still says tags 'charging-session' and 'charging' and lists `driver_app_cards` without `meter_start_wh`. Update it to the sample, so that Chapter 19 and 20 writers follow the sample.
4. Lag measurement ends at the offset write, not the commit (code notes gotcha 1), while the `ProjectionStatusEndpoint` summary (line 12 at `ch14-s03`) says commit. 14.3 states the discrepancy. A fix forward could correct the summary or measure after the commit. Chapter 18 should use whichever it chooses.
5. The driver-app feed has no epoch (14.8 says "must", Exercise 4). Section 20.9 must pick this up.

## Chapter 15 (book/ch15-process-managers-and-sagas.md): chapter editor review, 2026-11-05

**State on arrival.** All eight sections (15.8 untagged), the hook (about 220 words), "In this chapter you will" (6), Starting point (`ch14-s07`), Summary (8), DDD canon covered (names from ddd-canon §8), Exercises (2 Reflect, 2 Extend, 2 Break it) and Further reading (7) were present, with headings matching outline.json. A script checked all 17 `File:`-headed blocks against `git show <tag>:<path>` (in order, one uniform indent removed), and all passed; none is over 100 characters or 40 lines. All seven Try-it diff stats match `git diff --shortstat`, and the totals (1669/1699/1830/1838/1859/1871/1905) match code-notes/ch15.md; "18 of the 30 new tests" in `ChargingJourneyTests` at `ch15-s02` and the 28 transition-table rows were counted at the tags. Checked at the tags: the state diagram against `Decide`/`Evolve` at `ch15-s02`, the actor's `CarryOut`/`PassivateIfDone` at `ch15-s03`, the USD 19.60 capture in `FakeContextTests`, `GiveBack` order, the `InMemoryPaymentPort` Warning at `ch15-s06`, ADR 0005 (Billing without actors), ADR 0011's reasons, domain story 03, and the 300-bus allocator test. The coder's deviations (NotStarted, `Next` with context and events, step-event names, `PowerAllocated`/`QueuedForPower`, the relay, ports at s03, the 504 at s03, `CostOf`, subscriber-not-journey release on normal end, timers without keys, Akka.Delivery not run, DData in memory) were already in the text. No banned words and no exclamation marks. 7662 words on arrival.

**Changes made**
- Removed four editorial-process references a reader cannot see ("the outline planned", "the outline's names", "Compared with the outline's `Next`", "the outline names no mechanism, so the coder added"); each now states the design reason or the tag in reader terms (15.2, 15.4).
- 15.2: "Every reply is matched ... with a `when` guard" was wrong: `HoldPlaced`, `HoldDeclined`, `BudgetCommandRejected` and `PaymentCaptured` have no guard. Now "every reply that names a session", and port results need none because only this journey's calls are piped back to it. "Enters the same state again to ask again" became "asks again with the same `Enter`" (it is `Again`, no re-entry).
- 15.6: the failed-write stash cross-reference was section 12.4; it is section 11.6 (persist failure stops the actor, stash dead-lettered), as Chapter 12 itself says.
- 15.3: the remember-entities Note now names the default (durable DData files on disk, a volume per pod), presents the Eventsourced store as something the sample tried (not visible at any tag), and says "tests that check orderings" rather than "read tag offsets" (code notes: orderings and `CurrentPersistenceIds`).
- 15.1: added that `RecordAndSkip` writes the event to `projection_failures` after five tries (honesty about the at-least-once claim), and tied the process manager to Chapter 1's Helland "activity" (ddd-canon §2). Removed a sentence that repeated the quoted `Translate` comment.
- 15.4: the happy-path sequence diagram shows `JourneySessionRelay`, but the test tells the session events itself; the text now says the test plays the relay.
- 15.8: `AllocationsRebalanced` has one entry per admitted request, so "persist as many allocation entries" became "up to as many ..., one per admitted request".
- Small trims to stay near the target. Final count is 7703 by `wc -w` (target 7000, +10.0%; prose well over 60%). No code listing was changed.

**Open items for the editor-in-chief / coder**
1. Add the durable projection id `journey-session-relay-v1` (and `session-ended-subscriber-v1`, tag `charging-journey`) to sample-architecture.md §6, as the coder asked; ADR 0009 already lists them.
2. ADR 0011 in the sample (`docs/adr/0011-delivery-guarantees.md` at `ch15-s06`) cites "section 12.4" for a message lost in the stash after a failed write; it should be section 11.6. Fix forward in the sample.
3. Outline 15.2 to 15.4 still say `Started`, `StepCompleted...`, `AllocationGranted/AllocationQueued replies`, `Next(state, input) -> (state, effects)`, and put the ports at s04. Update the outline to the sample so Chapters 16, 17 and 19 writers use `NotStarted`, the step-event names, `PowerAllocated`/`QueuedForPower`, `JourneyStep` and `JourneySessionRelay`.
4. Chapter 16.5 must keep the Warning's rule: the Billing adapter remembers only captures that happened or were refused for good, never "provider unavailable", under the `{journeyId}-capture` key.
5. Chapter 19.3: `JourneyStarted.CommandId` is stored but any `StartCharging` to a started journey is answered `CommandAccepted(LastSequenceNr)` without a check (code notes, 19.3).

## Chapter 16 (book/ch16-integrating-bounded-contexts.md): chapter editor review, 2026-11-05

**State on arrival.** All ten sections, the hook (about 245 words), "In this chapter you will" (5), Starting point (`ch15-s07`), Summary (8), DDD canon covered, Exercises (2 Reflect, 2 Extend, 2 Break it) and Further reading (8) were present, with headings that match outline.json. A script checked all 22 `File:`-headed blocks (21 fenced, 1 JSON with an italic header) against `git show <tag>:<path>`, and all passed. None is over 100 characters or 40 lines. All ten Try-it diff stats and test totals match code-notes/ch16.md. Cross-references were checked against the earlier chapters: 2.6/2.7 `ChargerTwin`, 3.9, 4.1 story 02, 4.7/4.8, 5.5/5.6/5.9, 7.7, 8.6, 11.7's four columns, 12.5, 12.6, 12.7's sidebar, 13.6/13.7, 14.4, 15.1, 15.5, 15.6, and ADRs 0003, 0004 and 0005. Test names, the `TallyPayClientTests` key and count, the gateway options, the AppHost endpoints and the dispatcher header were checked at their tags. 9775 words on arrival.

**Changes made**
- Style guide §8.3: seven bare "transaction" became "OCPP transaction" (hook, 16.6 three times) or "database transaction" (16.1 twice, 16.3, Summary). Two "outbox" became "outbox table" (16.2, Exercise 2).
- 16.1: one sentence says the table `read_models.integration_outbox` is named for what leaves Charging, while the guarantee comes from the journal (the row commits with the offset). Without it, "the journal is the outbox" and a table called outbox read as a contradiction. The pricing note became its own paragraph. "A consumer needs no Ohmline assembly" became "no Ohmline type": Billing does reference `Charging.Contracts`. The polling cost now points at section 14.3's lag (100 ms intervals since Chapter 14) instead of "a second or two".
- 16.2: the Kafka sidebar now says Akka.Streams.Kafka 1.5.73's producer sinks are untried in the sample (research lists only the version).
- 16.3: added the judgment section 4.8 handed to this section. Billing moves money, which the heuristic sends toward event sourcing. The chapter now says where the history lives instead, and what is lost (a hold row keeps only its latest status and `updated_at`). Added one sentence on Akka.Delivery's `EventSourcedProducerQueue`, the canon's alternative for a non-event-sourced context (ddd-canon §5.5), because "DDD canon covered" lists it. Removed a duplicate 4.8 reference.
- 16.4: says that `Microsoft.Extensions.Http.Resilience` 10.11.0 was already in CPM since `ch12-s10` (the outline asks to add it; this is the coder's deviation 1). Removed prose that repeated the code's comments (the 91/96 codes, "at the border").
- 16.5: "Section 5.4 drew the first direction" was wrong, because 5.4 drew both rows. The text now says so and names where each row is built. The hold's reply timer is section 15.6, not 15.5.
- 16.7: says the journey's proxy is registered but unused at the tag (deviation 11).
- 16.8: "the only ACL project that references Akka" became a contrast with the gateway's Akka-free translator project. "rebalances" became "rebalances its allocations" (§8.3). Removed "No step calls the next synchronously", which is doubtful because the poller and port ask, and two sentences that repeated code comments.
- 16.9: "configured exactly as `aspire run`" became "by the same AppHost code", and the simulator is now noted as explicit-start. The fixed-port rule is section 12.9's free-port rule; 9.1 has none. "Any of eight resources" became "any process the AppHost started". Removed the claim that section 21.4's CI runs the System trait separately, because outline 21.4 does not say so.
- 16.10: "fails the build" became "fails the test run". The rename scenario now says why Billing breaks: it ships separately against the old contract, and a rebuilt Billing misreads the stored entries.
- Try-it 16.5, 16.7 and 16.8: `aspire run --project` became `dotnet run --project src/Ohmline.AppHost`, as in section 12.10's box.
- Further reading: the DDD Reference URL is now the PDF that the other chapters and ddd-canon §1 use.
- Trims to stay within the target. The final count is 9896 by `wc -w` (target 9000, +10.0%), with about 76% prose. No code listing was changed, and all 22 blocks pass again.

**Open items for the editor-in-chief / coder / other chapter editors**
1. Chapter 15, line 516, says "Billing prices receipts exactly in Chapter 16". At `ch16-s01` the relay prices by `SessionTerms.CostOf`, the journey's rule, unrounded (coder deviation 4), and 16.1 says so. Reword Chapter 15, or have a later fix forward price receipts with the Tariffs calculator and `RoundingPolicy`.
2. Chapter 15, line 629, names TallyPay's header `Idempotency-Key`; the sample uses `X-Idempotency-Key`.
3. Chapter 5, line 678, says the Pluggable Component Framework "appears" in the gateway (16.6). The sample has none yet, and 16.6 says so. Change it to "would appear".
4. Outline 16.4 ("add ... to CPM"), 16.5/16.7 (a `charger-commands` region), 16.1 (kernel-typed fields) and 16.8 should be updated to the sample per the coder's deviations, so that Chapters 17 to 21 follow the sample.
5. The SQLite actor-test timeouts beside the system test (errata, `ch16-s10`) are still open. If section 21.4's CI keeps the `System` trait in the main job, it inherits them.
6. Break-it exercise 6 (the `23505` from `InvoiceIssuerTests`) and Reflect 2's "a second stream with its own offset" are the writer's predictions, not recorded runs. A quick check by the coder would let the text keep them as stated.

## Chapter 17 (book/ch17-tracing-through-the-mailbox.md): chapter editor review, 2026-11-05

**State on arrival.** The chapter had all nine sections with titles and tags that match outline.json. It also had the hook, "In this chapter you will" (6 bullets), Starting point (`ch16-s10`), Summary (6), DDD canon covered (all names from ddd-canon §8), Exercises (1 Reflect, 2 Extend, 2 Break it) and Further reading (8). A script checked all 16 `File:`-headed blocks against `git show <tag>:<path>`, and all passed. The one diff block matches `git diff ch17-s03 ch17-s04`. No block is over 100 characters or 40 lines. All nine Try-it diff stats match `git diff --shortstat`, the test totals match code-notes/ch17.md, and every cited test name exists at its tag. The exercise details were checked at their tags: `Alarm`/`Arm` and the 50 ms timer, `ChargingJourneyActor.Pipe`, `AddTraceEnvelopeTagger<TracedEventTagger>` in `WithOhmlineJournal`, the 20 s `Patience`, and the `Ohmline.Probe…` assertion. The research claims (semconv 1.45 rename, links as default, `UseOtlpExporter` exclusivity, the template's net10.0/OTel 1.17.0, the Phobos facts and hedges) agree with research/telemetry.md and ADR 0012. The chapter was 7027 words on arrival.

**Changes made**
- 17.4 Warning: the expand-then-contract reference pointed at Chapter 21. That rule is section 20.2 (deploy the read path before the write path), so the Warning now cites 20.2.
- 17.6: "All three [persist, apply, persisted] ... start after it has ended" was wrong for `persist`, whose explicit start time falls inside the `process` span (StashAndPersistTests). It now says "are recorded after it has ended". The text also names `PersistTraced`, which the outline asks for, beside `PersistAllTraced`.
- 17.7: `conversation_id` was said to be "on every span the journey records". At `ch17-s07` only `ConsumerSpans` sets it, not the write spans, so the text now says "every span the journey processes a message in". The links paragraph now says that semconv allows a parent as an opt-in. One sentence names the feed relay (`IntegrationEventRelay`) and the subscribers as projections that get the linked span, because the outline lists both. "The timer exposed a bug" became "Traced timers exposed a bug:".
- 17.5: "renamed ... in 1.45.0" now says semantic conventions 1.45.0, not OTel .NET. The text adds that semconv registers no value for Akka, so Ohmline writes `akka` (research §4). It lists `ConsumerSpans`' four `messaging.*` attributes exactly as the code at `ch17-s07` sets them.
- 17.3: the Akka.Hosting citation now names its source, the XML docs of `AkkaTraceContextProcessor` in 1.5.73. A sentence now introduces the sequence diagram, as style guide §5 requires.
- 17.2: `aspire run` became `dotnet run --project src/Ohmline.AppHost`, to match 12.10 and 16.x. A hedge was added: the coder ran the AppHost only through the system tests and did not inspect the dashboard.
- 17.1: added a Version note on .NET 11's `AddTracing`/`ActivitySourceFactory`, hedged where the research says UNVERIFIED (how they interact with `AddSource`). The meter paragraph lost text that repeated the quoted doc comment.
- 17.4: the reply paragraph, which repeated the `AskTraced` doc comment, is shorter.
- 17.9: the criteria table's "The question" column had two rows that were not questions (Cost, The journal) and one that was a bare list (Overhead). All three are questions now.
- The final count is 7148 by `wc -w` (target 6500, +10.0%). No code listing was changed, and all 16 blocks pass again.

**Open items for the editor-in-chief / coder / other chapter editors**
1. ch17-s08 still has no green `tools/verify-tags.sh` run because of the `LinkTests` warm-up race (errata, "observed, not fixed"). The 17.8 Note and Exercise 2 describe it. If the coder fixes it forward, change the Note.
2. The Aspire dashboard has not been looked at by hand for this chapter. If a later pass runs `dotnet run --project src/Ohmline.AppHost` at `ch17-s08`, 17.2 can describe what the Traces page shows and drop the hedge.
3. Chapter 18 (18.1, 18.4) must use `TracedPersistentActor.OnMessageHandled` for command counts, as 17.6 promises. Chapter 19 (19.8) must reuse `TraceEnvelope`/`AskTraced` and the projection span's traceparent, as 17.7 promises.
4. Outline 17.7's code change says "event metadata". The sample stores a `TraceEnvelope` and tags it with `TracedEventTagger` (coder deviation 6), so update the outline to match the sample for later writers.

## Chapter 18 (book/ch18-metrics-logs-health.md): chapter editor review, 2026-11-05

**State on arrival.** All nine sections are present, with titles and tags that match outline.json. The chapter also has the hook, "In this chapter you will" (6 bullets), Starting point (`ch17-s09`), Summary (8), DDD canon covered, Exercises (1 Reflect, 2 Extend, 2 Break it) and Further reading (8). The count was 6572 words. All 22 `File:` blocks pass the extraction check against their tags, and so does the 18.1 diff hunk. Every Try-it diff stat and test count matches `git diff --shortstat` and the coder's notes.

**Changes made**
- 18.1: the cardinality arithmetic contradicted its own definition. "3 × 20 × 15 … a few hundred series at most" now reads "at most 900 combinations, and far fewer in practice".
- 18.2: "the internal `ActorCell`" was wrong, because `ActorCell` is public (research §6.1 casts to it from user code). It is now called a public class that is an implementation detail. "Reads zero when it matters" became "misses the wait that matters".
- 18.2: the claim that `EventStream.Subscribe<T>` "rejects the interface `IActorTelemetryEvent` (CS0308)" was wrong. I compiled against Akka 1.5.73: `Subscribe<IActorTelemetryEvent>(Self)` builds with `using Akka.Event;`. Without that using, CS0308 appears, because the generic overload is an extension in `Akka.Event` (`LifecycleMetricsListener.cs` imports only `Akka.Actor`). The text now gives that cause and the real compiler message.
- 18.3: `InstrumentRecorder` was attributed to "section 18.1", which never shows it. The text now gives its location and says that the 18.1 and 18.2 tests use it.
- 18.4: `ChargingJourneyActor` logs from both `Apply` (the `apply` span) and a command handler (`PortFailed`, the `process` span). The text now says "while the actor's `process` or `apply` span is current".
- 18.6: added the ADR's practical consequence: until adoption, a noisy source is turned off through `AddSource` or the Collector, not with `DisableTracing`.
- 18.9: defined burn rate (1 = exactly the budget over 28 days). "Spends 2%" became "about 2%" (14.4/672 h = 2.1%; 2% holds for a 30-day window). Added the zero-traffic case: with no journeys, nothing to divide by, and no alert.
- Exercise 4: removing `HealthTags.Ready` leaves the `akka`/`persistence` tags, so the check is not "untagged". It now says "a check without the `ready` tag", and adds that the cluster readiness check alone keeps ready at 200.
- The final count is 6735 by `wc -w` (target 6500, +3.6%). No code listing was changed.

**Open items for the editor-in-chief / coder / other chapter editors**
1. Chapter 17, line 505, says "Chapter 18 counts commands there [`OnMessageHandled`]". The sample counts in the new `OnDecided` hook (coder deviation 1), and 18.1 explains why. Reword 17.6 to "Chapter 18 counts commands where the decision is visible", or similar.
2. Chapter 14, line 345, promises that "Chapter 18 turns these numbers into metrics, dashboards and an alert that pages someone when the history stops moving". At `ch18-s09`, projection lag is still only `GET /projections/status`, and 18.9 sends it to exercise 3. Either soften Chapter 14 or have the coder add a lag gauge and rule in a fix forward.
3. Chapter 15, line 774, cites "how long a command waits in the budget's mailbox (Chapter 18's actor metrics)". Chapter 18 deliberately records no mailbox depth (18.2). Point Chapter 15 at the `stash` spans of 17.6 or at Phobos instead.
4. The coder's notes (gotcha 4) and `LifecycleMetricsListener` keep the CS0308 misdiagnosis. That is harmless in code, but the notes should not propagate it to Chapter 19 or later writers.
5. `tools/verify-tags.sh` has not been run for ch18 tags (coder's notes).

## Chapter 19 (book/ch19-mobile-edge-maui.md): chapter editor review, 2026-11-05

**State on arrival.** All ten sections are present, with titles and tags that match outline.json (19.9 and 19.10 untagged). The chapter also has the hook, "In this chapter you will" (6), Starting point (`ch18-s09`), Summary (8), DDD canon covered, Exercises (2 Reflect, 2 Extend, 2 Break it) and Further reading (8). It was 8759 words. A script checked all 22 `File:`-headed blocks against `git show <tag>:<path>`, and all passed, with no line over 100 characters and no block over 40 lines. All five Try-it diff stats match `git diff --shortstat`. I re-ran `Ohmline.Mobile.Core.Tests` at s04 to s07 from `git archive` copies (6, 47, 73 and 88, as the boxes say) and counted the filtered totals from source (ClientContractsTests 39, SendTwiceTests 12, OutboxDispatcherTests 11). The exercise details were checked at their tags: `card.Version >= accepted` in `SessionCardView.Shows`, `RouteOf`'s reserve-connector route, and the order test's assertions. The research claims (MAUI rc.2 under the rc.3 SDK, API 24, the Akka trim/AOT table, #5153/#8790/#7246/maui#2244, 2.4 MB, 10 ms tick, SignalR client headers) agree with research/mobile.md.

**Changes made**
- 19.7: "WithAutomaticReconnect tries for about 45 s" became "tries four times, after 0, 2, 10 and 30 s" (42 s in total; research §3). "Same transaction" became "same database transaction" (style §8.3). The Helland 2007 paraphrase now uses his own terms (a guess that must look like one).
- 19.8: hedged the claim that `HttpClient`'s handler injects `traceparent` on the phone ("not yet observed on a device", coder gotcha 10). `docs/observability.md` "shows an emulator pointed at the Collector" became "explains how to point", because the doc is hedged and nothing ran on an emulator. Added that production phones export to a public, authenticated Collector, never a dashboard (outline 19.8, research §6). The Try-it said `TapToJournalTraceTests` asserts "the parent chain shown in the table", but the table includes the `stash` span, which the test does not assert. It now names what the two tests assert.
- 19.10: the Version note said the 1.6 betas AOT-compile "cleanly"; only beta5 was measured, and its NativeAOT build left three warnings (research §2.2). It now says so. Added that iOS's runtime was not tried.
- Small trims (19.1 artifact list, 19.9 lead-in) to stay inside the target. Final count 8794 by `wc -w` (target 8000, +9.9%). No code listing was changed; all 22 blocks pass again.

**Open items for the editor-in-chief / coder / other chapter editors**
1. **CS0200 in the MAUI head (coder: NEEDS EDITOR ACTION).** A sequential `tools/verify-tags.sh 'ch19-*'` fails the head build at s05 and s07 (MAUI 11 rc.2 XAML incremental hot reload plus a warm compiler server). The 19.5 Version note tells readers to run `dotnet build-server shutdown`. Decide between the fix forward in errata.md (`EnableMauiIncrementalHotReload=false` in the head, `dotnet build-server shutdown` in verify-tags.sh, landing in ch20-s01's diff) and amending `ch19-s04` under architecture §10. If the fix lands, the 19.5 note can say it is fixed from that tag.
2. Chapter 12, line 648, says a key reused with a different body "is not detected yet; the driver app's outbox needs that check, and Chapter 19 adds it". The server still does not compare bodies. 19.3 moves the id into the body, rejects a header that names another command (400), and the app always resends its stored bytes. Reword Chapter 12 to "Chapter 19 closes the gap from the app's side".
3. The outline's 19.2/19.3/19.5 to 19.8 text should follow the coder's deviations for later writers: `ProblemDto`, `DriverSessionCards`, `DriverFeedPage`; 422 `unknown-connector`; `SendAccepted`/`SendRejected`/`SendTransientFailure`; the session page and stop command; `SqliteNode` instead of Akka.Hosting.TestKit; no `Aspire.Hosting.Maui`.
4. Nothing in the chapter has been run on an emulator or device. Every runtime claim about Android is hedged (the 19.4 Version note, 19.8). If an emulator run is recorded later, 19.6 to 19.8 can drop the hedges.

## Chapter 20 (book/ch20-events-are-forever.md): chapter editor review, 2026-11-05

**State on arrival.** All ten sections are present, and their titles and tags match outline.json. The chapter also has the hook, "In this chapter you will" (6), Starting point (`ch19-s08`), Summary (8), DDD canon covered, Exercises (2 Reflect, 2 Extend, 2 Break it) and Further reading (8). It was 7655 words. A script checked all 21 `File:`-headed blocks against `git show <tag>:<path>`, and all passed, with no line over 100 characters. The 20.7 `diff` block matches `git diff ch20-s06 ch20-s07`. All eleven Try-it diff stats, including the alt branch, match `git diff --shortstat`, and the full-suite counts match the coder's table. The filtered totals were checked from source: JournalInspect 8, OlderShape 3, Upcast 4, LegacyEventsReadAdapter 3, Corpus 119+3, UnreadableSnapshot 2, RenamedEvent 1, Migrator.Tests 2+5, BillingReadsTheFeed 6, and PersonalData 10 projects + 2 facts. Cross-references to 7.7, 10.3, 10.8, 11.2, 11.3, 11.7, 12.2, 12.6, 12.10, 13.4, 14.8, 16.9, 17.7, 18.1 (`command.type`) and 19.2 (`/app/v2`) were checked against those chapters.

**Changes made**
- 20.1: added a sentence introducing the Mermaid diagram (style §5). "anticorruption layer" became "Anticorruption Layer (ACL)", so the ACL abbreviation used in 20.7 is introduced in this chapter (style §8.4).
- 20.5: the claim that Akka.NET 1.6 keeps a wire-format corpus is now attributed to its documentation (research operations.md §2.8 gives it only from docs). Shortened the paragraph about the two corpus facts.
- 20.6: "Skipping has to happen at the store" was imprecise. It now says that a snapshot that loads is applied and replay starts after it, and that only a failed load (`UnknownManifestException`) sends recovery back to event 1, which matches the coder's reading of Eventsourced.Recovery.cs.
- 20.7: the rename table gained a row for the refusal and the snapshot case (code `connector-faulted` and `$kind` `"faulted"` kept), from the coder's notes. Added a forward reference to 21.7 for rolling updates. Added an `Anti-pattern: changing maxNumberOfShards, PersistenceId formats or serializer IDs` callout (same name as 12.2), because the outline asks 20.7 to say that shard type names and persistence ids never change, and "Anti-pattern catalog" is listed under DDD canon covered.
- 20.8: "DDL for each database" became "DDL in its repository, under `docs/ddl/`", since only PostgreSQL's files are verified. Hedged that the failure path of `WaitForCompletion` is Aspire's documented behaviour, because the tests run only the successful path. Added a forward reference to 21.6 (migrator Job). Cut the CS0433 aside to stay within the word target; it is still in the code notes.
- 20.9: "can set it back as quickly" glossed over the fact that the endpoints read `ReadModelOptions` through `IOptions`. It now says each node switches at startup. Billing stepping over `.v2` is now tied to `Given_both_versions_of_a_session_then_Billing_invoices_it_once`.
- Small trims in 20.4, 20.5 and 20.8. Final count 7680 by `wc -w` (target 7000, +9.7%; prose about 75%). No code listing was changed, and all 21 blocks pass again.

**Open items for the editor-in-chief / coder / other chapter editors**
1. Style guide §8.2 says to update the glossary when ch20-s07 changes it. The "Connector status" row should now name `OutOfOrder` (event `ConnectorOutOfOrder`, written as `charging.connector-out-of-order.v1`, with old rows read under `charging.connector-faulted.v1`). It should also gain a "Repaired" row (`ConnectorRepaired`, manifest `charging.connector-fault-cleared.v1` kept). Status value `"faulted"` and `RejectionCodes.ConnectorFaulted` stay on the wire. I did not edit the binding style guide.
2. `docs/contracts-that-never-die.md` at ch20-s04 says read adapters apply to "the journal's replay and, through the journal, its queries", but the coder's gotcha 8 says `EventsByTag` with adapters is untested. The chapter keeps the hedged wording. The coder may want to soften the doc in a later tag.
3. Helland is cited as *ACM Queue* 2016 here and as the CIDR 2015 paper in Chapter 11. Both are legitimate, but the copy-edit pass may want one form.
4. The feed epoch promised in 14.8 is still not implemented at ch20-s10. The chapter says so, and Chapter 21 should not assume it.

## Chapter 21 (book/ch21-releasing-and-running-the-cluster.md): chapter editor review, 2026-11-05

**State on arrival.** All ten sections are present, and their titles and tags match outline.json. The chapter also has the hook, "In this chapter you will" (6), Starting point (`ch20-s10`), Summary (8), DDD canon covered, Exercises (2 Reflect, 2 Extend, 2 Break it) and Further reading (8). It was 9892 words. A script checked all 27 `File:`-headed blocks (three of them JSON with an italic header) against `git show <tag>:<path>`, and all passed. No line is over 100 characters and no block is over 40 lines. The 21.10 `diff` hunk matches `git diff ch21-s09 ch21-s10 -- Directory.Packages.props`. All eleven Try-it diff stats, including `wrong/ch21-s02-minver-appversion`, match `git diff --shortstat`. The test totals match code-notes/ch21.md (2599/2613/2613/2615/2623/2632/2651/2661/2664/2676). `v1.0.0`, `upgrade/akka-1.6` and the wrong/ branch exist. These were checked against research and the sample:
- the AppVersion ordering and rebalance pause (operations.md §4.1);
- the SerializationException-drops-message quote (operations.md §2.6);
- #8702, #8445 and #8545 (akka-core.md);
- release-please v5 and node24;
- the default container tag `latest` and UID 1654;
- the Petabridge Deployment guidance;
- Evans's Continuous Integration quote (*DDD Reference*, p. 5);
- the 75 s default budget, the 30 s phase sum and the 60 s worst case of Exercise 6 (`ShutdownBudgetTests`);
- the migrator Job, RBAC rules, rolling-update and blue/green runbooks, and ADR 0017;
- each exercise's test names and identifiers at their tags.

**Changes made**
- Hook: "three things went wrong" listed only two, so it is now two. 21.2 now says the AppVersion trap was "the first failure" of the opening rather than "the eleven minutes".
- 21.1: removed "a deviation from the outline", which is production language the reader should not see. The Tariffs.Contracts reason is kept.
- 21.5 and 21.7: "The coder's run" and "The coder ran it" became "A run at `v1.0.0`" and "Here is a real run on a laptop". The heading spelling "artefact" became "artifact", as in the rest of the book. The `reconnect-storm` fallback no longer claims that "many chargers" reconnect after one second, a claim with no source.
- 21.6: the migrator-Job callout was a `Warning`, but a refused apply does not silently lose data, so it is now a `Note`. It moved from "Downing with a lease" to beside the paragraph that introduces the migrator. The lease-majority sentence ("two equal halves, or a minority ..., never both stay up") is now "at most one side stays up, even when the halves are equal".
- 21.7: "OCPP 1.6J has no message for this" became "Ohmline's OCPP-style protocol ...", following the style guide's §7 rule against conformance-style claims about OCPP.
- 21.8: the account of section 10.9 was wrong. It said the refused incarnation stops "after its sender may already have been told yes", but 10.9 and `TwoIncarnationsTests` show that the refused writer's sender gets no reply. The text now says the index refuses whichever batch arrives second, that the incarnation stops without answering, that which side wins is timing and not rules, and that without the index the histories interleave. This matches ADR 0017.
- 21.9: amounts are now in the house style (USD 0.40, USD 2.50, USD 15.09 instead of USD 16.59), with the reference fixed to section 8.3, where the primer's charge is priced. The rollback criteria now add that the deploy commit is reverted after `rollout undo`, so that the overlay matches what is running.
- 21.10: the Version note claimed V2's "serialization API was still changing between betas", which no research note supports. It now says "a beta may still change its defaults and its APIs". "The first Akka.NET serializer that round-trips a union as a field" became a comparison with 1.5's default serializer (section 3.9). The rollback hazard is now worded as in research ("rows 1.6 wrote with `primitive` may be unreadable" to a 1.5 process without Akka.Remote's configuration).
- Try-it boxes 21.4, 21.6 and 21.10 run `git diff --stat` excluding lock files but quoted only the totals that include them. They now give both counts (4/166; 23/840/25; 9/308/1), each checked with `git diff --shortstat`.
- DDD canon covered now uses the §8 names ("OHS", "Event Sourcing (snapshots, upcasting)") and adds "Functional core / actor shell" for the flag-in-the-shell rule.
- Small trims in 21.2, 21.3, 21.4, 21.5, 21.7 and 21.9 to pay for the additions. Final count 9898 by `wc -w` (target 9000, +10.0%; about 80% prose). No code listing was changed, and all 27 blocks pass again.

**Open items for the editor-in-chief / coder / other chapter editors**
1. ch21-s08 adds "Pricing model" to the sample's `docs/glossary.md`, and the style guide §8.2 table should gain the same row (`PricingModel`, `SessionStarted.PricingModel`: `standard` / `dynamic-idle-fee`, chosen by a flag, read only by Decide). I did not edit the binding style guide.
2. The chapter is at the +10% limit. Any addition by a later pass needs an equal cut.
3. Still unverified, and hedged in the text:
   - release-please updating `version.txt` only when it exists;
   - both workflows running on GitHub, and the push to ghcr.io;
   - any Kubernetes runtime behaviour (no kind or kubectl apply);
   - a zip build failing under `-warnaserror`.

   The coder could close the Kubernetes item with a kind run if one becomes available.
4. In the 21.9 rollback paragraph, the text says "a check the runbook should name". `docs/runbooks/rolling-update.md` does not yet say "publish no capped tariff while a 0.9.0 node remains". The coder may add it in a later tag. The chapter does not claim that the runbook says it.
5. The Ch02.Hosted shutdown-log flake (errata.md) remains open for the editor-in-chief to decide, since fixing it changes code quoted in section 2.9.

## Whole book: continuity editor pass, 2026-11-05

**Checks run (all 21 chapters).** H1 and every `## N.M` heading match outline.json; tail sections are Summary, DDD canon covered, Exercises, Further reading in that order; no heading level jumps and nothing deeper than `####`. Every "Starting point" names the previous chapter's last tag, which exists in the sample. Every "section N.M" and "Chapter N" reference names a section or chapter that exists. No Markdown anchors or links to local files. About 300 forward references were read against their target sections, and the backward section references were checked against target titles. A script re-checked all 391 `File:`-headed blocks against `git show <tag>:<path>` after the edits: 0 mismatches. No code listing was changed.

**Fixes: broken promises and wrong cross-references**
- Ch 4 (4.4) and Ch 7 (7.1, 7.8): "section 15.8 measures it / makes it" became "says how to measure", because 15.8 has no benchmark and only lists what to measure.
- Ch 4 (4.7): "which bounded contexts exist, nor how each is built; that is the next section" now credits Chapter 5 with the first and 4.8 with the second.
- Ch 5 (5.9): the Pluggable Component Framework "would appear" in the gateway, and 16.6 defers it (Ch 16 open item 3).
- Ch 6 (6.3): the 8.3 calculator "rounds each line once and sums the rounded lines", not "once, at the end" (Ch 8 open item 2).
- Ch 8 (8.4): dropped the claim that 14.6's site live board shows whose idle fee starts soon. No read model in Chapter 13 or 14 does.
- Ch 10 (10.5): "mailbox-depth metrics (section 18.2)" became the `stash` spans of 17.6, with 18.2 explaining why mailbox depth misses the wait. Chapter 18 records no mailbox depth.
- Ch 10 (10.8 table): `deleted` is no longer "a soft-delete flag". It now reads "marks the highest row kept after a delete", pointing to section 11.7 (Ch 11 open item 2).
- Ch 12 (12.6): "Chapter 19 adds [the body check]" became "Chapter 19 closes that gap from the app's side" (Ch 19 open item 2).
- Ch 13 (13.2): removed "(section 14.6's driver-app read model does)" from the disjoint-tags sentence (Ch 14 open item 2).
- Ch 14 (14.3): no longer promises that Chapter 18 builds a lag metric and alert. It says Chapter 18 leaves that as an exercise (Ch 18 open item 2).
- Ch 15 (15.4): "Billing prices receipts exactly in Chapter 16" became "Chapter 16's receipts reuse the rule" (Ch 16 open item 1). 15.6: TallyPay's header is `X-Idempotency-Key` (Ch 16 open item 2). 15.8: "Chapter 18's actor metrics" became "section 17.6's `stash` spans" (Ch 18 open item 3).
- Ch 17 (17.6) and Ch 18 (18.1): 17.6 now says Chapter 18 counts commands where the decision is visible. 18.1 no longer says 17.6 "proposed" `OnMessageHandled` (Ch 18 open item 1).

**Fixes: duplicated explanations**
- 14.3 re-explained the two pollers of 13.3 in full. It is now one back-reference sentence that names both settings.
- 11.5's PoisonPill callout described the outcome of `wrong/ch12-s04-context-stop`, which 12.4 demonstrates. It now just points to that branch.
- Ch 7 had two callouts, "anemic actor" and "logic in Receive handlers", repeating 4.5 ahead of 10.10. They are now one callout under the Appendix C name, "anemic actor and logic in Receive", that refers back to 4.5 and forward to 10.10. 4.5's callout uses the same name and points to 10.10.

**Fixes: terminology and spelling drift**
- "anticorruption layer(s)" in prose became Evans's pattern name, "Anticorruption Layer(s)" (style §8.4): Ch 1, 5, 6, 15 and 16, 12 lines.
- Spelling doublets unified to the majority form, outside code: recognise to recognize (Ch 11, 12, 15, 20), modelled to modeled (Ch 11, 12), catalogue(d) to catalog(ed) (Ch 4, 9, 11).

**Word budget.** Every edit was paid for inside the touched paragraphs. Ch 4 6597, Ch 5 8250, Ch 12 9349, Ch 15 7700 and Ch 17 7150 are all at or under +10%. Ch 9 was already 6638 (+10.6%) before this pass, and only spelling changed there.

**Left open (outside a continuity pass, or needing the coder or editor-in-chief)**
1. Ch 11 says "Chapter 12 wires [the listing] into the node as the `charge-point-listing` shard". The sample does this at `ch12-s02` (`WithShardRegion<ChargePointListingRegion>`), but the text of Chapter 12 never mentions it. Add a sentence to 12.2 if budget allows.
2. Helland is cited three ways: CIDR 2007 (Ch 1), CIDR 2015 (Ch 11) and *ACM Queue* 2016 (Ch 20). Unify in copy-edit.
3. Ch 1's "How to follow along" and the Appendix A and C references depend on `book/00-front-matter.md` and `book/appA…appE`, which do not exist yet.
4. Style guide §8.2 glossary rows requested by the chapter editors (Ch 4, 6, 7, 8, 20 and 21 open items) are still not mirrored. I did not edit the binding style guide.
5. The "changing `maxNumberOfShards`…" callout appears three times (4.5, 12.2, 20.7). Each is short and tied to its section, so it is kept. A layout pass may cut 4.5's.
