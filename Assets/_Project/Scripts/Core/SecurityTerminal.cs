using System;
using UnityEngine;

namespace Deadzone.Core
{
    [RequireComponent(typeof(Collider))]
    public class SecurityTerminal : MonoBehaviour
    {
        [Header("Terminal Settings")]
        [SerializeField] private DoorController linkedDoor;
        [SerializeField] private float overrideDuration = 2.0f;

        private float currentProgress = 0f;
        private bool isPlayerNearby = false;
        private bool isOverridden = false;

        public event Action<float, float> OnOverrideProgress;
        public event Action OnTerminalUnlocked;

        public bool IsOverridden => isOverridden;

        private void Start()
        {
            if (TryGetComponent<Collider>(out var col)) col.isTrigger = true;
        }

        private void Update()
        {
            if (isOverridden) return;

            if (isPlayerNearby)
            {
                currentProgress += Time.deltaTime;
                OnOverrideProgress?.Invoke(currentProgress, overrideDuration);

                if (currentProgress >= overrideDuration)
                {
                    UnlockTerminal();
                }
            }
            else if (currentProgress > 0f)
            {
                currentProgress = Mathf.Max(0f, currentProgress - Time.deltaTime);
                OnOverrideProgress?.Invoke(currentProgress, overrideDuration);
            }
        }

        private void UnlockTerminal()
        {
            isOverridden = true;
            OnTerminalUnlocked?.Invoke();

            if (linkedDoor != null) linkedDoor.OpenDoor();
            if (AudioManager.Instance != null) AudioManager.Instance.PlayPickup(1.3f);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player")) isPlayerNearby = true;
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player")) isPlayerNearby = false;
        }
    }
}
