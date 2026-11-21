using System;
using UnityEngine;
using Deadzone.Player;
using Deadzone.UI;

namespace Deadzone.Core
{
    public class PowerSubstation : MonoBehaviour
    {
        [Header("Substation Info")]
        public string stationName = "Substation Alpha";
        public string sectorName = "Sector B";
        [SerializeField] private float rebootTimeRequired = 3.0f;
        [SerializeField] private float interactRadius = 4.0f;

        private float currentProgress = 0f;
        private bool isOnline = false;
        private Light stationLight;
        private Transform playerTransform;

        public static event Action<PowerSubstation> OnSubstationRebooted;
        public static int TotalSubstationsOnline { get; private set; } = 0;
        public static void ResetGridCount() => TotalSubstationsOnline = 0;
        public static int TotalSubstationsCount { get; private set; } = 0;

        public bool IsOnline => isOnline;
        public float RebootProgressRatio => Mathf.Clamp01(currentProgress / rebootTimeRequired);
        public bool IsPlayerInRange { get; private set; }

        private void Awake()
        {
            TotalSubstationsCount++;
        }

        private void Start()
        {
            SetupLighting();
            if (PlayerController.Instance != null) playerTransform = PlayerController.Instance.transform;
        }

        private void SetupLighting()
        {
            GameObject lightGo = new GameObject("StationLight");
            lightGo.transform.SetParent(transform);
            lightGo.transform.localPosition = new Vector3(0, 2.2f, 0);
            stationLight = lightGo.AddComponent<Light>();
            stationLight.type = LightType.Point;
            stationLight.color = new Color(1.0f, 0.25f, 0.25f); // Red = Offline
            stationLight.range = 10f;
            stationLight.intensity = 2.5f;
        }

        private void Update()
        {
            if (isOnline) return;

            if (!playerTransform && PlayerController.Instance != null) playerTransform = PlayerController.Instance.transform;

            if (playerTransform != null)
            {
                float dist = Vector3.Distance(transform.position, playerTransform.position);
                IsPlayerInRange = dist <= interactRadius;

                bool isInteracting = Input.GetKey(KeyCode.E) || (PlayerController.Instance != null && PlayerController.Instance.CurrentCommand.interact);

                if (IsPlayerInRange && isInteracting)
                {
                    currentProgress += Time.deltaTime;
                    if (stationLight != null) stationLight.intensity = 2f + Mathf.PingPong(Time.time * 6f, 2.5f);

                    if (currentProgress >= rebootTimeRequired)
                    {
                        RebootStation();
                    }
                }
                else
                {
                    currentProgress = Mathf.Max(0f, currentProgress - Time.deltaTime * 1.5f);
                    if (stationLight != null) stationLight.intensity = 2.5f;
                }
            }
        }

        public void RebootStation()
        {
            if (isOnline) return;
            isOnline = true;
            currentProgress = rebootTimeRequired;
            TotalSubstationsOnline++;

            if (stationLight != null)
            {
                stationLight.color = new Color(0.2f, 1.0f, 0.4f); // Green = Online
                stationLight.intensity = 4.0f;
            }

            KillFeedUI.PostMessage($"✔ POWER RESTORED: [{stationName}] in {sectorName} ONLINE!", new Color(0.2f, 1f, 0.5f));
            if (AudioManager.Instance != null) AudioManager.Instance.PlayReload(1.4f);

            if (TotalSubstationsOnline >= 2 && ExtractionZone.Instance != null)
            {
                ExtractionZone.Instance.ActivateZone();
            }

            if (RunTelemetryLogger.Instance != null)
            {
                float hp = PlayerController.Instance != null ? PlayerController.Instance.GetComponent<Health>().CurrentHealth : 100f;
                RunTelemetryLogger.Instance.RecordObjectiveCheckpoint($"Substation_{stationName}", hp);
            }

            OnSubstationRebooted?.Invoke(this);
        }

        private void OnGUI()
        {
            GUI.matrix = Matrix4x4.identity; // raw screen pixels (world-anchored / safe-area aware)
            if (isOnline || !IsPlayerInRange) return;

            float w = 340, h = 50;
            float x = (Screen.width - w) * 0.5f;
            float y = Screen.height * 0.65f;

            GUI.Box(new Rect(x, y, w, h), "");
            GUI.color = new Color(0.2f, 0.9f, 1f);
            string prompt = currentProgress > 0f
                ? $"REBOOTING {stationName.ToUpper()}... ({RebootProgressRatio * 100:F0}%)"
                : $"HOLD [E] TO REBOOT {stationName.ToUpper()}";
            GUI.Label(new Rect(x + 10, y + 6, w - 20, 20), prompt);

            // Progress bar
            GUI.color = new Color(0.2f, 1.0f, 0.4f);
            GUI.Box(new Rect(x + 10, y + 30, (w - 20) * RebootProgressRatio, 12), "");
            GUI.color = Color.white;
        }

        private void OnDestroy()
        {
            TotalSubstationsCount = Mathf.Max(0, TotalSubstationsCount - 1);
            if (isOnline) TotalSubstationsOnline = Mathf.Max(0, TotalSubstationsOnline - 1);
        }
    }
}
