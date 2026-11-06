"""Compile book/98-references.md from editorial/references/*.json and registry.py."""
import json, os, re, sys, collections

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from registry import WORKS, ALIASES, CATS, CAT_TITLES

ROOT = sys.argv[1]
REFS = os.path.join(ROOT, "editorial", "references")
BOOK = os.path.join(ROOT, "book")
OUT = os.path.join(BOOK, "98-references.md")

URL_RE = re.compile(r"https?://[^\s;]+")

def norm(u):
    u = u.strip()
    u = re.sub(r"^https?://", "", u)
    u = re.sub(r"^www\.", "", u)
    u = u.split("#")[0].rstrip("/")
    return u.lower()

# ---------------------------------------------------------------- load ledgers
order = ["00-front-matter"] + sorted(
    f[:-5] for f in os.listdir(REFS) if f.startswith("ch") and f.endswith(".json")) + ["99-appendices"]
ledgers = {name: json.load(open(os.path.join(REFS, name + ".json"))) for name in order}

# original-case URL for each key (first seen, fragment stripped)
display = {}
for name in order:
    for e in ledgers[name]:
        for u in URL_RE.findall(str(e.get("url", ""))):
            display.setdefault(norm(u), u.split("#")[0])

# ---------------------------------------------------------------- registry index
part_of = {}      # key -> (work, label)
for w in WORKS:
    for key, label in w["parts"]:
        assert key not in part_of, "duplicate key " + key
        part_of[key] = (w, label)

errors = []
for key in display:
    if key not in part_of and key not in ALIASES:
        errors.append("unmapped ledger URL: " + key)
for key in part_of:
    if key not in display:
        errors.append("registry URL not in any ledger: " + key)
for a, (c, _) in ALIASES.items():
    if a not in display: errors.append("alias not in ledger: " + a)
    if c not in part_of: errors.append("alias target not in registry: " + c)
if errors:
    print("\n".join(errors)); sys.exit(1)

def resolve(u):
    """ledger URL -> (work, label, url to link)"""
    k = norm(u)
    if k in ALIASES:
        ck = ALIASES[k][0]
        w, label = part_of[ck]
        return w, label, display[ck]
    w, label = part_of[k]
    return w, label, u

# ---------------------------------------------------------------- markdown helpers
def esc(s):
    """Escape Markdown outside code spans; keep code spans balanced."""
    s = s.replace("\n", " ")
    parts = s.split("`")
    if len(parts) % 2 == 0:          # unbalanced backtick: escape the last one
        parts[-2] = parts[-2] + "\\`" + parts[-1]
        parts = parts[:-1]
    out = []
    for i, p in enumerate(parts):
        if i % 2 == 0:
            p = p.replace("*", "")
            p = re.sub(r"([\\_\[\]<>|])", r"\\\1", p)
        out.append(p)
    return "`".join(out)

