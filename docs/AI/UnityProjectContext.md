# Unity project context — My Future City

<!-- unity-onboarding:generated:start -->
## Summary

Confirmed Unity project at `C:\github\my-future-city`: an offline, Persian-language, low-poly Android city-adventure MVP. Current playable content contains two five-stage jobs (Engineer and Teacher), generated into a menu and one neighbourhood scene by an Editor script.

## Confirmed environment

- Unity: `6000.3.11f1` (`ProjectSettings/ProjectVersion.txt`).
- Render pipeline: Built-in Render Pipeline with an `Unlit/Color` base palette (no SRP package). This avoids the Android lighting variance observed with `Mobile/Diffuse`.
- Input: legacy Input Manager plus direct touch handling (confirmed in runtime code; no Input System package).
- UI: uGUI, code-built at runtime, with bundled Vazirmatn font and a local Persian shaping helper.
- Target: Android, IL2CPP, API 24 minimum, universal ARMv7/ARM64/x86_64 APK (`BuildScript.cs`).

## Structure and boundaries

- `Assets/_Game/Scripts/Core`: pure C# mission, wallet, save, city-progress and recommendation rules; EditMode-tested.
- `Assets/_Game/Scripts/Runtime`: MonoBehaviour adapters for player, camera, interaction, save bridge and UI.
- `Assets/_Game/Scripts/Editor`: scene generation, configuration validation and Android build entry point.
- `Assets/_Game/Resources/Data/missions.json`: data-driven jobs, missions and buildings.
- `Assets/_Game/Tests/EditMode`: Unity Test Framework tests; `tools/LogicTests` provides a Mono fallback for pure core tests.

Assemblies follow `Core -> Runtime`, while `Editor` references the runtime surface to generate and validate scenes. `GameContext` is the deliberate single composition root for save-backed core services.

## Startup and scenes

`SceneGenerator.GenerateAll()` creates and registers `MainMenu` then `Neighborhood01`. `MainMenuController` chooses/loads a character; `GameSceneController` creates HUD and connects mission/city events. Generated scenes/materials are intentionally not committed.

## Validation and tooling

- GitHub workflows: repository validation, Unity EditMode tests, Android build.
- Current local evidence: repository validation passes; Unix shell scripts need LF checkout on Windows before running through Bash.
- Unity Editor executable, connected Unity MCP bridge, Unity license, APK and device validation are unavailable in this workspace. No Unity Editor compile or Android build has been observed.

## Constraints and risks

- Do not commit tokens or licenses. The provided token folder was inspected by filename only and is not needed for local game code.
- Regenerate scenes through the Editor script after generator changes; never hand-edit generated scene YAML.
- Visual fixes require a Unity/Android render check; static source changes are not visual evidence.
<!-- unity-onboarding:generated:end -->
