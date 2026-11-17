using UnityEngine;
using Deadzone.Core;
using Deadzone.Camera;

namespace Deadzone.Combat
{
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(Collider))]
    public class ExplosiveBarrel : MonoBehaviour
    {
        [Header("Explosion Config")]
        [SerializeField] private float explosionRadius = 5.5f;
        [SerializeField] private float explosionDamage = 120f;
        [SerializeField] private LayerMask damageLayers;

        private Health health;
        private bool hasExploded = false;

        private void Awake()
        {
            health = GetComponent<Health>();
        }

        private void Start()
        {
            health.OnDeath += Explode;
        }

        private void OnDestroy()
        {
            if (health != null) health.OnDeath -= Explode;
        }

        private void Explode()
        {
            if (hasExploded) return;
            hasExploded = true;

            // Trigger Camera Shake
            if (TopDownCamera.Instance != null)
            {
                TopDownCamera.Instance.TriggerShake(0.45f, 0.25f);
            }

            // Damage all entities in radius
            Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);
            foreach (var col in colliders)
            {
                // Skip self
                if (col.gameObject == gameObject) continue;

                if (col.TryGetComponent<IDamageable>(out var damageable))
                {
                    float distance = Vector3.Distance(transform.position, col.transform.position);
                    float falloff = Mathf.Clamp01(1f - (distance / explosionRadius));
                    damageable.TakeDamage(explosionDamage * falloff);
                }
            }

            // Play explosion audio
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayBossRoar(1.2f);
            }

            Destroy(gameObject, 0.1f);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }
    }
}