def excerpt(text, limit=150):
    t = " ".join(str(text).split())
    t = t.replace("``", "")
    t = re.sub(r"\[([^\]]+)\]\([^)]*\)", r"\1", t)   # keep link text, drop the target
    if len(t) <= limit:
        return t
    cut = t[:limit]
    if " " in cut[limit // 2:]:
        cut = cut[:cut.rindex(" ")]
    cut = cut.rstrip(" ,;:.(")
    if cut.count("`") % 2:
        cut = cut[:cut.rindex("`")].rstrip()
    return cut + " …"

def section_label(s):
    s = str(s).strip()
    m = re.match(r"^((?:[A-E]|\d+)\.\d+(?:\.\d+)*)\b", s)
    return m.group(1) if m else s

EXCLUDE = {"ch05-bounded-contexts-and-context-maps-10"}  # attribution removed from the book

GENERIC = {"GitHub", "Microsoft Learn", "NuGet", "GitHub gist", "Internet Archive copy", "Destroy All Software",
           "Cognitect blog", "Mechanical Sympathy", "Jane Street blog", "Petabridge blog", ".NET Blog",
           "Google Research", "authors' site", "author's site", "talk page", "repository", "repository and README",
           "MIT DSpace record", "PDF", "NASA (PDF)", "Ink & Switch essay", "IJCAI proceedings (PDF)",
           "PDF (erlang.org)", "ACM Digital Library", "IEEE (DOI)", "ACM (DOI)", "ACM Queue",
           "Journal of Functional Programming (DOI)", "MIT Press Direct (DOI)", "online edition",
           "FMCAD 2011 (PDF)", "slides (Speaker Deck)", "CIDR 2015 (PDF)", "Destroy All Software"}

def skip_label(label):
    return (not label or label.startswith("publisher page") or label in GENERIC
            or re.fullmatch(r"[\w.\-]+\.[a-z]{2,}", label) is not None)

KIND = {"quote": "Quote", "claim": "Claim", "further-reading": "Further reading"}

def chapter_title(name):
    path = os.path.join(BOOK, name + ".md")
    if name == "00-front-matter":
        return "Front matter"
    if name == "99-appendices":
        return "Appendices"
    with open(path) as f:
        for line in f:
            if line.startswith("# "):
                return line[2:].strip()
    return name

# ---------------------------------------------------------------- Part 1
out = []
out.append("# References\n")
out.append("This section lists every source the book quotes, paraphrases or recommends, as recorded in the "
           "editorial reference ledgers after each citation was checked against the source. Part 1 lists the "
           "citations chapter by chapter with the section that makes them; Part 2 lists each source once. "
           "Appendix E keeps a shorter, annotated reading list.\n")
out.append("## Part 1. Index of references by chapter\n")
out.append("Each line gives the section, the kind of citation, a short excerpt of the book's wording, the "
           "source with a link, and where in the source to look. Quotations from the book's own sample code "
           "are listed with their file and tag and have no external link.\n")

cited_in = collections.defaultdict(set)
counts = collections.Counter()
excluded = []
index_count = 0
per_chapter = {}
for name in order:
    entries = ledgers[name]
    lines = []
    for e in entries:
        act = (e.get("action") or "").lower()
        if e["id"] in EXCLUDE:
            excluded.append(e["id"]); continue
        urls = URL_RE.findall(str(e.get("url", "")))
        links = []
        for u in urls:
            w, label, link = resolve(u)
            cited_in[w["key"]].add(name)
            lab = w["short"] + ("" if skip_label(label) else ", " + label)
            links.append("[%s](%s)" % (lab, link))
        if links:
            src = "; ".join(links)
        else:
            title = str(e.get("title", ""))
            if "/" in title or title.endswith(".cs"):
                title = "`" + title.replace("`", "") + "`"
            else:
                title = esc(title)
            src = esc(str(e.get("author", ""))) + ", " + title
            m = re.search(r"git show (\S+)", str(e.get("url", "")))
            if m:
                src += " at sample tag `" + m.group(1) + "`"
            src += " (no external source)"
        loc = str(e.get("locator") or "").strip()
        sec = section_label(e.get("section", ""))
        kind = KIND.get(e.get("kind"), e.get("kind"))
        head = "**%s**" % esc(sec) if sec.lower() == kind.lower() else "**%s** · %s" % (esc(sec), kind)
        line = "- %s · “%s” — %s" % (head, esc(excerpt(e.get("text", ""), 320 if e.get("kind") == "quote" else 150)), src)
        if loc:
            line += ". Locator: " + esc(loc.rstrip("."))
        line += "."
        lines.append(line)
        counts[e.get("kind")] += 1
    per_chapter[name] = len(lines)
    index_count += len(lines)
    out.append("### " + chapter_title(name) + "\n")
    out.append("\n".join(lines) + "\n")

# ---------------------------------------------------------------- Part 2
def chap_ref(name):
    if name == "00-front-matter": return "front matter"
    if name == "99-appendices": return "appendices"
    return "ch. %d" % int(name[2:4])

out.append("## Part 2. Bibliography\n")
out.append("Each source appears once, grouped by kind. Books link to the publisher's or bookstore's page; "
           "further links point to the pages, chapters or files the book cites. \"Cited in\" names the "
           "chapters whose citations use the source.\n")
bib_counts = collections.Counter()
for cat in CATS:
    works = [w for w in WORKS if w["cat"] == cat and w["key"] in cited_in]
    def sortkey(w):
        if cat == "doc":
            return (w["author"].lower(), w["title"].lower())
        if w["key"] == "nasa-mco":
            return ("nasa", "", "")
        first = re.split(r",| and |;| \(| / ", w["author"])[0].strip()
        return (first.split()[-1].lower(), first.lower(), w["title"].lower())
    works.sort(key=sortkey)
    out.append("### " + CAT_TITLES[cat] + "\n")
    items = []
    for w in works:
        first_key, first_label = w["parts"][0]
        title = w["title"]
        s = "- %s. [*%s*](%s). %s." % (w["author"].rstrip("."), title, display[first_key], w["details"].rstrip("."))
        rest = w["parts"][1:]
        if cat == "book":
            s = "- %s. *%s*. %s. [%s](%s)" % (w["author"].rstrip("."), title, w["details"].rstrip("."),
                                              first_label[0].upper() + first_label[1:], display[first_key])
            if rest:
                s += "; " + "; ".join("[%s](%s)" % (lab, display[k]) for k, lab in rest)
            s += "."
        elif rest:
            s += " Pages cited: " + "; ".join("[%s](%s)" % (lab, display[k]) for k, lab in w["parts"]) + "."
        chs = sorted(cited_in[w["key"]], key=order.index)
        s += " Cited in: " + ", ".join(chap_ref(c) for c in chs) + "."
        items.append(s)
    bib_counts[cat] = len(items)
    out.append("\n".join(items) + "\n")

unused = [w["key"] for w in WORKS if w["key"] not in cited_in]
with open(OUT, "w") as f:
    f.write("\n".join(out).rstrip() + "\n")

print(json.dumps(dict(index_entries=index_count, by_kind=dict(counts), excluded=excluded,
                      per_chapter=per_chapter, bibliography=dict(bib_counts),
                      bibliography_total=sum(bib_counts.values()), unused_works=unused), indent=1))
