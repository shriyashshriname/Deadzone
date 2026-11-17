using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace Deadzone.Core
{
    [Serializable]
    public class PlayerSaveData
    {
        public int highScoreKills = 0;
        public int totalRuns = 0;
        public int totalGems = 0;
        public List<string> unlockedWeapons = new List<string> { "Assault Rifle" };
        public float masterVolume = 1f;
        public float sfxVolume = 1f;
        public bool aimAssist = true;
    }

    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }

        public PlayerSaveData CurrentSave { get; private set; } = new PlayerSaveData();

        private string saveFilePath;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                saveFilePath = Path.Combine(Application.persistentDataPath, "deadzone_save.json");
                LoadGame();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void SaveGame()
        {
            try
            {
                string json = JsonUtility.ToJson(CurrentSave, true);
                File.WriteAllText(saveFilePath, json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Failed to save game: {ex.Message}");
            }
        }

        public void LoadGame()
        {
            try
            {
                if (File.Exists(saveFilePath))
                {
                    string json = File.ReadAllText(saveFilePath);
                    CurrentSave = JsonUtility.FromJson<PlayerSaveData>(json);
                }
                else
                {
                    CurrentSave = new PlayerSaveData();
                    SaveGame();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Failed to load save, using defaults: {ex.Message}");
                CurrentSave = new PlayerSaveData();
            }
        }

        public void RegisterRunScore(int kills, int gems)
        {
            CurrentSave.totalRuns++;
            CurrentSave.totalGems += gems;
            if (kills > CurrentSave.highScoreKills)
            {
                CurrentSave.highScoreKills = kills;
            }
            SaveGame();
        }
    }
}
