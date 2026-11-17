using UnityEngine;
using Deadzone.Core;
using Deadzone.Camera;

namespace Deadzone.Combat
{
    [RequireComponent(typeof(Collider))]
    public class Grenade : MonoBehaviour
    {
        [Header("Grenade Stats")]
        [SerializeField] private float fuseTime = 2.0f;
        [SerializeField] private float explosionRadius = 6.0f;
        [SerializeField] private float explosionDamage = 120f;

        private float spawnTime;
        private bool hasExploded = false;

        private void Start()
        {
            spawnTime = Time.time;
        }

        private void Update()
        {
            if (hasExploded) return;
            if (Time.time - spawnTime >= fuseTime)
            {
                Explode();
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (hasExploded) return;
            if (collision.collider.CompareTag("Enemy") || collision.collider.GetComponentInParent<IDamageable>() != null)
            {
                Explode();
            }
        }

        public void Explode()
        {
            if (hasExploded) return;
            hasExploded = true;

            if (TopDownCamera.Instance != null)
            {
                TopDownCamera.Instance.TriggerShake(0.5f, 0.25f);
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayBossRoar(1.2f);
            }

            Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius);
            foreach (var col in hits)
            {
                if (col.gameObject == gameObject) continue;
                var dmg = col.GetComponentInParent<IDamageable>() ?? col.GetComponent<IDamageable>();
                if (dmg != null)
                {
                    float d = Vector3.Distance(transform.position, col.transform.position);
                    float factor = Mathf.Clamp01(1f - (d / explosionRadius));
                    dmg.TakeDamage(explosionDamage * factor);
                }
            }

            if (MuzzleFlashPool.Instance != null)
            {
                MuzzleFlashPool.Instance.TriggerFlash(transform.position + Vector3.up * 0.5f, Vector3.up, 4.5f, explosionRadius * 1.5f);
            }

            Destroy(gameObject);
        }
    }
}
