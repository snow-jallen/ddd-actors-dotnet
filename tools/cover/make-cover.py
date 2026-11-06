#!/usr/bin/env python3
"""Generate the book cover as HTML+SVG (tools/cover/cover.html); render it with render-cover.sh.

The art is an Ohmline one-line diagram drawn as an actor system: small actors (ring = shell,
dot = functional core) grouped into bounded contexts, amber message traces between them, and
one large actor taking messages through its mailbox one at a time.
"""
import math, random, pathlib

W, H = 1600, 2400
BLUE, DEEP, DIM, PALE, AMBER, PAPER = "#2140A8", "#16297A", "#4F6BC4", "#9DB2EA", "#F5B021", "#F3F5FB"
rnd = random.Random(11)
S = 112          # grid pitch
OX, OY = 76, 92  # grid origin
COLS, ROWS = 14, 12

def cx(c): return OX + c * S
def cy(r): return OY + r * S

contexts = {  # name: (c0, r0, c1, r1)
    "Charging":    (0, 1, 6, 7),
    "Site Energy": (8, 0, 13, 3),
    "Billing":     (8, 5, 12, 6),
}
def ctx_of(c, r):
    for name, (c0, r0, c1, r1) in contexts.items():
        if c0 <= c <= c1 and r0 <= r <= r1: return name
    return None

big = (1200, 1125, 255)   # the focal actor: centre x, centre y, radius
def under_big(x, y): return math.hypot(x - big[0], y - big[1]) < big[2] + 70

svg = []
add = svg.append
add(f'<rect width="{W}" height="{H}" fill="{BLUE}"/>')
# faint survey grid
for c in range(COLS + 1):
    add(f'<line x1="{cx(c)-S/2}" y1="0" x2="{cx(c)-S/2}" y2="1330" stroke="{DEEP}" stroke-width="1" opacity=".35"/>')
for r in range(ROWS + 1):
    add(f'<line x1="0" y1="{cy(r)-S/2}" x2="{W}" y2="{cy(r)-S/2}" stroke="{DEEP}" stroke-width="1" opacity=".35"/>')
# bounded contexts
for name, (c0, r0, c1, r1) in contexts.items():
    x0, y0, x1, y1 = cx(c0) - S * .45, cy(r0) - S * .45, cx(c1) + S * .45, cy(r1) + S * .45
    add(f'<rect x="{x0}" y="{y0}" width="{x1-x0}" height="{y1-y0}" rx="34" fill="{DEEP}" fill-opacity=".55" stroke="{PALE}" stroke-width="2.5" stroke-dasharray="14 10"/>')
    add(f'<text x="{x0+26}" y="{y0+44}" font-family="Archivo" font-weight="600" font-size="27" fill="{PALE}" letter-spacing=".5">{name}</text>')

# actors
lit = set()
cells = []
for c in range(COLS):
    for r in range(ROWS):
        x, y = cx(c), cy(r)
        if under_big(x, y) or y > 1290: continue
        ctx = ctx_of(c, r)
        if ctx is None and rnd.random() < .45: continue
        if ctx and r == contexts[ctx][1] and c <= contexts[ctx][0] + 1: continue  # keep label clear
        on = ctx is not None and rnd.random() < .26
        if on: lit.add((c, r))
        cells.append((c, r, ctx, on))
for c, r, ctx, on in cells:
    x, y = cx(c), cy(r)
    ring = AMBER if on else (PALE if ctx else DIM)
    core = AMBER if on else (PALE if ctx else DIM)
    op = 1 if ctx else .55
    # ring with a mailbox notch on the left
    a0, a1, rad = math.radians(200), math.radians(160), 30
    p0 = (x + rad * math.cos(a0), y + rad * math.sin(a0)); p1 = (x + rad * math.cos(a1), y + rad * math.sin(a1))
    add(f'<path d="M{p0[0]:.1f},{p0[1]:.1f} A{rad},{rad} 0 1 1 {p1[0]:.1f},{p1[1]:.1f}" fill="none" stroke="{ring}" stroke-width="4.5" stroke-linecap="round" opacity="{op}"/>')
    add(f'<circle cx="{x}" cy="{y}" r="{11 if on else 8}" fill="{core}" opacity="{op}"/>')

# message traces between lit actors (orthogonal routes), plus ACL diamonds where they cross a boundary
litl = sorted(lit)
pairs = []
for i, a in enumerate(litl):
    best = sorted((b for b in litl if b != a), key=lambda b: abs(b[0]-a[0]) + abs(b[1]-a[1]))
    for b in best[:1]:
        if (b, a) not in pairs: pairs.append((a, b))
# a few long cross-context flows
cross = [(min(lit, key=lambda p: (ctx_of(*p) != "Charging", -p[0])), min(lit, key=lambda p: (ctx_of(*p) != "Site Energy", p[0]))),
         (min(lit, key=lambda p: (ctx_of(*p) != "Charging", -p[1])), min(lit, key=lambda p: (ctx_of(*p) != "Billing", p[0])))]
