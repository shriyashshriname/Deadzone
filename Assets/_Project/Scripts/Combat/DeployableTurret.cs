using UnityEngine;
using Deadzone.Core;
using Deadzone.Enemies;
using Deadzone.Player;

namespace Deadzone.Combat
{
    public class DeployableTurret : MonoBehaviour
    {
        [Header("Turret Parameters")]
        [SerializeField] private float scanRadius = 16f;
        [SerializeField] private float fireRate = 6f;
        [SerializeField] private float damagePerShot = 14f;
        [SerializeField] private float lifetime = 25f;
        [SerializeField] private Transform firePoint;
        [SerializeField] private GameObject projectilePrefab;

        private float nextFireTime;
        private float spawnTime;
        private Transform currentTarget;

        private void Start()
        {
            spawnTime = Time.time;
        }

        private void Update()
        {
            if (Time.time - spawnTime >= lifetime)
            {
                Destroy(gameObject);
                return;
            }

            FindTarget();

            if (currentTarget != null)
            {
                Vector3 dir = (currentTarget.position - transform.position).normalized;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.01f)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 12f * Time.deltaTime);
                }

                if (Time.time >= nextFireTime)
                {
                    Fire();
                }
            }
        }

        private void FindTarget()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, scanRadius);
            float closestDist = float.MaxValue;
            currentTarget = null;

            foreach (var hit in hits)
            {
                if (hit.gameObject == gameObject || hit.CompareTag("Player") || hit.GetComponentInParent<PlayerController>() != null) continue;

                bool isEnemy = hit.CompareTag("Enemy") 
                    || hit.GetComponentInParent<ZombieEnemy>() != null 
                    || hit.GetComponentInParent<RunnerEnemy>() != null 
                    || hit.GetComponentInParent<SpitterEnemy>() != null 
                    || hit.GetComponentInParent<TankEnemy>() != null 
                    || hit.GetComponentInParent<BossBrute>() != null 
                    || hit.GetComponentInParent<CrawlerKingBoss>() != null;

                if (isEnemy)
                {
                    Health hp = hit.GetComponentInParent<Health>();
                    if (hp != null && hp.IsDead) continue;

                    float d = Vector3.Distance(transform.position, hit.transform.position);
                    if (d < closestDist)
                    {
                        closestDist = d;
                        currentTarget = hit.transform;
                    }
                }
            }
        }

        private void Fire()
        {
            nextFireTime = Time.time + (1f / fireRate);

            if (projectilePrefab != null)
            {
                Vector3 pos = firePoint ? firePoint.position : transform.position + Vector3.up * 0.8f;
                Vector3 dir = currentTarget != null ? (currentTarget.position + Vector3.up * 0.4f - pos).normalized : transform.forward;
                GameObject projObj = Instantiate(projectilePrefab, pos, Quaternion.LookRotation(dir));
                if (projObj.TryGetComponent<Projectile>(out var proj))
                {
                    proj.Initialize(damagePerShot, 35f);
                }
            }

            if (AudioManager.Instance != null) AudioManager.Instance.PlayShoot(0.35f);
        }
    }
}
