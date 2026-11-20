using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Deadzone.Enemies;
using Deadzone.Player;
using Deadzone.UI;

namespace Deadzone.Core
{
    [Serializable]
    public class WaveConfig
    {
        public int waveNumber;
        public string title = "";
        public int walkerCount = 8, runnerCount = 0, spitterCount = 0, tankCount = 0;
        public bool spawnBossBrute = false, spawnCrawlerBoss = false, spawnAirdrop = false;
        public int rivalOperatorCount = 0;
        public float speedMultiplier = 1.0f, healthMultiplier = 1.0f, spawnInterval = 1.5f;
    }

    public class WaveManager : MonoBehaviour
    {
        public static WaveManager Instance { get; private set; }

        [Header("Enemy & Rival Prefabs")]
        [SerializeField] private GameObject walkerPrefab, runnerPrefab, spitterPrefab, tankPrefab;
        [SerializeField] private GameObject bossBrutePrefab, bossCrawlerPrefab, rivalOperatorPrefab, airdropPrefab;

        [Header("Loot Drop Prefabs")]
        [SerializeField] private GameObject healthPickupPrefab, ammoPickupPrefab, gemPickupPrefab;

        [Header("Spawn Settings")]
        [SerializeField] private float spawnRadius = 26f;
        [SerializeField] private List<WaveConfig> waves = new List<WaveConfig>();
        [Header("Horde Tuning (founder 2026-11-20: many enemies, easy to kill)")]
        [SerializeField] private float hordeMultiplier = 1.8f;   // scales walker counts
        [SerializeField] private float spawnPaceMultiplier = 0.7f; // < 1 = faster spawning

        private int currentWaveIndex = 0, remainingInQueue = 0, activeEnemiesCount = 0;
        private bool isIntermission = false;
        private float intermissionTimer = 0f;
        private string currentWaveTitle = "";

        public event Action<int, string> OnWaveStarted;
        public event Action<int> OnWaveCompleted;

        public int CurrentWaveNumber => currentWaveIndex + 1;
        public int TotalRemainingHostiles => remainingInQueue + activeEnemiesCount;
        public bool IsIntermission => isIntermission;
        public float IntermissionTimer => intermissionTimer;
        public string CurrentWaveTitle => currentWaveTitle;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
            InitializeCampaignWaves();
        }

        private void Start() => StartCoroutine(StartNextWaveRoutine());

        private void InitializeCampaignWaves()
        {
            if (waves.Count > 0) return;
            waves.Add(new WaveConfig { waveNumber = 1, title = "Wave 1: Outbreak Infiltration", walkerCount = 8, speedMultiplier = 1.0f, spawnInterval = 1.8f });
            waves.Add(new WaveConfig { waveNumber = 2, title = "Wave 2: Sprinters Unleashed", walkerCount = 8, runnerCount = 6, speedMultiplier = 1.08f, spawnInterval = 1.5f });
            waves.Add(new WaveConfig { waveNumber = 3, title = "Wave 3: Supply Drop Alert", walkerCount = 10, runnerCount = 6, spitterCount = 4, spawnAirdrop = true, speedMultiplier = 1.15f, spawnInterval = 1.4f });
            waves.Add(new WaveConfig { waveNumber = 4, title = "Wave 4: Armored Vanguard", walkerCount = 12, runnerCount = 8, spitterCount = 4, tankCount = 2, speedMultiplier = 1.22f, spawnInterval = 1.3f });
            waves.Add(new WaveConfig { waveNumber = 5, title = "Wave 5: APEX SHOWDOWN - Subject Zero & Rivals", walkerCount = 8, runnerCount = 6, spawnBossBrute = true, rivalOperatorCount = 2, speedMultiplier = 1.28f, spawnInterval = 1.4f });
            waves.Add(new WaveConfig { waveNumber = 6, title = "Wave 6: Midnight Frenzy & Airdrop", walkerCount = 14, runnerCount = 10, spitterCount = 6, tankCount = 3, spawnAirdrop = true, speedMultiplier = 1.35f, spawnInterval = 1.1f });
            waves.Add(new WaveConfig { waveNumber = 7, title = "Wave 7: EXTRACTION ROYALE - Crawler King & Apex Rivals", runnerCount = 10, spitterCount = 6, tankCount = 2, spawnCrawlerBoss = true, rivalOperatorCount = 3, speedMultiplier = 1.42f, spawnInterval = 1.2f });
        }

