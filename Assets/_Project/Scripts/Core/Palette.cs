using UnityEngine;

namespace Deadzone.Core
{
    /// <summary>
    /// Single source of colour for DEADZONE (art bible, Docs/ROADMAP_NEXT.md §4).
    /// Style: grounded low-poly outbreak. Mood: night facility on emergency power.
    /// Materials, lights, fog, particles and UI should read from here, never from literal Color values.
    /// </summary>
    public static class Palette
    {
        // World
        public static readonly Color Floor          = Hex(0x3B3D3A); // stained concrete
        public static readonly Color Structure      = Hex(0x55595A); // gunmetal
        public static readonly Color Props          = Hex(0xC9A227); // hazard yellow
        public static readonly Color Shadow         = Hex(0x1D2228); // cold blue-black

        // Characters
        public static readonly Color OperatorPrimary   = Hex(0x4F5A3C); // olive drab
        public static readonly Color OperatorSecondary = Hex(0xB8A47A); // coyote tan
        public static readonly Color InfectedSkin      = Hex(0x8A9479); // sickly grey-green

        // Gameplay accents (saturated cues against the desaturated world)
        public static readonly Color Health    = Hex(0x2ECC71); // emerald
        public static readonly Color Ammo      = Hex(0xF5A623); // amber
        public static readonly Color Objective = Hex(0x22D3EE); // cyan
        public static readonly Color Danger    = Hex(0xD7263D); // crimson / acid / blood

        // Lighting mood
        public static readonly Color MoonKey     = Hex(0x9FB4D8); // cool moonlight key
        public static readonly Color SodiumLamp  = Hex(0xFFB15C); // warm floor pools
        public static readonly Color AlarmStrobe = Hex(0xFF3B30); // objective strobes
        public static readonly Color Fog         = Hex(0x232A33); // swallows far edges

        // UI
        public static readonly Color UIText    = Hex(0xE6E2D6); // warm off-white, never pure white
        public static readonly Color UIOutline = Hex(0x14181D);

        public static Color WithAlpha(Color c, float a) { c.a = a; return c; }

        static Color Hex(int rgb) =>
            new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);
    }
}
