using UnityEngine;

namespace Deadzone.UI
{
    public static class MobileControlsDrawer
    {
        private static Texture2D circleTex, ringTex;
        private static GUIStyle btnStyle;

        public static void EnsureTextures()
        {
            if (circleTex != null && ringTex != null) return;
            int sz = 128;
            circleTex = new Texture2D(sz, sz, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            ringTex = new Texture2D(sz, sz, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            Vector2 c = new Vector2(sz * 0.5f, sz * 0.5f);
            float rad = sz * 0.48f;
            Color[] cPix = new Color[sz * sz];
            Color[] rPix = new Color[sz * sz];
            for (int y = 0; y < sz; y++)
            {
                for (int x = 0; x < sz; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), c) / rad;
                    int i = y * sz + x;
                    cPix[i] = new Color(1f, 1f, 1f, Mathf.Clamp01((1f - d) * 16f));
                    float ringD = Mathf.Abs(d - 0.85f);
                    float rim = Mathf.Clamp01((0.15f - ringD) * 16f);
                    float fill = d < 0.85f ? 0.22f : 0f;
                    rPix[i] = new Color(1f, 1f, 1f, Mathf.Clamp01(rim + fill));
                }
            }
            circleTex.SetPixels(cPix); circleTex.Apply();
            ringTex.SetPixels(rPix); ringTex.Apply();
        }

        public static void DrawCircle(Vector2 center, float radius, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(new Rect(center.x - radius, center.y - radius, radius * 2f, radius * 2f), circleTex);
            GUI.color = Color.white;
        }

        public static void DrawRing(Vector2 center, float radius, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(new Rect(center.x - radius, center.y - radius, radius * 2f, radius * 2f), ringTex);
            GUI.color = Color.white;
        }

        public static void DrawButton(Vector2 center, float radius, Color color, string label, bool pressed)
        {
            float r = pressed ? radius * 1.08f : radius;
            DrawCircle(center, r, color);
            DrawRing(center, r, new Color(1f, 1f, 1f, 0.75f));

            if (btnStyle == null)
            {
                btnStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = Color.white }
                };
            }
            btnStyle.fontSize = Mathf.Clamp(Mathf.RoundToInt(r * 0.36f), 10, 24);
            GUI.Label(new Rect(center.x - r, center.y - r, r * 2f, r * 2f), label, btnStyle);
        }
    }
}
