using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Deadzone.Core;

namespace Deadzone.Editor
{
    /// <summary>
    /// Dresses Graybox_Sandbox with the Blender environment kit (scripts/blender_env_kit.py):
    /// imports + palette materials, prefabs with colliders/static flags, seeded scatter of themed
    /// clusters, perimeter dressing, grass, lamp lights, night-facility atmosphere, and a SAVED navmesh.
    /// Idempotent: deletes and rebuilds the "Environment" root each run.
    /// Headless: -executeMethod Deadzone.Editor.EnvironmentBuilder.BuildAll
    /// </summary>
    public static class EnvironmentBuilder
    {
        const string ModelDir = "Assets/_Project/Art/Environment/Models";
        const string MatDir = "Assets/_Project/Art/Environment/Materials";
        const string PrefabDir = "Assets/_Project/Prefabs/Environment";
        const string ScenePath = "Assets/_Project/Scenes/Graybox_Sandbox.unity";
        const string NavMeshAssetPath = "Assets/_Project/Scenes/Graybox_Sandbox_NavMesh.asset";
        const int Seed = 20261120;

        // Same hexes as scripts/blender_env_kit.py PAL
        static readonly Dictionary<string, string> Pal = new Dictionary<string, string>
        {
            {"Concrete","6B6E6A"},{"ConcreteDark","4A4D4A"},{"Rust","7A4A2E"},{"Metal","55595A"},
            {"MetalDark","2F3336"},{"Wood","5C4632"},{"WoodDark","3E3024"},{"Grass","6F8240"},
            {"GrassDry","A79C57"},{"GrassDark","4A5A2E"},{"Dirt","54473A"},{"Hazard","C9A227"},
            {"Rubber","1F2124"},{"Paint","5E6B5C"},{"Glass","22303A"},{"Sandbag","9C8A62"},
            {"Bulb","FFB15C"},{"Cone","D9662B"},{"White","D8D4C8"},{"Leaf","4C5A2C"},
        };

        enum Col { None, Box, Base }
        // prop -> collider, casts shadows
        static readonly Dictionary<string, (Col col, bool shadow)> Rules = new Dictionary<string, (Col, bool)>
        {
            {"Grass_A",(Col.None,false)},{"Grass_B",(Col.None,false)},{"Grass_C",(Col.None,false)},
            {"Weeds_Tall",(Col.None,false)},{"Bush_Dead",(Col.None,true)},{"Rock_A",(Col.Box,true)},
            {"Rock_B",(Col.None,true)},{"Pole_Leaning",(Col.Base,true)},{"Pole_Snapped",(Col.Base,true)},
            {"Lamp_Bent",(Col.Base,true)},{"Rebar_Chunk_A",(Col.Box,true)},{"Rebar_Chunk_B",(Col.Box,true)},
            {"Rubble_Pile",(Col.Box,true)},{"Fence_Chainlink",(Col.Box,true)},{"Fence_Broken",(Col.Box,true)},
            {"Sandbags",(Col.Box,true)},{"Tire_Stack",(Col.Box,true)},{"Car_Wreck",(Col.Box,true)},
            {"Drums_Cluster",(Col.Box,true)},{"Drum_Toppled",(Col.Box,true)},{"Pallet_Planks",(Col.None,true)},
            {"Traffic_Cone",(Col.None,true)},{"Sign_Bent",(Col.Base,true)},{"Barbed_Wire",(Col.Box,true)},
            {"Dirt_Patch_A",(Col.None,false)},{"Dirt_Patch_B",(Col.None,false)},{"Barrier_Broken",(Col.Box,true)},
            {"Debris_Planks",(Col.None,false)},{"Ground_Overgrowth",(Col.None,false)},{"Ground_Dirt_Large",(Col.None,false)},
        };

