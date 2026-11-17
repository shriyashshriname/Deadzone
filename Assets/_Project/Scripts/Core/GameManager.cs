using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Deadzone.Core
{
    public enum GameState
    {
        Playing,
        GameOver,
        Victory
    }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameState CurrentState { get; private set; } = GameState.Playing;
        public int KillCount { get; private set; } = 0;
        public int CreditsEarned { get; private set; } = 0;
        public int GemCount => CreditsEarned; // Compatibility alias

        public event Action<GameState> OnGameStateChanged;
        public event Action<int> OnKillCountChanged;
        public event Action<int> OnCreditsChanged;
        public event Action<int> OnGemCountChanged;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                if (AtmosphereController.Instance == null)
                {
                    GameObject atmo = new GameObject("AtmosphereController");
                    atmo.AddComponent<AtmosphereController>();
                }
            }
            else Destroy(gameObject);
        }

        public void AddKill()
        {
            KillCount++;
            OnKillCountChanged?.Invoke(KillCount);
        }

        public void AddCredits(int amount)
        {
            CreditsEarned += amount;
            OnCreditsChanged?.Invoke(CreditsEarned);
            OnGemCountChanged?.Invoke(CreditsEarned);
        }

        public void AddGems(int amount) => AddCredits(amount);

        public bool SpendCredits(int amount)
        {
            if (CreditsEarned < amount) return false;
            CreditsEarned -= amount;
            OnCreditsChanged?.Invoke(CreditsEarned);
            OnGemCountChanged?.Invoke(CreditsEarned);
            return true;
        }

        public bool SpendGems(int amount) => SpendCredits(amount);

        public void TriggerGameOver()
        {
            if (CurrentState != GameState.Playing) return;

            CurrentState = GameState.GameOver;
            if (RunTelemetryLogger.Instance != null) RunTelemetryLogger.Instance.EndRun("KIA", KillCount, CreditsEarned);
            OnGameStateChanged?.Invoke(CurrentState);
        }

        public void TriggerVictory()
        {
            if (CurrentState != GameState.Playing) return;

            CurrentState = GameState.Victory;
            if (RunTelemetryLogger.Instance != null) RunTelemetryLogger.Instance.EndRun("EXTRACTED", KillCount, CreditsEarned);
            OnGameStateChanged?.Invoke(CurrentState);
        }

        public void RestartGame()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
