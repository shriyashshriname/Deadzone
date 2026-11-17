using UnityEngine;
using Deadzone.Core;

namespace Deadzone.Combat
{
    [RequireComponent(typeof(Collider))]
    public class AcidProjectile : MonoBehaviour
    {
        [Header("Acid Projectile")]
        [SerializeField] private float speed = 12f;
        [SerializeField] private float damage = 18f;
        [SerializeField] private float lifetime = 4f;

        private float spawnTime;

        private void Start()
        {
            spawnTime = Time.time;
        }

        public void Initialize(float acidDamage, float projectileSpeed)
        {
            damage = acidDamage;
            speed = projectileSpeed;
        }

        private void Update()
        {
            transform.position += transform.forward * (speed * Time.deltaTime);

            if (Time.time - spawnTime >= lifetime)
            {
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // Do not hit other enemies or spitters
            if (other.CompareTag("Enemy")) return;

            if (other.TryGetComponent<IDamageable>(out var damageable))
            {
                damageable.TakeDamage(damage);
            }

            Destroy(gameObject);
        }
    }
}
