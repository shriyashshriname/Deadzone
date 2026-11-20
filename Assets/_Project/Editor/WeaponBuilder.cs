using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Deadzone.Combat;
using Deadzone.Data;

namespace Deadzone.Editor
{
    /// <summary>
    /// M1 weapons: imports the Quaternius Sci-Fi Essentials guns (processed by
    /// scripts/blender_creature_import.py), builds normalised gun prefabs (barrel +Z, grip at origin)
    /// in futuristic (glowing) and classic (gunmetal) finishes, creates the futuristic WeaponData pool in
    /// Resources/Weapons (enemy drops), gives the classic weapons models, and adds WeaponVisual to the
    /// player. Idempotent. Headless: -executeMethod Deadzone.Editor.WeaponBuilder.BuildAll
    /// </summary>
    public static class WeaponBuilder
    {
        const string Art = "Assets/_Project/Art/Weapons";
        const string PrefabDir = "Assets/_Project/Prefabs/Weapons";
        const string PoolDir = "Assets/_Project/Resources/Weapons";
        const string ResMatDir = "Assets/_Project/Resources/Materials";
        const string ClassicDataDir = "Assets/_Project/Scripts/Data";
        const string ScenePath = "Assets/_Project/Scenes/Graybox_Sandbox.unity";

        // model, batch texture, real length (m)
        static readonly Dictionary<string, (string batch, float length)> Guns = new Dictionary<string, (string, float)>
        {
            {"Pistol", ("Batch1", 0.42f)}, {"Revolver", ("Batch2", 0.48f)}, {"Rifle", ("Batch1", 0.95f)}, {"Sniper", ("Batch2", 1.3f)},
        };
        const float Readability = 1.35f; // guns are tiny from 14 m up; oversize a little

        struct Gun { public string name, model; public WeaponRarity rarity; public WeaponType type; public float dmg, rate, speed, spread; public int pen, mag, pellets; public float reload, shake, noise; public Color tracer; }

        static readonly Gun[] Futuristic =
        {
            new Gun { name = "Scout Carbine", model = "Rifle", rarity = WeaponRarity.Common, type = WeaponType.AssaultRifle, dmg = 20, rate = 8, speed = 48, spread = 2.4f, pen = 1, mag = 30, pellets = 1, reload = 1.6f, shake = .1f, noise = 22, tracer = new Color(.9f,.9f,.75f,1) },
            new Gun { name = "Pulse Pistol", model = "Pistol", rarity = WeaponRarity.Rare, type = WeaponType.Pistol, dmg = 32, rate = 5, speed = 55, spread = 1.5f, pen = 1, mag = 18, pellets = 1, reload = 1.0f, shake = .08f, noise = 14, tracer = new Color(.13f,.83f,.93f,1) },
            new Gun { name = "Plasma Rifle", model = "Rifle", rarity = WeaponRarity.Epic, type = WeaponType.AssaultRifle, dmg = 27, rate = 11, speed = 52, spread = 2.0f, pen = 1, mag = 40, pellets = 1, reload = 1.6f, shake = .1f, noise = 26, tracer = new Color(.35f,1f,.45f,1) },
            new Gun { name = "Ion Revolver", model = "Revolver", rarity = WeaponRarity.Epic, type = WeaponType.Pistol, dmg = 75, rate = 2.2f, speed = 60, spread = .8f, pen = 2, mag = 6, pellets = 1, reload = 1.4f, shake = .2f, noise = 22, tracer = new Color(.66f,.33f,.97f,1) },
            new Gun { name = "Rail Sniper", model = "Sniper", rarity = WeaponRarity.Legendary, type = WeaponType.SniperRifle, dmg = 160, rate = .9f, speed = 90, spread = .2f, pen = 5, mag = 5, pellets = 1, reload = 1.9f, shake = .35f, noise = 34, tracer = new Color(.96f,.65f,.14f,1) },
        };

        static readonly Dictionary<string, string> ClassicModels = new Dictionary<string, string>
        {
            {"WeaponData_AssaultRifle", "Rifle"}, {"WeaponData_SMG", "Pistol"}, {"WeaponData_Shotgun", "Revolver"}, {"WeaponData_Sniper", "Sniper"},
        };

