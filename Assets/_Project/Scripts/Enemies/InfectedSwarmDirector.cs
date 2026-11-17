using System.Collections.Generic;
using UnityEngine;
using Deadzone.Player;

namespace Deadzone.Enemies
{
    public class InfectedSwarmDirector : MonoBehaviour
    {
        public static InfectedSwarmDirector Instance { get; private set; }

        [Header("Swarm Dispersion")]
        [SerializeField] private float flankAngleSpread = 60f;

        private List<ZombieEnemy> registeredEnemies = new List<ZombieEnemy>();

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void RegisterEnemy(ZombieEnemy enemy)
        {
            if (!registeredEnemies.Contains(enemy))
            {
                registeredEnemies.Add(enemy);
            }
        }

        public void UnregisterEnemy(ZombieEnemy enemy)
        {
            registeredEnemies.Remove(enemy);
        }

        public void AlertEnemiesInRadius(Vector3 origin, float radius)
        {
            float radiusSqr = radius * radius;
            for (int i = 0; i < registeredEnemies.Count; i++)
            {
                ZombieEnemy enemy = registeredEnemies[i];
                if (enemy != null && (enemy.transform.position - origin).sqrMagnitude <= radiusSqr)
                {
                    enemy.OnAcousticNoiseAlert(origin);
                }
            }
        }

        public Vector3 GetFlankDirection(ZombieEnemy enemy, Vector3 playerPos)
        {
            int index = registeredEnemies.IndexOf(enemy);
            if (index == -1 || registeredEnemies.Count <= 1)
            {
                Vector3 direct = (playerPos - enemy.transform.position).normalized;
                direct.y = 0f;
                return direct;
            }

            // Distribute angles evenly around player
            float offset = ((float)index / registeredEnemies.Count - 0.5f) * flankAngleSpread;
            Vector3 baseDir = (playerPos - enemy.transform.position).normalized;
            baseDir.y = 0f;

            Quaternion rotation = Quaternion.Euler(0f, offset, 0f);
            return rotation * baseDir;
        }
    }
}
