#!/usr/bin/env bash
# Prints a one-line % complete for the book build workflow.
B="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
J=/home/jonathan/.claude/projects/-home-jonathan-code-scienceNight/45f474fc-e89c-490e-8b5f-ec3ffd186198/subagents/workflows/wf_3a487f2e-ad2/journal.jsonl
python3 - "$B" "$J" <<'PY'
import json, sys, subprocess, os
B, J = sys.argv[1], sys.argv[2]
lab = {}; done = set()
for l in open(J):
    d = json.loads(l)
    if d.get('type') == 'started': lab[d['agentId']] = d.get('label','')
    elif d.get('type') == 'result' and d.get('agentId'): done.add(d['agentId'])
finished = [lab[a] for a in lab if a in done]
running = [lab[a] for a in lab if a not in done]
import glob
o = json.load(open(f'{B}/editorial/outline.json'))
chs = o['chapters']; ntags = sum(1 for c in chs for s in c['sections'] if s['codeTag'])
amended = any(l.startswith('editor:amend') for l in finished)
tags = subprocess.run(['git','-C',f'{B}/sample','tag','-l','ch*'],capture_output=True,text=True).stdout.split()
n = len(chs)
w = sum(1 for l in finished if l.startswith('write:'))
e = sum(1 for l in finished if l.startswith('edit:'))
v = sum(1 for l in finished if l.startswith('verify:'))
f = sum(1 for l in finished if l.startswith('finish:'))
words = 0
for p in glob.glob(f'{B}/book/*.md'): words += len(open(p).read().split())
pct = (3 if amended else 0) + (45 * len(tags) / ntags if amended else 0) + 17*w/n + 12*e/n + 13*v/n + 10*f/3
print(f"{pct:5.1f}% | outline {'amended' if amended else 'amending'} ({n} ch, {ntags} tags) | tags {len(tags)}/{ntags} | written {w}/{n} edited {e}/{n} verified {v}/{n} | finish {f}/3 | {words:,} words | running: {', '.join(sorted(running)) or '-'}")
PY
