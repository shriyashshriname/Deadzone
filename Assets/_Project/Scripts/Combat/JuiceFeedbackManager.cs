using System.Collections;
using UnityEngine;
using Deadzone.Camera;

namespace Deadzone.Combat
{
    /// <summary>
    /// Implements juice & game-feel feedback: screen shake, hit-stop micro-pauses,
    /// and combat impact timing. Follows .agents/skills/juice-game-feel/.
    /// </summary>
    public class JuiceFeedbackManager : MonoBehaviour
    {
        public static JuiceFeedbackManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private bool enableJuice = true;
        [SerializeField] private bool enableHitStop = true;
        [SerializeField] private bool enableCameraTrauma = true;

        private Coroutine activeHitStop;

        public bool EnableJuice { get => enableJuice; set => enableJuice = value; }
        public bool EnableHitStop { get => enableHitStop; set => enableHitStop = value; }
        public bool EnableCameraTrauma { get => enableCameraTrauma; set => enableCameraTrauma = value; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void TriggerHeavyHitFeedback(float damage)
        {
            if (!enableJuice) return;

            // Heavy damage threshold (>= 60 dmg, e.g. Sniper, Grenade, Barrel)
            if (damage >= 60f)
            {
                if (enableHitStop)
                {
                    PlayHitStop(0.04f);
                }
                if (enableCameraTrauma && TopDownCamera.Instance != null)
                {
                    TopDownCamera.Instance.TriggerShake(0.35f, 0.18f);
                }
            }
            else if (damage >= 35f)
            {
                if (enableCameraTrauma && TopDownCamera.Instance != null)
                {
                    TopDownCamera.Instance.TriggerShake(0.18f, 0.12f);
                }
            }
        }

        public void PlayHitStop(float duration = 0.04f)
        {
            if (!enableHitStop) return;
            if (activeHitStop != null) StopCoroutine(activeHitStop);
            activeHitStop = StartCoroutine(HitStopRoutine(duration));
        }

        private IEnumerator HitStopRoutine(float duration)
        {
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(duration);
            Time.timeScale = 1f;
            activeHitStop = null;
        }
    }
}
