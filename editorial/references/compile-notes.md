# References compile notes

Built `book/98-references.md` from the 23 ledgers in this folder (640 entries in total after the 2026-11-06 fixes; 633 before). Nothing in the references comes from outside the ledgers: every author, title, edition, URL and locator is taken from an entry. Where a source has several entries, the bibliography line combines them, using the fullest details any entry recorded (for example, the ISBN from one entry and the publication date from another).

## Counts

- Index (Part 1): 639 entries (323 claims, 75 quotations, 241 further-reading lines) across the front matter, chapters 1 to 21 and the appendices.
- Bibliography (Part 2): 132 sources (26 books, 28 papers and articles, 5 talks, 50 documentation and specification entries, 23 blog posts). They cover every distinct URL in the ledgers.
- Quotation excerpts are cut at 320 characters, so the quoted words show; other excerpts at 150.

## Entries left out or shown without a link

- `ch05-bounded-contexts-and-context-maps-10` (Khononov 2021, ch. 8, "onion and clean architecture as variants of ports and adapters") is left out of the index. Its action is `removed`: the attribution was taken out of the chapter and replaced with the Robert C. Martin 2012 statement, which has its own entry.
- `ch20-events-are-forever-13` is also marked `removed`, but what was removed is the Vaughn Vernon locator. The Evans citation (book ch. 14 and Reference p. 36) is still in the chapter, so the entry stays in the index as an Evans citation.
- `ch07-...-16/17/18` and `ch10-...-04/06/09` quote the book's own sample code, and `ch04-discovering-the-domain-20` (`kept-unlinked`) quotes the reader's compiler output. They appear in the index with the file and sample tag, have no external link, and are left out of the bibliography.

## Conflicts between ledgers and how they were resolved

1. **Kent Beck, *Test Driven Development: By Example*: URL.** `99-appendices-74` links the O'Reilly page `https://www.oreilly.com/library/view/test-driven-development/0321146530/`, which it says it read in a browser after curl got a 403. `ch03-...-02` and `ch03-...-20` record that the same O'Reilly URL "returned 403, could not be verified" and replace it with the InformIT page `https://www.informit.com/store/test-driven-development-by-example-9780321146533`. Their evidence is the InformIT product page plus the publisher's index PDF (Learning Tests, pp. 136-137). **Chosen: InformIT**, as the publisher's page. It has stronger evidence and matches the instruction to link books to the publisher or bookstore. The title follows InformIT ("Test Driven Development", no hyphen). Appendix E.7 now uses the InformIT link and title.
2. **Hohpe and Woolf, *Enterprise Integration Patterns*: URL.** `ch16-integrating-bounded-contexts-09` uses `.../enterprise-integration-patterns-designing-building-and-9780321200686`. Eight other entries, including `99-appendices-38`, use `.../enterprise-integration-patterns-designing-building-9780321200686`, and several of them record a fetch of that page ("Published Oct 10, 2003 by Addison-Wesley Professional"). **Chosen: `...designing-building-9780321200686`.** Since 2026-11-06 chapter 16 and its ledger entry use this slug too, so the alias is gone.
3. **Vernon, *Implementing Domain-Driven Design*: URL and ISBN.** `ch07-aggregates-and-the-decider-21` also links `.../implementing-domain-driven-design-9780133039924`, a second InformIT listing under a different ISBN. Nineteen entries use `.../implementing-domain-driven-design-9780321834577`, and their edition fields give ISBN 978-0-321-83457-7. **Chosen: 9780321834577.** Since 2026-11-06 `ch07-...-21` uses it too, so the alias is gone. The O'Reilly Learning path uses ISBN 9780133039900, and one entry links a section page there. That link is kept as an extra page, not as the book's main link.
4. **Khononov, *Learning Domain-Driven Design*: ISBN.** The ledgers give two ISBNs:
   - 9781098100124: `ch04-...-12`, `ch05-...-33` and `ch11-...-20`.
   - 978-1-098-10013-1 (9781098100131): `ch14-...-06/20/26` and `ch16-...-02/04/20`.

   No evidence field quotes either number. `ch13-...-04/15` record that the metadata came from the O'Reilly Learning public TOC/metadata API "for ISBN 9781098100124", and every publisher URL in the ledgers uses that number. The ch14/ch16 entries rely on third-party pages (schweitzer-online.de, a pageplace.de preview PDF) and do not say where their ISBN came from. **Chosen: 978-1-098-10012-4 (9781098100124)**, since the publisher's own metadata supports it. Both are valid ISBN-13s and may belong to different formats of the same edition, so 9781098100131 is not necessarily wrong. The chapter text does not cite an ISBN, so nothing in the chapters needed to change.
