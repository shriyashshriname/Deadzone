using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using Deadzone.Core;

namespace Deadzone.Editor
{
    /// <summary>
    /// Puts CC0 creatures (Quaternius Ultimate Monsters / Sci-Fi Essentials, processed by
    /// scripts/blender_creature_import.py) on enemy prefabs: generic-rig import, per-creature
    /// controller (locomotion + attack/hit/death), materials, fitted visual, measured stride.
    /// Idempotent. Headless: -executeMethod Deadzone.Editor.CreatureBuilder.BuildAll
    /// </summary>
    public static class CreatureBuilder
    {
        const string Root = "Assets/_Project/Art/Creatures";
        const string ModelDir = Root + "/Models";
        const string MatDir = Root + "/Materials";
        const string CtrlDir = Root + "/Controllers";
        const string Prefabs = "Assets/_Project/Prefabs";
        const string VisualName = "Visual";

        public struct Spec
        {
            public string prefab, model, material;
            public VisualFit.Mode fit; public float size; // Height: multiplier of collider height; Width: metres
            public float yaw, fallbackSpeed;
            public string attack, hit, death;
        }

        public static readonly Spec[] Creatures =
        {
            new Spec { prefab = "Runner_Prefab", model = "DZ_Demon", material = "Monster", fit = VisualFit.Mode.Height, size = 1.05f, yaw = 0f, fallbackSpeed = 4f, attack = "punch", hit = "hitreact", death = "death" },
            new Spec { prefab = "Spitter_Prefab", model = "DZ_Trilobite", material = "Robot", fit = VisualFit.Mode.Width, size = 2.0f, yaw = 0f, fallbackSpeed = 2.5f, attack = "attack", hit = "hit", death = null },
            new Spec { prefab = "Crawler_King_Prefab", model = "DZ_Yeti", material = "Monster", fit = VisualFit.Mode.Height, size = 1.45f, yaw = 0f, fallbackSpeed = 4f, attack = "punch", hit = "hitreact", death = "death" },
        };

        [MenuItem("Deadzone/Art/Build Creatures")]
        public static void BuildAll()
        {
            Directory.CreateDirectory(MatDir); Directory.CreateDirectory(CtrlDir);
            ConfigureTextures();
            foreach (var c in Creatures)
            {
                string modelPath = $"{ModelDir}/{c.model}.fbx";
                if (!File.Exists(modelPath)) { Debug.LogWarning($"[Creature] Missing {modelPath}"); continue; }
                var avatar = ConfigureModel(modelPath);
                var ctrl = BuildController(c, modelPath);
                string path = $"{Prefabs}/{c.prefab}.prefab";
                var contents = PrefabUtility.LoadPrefabContents(path);
                ApplyCreature(contents, c, modelPath, avatar, ctrl);
                PrefabUtility.SaveAsPrefabAsset(contents, path);
                PrefabUtility.UnloadPrefabContents(contents);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Creature] Done");
        }

