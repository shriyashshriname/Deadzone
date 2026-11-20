using System;
using System.Collections.Generic;
using UnityEngine;
using Deadzone.Combat;
using Deadzone.UI;

namespace Deadzone.Core
{
    public enum ItemKind { Medkit, AmmoPack, TechScrap, BioSample }

    [Serializable]
    public class ItemStack
    {
        public ItemKind kind;
        public int count;
    }

    /// <summary>
    /// The player's run backpack ("storage"). Slot-limited, stackable items:
    ///  - Medkit / Ammo Pack: carried consumables, used from the HUD bar (tap) or H / J.
    ///  - Tech Scrap / Bio Sample: salvage worth credits, paid out only on extraction; lost on death.
    /// Capacity is a permanent upgrade (tier saved in the stash): 4 / 6 / 8 / 10 slots.
    /// </summary>
    public class Backpack : MonoBehaviour
    {
        public static Backpack Instance { get; private set; }

        public static readonly int[] TierSlots = { 4, 6, 8, 10 };
        public static readonly int[] UpgradeCost = { 150, 300, 500 }; // cost to go from tier i to i+1
        public const float MedkitHeal = 50f;

        [SerializeField] private int tier;
        [SerializeField] private List<ItemStack> slots = new List<ItemStack>();

        public event Action OnChanged;

        public int Tier => tier;
        public int Capacity => TierSlots[Mathf.Clamp(tier, 0, TierSlots.Length - 1)];
        public int UsedSlots => slots.Count;
        public IReadOnlyList<ItemStack> Slots => slots;
        public bool IsMaxTier => tier >= TierSlots.Length - 1;
        public int NextUpgradeCost => IsMaxTier ? 0 : UpgradeCost[tier];

        public static int MaxStack(ItemKind k) => k switch { ItemKind.Medkit => 3, ItemKind.AmmoPack => 2, _ => 5 };
        public static int Value(ItemKind k) => k switch { ItemKind.TechScrap => 25, ItemKind.BioSample => 15, _ => 0 };
        public static bool IsSalvage(ItemKind k) => k == ItemKind.TechScrap || k == ItemKind.BioSample;
        public static string Label(ItemKind k) => k switch { ItemKind.Medkit => "MED", ItemKind.AmmoPack => "AMMO", ItemKind.TechScrap => "TECH", _ => "BIO" };
        public static Color ColorOf(ItemKind k) => k switch
        {
            ItemKind.Medkit => Palette.Health,
            ItemKind.AmmoPack => Palette.Ammo,
            ItemKind.TechScrap => Palette.Objective,
            _ => new Color(0.66f, 0.85f, 0.35f),
        };

        public int SalvageValue
        {
            get { int v = 0; foreach (var s in slots) v += Value(s.kind) * s.count; return v; }
        }

        private void Awake()
        {
            Instance = this;
            if (OutpostStashManager.Instance != null)
                tier = Mathf.Clamp(OutpostStashManager.Instance.GetStashData().backpackTier, 0, TierSlots.Length - 1);
        }

        private void OnEnable()
        {
            if (GameManager.Instance != null) GameManager.Instance.OnGameStateChanged += HandleState;
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null) GameManager.Instance.OnGameStateChanged -= HandleState;
        }

        private void Update()
        {
            if (Deadzone.UI.HUDController.IsMenuOpen) return;
            if (Input.GetKeyDown(KeyCode.H)) UseFirst(ItemKind.Medkit);
            if (Input.GetKeyDown(KeyCode.J)) UseFirst(ItemKind.AmmoPack);
        }

        private void HandleState(GameState state)
        {
            if (state == GameState.GameOver && slots.Count > 0)
            {
                int lost = SalvageValue;
                slots.Clear();
                OnChanged?.Invoke();
                if (lost > 0) KillFeedUI.PostMessage($"✖ BACKPACK LOST — {lost} cr of salvage left behind", Palette.Danger);
            }
        }

        public bool CanAdd(ItemKind k)
        {
            foreach (var s in slots) if (s.kind == k && s.count < MaxStack(k)) return true;
            return slots.Count < Capacity;
        }

        /// <summary>Adds n items, topping up existing stacks before opening new slots. False if anything didn't fit.</summary>
        public bool TryAdd(ItemKind k, int n = 1)
        {
            int left = n;
            foreach (var s in slots)
            {
                if (left <= 0) break;
                if (s.kind != k) continue;
                int room = MaxStack(k) - s.count;
                int put = Mathf.Min(room, left);
                s.count += put; left -= put;
            }
            while (left > 0 && slots.Count < Capacity)
            {
                int put = Mathf.Min(MaxStack(k), left);
                slots.Add(new ItemStack { kind = k, count = put });
                left -= put;
            }
            if (left < n) OnChanged?.Invoke();
            return left == 0;
        }

        public bool UseFirst(ItemKind k)
        {
            for (int i = 0; i < slots.Count; i++) if (slots[i].kind == k) return Use(i);
            return false;
        }

        /// <summary>Uses one item from a slot. Salvage can't be used; consumables only if they'd help.</summary>
        public bool Use(int index)
        {
            if (index < 0 || index >= slots.Count) return false;
            var s = slots[index];
            bool used = false;
            if (s.kind == ItemKind.Medkit && TryGetComponent<Health>(out var hp) && hp.CurrentHealth < hp.MaxHealth && !hp.IsDead)
            {
                hp.Heal(MedkitHeal);
                used = true;
            }
            else if (s.kind == ItemKind.AmmoPack)
            {
                var w = GetComponentInChildren<Weapon>();
                if (w != null && w.Data != null && w.CurrentAmmo < w.Data.magazineCapacity) { w.RefillInstant(); used = true; }
            }
            if (!used) return false;
            s.count--;
            if (s.count <= 0) slots.RemoveAt(index);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayPickup();
            OnChanged?.Invoke();
            return true;
        }

        /// <summary>Converts carried salvage into run credits (called on extraction). Returns the amount.</summary>
        public int BankSalvage()
        {
            int value = SalvageValue;
            slots.RemoveAll(s => IsSalvage(s.kind));
            if (value > 0 && GameManager.Instance != null) GameManager.Instance.AddCredits(value);
            if (value > 0) KillFeedUI.PostMessage($"✔ SALVAGE SOLD: +{value} credits", Palette.Health);
            OnChanged?.Invoke();
            return value;
        }

        /// <summary>Buys the next capacity tier with run credits; the tier is saved permanently.</summary>
        public bool TryUpgrade()
        {
            if (IsMaxTier || GameManager.Instance == null) return false;
            if (!GameManager.Instance.SpendCredits(NextUpgradeCost)) return false;
            SetTier(tier + 1);
            KillFeedUI.PostMessage($"⬆ BACKPACK UPGRADED: {Capacity} slots", Palette.Objective);
            return true;
        }

        public void SetTier(int newTier)
        {
            tier = Mathf.Clamp(newTier, 0, TierSlots.Length - 1);
            if (OutpostStashManager.Instance != null)
            {
                OutpostStashManager.Instance.GetStashData().backpackTier = tier;
                OutpostStashManager.Instance.SaveStash();
            }
            OnChanged?.Invoke();
        }
    }
}
