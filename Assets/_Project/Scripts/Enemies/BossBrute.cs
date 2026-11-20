using System;
using UnityEngine;
using Deadzone.Core;
using Deadzone.Camera;
using Deadzone.Player;

namespace Deadzone.Enemies
{
    public enum BossState
    {
        Chasing,
        Telegraphing,
        Charging,
        Stunned
    }

    [RequireComponent(typeof(Health))]
    public class BossBrute : MonoBehaviour
    {
        [Header("Boss Identity")]
        public string bossName = "Subject Zero - Sector 1 Brute";

        [Header("Movement & Charge")]
        [SerializeField] private float chaseSpeed = 3.2f;
        [SerializeField] private float chargeSpeed = 16f;
        [SerializeField] private float chargeCooldown = 6.0f;
        [SerializeField] private float telegraphDuration = 1.2f;
        [SerializeField] private float stunDuration = 2.5f;

        [Header("Combat & Damage")]
        [SerializeField] private float meleeDamage = 35f;
        [SerializeField] private float chargeDamage = 60f;
        [SerializeField] private float meleeRange = 2.5f;

        private Health health;
        private Transform playerTarget;
        private BossState currentState = BossState.Chasing;
        private float stateTimer;
        private Vector3 chargeDirection;

        public event Action<float, float> OnBossHealthChanged;
        public event Action<BossState> OnBossStateChanged;

        private void Awake()
        {
            health = GetComponent<Health>();
        }

        private void Start()
        {
            health.OnHealthChanged += HandleHealthChanged;
            health.OnDeath += HandleDeath;
            if (PlayerController.Instance != null) playerTarget = PlayerController.Instance.transform;
            stateTimer = Time.time + chargeCooldown;
        }

        private void OnDestroy()
        {
            if (health != null)
            {
                health.OnHealthChanged -= HandleHealthChanged;
                health.OnDeath -= HandleDeath;
            }
        }

        private void Update()
        {
            if (health.IsDead) return;
            if (!playerTarget && PlayerController.Instance != null) playerTarget = PlayerController.Instance.transform;
            if (!playerTarget) return;

            switch (currentState)
            {
                case BossState.Chasing:
                    UpdateChasing();
                    break;
                case BossState.Telegraphing:
                    UpdateTelegraphing();
                    break;
                case BossState.Charging:
                    UpdateCharging();
                    break;
                case BossState.Stunned:
                    UpdateStunned();
                    break;
            }
        }

        private void UpdateChasing()
        {
            float dist = Vector3.Distance(transform.position, playerTarget.position);
            Vector3 dir = (playerTarget.position - transform.position).normalized;
            dir.y = 0f;

            if (dir.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 5f * Time.deltaTime);
            }

            transform.position += dir * (chaseSpeed * Time.deltaTime);

            if (dist <= meleeRange)
            {
                if (playerTarget.TryGetComponent<IDamageable>(out var dmg))
                {
                    dmg.TakeDamage(meleeDamage * Time.deltaTime);
                }
            }

            if (Time.time >= stateTimer)
            {
                StartTelegraph();
            }
        }

        private void StartTelegraph()
        {
            Deadzone.Core.CharacterAnimDriver.PlayAttack(this);
            currentState = BossState.Telegraphing;
            stateTimer = Time.time + telegraphDuration;
            chargeDirection = (playerTarget.position - transform.position).normalized;
            chargeDirection.y = 0f;
            transform.rotation = Quaternion.LookRotation(chargeDirection);
            OnBossStateChanged?.Invoke(currentState);
        }

        private void UpdateTelegraphing()
        {
            // Lock onto direction during telegraph windup
            if (Time.time >= stateTimer)
            {
                currentState = BossState.Charging;
                stateTimer = Time.time + 1.8f; // Max charge duration
                OnBossStateChanged?.Invoke(currentState);
                if (TopDownCamera.Instance != null) TopDownCamera.Instance.TriggerShake(0.3f, 0.2f);
            }
        }

        private void UpdateCharging()
        {
            transform.position += chargeDirection * (chargeSpeed * Time.deltaTime);

            // Check collision with player
            if (playerTarget != null && Vector3.Distance(transform.position, playerTarget.position) <= 2.2f)
            {
                if (playerTarget.TryGetComponent<IDamageable>(out var dmg))
                {
                    dmg.TakeDamage(chargeDamage);
                }
                EndChargeWithStun();
                return;
            }

            // Check wall collision to stun boss
            if (Physics.Raycast(transform.position + Vector3.up * 0.5f, chargeDirection, out RaycastHit wallHit, 1.8f))
            {
                if (!wallHit.collider.CompareTag("Player") && !wallHit.collider.CompareTag("Enemy"))
                {
                    EndChargeWithStun();
                    return;
                }
            }

            // Charge timeout
            if (Time.time >= stateTimer)
            {
                EndChargeNormal();
            }
        }

        private void EndChargeWithStun()
        {
            currentState = BossState.Stunned;
            stateTimer = Time.time + stunDuration;
            OnBossStateChanged?.Invoke(currentState);
            if (TopDownCamera.Instance != null) TopDownCamera.Instance.TriggerShake(0.4f, 0.3f);
        }

        private void EndChargeNormal()
        {
            currentState = BossState.Chasing;
            stateTimer = Time.time + chargeCooldown;
            OnBossStateChanged?.Invoke(currentState);
        }

        private void UpdateStunned()
        {
            if (Time.time >= stateTimer)
            {
                currentState = BossState.Chasing;
                stateTimer = Time.time + chargeCooldown;
                OnBossStateChanged?.Invoke(currentState);
            }
        }

        private void HandleHealthChanged(float curr, float max)
        {
            OnBossHealthChanged?.Invoke(curr, max);

            // Phase 2 Enrage (< 50% HP)
            if (curr < max * 0.5f)
            {
                chaseSpeed = 4.2f;
                chargeCooldown = 4.0f;
            }
        }

        [SerializeField] private bool triggerVictoryOnDeath = false;

        public void SetSpeedMultiplier(float mult)
        {
            chaseSpeed *= mult;
            chargeSpeed *= mult;
        }

        private void HandleDeath()
        {
            if (TryGetComponent<Collider>(out var col)) col.enabled = false;
            if (triggerVictoryOnDeath && GameManager.Instance != null) GameManager.Instance.TriggerVictory();
            Destroy(gameObject, 1.5f);
        }
    }
}