        [MenuItem("Deadzone/Art/Build Weapons")]
        public static void BuildAll()
        {
            Directory.CreateDirectory(PrefabDir); Directory.CreateDirectory(PoolDir); Directory.CreateDirectory(ResMatDir);
            ConfigureImports();
            var prefabs = new Dictionary<string, GameObject>();
            foreach (var g in Guns)
            {
                prefabs[$"{g.Key}_Futuristic"] = BuildGunPrefab(g.Key, GunMaterial(g.Value.batch, true), g.Value.length * Readability, $"{PrefabDir}/Gun_{g.Key}_Futuristic.prefab");
                prefabs[$"{g.Key}_Classic"] = BuildGunPrefab(g.Key, GunMaterial(g.Value.batch, false), g.Value.length * Readability, $"{PrefabDir}/Gun_{g.Key}_Classic.prefab");
            }
            foreach (var f in Futuristic) WriteFuturistic(f, prefabs[$"{f.model}_Futuristic"]);
            foreach (var kv in ClassicModels)
            {
                var d = AssetDatabase.LoadAssetAtPath<WeaponData>($"{ClassicDataDir}/{kv.Key}.asset");
                if (d == null) continue;
                d.modelPrefab = prefabs[$"{kv.Value}_Classic"];
                EditorUtility.SetDirty(d);
            }
            PickupRingMaterial();
            AddVisualToPlayer();
            AssetDatabase.SaveAssets();
            Debug.Log($"[Weapons] Built {prefabs.Count} gun prefabs, {Futuristic.Length} futuristic weapons in Resources/Weapons");
        }