        private WaveConfig GetConfigForCurrentWave()
        {
            if (currentWaveIndex < waves.Count) return waves[currentWaveIndex];
            int tier = (currentWaveIndex + 1) - 7;
            bool isBoss = (currentWaveIndex + 1) % 5 == 0;
            return new WaveConfig
            {
                waveNumber = currentWaveIndex + 1,
                title = isBoss ? $"Wave {currentWaveIndex + 1}: SHOWDOWN ROYALE" : $"Wave {currentWaveIndex + 1}: Endless Swarm Tier {tier}",
                walkerCount = 14 + (tier * 3), runnerCount = 8 + (tier * 2), spitterCount = 4 + tier, tankCount = 2 + (tier / 2),
                spawnBossBrute = isBoss && (tier % 2 == 1), spawnCrawlerBoss = isBoss && (tier % 2 == 0),
                spawnAirdrop = (currentWaveIndex + 1) % 2 == 0, rivalOperatorCount = isBoss ? 3 : (tier % 2 == 1 ? 2 : 1),
                speedMultiplier = Mathf.Min(1.65f, 1.40f + (tier * 0.04f)), healthMultiplier = 1.0f + (tier * 0.08f),
                spawnInterval = Mathf.Max(0.7f, 1.2f - (tier * 0.05f))
            };
        }

        private IEnumerator StartNextWaveRoutine()
        {
            isIntermission = true;
            intermissionTimer = 3.5f;
            while (intermissionTimer > 0f) { intermissionTimer -= Time.deltaTime; yield return null; }
            isIntermission = false;

            WaveConfig wave = GetConfigForCurrentWave();
            currentWaveTitle = wave.title;
            OnWaveStarted?.Invoke(wave.waveNumber, wave.title);

            if (wave.spawnAirdrop && airdropPrefab != null)
            {
                Vector3 aPos = new Vector3(UnityEngine.Random.Range(-18f, 18f), 0.5f, UnityEngine.Random.Range(-18f, 18f));
                Instantiate(airdropPrefab, aPos, Quaternion.identity);
                KillFeedUI.PostMessage("★ SUPPLY AIRDROP: High-Value Crate dropped in central sector!", new Color(1f, 0.85f, 0.2f));
            }

            for (int r = 0; r < wave.rivalOperatorCount; r++)
            {
                if (rivalOperatorPrefab != null)
                {
                    Vector3 rPos = new Vector3(UnityEngine.Random.Range(-48f, 48f), 0.5f, UnityEngine.Random.Range(-48f, 48f));
                    var rival = Instantiate(rivalOperatorPrefab, rPos, Quaternion.identity);
                    Grounding.SnapToGround(rival, Grounding.PlayerFloorY(PlayerController.Instance != null ? PlayerController.Instance.transform : null));
                }
            }

            List<GameObject> queue = new List<GameObject>();
            int walkers = Mathf.RoundToInt(wave.walkerCount * hordeMultiplier);
            for (int i = 0; i < walkers; i++) if (walkerPrefab) queue.Add(walkerPrefab);
            for (int i = 0; i < wave.runnerCount; i++) if (runnerPrefab) queue.Add(runnerPrefab);
            for (int i = 0; i < wave.spitterCount; i++) if (spitterPrefab) queue.Add(spitterPrefab);
            for (int i = 0; i < wave.tankCount; i++) if (tankPrefab) queue.Add(tankPrefab);

            for (int i = queue.Count - 1; i > 0; i--) { int r = UnityEngine.Random.Range(0, i + 1); var t = queue[i]; queue[i] = queue[r]; queue[r] = t; }
            if (wave.spawnBossBrute && bossBrutePrefab) queue.Add(bossBrutePrefab);
            if (wave.spawnCrawlerBoss && bossCrawlerPrefab) queue.Add(bossCrawlerPrefab);

            remainingInQueue = queue.Count; activeEnemiesCount = 0;
            for (int i = 0; i < queue.Count; i++)
            {
                // Hard cap: never exceed 35 active enemies concurrently
                while (activeEnemiesCount >= 35)
                {
                    yield return new WaitForSeconds(0.25f);
                }

                SpawnEnemy(queue[i], wave.speedMultiplier, wave.healthMultiplier);
                remainingInQueue--; activeEnemiesCount++;
                yield return new WaitForSeconds(wave.spawnInterval * spawnPaceMultiplier);
            }

            while (activeEnemiesCount > 0) yield return new WaitForSeconds(0.4f);

            OnWaveCompleted?.Invoke(wave.waveNumber);
            if ((wave.spawnBossBrute || wave.spawnCrawlerBoss) && ExtractionZone.Instance != null) ExtractionZone.Instance.ActivateZone();
            currentWaveIndex++;
            StartCoroutine(StartNextWaveRoutine());
        }

