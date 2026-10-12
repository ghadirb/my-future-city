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
