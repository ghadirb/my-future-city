using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

namespace MyFutureCity.EditorTools
{
    /// <summary>
    /// Headless / menu Android build.
    /// CI entry point (GameCI buildMethod): MyFutureCity.EditorTools.BuildScript.BuildAndroid
    /// It regenerates the scenes, validates the project, applies Android settings and builds an APK.
    /// </summary>
    public static class BuildScript
    {
        // Change before publishing to a store; must be unique worldwide.
        public const string PackageId = "com.myfuturecity.game";
        public const string ProductName = "My Future City";
        public const string DefaultVersion = "0.1.0";

        [MenuItem("Tools/My Future City/Build Android APK (local)")]
        public static void BuildAndroidFromMenu()
        {
            try { Run(); }
            catch (Exception e) { Debug.LogError("[MyFutureCity] Build failed: " + e.Message); }
        }

        public static void BuildAndroid()
        {
            try
            {
                Run();
            }
            catch (Exception e)
            {
                Debug.LogError("[MyFutureCity] Build failed: " + e);
                EditorApplication.Exit(1);
            }
        }

        private static Dictionary<string, string> ParseArgs()
        {
            var map = new Dictionary<string, string>();
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (!args[i].StartsWith("-")) continue;
                string key = args[i].Substring(1);
                string val = (i + 1 < args.Length && !args[i + 1].StartsWith("-")) ? args[i + 1] : "true";
                map[key] = val;
            }
            return map;
        }

        private static string Get(Dictionary<string, string> a, string key, string fallback)
        {
            string v;
            return a.TryGetValue(key, out v) && !string.IsNullOrEmpty(v) ? v : fallback;
        }

        private static void Run()
        {
            var args = ParseArgs();

            Debug.Log("[MyFutureCity] Generating scenes...");
            SceneGenerator.GenerateAll();

            Debug.Log("[MyFutureCity] Validating project...");
            var issues = ProjectValidator.Validate();
            int errors = 0;
            foreach (var i in issues)
            {
                if (i.IsError) { errors++; Debug.LogError("[MyFutureCity] " + i); }
                else Debug.LogWarning("[MyFutureCity] " + i);
            }
            if (errors > 0) throw new Exception("Project validation failed with " + errors + " error(s).");

            ConfigureAndroid(args);

            string output = Get(args, "customBuildPath", "Builds/Android/MyFutureCity.apk");
            if (!output.EndsWith(".apk", StringComparison.OrdinalIgnoreCase))
                output = Path.Combine(output, Get(args, "customBuildName", "MyFutureCity") + ".apk");
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));

            EditorUserBuildSettings.buildAppBundle = false;
            var options = new BuildPlayerOptions
            {
                scenes = new[] { SceneGenerator.MenuScenePath, SceneGenerator.GameScenePath },
                locationPathName = output,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None
            };

            Debug.Log("[MyFutureCity] Building " + output);
            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary s = report.summary;
            if (s.result != BuildResult.Succeeded)
                throw new Exception("BuildPipeline result: " + s.result + " (errors: " + s.totalErrors + ")");

            Debug.Log("[MyFutureCity] Build succeeded: " + s.outputPath + " (" + (s.totalSize / (1024.0 * 1024.0)).ToString("0.0") + " MB, " + s.totalTime + ")");
        }

        public static void ConfigureAndroid(Dictionary<string, string> args)
        {
            string version = Get(args, "buildVersion", DefaultVersion);
            int versionCode;
            if (!int.TryParse(Get(args, "androidVersionCode", "1"), out versionCode) || versionCode < 1) versionCode = 1;

            PlayerSettings.companyName = "MyFutureCity";
            PlayerSettings.productName = ProductName;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, PackageId);
            PlayerSettings.bundleVersion = version;
            PlayerSettings.Android.bundleVersionCode = versionCode;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;

            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            // One universal APK that installs on every common phone: 32-bit ARM (budget phones such as
            // many Android 10 devices ship a 32-bit userspace even on 64-bit CPUs), 64-bit ARM, and
            // x86_64 (emulators / Chromebooks). An ARM64-only APK fails with INSTALL_FAILED_NO_MATCHING_ABIS.
            PlayerSettings.Android.targetArchitectures =
                AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64 | AndroidArchitecture.X86_64;
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android, ManagedStrippingLevel.Low);

            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
            PlayerSettings.colorSpace = ColorSpace.Gamma;

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            // The game is fully offline: request no permissions.
            PlayerSettings.Android.forceInternetPermission = false;
            PlayerSettings.Android.forceSDCardPermission = false;

            Debug.Log("[MyFutureCity] Android configured: " + PackageId + " v" + version + " (" + versionCode + ")");
        }
    }
}
