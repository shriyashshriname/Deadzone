using System;
using UnityEngine;
using Deadzone.Player;
using Deadzone.Enemies;

namespace Deadzone.Core
{
    public class VaultLootController : MonoBehaviour
    {
        [Header("Vault Configuration")]
        [SerializeField] private float unlockDuration = 20f;
        [SerializeField] private float interactRadius = 3.5f;
        [SerializeField] private int gemReward = 150;
        [SerializeField] private GameObject lootDropPrefab;

        private bool isUnlocking;
        private bool isOpened;
        private float unlockTimer;
        private Transform playerTransform;

        public event Action<float, float> OnVaultProgressUpdated; // pct (0-1), remaining seconds
        public event Action OnVaultBreachStarted;
        public event Action OnVaultOpened;

        public bool IsOpened => isOpened;
        public bool IsUnlocking => isUnlocking;

        private void Start()
        {
            if (PlayerController.Instance != null)
            {
                playerTransform = PlayerController.Instance.transform;
            }
        }

        private void Update()
        {
            if (isOpened) return;

            if (!playerTransform && PlayerController.Instance != null)
            {
                playerTransform = PlayerController.Instance.transform;
            }

            if (!isUnlocking)
            {
                // Check if player is near to trigger breach
                if (playerTransform && Vector3.Distance(transform.position, playerTransform.position) <= interactRadius)
                {
                    StartBreach();
                }
            }
            else
            {
                unlockTimer -= Time.deltaTime;
                float progress = Mathf.Clamp01(1f - (unlockTimer / unlockDuration));
                OnVaultProgressUpdated?.Invoke(progress, unlockTimer);

                // Periodic alarm noise every 5 seconds to draw swarm
                if (Mathf.FloorToInt(unlockTimer) % 5 == 0 && InfectedSwarmDirector.Instance != null)
                {
                    InfectedSwarmDirector.Instance.AlertEnemiesInRadius(transform.position, 35f);
                }

                if (unlockTimer <= 0f)
                {
                    CompleteBreach();
                }
            }
        }

        public void StartBreach()
        {
            if (isUnlocking || isOpened) return;

            isUnlocking = true;
            unlockTimer = unlockDuration;
            OnVaultBreachStarted?.Invoke();

            // Broadcast immediate security alarm noise
            if (InfectedSwarmDirector.Instance != null)
            {
                InfectedSwarmDirector.Instance.AlertEnemiesInRadius(transform.position, 40f);
            }

            Debug.Log("[VAULT] Security breach initiated! Defend the perimeter for 20 seconds!");
        }

        private void CompleteBreach()
        {
            isUnlocking = false;
            isOpened = true;

            // Grant bonus gems to save
            if (SaveManager.Instance != null && SaveManager.Instance.CurrentSave != null)
            {
                SaveManager.Instance.CurrentSave.totalGems += gemReward;
                SaveManager.Instance.SaveGame();
            }

            // Spawn loot drop
            if (lootDropPrefab != null)
            {
                Instantiate(lootDropPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
            }

            OnVaultOpened?.Invoke();
            Debug.Log($"[VAULT] High-Security Vault Opened! {gemReward} Gems & Loot granted!");
        }
    }
}
