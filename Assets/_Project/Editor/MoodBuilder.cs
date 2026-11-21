using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Deadzone.Core;

namespace Deadzone.Editor
{
    /// <summary>
    /// "Mood pass" from the founder's reference images (Art/image refernce, 2026-11-21): night + rain.
    /// Editor side only — the runtime look (fog, ambient, moon, grading, rain, lightning) lives in
    /// Core/AtmosphereController + Core/WeatherRain. This builder:
    ///  - switches the URP renderer to Forward+ so every lamp lights the (single, huge) floor,
    ///  - generates rain textures/materials into Resources/FX,
    ///  - makes the ground and hard surfaces wet (smoothness),
    ///  - sets the gameplay camera angle,
    ///  - tunes lamp lights and gives some car wrecks headlights.
    /// Idempotent. Called at the end of EnvironmentBuilder.Scatter, or headless:
    /// -executeMethod Deadzone.Editor.MoodBuilder.Apply
    /// </summary>
    public static class MoodBuilder
    {
        const string ScenePath = "Assets/_Project/Scenes/Graybox_Sandbox.unity";
        const string FxDir = "Assets/_Project/Resources/FX";
        const string RendererPath = "Assets/_Project/Settings/Deadzone_RendererData.asset";

        // Gameplay camera: a bit lower and closer than before so building/prop faces read like the refs.
        public const float CameraPitch = 52f;
        public const float CameraDistance = 14f; // Build #9: closer so the hero reads bigger in landscape

        [MenuItem("Deadzone/Art/Apply Night Rain Mood")]
        public static void Apply()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ApplyInOpenScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        public static void ApplyInOpenScene()
        {
            SetupRenderer();
            BuildRainAssets();
            MakeSurfacesWet();
            SetupCamera();
            TuneLamps();
            AddCarHeadlights();
            AssetDatabase.SaveAssets();
            Debug.Log("[Mood] Night-rain mood applied");
        }

        // ---------- renderer ----------
        static void SetupRenderer()
        {
            var rd = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (rd == null) { Debug.LogWarning("[Mood] Renderer data not found"); return; }
            // Forward keeps a per-OBJECT light limit; the floor is one 120 m mesh, so only 2 lamp pools
            // could ever show on the ground. Forward+ lights per screen tile instead.
            rd.renderingMode = RenderingMode.ForwardPlus;
            // Without PostProcessData URP silently skips ALL post (bloom, grading, vignette). It was unset.
            if (rd.postProcessData == null)
            {
                rd.postProcessData = AssetDatabase.LoadAssetAtPath<PostProcessData>(
                    "Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset");
                Debug.Log($"[Mood] PostProcessData assigned: {rd.postProcessData != null}");
            }
            EditorUtility.SetDirty(rd);
        }

        // ---------- rain textures + materials ----------
        static void BuildRainAssets()
        {
            Directory.CreateDirectory(FxDir);
            var streakTex = WriteTexture($"{FxDir}/T_RainStreak.png", 8, 64, (u, v) =>
            {
                float across = 1f - Mathf.Abs(u * 2f - 1f);               // soft sides
                float along = Mathf.Sin(v * Mathf.PI);                     // fades at both ends
                return Mathf.Clamp01(Mathf.Pow(across, 1.5f) * along);
            });
            var splashTex = WriteTexture($"{FxDir}/T_RainSplash.png", 64, 64, (u, v) =>
            {
                float d = new Vector2(u - 0.5f, v - 0.5f).magnitude * 2f; // 0 centre .. 1 edge
                float ring = Mathf.Exp(-Mathf.Pow((d - 0.72f) / 0.11f, 2f));
                return Mathf.Clamp01(ring + 0.15f * Mathf.Max(0f, 1f - d));
            });
            MakeAdditiveParticleMat($"{FxDir}/M_RainStreak.mat", streakTex);
            MakeAdditiveParticleMat($"{FxDir}/M_RainSplash.mat", splashTex);
            MakeAdditiveParticleMat($"{FxDir}/M_AimLine.mat", null); // sniper range-finder line (vertex-coloured)
        }

