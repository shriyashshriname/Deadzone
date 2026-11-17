using UnityEngine;
using Deadzone.Player;

namespace Deadzone.Core
{
    public class SimpleSpawner : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private float spawnInterval = 3f;
        [SerializeField] private float spawnRadius = 15f;
        [SerializeField] private int maxEnemies = 10;

        private float nextSpawnTime;

        private void Update()
        {
            if (!enemyPrefab || Time.time < nextSpawnTime) return;

            // Check how many enemies exist
            int currentEnemies = FindObjectsByType<Deadzone.Enemies.ZombieEnemy>(FindObjectsSortMode.None).Length;
            if (currentEnemies >= maxEnemies) return;

            SpawnEnemy();
            nextSpawnTime = Time.time + spawnInterval;
        }

        private void SpawnEnemy()
        {
            Vector3 center = (PlayerController.Instance != null) ? PlayerController.Instance.transform.position : transform.position;

            Vector2 randomCircle = Random.insideUnitCircle.normalized * spawnRadius;
            Vector3 spawnPos = center + new Vector3(randomCircle.x, 0.5f, randomCircle.y);

            Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
        }
    }
}
