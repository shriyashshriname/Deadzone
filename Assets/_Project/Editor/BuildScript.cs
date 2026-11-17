#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Deadzone.Editor
{
    public static class BuildScript
    {
        private static readonly string[] Scenes = { "Assets/_Project/Scenes/Graybox_Sandbox.unity" };

        [InitializeOnLoadMethod]
        public static void EnsureScriptingDefines()
        {
            EnsureDefineForTarget(UnityEditor.Build.NamedBuildTarget.Standalone);
            EnsureDefineForTarget(UnityEditor.Build.NamedBuildTarget.Android);
        }

        private static void EnsureDefineForTarget(UnityEditor.Build.NamedBuildTarget target)
        {
            string defines = PlayerSettings.GetScriptingDefineSymbols(target);
            if (!defines.Contains("ENABLE_UNITY_COLLECTIONS_CHECKS"))
            {
                string newDefines = string.IsNullOrEmpty(defines)
                    ? "ENABLE_UNITY_COLLECTIONS_CHECKS"
                    : defines + ";ENABLE_UNITY_COLLECTIONS_CHECKS";
                PlayerSettings.SetScriptingDefineSymbols(target, newDefines);
            }
        }

        [MenuItem("Deadzone/Build/Windows (x64)")]
        public static void BuildWindows()
        {
            string buildPath = "Builds/Windows/Deadzone.exe";
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = Scenes,
                locationPathName = buildPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[BuildScript] Windows Build Succeeded: {summary.totalSize / (1024 * 1024)} MB");
            }
            else
            {
                Debug.LogError($"[BuildScript] Windows Build Failed with {summary.totalErrors} errors.");
            }
        }

        [MenuItem("Deadzone/Build/Android APK")]
        public static void BuildAndroid()
        {
            string buildPath = "Builds/Android/Deadzone.apk";
            string dir = System.IO.Path.GetDirectoryName(buildPath);
            if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);

            EditorUserBuildSettings.buildAppBundle = false;

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = Scenes,
                locationPathName = buildPath,
                target = BuildTarget.Android,
                options = BuildOptions.Development
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[BuildScript] Android APK Succeeded: {summary.totalSize / (1024 * 1024)} MB");
                foreach (var f in report.GetFiles())
                {
                    Debug.Log($"[BuildFile] {f.path} ({f.role}, {f.size} bytes)");
                }
            }
            else
            {
                Debug.LogError($"[BuildScript] Android Build Failed with {summary.totalErrors} errors.");
            }
        }
    }
}
#endif
