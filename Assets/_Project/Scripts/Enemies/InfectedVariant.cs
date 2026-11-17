using UnityEngine;
using Deadzone.Core;
using Deadzone.Combat;

namespace Deadzone.Enemies
{
    public enum VariantType
    {
        Standard,
        Armored,
        AcidBlood,
        Frenzy
    }

    [RequireComponent(typeof(Health))]
    public class InfectedVariant : MonoBehaviour
    {
        [Header("Variant Settings")]
        [SerializeField] private VariantType variantType = VariantType.Standard;
        [SerializeField] private float acidBurstDamage = 25f;
        [SerializeField] private float acidBurstRadius = 3.5f;

        private Health health;

        public VariantType Variant => variantType;

        private void Awake()
        {
            health = GetComponent<Health>();
        }

        private void Start()
        {
            health.OnDeath += HandleDeath;
            ApplyVariantBuffs();
        }

        private void OnDestroy()
        {
            if (health != null) health.OnDeath -= HandleDeath;
        }

        public void SetVariant(VariantType type)
        {
            variantType = type;
            ApplyVariantBuffs();
        }

        private void ApplyVariantBuffs()
        {
            var renderer = GetComponentInChildren<Renderer>();
            if (renderer == null || renderer.material == null) return;

            switch (variantType)
            {
                case VariantType.Armored:
                    // Tint metallic dark grey
                    if (renderer.material.HasProperty("_BaseColor")) renderer.material.SetColor("_BaseColor", new Color(0.35f, 0.35f, 0.4f));
                    break;
                case VariantType.AcidBlood:
                    // Tint neon toxic green
                    if (renderer.material.HasProperty("_BaseColor")) renderer.material.SetColor("_BaseColor", new Color(0.2f, 0.9f, 0.2f));
                    break;
                case VariantType.Frenzy:
                    // Tint fiery red
                    if (renderer.material.HasProperty("_BaseColor")) renderer.material.SetColor("_BaseColor", new Color(0.95f, 0.2f, 0.1f));
                    break;
            }
        }

        private void HandleDeath()
        {
            if (variantType == VariantType.AcidBlood)
            {
                TriggerAcidBurst();
            }

            // Elite variants guarantee a loot drop
            if (variantType != VariantType.Standard)
            {
                SpawnGuaranteedLoot();
            }
        }

        private void TriggerAcidBurst()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, acidBurstRadius);
            foreach (var hit in hits)
            {
                if (hit.CompareTag("Player") && hit.TryGetComponent<IDamageable>(out var dmg))
                {
                    dmg.TakeDamage(acidBurstDamage);
                }
            }

            if (AudioManager.Instance != null) AudioManager.Instance.PlayHit(1.1f);
        }

        private void SpawnGuaranteedLoot()
        {
            // Drop loot at death position
            GameObject dropObj = new GameObject("EliteLootDrop");
            dropObj.transform.position = transform.position + Vector3.up * 0.5f;
            var drop = dropObj.AddComponent<LootDrop>();
        }
    }
}
