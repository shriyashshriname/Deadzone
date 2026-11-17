#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Deadzone.Editor
{
    public static class BuildScript
    {
        private static readonly string[] Scenes = { "Assets/_Project/Scenes/Graybox_Sandbox.unity" };

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
            }
            else
            {
                Debug.LogError($"[BuildScript] Android Build Failed with {summary.totalErrors} errors.");
            }
        }
    }
}
#endif
