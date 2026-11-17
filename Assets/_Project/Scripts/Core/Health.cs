using System;
using UnityEngine;

namespace Deadzone.Core
{
    public interface IDamageable
    {
        void TakeDamage(float amount);
        bool IsDead { get; }
    }

    public class Health : MonoBehaviour, IDamageable
    {
        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth;

        public event Action<float, float> OnHealthChanged; // current, max
        public event Action OnDeath;

        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public bool IsDead => currentHealth <= 0f;

        private void Awake()
        {
            currentHealth = maxHealth;
        }

        public void TakeDamage(float amount)
        {
            if (IsDead || amount <= 0f) return;

            currentHealth = Mathf.Max(0f, currentHealth - amount);
            if (CompareTag("Player") && RunTelemetryLogger.Instance != null) RunTelemetryLogger.Instance.RecordDamageTaken(currentHealth, maxHealth);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            if (IsDead)
            {
                OnDeath?.Invoke();
            }
        }

        public void Heal(float amount)
        {
            if (IsDead || amount <= 0f) return;

            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public void ResetHealth()
        {
            currentHealth = maxHealth;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public void Initialize(float max)
        {
            maxHealth = max;
            currentHealth = max;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }
    }
}
