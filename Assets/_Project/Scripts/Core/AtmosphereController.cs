using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Deadzone.Core
{
    /// <summary>
    /// Configures cinematic post-processing, atmospheric fog, and directional
    /// lighting to deliver a grounded, gritty outbreak aesthetic for URP Mobile.
    /// </summary>
    public class AtmosphereController : MonoBehaviour
    {
        public static AtmosphereController Instance { get; private set; }

        [Header("Atmospheric Fog")]
        [SerializeField] private bool enableFog = true;
        [SerializeField] private Color fogColor = new Color(0.06f, 0.09f, 0.14f); // Dark industrial haze
        [SerializeField] private float fogDensity = 0.012f;

        [Header("Ambient Lighting")]
        [SerializeField] private Color ambientColor = new Color(0.20f, 0.25f, 0.32f); // Cool blue-grey ambient
        [SerializeField] private Color directionalLightColor = new Color(0.85f, 0.90f, 0.95f);
        [SerializeField] private float directionalIntensity = 0.9f;

        private Volume globalVolume;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            ApplyAtmosphere();
            SetupPostProcessing();
        }

        public void ApplyAtmosphere()
        {
            // 1. Fog setup
            RenderSettings.fog = enableFog;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogDensity = fogDensity;

            // 2. Ambient light setup
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = ambientColor;

            // 3. Directional sun adjustments
            Light sun = RenderSettings.sun;
            if (sun != null)
            {
                sun.color = directionalLightColor;
                sun.intensity = directionalIntensity;
                sun.shadows = LightShadows.Soft;
            }
        }

        private void SetupPostProcessing()
        {
            globalVolume = gameObject.GetComponent<Volume>();
            if (globalVolume == null) globalVolume = gameObject.AddComponent<Volume>();
            globalVolume.isGlobal = true;

            VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "Deadzone_RuntimeProfile";

            // Bloom (glowing tracers, muzzle sparks, objective terminals)
            Bloom bloom = profile.Add<Bloom>(true);
            bloom.threshold.value = 1.1f;
            bloom.intensity.value = 0.85f;
            bloom.scatter.value = 0.65f;

            // Vignette (dark claustrophobic tactical edges)
            Vignette vignette = profile.Add<Vignette>(true);
            vignette.intensity.value = 0.32f;
            vignette.smoothness.value = 0.45f;
            vignette.color.value = new Color(0.02f, 0.02f, 0.05f);

            // Color Adjustments (gritty desaturation + punchy contrast)
            ColorAdjustments colorAdj = profile.Add<ColorAdjustments>(true);
            colorAdj.contrast.value = 22f;
            colorAdj.saturation.value = -12f;
            colorAdj.colorFilter.value = new Color(0.90f, 0.95f, 1.0f); // Cool tint

            // Tonemapping (cinematic highlight roll-off)
            Tonemapping tonemapping = profile.Add<Tonemapping>(true);
            tonemapping.mode.value = TonemappingMode.Neutral;

            globalVolume.profile = profile;
        }
    }
}
