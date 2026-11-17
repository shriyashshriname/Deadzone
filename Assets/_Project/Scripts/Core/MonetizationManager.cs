using System;
using UnityEngine;
using Deadzone.Player;

namespace Deadzone.Core
{
    public class MonetizationManager : MonoBehaviour
    {
        public static MonetizationManager Instance { get; private set; }

        public event Action<bool> OnRewardedAdFinished;
        public event Action<int> OnGemsPurchased;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void ShowRewardedVideo(Action onComplete)
        {
            // Simulates Google AdMob Rewarded Ad lifecycle
            Debug.Log("[MonetizationManager] Showing Rewarded Video Ad...");
            onComplete?.Invoke();
            OnRewardedAdFinished?.Invoke(true);
        }

        public void TriggerReviveAd()
        {
            ShowRewardedVideo(() =>
            {
                if (PlayerController.Instance != null)
                {
                    var hp = PlayerController.Instance.GetComponent<Health>();
                    if (hp != null)
                    {
                        hp.ResetHealth();
                        PlayerController.Instance.enabled = true;
                    }
                }
            });
        }

        public void PurchaseGemPack(int gemCount)
        {
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.CurrentSave.totalGems += gemCount;
                SaveManager.Instance.SaveGame();
            }

            OnGemsPurchased?.Invoke(gemCount);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayPickup(1.5f);
        }
    }
}
