using UnityEngine;
using Deadzone.Core;

namespace Deadzone.UI
{
    /// <summary>
    /// Backpack bar under the top-centre banners: one tappable tile per slot (tap MED / AMMO to use),
    /// empty tiles for free capacity, and a "BAG n/N · SALVAGE x cr" caption. Self-installs.
    /// </summary>
    public class BackpackHUD : MonoBehaviour
    {
        private GUIStyle tileStyle, captionStyle;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var go = new GameObject("[BackpackHUD]");
            DontDestroyOnLoad(go);
            go.AddComponent<BackpackHUD>();
        }

        private void OnGUI()
        {
            var bag = Backpack.Instance;
            if (bag == null) return;
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;

            UIScale.Begin();
            float scale = 1f; // UIScale handles device resolution
            float tile = 46f * scale, gap = 5f * scale;
            int cap = bag.Capacity;
            float w = cap * tile + (cap - 1) * gap;
            float x0 = (UIScale.W - w) * 0.5f, y0 = UIScale.Portrait ? 182f : 76f;

            if (tileStyle == null)
            {
                tileStyle = new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                captionStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            }
            tileStyle.fontSize = Mathf.RoundToInt(11 * scale);
            captionStyle.fontSize = Mathf.RoundToInt(11 * scale);

            Color old = GUI.color;
            bool interactable = !HUDController.IsMenuOpen;
            for (int i = 0; i < cap; i++)
            {
                var r = new Rect(x0 + i * (tile + gap), y0, tile, tile);
                if (i < bag.Slots.Count)
                {
                    var s = bag.Slots[i];
                    GUI.color = Backpack.ColorOf(s.kind);
                    string label = $"{Backpack.Label(s.kind)}\nx{s.count}";
                    if (GUI.Button(r, label, tileStyle) && interactable) bag.Use(i);
                }
                else
                {
                    GUI.color = new Color(1f, 1f, 1f, 0.25f);
                    GUI.Box(r, "");
                }
            }

            GUI.color = Palette.WithAlpha(Palette.UIText, 0.9f);
            int salvage = bag.SalvageValue;
            string cap2 = $"BAG {bag.UsedSlots}/{cap}" + (salvage > 0 ? $"  ·  SALVAGE {salvage} cr (extract to cash in)" : "");
            GUI.Label(new Rect(x0 - 120f, y0 + tile + 2f, w + 240f, 18f * scale), cap2, captionStyle);
            GUI.color = old;
        }
    }
}
