using UnityEngine;

namespace Deadzone.UI
{
    /// <summary>
    /// Resolution-independent IMGUI for the HUD panels. Call <see cref="Begin"/> at the top of OnGUI, then lay
    /// out in virtual units using <see cref="W"/> / <see cref="H"/> instead of Screen.width / Screen.height.
    /// The short side of the safe area is always <see cref="ShortSide"/> (480) units, so panels keep the same
    /// proportions on every phone, and everything stays clear of notches and the navigation bar.
    /// Raw-pixel OnGUI code (touch sticks, world-anchored labels) must reset GUI.matrix to identity itself.
    /// </summary>
    public static class UIScale
    {
        public const float ShortSide = 480f;

        /// <summary>Virtual width / height of the safe area in UI units.</summary>
        public static float W { get; private set; } = ShortSide;
        public static float H { get; private set; } = ShortSide;
        /// <summary>Pixels per UI unit.</summary>
        public static float K { get; private set; } = 1f;
        public static bool Portrait => H > W;

        public static void Begin()
        {
            Rect safe = Screen.safeArea;
            if (safe.width < 1f || safe.height < 1f) safe = new Rect(0, 0, Screen.width, Screen.height);
            K = Mathf.Max(0.5f, Mathf.Min(safe.width, safe.height) / ShortSide);
            W = safe.width / K;
            H = safe.height / K;
            // safeArea is y-up; GUI space is y-down.
            var offset = new Vector3(safe.xMin, Screen.height - safe.yMax, 0f);
            GUI.matrix = Matrix4x4.TRS(offset, Quaternion.identity, new Vector3(K, K, 1f));
        }
    }
}
