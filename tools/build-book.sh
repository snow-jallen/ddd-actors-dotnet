#!/usr/bin/env bash
# Assemble the book into build/book.md and, if pandoc is available, build/book.html and
# build/book.epub, each with a table of contents.
#
#   tools/build-book.sh
#   PANDOC=/opt/pandoc/bin/pandoc tools/build-book.sh   # a specific pandoc binary
#   BOOK_NO_MERMAID=1 tools/build-book.sh               # HTML without the Mermaid script
#
# Order: book/00-front-matter.md, then book/chNN-*.md in chapter order with a part heading
# before the first chapter of each part (parts from editorial/outline.json), then every other
# numbered back-matter file (book/98-*.md, book/99-appendices.md) in name order.
#
# Needs bash 4 or later (macOS ships 3.2) and python3 to read outline.json.
set -euo pipefail
shopt -s nullglob

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
book="$root/book"
out="$root/build"
md="$out/book.md"
title="Domain-Driven Design with the Actor Model on .NET"
subtitle="Functional core, actor shell with C# 15, .NET 11 and Akka.NET"
mermaid_version="11.4.1"

die() { printf 'build-book: %s\n' "$*" >&2; exit 1; }

[[ -f "$book/00-front-matter.md" ]] || die "missing book/00-front-matter.md"
[[ -f "$root/editorial/outline.json" ]] || die "missing editorial/outline.json"

# Chapter files in order; every chapter in outline.json must exist exactly once.
chapters=()
for path in "$book"/ch[0-9][0-9]-*.md; do chapters+=("${path##*/}"); done
expected="$(python3 -I -c 'import json,sys; print(len(json.load(open(sys.argv[1]))["chapters"]))' \
  "$root/editorial/outline.json")"
