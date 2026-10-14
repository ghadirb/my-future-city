#!/usr/bin/env python3
"""Repo sanity checks that need no Unity: data validity, required files, secret scan."""
import json, os, re, sys, subprocess

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
errors = []

def err(msg): errors.append(msg)

# 1. required files
required = [
    "ProjectSettings/ProjectVersion.txt", "Packages/manifest.json",
    "Assets/_Game/Resources/Data/missions.json",
    "Assets/_Game/Resources/Data/activities.json",
    "Assets/_Game/Resources/Fonts/Vazirmatn-Regular.ttf",
    "Assets/_Game/Scripts/Core/MyFutureCity.Core.asmdef",
    "Assets/_Game/Scripts/Runtime/MyFutureCity.Runtime.asmdef",
    "Assets/_Game/Scripts/Editor/MyFutureCity.Editor.asmdef",
    "Assets/_Game/Scripts/Editor/BuildScript.cs", ".gitignore",
]
for r in required:
    if not os.path.exists(os.path.join(ROOT, r)): err("missing required file: " + r)

# 2. JSON files parse
for dp, dn, fn in os.walk(ROOT):
    if ".git" in dp.split(os.sep): continue
    for f in fn:
        if f.endswith((".json", ".asmdef")):
            p = os.path.join(dp, f)
            try: json.load(open(p, encoding="utf-8"))
            except Exception as e: err("invalid JSON %s: %s" % (os.path.relpath(p, ROOT), e))

# 3. mission data cross references (mirror of MissionDatabaseValidator)
try:
    db = json.load(open(os.path.join(ROOT, "Assets/_Game/Resources/Data/missions.json"), encoding="utf-8"))
    jobs = {j["id"] for j in db["jobs"]}; bld = {b["id"] for b in db["buildings"]}
    ids = [m["id"] for m in db["missions"]]
    if len(ids) != len(set(ids)): err("duplicate mission ids")
    for m in db["missions"]:
        if m["jobId"] not in jobs: err(m["id"] + ": unknown job")
        if m.get("requiresMissionId") and m["requiresMissionId"] not in ids: err(m["id"] + ": unknown requirement")
        if not m["objectives"]: err(m["id"] + ": no objectives")
        for o in m["objectives"]:
            if o["type"] not in ("talk", "collect", "interact"): err(m["id"] + ": bad objective type " + o["type"])
            if o.get("buildingId") and o["buildingId"] not in bld: err(m["id"] + ": unknown building")
        if m["reward"].get("buildingId") and m["reward"]["buildingId"] not in bld: err(m["id"] + ": reward unknown building")
except Exception as e:
    err("mission data check failed: %r" % e)

# 3b. real job activities (mirror of ActivityDatabaseValidator): every decision must be a real choice
REAL_ACTIVITY_JOBS = {"teacher"}   # jobs already redesigned as real work; extend as more jobs are converted
try:
    acts = json.load(open(os.path.join(ROOT, "Assets/_Game/Resources/Data/activities.json"), encoding="utf-8"))["activities"]
    mission = {m["id"]: m for m in db["missions"]}
    seen = set()
    for a in acts:
        key = a["missionId"] + "/" + a["targetId"]
        p = "activity " + key + ": "
        if key in seen: err(p + "duplicate")
        seen.add(key)
        m = mission.get(a["missionId"])
        if not m: err(p + "unknown mission"); continue
        if m["jobId"] != a["jobId"]: err(p + "jobId does not match mission")
        obj = [o for o in m["objectives"] if o["type"] == "interact" and o["targetId"] == a["targetId"]]
        if not obj: err(p + "mission has no interact objective with this target")
        elif len(a["scenes"]) < obj[0]["count"]: err(p + "fewer scenes than required spots")
        for f in ("realWorldFa", "childDoesFa", "skillFa"):
            if not a.get(f): err(p + "missing " + f)
        for si, sc in enumerate(a["scenes"]):
            if not sc.get("titleFa") or not sc["steps"]: err(p + "scene %d incomplete" % si)
            for ti, st in enumerate(sc["steps"]):
                q = p + "scene %d step %d: " % (si, ti)
                ops = st["options"]
                if not st["promptFa"]: err(q + "no prompt")
                if not 2 <= len(ops) <= 3: err(q + "needs 2-3 options")
                if any(not o["textFa"] or not o["feedbackFa"] for o in ops): err(q + "option without text/feedback")
                good = sum(1 for o in ops if o["correct"])
                if not st.get("free"):
                    if good == 0: err(q + "no correct option")
                    if good == len(ops): err(q + "every option correct (use free)")
                    if not st.get("hintFa"): err(q + "no hint")
    # redesigned jobs: no mission may be finishable by collecting/talking only
    for m in db["missions"]:
        if m["jobId"] in REAL_ACTIVITY_JOBS:
            if not any(o["type"] == "interact" and (m["id"] + "/" + o["targetId"]) in seen for o in m["objectives"]):
                err(m["id"] + ": real-work job but no activity objective")
except Exception as e:
    err("activity data check failed: %r" % e)

# 4. no secrets in tracked files
patterns = [r"ghp_[A-Za-z0-9]{30,}", r"github_pat_[A-Za-z0-9_]{30,}", r"cfut_[A-Za-z0-9]{30,}", r"-----BEGIN [A-Z ]*PRIVATE KEY-----"]
tracked = subprocess.run(["git", "ls-files"], cwd=ROOT, capture_output=True, text=True).stdout.split("\n")
for f in tracked:
    if not f or f.endswith((".ttf", ".png", ".apk")): continue
    p = os.path.join(ROOT, f)
    try: text = open(p, encoding="utf-8", errors="ignore").read()
    except Exception: continue
    for pat in patterns:
        if re.search(pat, text) and not f.startswith("tools/validate_repo.py"):
            err("possible secret in " + f)

if errors:
    print("FAILED:"); [print(" -", e) for e in errors]; sys.exit(1)
print("repo validation OK")
