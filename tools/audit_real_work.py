#!/usr/bin/env python3
"""Audit: which missions are real work (have an interact objective backed by an activity) and how many decisions they hold.

Usage: python3 tools/audit_real_work.py
Prints one row per mission and a per-job summary; exits 1 if any mission can be finished by collecting/talking only.
"""
import json, os, sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
load = lambda n: json.load(open(os.path.join(ROOT, "Assets/_Game/Resources/Data", n), encoding="utf-8"))
missions = load("missions.json")["missions"]
acts = {(a["missionId"], a["targetId"]): a for a in load("activities.json")["activities"]}

summary, bad = {}, []
for m in missions:
    types, decisions, real = [], 0, False
    for o in m["objectives"]:
        types.append(o["type"][:3])
        if o["type"] == "interact":
            a = acts.get((m["id"], o["targetId"]))
            if a:
                real = True
                decisions += sum(len(a["scenes"][i % len(a["scenes"])]["steps"]) for i in range(o["count"]))
    s = summary.setdefault(m["jobId"], [0, 0, 0])
    s[0] += 1; s[1] += 1 if real else 0; s[2] += decisions
    if not real: bad.append(m["id"])
    print("%-12s %-26s %-22s decisions=%-3d %s" % (m["jobId"][:12], m["id"][:26], "+".join(types), decisions, "REAL" if real else "COLLECT/TALK ONLY"))

print("\nper job: missions / real / decisions")
for j, (n, r, d) in summary.items(): print("  %-12s %d / %d / %d" % (j, n, r, d))
variants = [(a["missionId"], a["targetId"], len(a.get("variants", []))) for a in acts.values() if a.get("variants")]
print("activities with tag variants:", variants or "none")
sys.exit(1 if bad else 0)
