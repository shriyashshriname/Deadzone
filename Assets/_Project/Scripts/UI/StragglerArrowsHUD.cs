using UnityEngine;
using Deadzone.Core;

namespace Deadzone.UI
{
    /// <summary>
    /// When a wave is down to its last few enemies (nothing left to spawn), draws a red arrow at the
    /// screen edge pointing at each off-screen enemy, plus a "N LEFT" tag, so the player can finish the
    /// wave instead of wandering. Self-installs.
    /// </summary>
    public class StragglerArrowsHUD : MonoBehaviour
    {
        private const int ShowAtOrBelow = 5;
        private GUIStyle arrowStyle, tagStyle;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var go = new GameObject("[StragglerArrowsHUD]");
            DontDestroyOnLoad(go);
            go.AddComponent<StragglerArrowsHUD>();
        }

        private void OnGUI()
        {
            var wm = WaveManager.Instance;
            var cam = UnityEngine.Camera.main;
            if (wm == null || cam == null || wm.IsIntermission) return;
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;
            if (wm.TotalRemainingHostiles == 0 || wm.TotalRemainingHostiles > ShowAtOrBelow || wm.AliveWaveEnemies.Count == 0) return;

            float s = Mathf.Clamp(Screen.height / 720f, 0.8f, 1.8f);
            if (arrowStyle == null)
            {
                arrowStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
                tagStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            }
            arrowStyle.fontSize = Mathf.RoundToInt(30 * s);
            tagStyle.fontSize = Mathf.RoundToInt(13 * s);

            Color old = GUI.color;
            Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            float margin = 46f * s;
            foreach (var hp in wm.AliveWaveEnemies)
            {
                if (hp == null || hp.IsDead) continue;
                Vector3 sp = cam.WorldToScreenPoint(hp.transform.position + Vector3.up);
                bool behind = sp.z < 0f;
                Vector2 gp = new Vector2(sp.x, Screen.height - sp.y);
                if (behind) gp = center - (gp - center);
                bool onScreen = !behind && gp.x > margin && gp.x < Screen.width - margin && gp.y > margin && gp.y < Screen.height - margin;
                if (onScreen) continue;

                Vector2 dir = (gp - center).normalized;
                float k = Mathf.Min((Screen.width * 0.5f - margin) / Mathf.Max(Mathf.Abs(dir.x), 1e-3f),
                                    (Screen.height * 0.5f - margin) / Mathf.Max(Mathf.Abs(dir.y), 1e-3f));
                Vector2 pos = center + dir * k;
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + 90f;

                Matrix4x4 m = GUI.matrix;
                GUIUtility.RotateAroundPivot(angle, pos);
                GUI.color = Palette.Danger;
                GUI.Label(new Rect(pos.x - 20f * s, pos.y - 20f * s, 40f * s, 40f * s), "▲", arrowStyle);
                GUI.matrix = m;
            }
            GUI.color = Palette.Danger;
            GUI.Label(new Rect(center.x - 120f * s, 112f * s + 70f * s, 240f * s, 22f * s), $"{wm.TotalRemainingHostiles} HOSTILE{(wm.TotalRemainingHostiles == 1 ? "" : "S")} LEFT", tagStyle);
            GUI.color = old;
        }
    }
}