        static void ConfigureTextures()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Root }))
            {
                var p = AssetDatabase.GUIDToAssetPath(guid);
                var ti = (TextureImporter)AssetImporter.GetAtPath(p);
                ti.maxTextureSize = 1024;
                ti.sRGBTexture = true;
                ti.mipmapEnabled = true;
                ti.SaveAndReimport();
            }
        }

        static Avatar ConfigureModel(string path)
        {
            var mi = (ModelImporter)AssetImporter.GetAtPath(path);
            mi.animationType = ModelImporterAnimationType.Generic;
            mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            mi.importAnimation = true;
            mi.materialImportMode = ModelImporterMaterialImportMode.None;
            mi.meshCompression = ModelImporterMeshCompression.Medium;
            mi.SaveAndReimport();
            var clips = mi.defaultClipAnimations;
            foreach (var c in clips)
            {
                string n = c.name.ToLower();
                c.loopTime = n.Contains("idle") || n.Contains("walk") || n.Contains("run");
                c.loopPose = c.loopTime;
            }
            mi.clipAnimations = clips;
            mi.SaveAndReimport();
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault();
        }

        static AnimationClip Clip(string modelPath, string keyword)
        {
            if (string.IsNullOrEmpty(keyword)) return null;
            var all = AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview")).ToArray();
            return all.FirstOrDefault(c => c.name.ToLower() == keyword) ?? all.FirstOrDefault(c => c.name.ToLower().Contains(keyword));
        }

        static AnimatorController BuildController(Spec c, string modelPath)
        {
            var ctrl = AnimatorControllerUtil.Reset($"{CtrlDir}/{c.model}.controller");
            var sm = ctrl.layers[0].stateMachine;
            ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
            ctrl.AddParameter(new AnimatorControllerParameter { name = "AnimSpeed", type = AnimatorControllerParameterType.Float, defaultFloat = 1f });
            var loco = AnimatorControllerUtil.AddLocomotion(ctrl, sm,
                (Clip(modelPath, "idle"), 0f), (Clip(modelPath, "walk"), 0.45f), (Clip(modelPath, "run"), 1f));
            AnimatorControllerUtil.AddDeath(ctrl, sm, Clip(modelPath, c.death));
            AnimatorControllerUtil.AddOneShot(ctrl, sm, loco, "Attack", Clip(modelPath, c.attack), 1.2f);
            AnimatorControllerUtil.AddOneShot(ctrl, sm, loco, "Hit", Clip(modelPath, c.hit));
            EditorUtility.SetDirty(ctrl);
            return ctrl;
        }

        static Material GetMaterial(string kind)
        {
            string path = $"{MatDir}/M_Creature_{kind}.mat";
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
            m.shader = shader;
            m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_Metallic", 0f);
            m.EnableKeyword("_EMISSION");
            if (kind == "Robot")
            {
                m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{Root}/Textures/T_Enemies_Large_BaseColor.png"));
                m.SetTexture("_EmissionMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{Root}/Textures/T_Enemies_Large_Emissive.png"));
                m.SetColor("_EmissionColor", new Color(1.6f, 1.6f, 1.6f));
                m.SetFloat("_Smoothness", 0.45f);
                m.SetFloat("_Metallic", 0.3f);
            }
            else
            {
                m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{Root}/T_Atlas_Monsters.png"));
                m.SetColor("_BaseColor", new Color(0.86f, 0.84f, 0.84f)); // knock the cartoon brightness down a touch
                m.SetColor("_EmissionColor", Color.black);
                m.SetFloat("_Smoothness", 0.15f);
            }
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            return m;
        }

        public static void ApplyCreature(GameObject root, Spec c, string modelPath, Avatar avatar, RuntimeAnimatorController ctrl)
        {
            var old = root.transform.Find(VisualName);
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var mr = root.GetComponent<MeshRenderer>(); if (mr != null) Object.DestroyImmediate(mr, true);
            var mf = root.GetComponent<MeshFilter>(); if (mf != null) Object.DestroyImmediate(mf, true);

            var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            var vis = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);
            vis.name = VisualName;
            var mat = GetMaterial(c.material);
            foreach (var smr in vis.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                smr.sharedMaterials = Enumerable.Repeat(mat, smr.sharedMaterials.Length).ToArray();
                smr.quality = SkinQuality.Bone2;
                smr.updateWhenOffscreen = false;
            }
            foreach (var r in vis.GetComponentsInChildren<MeshRenderer>())
                r.sharedMaterials = Enumerable.Repeat(mat, r.sharedMaterials.Length).ToArray();

            float target = c.size;
            if (c.fit == VisualFit.Mode.Height)
            {
                VisualFit.ColliderBottomWorldY(root, out float h);
                target = h * c.size;
            }
            VisualFit.Fit(root, vis, c.fit, target, c.yaw);

            var anim = vis.GetComponent<Animator>() ?? vis.AddComponent<Animator>();
            anim.runtimeAnimatorController = ctrl;
            anim.avatar = avatar;
            anim.applyRootMotion = false;
            anim.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

            var driver = vis.AddComponent<CharacterAnimDriver>();
            float measured = VisualFit.MeasureGroundSpeed(vis, Clip(modelPath, "run"));
            driver.Configure(measured > 0f ? measured : c.fallbackSpeed);
            Debug.Log($"[Creature] {root.name} <- {c.model}: run ground speed {measured:0.00} m/s (fallback {c.fallbackSpeed}), size {target:0.00} m ({c.fit})");
            EditorUtility.SetDirty(driver);
        }

        // ---------- verification ----------
        [MenuItem("Deadzone/Art/Capture Creatures")]
        public static void Capture()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.localScale = Vector3.one * 3f;
            var gm = new Material(Shader.Find("Universal Render Pipeline/Lit")); gm.SetColor("_BaseColor", Palette.Floor);
            ground.GetComponent<Renderer>().sharedMaterial = gm;
            var sun = new GameObject("Key").AddComponent<Light>();
            sun.type = LightType.Directional; sun.intensity = 1.3f; sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(40f, 200f, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.45f, 0.47f, 0.52f);

            for (int i = 0; i < Creatures.Length; i++)
            {
                var c = Creatures[i];
                string modelPath = $"{ModelDir}/{c.model}.fbx";
                var src = AssetDatabase.LoadAssetAtPath<GameObject>($"{Prefabs}/{c.prefab}.prefab");
                var root = new GameObject(c.prefab);
                root.transform.localScale = src.transform.localScale;
                var cap = root.AddComponent<CapsuleCollider>();
                root.transform.position = new Vector3((i - 1) * 4.5f, src.transform.localScale.y, 0f);
                ApplyCreature(root, c, modelPath, AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<Avatar>().FirstOrDefault(),
                    AssetDatabase.LoadAssetAtPath<AnimatorController>($"{CtrlDir}/{c.model}.controller"));
                var arrow = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Object.DestroyImmediate(arrow.GetComponent<Collider>());
                arrow.transform.position = new Vector3(root.transform.position.x, 0.03f, 1.6f);
                arrow.transform.localScale = new Vector3(0.15f, 0.05f, 1.2f);
                var am = new Material(Shader.Find("Universal Render Pipeline/Lit")); am.SetColor("_BaseColor", Palette.Objective);
                arrow.GetComponent<Renderer>().sharedMaterial = am;
            }
            CaptureUtil.Render("creatures_warmup", new Vector3(0f, 2.5f, 13f), Quaternion.Euler(8f, 180f, 0f), 45f);
            CaptureUtil.Render("creatures_front", new Vector3(0f, 2.5f, 13f), Quaternion.Euler(8f, 180f, 0f), 45f);
            CaptureUtil.Render("creatures_gamecam", new Vector3(0f, 11.5f, -8f), Quaternion.Euler(55f, 0f, 0f), 45f);
        }
    }

    public static class CaptureUtil
    {
        public static void Render(string name, Vector3 pos, Quaternion rot, float fov)
        {
            var cam = new GameObject("CaptureCam").AddComponent<UnityEngine.Camera>();
            cam.transform.SetPositionAndRotation(pos, rot);
            cam.fieldOfView = fov;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Palette.Fog;
            var rt = new RenderTexture(1600, 700, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            Directory.CreateDirectory("Captures");
            File.WriteAllBytes($"Captures/{name}.png", tex.EncodeToPNG());
            RenderTexture.active = null;
            cam.targetTexture = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(cam.gameObject);
            Debug.Log($"[Capture] Captures/{name}.png");
        }
    }
}