        static Texture2D WriteTexture(string path, int w, int h, System.Func<float, float, float> alpha)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float a = alpha((x + 0.5f) / w, (y + 0.5f) / h);
                    t.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            t.Apply();
            File.WriteAllBytes(path, t.EncodeToPNG());
            Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(path);
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureType = TextureImporterType.Default;
            ti.alphaSource = TextureImporterAlphaSource.FromInput;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = false;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static void MakeAdditiveParticleMat(string path, Texture2D tex)
        {
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
            m.shader = shader;
            m.SetTexture("_BaseMap", tex);
            m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_Surface", 1f);            // transparent
            m.SetFloat("_Blend", 2f);              // additive
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            m.SetFloat("_ZWrite", 0f);
            m.SetFloat("_Cull", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
        }

        // ---------- wet surfaces ----------
        static void MakeSurfacesWet()
        {
            var floor = GameObject.Find("Floor");
            if (floor != null && floor.TryGetComponent<Renderer>(out var fr) && fr.sharedMaterial != null)
            {
                var m = fr.sharedMaterial;
                m.SetColor("_BaseColor", new Color(0.17f, 0.18f, 0.19f)); // dark wet asphalt
                m.SetFloat("_Smoothness", 0.66f);
                m.SetFloat("_Metallic", 0f);
                m.SetFloat("_SpecularHighlights", 1f);
                m.DisableKeyword("_SPECULARHIGHLIGHTS_OFF");
                EditorUtility.SetDirty(m);
            }
            SetSmooth("Mat_Wall", 0.28f);
            SetSmooth("Mat_Cargo_Blue", 0.4f);
            SetSmooth("Mat_Cargo_Orange", 0.4f);
            const string env = "Assets/_Project/Art/Environment/Materials";
            SetSmoothAt($"{env}/M_Env_Concrete.mat", 0.3f);
            SetSmoothAt($"{env}/M_Env_ConcreteDark.mat", 0.3f);
            SetSmoothAt($"{env}/M_Env_Metal.mat", 0.5f);
            SetSmoothAt($"{env}/M_Env_MetalDark.mat", 0.5f);
            SetSmoothAt($"{env}/M_Env_Rust.mat", 0.32f);
            SetSmoothAt($"{env}/M_Env_Rubber.mat", 0.45f);
            SetSmoothAt($"{env}/M_Env_Paint.mat", 0.4f);
            SetSmoothAt($"{env}/M_Env_Glass.mat", 0.85f);

            // Vegetation read as bright lawn green at night; pull it toward a dark, muted olive.
            // Computed from the source palette so re-running stays idempotent.
            foreach (var key in new[] { "Grass", "GrassDry", "GrassDark", "Leaf", "Olive", "Dirt" })
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>($"{env}/M_Env_{key}.mat");
                if (m == null || !EnvironmentBuilder.Pal.TryGetValue(key, out var hex)) continue;
                ColorUtility.TryParseHtmlString("#" + hex, out var c);
                Color.RGBToHSV(c, out float h, out float s, out float v);
                m.SetColor("_BaseColor", Color.HSVToRGB(h, s * 0.6f, v * (key == "Dirt" ? 0.8f : 0.55f)));
                EditorUtility.SetDirty(m);
            }
        }

        static void SetSmooth(string matName, float s)
        {
            var guid = AssetDatabase.FindAssets($"{matName} t:Material").FirstOrDefault();
            if (guid != null) SetSmoothAt(AssetDatabase.GUIDToAssetPath(guid), s);
        }

        static void SetSmoothAt(string path, float s)
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null || !m.HasProperty("_Smoothness")) return;
            m.SetFloat("_Smoothness", s);
            EditorUtility.SetDirty(m);
        }

        // ---------- camera ----------
        static void SetupCamera()
        {
            var cam = Object.FindAnyObjectByType<Deadzone.Camera.TopDownCamera>();
            if (cam == null) { Debug.LogWarning("[Mood] No TopDownCamera"); return; }
            float p = CameraPitch * Mathf.Deg2Rad;
            var offset = new Vector3(0f, Mathf.Sin(p) * CameraDistance, -Mathf.Cos(p) * CameraDistance);
            var so = new SerializedObject(cam);
            so.FindProperty("offset").vector3Value = offset;
            so.FindProperty("pitchAngle").floatValue = CameraPitch;
            so.ApplyModifiedPropertiesWithoutUndo();
            cam.transform.SetPositionAndRotation(offset, Quaternion.Euler(CameraPitch, 0f, 0f));
            var c = cam.GetComponent<UnityEngine.Camera>();
            c.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            c.farClipPlane = 80f;
            Debug.Log($"[Mood] Camera pitch {CameraPitch} offset {offset}");
        }

        // ---------- lights ----------
        static void TuneLamps()
        {
            var lights = GameObject.Find("Environment/Lights");
            if (lights == null) return;
            int n = 0;
            foreach (var l in lights.GetComponentsInChildren<Light>())
            {
                if (l.name != "LampLight") continue;
                l.color = Palette.SodiumLamp;
                l.range = 16f;
                l.intensity = 30f;
                l.shadows = LightShadows.None;
                n++;
            }
            Debug.Log($"[Mood] Tuned {n} lamp lights");
        }

        static void AddCarHeadlights()
        {
            var props = GameObject.Find("Environment/Props");
            var lights = GameObject.Find("Environment/Lights");
            if (props == null || lights == null) return;
            foreach (Transform t in lights.transform.Cast<Transform>().ToList())
                if (t.name == "Headlight") Object.DestroyImmediate(t.gameObject);

            int lit = 0;
            foreach (Transform car in props.transform)
            {
                if (!car.name.StartsWith("Car_Wreck") || lit >= 4) continue;
                var r = car.GetComponentInChildren<Renderer>();
                if (r == null) continue;
                var b = r.bounds;
                // front = the end of the long axis; car models face local +Z or +X, use the longer one.
                Vector3 fwd = b.size.x > b.size.z ? car.right : car.forward;
                fwd.y = 0f; fwd.Normalize();
                float half = Mathf.Max(b.size.x, b.size.z) * 0.5f;
                Vector3 side = Vector3.Cross(Vector3.up, fwd);
                foreach (float s in new[] { -0.55f, 0.55f })
                {
                    var l = new GameObject("Headlight").AddComponent<Light>();
                    l.transform.SetParent(lights.transform);
                    l.transform.position = new Vector3(b.center.x, 0.75f, b.center.z) + fwd * (half + 0.05f) + side * s;
                    l.transform.rotation = Quaternion.LookRotation(fwd + Vector3.down * 0.22f);
                    l.type = LightType.Spot;
                    l.spotAngle = 55f;
                    l.innerSpotAngle = 25f;
                    l.range = 13f;
                    l.intensity = 16f;
                    l.color = new Color(0.95f, 0.93f, 0.85f);
                    l.shadows = LightShadows.None;
                }
                lit++;
            }
            Debug.Log($"[Mood] Headlights on {lit} car wrecks");
        }
    }
}
