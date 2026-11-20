using System;
using UnityEngine;
using Deadzone.Core;
using Deadzone.Camera;
using Deadzone.Combat;
using Deadzone.Player;

namespace Deadzone.Enemies
{
    public enum CrawlerBossState
    {
        Skittering,
        LeapAttack,
        AcidBarrage,
        BurrowAmbush
    }

    [RequireComponent(typeof(Health))]
    public class CrawlerKingBoss : MonoBehaviour
    {
        [Header("Identity")]
        public string bossName = "Crawler King - Hive Patriarch";

        [Header("Movement & Leap")]
        [SerializeField] private float moveSpeed = 5.2f;
        [SerializeField] private float leapSpeed = 18f;
        [SerializeField] private float leapCooldown = 5.0f;
        [SerializeField] private float leapRange = 8f;

        [Header("Combat & Acid")]
        [SerializeField] private float meleeDamage = 28f;
        [SerializeField] private float acidBurstInterval = 6.0f;
        [SerializeField] private GameObject acidProjectilePrefab;
        [SerializeField] private Transform spitPoint;

        private Health health;
        private Transform playerTarget;
        private CrawlerBossState currentState = CrawlerBossState.Skittering;
        private float nextLeapTime;
        private float nextAcidTime;
        private bool isEnraged;

        public event Action<float, float> OnBossHealthChanged;
        public event Action<CrawlerBossState> OnStateChanged;

        private void Awake()
        {
            health = GetComponent<Health>();
        }

        private void Start()
        {
            health.OnHealthChanged += HandleHealthChanged;
            health.OnDeath += HandleDeath;

            if (PlayerController.Instance != null)
            {
                playerTarget = PlayerController.Instance.transform;
            }

            nextLeapTime = Time.time + leapCooldown;
            nextAcidTime = Time.time + acidBurstInterval;
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
            if (health.IsDead || !playerTarget) return;

            float dist = Vector3.Distance(transform.position, playerTarget.position);

            switch (currentState)
            {
                case CrawlerBossState.Skittering:
                    HandleSkitter(dist);
                    break;
                case CrawlerBossState.LeapAttack:
                    // Leap handled by physics/timer
                    break;
                case CrawlerBossState.AcidBarrage:
                    // Spit executed in burst
                    break;
            }

            // Acid Barrage logic
            if (Time.time >= nextAcidTime && dist > 4f)
            {
                ExecuteAcidBarrage();
            }
        }

        private void HandleSkitter(float dist)
        {
            Vector3 dir = (playerTarget.position - transform.position).normalized;
            dir.y = 0f;

            if (dir.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 10f * Time.deltaTime);
            }

            transform.position += dir * (moveSpeed * Time.deltaTime);

            // Trigger leap attack if within optimal leap range
            if (dist <= leapRange && dist > 3f && Time.time >= nextLeapTime)
            {
                ExecuteLeap(dir);
            }
            else if (dist <= 2f)
            {
                // Melee strike
                if (playerTarget.TryGetComponent<IDamageable>(out var dmg))
                {
                    dmg.TakeDamage(meleeDamage * Time.deltaTime);
                }
            }
        }

        private void ExecuteLeap(Vector3 dir)
        {
            Deadzone.Core.CharacterAnimDriver.PlayAttack(this);
            currentState = CrawlerBossState.LeapAttack;
            nextLeapTime = Time.time + (isEnraged ? leapCooldown * 0.6f : leapCooldown);
            OnStateChanged?.Invoke(currentState);

            // Quick kinetic leap impulse
            transform.position += dir * (leapSpeed * 0.4f);

            if (TopDownCamera.Instance != null)
            {
                TopDownCamera.Instance.TriggerShake(0.2f, 0.15f);
            }

            currentState = CrawlerBossState.Skittering;
            OnStateChanged?.Invoke(currentState);
        }

        private void ExecuteAcidBarrage()
        {
            Deadzone.Core.CharacterAnimDriver.PlayAttack(this);
            nextAcidTime = Time.time + (isEnraged ? acidBurstInterval * 0.6f : acidBurstInterval);

            if (spitPoint != null && acidProjectilePrefab != null)
            {
                // 3-way spread
                float[] angles = { -15f, 0f, 15f };
                for (int i = 0; i < angles.Length; i++)
                {
                    Quaternion rot = spitPoint.rotation * Quaternion.Euler(0f, angles[i], 0f);
                    Instantiate(acidProjectilePrefab, spitPoint.position, rot);
                }
            }
        }

        private void HandleHealthChanged(float current, float max)
        {
            OnBossHealthChanged?.Invoke(current, max);

            // Enrage threshold at 35% HP
            if (!isEnraged && (current / max) <= 0.35f)
            {
                isEnraged = true;
                moveSpeed *= 1.35f;
                Debug.Log($"[CRAWLER KING] Enraged! Hive Patriarch speed and attack rate accelerated!");
            }
        }

        public void SetSpeedMultiplier(float mult)
        {
            moveSpeed *= mult;
            leapSpeed *= mult;
        }

        private void HandleDeath()
        {
            if (TryGetComponent<Collider>(out var col)) col.enabled = false;
            Debug.Log($"[CRAWLER KING] Hive Patriarch defeated!");
            Destroy(gameObject, 0.8f);
        }
    }
}
