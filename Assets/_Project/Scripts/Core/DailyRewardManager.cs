using System;
using UnityEngine;

namespace Deadzone.Core
{
    public class DailyRewardManager : MonoBehaviour
    {
        public static DailyRewardManager Instance { get; private set; }

        private const string LastLoginKey = "DZ_LastLoginDate";
        private const string StreakKey = "DZ_LoginStreak";

        public int CurrentStreak { get; private set; } = 1;
        public bool CanClaimToday { get; private set; } = false;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                CheckDailyStreak();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void CheckDailyStreak()
        {
            string lastDateStr = PlayerPrefs.GetString(LastLoginKey, string.Empty);
            CurrentStreak = PlayerPrefs.GetInt(StreakKey, 1);

            if (string.IsNullOrEmpty(lastDateStr))
            {
                CanClaimToday = true;
                return;
            }

            if (DateTime.TryParse(lastDateStr, out DateTime lastDate))
            {
                TimeSpan diff = DateTime.UtcNow.Date - lastDate.Date;
                if (diff.Days == 1)
                {
                    CurrentStreak = Mathf.Min(7, CurrentStreak + 1);
                    CanClaimToday = true;
                }
                else if (diff.Days > 1)
                {
                    CurrentStreak = 1; // Reset streak
                    CanClaimToday = true;
                }
                else
                {
                    CanClaimToday = false; // Already claimed today
                }
            }
        }

        public void ClaimReward()
        {
            if (!CanClaimToday) return;

            int gemsAwarded = CurrentStreak * 15;
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.CurrentSave.totalGems += gemsAwarded;
                SaveManager.Instance.SaveGame();
            }

            PlayerPrefs.SetString(LastLoginKey, DateTime.UtcNow.ToString("o"));
            PlayerPrefs.SetInt(StreakKey, CurrentStreak);
            PlayerPrefs.Save();

            CanClaimToday = false;
            if (AudioManager.Instance != null) AudioManager.Instance.PlayPickup(1.3f);
        }
    }
}
