using UnityEngine;
using UnityEngine.UI;
using Deadzone.Player;
using Deadzone.Combat;
using Deadzone.Core;

namespace Deadzone.UI
{
    public class MobileControlsManager : MonoBehaviour
    {
        [Header("Touch Joysticks")]
        [SerializeField] private VirtualJoystick moveJoystick;
        [SerializeField] private VirtualJoystick aimJoystick;

        [Header("Action Buttons")]
        [SerializeField] private Button fireButton;
        [SerializeField] private Button dashButton;
        [SerializeField] private Button reloadButton;
        [SerializeField] private Button swapWeaponButton;
        [SerializeField] private Button contextInteractButton;

        [Header("Settings & Shooting Modes (B4)")]
        [SerializeField] private TouchShootingMode shootingMode = TouchShootingMode.ModeA_PushToFire;
        [SerializeField] private bool enableAimAssist = true;
        [SerializeField] private bool forceMobileUIInEditor = false;

        private PlayerController player;
        private MobileTouchInputProvider touchProvider;
        private bool isContextHeld = false;
        private float currentInteractionProgress = 0f;
        private bool hasInteractableTarget = false;

        public TouchShootingMode CurrentMode => shootingMode;

        private void Start()
        {
            bool isMobile = Application.isMobilePlatform || forceMobileUIInEditor;
            gameObject.SetActive(isMobile);

            if (PlayerController.Instance != null)
            {
                player = PlayerController.Instance;
                touchProvider = new MobileTouchInputProvider(player.transform)
                {
                    ShootingMode = shootingMode,
                    EnableAimAssist = enableAimAssist
                };
                player.SetInputProvider(touchProvider);
            }

            if (dashButton != null) dashButton.onClick.AddListener(() => { if (touchProvider != null) touchProvider.DashTriggered = true; });
            if (reloadButton != null) reloadButton.onClick.AddListener(() => { if (touchProvider != null) touchProvider.ReloadTriggered = true; });
            if (swapWeaponButton != null) swapWeaponButton.onClick.AddListener(() => { if (touchProvider != null) touchProvider.SwitchWeaponTriggered = true; });

            UpdateModeUI();
        }

        public void SetShootingMode(TouchShootingMode mode)
        {
            shootingMode = mode;
            if (touchProvider != null) touchProvider.ShootingMode = mode;
            UpdateModeUI();
        }

        private void UpdateModeUI()
        {
            // In Mode B, explicit fire button is displayed
            if (fireButton != null)
            {
                fireButton.gameObject.SetActive(shootingMode == TouchShootingMode.ModeB_AimAndFire);
            }
        }

        private void Update()
        {
            if (player == null && PlayerController.Instance != null)
            {
                player = PlayerController.Instance;
                touchProvider = new MobileTouchInputProvider(player.transform)
                {
                    ShootingMode = shootingMode,
                    EnableAimAssist = enableAimAssist
                };
                player.SetInputProvider(touchProvider);
            }

            if (touchProvider == null) return;

            // Stream joystick values to provider
            if (moveJoystick != null) touchProvider.MoveJoystickVector = moveJoystick.InputDirection;
            if (aimJoystick != null)
            {
                touchProvider.AimJoystickVector = aimJoystick.InputDirection;
                touchProvider.IsAimPressed = aimJoystick.IsPressed;
            }

            // Poll context interaction targets (B5)
            PollContextInteraction();
            touchProvider.ContextInteractHeld = isContextHeld;
        }

        private void PollContextInteraction()
        {
            hasInteractableTarget = false;
            currentInteractionProgress = 0f;

            if (player == null) return;

            // 1. Check DBNO Revive
            if (PlayerRevive.Instance != null && PlayerRevive.Instance.IsDowned)
            {
                hasInteractableTarget = true;
                currentInteractionProgress = PlayerRevive.Instance.ReviveProgressRatio;
            }

            // 2. Check Power Substations
            if (!hasInteractableTarget)
            {
                var stations = FindObjectsByType<PowerSubstation>(FindObjectsSortMode.None);
                for (int i = 0; i < stations.Length; i++)
                {
                    if (stations[i].IsPlayerInRange && !stations[i].IsOnline)
                    {
                        hasInteractableTarget = true;
                        currentInteractionProgress = stations[i].RebootProgressRatio;
                        break;
                    }
                }
            }

            // 3. Check Supply Airdrops
            if (!hasInteractableTarget)
            {
                var airdrops = FindObjectsByType<SupplyAirdrop>(FindObjectsSortMode.None);
                for (int i = 0; i < airdrops.Length; i++)
                {
                    if (airdrops[i].IsPlayerInRange && !airdrops[i].IsClaimed)
                    {
                        hasInteractableTarget = true;
                        currentInteractionProgress = airdrops[i].HoldProgressRatio;
                        break;
                    }
                }
            }

            // 4. Check Extraction Zone
            if (!hasInteractableTarget && ExtractionZone.Instance != null && ExtractionZone.Instance.IsActive)
            {
                if (ExtractionZone.Instance.IsPlayerInside && !ExtractionZone.Instance.IsExtracted)
                {
                    hasInteractableTarget = true;
                    currentInteractionProgress = Mathf.Clamp01(ExtractionZone.Instance.CurrentHoldTime / ExtractionZone.Instance.RequiredHoldTime);
                }
            }

            if (contextInteractButton != null)
            {
                contextInteractButton.gameObject.SetActive(hasInteractableTarget);
            }
        }

        // GUI Fallback for Context Interact Button with Progress Ring (B5)
        private void OnGUI()
        {
            if (!hasInteractableTarget) return;

            float size = 80f;
            float x = Screen.width - size - 40f;
            float y = Screen.height * 0.52f;
            Rect btnRect = new Rect(x, y, size, size);

            Event e = Event.current;
            if (e.type == EventType.MouseDown && btnRect.Contains(e.mousePosition))
            {
                isContextHeld = true;
            }
            else if (e.type == EventType.MouseUp)
            {
                isContextHeld = false;
            }

            // Render Context Action Disk & Progress Ring
            GUI.color = isContextHeld ? new Color(0.2f, 1f, 0.4f, 0.85f) : new Color(0.1f, 0.6f, 1f, 0.7f);
            GUI.Box(btnRect, "");

            // Progress Ring Fill indicator
            if (currentInteractionProgress > 0f)
            {
                GUI.color = Color.yellow;
                GUI.Box(new Rect(x + 4, y + size - 10, (size - 8) * currentInteractionProgress, 6), "");
            }

            GUI.color = Color.white;
            GUI.skin.label.alignment = TextAnchor.MiddleCenter;
            GUI.Label(btnRect, $"HOLD\n{(currentInteractionProgress * 100):F0}%");
            GUI.skin.label.alignment = TextAnchor.UpperLeft;
        }
    }
}
