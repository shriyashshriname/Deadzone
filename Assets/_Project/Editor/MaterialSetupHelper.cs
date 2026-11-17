#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Deadzone.Player;
using Deadzone.Combat;
using Deadzone.Data;

namespace Deadzone.Editor
{
    public static class MaterialSetupHelper
    {
        [MenuItem("Deadzone/Art/Setup Materials and Juiced Scene")]
        public static void SetupMaterialsAndScene()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Materials"))
            {
                AssetDatabase.CreateFolder("Assets/_Project", "Materials");
            }

            // Built-in Render Pipeline Shaders: Standard / Mobile / Unlit
            Shader solidShader = Shader.Find("Standard") 
                              ?? Shader.Find("Mobile/Diffuse") 
                              ?? Shader.Find("Unlit/Color");

            // 1. Player Material (Vibrant Tactical Cyan Glow)
            Material matPlayer = new Material(solidShader) { color = new Color(0.0f, 0.85f, 1.0f), name = "Mat_Player" };
            AssetDatabase.CreateAsset(matPlayer, "Assets/_Project/Materials/Mat_Player.mat");

            // 2. Zombie Walker Material (Toxic Decaying Moss Green)
            Material matZombie = new Material(solidShader) { color = new Color(0.20f, 0.72f, 0.22f), name = "Mat_Zombie" };
            AssetDatabase.CreateAsset(matZombie, "Assets/_Project/Materials/Mat_Zombie.mat");

            // 3. Floor Material (Dark Industrial Concrete Grid)
            Material matFloor = new Material(solidShader) { color = new Color(0.14f, 0.15f, 0.18f), name = "Mat_Floor" };
            AssetDatabase.CreateAsset(matFloor, "Assets/_Project/Materials/Mat_Floor.mat");

            // 4. Wall Material (Gunmetal Slate)
            Material matWall = new Material(solidShader) { color = new Color(0.30f, 0.32f, 0.38f), name = "Mat_Wall" };
            AssetDatabase.CreateAsset(matWall, "Assets/_Project/Materials/Mat_Wall.mat");

            // 5. Bullet Tracer Material (Blazing Gold Amber)
            Material matBullet = new Material(solidShader) { color = new Color(1.0f, 0.85f, 0.1f), name = "Mat_Bullet" };
            AssetDatabase.CreateAsset(matBullet, "Assets/_Project/Materials/Mat_Bullet.mat");

            // 6. Boss Material (Blood Crimson)
            Material matBoss = new Material(solidShader) { color = new Color(0.88f, 0.12f, 0.12f), name = "Mat_Boss" };
            AssetDatabase.CreateAsset(matBoss, "Assets/_Project/Materials/Mat_Boss.mat");

            AssetDatabase.SaveAssets();

            // Open Scene
            var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Graybox_Sandbox.unity");

