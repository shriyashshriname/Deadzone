using UnityEngine;
using Deadzone.Core;
using Deadzone.Player;

namespace Deadzone.Enemies
{
    [RequireComponent(typeof(Health))]
    public class TankEnemy : MonoBehaviour
    {
        [Header("Tank Stats")]
        [SerializeField] private float moveSpeed = 2.2f;
        [SerializeField] private float damage = 45f;
        [SerializeField] private float attackRange = 2.0f;
        [SerializeField] private float attackInterval = 1.8f;

        private Health health;
        private Transform playerTarget;
        private float nextAttackTime;

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

            float dist = Vector3.Distance(transform.position, playerTarget.position);
            Vector3 dir = (playerTarget.position - transform.position).normalized;
            dir.y = 0f;

            if (dir.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 4f * Time.deltaTime);
            }

            if (dist > attackRange)
            {
                transform.position += dir * (moveSpeed * Time.deltaTime);
            }
            else if (Time.time >= nextAttackTime)
            {
                Attack();
            }
        }

        private void Attack()
        {
            nextAttackTime = Time.time + attackInterval;

            if (playerTarget != null && playerTarget.TryGetComponent<IDamageable>(out var dmg))
            {
                dmg.TakeDamage(damage);
                if (Camera.TopDownCamera.Instance != null) Camera.TopDownCamera.Instance.TriggerShake(0.3f, 0.2f);
            }
        }

        public void SetSpeedMultiplier(float mult)
        {
            moveSpeed *= mult;
        }

        private void HandleDeath()
        {
            if (TryGetComponent<Collider>(out var col)) col.enabled = false;
            Destroy(gameObject, 0.6f);
        }
    }
}
