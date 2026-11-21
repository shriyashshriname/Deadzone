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

        public const string VersionPrefix = "0.4";

        /// <summary>
        /// Every build gets the next build number so phone tests can be matched to builds.
        /// Shown in-game by Deadzone.UI.BuildNumberLabel and written to Builds/BUILD_NUMBER.txt.
        /// </summary>
        public static int StampNextBuildNumber()
        {
            int n = PlayerSettings.Android.bundleVersionCode + 1;
            PlayerSettings.Android.bundleVersionCode = n;
            PlayerSettings.bundleVersion = $"{VersionPrefix}.{n}";
            PlayerSettings.productName = "DEADZONE";
            AssetDatabase.SaveAssets();
            System.IO.Directory.CreateDirectory("Builds");
            System.IO.File.WriteAllText("Builds/BUILD_NUMBER.txt",
                $"Build #{n} · v{PlayerSettings.bundleVersion} · {System.DateTime.Now:yyyy-MM-dd HH:mm}" + System.Environment.NewLine);
            Debug.Log($"[BuildScript] Stamped Build #{n} (v{PlayerSettings.bundleVersion})");
            return n;
        }

        /// <summary>Windows build for screenshot checks (DevCaptureRunner). Does NOT bump the build number.</summary>
        public static void BuildWindowsCapture()
        {
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = Scenes,
                locationPathName = "Builds/WindowsCapture/Deadzone.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            });
            Debug.Log($"[BuildScript] Capture build: {report.summary.result}");
            if (Application.isBatchMode) EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
        }

        [MenuItem("Deadzone/Build/Windows (x64)")]
        public static void BuildWindows()
        {
            string buildPath = "Builds/Windows/Deadzone.exe";
            int buildNo = StampNextBuildNumber();
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
                Debug.Log($"[BuildScript] Windows Build #{buildNo} Succeeded: {summary.totalSize / (1024 * 1024)} MB uncompressed");
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
            int buildNo = StampNextBuildNumber();
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

            // Founder (Build #9): landscape (Build #8 tried portrait; reverted). The autorotate flags did not
            // reach the manifest ("fullUser" = portrait allowed), so the APK starts fixed landscape and
            // GameManager then allows both landscape sides at runtime (never portrait).
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

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
                long apk = new System.IO.FileInfo(buildPath).Length;
                Debug.Log($"[BuildScript] Android APK Build #{buildNo} Succeeded: {apk / (1024 * 1024)} MB apk");
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