5. **Werner Vogels, "Eventually Consistent": which page.** `ch14-...-02/23` say the queue.acm.org page and the ACM DL returned 403, so they link the author's blog page. `99-appendices-35` linked `https://queue.acm.org/detail.cfm?id=1466448` after reading it in a Playwright browser (evidence: the "Client-side Consistency" section). Both were verified by some method, and the blog page says it was revised for ACM Queue. **Revised 2026-11-06:** the queue.acm.org page still returns 403 and was dropped; the All Things Distributed page (titled "Eventually Consistent - Revisited") is now the primary link everywhere, with the DOI 10.1145/1466443.1466448 (Crossref: *Queue* 6(6)) as the second link.
6. **Pat Helland, "Immutability Changes Everything": venue.** `ch20-events-are-forever-17` notes that the chapter originally cited ACM Queue (2016, `queue.acm.org/detail.cfm?id=2884038`). That page could not be fetched, so the entry was changed to the CIDR 2015 paper. All five entries now cite CIDR 2015. **Chosen: CIDR 2015**, and the Queue URL is not listed.
7. **OCPP 1.6: edition.** `00-front-matter-01` and `ch16-...-13` give "OCPP 1.6 (2015)". `ch02-...-11` cites "edition 2 FINAL, 2017-09-28", checked against a copy of the edition 2 PDF. The two do not conflict, because edition 2 revises the 2015 release. **The bibliography gives both** ("OCPP 1.6, 2015; edition 2 FINAL, 2017-09-28"). It also notes that the official download is behind a form, so the ledgers additionally cite copies in the mobilityhouse/ocpp repository.
8. **Pete Hodgson, "Feature Toggles": date.** `99-appendices-89` gives "09 October 2017". `ch21-...-30` gives "2016, last revised 09 October 2017" and quotes the revision note as evidence. **Chosen: "2016, last revised 9 October 2017"**, which includes both. Appendix E's "(2017)" is the revision date and was left as it is.
9. **Publication years for books published late in the year with the next year's copyright** (Evans 2003/2004, Fowler *Analysis Patterns* 1996/1997, Beck 2002/2003, Vernon *Reactive Messaging Patterns* 2015/2016, Hofer and Schwentner 2021/2022, Cohn 2009/2010, Martin *Clean Code* 2008/2009). The ledgers mix the two years. The bibliography shows both, as "year (published …; copyright …)", using the publication dates the entries recorded. The years in Appendix E and in the chapters are unchanged, because each matches one of the two.
10. **Microsoft Learn URLs with and without `en-us/`.** The SignalR .NET client, SignalR groups and MAUI app-lifecycle pages are cited both ways. Both forms resolve to the same page. The bibliography uses the locale-neutral form, which is the one Appendix E uses. This is a normalization of identical pages, not a conflict of evidence.
11. **Akka.NET source files cited at different refs** (Akka.Cluster.Sharding `reference.conf` at tag 1.5.73, tag 1.5.74 and dev; `akka.conf` at 1.5.73 and dev). These are different versions cited for different claims, not conflicts. Each is listed separately under the akkadotnet/akka.net entry.

## Grouping decisions

- Large documentation sets are one bibliography entry each, listing the pages cited: getakka.net, akkadotnet/akka.net source, Akka.NET release notes/issues/PRs, Microsoft Learn C#, .NET, ASP.NET Core, Aspire and opentelemetry.io. Every page is still linked, both from the bibliography line and from each index line.
- Pattern pages on enterpriseintegrationpatterns.com are listed under the *Enterprise Integration Patterns* book. The PoEAA catalog and the "Money" entry are listed under *Patterns of Enterprise Application Architecture*. The Effective Aggregate Design PDFs are listed under the essay series.
- The Evans passage reproduced on Microsoft Learn (`ch12`: "Eric Evans (quoted by Microsoft Learn)") is listed as an extra link on the Evans book entry, because the ledger attributes it to the book.

## Appendix E

Kept as the annotated reading list. Its introduction now points to `book/98-references.md`, and the Beck link and title follow conflict 1. A check confirmed that all 103 links in Appendix E appear in Part 2 of the references. Every Appendix E entry already had a ledger record (`99-appendices-10` to `-89`).

## Regenerating

The compiler (`compile.py`) and the URL-to-source registry (`registry.py`) are now kept in `editorial/references/compiler/`; run `python3 -I editorial/references/compiler/compile.py .` from the repository root. The compiler checks that every ledger URL maps to a source and that every registry URL appears in some ledger, and it stops if either check fails. If the ledgers change, edit `98-references.md` directly, or rebuild it from the ledgers using the grouping described above.

## 2026-11-06 reference fixes

The adversarial reference check's findings are resolved in `fix-report.md`. Every ledger `text` field now quotes the book's current wording (checked mechanically: every Part 1 excerpt occurs in the chapter it indexes), editorial annotations such as "(added)" and "(was: ...)" were moved out of the `text` fields into `notes`/`action`, quotations recorded as claims were re-kinded to `quote`, and seven entries were added (two runtime messages in ch. 3, the IDDD_Samples `DomainEvent` link and the Knowledge Level quotation in ch. 8, Wlaschin's "Against Railway-Oriented Programming" in ch. 11, Akka.Streams.Kafka in ch. 16 and the OpenTelemetry conversation-id quotation in ch. 17). The registry gained the new URLs.