for a, b in pairs + cross:
    (ac, ar), (bc, br) = a, b
    x0, y0, x1, y1 = cx(ac) + 34, cy(ar), cx(bc) - 34, cy(br)
    if bc < ac: x0, x1 = cx(ac) - 34, cx(bc) + 34
    mx = (x0 + x1) / 2
    add(f'<path d="M{x0},{y0} H{mx} V{y1} H{x1}" fill="none" stroke="{AMBER}" stroke-width="3.5" stroke-linejoin="round" opacity=".9"/>')
    add(f'<circle cx="{x1}" cy="{y1}" r="6" fill="{AMBER}"/>')
    sa, sb = ctx_of(ac, ar), ctx_of(bc, br)
    if sa != sb and sa and sb:
        right = bc > ac
        edge = (cx(contexts[sa][2]) + S * .45) if right else (cx(contexts[sa][0]) - S * .45)
        if (right and mx > edge) or (not right and mx < edge): dx, dy = edge, y0
        else:
            dx, dy = ((cx(contexts[sb][0]) - S * .45) if right else (cx(contexts[sb][2]) + S * .45)), y1
        if 30 < dx < W - 30: add(f'<rect x="{dx-13}" y="{dy-13}" width="26" height="26" transform="rotate(45 {dx} {dy})" fill="{BLUE}" stroke="{AMBER}" stroke-width="3.5"/>')

# focal actor: shell ring with a mailbox notch, a queue of messages entering, an amber core
bx, by, br = big
add(f'<circle cx="{bx}" cy="{by}" r="{br+46}" fill="{BLUE}"/>')
gap = math.radians(16)
a0, a1 = math.pi + gap, math.pi - gap
p0 = (bx + br * math.cos(a0), by + br * math.sin(a0)); p1 = (bx + br * math.cos(a1), by + br * math.sin(a1))
add(f'<path d="M{p0[0]:.1f},{p0[1]:.1f} A{br},{br} 0 1 1 {p1[0]:.1f},{p1[1]:.1f}" fill="none" stroke="{PAPER}" stroke-width="30" stroke-linecap="round"/>')
add(f'<circle cx="{bx}" cy="{by}" r="{br-62}" fill="none" stroke="{PALE}" stroke-width="3" stroke-dasharray="4 14" opacity=".8"/>')
add(f'<circle cx="{bx}" cy="{by}" r="{br*.42}" fill="{AMBER}"/>')
add(f'<circle cx="{bx}" cy="{by}" r="{br*.42}" fill="none" stroke="{DEEP}" stroke-width="5" opacity=".35"/>')
# lambda-ish mark in the core: the pure decide function
s = br * .2
add(f'<path d="M{bx-s*.62},{by-s*1.05} Q{bx-s*.15},{by-s*1.1} {bx+s*.05},{by-s*.45} L{bx+s*.72},{by+s} M{bx+s*.02},{by-s*.2} L{bx-s*.72},{by+s}" stroke="{DEEP}" stroke-width="19" stroke-linecap="round" stroke-linejoin="round" fill="none"/>')
# the queue: messages waiting at the mailbox, one inside
qx = bx - br
for i, k in enumerate(range(6)):
    x = qx - 70 - i * 74
    add(f'<rect x="{x-24}" y="{by-17}" width="48" height="34" rx="6" fill="{AMBER if i < 3 else PALE}" opacity="{1 - i*.13:.2f}"/>')
add(f'<path d="M{qx-40},{by} H{bx-br*.42-26}" stroke="{AMBER}" stroke-width="6" stroke-linecap="round"/>')
add(f'<circle cx="{bx-br*.42-26}" cy="{by}" r="11" fill="{AMBER}"/>')

art = "\n".join(svg)
html = f'''<!doctype html><html><head><meta charset="utf-8">
<link rel="preconnect" href="https://fonts.googleapis.com"><link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
<link href="https://fonts.googleapis.com/css2?family=Archivo:wght@400;500;600&family=Big+Shoulders+Display:wght@700;800;900&display=block" rel="stylesheet">
<style>
  html,body{{margin:0;width:{W}px;height:{H}px;overflow:hidden;background:{BLUE};}}
  .art{{position:absolute;inset:0;}}
  .type{{position:absolute;left:96px;right:96px;bottom:128px;color:{PAPER};}}
  h1{{font-family:"Big Shoulders Display";font-weight:900;font-size:186px;line-height:.86;letter-spacing:-1px;margin:0;}}
  h1 .on{{display:block;font-weight:700;font-size:120px;color:{PALE};margin:.10em 0 .12em;}}
  p.sub{{font-family:Archivo;font-weight:500;font-size:46px;line-height:1.25;margin:48px 0 0;max-width:20ch;color:{PAPER};}}
  p.foot{{position:absolute;left:96px;right:96px;bottom:70px;margin:0;font-family:Archivo;font-weight:400;font-size:30px;color:{PALE};
    display:flex;justify-content:space-between;border-top:2px solid {DIM};padding-top:26px;}}
</style></head><body>
<svg class="art" viewBox="0 0 {W} {H}" width="{W}" height="{H}" xmlns="http://www.w3.org/2000/svg">{art}</svg>
<div class="type">
  <h1>Domain-Driven Design <span class="on">with the Actor Model on</span>.NET</h1>
  <p class="sub">Functional core, actor shell with C#&nbsp;15, .NET&nbsp;11 and Akka.NET</p>
</div>
<p class="foot"><span>One system, built in 174 tagged steps</span><span>First edition 2026</span></p>
</body></html>'''
out = pathlib.Path(__file__).with_name("cover.html")
out.write_text(html)
print(out)
