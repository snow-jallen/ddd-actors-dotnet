# Domain-Driven Design with the Actor Model on .NET

*Functional core, actor shell with C# 15, .NET 11 and Akka.NET*

This folder holds everything that makes up the book: the manuscript, the companion sample repo it quotes, the research it rests on, the editorial contracts every chapter was written against, and the tools that build it.

The book teaches a working C# developer to model a real domain with Domain-Driven Design, implement it as a pure functional core inside an Akka.NET actor shell, test it at every level, observe it in production, serve it to an offline-capable .NET MAUI app, and ship and evolve it without breaking history. The running example is Ohmline, a fictional operator of public EV charge points. Every company in the book and the sample is fictional.

## Layout

| Path | What it is |
|---|---|
| `book/` | The manuscript in Markdown: `00-front-matter.md`, `ch01-*.md` to `ch21-*.md`, `99-appendices.md` |
| `sample/` | The companion sample repo, `Ohmline`: its own git repository, with one annotated tag per book section (`chNN-sMM`) and side branches (`wrong/*`, `alt/*`, `upgrade/akka-1.6`). Every code listing in the book is copied from it. See `sample/README.md` |
| `research/` | Research notes with verified versions and spike code (`akka-core.md`, `csharp-dotnet11.md`, `ddd-canon.md`, `mobile.md`, `operations.md`, `telemetry.md`, `testing.md`). The spikes use other names (`Order`, `Shop`) and are never quoted in the book |
| `editorial/` | The binding contracts: `outline.md` and `outline.json` (chapters, sections, tags), `style-guide.md` (voice, chapter anatomy, code-quoting rule, glossary), `sample-architecture.md` (repo layout, pinned versions, build and git rules); plus the working record: `code-notes/`, `review-log.md`, `errata.md`, `progress.md`, `queued-jobs.md` |
| `tools/` | `build-book.sh` (assembles the book), `env.sh` (selects the book's local .NET 11 SDK, Android SDK and JDK), `dotnet-install.sh`, `progress.sh` (progress meter used while the book was produced) |
| `build/` | Output of `tools/build-book.sh`; generated, safe to delete |
| `.dotnet/`, `.android-sdk/`, `.jdk/` | Local toolchains used to verify the sample (the .NET 11 RC3 SDK with the `maui-android` workload, an Android SDK, a JDK); not part of the book |

## Reading the book

Read `book/` in file order, front matter first. The book is meant to be read with the sample open:

```bash
cd sample
git tag -n --sort=version:refname       # every section tag, in book order
git checkout ch07-s04                   # the sample at the end of section 7.4
git diff ch07-s03 ch07-s04 --stat       # what section 7.4 changed
dotnet test --solution Ohmline.slnx
git checkout main
```

The front matter lists what to install (the .NET SDK named in `sample/global.json`, Podman or Docker from Chapter 12, the Aspire CLI, and for Chapter 19 only the MAUI workload with an Android SDK and a JDK). Appendix C maps every tag to its section.

## Building the book

```bash
tools/build-book.sh
```

The script concatenates the front matter, the 21 chapters in order (with part headings taken from `editorial/outline.json`) and the back matter into `build/book.md`. If `pandoc` is on the `PATH`, it also writes `build/book.html` (standalone, with a table of contents; Mermaid diagrams are rendered in the browser from a pinned CDN script) and `build/book.epub` (with a table of contents; Mermaid blocks appear as source). Set `PANDOC=/path/to/pandoc` to use a specific binary, or `BOOK_NO_MERMAID=1` for HTML without the script.

## Building and verifying the sample

From `sample/`, with the SDK in `global.json` installed (on this machine, `source tools/env.sh` from the book folder selects it):

```bash
dotnet build Ohmline.slnx -warnaserror
dotnet test --solution Ohmline.slnx                  # Integration and System tests need Podman or Docker
dotnet build mobile/Ohmline.Mobile.slnx -warnaserror # from ch19-s04; needs the maui-android workload
tools/verify-tags.sh                                 # build and test every section tag in a temporary worktree
```

`editorial/sample-architecture.md` §11 defines when a tag is done; `sample/mobile/README.md` explains the Android toolchain.

## Licence

The sample is released under the MIT licence (`sample/LICENSE`).