        // Cluster themes: (prop, count, radius)
        static readonly Dictionary<string, (string prop, int n, float r)[]> Themes = new Dictionary<string, (string, int, float)[]>
        {
            {"overgrowth", new[]{("Dirt_Patch_A",1,0f),("Weeds_Tall",4,2.5f),("Grass_B",6,3f),("Bush_Dead",1,2.5f),("Rock_A",1,2.5f),("Rock_B",2,3f)}},
            {"wreck", new[]{("Car_Wreck",1,0f),("Tire_Stack",1,3.2f),("Debris_Planks",1,3f),("Grass_A",5,4f),("Drum_Toppled",1,3.5f),("Dirt_Patch_B",1,2f)}},
            {"roadside", new[]{("Lamp_Bent",1,0f),("Sign_Bent",1,2.5f),("Traffic_Cone",3,3f),("Barrier_Broken",1,3f),("Grass_C",4,3f)}},
            {"construction", new[]{("Rebar_Chunk_A",1,0f),("Rebar_Chunk_B",1,3f),("Rubble_Pile",1,3f),("Pallet_Planks",1,3f),("Weeds_Tall",2,3f)}},
            {"checkpoint", new[]{("Sandbags",2,1.6f),("Barbed_Wire",1,3f),("Drums_Cluster",1,3f),("Pole_Snapped",1,3.5f),("Grass_A",3,3f)}},
            {"powerline", new[]{("Pole_Leaning",1,0f),("Fence_Broken",1,2.8f),("Weeds_Tall",3,2.5f),("Rubble_Pile",1,3f),("Grass_B",3,3f)}},
        };

        [MenuItem("Deadzone/Art/Build Environment")]
        public static void BuildAll()
        {
            var mats = BuildMaterials();
            ImportModels(mats);
            var prefabs = BuildPrefabs();
            Scatter(prefabs);
        }

        // ---------- materials / import ----------
        static Dictionary<string, Material> BuildMaterials()
        {
            Directory.CreateDirectory(MatDir);
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            var d = new Dictionary<string, Material>();
            foreach (var kv in Pal)
            {
                string path = $"{MatDir}/M_Env_{kv.Key}.mat";
                var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
                m.shader = shader;
                ColorUtility.TryParseHtmlString("#" + kv.Value, out var c);
                m.SetColor("_BaseColor", c);
                m.SetFloat("_Smoothness", kv.Key.StartsWith("Metal") || kv.Key == "Glass" ? 0.35f : 0.08f);
                m.SetFloat("_Metallic", 0f);
                m.enableInstancing = true;
                if (kv.Key == "Bulb")
                {
                    m.EnableKeyword("_EMISSION");
                    m.SetColor("_EmissionColor", c * 2.5f);
                    m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                }
                EditorUtility.SetDirty(m);
                d[kv.Key] = m;
            }
            AssetDatabase.SaveAssets();
            return d;
        }

