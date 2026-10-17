#!/usr/bin/env python3
"""Spread the correct option evenly over positions in normal (non-free, non-ordered) steps.

Why: in older content ~56% of correct answers were option #1, so a child could win by always
tapping the first button. That makes learning meaningless and pollutes the 'success' signal in the
interest report. Deterministic (no randomness): re-running is a no-op once balanced.
Only the position of the correct option moves; text/feedback/tags stay attached to their option.
"""
import json, sys
P = 'Assets/_Game/Resources/Data/activities.json'

def steps(d):
    for a in d['activities']:
        groups = [a.get('scenes', [])] + [v.get('scenes', []) for v in a.get('variants', [])]
        for sc_list in groups:
            for sc in sc_list:
                for st in sc.get('steps', []):
                    if st.get('free') or st.get('ordered'):
                        continue
                    if sum(1 for o in st['options'] if o.get('correct')) == 1:
                        yield st

def main(check=False):
    d = json.load(open(P, encoding='utf-8'))
    counters = {2: 0, 3: 0}
    moved = 0
    for st in steps(d):
        n = len(st['options'])
        target = (counters[n] * 2 + 1) % n if n == 3 else counters[n] % 2   # 3 opts: 1,0,2,1,0,2...
        counters[n] += 1
        ci = next(i for i, o in enumerate(st['options']) if o.get('correct'))
        if ci != target:
            moved += 1
            if not check:
                opt = st['options'].pop(ci)
                st['options'].insert(target, opt)
    if check:
        print('would move', moved); return moved
    open(P, 'w', encoding='utf-8').write(json.dumps(d, indent=2, ensure_ascii=False) + '\n')
    print('moved', moved)

if __name__ == '__main__':
    main('--check' in sys.argv)
