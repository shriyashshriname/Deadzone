#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Deadzone.Core;
using Deadzone.Enemies;
using Deadzone.Combat;
using Deadzone.Player;
using Deadzone.Data;
using Deadzone.UI;

namespace Deadzone.Editor
{
    public static class WaveHordeSetupHelper
    {
        [MenuItem("Deadzone/Setup Wave Horde Prefabs and Scene")]
        public static void SetupAllPrefabsAndScene()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Materials")) AssetDatabase.CreateFolder("Assets/_Project", "Materials");
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Prefabs")) AssetDatabase.CreateFolder("Assets/_Project", "Prefabs");

            Shader solidShader = Shader.Find("Standard") ?? Shader.Find("Mobile/Diffuse") ?? Shader.Find("Unlit/Color");

            // 1. Materials
            Material matRunner = GetOrCreateMat("Assets/_Project/Materials/Mat_Runner.mat", solidShader, new Color(1.0f, 0.35f, 0.05f));
            Material matSpitter = GetOrCreateMat("Assets/_Project/Materials/Mat_Spitter.mat", solidShader, new Color(0.65f, 1.0f, 0.15f));
            Material matTank = GetOrCreateMat("Assets/_Project/Materials/Mat_Tank.mat", solidShader, new Color(0.22f, 0.24f, 0.28f));
            Material matBossBrute = GetOrCreateMat("Assets/_Project/Materials/Mat_BossBrute.mat", solidShader, new Color(0.88f, 0.12f, 0.12f));
            Material matCrawlerKing = GetOrCreateMat("Assets/_Project/Materials/Mat_CrawlerKing.mat", solidShader, new Color(0.65f, 0.15f, 0.90f));
            Material matAcid = GetOrCreateMat("Assets/_Project/Materials/Mat_Acid.mat", solidShader, new Color(0.3f, 1.0f, 0.1f));
            Material matBarrel = GetOrCreateMat("Assets/_Project/Materials/Mat_Barrel.mat", solidShader, new Color(0.95f, 0.25f, 0.05f));
            Material matTurret = GetOrCreateMat("Assets/_Project/Materials/Mat_Turret.mat", solidShader, new Color(0.15f, 0.75f, 0.95f));
            Material matGrenade = GetOrCreateMat("Assets/_Project/Materials/Mat_Grenade.mat", solidShader, new Color(0.9f, 0.55f, 0.1f));
            Material matRival = GetOrCreateMat("Assets/_Project/Materials/Mat_Rival.mat", solidShader, new Color(0.85f, 0.18f, 0.25f));
            Material matAirdrop = GetOrCreateMat("Assets/_Project/Materials/Mat_Airdrop.mat", solidShader, new Color(1.0f, 0.85f, 0.1f));
            Material matBoxA = GetOrCreateMat("Assets/_Project/Materials/Mat_Cargo_Blue.mat", solidShader, new Color(0.18f, 0.35f, 0.65f));
            Material matBoxB = GetOrCreateMat("Assets/_Project/Materials/Mat_Cargo_Orange.mat", solidShader, new Color(0.85f, 0.45f, 0.10f));

            Material matHpPickup = GetOrCreateMat("Assets/_Project/Materials/Mat_Pickup_Health.mat", solidShader, new Color(0.1f, 1.0f, 0.35f));
            Material matAmmoPickup = GetOrCreateMat("Assets/_Project/Materials/Mat_Pickup_Ammo.mat", solidShader, new Color(1.0f, 0.85f, 0.1f));
            Material matGemPickup = GetOrCreateMat("Assets/_Project/Materials/Mat_Pickup_Gem.mat", solidShader, new Color(0.1f, 0.9f, 1.0f));
            Material matWall = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Mat_Wall.mat");

            AssetDatabase.SaveAssets();

            // 2. Projectiles & Abilities Prefabs
            GameObject bulletPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Bullet_Prefab.prefab");

