using UnityEngine;
using Deadzone.Core;
using Deadzone.Player;

namespace Deadzone.Enemies
{
    [RequireComponent(typeof(Health))]
    public class RunnerEnemy : MonoBehaviour
    {
        [Header("Runner Stats")]
        [SerializeField] private float moveSpeed = 6.5f;
        [SerializeField] private float rotationSpeed = 12f;
        [SerializeField] private float leapDistance = 3.5f;
        [SerializeField] private float damage = 20f;
        [SerializeField] private float attackCooldown = 1.2f;

        private Health health;
        private Transform playerTarget;
        private float nextAttackTime;
        private bool isLeaping;
        private Vector3 leapTarget;
        private float leapStartTime;
        private const float LeapDuration = 0.35f;

        private void Awake()
        {
            health = GetComponent<Health>();
        }

        private void Start()
        {
            health.OnDeath += HandleDeath;
            if (PlayerController.Instance != null) playerTarget = PlayerController.Instance.transform;
        }

        private void OnDestroy()
        {
            if (health != null) health.OnDeath -= HandleDeath;
        }

        private void Update()
        {
            if (health.IsDead) return;
            if (!playerTarget && PlayerController.Instance != null) playerTarget = PlayerController.Instance.transform;
            if (!playerTarget) return;

            if (isLeaping)
            {
                ExecuteLeap();
                return;
            }

            float dist = Vector3.Distance(transform.position, playerTarget.position);

            // Trigger leap attack if in range
            if (dist <= leapDistance && Time.time >= nextAttackTime)
            {
                StartLeap();
                return;
            }

            // Normal rapid pursuit
            Vector3 dir = (playerTarget.position - transform.position).normalized;
            dir.y = 0f;

            if (dir.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
            }

            transform.position += dir * (moveSpeed * Time.deltaTime);
        }

        private void StartLeap()
        {
            isLeaping = true;
            leapStartTime = Time.time;
            leapTarget = playerTarget.position;
            leapTarget.y = transform.position.y; // stay on the floor; player root height differs
            nextAttackTime = Time.time + attackCooldown;
        }

        private void ExecuteLeap()
        {
            float elapsed = (Time.time - leapStartTime) / LeapDuration;
            transform.position = Vector3.Lerp(transform.position, leapTarget, elapsed);

            if (elapsed >= 1f)
            {
                isLeaping = false;
                // Damage check at landing
                if (playerTarget != null && Vector3.Distance(transform.position, playerTarget.position) <= 1.5f)
                {
                    if (playerTarget.TryGetComponent<IDamageable>(out var damageable))
                    {
                        damageable.TakeDamage(damage);
                    }
                }
            }
        }

        public void SetSpeedMultiplier(float mult)
        {
            moveSpeed *= mult;
        }

        private void HandleDeath()
        {
            if (TryGetComponent<Collider>(out var col)) col.enabled = false;
            Destroy(gameObject, 0.4f);
        }
    }
}
