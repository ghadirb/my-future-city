#!/usr/bin/env bash
# Compiles the Unity-independent core (Assets/_Game/Scripts/Core) together with the real EditMode
# test sources and runs them under Mono. Needs: mono-mcs mono-runtime (apt install mono-mcs).
set -euo pipefail
cd "$(dirname "$0")/.."
mkdir -p tools/out
mcs -nowarn:1591 -out:tools/out/logic-tests.exe \
  $(find Assets/_Game/Scripts/Core -name '*.cs') \
  $(find Assets/_Game/Tests/EditMode -name '*.cs' ! -name '*Json*') \
  tools/LogicTests/NUnitShim.cs tools/LogicTests/Runner.cs
mono tools/out/logic-tests.exe
