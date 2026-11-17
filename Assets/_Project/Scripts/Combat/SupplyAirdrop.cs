using System;
using UnityEngine;
using Deadzone.Core;
using Deadzone.Player;

namespace Deadzone.Combat
{
    public class SupplyAirdrop : MonoBehaviour
    {
        [Header("Airdrop Config")]
        [SerializeField] private float requiredHoldTime = 3.0f;
        [SerializeField] private float interactRadius = 4.0f;
        [SerializeField] private GameObject gemPickupPrefab;
        [SerializeField] private GameObject healthPickupPrefab;
        [SerializeField] private GameObject ammoPickupPrefab;

        private float currentHoldProgress = 0f;
        private bool isClaimed = false;
        private Light beaconLight;
        private Transform playerTransform;

        public static event Action OnAirdropClaimed;
        public float HoldProgressRatio => Mathf.Clamp01(currentHoldProgress / requiredHoldTime);
        public bool IsClaimed => isClaimed;
        public bool IsPlayerInRange { get; private set; }

        private void Start()
        {
            if (PlayerController.Instance != null)
            {
                playerTransform = PlayerController.Instance.transform;
            }

            SetupBeacon();
        }

        private void SetupBeacon()
        {
            GameObject lightGo = new GameObject("AirdropBeaconLight");
            lightGo.transform.SetParent(transform);
            lightGo.transform.localPosition = new Vector3(0, 2.5f, 0);
            beaconLight = lightGo.AddComponent<Light>();
            beaconLight.type = LightType.Point;
            beaconLight.color = new Color(1.0f, 0.85f, 0.1f);
            beaconLight.range = 14f;
            beaconLight.intensity = 3.0f;
        }

        private void Update()
        {
            if (isClaimed) return;

            // Pulse beacon light
            if (beaconLight != null)
            {
                beaconLight.intensity = 2.0f + Mathf.PingPong(Time.time * 2.5f, 2.0f);
            }

            if (!playerTransform && PlayerController.Instance != null)
            {
                playerTransform = PlayerController.Instance.transform;
            }

            if (playerTransform != null)
            {
                float dist = Vector3.Distance(transform.position, playerTransform.position);
                IsPlayerInRange = dist <= interactRadius;

                bool isInteracting = Input.GetKey(KeyCode.E) || (PlayerController.Instance != null && PlayerController.Instance.CurrentCommand.interact);

                if (IsPlayerInRange && isInteracting)
                {
                    currentHoldProgress += Time.deltaTime;
                    if (currentHoldProgress >= requiredHoldTime)
                    {
                        ClaimAirdrop();
                    }
                }
                else
                {
                    currentHoldProgress = Mathf.Max(0f, currentHoldProgress - Time.deltaTime * 1.5f);
                }
            }
        }

        public void ClaimAirdrop()
        {
            if (isClaimed) return;
            isClaimed = true;
            currentHoldProgress = requiredHoldTime;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddCredits(50);
            }

            if (playerTransform != null && playerTransform.TryGetComponent<Health>(out var hp))
            {
                hp.Heal(100f);
            }

            SpawnLootRewards();

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayReload(1.3f);
            }

            OnAirdropClaimed?.Invoke();

            if (beaconLight != null)
            {
                beaconLight.color = new Color(0.2f, 1.0f, 0.4f);
                beaconLight.intensity = 5.0f;
            }

            Destroy(gameObject, 4.0f);
        }

        private void SpawnLootRewards()
        {
            Vector3 center = transform.position;
            if (gemPickupPrefab != null)
            {
                Instantiate(gemPickupPrefab, center + new Vector3(1.2f, 0.5f, 0), Quaternion.identity);
                Instantiate(gemPickupPrefab, center + new Vector3(-1.2f, 0.5f, 0), Quaternion.identity);
            }
            if (healthPickupPrefab != null)
            {
                Instantiate(healthPickupPrefab, center + new Vector3(0, 0.5f, 1.2f), Quaternion.identity);
            }
            if (ammoPickupPrefab != null)
            {
                Instantiate(ammoPickupPrefab, center + new Vector3(0, 0.5f, -1.2f), Quaternion.identity);
            }
        }

        private void OnGUI()
        {
            if (isClaimed || !IsPlayerInRange) return;

            float w = 320, h = 48;
            float x = (Screen.width - w) * 0.5f;
            float y = Screen.height * 0.70f;

            GUI.Box(new Rect(x, y, w, h), "");
            GUI.color = new Color(1.0f, 0.85f, 0.1f);
            string prompt = currentHoldProgress > 0f 
                ? $"OPENING SUPPLY CRATE... ({HoldProgressRatio * 100:F0}%)" 
                : "HOLD [E] TO CLAIM SUPPLY CRATE";
            GUI.Label(new Rect(x + 10, y + 6, w - 20, 20), prompt);

            // Progress bar
            GUI.color = new Color(0.2f, 0.9f, 1.0f);
            GUI.Box(new Rect(x + 10, y + 28, (w - 20) * HoldProgressRatio, 12), "");
            GUI.color = Color.white;
        }
    }
}
