using UnityEngine;
using Deadzone.Player;
using Deadzone.Combat;
using Deadzone.Enemies;

namespace Deadzone.Core
{
    public class AutonomousCombatBot : MonoBehaviour
    {
        [Header("Bot Controller")]
        [SerializeField] private bool botActive = false;
        [SerializeField] private float scanRange = 25f;
        [SerializeField] private float fireDistance = 14f;
        [SerializeField] private float panicDashDistance = 2.8f;

        private PlayerController player;
        private Weapon weapon;
        private PlayerDash dash;

        private void Start()
        {
            player = GetComponent<PlayerController>();
            weapon = GetComponentInChildren<Weapon>();
            dash = GetComponent<PlayerDash>();
        }

        private void Update()
        {
            if (!botActive) return;

            // Find nearest infected
            GameObject nearestEnemy = FindNearestInfected();
            if (nearestEnemy == null) return;

            float dist = Vector3.Distance(transform.position, nearestEnemy.transform.position);
            Vector3 aimDir = (nearestEnemy.transform.position - transform.position).normalized;
            aimDir.y = 0f;

            // Aim towards target
            if (aimDir.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(aimDir), 12f * Time.deltaTime);
            }

            // Panic dash if enemy is too close
            if (dist <= panicDashDistance && dash != null)
            {
                dash.TryTriggerDash();
            }

            // Fire weapon if within range
            if (dist <= fireDistance && weapon != null && weapon.CanFire())
            {
                weapon.TryFire();
            }
        }

        private GameObject FindNearestInfected()
        {
            // Search all enemy colliders in radius
            Collider[] hits = Physics.OverlapSphere(transform.position, scanRange);
            GameObject closest = null;
            float closestDist = float.MaxValue;

            foreach (var hit in hits)
            {
                if (hit.CompareTag("Enemy") || hit.GetComponent<ZombieEnemy>() || hit.GetComponent<RunnerEnemy>() || hit.GetComponent<BossBrute>())
                {
                    float d = Vector3.Distance(transform.position, hit.transform.position);
                    if (d < closestDist)
                    {
                        closestDist = d;
                        closest = hit.gameObject;
                    }
                }
            }

            return closest;
        }

        public void SetBotActive(bool active)
        {
            botActive = active;
        }
    }
}