            GameObject acidGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            acidGo.name = "Acid_Projectile_Prefab"; acidGo.transform.localScale = new Vector3(0.45f, 0.45f, 0.45f);
            acidGo.GetComponent<Renderer>().sharedMaterial = matAcid; acidGo.GetComponent<SphereCollider>().isTrigger = true;
            acidGo.AddComponent<AcidProjectile>();
            GameObject acidPrefab = PrefabUtility.SaveAsPrefabAsset(acidGo, "Assets/_Project/Prefabs/Acid_Projectile_Prefab.prefab");
            Object.DestroyImmediate(acidGo);

            GameObject grenGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            grenGo.name = "Grenade_Prefab"; grenGo.transform.localScale = new Vector3(0.45f, 0.45f, 0.45f);
            grenGo.GetComponent<Renderer>().sharedMaterial = matGrenade; grenGo.AddComponent<Grenade>();
            var grenRb = grenGo.AddComponent<Rigidbody>(); grenRb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            GameObject grenadePrefab = PrefabUtility.SaveAsPrefabAsset(grenGo, "Assets/_Project/Prefabs/Grenade_Prefab.prefab");
            Object.DestroyImmediate(grenGo);

            GameObject turretGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            turretGo.name = "Turret_Prefab"; turretGo.transform.localScale = new Vector3(0.9f, 0.6f, 0.9f);
            turretGo.GetComponent<Renderer>().sharedMaterial = matTurret;
            var turretComp = turretGo.AddComponent<DeployableTurret>();
            SerializedObject turretSo = new SerializedObject(turretComp);
            if (bulletPrefab != null) turretSo.FindProperty("projectilePrefab").objectReferenceValue = bulletPrefab;
            turretSo.ApplyModifiedProperties();
            GameObject turretPrefab = PrefabUtility.SaveAsPrefabAsset(turretGo, "Assets/_Project/Prefabs/Turret_Prefab.prefab");
            Object.DestroyImmediate(turretGo);

            // 3. Enemy Prefabs with HitFlash & TeamMember
            GameObject runnerGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            runnerGo.name = "Runner_Prefab"; runnerGo.tag = "Enemy"; runnerGo.transform.localScale = new Vector3(0.85f, 0.85f, 0.85f);
            runnerGo.GetComponent<Renderer>().sharedMaterial = matRunner; runnerGo.AddComponent<Health>().Initialize(45f);
            runnerGo.AddComponent<RunnerEnemy>(); runnerGo.AddComponent<HitFlash>(); runnerGo.AddComponent<TeamMember>().TeamId = 0;
            GameObject runnerPrefab = PrefabUtility.SaveAsPrefabAsset(runnerGo, "Assets/_Project/Prefabs/Runner_Prefab.prefab");
            Object.DestroyImmediate(runnerGo);

            GameObject spitterGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            spitterGo.name = "Spitter_Prefab"; spitterGo.tag = "Enemy"; spitterGo.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);
            spitterGo.GetComponent<Renderer>().sharedMaterial = matSpitter; spitterGo.AddComponent<Health>().Initialize(50f);
            var spitterEnemy = spitterGo.AddComponent<SpitterEnemy>(); spitterGo.AddComponent<HitFlash>(); spitterGo.AddComponent<TeamMember>().TeamId = 0;
            new SerializedObject(spitterEnemy) { }.FindProperty("acidPrefab").objectReferenceValue = acidPrefab;
            GameObject spitterPrefab = PrefabUtility.SaveAsPrefabAsset(spitterGo, "Assets/_Project/Prefabs/Spitter_Prefab.prefab");
            Object.DestroyImmediate(spitterGo);