            // Fix Camera to Solid Dark Charcoal Color (Never Pink Skybox)
            UnityEngine.Camera cam = UnityEngine.Camera.main ?? Object.FindAnyObjectByType<UnityEngine.Camera>();
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.10f, 0.11f, 0.14f);
            }

            // Apply Floor
            GameObject floor = GameObject.Find("Floor") ?? GameObject.Find("Ground");
            if (floor != null && floor.TryGetComponent<Renderer>(out var floorRend))
            {
                floorRend.sharedMaterial = matFloor;
            }

            // Apply Player & Wire Components
            GameObject player = GameObject.FindWithTag("Player") ?? GameObject.Find("Player");
            if (player != null)
            {
                var rends = player.GetComponentsInChildren<Renderer>();
                foreach (var r in rends) r.sharedMaterial = matPlayer;

                // Ensure PlayerDash is attached!
                if (player.GetComponent<PlayerDash>() == null)
                {
                    player.AddComponent<PlayerDash>();
                }

                // Ensure Weapon is fully configured
                Weapon weapon = player.GetComponentInChildren<Weapon>();
                if (weapon != null)
                {
                    WeaponData arData = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/_Project/Scripts/Data/WeaponData_AssaultRifle.asset");
                    WeaponData shotgunData = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/_Project/Scripts/Data/WeaponData_Shotgun.asset");
                    WeaponData smgData = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/_Project/Scripts/Data/WeaponData_SMG.asset");
                    GameObject bulletPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Bullet_Prefab.prefab");

                    SerializedObject so = new SerializedObject(weapon);
                    if (arData != null) so.FindProperty("data").objectReferenceValue = arData;
                    if (bulletPrefab != null) so.FindProperty("projectilePrefab").objectReferenceValue = bulletPrefab;
                    so.ApplyModifiedProperties();

                    // Wire to PlayerController (Equipped Weapon + Keys 1, 2, 3 Loadout)
                    PlayerController pc = player.GetComponent<PlayerController>();
                    if (pc != null)
                    {
                        SerializedObject pcSo = new SerializedObject(pc);
                        pcSo.FindProperty("equippedWeapon").objectReferenceValue = weapon;
                        if (arData != null) pcSo.FindProperty("assaultRifleData").objectReferenceValue = arData;
                        if (shotgunData != null) pcSo.FindProperty("shotgunData").objectReferenceValue = shotgunData;
                        if (smgData != null) pcSo.FindProperty("smgData").objectReferenceValue = smgData;
                        pcSo.ApplyModifiedProperties();
                    }
                }
            }

            // Wire GameManager
            if (GameObject.Find("GameManager") == null)
            {
                new GameObject("GameManager").AddComponent<Deadzone.Core.GameManager>();
            }

            // Wire WaveManager with Horde Spawner
            GameObject waveObj = GameObject.Find("WaveManager");
            if (waveObj == null)
            {
                waveObj = new GameObject("WaveManager");
                var wm = waveObj.AddComponent<Deadzone.Core.WaveManager>();
                GameObject zombiePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Zombie_Prefab.prefab");
                if (zombiePrefab != null)
                {
                    SerializedObject wmSo = new SerializedObject(wm);
                    wmSo.FindProperty("walkerPrefab").objectReferenceValue = zombiePrefab;
                    wmSo.ApplyModifiedProperties();
                }
            }

            // Wire In-Game Tactical HUD
            if (GameObject.Find("HUDController") == null)
            {
                new GameObject("HUDController").AddComponent<Deadzone.UI.HUDController>();
            }

            // Wire AudioManager
            if (GameObject.Find("AudioManager") == null)
            {
                new GameObject("AudioManager").AddComponent<Deadzone.Core.AudioManager>();
            }

            // Apply materials directly to Prefabs
            GameObject zPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Zombie_Prefab.prefab");
            if (zPrefab != null && zPrefab.TryGetComponent<Renderer>(out var zRend))
            {
                zRend.sharedMaterial = matZombie;
                EditorUtility.SetDirty(zPrefab);
            }

            GameObject bPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Bullet_Prefab.prefab");
            if (bPrefab != null && bPrefab.TryGetComponent<Renderer>(out var bRend))
            {
                bRend.sharedMaterial = matBullet;
                EditorUtility.SetDirty(bPrefab);
            }

            // Apply Enemies
            GameObject[] zombies = GameObject.FindGameObjectsWithTag("Enemy");
            foreach (var z in zombies)
            {
                var rends = z.GetComponentsInChildren<Renderer>();
                foreach (var r in rends) r.sharedMaterial = matZombie;
            }

            // Setup Lighting & Atmosphere
            Light sun = Object.FindAnyObjectByType<Light>();
            if (sun == null)
            {
                GameObject lightObj = new GameObject("Directional Light");
                sun = lightObj.AddComponent<Light>();
                sun.type = LightType.Directional;
            }
            sun.color = new Color(0.96f, 0.96f, 1.0f);
            sun.intensity = 1.25f;
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            RenderSettings.ambientLight = new Color(0.25f, 0.27f, 0.32f);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[ART PASS COMPLETE] Built-in Standard materials applied, PlayerDash & Weapon wired, Pink screen eliminated!");
        }
    }
}
#endif
