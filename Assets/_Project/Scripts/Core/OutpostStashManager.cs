using System;
using System.IO;
using UnityEngine;
using Deadzone.UI;

namespace Deadzone.Core
{
    [Serializable]
    public class StashSaveData
    {
        public int schemaVersion = 2; // Schema v2 splits Credits and Gems
        public int bankedCredits = 0; // Earned in-game currency
        public int premiumGems = 0;   // Purchase-only premium currency (placeholder)
        public int bankedGems = 0;    // Legacy v1 field kept for migration
        public int totalSuccessfulExtractions = 0;
        public int totalRunsAttempted = 0;
        public int lifetimeKills = 0;
        public int permanentHealthBonus = 0;
        public int permanentDamageBonus = 0;
        public int backpackTier = 0;          // M1 backpack capacity tier (0..3)
    }

    public class OutpostStashManager : MonoBehaviour
    {
        public static OutpostStashManager Instance { get; private set; }

        private StashSaveData stashData = new StashSaveData();
        private string saveFilePath;

        public int BankedCredits => stashData.bankedCredits;
        public int PremiumGems => stashData.premiumGems;
        public int BankedGems => stashData.bankedCredits; // Compatibility alias
        public int SuccessfulExtractions => stashData.totalSuccessfulExtractions;
        public int LifetimeKills => stashData.lifetimeKills;

        public void SetSaveFilePathForTesting(string path) => saveFilePath = path;
        public StashSaveData GetStashData() => stashData;
        public void SetStashData(StashSaveData data) => stashData = data;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            saveFilePath = Path.Combine(Application.persistentDataPath, "deadzone_stash.json");
            LoadStash();
        }

        public void LoadStash()
        {
            try
            {
                if (File.Exists(saveFilePath))
                {
                    string json = File.ReadAllText(saveFilePath);
                    stashData = JsonUtility.FromJson<StashSaveData>(json) ?? new StashSaveData();

                    // Migration: v1 unversioned gems to v2 Credits
                    if (stashData.schemaVersion < 2)
                    {
                        try
                        {
                            string backupPath = saveFilePath + ".v1.bak";
                            if (!File.Exists(backupPath)) File.Copy(saveFilePath, backupPath, true);
                        }
                        catch (Exception backupErr)
                        {
                            Debug.LogWarning($"[STASH] Could not write backup: {backupErr.Message}");
                        }

                        stashData.bankedCredits += stashData.bankedGems;
                        stashData.bankedGems = 0;
                        stashData.schemaVersion = 2;
                        SaveStash();
                        Debug.Log("[STASH] Migrated save data to Schema v2 (Credits & Gems Split).");
                    }
                }
                else
                {
                    stashData = new StashSaveData { schemaVersion = 2 };
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[STASH] Failed to load stash: {e.Message}");
                stashData = new StashSaveData { schemaVersion = 2 };
            }
        }

        public void SaveStash()
        {
            try
            {
                string json = JsonUtility.ToJson(stashData, true);
                File.WriteAllText(saveFilePath, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[STASH] Failed to save stash: {e.Message}");
            }
        }

        public void BankRunBounty(int extractedCredits, int kills)
        {
            int bonusCredits = Mathf.RoundToInt(extractedCredits * 1.5f);
            stashData.bankedCredits += bonusCredits;
            stashData.totalSuccessfulExtractions++;
            stashData.lifetimeKills += kills;
            SaveStash();

            KillFeedUI.PostMessage($"★ BOUNTY BANKED: +{bonusCredits} Credits to Outpost Stash (x1.5 Evac Bonus)!", new Color(0.2f, 1f, 0.4f));
        }

        public bool TryPurchasePermanentPerk(string perkName, int cost)
        {
            if (stashData.bankedCredits < cost) return false;

            stashData.bankedCredits -= cost;
            if (perkName == "Health") stashData.permanentHealthBonus += 20;
            else if (perkName == "Damage") stashData.permanentDamageBonus += 10;
            SaveStash();
            return true;
        }

        public void RecordRunAttempt()
        {
            stashData.totalRunsAttempted++;
            SaveStash();
        }
    }
}