            GameObject tankGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            tankGo.name = "Tank_Prefab"; tankGo.tag = "Enemy"; tankGo.transform.localScale = new Vector3(1.4f, 1.3f, 1.4f);
            tankGo.GetComponent<Renderer>().sharedMaterial = matTank; tankGo.AddComponent<Health>().Initialize(180f);
            tankGo.AddComponent<TankEnemy>(); tankGo.AddComponent<HitFlash>(); tankGo.AddComponent<TeamMember>().TeamId = 0;
            GameObject tankPrefab = PrefabUtility.SaveAsPrefabAsset(tankGo, "Assets/_Project/Prefabs/Tank_Prefab.prefab");
            Object.DestroyImmediate(tankGo);

            GameObject bruteGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            bruteGo.name = "Boss_Brute_Prefab"; bruteGo.tag = "Enemy"; bruteGo.transform.localScale = new Vector3(2.2f, 2.2f, 2.2f);
            bruteGo.GetComponent<Renderer>().sharedMaterial = matBossBrute; bruteGo.AddComponent<Health>().Initialize(500f);
            bruteGo.AddComponent<BossBrute>(); bruteGo.AddComponent<BossStompAttack>(); bruteGo.AddComponent<HitFlash>(); bruteGo.AddComponent<TeamMember>().TeamId = 0;
            GameObject brutePrefab = PrefabUtility.SaveAsPrefabAsset(bruteGo, "Assets/_Project/Prefabs/Boss_Brute_Prefab.prefab");
            Object.DestroyImmediate(bruteGo);

            GameObject crawlerGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            crawlerGo.name = "Crawler_King_Prefab"; crawlerGo.tag = "Enemy"; crawlerGo.transform.localScale = new Vector3(1.8f, 1.1f, 1.8f);
            crawlerGo.GetComponent<Renderer>().sharedMaterial = matCrawlerKing; crawlerGo.AddComponent<Health>().Initialize(450f);
            var crawlerBoss = crawlerGo.AddComponent<CrawlerKingBoss>(); crawlerGo.AddComponent<HitFlash>(); crawlerGo.AddComponent<TeamMember>().TeamId = 0;
            new SerializedObject(crawlerBoss) { }.FindProperty("acidProjectilePrefab").objectReferenceValue = acidPrefab;
            GameObject crawlerPrefab = PrefabUtility.SaveAsPrefabAsset(crawlerGo, "Assets/_Project/Prefabs/Crawler_King_Prefab.prefab");
            Object.DestroyImmediate(crawlerGo);

            // 4. Loot Pickups
            GameObject healthPickup = CreatePickup("Pickup_Health_Prefab", PrimitiveType.Cube, new Vector3(0.45f, 0.45f, 0.45f), matHpPickup, PickupType.Health, 35f);
            GameObject ammoPickup = CreatePickup("Pickup_Ammo_Prefab", PrimitiveType.Cube, new Vector3(0.5f, 0.35f, 0.5f), matAmmoPickup, PickupType.Ammo, 1f);
            GameObject gemPickup = CreatePickup("Pickup_Gem_Prefab", PrimitiveType.Sphere, new Vector3(0.4f, 0.45f, 0.4f), matGemPickup, PickupType.Gems, 10f);

            // 5. Supply Airdrop Crate Prefab
            GameObject airdropGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            airdropGo.name = "Supply_Airdrop_Prefab"; airdropGo.transform.localScale = new Vector3(1.6f, 1.4f, 1.6f);
            airdropGo.GetComponent<Renderer>().sharedMaterial = matAirdrop;
            var airdropComp = airdropGo.AddComponent<SupplyAirdrop>();
            SerializedObject airdropSo = new SerializedObject(airdropComp);
            airdropSo.FindProperty("gemPickupPrefab").objectReferenceValue = gemPickup;
            airdropSo.FindProperty("healthPickupPrefab").objectReferenceValue = healthPickup;
            airdropSo.FindProperty("ammoPickupPrefab").objectReferenceValue = ammoPickup;
            airdropSo.ApplyModifiedProperties();
            GameObject airdropPrefab = PrefabUtility.SaveAsPrefabAsset(airdropGo, "Assets/_Project/Prefabs/Supply_Airdrop_Prefab.prefab");
            Object.DestroyImmediate(airdropGo);

