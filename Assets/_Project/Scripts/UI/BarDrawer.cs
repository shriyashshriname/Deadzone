using UnityEngine;

namespace Deadzone.UI
{
    /// <summary>Flat, clean health bars for IMGUI (no skin boxes): dark frame, dark track, solid fill.</summary>
    public static class BarDrawer
    {
        public static readonly Color Green = new Color(0.20f, 0.86f, 0.36f, 1f);
        public static readonly Color Track = new Color(0.05f, 0.07f, 0.09f, 0.78f);
        public static readonly Color Frame = new Color(0f, 0f, 0f, 0.9f);

        /// <summary>Draws a bar in the current GUI space. pct 0..1.</summary>
        public static void Draw(Rect r, float pct, Color fill, float border = 1f)
        {
            Color old = GUI.color;
            GUI.color = Frame;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            var inner = new Rect(r.x + border, r.y + border, r.width - border * 2f, r.height - border * 2f);
            GUI.color = Track;
            GUI.DrawTexture(inner, Texture2D.whiteTexture);
            if (pct > 0f)
            {
                GUI.color = fill;
                GUI.DrawTexture(new Rect(inner.x, inner.y, inner.width * Mathf.Clamp01(pct), inner.height), Texture2D.whiteTexture);
                // subtle top highlight so it doesn't read as a flat sticker
                GUI.color = new Color(1f, 1f, 1f, 0.18f);
                GUI.DrawTexture(new Rect(inner.x, inner.y, inner.width * Mathf.Clamp01(pct), Mathf.Max(1f, inner.height * 0.35f)), Texture2D.whiteTexture);
            }
            GUI.color = old;
        }

        /// <summary>Bar centred over a world position (raw screen pixels; caller sets GUI.matrix = identity).</summary>
        public static bool DrawOverhead(UnityEngine.Camera cam, Vector3 worldPos, float pct, Color fill, out Vector2 topLeft, float widthAt720 = 54f, float heightAt720 = 7f)
        {
            topLeft = Vector2.zero;
            if (cam == null) return false;
            Vector3 sp = cam.WorldToScreenPoint(worldPos);
            if (sp.z <= 0f) return false;
            float k = Mathf.Max(1f, Mathf.Min(Screen.width, Screen.height) / 720f);
            float w = widthAt720 * k, h = Mathf.Max(4f, heightAt720 * k);
            topLeft = new Vector2(sp.x - w * 0.5f, Screen.height - sp.y - h * 0.5f);
            Draw(new Rect(topLeft.x, topLeft.y, w, h), pct, fill, Mathf.Max(1f, k));
            return true;
        }
    }
}
