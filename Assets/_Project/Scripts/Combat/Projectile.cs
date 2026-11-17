using System.Collections.Generic;
using UnityEngine;
using Deadzone.Core;
using Deadzone.Player;

namespace Deadzone.Combat
{
    public class Projectile : MonoBehaviour
    {
        [Header("Projectile Config")]
        [SerializeField] private float lifetime = 3f;

        private float damage = 25f;
        private float speed = 40f;
        private int penetration = 1;
        private int shooterTeam = 1;
        private float spawnTime;
        private readonly HashSet<IDamageable> alreadyHit = new HashSet<IDamageable>();

        public void Initialize(float damageAmount, float projectileSpeed, int penetrations = 1, int team = 1)
        {
            damage = damageAmount;
            speed = projectileSpeed;
            penetration = Mathf.Max(1, penetrations);
            shooterTeam = team;
            spawnTime = Time.time;
        }

        public void SetShooterTeam(int team) => shooterTeam = team;

        private void Awake()
        {
            Collider col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;
            }
        }

        private void Start()
        {
            if (spawnTime <= 0f) spawnTime = Time.time;
        }

        private void Update()
        {
            float step = speed * Time.deltaTime;

            // Swept multi-hit collision check for enemy penetration
            RaycastHit[] hits = Physics.RaycastAll(transform.position, transform.forward, step);
            if (hits.Length > 1) System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            for (int i = 0; i < hits.Length; i++)
            {
                var hit = hits[i];
                if (hit.collider.gameObject == gameObject) continue;

                TeamMember tm = hit.collider.GetComponentInParent<TeamMember>() ?? hit.collider.GetComponent<TeamMember>();
                if (tm != null && !tm.IsHostileTo(shooterTeam)) continue;
                if (shooterTeam == 1 && (hit.collider.CompareTag("Player") || hit.collider.GetComponentInParent<PlayerController>() != null)) continue;

                if (HandleImpact(hit.collider)) return;
            }

            transform.position += transform.forward * step;

            if (Time.time - spawnTime >= lifetime)
            {
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<Projectile>() != null) return;
            TeamMember tm = other.GetComponentInParent<TeamMember>() ?? other.GetComponent<TeamMember>();
            if (tm != null && !tm.IsHostileTo(shooterTeam)) return;
            if (shooterTeam == 1 && (other.CompareTag("Player") || other.GetComponentInParent<PlayerController>() != null)) return;
            HandleImpact(other);
        }

        private bool HandleImpact(Collider targetCol)
        {
            IDamageable damageable = targetCol.GetComponentInParent<IDamageable>() ?? targetCol.GetComponent<IDamageable>();
            if (damageable != null)
            {
                TeamMember tm = targetCol.GetComponentInParent<TeamMember>() ?? targetCol.GetComponent<TeamMember>();
                if (tm != null && !tm.IsHostileTo(shooterTeam)) return false;

                if (!alreadyHit.Contains(damageable))
                {
                    alreadyHit.Add(damageable);
                    damageable.TakeDamage(damage);
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayHit();

                    penetration--;
                    if (penetration <= 0)
                    {
                        Destroy(gameObject);
                        return true;
                    }
                }
                return false;
            }

            // Hit solid wall or obstacle
            Destroy(gameObject);
            return true;
        }
    }
}