            // 6. Rival Operator Prefab
            GameObject rivalGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            rivalGo.name = "Rival_Operator_Prefab"; rivalGo.tag = "Enemy"; rivalGo.transform.localScale = Vector3.one;
            rivalGo.GetComponent<Renderer>().sharedMaterial = matRival;
            rivalGo.AddComponent<Health>().Initialize(120f);
            rivalGo.AddComponent<TeamMember>().TeamId = 2;
            rivalGo.AddComponent<HitFlash>();
            var rivalComp = rivalGo.AddComponent<RivalOperator>();
            SerializedObject rivalSo = new SerializedObject(rivalComp);
            if (bulletPrefab != null) rivalSo.FindProperty("projectilePrefab").objectReferenceValue = bulletPrefab;
            rivalSo.FindProperty("deathLootPrefab").objectReferenceValue = gemPickup;
            rivalSo.ApplyModifiedProperties();
            GameObject rivalPrefab = PrefabUtility.SaveAsPrefabAsset(rivalGo, "Assets/_Project/Prefabs/Rival_Operator_Prefab.prefab");
            Object.DestroyImmediate(rivalGo);

            // 6B. Friendly Duo Squadmate Prefab
            Material matDuo = GetOrCreateMat("Assets/_Project/Materials/Mat_DuoSquadmate.mat", solidShader, new Color(0.15f, 0.85f, 1.0f));
            GameObject duoGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            duoGo.name = "Friendly_Squadmate_Prefab"; duoGo.tag = "Player"; duoGo.transform.localScale = Vector3.one;
            duoGo.GetComponent<Renderer>().sharedMaterial = matDuo;
            duoGo.AddComponent<Health>().Initialize(150f);
            duoGo.AddComponent<TeamMember>().TeamId = 1;
            duoGo.AddComponent<HitFlash>();
            var duoComp = duoGo.AddComponent<FriendlySquadmate>();
            SerializedObject duoSo = new SerializedObject(duoComp);
            if (bulletPrefab != null) duoSo.FindProperty("projectilePrefab").objectReferenceValue = bulletPrefab;
            duoSo.ApplyModifiedProperties();
            GameObject duoPrefab = PrefabUtility.SaveAsPrefabAsset(duoGo, "Assets/_Project/Prefabs/Friendly_Squadmate_Prefab.prefab");
            Object.DestroyImmediate(duoGo);

            // 6C. Power Substation Prefab
            Material matStation = GetOrCreateMat("Assets/_Project/Materials/Mat_Substation.mat", solidShader, new Color(0.95f, 0.75f, 0.15f));
            GameObject stationGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stationGo.name = "Substation_Prefab"; stationGo.transform.localScale = new Vector3(1.6f, 2.2f, 1.4f);
            stationGo.GetComponent<Renderer>().sharedMaterial = matStation;
            stationGo.AddComponent<PowerSubstation>();
            GameObject stationPrefab = PrefabUtility.SaveAsPrefabAsset(stationGo, "Assets/_Project/Prefabs/Substation_Prefab.prefab");
            Object.DestroyImmediate(stationGo);

            AssetDatabase.SaveAssets();

            // 7. Open Scene & Wire Infrastructure
            var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Graybox_Sandbox.unity");
            GameObject zombiePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Zombie_Prefab.prefab");
            if (zombiePrefab != null)
            {
                if (zombiePrefab.GetComponent<HitFlash>() == null) zombiePrefab.AddComponent<HitFlash>();
                if (zombiePrefab.GetComponent<TeamMember>() == null) zombiePrefab.AddComponent<TeamMember>().TeamId = 0;
                EditorUtility.SetDirty(zombiePrefab);
            }

