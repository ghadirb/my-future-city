#!/usr/bin/env bash
# Type-checks Core + Runtime + Editor sources against hand-written stubs (tools/stubs).
# This is a sanity net for typos, NOT a substitute for compiling inside real Unity.
set -euo pipefail
cd "$(dirname "$0")/.."
mkdir -p tools/out
mcs -target:library -nowarn:1591,0108,0114 -out:tools/out/typecheck.dll \
  $(find Assets/_Game/Scripts -name '*.cs') tools/stubs/UnityStubs.cs
echo "type-check OK"
