#!/usr/bin/env python3
"""Measure length cues in multiple-choice steps: how often the correct option is
the unique longest / unique shortest, plus short feedback/hints. Usage:
  python3 tools/measure_option_cues.py [jobId ...]   (default: all jobs)
Target per job: both cue rates near 33% (hard ceiling for longest: 55%)."""
import json, sys
d = json.load(open('Assets/_Game/Resources/Data/activities.json', encoding='utf-8'))
jobs = sys.argv[1:] or sorted({a['jobId'] for a in d['activities']})
for job in jobs:
    n = hi = lo = sf = sh = 0
    for a in d['activities']:
        if a['jobId'] != job: continue
        for scs in [a['scenes']] + [v['scenes'] for v in a.get('variants', [])]:
            for sc in scs:
                for st in sc['steps']:
                    if st.get('free') or st.get('ordered'): continue
                    o = st['options']; n += 1
                    L = [len(x['textFa']) for x in o]
                    c = [i for i, x in enumerate(o) if x['correct']]
                    if len(c) == 1:
                        if L[c[0]] == max(L) and L.count(max(L)) == 1: hi += 1
                        if L[c[0]] == min(L) and L.count(min(L)) == 1: lo += 1
                    sf += sum(1 for x in o if not x['correct'] and len(x['feedbackFa']) < 38)
                    sh += len(st.get('hintFa', '')) < 22
    if n:
        print(f"{job:12s} steps={n:3d} longest={100*hi//n:3d}% shortest={100*lo//n:3d}% short_feedback={sf} short_hint={sh}")