            // Wire WaveManager with Airdrop and Rival Bots
            GameObject waveObj = GameObject.Find("WaveManager") ?? new GameObject("WaveManager");
            WaveManager wm = waveObj.GetComponent<WaveManager>() ?? waveObj.AddComponent<WaveManager>();
            SerializedObject wmSo = new SerializedObject(wm);
            wmSo.FindProperty("walkerPrefab").objectReferenceValue = zombiePrefab;
            wmSo.FindProperty("runnerPrefab").objectReferenceValue = runnerPrefab;
            wmSo.FindProperty("spitterPrefab").objectReferenceValue = spitterPrefab;
            wmSo.FindProperty("tankPrefab").objectReferenceValue = tankPrefab;
            wmSo.FindProperty("bossBrutePrefab").objectReferenceValue = brutePrefab;
            wmSo.FindProperty("bossCrawlerPrefab").objectReferenceValue = crawlerPrefab;
            wmSo.FindProperty("airdropPrefab").objectReferenceValue = airdropPrefab;
            wmSo.FindProperty("rivalOperatorPrefab").objectReferenceValue = rivalPrefab;
            wmSo.FindProperty("healthPickupPrefab").objectReferenceValue = healthPickup;
            wmSo.FindProperty("ammoPickupPrefab").objectReferenceValue = ammoPickup;
            wmSo.FindProperty("gemPickupPrefab").objectReferenceValue = gemPickup;
            wmSo.FindProperty("spawnRadius").floatValue = 26f;
            wmSo.FindProperty("waves").ClearArray();
            wmSo.ApplyModifiedProperties();

            // Wire KillFeedUI
            if (GameObject.Find("KillFeedUI") == null)
            {
                new GameObject("KillFeedUI").AddComponent<KillFeedUI>();
            }

            // Wire Player Loadout & Team
            GameObject player = GameObject.FindWithTag("Player") ?? GameObject.Find("Player");
            if (player != null && player.TryGetComponent<PlayerController>(out var pc))
            {
                var ar = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/_Project/Scripts/Data/WeaponData_AssaultRifle.asset");
                var sg = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/_Project/Scripts/Data/WeaponData_Shotgun.asset");
                var smg = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/_Project/Scripts/Data/WeaponData_SMG.asset");
                var snp = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/_Project/Scripts/Data/WeaponData_Sniper.asset");

                SerializedObject pcSo = new SerializedObject(pc);
                if (ar != null) pcSo.FindProperty("assaultRifleData").objectReferenceValue = ar;
                if (sg != null) pcSo.FindProperty("shotgunData").objectReferenceValue = sg;
                if (smg != null) pcSo.FindProperty("smgData").objectReferenceValue = smg;
                if (snp != null) pcSo.FindProperty("sniperData").objectReferenceValue = snp;
                pcSo.ApplyModifiedProperties();

                if (player.GetComponent<TeamMember>() == null) player.AddComponent<TeamMember>().TeamId = 1;
                if (player.GetComponent<PlayerPerks>() == null) player.AddComponent<PlayerPerks>();
                if (player.GetComponent<PlayerRevive>() == null) player.AddComponent<PlayerRevive>();

                PlayerAbilities pa = player.GetComponent<PlayerAbilities>() ?? player.AddComponent<PlayerAbilities>();
                SerializedObject paSo = new SerializedObject(pa);
                paSo.FindProperty("grenadePrefab").objectReferenceValue = grenadePrefab;
                paSo.FindProperty("turretPrefab").objectReferenceValue = turretPrefab;
                paSo.ApplyModifiedProperties();
            }

            // Spawn Friendly Duo Squadmate in Scene
            GameObject existingDuo = GameObject.Find("Duo_Squadmate");
            if (existingDuo == null && duoPrefab != null)
            {
                GameObject spawnedDuo = (GameObject)PrefabUtility.InstantiatePrefab(duoPrefab);
                spawnedDuo.name = "Duo_Squadmate";
                spawnedDuo.transform.position = new Vector3(-3f, 0.5f, 0f);
            }

