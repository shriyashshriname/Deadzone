using UnityEngine;
using Deadzone.Core;

namespace Deadzone.UI
{
    /// <summary>
    /// Small "Build #N · vX" tag at the bottom centre of the screen (between the joysticks) so every
    /// phone test can be matched to its build. Self-installs at startup; no scene wiring needed.
    /// The number comes from PlayerSettings, stamped by Deadzone.Editor.BuildScript on every build.
    /// </summary>
    public class BuildNumberLabel : MonoBehaviour
    {
        private string text;
        private GUIStyle style;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var go = new GameObject("[BuildNumberLabel]");
            DontDestroyOnLoad(go);
            go.AddComponent<BuildNumberLabel>();
        }

        private void Awake()
        {
            // bundleVersion is "0.4.N"; N is the build number.
            string v = Application.version;
            string n = v.Contains(".") ? v.Substring(v.LastIndexOf('.') + 1) : v;
            text = $"Build #{n} · v{v}{(Debug.isDebugBuild ? " · dev" : "")}";
        }

        private void OnGUI()
        {
            GUI.matrix = Matrix4x4.identity; // raw screen pixels (world-anchored / safe-area aware)
            if (style == null)
            {
                style = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.LowerCenter,
                    fontSize = Mathf.Max(12, Screen.height / 55),
                    fontStyle = FontStyle.Bold,
                };
                style.normal.textColor = Palette.WithAlpha(Palette.UIText, 0.55f);
            }
            Rect safe = Screen.safeArea;
            // IMGUI's origin is top-left; safeArea's is bottom-left.
            float bottom = Screen.height - safe.yMin - 6f;
            GUI.Label(new Rect(safe.xMin, bottom - 40f, safe.width, 40f), text, style);
        }
    }
}
