using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Deadzone.Core
{
    /// <summary>
    /// Runtime owner of the zone mood (spawned by GameManager). Night + rain look from the founder's
    /// reference images (Art/image refernce): cool blue-black ambient and fog, weak moonlight, warm sodium
    /// lamp pools (scene lights), teal-shadow / amber-highlight grading, bloom on lamps, vignette,
    /// camera-following rain and occasional lightning. Mobile-safe: no HDR, no extra shadow casters.
    /// </summary>
    public class AtmosphereController : MonoBehaviour
    {
        public static AtmosphereController Instance { get; private set; }

        [Header("Fog (linear, swallows the far edge of the screen)")]
        [SerializeField] private bool enableFog = true;
        [SerializeField] private Color fogColor = new Color(0.055f, 0.075f, 0.11f);
        [SerializeField] private float fogStart = 20f;
        [SerializeField] private float fogEnd = 58f;

        [Header("Night lighting")]
        [SerializeField] private Color ambientSky = new Color(0.26f, 0.30f, 0.40f);
        [SerializeField] private Color ambientEquator = new Color(0.19f, 0.21f, 0.26f);
        [SerializeField] private Color ambientGround = new Color(0.10f, 0.10f, 0.11f);
        [SerializeField] private Color moonColor = new Color(0.62f, 0.72f, 0.95f);
        [SerializeField] private float moonIntensity = 0.8f;

        [Header("Player fill light (keeps the hero readable in the dark)")]
        [SerializeField] private bool playerFillLight = true;
        [SerializeField] private Color playerFillColor = new Color(0.80f, 0.87f, 1f);
        [SerializeField] private float playerFillIntensity = 2.6f;
        [SerializeField] private float playerFillRange = 9f;

        [Header("Weather")]
        [SerializeField] private bool rain = true;
        [SerializeField] private bool lightning = true;

        private Volume globalVolume;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
        }

        private void Start()
        {
            ApplyAtmosphere();
            SetupPostProcessing();
            EnableCameraPost();
            if (playerFillLight) AttachPlayerFillLight();
            if (rain || lightning)
            {
                var w = gameObject.AddComponent<WeatherRain>();
                w.Configure(rain, lightning, moonIntensity);
            }
        }

        public void ApplyAtmosphere()
        {
            RenderSettings.fog = enableFog;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogStartDistance = fogStart;
            RenderSettings.fogEndDistance = fogEnd;

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = ambientSky;
            RenderSettings.ambientEquatorColor = ambientEquator;
            RenderSettings.ambientGroundColor = ambientGround;

            // No bright default sky in reflections: wet surfaces should only mirror the lamps.
            RenderSettings.skybox = null;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            RenderSettings.customReflectionTexture = null;
            RenderSettings.reflectionIntensity = 0f;

            Light sun = RenderSettings.sun;
            if (sun == null)
            {
                foreach (var l in FindObjectsByType<Light>(FindObjectsInactive.Exclude))
                    if (l.type == LightType.Directional) { sun = l; break; }
            }
            if (sun != null)
            {
                sun.color = moonColor;
                sun.intensity = moonIntensity;
                sun.shadows = LightShadows.Hard;
                sun.shadowStrength = 0.75f;
            }

            var cam = UnityEngine.Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = fogColor;
            }
        }

        private void SetupPostProcessing()
        {
            globalVolume = gameObject.GetComponent<Volume>();
            if (globalVolume == null) globalVolume = gameObject.AddComponent<Volume>();
            globalVolume.isGlobal = true;
            globalVolume.priority = 10f;

            VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "Deadzone_NightRain";

            // LDR pipeline: threshold below 1 so emissive lamp heads, muzzle flashes and tracers glow.
            Bloom bloom = profile.Add<Bloom>(true);
            bloom.threshold.value = 0.82f;
            bloom.intensity.value = 0.75f;
            bloom.scatter.value = 0.7f;
            bloom.tint.value = new Color(1f, 0.86f, 0.70f);

            // The reference look: teal/blue shadows, warm amber highlights.
            SplitToning split = profile.Add<SplitToning>(true);
            split.shadows.value = new Color(0.44f, 0.53f, 0.58f);
            split.highlights.value = new Color(0.86f, 0.60f, 0.36f);
            split.balance.value = 0f;

            ColorAdjustments colorAdj = profile.Add<ColorAdjustments>(true);
            colorAdj.postExposure.value = 0.4f;
            colorAdj.contrast.value = 12f;
            colorAdj.saturation.value = -8f;

            Vignette vignette = profile.Add<Vignette>(true);
            vignette.intensity.value = 0.28f;
            vignette.smoothness.value = 0.5f;
            vignette.color.value = new Color(0.01f, 0.015f, 0.03f);

            globalVolume.profile = profile;
        }

        private static void EnableCameraPost()
        {
            var cam = UnityEngine.Camera.main;
            if (cam == null) return;
            var data = cam.GetUniversalAdditionalCameraData();
            if (data != null) data.renderPostProcessing = true;
        }

        private void AttachPlayerFillLight()
        {
            var player = Deadzone.Player.PlayerController.Instance;
            if (player == null) player = FindAnyObjectByType<Deadzone.Player.PlayerController>();
            if (player == null || player.transform.Find("PlayerFillLight") != null) return;
            var l = new GameObject("PlayerFillLight").AddComponent<Light>();
            l.transform.SetParent(player.transform, false);
            l.transform.localPosition = new Vector3(0f, 3.2f, 0f);
            l.type = LightType.Point;
            l.color = playerFillColor;
            l.intensity = playerFillIntensity;
            l.range = playerFillRange;
            l.shadows = LightShadows.None;
        }
    }
}