            // 8. Mega Arena Geometry (120m x 120m = 14,400 sq meters)
            GameObject floor = GameObject.Find("Floor") ?? GameObject.Find("Ground");
            if (floor != null) floor.transform.localScale = new Vector3(12f, 1f, 12f);

            BuildWall("Wall_North", new Vector3(0, 3f, 60.5f), new Vector3(124f, 6f, 2f), matWall);
            BuildWall("Wall_South", new Vector3(0, 3f, -60.5f), new Vector3(124f, 6f, 2f), matWall);
            BuildWall("Wall_East", new Vector3(60.5f, 3f, 0), new Vector3(2f, 6f, 124f), matWall);
            BuildWall("Wall_West", new Vector3(-60.5f, 3f, 0), new Vector3(2f, 6f, 124f), matWall);

            // Sector A: Cargo Depot (Shipping Containers)
            BuildWall("Container_NW1", new Vector3(-35f, 2f, 35f), new Vector3(8f, 3.8f, 3.5f), matBoxA);
            BuildWall("Container_NW2", new Vector3(-25f, 2f, 42f), new Vector3(3.5f, 3.8f, 8f), matBoxB);
            BuildWall("Container_NW3", new Vector3(-42f, 2f, 22f), new Vector3(3.5f, 3.8f, 8f), matBoxA);

            // Sector B: Power Station
            BuildWall("Pillar_NE1", new Vector3(32f, 3f, 32f), new Vector3(4f, 6f, 4f), matWall);
            BuildWall("Substation_NE", new Vector3(38f, 2f, 24f), new Vector3(12f, 3.5f, 1.5f), matWall);
            BuildHazardBarrel("Barrel_NE1", new Vector3(28f, 0.75f, 28f), matBarrel);
            BuildHazardBarrel("Barrel_NE2", new Vector3(36f, 0.75f, 38f), matBarrel);

            // Sector C: Hazard Containment
            BuildWall("Pillar_SW1", new Vector3(-32f, 3f, -32f), new Vector3(4f, 6f, 4f), matWall);
            BuildWall("Barricade_SW", new Vector3(-38f, 1.5f, -22f), new Vector3(12f, 2.8f, 1.5f), matWall);
            BuildHazardBarrel("Barrel_SW1", new Vector3(-28f, 0.75f, -28f), matBarrel);
            BuildHazardBarrel("Barrel_SW2", new Vector3(-40f, 0.75f, -38f), matBarrel);

            // Sector D: Logistics & Hangars
            BuildWall("Container_SE1", new Vector3(35f, 2f, -35f), new Vector3(8f, 3.8f, 3.5f), matBoxB);
            BuildWall("Container_SE2", new Vector3(25f, 2f, -42f), new Vector3(3.5f, 3.8f, 8f), matBoxA);
            BuildWall("Pillar_SE1", new Vector3(42f, 3f, -24f), new Vector3(4f, 6f, 4f), matWall);

            // Center Plaza
            BuildWall("Pillar_Center_NW", new Vector3(-16f, 2.5f, 16f), new Vector3(3.5f, 5f, 3.5f), matWall);
            BuildWall("Pillar_Center_NE", new Vector3(16f, 2.5f, 16f), new Vector3(3.5f, 5f, 3.5f), matWall);
            BuildWall("Pillar_Center_SW", new Vector3(-16f, 2.5f, -16f), new Vector3(3.5f, 5f, 3.5f), matWall);
            BuildWall("Pillar_Center_SE", new Vector3(16f, 2.5f, -16f), new Vector3(3.5f, 5f, 3.5f), matWall);

            // Outpost Stash Manager
            if (GameObject.Find("OutpostStashManager") == null)
            {
                new GameObject("OutpostStashManager").AddComponent<OutpostStashManager>();
            }

