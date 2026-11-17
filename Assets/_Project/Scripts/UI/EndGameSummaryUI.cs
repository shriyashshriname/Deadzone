using UnityEngine;
using UnityEngine.UI;
using Deadzone.Core;

namespace Deadzone.UI
{
    public class EndGameSummaryUI : MonoBehaviour
    {
        [Header("Summary Window")]
        [SerializeField] private GameObject windowContainer;
        [SerializeField] private Text statusTitleText;
        [SerializeField] private Text killsText;
        [SerializeField] private Text wavesText;
        [SerializeField] private Text gemsText;
        [SerializeField] private Text highScoreText;
        [SerializeField] private Button restartButton;

        private void Awake()
        {
            if (windowContainer != null) windowContainer.SetActive(false);
            if (restartButton != null) restartButton.onClick.AddListener(OnRestartClicked);
        }

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged += HandleGameState;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= HandleGameState;
            }
        }

        private void HandleGameState(GameState state)
        {
            if (state == GameState.Playing) return;

            ShowSummary(state == GameState.Victory);
        }

        private void ShowSummary(bool isVictory)
        {
            if (windowContainer != null) windowContainer.SetActive(true);

            int kills = GameManager.Instance != null ? GameManager.Instance.KillCount : 0;
            int wave = WaveManager.Instance != null ? WaveManager.Instance.CurrentWaveNumber : 1;
            int gems = kills * 2; // Run reward formula

            if (statusTitleText != null)
            {
                statusTitleText.text = isVictory ? "MISSION ACCOMPLISHED" : "K.I.A. - MISSION FAILED";
                statusTitleText.color = isVictory ? Color.green : Color.red;
            }

            if (killsText != null) killsText.text = $"Total Kills: {kills}";
            if (wavesText != null) wavesText.text = $"Waves Reached: {wave}";
            if (gemsText != null) gemsText.text = $"Gems Collected: +{gems}";

            // Register into persistent save
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.RegisterRunScore(kills, gems);
                if (highScoreText != null)
                {
                    highScoreText.text = $"High Score: {SaveManager.Instance.CurrentSave.highScoreKills} Kills";
                }
            }
        }

        private void OnRestartClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RestartGame();
            }
        }
    }
}
