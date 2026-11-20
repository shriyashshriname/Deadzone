using System;
using UnityEngine;
using Deadzone.UI;

namespace Deadzone.Core
{
    [RequireComponent(typeof(Collider))]
    public class ExtractionZone : MonoBehaviour
    {
        [Header("Extraction Settings")]
        [SerializeField] private float requiredHoldTime = 5.0f;
        [SerializeField] private bool isActiveOnStart = false;

        private float currentHoldTime = 0f;
        private bool isPlayerInside = false;
        private bool isExtracted = false;
        private Light evacSpotlight;

        public event Action<float, float> OnExtractionProgress;
        public event Action OnExtractionCompleted;
        public event Action OnZoneActivated;

        private static ExtractionZone instance;
        public static ExtractionZone Instance
        {
            get => instance != null ? instance : (instance = FindAnyObjectByType<ExtractionZone>());
            set => instance = value;
        }

        public bool IsActive { get; private set; }
        public float CurrentHoldTime => currentHoldTime;
        public float RequiredHoldTime => requiredHoldTime;
        public bool IsPlayerInside => isPlayerInside;
        public bool IsExtracted => isExtracted;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            if (TryGetComponent<Collider>(out var col)) col.isTrigger = true;
            SetupEvacLight();
            if (isActiveOnStart) ActivateZone();
        }

        private void SetupEvacLight()
        {
            GameObject lightGo = new GameObject("EvacSpotlight");
            lightGo.transform.SetParent(transform);
            lightGo.transform.localPosition = new Vector3(0, 4.5f, 0);
            evacSpotlight = lightGo.AddComponent<Light>();
            evacSpotlight.type = LightType.Point;
            evacSpotlight.color = new Color(0.2f, 1f, 0.4f);
            evacSpotlight.range = 16f;
            evacSpotlight.intensity = 0f;
        }

        public void ActivateZone()
        {
            if (IsActive) return;
            IsActive = true;
            if (evacSpotlight != null) evacSpotlight.intensity = 4.5f;

            KillFeedUI.PostMessage("🚁 EVAC CHOPPER TOUCHDOWN! Evacuate at Central Helipad!", new Color(0.2f, 1f, 0.4f));
            if (AudioManager.Instance != null) AudioManager.Instance.PlayReload(1.5f);

            OnZoneActivated?.Invoke();
        }

        public void ResetZone()
        {
            IsActive = false;
            currentHoldTime = 0f;
            isExtracted = false;
            if (evacSpotlight != null) evacSpotlight.intensity = 0f;
        }

        private void Update()
        {
            if (!IsActive || isExtracted) return;

            if (isPlayerInside)
            {
                currentHoldTime += Time.deltaTime;
                OnExtractionProgress?.Invoke(currentHoldTime, requiredHoldTime);

                if (currentHoldTime >= requiredHoldTime)
                {
                    ExecuteExtraction();
                }
            }
            else if (currentHoldTime > 0f)
            {
                currentHoldTime = Mathf.Max(0f, currentHoldTime - (Time.deltaTime * 1.5f));
                OnExtractionProgress?.Invoke(currentHoldTime, requiredHoldTime);
            }
        }

        private void ExecuteExtraction()
        {
            isExtracted = true;
            OnExtractionCompleted?.Invoke();
            // Let the dropship board the player and fly off before the debrief appears.
            if (DropshipController.Instance != null) DropshipController.Instance.Depart(FinishExtraction);
            else FinishExtraction();
        }

        private void FinishExtraction()
        {

            int credits = GameManager.Instance != null ? GameManager.Instance.CreditsEarned : 0;
            int kills = GameManager.Instance != null ? GameManager.Instance.KillCount : 0;

            if (OutpostStashManager.Instance != null)
            {
                OutpostStashManager.Instance.BankRunBounty(credits, kills);
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.TriggerVictory();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player")) isPlayerInside = true;
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player")) isPlayerInside = false;
        }
    }
}
