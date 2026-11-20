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
    /// Replaces capsule placeholders with the rigged CC0 character (Kenney 'characterMedium', CC0)
    /// wearing DEADZONE-graded skins (scripts/grade_skins.py). Idempotent: safe to re-run, including
    /// after "Deadzone/Setup Wave Horde Prefabs and Scene" regenerates the capsule prefabs.
    /// Headless: Unity -batchmode -projectPath . -executeMethod Deadzone.Editor.CharacterArtBuilder.BuildAll -quit
    /// </summary>
    public static class CharacterArtBuilder
    {
        const string Root = "Assets/_Project/Art/Characters";
        const string ModelPath = Root + "/Models/DZ_Character.fbx";
        const string IdlePath = Root + "/Models/DZ_Anim_Idle.fbx";
        const string RunPath = Root + "/Models/DZ_Anim_Run.fbx";
        const string ZombieAnimPath = Root + "/Models/DZ_Anim_Zombie.fbx";   // Quaternius UAL2 (CC0)
        const string ZombieControllerPath = Root + "/DZ_Zombie.controller";
        const string TexDir = Root + "/Textures";
        const string MatDir = Root + "/Materials";
        const string ControllerPath = Root + "/DZ_Locomotion.controller";
        const string Prefabs = "Assets/_Project/Prefabs";
        const string ScenePath = "Assets/_Project/Scenes/Graybox_Sandbox.unity";
        const string VisualName = "Visual";
        // Humanoid retargeting already orients the animated body to the Animator's +Z. (A 180 here only looked right
        // in a static T-pose capture; in play it made every humanoid face backwards.) Verified with in-play captures.
        const float ModelYaw = 0f;

        // prefab, skin, height multiplier vs collider, fallback reference speed (m/s), zombie animation set?
        // Runner / Spitter / Crawler King are creatures now (CreatureBuilder).
        static readonly (string prefab, string skin, float heightMul, float refSpeed, bool zombie)[] Targets =
        {
            ("Zombie_Prefab", "Walker", 1.00f, 1.2f, true),
            ("Tank_Prefab", "Brute", 1.00f, 1.2f, true),
            ("Boss_Brute_Prefab", "Brute", 1.00f, 1.5f, true),
            ("Rival_Operator_Prefab", "Rival", 1.00f, 3.0f, false),
            ("Friendly_Squadmate_Prefab", "Squadmate", 1.00f, 3.0f, false),
        };

        static Avatar avatar;
        static bool isHuman;

        [MenuItem("Deadzone/Art/Build Rigged Characters")]
        public static void BuildAll()
        {
            ConfigureImporters();
            var controller = BuildController();
            var zombieController = BuildZombieController();
            var run = LoadClip(RunPath);
            var zombieWalk = LoadClip(ZombieAnimPath, "walk");
            int n = 0;
            foreach (var t in Targets)
            {
                string path = $"{Prefabs}/{t.prefab}.prefab";
                if (!File.Exists(path)) { Debug.LogWarning($"[CharacterArt] Missing {path}"); continue; }
                var contents = PrefabUtility.LoadPrefabContents(path);
                ApplyVisual(contents, GetMaterial(t.skin), t.zombie ? zombieController : controller, t.heightMul, t.refSpeed, t.zombie ? zombieWalk : run);
                PrefabUtility.SaveAsPrefabAsset(contents, path);
                PrefabUtility.UnloadPrefabContents(contents);
                n++;
            }
            n += ApplyToScenePlayer(controller);
            AssetDatabase.SaveAssets();
            Debug.Log($"[CharacterArt] Done: {n} characters rigged. Avatar={(isHuman ? "Humanoid" : "Generic")} valid={avatar != null && avatar.isValid}");
        }

        // ---------- Import ----------
        static void ConfigureImporters()
        {
            var mi = (ModelImporter)AssetImporter.GetAtPath(ModelPath);
            mi.animationType = ModelImporterAnimationType.Human;
            mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            mi.importAnimation = false;
            mi.materialImportMode = ModelImporterMaterialImportMode.None;
            mi.SaveAndReimport();
            avatar = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Avatar>().FirstOrDefault();
            isHuman = avatar != null && avatar.isValid && avatar.isHuman;

            if (!isHuman)
            {
                Debug.LogWarning("[CharacterArt] Humanoid avatar invalid; falling back to Generic (all clips share this rig).");
                mi.animationType = ModelImporterAnimationType.Generic;
                mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                mi.SaveAndReimport();
                avatar = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Avatar>().FirstOrDefault();
            }

            ConfigureClip(IdlePath, "idle");
            ConfigureClip(RunPath, "run");
            ConfigureZombieClips();
            ConfigureTextures();
        }

        static void ConfigureClip(string path, string keyword)
        {
            var ai = (ModelImporter)AssetImporter.GetAtPath(path);
            ai.animationType = isHuman ? ModelImporterAnimationType.Human : ModelImporterAnimationType.Generic;
            // Clip files collapse the 'Root' node, so their hierarchy differs from the model's.
            // Humanoid: each clip file builds its own avatar and Mecanim retargets between them.
            // Generic: must share the model's avatar (paths would not match otherwise).
            if (isHuman) ai.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            else { ai.avatarSetup = ModelImporterAvatarSetup.CopyFromOther; ai.sourceAvatar = avatar; }
            ai.importAnimation = true;
            ai.materialImportMode = ModelImporterMaterialImportMode.None;
            ai.SaveAndReimport(); // refresh defaultClipAnimations for the new rig type

            var take = ai.defaultClipAnimations.FirstOrDefault(c => c.name.ToLower().Contains(keyword))
                       ?? ai.defaultClipAnimations.Last();
            take.name = keyword == "run" ? "DZ_Run" : "DZ_Idle";
            take.loopTime = true;
            take.loopPose = true;
            take.lockRootRotation = true;
            take.lockRootHeightY = true;
            take.lockRootPositionXZ = true;
            take.keepOriginalOrientation = true;
            take.keepOriginalPositionY = true;
            take.keepOriginalPositionXZ = true;
            ai.clipAnimations = new[] { take };
            ai.SaveAndReimport();
        }

        static void ConfigureZombieClips()
        {
            if (!File.Exists(ZombieAnimPath)) { Debug.LogWarning("[CharacterArt] No zombie animation file"); return; }
            var ai = (ModelImporter)AssetImporter.GetAtPath(ZombieAnimPath);
            ai.animationType = ModelImporterAnimationType.Human;
            ai.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            ai.importAnimation = true;
            ai.materialImportMode = ModelImporterMaterialImportMode.None;
            ai.SaveAndReimport();
            var clips = ai.defaultClipAnimations;
            foreach (var c in clips)
            {
                string n = c.name.ToLower();
                c.loopTime = n.Contains("loop");
                c.loopPose = c.loopTime;
                c.lockRootRotation = c.lockRootHeightY = c.lockRootPositionXZ = true;
                c.keepOriginalOrientation = c.keepOriginalPositionY = c.keepOriginalPositionXZ = true;
                // UAL2's mannequin faces the opposite way to Unity's humanoid forward: without this the
                // retargeted zombie walks backwards (verified with in-play captures from behind).
                c.rotationOffset = 180f;
            }
            ai.clipAnimations = clips;
            ai.SaveAndReimport();
            var avatarZ = AssetDatabase.LoadAllAssetsAtPath(ZombieAnimPath).OfType<Avatar>().FirstOrDefault();
            Debug.Log($"[CharacterArt] Zombie clips: {string.Join(", ", clips.Select(c => c.name))} avatarHuman={avatarZ != null && avatarZ.isHuman}");
        }

        static void ConfigureTextures()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { TexDir }))
            {
                var p = AssetDatabase.GUIDToAssetPath(guid);
                var ti = (TextureImporter)AssetImporter.GetAtPath(p);
                ti.textureType = TextureImporterType.Default;
                ti.sRGBTexture = true;
                ti.maxTextureSize = 1024;
                ti.mipmapEnabled = true;
                ti.filterMode = FilterMode.Bilinear;
                ti.SaveAndReimport();
            }
        }

        // ---------- Materials & Animator ----------
        static Material GetMaterial(string skin)
        {
            Directory.CreateDirectory(MatDir);
            string path = $"{MatDir}/M_Char_{skin}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (mat == null) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, path); }
            mat.shader = shader;
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexDir}/T_{skin}.png");
            mat.SetTexture("_BaseMap", tex);
            mat.SetColor("_BaseColor", Color.white);
            mat.SetFloat("_Smoothness", 0.12f);
            mat.SetFloat("_Metallic", 0f);
            mat.EnableKeyword("_EMISSION");                 // lets HitFlash push an emissive flash
            mat.SetColor("_EmissionColor", Color.black);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static AnimatorController BuildController()
        {
            var idle = LoadClip(IdlePath);
            var run = LoadClip(RunPath);
            var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath)
                       ?? AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

            // Reset to a known state so re-runs stay idempotent (keeps the asset GUID).
            foreach (var p in ctrl.parameters.ToArray()) ctrl.RemoveParameter(p);
            var sm = ctrl.layers[0].stateMachine;
            foreach (var s in sm.states.ToArray()) sm.RemoveState(s.state);
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(ControllerPath).OfType<BlendTree>().ToArray())
                AssetDatabase.RemoveObjectFromAsset(o);

            ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
            ctrl.AddParameter(new AnimatorControllerParameter
            { name = "AnimSpeed", type = AnimatorControllerParameterType.Float, defaultFloat = 1f });

            var tree = new BlendTree { name = "Locomotion", blendParameter = "Speed", useAutomaticThresholds = false };
            AssetDatabase.AddObjectToAsset(tree, ctrl);
            tree.AddChild(idle, 0f);
            tree.AddChild(run, 1f);

            var state = sm.AddState("Locomotion");
            state.motion = tree;
            state.speedParameterActive = true;
            state.speedParameter = "AnimSpeed";
            sm.defaultState = state;
            // AimIK (hands on the gun, look at target) needs the IK pass on the base layer.
            var layers = ctrl.layers;
            layers[0].iKPass = true;
            ctrl.layers = layers;
            EditorUtility.SetDirty(ctrl);
            return ctrl;
        }

        static AnimationClip LoadClip(string path) =>
            AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                .First(c => !c.name.StartsWith("__preview"));

        static AnimationClip LoadClip(string path, string keyword) =>
            AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                .FirstOrDefault(c => !c.name.StartsWith("__preview") && c.name.ToLower().Contains(keyword));

        /// <summary>Zombie set: idle/walk blend + Attack (scratch) and Hit (knockback) one-shots. No death clip:
        /// CharacterAnimDriver topples the corpse procedurally.</summary>
        static AnimatorController BuildZombieController()
        {
            var ctrl = AnimatorControllerUtil.Reset(ZombieControllerPath);
            var sm = ctrl.layers[0].stateMachine;
            ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
            ctrl.AddParameter(new AnimatorControllerParameter { name = "AnimSpeed", type = AnimatorControllerParameterType.Float, defaultFloat = 1f });
            var loco = AnimatorControllerUtil.AddLocomotion(ctrl, sm, (LoadClip(ZombieAnimPath, "idle"), 0f), (LoadClip(ZombieAnimPath, "walk"), 1f));
            AnimatorControllerUtil.AddOneShot(ctrl, sm, loco, "Attack", LoadClip(ZombieAnimPath, "scratch"));
            AnimatorControllerUtil.AddOneShot(ctrl, sm, loco, "Hit", LoadClip(ZombieAnimPath, "hit"));
            EditorUtility.SetDirty(ctrl);
            return ctrl;
        }

        // ---------- Visual swap ----------
        static void ApplyVisual(GameObject root, Material mat, RuntimeAnimatorController ctrl, float heightMul, float refSpeed, AnimationClip locomotionClip)
        {
            var old = root.transform.Find(VisualName);
            if (old != null) Object.DestroyImmediate(old.gameObject);

            // The capsule mesh goes; colliders, CharacterController and gameplay scripts stay.
            var mr = root.GetComponent<MeshRenderer>(); if (mr != null) Object.DestroyImmediate(mr, true);
            var mf = root.GetComponent<MeshFilter>(); if (mf != null) Object.DestroyImmediate(mf, true);

            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            var vis = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);
            vis.name = VisualName;

            var smr = vis.GetComponentInChildren<SkinnedMeshRenderer>();
            smr.sharedMaterial = mat;
            smr.quality = SkinQuality.Bone2;          // mobile skinning budget
            smr.updateWhenOffscreen = false;
            smr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;

            // Fit to the collider height in world metres (cancels non-uniform root scale), feet on the floor.
            VisualFit.ColliderBottomWorldY(root, out float colliderHeight);
            VisualFit.Fit(root, vis, VisualFit.Mode.Height, colliderHeight * 0.97f * heightMul, ModelYaw);

            var anim = vis.GetComponent<Animator>() ?? vis.AddComponent<Animator>();
            anim.runtimeAnimatorController = ctrl;
            anim.avatar = avatar;
            anim.applyRootMotion = false;
            anim.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

            var driver = vis.AddComponent<CharacterAnimDriver>();
            var feet = new[] { anim.GetBoneTransform(HumanBodyBones.LeftFoot), anim.GetBoneTransform(HumanBodyBones.RightFoot) };
            float measured = isHuman && feet[0] != null ? VisualFit.MeasureGroundSpeed(vis, locomotionClip, feet) : 0f;
            driver.Configure(measured > 0f ? measured : refSpeed);
            Debug.Log($"[CharacterArt] {root.name}: run cycle ground speed {measured:0.00} m/s (fallback {refSpeed})");
            EditorUtility.SetDirty(driver);
        }

        static int ApplyToScenePlayer(RuntimeAnimatorController ctrl)
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            int n = 0;
            foreach (var pc in Object.FindObjectsByType<Deadzone.Player.PlayerController>(FindObjectsInactive.Include))
            {
                ApplyVisual(pc.gameObject, GetMaterial("Operator"), ctrl, 1f, 3f, LoadClip(RunPath));
                n++;
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return n;
        }

        // ---------- Verification capture ----------
        [MenuItem("Deadzone/Art/Capture Character Lineup")]
        public static void CaptureLineup()
        {
            ConfigureImporters();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var run = LoadClip(RunPath);
            var idle = LoadClip(IdlePath);
            string[] skins = { "Operator", "Squadmate", "Rival", "Walker", "Runner", "Spitter", "Brute" };

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.localScale = Vector3.one * 3f;
            var gm = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            gm.SetColor("_BaseColor", Palette.Floor);
            ground.GetComponent<Renderer>().sharedMaterial = gm;

            var sun = new GameObject("Key").AddComponent<Light>();
            sun.type = LightType.Directional; sun.color = Palette.MoonKey; sun.intensity = 1.3f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = Palette.Shadow * 2.2f;

            var ctrl = BuildController();
            float[] heightMul = { 1f, 1f, 1f, 1f, 1f, 1f, 1.35f };
            for (int i = 0; i < skins.Length; i++)
            {
                // Same structure as the gameplay prefabs: capsule-sized root at y=1, facing +Z.
                var root = new GameObject(skins[i]);
                root.AddComponent<CapsuleCollider>();
                root.transform.position = new Vector3((i - 3) * 1.7f, 1f, 0f);
                ApplyVisual(root, GetMaterial(skins[i]), ctrl, heightMul[i], 3f, run);
                // Bind pose on purpose: humanoid SampleAnimation can override body orientation,
                // which would hide a facing bug. The arrow marks the gameplay root's +Z (aim direction).
                var arrow = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Object.DestroyImmediate(arrow.GetComponent<Collider>());
                arrow.transform.SetParent(root.transform, false);
                arrow.transform.localPosition = new Vector3(0f, -0.95f, 0.6f);
                arrow.transform.localScale = new Vector3(0.12f, 0.05f, 0.6f);
                var am = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                am.SetColor("_BaseColor", Palette.Objective);
                arrow.GetComponent<Renderer>().sharedMaterial = am;
            }

            sun.transform.rotation = Quaternion.Euler(40f, 200f, 0f); // light from the +Z side for the front shot
            Render("warmup", new Vector3(0f, 1.3f, 9f), Quaternion.Euler(4f, 180f, 0f), 42f);
            Render("lineup_front", new Vector3(0f, 1.3f, 9f), Quaternion.Euler(4f, 180f, 0f), 42f); // camera on +Z: should see faces
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            Render("lineup_gamecam", new Vector3(0f, 11.5f, -8f), Quaternion.Euler(55f, 0f, 0f), 50f);
        }

        [MenuItem("Deadzone/Art/Capture Gameplay Scene")]
        public static void CaptureScene()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var cam = UnityEngine.Camera.main ?? Object.FindAnyObjectByType<UnityEngine.Camera>();
            var player = Object.FindAnyObjectByType<Deadzone.Player.PlayerController>();
            // Drop a few infected around the player so the frame shows the cast together.
            string[] enemies = { "Zombie_Prefab", "Runner_Prefab", "Spitter_Prefab", "Tank_Prefab", "Zombie_Prefab" };
            for (int i = 0; i < enemies.Length; i++)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{Prefabs}/{enemies[i]}.prefab");
                float ang = (i * 55f - 110f) * Mathf.Deg2Rad;
                var pos = player.transform.position + new Vector3(Mathf.Sin(ang), 0f, Mathf.Cos(ang)) * (3.5f + i * 0.6f);
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                go.transform.position = new Vector3(pos.x, prefab.transform.position.y, pos.z);
                go.transform.LookAt(new Vector3(player.transform.position.x, go.transform.position.y, player.transform.position.z));
            }
            foreach (var a in Object.FindObjectsByType<Animator>(FindObjectsInactive.Exclude))
            {
                a.Rebind();
                a.SetFloat("Speed", 1f);
                a.Update(0.2f + Random.value * 0.3f);
            }
            // Gameplay framing: 55 degree pitch, ~14 m back, aimed at the player.
            var target = player.transform.position;
            var rot = Quaternion.Euler(55f, 0f, 0f);
            Render("scene_gamecam", target - rot * Vector3.forward * 14f, rot, 50f);
            Render("scene_gamecam", target - rot * Vector3.forward * 14f, rot, 50f);
            Render("scene_close", target + new Vector3(0f, 3.2f, -6f), Quaternion.Euler(22f, 0f, 0f), 50f);
            Render("scene_overview", new Vector3(0f, 75f, -62f), Quaternion.Euler(50f, 0f, 0f), 60f);
            Render("scene_corner", new Vector3(-30f, 16f, -46f), Quaternion.Euler(50f, 20f, 0f), 55f);
            var shipPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Environment/Dropship.prefab");
            if (shipPrefab != null)
            {
                var ship = (GameObject)PrefabUtility.InstantiatePrefab(shipPrefab);
                ship.transform.SetPositionAndRotation(new Vector3(0f, 0f, 0f), Quaternion.Euler(0f, 30f, 0f));
                player.gameObject.SetActive(false);
                Render("scene_helipad", new Vector3(13f, 10f, -15f), Quaternion.Euler(30f, -40f, 0f), 50f);
            }
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single); // discard the temp enemies
        }

        static void Render(string name, Vector3 pos, Quaternion rot, float fov)
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
            Object.DestroyImmediate(cam.gameObject);
            Debug.Log($"[CharacterArt] Captured Captures/{name}.png");
        }
    }
}
