#!/usr/bin/env python3
"""Repo sanity checks that need no Unity: data validity, required files, secret scan."""
import json, os, re, sys, subprocess

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
errors = []

def err(msg): errors.append(msg)

# 1. required files
required = [
    "ProjectSettings/ProjectVersion.txt", "Packages/manifest.json",
    "Assets/_Game/Resources/Data/missions.json", "Assets/_Game/Resources/Data/lessons.json",
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

# 3b. teaching lessons (mirror of LessonDatabaseValidator) and the "teacher must really teach" rule
TEACH_TARGETS = {"lesson_topic", "teach_board", "teach_students", "teach_help", "teach_activity", "teach_full"}
try:
    ldb = json.load(open(os.path.join(ROOT, "Assets/_Game/Resources/Data/lessons.json"), encoding="utf-8"))
    kinds_ok = {"present", "explain", "check", "help"}
    step_ids = set()
    def check_step(s, where, allowed):
        sid = where + "/" + str(s.get("id"))
        if not s.get("id") or s["id"] in step_ids: err(sid + ": missing or duplicate step id")
        step_ids.add(s.get("id"))
        if s.get("kind") not in allowed: err(sid + ": kind not allowed here: " + str(s.get("kind")))
        for f in ("promptFa", "successFa", "retryHintFa"):
            if not s.get(f): err(sid + ": missing " + f)
        opts = s.get("options") or []
        if not (2 <= len(opts) <= 4): err(sid + ": needs 2-4 options"); return
        if any(not o for o in opts): err(sid + ": empty option")
        if not (0 <= s.get("correctIndex", -1) < len(opts)): err(sid + ": correctIndex out of range")
        fb = s.get("feedbackFa") or []
        if fb and len(fb) != len(opts): err(sid + ": feedbackFa must match options")
    lids = [l["id"] for l in ldb["lessons"]]
    if len(lids) != len(set(lids)): err("duplicate lesson ids")
    if not lids: err("lessons.json has no lessons")
    for l in ldb["lessons"]:
        for f in ("titleFa", "conceptFa"):
            if not l.get(f): err(l["id"] + ": missing " + f)
        for st in l["steps"]: check_step(st, l["id"], kinds_ok)
        count = lambda k: sum(1 for st in l["steps"] if st["kind"] == k)
        if count("present") < 1 or count("explain") < 1: err(l["id"] + ": needs a present and an explain step")
        if count("check") < 2 or count("help") < 2: err(l["id"] + ": needs at least 2 check and 2 help steps")
    if len(ldb.get("manageSteps", [])) < 3: err("needs at least 3 shared manage steps")
    for st in ldb.get("manageSteps", []): check_step(st, "manage", {"manage"})

    teacher = [m for m in db["missions"] if m["jobId"] == "teacher"]
    if len(teacher) != 5: err("teacher must have exactly 5 stages")
    for m in teacher:
        interact_teach = [o for o in m["objectives"] if o["type"] == "interact" and o["targetId"] in TEACH_TARGETS]
        if not interact_teach: err(m["id"] + ": a teacher stage must contain real teaching work, not only collecting/talking")
    for m in teacher[1:]:
        if any(o["type"] == "collect" for o in m["objectives"]):
            err(m["id"] + ": only the preparation stage may collect items")
    if teacher and not any(o["targetId"] == "lesson_topic" for o in teacher[0]["objectives"]):
        err(teacher[0]["id"] + ": the first stage must let the child choose the lesson topic")
    if teacher and not any(o["targetId"] == "teach_full" for o in teacher[-1]["objectives"]):
        err(teacher[-1]["id"] + ": the last stage must run a whole lesson")
except Exception as e:
    err("lesson data check failed: %r" % e)

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
