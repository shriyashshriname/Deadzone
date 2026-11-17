using System;
using System.Collections.Generic;
using UnityEngine;
using Deadzone.Core;

namespace Deadzone.Player
{
    public enum PerkType
    {
        DamageBoost,
        SpeedBoost,
        HealthBoost,
        FireRateBoost,
        DashCooldownBoost
    }

    [Serializable]
    public class PerkOption
    {
        public PerkType perkType;
        public string title;
        public string description;
        public float value;
    }

    public class PlayerPerks : MonoBehaviour
    {
        public static PlayerPerks Instance { get; private set; }

        public float DamageMultiplier { get; private set; } = 1.0f;
        public float SpeedMultiplier { get; private set; } = 1.0f;
        public float FireRateMultiplier { get; private set; } = 1.0f;
        public float DashCooldownMultiplier { get; private set; } = 1.0f;

        public event Action<List<PerkOption>> OnPerkSelectionPrompt;
        public event Action<PerkOption> OnPerkApplied;

        private Health playerHealth;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            playerHealth = GetComponent<Health>();
        }

        public bool TryPurchaseUpgrade(PerkType type, int cost)
        {
            if (GameManager.Instance == null || GameManager.Instance.CreditsEarned < cost) return false;
            if (!GameManager.Instance.SpendCredits(cost)) return false;

            switch (type)
            {
                case PerkType.DamageBoost:
                    DamageMultiplier += 0.20f;
                    break;
                case PerkType.SpeedBoost:
                    SpeedMultiplier += 0.15f;
                    break;
                case PerkType.HealthBoost:
                    if (playerHealth == null) playerHealth = GetComponent<Health>();
                    if (playerHealth != null)
                    {
                        playerHealth.Initialize(playerHealth.MaxHealth + 35f);
                        playerHealth.Heal(999f);
                    }
                    break;
                case PerkType.FireRateBoost:
                    FireRateMultiplier += 0.25f;
                    break;
                case PerkType.DashCooldownBoost:
                    DashCooldownMultiplier = Mathf.Max(0.35f, DashCooldownMultiplier - 0.20f);
                    break;
            }

            if (AudioManager.Instance != null) AudioManager.Instance.PlayPickup(1.2f);
            return true;
        }

        public void OfferPerkSelection()
        {
            List<PerkOption> pool = new List<PerkOption>
            {
                new PerkOption { perkType = PerkType.DamageBoost, title = "Hollow-Point Rounds", description = "+20% Bullet Damage", value = 0.20f },
                new PerkOption { perkType = PerkType.SpeedBoost, title = "Combat Stems", description = "+15% Movement Speed", value = 0.15f },
                new PerkOption { perkType = PerkType.HealthBoost, title = "Bio-Kevlar Plating", description = "+35 Max Health & Instant Heal", value = 35f },
                new PerkOption { perkType = PerkType.FireRateBoost, title = "Hair Trigger", description = "+25% Weapon Fire Rate", value = 0.25f },
                new PerkOption { perkType = PerkType.DashCooldownBoost, title = "Kinetic Thrusters", description = "-20% Dash Cooldown", value = 0.20f }
            };

            // Pick 3 unique random perks
            List<PerkOption> choices = new List<PerkOption>();
            while (choices.Count < 3 && pool.Count > 0)
            {
                int index = UnityEngine.Random.Range(0, pool.Count);
                choices.Add(pool[index]);
                pool.RemoveAt(index);
            }

            OnPerkSelectionPrompt?.Invoke(choices);
        }

        public void SelectPerk(PerkOption perk)
        {
            switch (perk.perkType)
            {
                case PerkType.DamageBoost:
                    DamageMultiplier += perk.value;
                    break;
                case PerkType.SpeedBoost:
                    SpeedMultiplier += perk.value;
                    break;
                case PerkType.HealthBoost:
                    if (playerHealth != null)
                    {
                        playerHealth.Heal(perk.value);
                    }
                    break;
                case PerkType.FireRateBoost:
                    FireRateMultiplier += perk.value;
                    break;
                case PerkType.DashCooldownBoost:
                    DashCooldownMultiplier = Mathf.Max(0.4f, DashCooldownMultiplier - perk.value);
                    break;
            }

            OnPerkApplied?.Invoke(perk);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayPickup(1.1f);
        }
    }
}
