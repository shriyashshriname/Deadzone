using UnityEngine;
using Deadzone.Core;
using Deadzone.Player;

namespace Deadzone.Enemies
{
    [RequireComponent(typeof(Health))]
    public class ZombieEnemy : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 3.5f;
        [SerializeField] private float rotationSpeed = 8f;
        [SerializeField] private float stoppingDistance = 1.2f;

        [Header("Combat")]
        [SerializeField] private float damage = 15f;
        [SerializeField] private float attackInterval = 1.0f;
        [SerializeField] private float attackRange = 1.5f;

        private Health health;
        private Transform playerTarget;
        private float nextAttackTime;
        private bool isDead;

        private void Awake()
        {
            health = GetComponent<Health>();
        }

        private void Start()
        {
            health.OnDeath += HandleDeath;

            if (PlayerController.Instance != null)
            {
                playerTarget = PlayerController.Instance.transform;
            }

            if (InfectedSwarmDirector.Instance != null)
            {
                InfectedSwarmDirector.Instance.RegisterEnemy(this);
            }
        }

        private void OnDestroy()
        {
            if (health != null) health.OnDeath -= HandleDeath;
            if (InfectedSwarmDirector.Instance != null)
            {
                InfectedSwarmDirector.Instance.UnregisterEnemy(this);
            }
        }

        private void Update()
        {
            if (isDead) return;

            if (!playerTarget && PlayerController.Instance != null)
            {
                playerTarget = PlayerController.Instance.transform;
            }

            if (!playerTarget) return;

            float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);

            // Move towards player if outside stopping distance
            if (distanceToPlayer > stoppingDistance)
            {
                Vector3 direction = (playerTarget.position - transform.position).normalized;
                direction.y = 0f;

                if (direction.sqrMagnitude > 0.01f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
                }

                transform.position += direction * (moveSpeed * Time.deltaTime);
            }

            // Attack player if within range
            if (distanceToPlayer <= attackRange && Time.time >= nextAttackTime)
            {
                AttackPlayer();
            }
        }

        private void AttackPlayer()
        {
            nextAttackTime = Time.time + attackInterval;

            if (playerTarget.TryGetComponent<IDamageable>(out var damageable))
            {
                damageable.TakeDamage(damage);
            }
        }

        public void SetSpeedMultiplier(float mult)
        {
            moveSpeed *= mult;
        }

        public bool IsAlerted { get; private set; }
        public Vector3 LastSoundOrigin { get; private set; }

        public virtual void OnAcousticNoiseAlert(Vector3 soundOrigin)
        {
            if (isDead) return;
            IsAlerted = true;
            LastSoundOrigin = soundOrigin;

            // Snap target to player if heard gunshot
            if (PlayerController.Instance != null)
            {
                playerTarget = PlayerController.Instance.transform;
            }
        }

        private void HandleDeath()
        {
            isDead = true;
            if (AudioManager.Instance != null) AudioManager.Instance.PlayHit();

            // Disable collider immediately so it stops blocking shots
            if (TryGetComponent<Collider>(out var col)) col.enabled = false;

            // Destroy gameobject after brief delay
            Destroy(gameObject, 0.25f);
        }
    }
}
