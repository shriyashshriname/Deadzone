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
        }

        private static void EnsureDefineForTarget(UnityEditor.Build.NamedBuildTarget target)
        {
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

            string sdkPath = "E:/Unity/Hub/Editor/6000.6.9f1/Editor/Data/PlaybackEngines/AndroidPlayer/SDK";
            string jdkPath = "E:/Unity/Hub/Editor/6000.6.9f1/Editor/Data/PlaybackEngines/AndroidPlayer/OpenJDK";
            string ndkPath = "E:/Unity/Hub/Editor/6000.6.9f1/Editor/Data/PlaybackEngines/AndroidPlayer/NDK";
            if (System.IO.Directory.Exists(sdkPath))
            {
                EditorPrefs.SetBool("SdkUseEmbedded", true);
                EditorPrefs.SetString("AndroidSdkRoot", sdkPath);
            }
            if (System.IO.Directory.Exists(jdkPath))
            {
                EditorPrefs.SetBool("JdkUseEmbedded", true);
                EditorPrefs.SetString("JdkPath", jdkPath);
            }
            if (System.IO.Directory.Exists(ndkPath))
            {
                EditorPrefs.SetBool("NdkUseEmbedded", true);
                EditorPrefs.SetString("AndroidNdkRoot", ndkPath);
            }

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
