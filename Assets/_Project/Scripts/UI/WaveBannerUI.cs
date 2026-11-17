using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Deadzone.Core;

namespace Deadzone.UI
{
    public class WaveBannerUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject bannerPanel;
        [SerializeField] private Text waveTitleText;
        [SerializeField] private Text waveSubtitleText;

        private void Awake()
        {
            if (bannerPanel != null) bannerPanel.SetActive(false);
        }

        private void Start()
        {
            if (WaveManager.Instance != null)
            {
                WaveManager.Instance.OnWaveStarted += ShowWaveStarted;
                WaveManager.Instance.OnWaveCompleted += ShowWaveCompleted;
            }
        }

        private void OnDestroy()
        {
            if (WaveManager.Instance != null)
            {
                WaveManager.Instance.OnWaveStarted -= ShowWaveStarted;
                WaveManager.Instance.OnWaveCompleted -= ShowWaveCompleted;
            }
        }

        private void ShowWaveStarted(int wave, string title)
        {
            StopAllCoroutines();
            StartCoroutine(DisplayBanner($"WAVE {wave}", string.IsNullOrEmpty(title) ? "INFECTED DETECTED" : title, 2.5f));
        }

        private void ShowWaveCompleted(int wave)
        {
            StopAllCoroutines();
            StartCoroutine(DisplayBanner($"WAVE {wave} CLEARED", "SECTOR SECURED", 2.0f));
        }

        private IEnumerator DisplayBanner(string title, string subtitle, float duration)
        {
            if (bannerPanel == null) yield break;

            if (waveTitleText != null) waveTitleText.text = title;
            if (waveSubtitleText != null) waveSubtitleText.text = subtitle;

            bannerPanel.SetActive(true);
            yield return new WaitForSeconds(duration);
            bannerPanel.SetActive(false);
        }
    }
}
