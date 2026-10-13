using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using MyFutureCity.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MyFutureCity.EditorTools
{
    public class ValidationIssue
    {
        public bool IsError;
        public string Message;
        public override string ToString() { return (IsError ? "ERROR: " : "WARN:  ") + Message; }
    }

    /// <summary>Checks that scenes, data and settings are in a buildable state and explains what is missing.</summary>
    public static class ProjectValidator
    {
        private static readonly Regex PackageIdPattern = new Regex(@"^[a-zA-Z][a-zA-Z0-9_]*(\.[a-zA-Z][a-zA-Z0-9_]*)+$");

        [MenuItem("Tools/My Future City/Validate Project")]
        public static void ValidateFromMenu()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var issues = Validate();
            int errors = 0;
            foreach (var i in issues)
            {
                if (i.IsError) { errors++; Debug.LogError("[MyFutureCity] " + i); }
                else Debug.LogWarning("[MyFutureCity] " + i);
            }
            Debug.Log("[MyFutureCity] Validation finished: " + errors + " error(s), " + (issues.Count - errors) + " warning(s).");
        }

        private static void Error(List<ValidationIssue> list, string msg) { list.Add(new ValidationIssue { IsError = true, Message = msg }); }
        private static void Warn(List<ValidationIssue> list, string msg) { list.Add(new ValidationIssue { IsError = false, Message = msg }); }

        public static List<ValidationIssue> Validate()
        {
            var issues = new List<ValidationIssue>();

            if (!PackageIdPattern.IsMatch(BuildScript.PackageId))
                Error(issues, "Invalid Android package id: '" + BuildScript.PackageId + "'.");

            if (AssetDatabase.LoadAssetAtPath<Font>("Assets/_Game/Resources/Fonts/Vazirmatn-Regular.ttf") == null)
                Error(issues, "Persian font missing: Assets/_Game/Resources/Fonts/Vazirmatn-Regular.ttf");

            MissionDatabase db = null;
            var json = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Game/Resources/Data/missions.json");
            if (json == null) Error(issues, "Mission data missing: Assets/_Game/Resources/Data/missions.json");
            else
            {
                try { db = JsonUtility.FromJson<MissionDatabase>(json.text); }
                catch (System.Exception e) { Error(issues, "missions.json is not valid: " + e.Message); }
                if (db != null)
                    foreach (var err in MissionDatabaseValidator.Validate(db)) Error(issues, "missions.json: " + err);
            }

            if (!File.Exists(SceneGenerator.MenuScenePath)) Error(issues, "Menu scene missing (run Tools > My Future City > Generate Scenes).");
            if (!File.Exists(SceneGenerator.GameScenePath)) Error(issues, "Game scene missing (run Tools > My Future City > Generate Scenes).");

            var inBuild = new HashSet<string>();
            foreach (var s in EditorBuildSettings.scenes) if (s.enabled) inBuild.Add(s.path);
            if (!inBuild.Contains(SceneGenerator.MenuScenePath)) Error(issues, "Menu scene is not enabled in Build Settings.");
            if (!inBuild.Contains(SceneGenerator.GameScenePath)) Error(issues, "Game scene is not enabled in Build Settings.");

            if (File.Exists(SceneGenerator.MenuScenePath)) ValidateMenuScene(issues);
            if (File.Exists(SceneGenerator.GameScenePath)) ValidateGameScene(issues, db);
            return issues;
        }

        private static void CheckMissingScripts(List<ValidationIssue> issues, string sceneName)
        {
            foreach (var root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
            {
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) > 0)
                        Error(issues, sceneName + ": missing script on '" + t.name + "'.");
                }
            }
        }

        private static void ValidateMenuScene(List<ValidationIssue> issues)
        {
            EditorSceneManager.OpenScene(SceneGenerator.MenuScenePath, OpenSceneMode.Single);
            if (Object.FindFirstObjectByType<MainMenuController>() == null) Error(issues, "MainMenu: no MainMenuController.");
            if (Object.FindFirstObjectByType<Camera>() == null) Error(issues, "MainMenu: no camera.");
            CheckMissingScripts(issues, "MainMenu");
        }

        private static void ValidateGameScene(List<ValidationIssue> issues, MissionDatabase db)
        {
            EditorSceneManager.OpenScene(SceneGenerator.GameScenePath, OpenSceneMode.Single);
            const string n = "Neighborhood01";

            var cams = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
            if (cams.Length == 0) Error(issues, n + ": no camera.");
            if (Object.FindFirstObjectByType<ThirdPersonCamera>() == null) Error(issues, n + ": no ThirdPersonCamera.");
            var player = Object.FindFirstObjectByType<PlayerController>();
            if (player == null) Error(issues, n + ": no PlayerController (movement system).");
            else if (player.GetComponent<CharacterController>() == null) Error(issues, n + ": player has no CharacterController.");

            var gsc = Object.FindFirstObjectByType<GameSceneController>();
            if (gsc == null) Error(issues, n + ": no GameSceneController.");
            else
            {
                if (gsc.player == null) Error(issues, n + ": GameSceneController.player is not assigned.");
                if (gsc.interactor == null) Error(issues, n + ": GameSceneController.interactor is not assigned.");
                if (gsc.appearance == null) Warn(issues, n + ": GameSceneController.appearance is not assigned.");
            }

            if (db != null)
            {
                var npcIds = new HashSet<string>();
                foreach (var npc in Object.FindObjectsByType<NpcInteractable>(FindObjectsSortMode.None)) npcIds.Add(npc.npcId);
                var itemCounts = new Dictionary<string, int>();
                foreach (var p in Object.FindObjectsByType<PickupInteractable>(FindObjectsSortMode.None)) Bump(itemCounts, p.targetId);
                foreach (var r in Object.FindObjectsByType<RepairInteractable>(FindObjectsSortMode.None)) Bump(itemCounts, r.targetId);
                foreach (var t in Object.FindObjectsByType<TeacherLessonInteractable>(FindObjectsSortMode.None)) Bump(itemCounts, t.targetId);
                var buildingIds = new HashSet<string>();
                foreach (var b in Object.FindObjectsByType<BuildingVisual>(FindObjectsSortMode.None)) buildingIds.Add(b.buildingId);

                foreach (var m in db.missions)
                {
                    if (!string.IsNullOrEmpty(m.startNpcId) && !npcIds.Contains(m.startNpcId))
                        Error(issues, n + ": mission '" + m.id + "' needs NPC '" + m.startNpcId + "' which is not in the scene.");
                    foreach (var o in m.objectives)
                    {
                        if (o.ParsedType() == ObjectiveType.Talk)
                        {
                            if (!npcIds.Contains(o.targetId)) Error(issues, n + ": mission '" + m.id + "' talks to unknown NPC '" + o.targetId + "'.");
                        }
                        else
                        {
                            int have;
                            itemCounts.TryGetValue(o.targetId ?? "", out have);
                            if (have < o.RequiredCount)
                                Error(issues, n + ": mission '" + m.id + "' needs " + o.RequiredCount + " '" + o.targetId + "' in the scene but only " + have + " found.");
                        }
                    }
                }
                foreach (var b in db.buildings)
                    if (!buildingIds.Contains(b.id)) Error(issues, n + ": building '" + b.id + "' has no BuildingVisual in the scene.");
            }
            CheckMissingScripts(issues, n);
        }

        private static void Bump(Dictionary<string, int> d, string key)
        {
            if (string.IsNullOrEmpty(key)) return;
            int v;
            d.TryGetValue(key, out v);
            d[key] = v + 1;
        }
    }
}