            // Power Substations
            BuildSubstation("Substation_Alpha", "Substation Alpha", "Sector B (Power Yard)", new Vector3(36f, 1.1f, 30f), matStation);
            BuildSubstation("Substation_Beta", "Substation Beta", "Sector C (Quarantine Core)", new Vector3(-36f, 1.1f, -30f), matStation);

            // Center Extraction Helipad
            GameObject ez = GameObject.Find("ExtractionZone") ?? GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ez.name = "ExtractionZone"; ez.transform.position = new Vector3(0, 0.05f, 0); ez.transform.localScale = new Vector3(10f, 0.05f, 10f);
            Material matEz = GetOrCreateMat("Assets/_Project/Materials/Mat_ExtractionZone.mat", solidShader, new Color(0.2f, 0.85f, 1.0f));
            if (ez.TryGetComponent<Renderer>(out var ezRend)) ezRend.sharedMaterial = matEz;
            if (ez.TryGetComponent<CapsuleCollider>(out var ezCol)) ezCol.isTrigger = true;
            if (ez.GetComponent<ExtractionZone>() == null) ez.AddComponent<ExtractionZone>();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[EXTRACTION CAMPAIGN COMPLETE] 120m Arena, Power Substations Alpha/Beta, Evac Helipad & Outpost Stash Active!");
        }

        private static void BuildSubstation(string name, string stationTitle, string sectorTitle, Vector3 pos, Material mat)
        {
            GameObject station = GameObject.Find(name) ?? GameObject.CreatePrimitive(PrimitiveType.Cube);
            station.name = name; station.transform.position = pos; station.transform.localScale = new Vector3(1.8f, 2.2f, 1.4f);
            if (station.TryGetComponent<Renderer>(out var rend)) rend.sharedMaterial = mat;
            PowerSubstation ps = station.GetComponent<PowerSubstation>() ?? station.AddComponent<PowerSubstation>();
            ps.stationName = stationTitle;
            ps.sectorName = sectorTitle;
        }

        private static void BuildHazardBarrel(string name, Vector3 pos, Material mat)
        {
            GameObject barrel = GameObject.Find(name) ?? GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            barrel.name = name; barrel.transform.position = pos; barrel.transform.localScale = new Vector3(0.9f, 0.75f, 0.9f);
            if (barrel.TryGetComponent<Renderer>(out var rend)) rend.sharedMaterial = mat;
            if (barrel.GetComponent<Health>() == null) barrel.AddComponent<Health>().Initialize(30f);
            if (barrel.GetComponent<ExplosiveBarrel>() == null) barrel.AddComponent<ExplosiveBarrel>();
        }

        private static GameObject CreatePickup(string name, PrimitiveType prim, Vector3 scale, Material mat, PickupType type, float amt)
        {
            GameObject go = GameObject.CreatePrimitive(prim);
            go.name = name; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            go.GetComponent<Collider>().isTrigger = true;
            var loot = go.AddComponent<LootDrop>();
            SerializedObject so = new SerializedObject(loot);
            so.FindProperty("pickupType").enumValueIndex = (int)type;
            so.FindProperty("amount").floatValue = amt;
            so.ApplyModifiedProperties();
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, $"Assets/_Project/Prefabs/{name}.prefab");
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static void BuildWall(string name, Vector3 pos, Vector3 scale, Material mat)
        {
            GameObject wall = GameObject.Find(name) ?? GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name; wall.transform.position = pos; wall.transform.localScale = scale;
            if (wall.TryGetComponent<Renderer>(out var rend)) rend.sharedMaterial = mat;
        }

        private static Material GetOrCreateMat(string path, Shader shader, Color color)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { mat = new Material(shader) { color = color }; AssetDatabase.CreateAsset(mat, path); }
            else { mat.shader = shader; mat.color = color; EditorUtility.SetDirty(mat); }
            return mat;
        }
    }
}
#endif
