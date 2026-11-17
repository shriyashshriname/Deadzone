using UnityEngine;
using Deadzone.Camera;

namespace Deadzone.Core
{
    public class SettingsManager : MonoBehaviour
    {
        public static SettingsManager Instance { get; private set; }

        public bool ScreenShakeEnabled { get; private set; } = true;
        public bool AimAssistEnabled { get; private set; } = true;

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

        public void SetMasterVolume(float volume)
        {
            AudioListener.volume = Mathf.Clamp01(volume);
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.CurrentSave.masterVolume = volume;
                SaveManager.Instance.SaveGame();
            }
        }

        public void SetScreenShake(bool enabled)
        {
            ScreenShakeEnabled = enabled;
        }

        public void SetAimAssist(bool enabled)
        {
            AimAssistEnabled = enabled;
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.CurrentSave.aimAssist = enabled;
                SaveManager.Instance.SaveGame();
            }
        }
    }
}