        static void ConfigureImports()
        {
            foreach (var path in Directory.GetFiles($"{Art}/Models", "*.fbx").Select(p => p.Replace('\\', '/')))
            {
                var mi = (ModelImporter)AssetImporter.GetAtPath(path);
                mi.animationType = ModelImporterAnimationType.None;
                mi.importAnimation = false;
                mi.materialImportMode = ModelImporterMaterialImportMode.None;
                mi.SaveAndReimport();
            }
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { $"{Art}/Textures" }))
            {
                var ti = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid));
                ti.maxTextureSize = 1024; ti.mipmapEnabled = true; ti.SaveAndReimport();
            }
        }

        static Material GunMaterial(string batch, bool futuristic)
        {
            string path = $"{Art}/M_Gun_{batch}_{(futuristic ? "Futuristic" : "Classic")}.mat";
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
            m.shader = shader;
            m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{Art}/Textures/T_Guns_{batch}_BaseColor.png"));
            m.SetFloat("_Metallic", 0.4f);
            m.SetFloat("_Smoothness", 0.5f);
            m.EnableKeyword("_EMISSION");
            if (futuristic)
            {
                m.SetColor("_BaseColor", Color.white);
                m.SetTexture("_EmissionMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{Art}/Textures/T_Guns_{batch}_Emissive.png"));
                m.SetColor("_EmissionColor", new Color(2f, 2f, 2f));
            }
            else
            {
                m.SetColor("_BaseColor", new Color(0.55f, 0.56f, 0.55f)); // gunmetal, no glow
                m.SetTexture("_EmissionMap", null);
                m.SetColor("_EmissionColor", Color.black);
            }
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>Normalises a gun model: barrel along +Z, up +Y, grip at the origin, total length = length.</summary>
        static GameObject BuildGunPrefab(string model, Material mat, float length, string outPath)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>($"{Art}/Models/DZ_Gun_{model}.fbx");
            var root = new GameObject(Path.GetFileNameWithoutExtension(outPath));
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(asset, root.transform);
            foreach (var r in inst.GetComponentsInChildren<Renderer>()) r.sharedMaterials = Enumerable.Repeat(mat, r.sharedMaterials.Length).ToArray();
            var pivot = new GameObject("Pivot").transform;     // rotate/scale through a pivot so the model keeps its import transform
            pivot.SetParent(root.transform, false);
            inst.transform.SetParent(pivot, true);

            // 1) longest axis -> Z
            var s = VisualFit.WorldBounds(inst).size;
            if (s.x >= s.y && s.x >= s.z) pivot.rotation = Quaternion.Euler(0f, -90f, 0f) * pivot.rotation;
            else if (s.y >= s.x && s.y >= s.z) pivot.rotation = Quaternion.Euler(90f, 0f, 0f) * pivot.rotation;
            // 2) thinnest axis -> X (height on Y)
            s = VisualFit.WorldBounds(inst).size;
            if (s.x > s.y) pivot.rotation = Quaternion.Euler(0f, 0f, 90f) * pivot.rotation;

            // 3) grip faces down & back: low vertices are the grip; muzzle is the far end from them
            var verts = WorldVerts(inst);
            var b = VisualFit.WorldBounds(inst);
            float lowY = b.min.y + b.size.y * 0.3f, highY = b.max.y - b.size.y * 0.3f;
            var low = verts.Where(v => v.y < lowY).ToList();
            var high = verts.Where(v => v.y > highY).ToList();
            // A gun is top-heavy along its length (barrel/receiver); if the low mass sits above the high mass, it's upside-down.
            if (low.Count > 0 && high.Count > 0 && Mathf.Abs(low.Average(v => v.z) - b.center.z) < 0.01f) { }
            if (low.Count > 0 && low.Average(v => v.z) > b.center.z)
                pivot.rotation = Quaternion.Euler(0f, 180f, 0f) * pivot.rotation;   // grip should be behind centre

            // 4) scale to length and put the grip at the origin
            b = VisualFit.WorldBounds(inst);
            float k = length / Mathf.Max(b.size.z, 1e-4f);
            pivot.localScale *= k;
            verts = WorldVerts(inst);
            b = VisualFit.WorldBounds(inst);
            lowY = b.min.y + b.size.y * 0.3f;
            var grip = verts.Where(v => v.y < lowY).DefaultIfEmpty(b.center).Aggregate(Vector3.zero, (a, v) => a + v) / Mathf.Max(1, verts.Count(v => v.y < lowY));
            Vector3 gripTop = new Vector3(grip.x, b.center.y, grip.z);
            pivot.position -= gripTop;

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, outPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        static List<Vector3> WorldVerts(GameObject go)
        {
            var list = new List<Vector3>();
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
            {
                if (mf.sharedMesh == null) continue;
                var m = mf.transform.localToWorldMatrix;
                list.AddRange(mf.sharedMesh.vertices.Select(v => m.MultiplyPoint3x4(v)));
            }
            return list;
        }

        static void WriteFuturistic(Gun g, GameObject model)
        {
            string path = $"{PoolDir}/{g.name.Replace(" ", "")}.asset";
            var d = AssetDatabase.LoadAssetAtPath<WeaponData>(path);
            if (d == null) { d = ScriptableObject.CreateInstance<WeaponData>(); AssetDatabase.CreateAsset(d, path); }
            d.weaponName = g.name; d.weaponType = g.type; d.rarity = g.rarity; d.futuristic = true;
            d.damagePerBullet = g.dmg; d.fireRate = g.rate; d.bulletSpeed = g.speed; d.spreadAngle = g.spread;
            d.penetration = g.pen; d.magazineCapacity = g.mag; d.bulletsPerShot = g.pellets; d.reloadDuration = g.reload;
            d.cameraShakeMagnitude = g.shake; d.cameraShakeDuration = 0.08f; d.acousticNoiseRadius = g.noise;
            d.tracerColor = g.tracer; d.modelPrefab = model;
            EditorUtility.SetDirty(d);
        }

        static void PickupRingMaterial()
        {
            string path = $"{ResMatDir}/M_PickupRing.mat";
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
            m.SetColor("_BaseColor", Color.white);
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", Color.white);
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
        }

        static void AddVisualToPlayer()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            foreach (var pc in Object.FindObjectsByType<Deadzone.Player.PlayerController>(FindObjectsInactive.Include))
            {
                var wv = pc.GetComponent<WeaponVisual>() ?? pc.gameObject.AddComponent<WeaponVisual>();
                var so = new SerializedObject(wv);
                so.FindProperty("weapon").objectReferenceValue = pc.GetComponentInChildren<Weapon>(true);
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        // ---------- verification ----------
        [MenuItem("Deadzone/Art/Capture Weapons")]
        public static void Capture()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var sun = new GameObject("Key").AddComponent<Light>();
            sun.type = LightType.Directional; sun.intensity = 1.4f; sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight = new Color(.5f, .5f, .55f);
            var files = Directory.GetFiles(PrefabDir, "*.prefab").Select(p => p.Replace('\\', '/')).OrderBy(p => p).ToArray();
            for (int i = 0; i < files.Length; i++)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(files[i]));
                go.transform.position = new Vector3(0f, i * 0.55f - 2f, (i % 2) * 2.2f - 1.1f);
                var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere); // origin marker = grip
                marker.transform.position = go.transform.position; marker.transform.localScale = Vector3.one * 0.06f;
            }
            // Side view (camera on -X looking +X): barrels should point right (+Z).
            CaptureUtil.Render("weapons_warmup", new Vector3(-7f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f), 45f);
            CaptureUtil.Render("weapons_side", new Vector3(-7f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f), 45f);
        }
    }
}
