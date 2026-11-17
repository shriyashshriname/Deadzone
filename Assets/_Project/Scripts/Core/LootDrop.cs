using UnityEngine;
using Deadzone.Core;
using Deadzone.Combat;
using Deadzone.Player;

namespace Deadzone.Core
{
    public enum PickupType
    {
        Health,
        Ammo,
        Gems
    }

    [RequireComponent(typeof(Collider))]
    public class LootDrop : MonoBehaviour
    {
        [Header("Drop Config")]
        [SerializeField] private PickupType pickupType = PickupType.Health;
        [SerializeField] private float amount = 35f;
        [SerializeField] private float bobSpeed = 3f;
        [SerializeField] private float rotateSpeed = 90f;

        private Vector3 startPos;

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

            if (pickupType == PickupType.Health)
            {
                if (other.TryGetComponent<Health>(out var hp) && hp.CurrentHealth < hp.MaxHealth)
                {
                    hp.Heal(amount);
                    collected = true;
                }
            }
            else if (pickupType == PickupType.Ammo)
            {
                var weapon = other.GetComponentInChildren<Weapon>();
                if (weapon != null)
                {
                    weapon.RefillInstant();
                    collected = true;
                }
            }
            else if (pickupType == PickupType.Gems)
            {
                if (GameManager.Instance != null) GameManager.Instance.AddCredits((int)amount);
                collected = true;
            }

            if (collected)
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayPickup();
                Destroy(gameObject);
            }
        }
    }
}