        static void ImportModels(Dictionary<string, Material> mats)
        {
            foreach (var path in Directory.GetFiles(ModelDir, "*.fbx").Select(p => p.Replace('\\', '/')))
            {
                var mi = (ModelImporter)AssetImporter.GetAtPath(path);
                mi.animationType = ModelImporterAnimationType.None;
                mi.importAnimation = false;
                mi.importCameras = false;
                mi.importLights = false;
                mi.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                mi.isReadable = false;
                mi.meshCompression = ModelImporterMeshCompression.Medium;
                foreach (var kv in mats)
                    mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "M_Env_" + kv.Key), kv.Value);
                mi.SaveAndReimport();
            }
            // Unit sanity: the car must be ~4.1 m long.
            var car = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelDir}/Car_Wreck.fbx");
            var rb = car.GetComponentInChildren<Renderer>().bounds;
            float len = Mathf.Max(rb.size.x, rb.size.z);
            Debug.Log($"[Env] Car_Wreck imported length {len:0.00} m (expected ~4.1)");
            if (Mathf.Abs(len - 4.12f) > 0.3f)
            {
                float f = 4.12f / len;
                foreach (var path in Directory.GetFiles(ModelDir, "*.fbx").Select(p => p.Replace('\\', '/')))
                {
                    var mi = (ModelImporter)AssetImporter.GetAtPath(path);
                    mi.globalScale *= f;
                    mi.SaveAndReimport();
                }
                Debug.Log($"[Env] Rescaled kit by {f:0.###}");
            }
        }

        static Dictionary<string, GameObject> BuildPrefabs()
        {
            Directory.CreateDirectory(PrefabDir);
            var d = new Dictionary<string, GameObject>();
            foreach (var kv in Rules)
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelDir}/{kv.Key}.fbx");
                if (model == null) { Debug.LogWarning($"[Env] Missing model {kv.Key}"); continue; }
                var root = new GameObject(kv.Key);
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);
                inst.transform.localPosition = Vector3.zero;
                foreach (var r in root.GetComponentsInChildren<Renderer>())
                {
                    r.shadowCastingMode = kv.Value.shadow ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
                    r.receiveShadows = true;
                }
                var b = root.GetComponentInChildren<Renderer>().bounds;
                if (kv.Value.col == Col.Box)
                {
                    var bc = root.AddComponent<BoxCollider>();
                    bc.center = b.center; bc.size = new Vector3(Mathf.Max(b.size.x, 0.3f), Mathf.Max(b.size.y, 0.5f), Mathf.Max(b.size.z, 0.3f));
                }
                else if (kv.Value.col == Col.Base)
                {
                    var bc = root.AddComponent<BoxCollider>();
                    bc.center = new Vector3(0f, 0.9f, 0f); bc.size = new Vector3(0.45f, 1.8f, 0.45f);
                }
                GameObjectUtility.SetStaticEditorFlags(root, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
                foreach (Transform t in root.GetComponentsInChildren<Transform>())
                    GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
                d[kv.Key] = PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabDir}/{kv.Key}.prefab");
                Object.DestroyImmediate(root);
            }
            return d;
        }

        // ---------- scatter ----------
        static List<Bounds> blocked;
        static System.Random rnd;
        static float R(float a, float b) => (float)(a + rnd.NextDouble() * (b - a));

        static bool Free(Vector3 p, float r, bool ignoreSmall = false)
        {
            if (Mathf.Abs(p.x) > 58.5f - r || Mathf.Abs(p.z) > 58.5f - r) return false;
            if (new Vector2(p.x, p.z).magnitude < 8.5f + r) return false; // extraction pad + spawn
            foreach (var b in blocked)
            {
                var e = b; e.Expand(new Vector3(r * 2f, 0f, r * 2f));
                if (p.x > e.min.x && p.x < e.max.x && p.z > e.min.z && p.z < e.max.z) return false;
            }
            return true;
        }

        static GameObject Place(Dictionary<string, GameObject> pf, string name, Vector3 pos, float yaw, Transform parent, float scale = 1f)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(pf[name], parent);
            go.transform.SetPositionAndRotation(new Vector3(pos.x, 0f, pos.z), Quaternion.Euler(0f, yaw, 0f));
            go.transform.localScale = Vector3.one * scale;
            if (Rules[name].col != Col.None)
            {
                Physics.SyncTransforms();
                var c = go.GetComponent<Collider>();
                if (c != null) blocked.Add(c.bounds);
            }
            return go;
        }

        static void Scatter(Dictionary<string, GameObject> pf)
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var old = GameObject.Find("Environment");
            if (old != null) Object.DestroyImmediate(old);
            rnd = new System.Random(Seed);

            var root = new GameObject("Environment").transform;
            var props = new GameObject("Props").transform; props.SetParent(root);
            var decor = new GameObject("Decor").transform; decor.SetParent(root);
            var lights = new GameObject("Lights").transform; lights.SetParent(root);

            blocked = new List<Bounds>();
            foreach (var c in Object.FindObjectsByType<Collider>(FindObjectsInactive.Exclude))
            {
                if (c.gameObject.name == "Floor" || c.transform.IsChildOf(root)) continue;
                if (c.GetComponentInParent<CharacterController>() != null) continue;
                var b = c.bounds; b.Expand(new Vector3(1.5f, 0f, 1.5f));
                blocked.Add(b);
            }

            // 0) Big flat ground patches (overgrowth / dirt) to break up the floor; may sit under props.
            var ground = new GameObject("Ground").transform; ground.SetParent(root);
            for (int i = 0, tries = 0; i < 34 && tries < 2000; tries++)
            {
                var p = new Vector3(R(-54f, 54f), 0f, R(-54f, 54f));
                if (new Vector2(p.x, p.z).magnitude < 13f) continue;
                var g = Place(pf, i % 3 == 0 ? "Ground_Dirt_Large" : "Ground_Overgrowth", p, R(0f, 360f), ground, R(0.8f, 1.6f));
                g.transform.position += Vector3.up * (0.002f * (i % 6)); // avoid z-fighting where patches overlap
                i++;
            }

            // 1) Themed clusters
            var centers = new List<(Vector3 p, string theme)>();
            string[] themeOrder = { "overgrowth", "wreck", "roadside", "construction", "checkpoint", "powerline" };
            int lamps = 0;
            for (int tries = 0; tries < 3000 && centers.Count < 30; tries++)
            {
                var p = new Vector3(R(-55f, 55f), 0f, R(-55f, 55f));
                if (!Free(p, 3.5f) || centers.Any(c => (c.p - p).sqrMagnitude < 13f * 13f)) continue;
                string theme = themeOrder[centers.Count % themeOrder.Length];
                if (theme == "roadside" && lamps >= 6) theme = "overgrowth";
                centers.Add((p, theme));
                float baseYaw = R(0f, 360f);
                foreach (var (prop, n, r) in Themes[theme])
                {
                    for (int i = 0; i < n; i++)
                    {
                        bool solid = Rules[prop].col != Col.None;
                        Vector3 pos = p;
                        bool ok = r == 0f;
                        for (int k = 0; k < 12 && !ok; k++)
                        {
                            float a = R(0f, Mathf.PI * 2f), d = R(r * 0.4f, r);
                            pos = p + new Vector3(Mathf.Cos(a) * d, 0f, Mathf.Sin(a) * d);
                            ok = solid ? Free(pos, 0.8f) : Free(pos, 0.2f);
                        }
                        if (!ok) continue;
                        float yaw = prop.StartsWith("Sandbags") || prop.StartsWith("Fence") ? baseYaw + i * 35f : R(0f, 360f);
                        var go = Place(pf, prop, pos, yaw, solid ? props : decor, solid ? 1f : R(0.85f, 1.3f));
                        if (prop == "Lamp_Bent") { AddLampLight(go, lights); lamps++; }
                    }
                }
            }

            // 2) Perimeter dressing: fence lines, poles, weeds, rubble along the inner wall faces
            string[] edge = { "Fence_Chainlink", "Fence_Broken", "Weeds_Tall", "Rubble_Pile", "Grass_B", "Pole_Leaning", "Dirt_Patch_B", "Weeds_Tall" };
            for (int side = 0; side < 4; side++)
            {
                for (float t = -55f; t < 55f; t += R(5.5f, 9f))
                {
                    string prop = edge[rnd.Next(edge.Length)];
                    float off = prop.StartsWith("Fence") ? 1.4f : R(1.2f, 2.6f);
                    Vector3 pos = side switch
                    {
                        0 => new Vector3(t, 0, 59.5f - off), 1 => new Vector3(t, 0, -59.5f + off),
                        2 => new Vector3(59.5f - off, 0, t), _ => new Vector3(-59.5f + off, 0, t),
                    };
                    float wallYaw = side < 2 ? 0f : 90f;
                    bool solid = Rules[prop].col != Col.None;
                    if (!Free(pos, solid ? 0.6f : 0.1f)) continue;
                    Place(pf, prop, pos, prop.StartsWith("Fence") ? wallYaw : R(0f, 360f), solid ? props : decor);
                }
            }

            // 3) Grass everywhere it fits, denser near clusters and walls
            string[] grass = { "Grass_A", "Grass_B", "Grass_C", "Grass_A", "Grass_C" };
            int placedGrass = 0;
            for (int tries = 0; tries < 9000 && placedGrass < 950; tries++)
            {
                Vector3 p;
                double roll = rnd.NextDouble();
                if (roll < 0.55 && centers.Count > 0)
                {
                    var c = centers[rnd.Next(centers.Count)].p; float a = R(0, Mathf.PI * 2), d = R(2f, 8f);
                    p = c + new Vector3(Mathf.Cos(a) * d, 0, Mathf.Sin(a) * d);
                }
                else if (roll < 0.75)
                {
                    float t = R(-57f, 57f), o = R(0.8f, 5f);
                    p = rnd.Next(4) switch { 0 => new Vector3(t, 0, 59.5f - o), 1 => new Vector3(t, 0, -59.5f + o), 2 => new Vector3(59.5f - o, 0, t), _ => new Vector3(-59.5f + o, 0, t) };
                }
                else p = new Vector3(R(-57f, 57f), 0, R(-57f, 57f));
                if (!Free(p, 0.1f)) continue;
                Place(pf, grass[rnd.Next(grass.Length)], p, R(0, 360), decor, R(1.4f, 2.3f));
                placedGrass++;
            }
            // a few loose dirt patches in the open to break up the floor
            for (int i = 0, tries = 0; i < 18 && tries < 400; tries++)
            {
                var p = new Vector3(R(-55f, 55f), 0, R(-55f, 55f));
                if (!Free(p, 1.5f)) continue;
                Place(pf, i % 2 == 0 ? "Dirt_Patch_A" : "Dirt_Patch_B", p, R(0, 360), decor, R(0.9f, 1.6f)); i++;
            }

            ApplyAtmosphere();
            FixSquadmateHeight();
            BakeAndSaveNavMesh();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            int solidCount = props.childCount, decorCount = decor.childCount;
            Debug.Log($"[Env] Placed {solidCount} solid props, {decorCount} decor ({placedGrass} grass), {lamps} lamp lights, {centers.Count} clusters");
        }

        static void AddLampLight(GameObject lamp, Transform parent)
        {
            var b = lamp.GetComponentInChildren<Renderer>().bounds;
            Vector3 basePos = lamp.transform.position;
            Vector3 head = new Vector3(basePos.x + (b.center.x - basePos.x) * 1.7f, b.max.y - 0.35f, basePos.z + (b.center.z - basePos.z) * 1.7f);
            var l = new GameObject("LampLight").AddComponent<Light>();
            l.transform.SetParent(parent);
            l.transform.position = head;
            l.type = LightType.Point;
            l.color = Palette.SodiumLamp;
            l.range = 11f;
            l.intensity = 3.5f;
            l.shadows = LightShadows.None;
        }

        static void ApplyAtmosphere()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = Palette.Fog;
            RenderSettings.fogStartDistance = 24f;
            RenderSettings.fogEndDistance = 70f;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.30f, 0.33f, 0.40f); // dusk: readable on phones
            var sun = RenderSettings.sun != null ? RenderSettings.sun : Object.FindAnyObjectByType<Light>();
            if (sun != null && sun.type == LightType.Directional)
            {
                sun.color = Color.Lerp(Palette.MoonKey, Color.white, 0.35f);
                sun.intensity = 1.05f;
                sun.shadows = LightShadows.Soft;
            }
            Recolor("Mat_Wall", new Color(0.36f, 0.37f, 0.35f));          // weathered concrete
            Recolor("Mat_Cargo_Blue", new Color(0.24f, 0.32f, 0.36f));    // faded teal container
            Recolor("Mat_Cargo_Orange", new Color(0.45f, 0.26f, 0.16f));  // rusted container
            Recolor("Mat_Barrel", Palette.Danger);                         // explosive = danger accent
            Recolor("Mat_Substation", Palette.Props);                      // hazard yellow objective
            Recolor("Mat_ExtractionZone", Palette.WithAlpha(Palette.Objective * 0.75f, 1f));
            var floor = GameObject.Find("Floor");
            if (floor != null && floor.TryGetComponent<Renderer>(out var fr) && fr.sharedMaterial != null)
            {
                fr.sharedMaterial.SetColor("_BaseColor", Palette.Floor);
                EditorUtility.SetDirty(fr.sharedMaterial);
            }
            if (Object.FindAnyObjectByType<UnityEngine.Camera>() is UnityEngine.Camera cam)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Palette.Fog;
            }
        }

        static void Recolor(string matName, Color c)
        {
            var guid = AssetDatabase.FindAssets($"{matName} t:Material").FirstOrDefault();
            if (guid == null) return;
            var m = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            if (m == null) return;
            c.a = 1f;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.1f);
            EditorUtility.SetDirty(m);
        }

        static void FixSquadmateHeight()
        {
            var duo = GameObject.Find("Duo_Squadmate");
            if (duo != null) Grounding.SnapToGround(duo, 0f);
        }

        static void BakeAndSaveNavMesh()
        {
            var surface = Object.FindAnyObjectByType<NavMeshSurface>();
            if (surface == null) { Debug.LogWarning("[Env] No NavMeshSurface"); return; }
            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;
            surface.BuildNavMesh();
            // Persist the data; an unsaved NavMeshData is dropped on reload, so builds had no navmesh.
            AssetDatabase.DeleteAsset(NavMeshAssetPath);
            AssetDatabase.CreateAsset(surface.navMeshData, NavMeshAssetPath);
            EditorUtility.SetDirty(surface);
            Debug.Log($"[Env] NavMesh baked and saved to {NavMeshAssetPath}");
        }
    }
}
