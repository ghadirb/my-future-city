#!/usr/bin/env python3
"""Apply text-only content patches to activities.json.

Each patch file defines P = [(ai, group, si, ti, correct_idx, hint|None,
{opt_idx: (text|None, feedback|None)})]. Aborts if the correct index differs
from the expectation, or a step is free/ordered, or a step is patched twice.
Only text is changed; structure, `correct`, `setsTag` and variants stay.
"""
import json, sys, importlib.util
path = 'Assets/_Game/Resources/Data/activities.json'
d = json.load(open(path, encoding='utf-8'))
def load(f):
    s = importlib.util.spec_from_file_location('m', f)
    m = importlib.util.module_from_spec(s); s.loader.exec_module(m); return m.P
patches = []
for f in sys.argv[1:]:
    patches += load(f)
n = 0; seen = set()
for ai, g, si, ti, ci, hint, opts in patches:
    a = d['activities'][ai]
    scs = a['scenes'] if g == 'base' else next(v['scenes'] for v in a['variants'] if v['tag'] == g)
    st = scs[si]['steps'][ti]
    assert not st.get('free') and not st.get('ordered'), (ai, g, si, ti)
    assert [i for i, o in enumerate(st['options']) if o['correct']] == [ci], ('correct idx mismatch', ai, g, si, ti)
    assert (ai, g, si, ti) not in seen, ('duplicate', ai, g, si, ti); seen.add((ai, g, si, ti))
    if hint: st['hintFa'] = hint
    for i, (t, fb) in opts.items():
        o = st['options'][i]
        if t: o['textFa'] = t
        if fb: o['feedbackFa'] = fb
    n += 1
open(path, 'w', encoding='utf-8').write(json.dumps(d, indent=2, ensure_ascii=False) + '\n')
print('patched steps', n)