(( ${#chapters[@]} == expected )) ||
  die "found ${#chapters[@]} chapter files in book/, outline.json lists $expected"

# "NN<TAB>Part title" for every chapter, from outline.json.
declare -A part_of
while IFS=$'\t' read -r number part; do
  part_of["$number"]="$part"
done < <(python3 -I -c '
import json, sys
for c in json.load(open(sys.argv[1]))["chapters"]:
    print("%02d\t%s" % (c["number"], c["part"]))
' "$root/editorial/outline.json")

# Back matter: every numbered file except the front matter, in name order (98-references.md
# if present, then 99-appendices.md).
back=()
for path in "$book"/[0-9][0-9]-*.md; do
  [[ "${path##*/}" == 00-* ]] || back+=("${path##*/}")
done

mkdir -p "$out"
append() { cat "$1"; printf '\n\n'; }

{
  append "$book/00-front-matter.md"
  current_part=""
  for file in "${chapters[@]}"; do
    number="${file:2:2}"
    part="${part_of[$number]:-}"
    [[ -n "$part" ]] || die "$file: chapter $number is not in outline.json"
    if [[ "$part" != "$current_part" ]]; then
      printf '# %s\n\n' "$part"
      current_part="$part"
    fi
    append "$book/$file"
  done
  for file in "${back[@]}"; do
    append "$book/$file"
  done
} > "$md"

words="$(wc -w < "$md" | tr -d ' ')"
printf 'build-book: %s (%s files, %s words)\n' "${md#"$root"/}" \
  "$(( 1 + ${#chapters[@]} + ${#back[@]} ))" "$words"

pandoc="${PANDOC:-$(command -v pandoc || true)}"
if [[ -z "$pandoc" ]]; then
  printf 'build-book: pandoc not found; skipping HTML and EPUB (set PANDOC or install pandoc)\n'
  exit 0
fi

common=(
  --from=commonmark_x
  --toc --toc-depth=2
  --metadata "lang=en"
)

# Mermaid blocks become <pre class="mermaid">; the script renders them in the browser.
header="$out/.mermaid-header.html"
html_extra=()
if [[ "${BOOK_NO_MERMAID:-}" != "1" ]]; then
  cat > "$header" <<EOF
<script type="module">
  import mermaid from "https://cdn.jsdelivr.net/npm/mermaid@${mermaid_version}/dist/mermaid.esm.min.mjs";
  for (const block of document.querySelectorAll("pre.mermaid")) {
    block.textContent = block.textContent;
  }
  mermaid.initialize({ startOnLoad: false, securityLevel: "strict" });
  await mermaid.run({ querySelector: "pre.mermaid" });
</script>
EOF
  html_extra+=(--include-in-header="$header")
fi

# The front matter's first heading is the title page, so HTML gets only a <title>.
"$pandoc" "${common[@]}" --standalone --metadata "pagetitle=$title" "${html_extra[@]}" \
  -o "$out/book.html" "$md"
printf 'build-book: build/book.html\n'

"$pandoc" "${common[@]}" --metadata "title=$title" --metadata "subtitle=$subtitle" \
  -o "$out/book.epub" "$md"
printf 'build-book: build/book.epub\n'

# ---------------------------------------------------------------------------------------------
# PDF: a print-styled HTML (title page, then contents, page numbers, a page break before every
# part, chapter and appendix) printed by a headless Chromium-family browser, so the Mermaid
# diagrams are rendered exactly as in the HTML. Needs network access to load Mermaid from the CDN.
#
#   BOOK_NO_PDF=1 tools/build-book.sh                      # skip the PDF
#   BOOK_PDF_BROWSER=/usr/bin/chromium tools/build-book.sh # a specific browser
#   BOOK_PDF_PAGE="7in 9.25in" tools/build-book.sh         # page size (default 7in x 9.25in)
if [[ "${BOOK_NO_PDF:-}" == "1" ]]; then
  exit 0
fi
browser="${BOOK_PDF_BROWSER:-}"
if [[ -z "$browser" ]]; then
  for candidate in google-chrome-stable google-chrome chromium chromium-browser microsoft-edge; do
    if command -v "$candidate" >/dev/null; then browser="$(command -v "$candidate")"; break; fi
  done
fi
if [[ -z "$browser" ]]; then
  printf 'build-book: no Chrome/Chromium/Edge found; skipping PDF (set BOOK_PDF_BROWSER)\n'
  exit 0
fi

page_size="${BOOK_PDF_PAGE:-7in 9.25in}"
print_header="$out/.print-header.html"
print_html="$out/.book-print.html"
cat > "$print_header" <<EOF
<style>
  @page {
    size: ${page_size};
    margin: 0.75in 0.7in 0.8in 0.7in;
    @bottom-center { content: counter(page); font: 9pt "Inter", "Helvetica Neue", Arial, sans-serif; color: #555; }
  }
  @page :first { @bottom-center { content: none; } }
  html { font-size: 10.5pt; }
  body {
    max-width: none; margin: 0; padding: 0; color: #111; background: #fff;
    font-family: "Source Serif 4", "Noto Serif", Georgia, "DejaVu Serif", serif; line-height: 1.45;
    hyphens: auto; text-align: left;
  }
  h1, h2, h3, h4 { font-family: "Inter", "Helvetica Neue", Arial, sans-serif; line-height: 1.2;
    break-after: avoid; page-break-after: avoid; }
  h1 { font-size: 1.9em; margin: 0 0 1em; break-before: page; page-break-before: always; }
  h2 { font-size: 1.35em; margin-top: 1.6em; }
  h3 { font-size: 1.12em; margin-top: 1.3em; }
  h4 { font-size: 1em; }
  p, li { orphans: 3; widows: 3; }
  a { color: inherit; text-decoration: none; }
  code { font-family: "JetBrains Mono", "DejaVu Sans Mono", monospace; font-size: 0.86em; }
  pre { font-size: 8.3pt; line-height: 1.35; white-space: pre-wrap; overflow-wrap: anywhere;
    background: #f5f5f4; border-left: 3px solid #c9c9c4; padding: 0.45em 0.7em; margin: 0.8em 0; }
  pre code { font-size: inherit; white-space: inherit; }
  div.sourceCode { background: none; margin: 0.8em 0; }
  pre.mermaid { background: none; border: 0; padding: 0; text-align: center; white-space: normal;
    break-inside: avoid; page-break-inside: avoid; }
  pre.mermaid svg { max-width: 100% !important; height: auto; }
  div.diagram { text-align: center; margin: 1em 0; break-inside: avoid; page-break-inside: avoid; }
  div.diagram svg { max-width: 100% !important; max-height: 7in; height: auto; }
  pre.mermaid-error { border-left-color: #b00020; }
  blockquote { margin: 1em 0; padding: 0.2em 0 0.2em 1em; border-left: 3px solid #8a8a85; color: #222; }
  table { border-collapse: collapse; width: 100%; font-size: 8.8pt; margin: 1em 0; break-inside: auto; }
  tr { break-inside: avoid; }
  th, td { border: 1px solid #c9c9c4; padding: 3px 5px; vertical-align: top; }
  th { background: #efefec; }
  img, svg { max-width: 100%; }
  hr { border: 0; border-top: 1px solid #ccc; }
  /* Title page: the front matter's first heading and the lines up to the preface. */
  .title-page { min-height: 7in; display: flex; flex-direction: column; justify-content: center; }
  .title-page h1 { font-size: 2.6em; break-before: auto; page-break-before: auto; margin-bottom: 0.3em; }
  .title-page h3 { font-weight: 400; font-size: 1.3em; margin-top: 0; color: #333; }
  .title-page p { font-size: 0.95em; color: #333; }
  nav#TOC { break-before: page; page-break-before: always; font-family: "Inter", "Helvetica Neue", Arial, sans-serif; font-size: 9.5pt; }
  nav#TOC::before { content: "Contents"; display: block; font-size: 1.9em; font-weight: 700; margin-bottom: 0.8em; }
  nav#TOC ul { list-style: none; padding-left: 0; }
  nav#TOC > ul > li { margin-top: 0.45em; font-weight: 600; }
  nav#TOC ul ul { padding-left: 1.4em; font-weight: 400; }
  header#title-block-header { display: none; }
</style>
<script type="module">
  // Title page first, then the contents, then the preface onward.
  const first = document.querySelector("body > h1");
  const toc = document.getElementById("TOC");
  if (first) {
    const page = document.createElement("section");
    page.className = "title-page";
    first.before(page);
    let node = first;
    while (node && !(node !== first && node.tagName === "H1")) {
      const next = node.nextElementSibling;
      page.append(node);
      node = next;
    }
    if (toc) page.after(toc);
  }
  // Render diagrams one at a time so a bad diagram cannot disturb the others; a diagram that
  // fails is printed as its source with a visible marker (grep the PDF for "DIAGRAM-ERROR").
  const { default: mermaid } = await import("https://cdn.jsdelivr.net/npm/mermaid@${mermaid_version}/dist/mermaid.esm.min.mjs");
  mermaid.initialize({ startOnLoad: false, securityLevel: "strict", theme: "neutral" });
  let n = 0;
  for (const block of document.querySelectorAll("pre.mermaid")) {
    const source = block.textContent;
    const id = "diagram-" + (++n);
    try {
      await mermaid.parse(source);
      const { svg } = await mermaid.render(id, source);
      const figure = document.createElement("div");
      figure.className = "diagram";
      figure.innerHTML = svg;
      block.replaceWith(figure);
    } catch (error) {
      document.getElementById(id)?.remove();
      document.getElementById("d" + id)?.remove();
      block.className = "mermaid-error";
      block.textContent = "DIAGRAM-ERROR: " + (error?.message ?? error) + "\n\n" + source;
    }
  }
  document.documentElement.dataset.ready = "1";
</script>
EOF

"$pandoc" "${common[@]}" --standalone --metadata "pagetitle=$title" \
  --include-in-header="$print_header" -o "$print_html" "$md"

profile="$(mktemp -d)"
trap 'rm -rf "$profile"' EXIT
"$browser" --headless=new --disable-gpu --no-first-run --no-default-browser-check \
  --user-data-dir="$profile" --no-pdf-header-footer --generate-pdf-document-outline \
  --run-all-compositor-stages-before-draw --virtual-time-budget="${BOOK_PDF_WAIT_MS:-120000}" \
  --print-to-pdf="$out/book.pdf" "file://$print_html" 2>/dev/null ||
  die "PDF rendering failed with $browser"
[[ -s "$out/book.pdf" ]] || die "PDF rendering produced no output"
printf 'build-book: build/book.pdf\n'