        private void SpawnEnemy(GameObject prefab, float speedMult, float healthMult)
        {
            Vector3 center = (PlayerController.Instance != null) ? PlayerController.Instance.transform.position : Vector3.zero;
            Vector2 circle = UnityEngine.Random.insideUnitCircle.normalized * spawnRadius;
            Vector3 spawnPos = center + new Vector3(circle.x, 0.5f, circle.y);
            spawnPos.x = Mathf.Clamp(spawnPos.x, -56f, 56f);
            spawnPos.z = Mathf.Clamp(spawnPos.z, -56f, 56f);
            GameObject enemy = Instantiate(prefab, spawnPos, Quaternion.identity);
            Grounding.SnapToGround(enemy, Grounding.PlayerFloorY(PlayerController.Instance != null ? PlayerController.Instance.transform : null));

            if (enemy.TryGetComponent<ZombieEnemy>(out var z)) z.SetSpeedMultiplier(speedMult);
            if (enemy.TryGetComponent<RunnerEnemy>(out var r)) r.SetSpeedMultiplier(speedMult);
            if (enemy.TryGetComponent<SpitterEnemy>(out var s)) s.SetSpeedMultiplier(speedMult);
            if (enemy.TryGetComponent<TankEnemy>(out var t)) t.SetSpeedMultiplier(speedMult);
            if (enemy.TryGetComponent<BossBrute>(out var b)) b.SetSpeedMultiplier(speedMult);
            if (enemy.TryGetComponent<CrawlerKingBoss>(out var c)) c.SetSpeedMultiplier(speedMult);

            bool isBoss = (prefab == bossBrutePrefab || prefab == bossCrawlerPrefab);
            if (enemy.TryGetComponent<Health>(out var hp))
            {
                if (healthMult > 1.0f) hp.Initialize(hp.MaxHealth * healthMult);
                hp.OnDeath += () =>
                {
                    activeEnemiesCount = Mathf.Max(0, activeEnemiesCount - 1);
                    if (GameManager.Instance != null) GameManager.Instance.AddKill();
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayHit();
                    TrySpawnLoot(enemy.transform.position, isBoss, prefab == spitterPrefab);
                };
            }
        }

        [Header("Weapon Drops (M1)")]
        [SerializeField, Range(0f, 1f)] private float weaponDropChance = 0.07f;
        [SerializeField, Range(0f, 1f)] private float futuristicEnemyDropChance = 0.30f;

        private void TrySpawnLoot(Vector3 pos, bool isBoss, bool futuristicEnemy = false)
        {
            Vector3 dropPos = pos + Vector3.up * 0.4f;
            // Guns: bosses always drop a high-tier gun; sci-fi enemies drop futuristic guns more often.
            float gunChance = isBoss ? 1f : (futuristicEnemy ? futuristicEnemyDropChance : weaponDropChance);
            if (UnityEngine.Random.value < gunChance)
            {
                var gun = Deadzone.Combat.WeaponPickup.RollWeapon(isBoss ? 0.9f : (futuristicEnemy ? 0.45f : 0.1f), futuristicEnemy || isBoss);
                Deadzone.Combat.WeaponPickup.Spawn(gun, pos + Vector3.forward * 0.6f);
            }
            // Salvage for the backpack: robots carry tech, infected carry bio samples; bosses drop a pile.
            float salvageChance = isBoss ? 1f : (futuristicEnemy ? 0.45f : 0.14f);
            if (UnityEngine.Random.value < salvageChance && gemPickupPrefab)
            {
                int n = isBoss ? 3 : 1;
                for (int s = 0; s < n; s++)
                {
                    var go = Instantiate(gemPickupPrefab, dropPos + new Vector3(UnityEngine.Random.Range(-0.9f, 0.9f), 0f, UnityEngine.Random.Range(-0.9f, 0.9f)), Quaternion.identity);
                    if (go.TryGetComponent<LootDrop>(out var ld))
                        ld.Configure(PickupType.Salvage, 1f, futuristicEnemy || isBoss ? ItemKind.TechScrap : ItemKind.BioSample);
                }
            }
            if (isBoss)
            {
                if (healthPickupPrefab) Instantiate(healthPickupPrefab, dropPos + Vector3.left * 0.8f, Quaternion.identity);
                if (gemPickupPrefab) Instantiate(gemPickupPrefab, dropPos + Vector3.right * 0.8f, Quaternion.identity);
                return;
            }
            float roll = UnityEngine.Random.value;
            if (roll < 0.28f && gemPickupPrefab) Instantiate(gemPickupPrefab, dropPos, Quaternion.identity);
            else if (roll < 0.44f && ammoPickupPrefab) Instantiate(ammoPickupPrefab, dropPos, Quaternion.identity);
            else if (roll < 0.54f && healthPickupPrefab) Instantiate(healthPickupPrefab, dropPos, Quaternion.identity);
        }
    }
}
