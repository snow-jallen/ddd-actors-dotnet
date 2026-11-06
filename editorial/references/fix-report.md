# Reference fix report (2026-11-06)

This report resolves the findings from the adversarial reference check. Each source named below was fetched again today. PDFs were downloaded and searched with `pdftotext`, web pages were fetched with curl, and pages that block curl (O'Reilly, ACM, MIT Press) were opened in a Playwright browser. The changes went to the chapter text, the ledgers and `book/98-references.md`, and to Appendix E where it applied. `98-references.md` is now regenerated from the ledgers by `editorial/references/compiler/compile.py`, so the index and the ledgers cannot drift apart again. A mechanical check confirms that every Part 1 excerpt now appears in the chapter it indexes. The book builds with `tools/build-book.sh`, which produces md, html and epub.

PDF page locators follow the 2015-03 DDD Reference (printed page = PDF page − 7).

## Findings and resolutions

| Finding | Resolution |
|---|---|
| ch04-09 Wlaschin "review the code" quote | **The quote was unverifiable and has been removed.** It is replaced by a quotation verified in the publisher's preface extract, which says the code serves "first as readable documentation of the domain but also as a compilable framework", plus a paraphrase of the ch. 2 extract ("Documenting the Domain") and ch. 5 ("Domain Modeling with Types"). The ledger is now kind=quote, and the pragprog extract URLs were added. |
| ch05-17 Partnership "p. 30" | The book now says the term is "marked with an asterisk ... p. 30; the contents page, p. iii, explains the asterisk as 'New term introduced since the 2004 book'". Both places were verified on PDF pp. 3 and 37. Ledger: kind=quote, locator 'p. iii (contents footnote); heading p. 30'. |
| ch04-13 canvas "cost reduction" | The README lists three roles, and cost reduction now appears only as "a fourth value in the canvas's YAML template", with a link to that YAML (line 18 verified). The ledger URL now includes the YAML, and the index uses the book's wording. |
| ch01-13 Agha primitives | The index was regenerated from the book. "The actor primitives are send, become and letactor" was re-verified in the Cambridge-hosted JFP PDF, §2 "Our actor language". |
| ch01-19 xUnit/MTP | The index and ledger text were regenerated from the book's corrected sentence. |
| ch01-03 OCPP 1.6J | The index and ledger text were regenerated from the book's sentence. |
| ch01-16 Decider isTerminal | The book now reads "an initial state and two core functions ... (Chassaing's Decider also has an `isTerminal` predicate, which this book leaves out)". The post was re-fetched and its record type was confirmed. |
| ch02-04 Armstrong | Ledger: kind=quote, locator §4.3.1 heading and §4.4, pp. 104–107. Re-verified in the thesis PDF. |
| ch02-05 "illusion of an in-memory collection" | The book now adds "p. 17", verified on PDF p. 24. Ledger: kind=quote. |
| ch02-10 Agha "replacement behavior" | Ledger: kind=quote, text taken from the book, and the bitsavers PDF URL added. Verified on PDF p. 50, §3.1.2. |
| ch02-11 OCPP heartbeat | Ledger: kind=quote, locator '§4.6 Heartbeat, p. 35 (incl. the JSON-over-WebSocket note)'. Both passages were verified on p. 35 of edition 2 FINAL. |
| ch02-13 akka.conf phase comment | Ledger: kind=quote. The text is line 1097 at tag 1.5.73, re-verified. |
| ch02-15/16 AK1005 and Hewitt | Ledger and index texts were regenerated. The AK1005 page and the Hewitt section heading were re-verified. |
| ch03-04 value object and others | All ch03 index excerpts were regenerated from the book. |
| ch03-02 Beck pp. 136–137 | **Kept: the page numbers are verified.** The publisher's index PDF (ptgmedia.pearsoncmg.com/.../beckindex.pdf) lists "Learning Tests, 136–137". The ledger verification field records this. |
| ch03 unledgered runtime messages | Added ch03-22 (`SwitchExpressionException`, CoreLib Strings.resx L4026) and ch03-23 (`UnionAmbiguousJsonValueType`, System.Text.Json Strings.resx L846). Both links are pinned to dotnet/runtime @ 6cf1b61 and both strings were verified. |
| ch04-11 IDDD p. 52 | **Kept: the page number is verified.** The index of the InformIT sample-pages PDF has "Supporting Subdomains ... defined, 52". |
| ch04-03 Big Picture phases | The book now names Brandolini's ch. 4 phases as the Leanpub TOC gives them (Kick-off ... Pick your problem). Pivotal events are credited to the ddd-crew glossary and quoted from it ("the few most significant events in the flow"), with a note that ch. 4 has no such phase. Hotspots are mapped to his "Problems and opportunities" and "Pick your problem". |
| ch04-05 policy glossary quote | Ledger: kind=quote, and the index now shows the quotation. |
| ch05 quotes (01, 08, 11, 14, 15, 18, 19, 20, 23, 27) | Ledger: kind=quote, with page locators p. 2, Palermo, p. 10, Conway, p. 29, p. 31, Foote/Yoder, p. 38, the Khononov blog and p. 49. Every quote was re-verified verbatim, and the ch05 index was regenerated. |
| ch05-23 Khononov | The index now quotes the 2020 blog post, the work it links. kind=quote. |
| ch05-09/26 Clean, Responsibility Layers | The index was regenerated: "The Dependency Rule" and "cast natural strata ..." (p. 50). |
| ch05-30/32/33/36 further reading | The index was regenerated from book lines 721–728. The ch05-30 URL now points to the InformIT page the book links. |
| ch06-12 / ch06-18 | Ledger: kind=quote with #page=18 ('Entities', p. 11) and #page=29 ('Assertions', p. 22). Both verified. |
| ch06-21 "entity-think" | The term was verified in a browser on O'Reilly's ch. 6 preview: "Although often overshadowed by entity-think, Value Objects are...". Ledger: kind=quote, with the O'Reilly URL added. |
| ch06-13 | Ledger and index now credit Evans with the root-only rule only. Locator: DDD Ref 'Aggregates', p. 16 (PDF p. 23), verified. |
| ch06-23 Cunningham | The index excerpt was regenerated and the false quotation marks are gone. |
| ch07-03 Vernon whose job | Text regenerated. The book and the ledger now link Vernon_2011_2.pdf#page=3, verified on p. 3. |
| ch07-08 Declarative Design | The book now quotes "as a kind of executable specification" and cites DDD Ref p. 25 (#page=32). Ledger: kind=quote. |
| ch07-11 Decider fold | Text regenerated. Locator: section "Fold", with the code line recorded in evidence. |
| ch07-12 seven elements | Text regenerated. |
| ch07-13 max-min | The book and the ledger now give pp. 524–527 (algorithm on p. 527, verified). |
| ch07-15 Vernon rule 1 | Text is now "Model true invariants in consistency boundaries.", with locator Part I, p. 3. |
| ch07-21 IDDD ch. 10/11 | Text regenerated, the duplicate URL removed, and the TOC locator set. |
| ch08-01, 10, 14, 16 | Ledger: kind=quote with page locators (pp. 13, 8, 19, 22). All verified. |
| ch08 Knowledge Level quote | Added ch08-26: DDD Ref p. 51 (#page=58), verified. |
| ch08 IDDD_Samples DomainEvent | Added ch08-25, with both the master URL and a URL pinned to commit 9b27b11. `public Date occurredOn();` verified. |
| ch08-04 domain service | The book now quotes "not a natural responsibility of an entity or value object" and cites DDD Ref p. 14 (#page=21). Ledger: kind=quote. |
| ch08-05 Specification uses | Text regenerated (paper names selection, validation, building to order). Locator: "Introduction". |
| ch08-07 Brandolini slide | Ledger text is now the verbatim "Whenever [Event] then [Command]". Slide 62 was verified in the Speaker Deck transcript. |
| ch09-02/17, ch11-30 Khononov pages | **The unverified pp. 164–165 were removed** from the book (ch09 further reading) and from all three locators. |
| ch09-04 Dan North | The book's link and section now point to #atm-example, 'BDD provides a "ubiquitous language" for analysis'. The template was verified there. |
| ch09-09 Hughes | Ledger: kind=quote, locator "abstract". Verified in the FMCAD PDF. |
| ch09-13 xUnit test smells | Text regenerated. The three xunitpatterns.com URLs were added to the ledger and the bibliography, and all return 200. |
| ch10-07 serializer ids | Text regenerated. "0 - 40 is reserved by Akka.NET itself" and "ID range 1-100" were re-verified. |
| ch10-13 deprecated plugins | Text regenerated. NuGet registration shows PostgreSql and SqlServer 1.5.37 deprecated as Legacy (alternative Akka.Persistence.Sql) and Sqlite 1.5.39 not deprecated. |
| ch10-14 provider name quote | Ledger: kind=quote, and the README is pinned at commit 623effa8, line 265 (verified). |
| ch11-06 Helland log | Text regenerated with the exact quotation. Ledger: kind=quote. Verified on CIDR15 p. 1. |
| ch11-17 Wlaschin ARoP | The book now quotes "are not part of any business process" and attributes it to the post, linked to #when-should-you-use-result. Added ch11-33, and the bibliography gained the post. |
| ch11-20 Khononov catalogue | Text regenerated. Khononov is credited only with the catalogue. |
| ch12-13 Vernon Application Services | **The O'Reilly link is kept.** It opens "Application Services" in a browser, and curl's 403 is a bot block. The book adds "p. 521" (sample-pages TOC), and the ledger also lists the sample-pages PDF. |
| ch12-17 Vernon one aggregate per transaction | Ledger: kind=quote. **The locator is corrected to p. 4**, where pdftotext places the sentence, after the "Rule: Model True Invariants..." heading that begins on p. 3. The finding had said p. 3. |
| ch12-18 Helland entity key | Ledger: kind=quote, section 1 (PDF p. 3). The book's straight quotes were left as they are, since that change is cosmetic. |
| ch12-16 Idempotent Receiver | Text regenerated and the EIP pattern URL added. |
| ch12-21 Akka.Aspire | Text regenerated. It now mentions the discovery backends, Kubernetes included. |
| ch13-02 Young | The book now attributes the quotation in the prose to the 2010 post "CQRS, Task Based UIs, Event Sourcing agh!". CQRS Documents p. 17 is cited only for the same idea. Both were verified. |
| ch13-10 Helland retries | Ledger: kind=quote. |
| ch14-23 Vogels | Ledger and index texts now carry book wording only. **The All Things Distributed URL is now the primary link** in ch14, Appendix E.3, the ledgers and the bibliography. The queue.acm.org link (403) was replaced by DOI 10.1145/1466443.1466448 (Crossref: Queue 6(6); the browser lands on queue.acm.org). The blog version is named "Eventually Consistent - Revisited". |
| ch14 editorial notes in the index | Every "(was: ...)", "(added)", "locator added" and URL-change note was removed from the ledger `text` fields and recorded in `notes`/`action`. |
| ch15-08 Sagas quote | Ledger: kind=quote, section 1 "Introduction". Verified on p. 2 of the Cornell PDF. |
| ch15-05/04 | Text regenerated from the book. "in effect" is kept. |
| ch16-06/01 Vernon | Text regenerated (ch. 8 notification log; ch. 13 section). |
| ch16-09 EIP slug | ch16 line 316 and its ledger entry now use the canonical `...designing-building-9780321200686`. The registry alias was removed. |
| ch16 Akka.Streams.Kafka | The book now links `KafkaProducer.PlainSink` in the README at tag 1.5.73 (verified). Added ch16-25 and a bibliography entry. |
| ch17-02 AddTracing | The book was reworded to match the page: AddTracing registers listeners and rules, and ActivitySourceFactory creates sources through DI. The logging-filter analogy was removed, and the page's warning about exporter listeners was added. |
| ch17-17 OTel consumer spans | Ledger: kind=quote, recording both fragments and both anchors (verified). |
| ch17 conversation_id | Added ch17-30 (quote, #process-span), verified. |
| ch17-21 Phobos per-node | Ledger: kind=quote, from the 1.0.0 "Simplified Pricing Model". |
| ch17-20 Sdkbin feed | Text regenerated from the book. The index no longer claims "not on nuget.org". |
| ch17-19 Phobos release notes | The book now limits "release notes" to what they state: 2.12.1 upgrades to Akka.NET 1.5.71, 2.0 moved to OpenTelemetry, and 2.12.0 added Akka.Streams propagation. Per-receive spans and remoting propagation are quoted from the Phobos tracing documentation, which was added to the ledger and the bibliography. The unsupported Tell/Ask/PipeTo/sharding list was dropped. |
| ch17-01 Cockburn | Text regenerated (the "Intent" paraphrase, re-verified). |
| 99-appendices-06 Repository page | Appendix B.3 and the index now give p. 17 (PDF p. 24). |
| 99-appendices-05 formalisms heading | The B.3 row now reads "Draw on Established Formalisms, When You Can", as in the InformIT TOC. |
| 99-appendices-10 Cunningham E.1 | Index regenerated with the verified quotation. |
| ch18-01 Evans relentlessly | Ledger: kind=quote. Locator DDD Ref p. 4 (PDF p. 11). |
| ch19-07 Evans p. 3 | The book now quotes "relentlessly in all communication within the team and in the code" with p. 4 (#page=11). The index and ledger were updated. |
| ch19-08 Wlaschin ch. 4 | Index regenerated (ch. 6 and ch. 7). |
| ch19-03 IDDD p. 92 | **Kept: the page number is verified.** The sample-pages index has "Customer-Supplier Development, Bounded Context relationships, 92, 94". |
| ch21-03 Khononov p. 57 | **The unverified page number was removed**, keeping ch. 4, section "Open-Host Service". The section was confirmed in the O'Reilly TOC in a browser. |
| ch20-05 IReadEventAdapter | The book now says the docs show `FromJournal` on `IEventAdapter` and links EventAdapters.cs#L75 at 1.5.73 (verified). The ledger URL was added. |
| ch18-07 pivotal event "new phase" | **The unsupported gloss was removed** from ch18, and the matching ch04 wording was removed too. |
| ch21-18 feature management title | The book, ledger and bibliography now use ".NET Feature Flag Management" (Azure App Configuration). |
| 99-appendices-47 Young versioning | Index regenerated ("copy-transform"). Locator: "Basic Type Based Versioning" > "Define a Version of an Event", "Weak Schema", "Cheating" > "Copy-Transform". |
| ch21-28 / ch18-16 Petabridge titles | Index regenerated with the posts' real titles. |
| 99-appendices-70 SignalR | Index regenerated ("handling a lost connection"). |
| 98-references stale snippets (ch18–21, appendices and all other chapters) | 352 stale ledger `text` fields were regenerated from the current chapter text, and the whole index was recompiled. |

## URLs re-fetched

All 94 URLs touched by these fixes were fetched again: 87 returned HTTP 200. The other seven return 403 to curl, so they were opened in a browser instead:

- the O'Reilly IDDD ch. 6 and ch. 14 pages, and the O'Reilly Khononov ch. 4 page
- dl.acm.org Sagas
- doi 10.1145/1466443.1466448
- doi 10.7551/mitpress/1086.001.0001 (Cloudflare page; Crossref confirms *Actors*, Agha, 1986)
- the Khononov book landing page

## Not resolved / caveats

- ch08-07: the strict text check flags it only because the book escapes the brackets as `\[Event\]` inside a link. The wording itself matches slide 62.
- Two ch11 index excerpts use bracketed editorial insertions (`[SnapshotSequenceNr]`, `[hand-off stop message]`). They are deliberately not verbatim.
- ch12-18: the optional change to the paper's curly “Entities” quotes was not made.
- The ch08 supple-design table still uses the DDD Reference's heading "Drawing on Established Formalisms" (p. 26). That heading is correct for the Reference, which the table cites. The book's heading differs.
