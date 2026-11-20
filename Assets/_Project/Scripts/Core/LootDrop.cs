using UnityEngine;
using Deadzone.Core;
using Deadzone.Combat;
using Deadzone.Player;
using Deadzone.UI;

namespace Deadzone.Core
{
    public enum PickupType
    {
        Health,
        Ammo,
        Gems,
        Salvage
    }

    [RequireComponent(typeof(Collider))]
    public class LootDrop : MonoBehaviour
    {
        [Header("Drop Config")]
        [SerializeField] private PickupType pickupType = PickupType.Health;
        [SerializeField] private float amount = 35f;
        [SerializeField] private float bobSpeed = 3f;
        [SerializeField] private float rotateSpeed = 90f;
        [SerializeField] private ItemKind salvageKind = ItemKind.BioSample;

        /// <summary>Turns this drop into another kind at runtime (used for salvage spawned from the gem prefab).</summary>
        public void Configure(PickupType type, float value, ItemKind kind)
        {
            pickupType = type; amount = value; salvageKind = kind;
            var block = new MaterialPropertyBlock();
            Color c = Backpack.ColorOf(kind);
            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                r.GetPropertyBlock(block);
                block.SetColor("_BaseColor", c);
                block.SetColor("_EmissionColor", c * 1.5f);
                r.SetPropertyBlock(block);
            }
        }

        private Vector3 startPos;
        private static float nextFullWarning;

        private void Start()
        {
            startPos = transform.position;
            if (TryGetComponent<Collider>(out var col)) col.isTrigger = true;
        }

        private void Update()
        {
            // Floating animation
            float newY = startPos.y + Mathf.Sin(Time.time * bobSpeed) * 0.2f;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
            transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            bool collected = false;
            var bag = other.GetComponent<Backpack>();
            if (pickupType == PickupType.Health)
            {
                // Badly hurt: heal on the spot. Otherwise stash a medkit; full bag + hurt: heal anyway.
                if (other.TryGetComponent<Health>(out var hp))
                {
                    bool hurt = hp.CurrentHealth < hp.MaxHealth;
                    if (hurt && hp.CurrentHealth < hp.MaxHealth * 0.6f) { hp.Heal(amount); collected = true; }
                    else if (bag != null && bag.TryAdd(ItemKind.Medkit)) { collected = true; KillFeedUI.PostMessage("+ MEDKIT stored", Palette.Health); }
                    else if (hurt) { hp.Heal(amount); collected = true; }
                }
            }
            else if (pickupType == PickupType.Ammo)
            {
                var weapon = other.GetComponentInChildren<Weapon>();
                bool low = weapon != null && weapon.Data != null && weapon.CurrentAmmo < weapon.Data.magazineCapacity * 0.35f;
                if (low) { weapon.RefillInstant(); collected = true; }
                else if (bag != null && bag.TryAdd(ItemKind.AmmoPack)) { collected = true; KillFeedUI.PostMessage("+ AMMO PACK stored", Palette.Ammo); }
                else if (weapon != null) { weapon.RefillInstant(); collected = true; }
            }
            else if (pickupType == PickupType.Gems)
            {
                if (GameManager.Instance != null) GameManager.Instance.AddCredits((int)amount);
                collected = true;
            }
            else if (pickupType == PickupType.Salvage)
            {
                if (bag != null && bag.TryAdd(salvageKind)) collected = true;
                else if (Time.time > nextFullWarning) { nextFullWarning = Time.time + 3f; KillFeedUI.PostMessage("BACKPACK FULL — upgrade capacity in the shop", Palette.Danger); }
            }
            if (collected)
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayPickup();
                Destroy(gameObject);
            }
        }
    }
}
