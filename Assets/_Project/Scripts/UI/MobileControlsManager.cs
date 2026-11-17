using UnityEngine;
using Deadzone.Player;
using Deadzone.Combat;

namespace Deadzone.UI
{
    public class MobileControlsManager : MonoBehaviour
    {
        public static MobileControlsManager Instance { get; private set; }
        public static bool ForceMobileInEditor { get; set; } = true;
        public static bool IsMobileActive => Application.isMobilePlatform || ForceMobileInEditor;

        [SerializeField] private TouchShootingMode shootingMode = TouchShootingMode.ModeA_PushToFire;
        [SerializeField] private bool enableAimAssist = true;

        private PlayerController player;
        private MobileTouchInputProvider touchProvider;
        private int moveFingerId = -1, aimFingerId = -1;
        private Vector2 defaultMoveBase, defaultAimBase, moveKnobPos, aimKnobPos;
        private float moveRadius, aimRadius, btnRadius, fireRadius, contextRadius;
        private Vector2 fireCenter, dashCenter, reloadCenter, swapCenter, grenadeCenter, turretCenter, contextCenter;
        private bool hasInteractableTarget, isFireHeld, isContextHeld;
        private float currentInteractionProgress;
        private string interactActionName = "USE";

        public TouchShootingMode CurrentMode => shootingMode;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) { Destroy(gameObject); return; }
            DontDestroyOnLoad(gameObject);
        }

        public MobileTouchInputProvider GetOrCreateProvider(Transform playerTransform)
        {
            if (touchProvider == null)
            {
                touchProvider = new MobileTouchInputProvider(playerTransform)
                {
                    ShootingMode = shootingMode,
                    EnableAimAssist = enableAimAssist
                };
            }
            return touchProvider;
        }

        private void Start()
        {
            RecalculateLayout();
            BindPlayer();
        }

        public void BindPlayer()
        {
            if (PlayerController.Instance != null)
            {
                player = PlayerController.Instance;
                touchProvider = GetOrCreateProvider(player.transform);
                if (IsMobileActive) player.SetInputProvider(touchProvider);
                else player.SetInputProvider(new DesktopInputProvider(player.transform));
            }
        }

        public void SetShootingMode(TouchShootingMode mode)
        {
            shootingMode = mode;
            if (touchProvider != null) touchProvider.ShootingMode = mode;
        }

        private void RecalculateLayout()
        {
            float w = Screen.width, h = Screen.height;
            moveRadius = h * 0.14f; aimRadius = h * 0.14f;
            btnRadius = h * 0.056f; fireRadius = h * 0.075f; contextRadius = h * 0.065f;

            defaultMoveBase = new Vector2(h * 0.22f, h * 0.74f);
            defaultAimBase = new Vector2(w - h * 0.25f, h * 0.74f);
            if (moveFingerId == -1) moveKnobPos = defaultMoveBase;
            if (aimFingerId == -1) aimKnobPos = defaultAimBase;

            fireCenter = new Vector2(w - h * 0.45f, h * 0.68f);
            dashCenter = new Vector2(w - h * 0.09f, h * 0.88f);
            reloadCenter = new Vector2(w - h * 0.09f, h * 0.52f);
            swapCenter = new Vector2(w - h * 0.20f, h * 0.42f);
            grenadeCenter = new Vector2(w - h * 0.33f, h * 0.42f);
            turretCenter = new Vector2(w - h * 0.45f, h * 0.50f);
            contextCenter = new Vector2(w - h * 0.25f, h * 0.24f);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1))
            {
                ForceMobileInEditor = !ForceMobileInEditor;
                BindPlayer();
            }

            if (player == null && PlayerController.Instance != null) BindPlayer();
            if (touchProvider == null) return;

            if (HUDController.IsMenuOpen)
            {
                ResetJoysticks();
                return;
            }

            RecalculateLayout();
            hasInteractableTarget = MobileContextScanner.Poll(player, out currentInteractionProgress, out interactActionName);
            ProcessTouchInput();
        }

        private void ResetJoysticks()
        {
            moveFingerId = -1; aimFingerId = -1;
            moveKnobPos = defaultMoveBase; aimKnobPos = defaultAimBase;
            isFireHeld = false; isContextHeld = false;
            if (touchProvider != null)
            {
                touchProvider.MoveJoystickVector = Vector2.zero; touchProvider.AimJoystickVector = Vector2.zero;
                touchProvider.IsAimPressed = false; touchProvider.FireButtonPressed = false; touchProvider.ContextInteractHeld = false;
            }
        }

        private void ProcessTouchInput()
        {
            if (Input.touchCount > 0)
            {
                bool fireHeld = false, ctxHeld = false;
                for (int i = 0; i < Input.touchCount; i++)
                {
                    Touch t = Input.GetTouch(i);
                    Vector2 p = new Vector2(t.position.x, Screen.height - t.position.y);
                    if (t.phase == TouchPhase.Began) HandleTouchBegan(t.fingerId, p);
                    else if (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary) HandleTouchMoved(t.fingerId, p);
                    else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) HandleTouchEnded(t.fingerId);

                    if (shootingMode == TouchShootingMode.ModeB_AimAndFire && Vector2.Distance(p, fireCenter) <= fireRadius) fireHeld = true;
                    if (hasInteractableTarget && Vector2.Distance(p, contextCenter) <= contextRadius) ctxHeld = true;
                }
                isFireHeld = fireHeld; isContextHeld = ctxHeld;
            }
            else if (ForceMobileInEditor)
            {
                ProcessMouseFallback();
            }

            touchProvider.FireButtonPressed = isFireHeld;
            touchProvider.ContextInteractHeld = isContextHeld;
        }

        private void HandleTouchBegan(int fingerId, Vector2 p)
        {
            if (HUDController.IsMenuOpen) return;

            if (Vector2.Distance(p, dashCenter) <= btnRadius) { touchProvider.DashTriggered = true; return; }
            if (Vector2.Distance(p, reloadCenter) <= btnRadius) { touchProvider.ReloadTriggered = true; return; }
            if (Vector2.Distance(p, swapCenter) <= btnRadius) { touchProvider.SwitchWeaponTriggered = true; return; }
            if (Vector2.Distance(p, grenadeCenter) <= btnRadius) { touchProvider.GrenadeTriggered = true; return; }
            if (Vector2.Distance(p, turretCenter) <= btnRadius) { touchProvider.TurretTriggered = true; return; }
            if (hasInteractableTarget && Vector2.Distance(p, contextCenter) <= contextRadius) return;
            if (shootingMode == TouchShootingMode.ModeB_AimAndFire && Vector2.Distance(p, fireCenter) <= fireRadius) return;

            // Fixed base: only activate if touching near the designated corner bases!
            if (Vector2.Distance(p, defaultMoveBase) <= moveRadius * 1.5f && moveFingerId == -1)
            {
                moveFingerId = fingerId;
                HandleTouchMoved(fingerId, p);
            }
            else if (Vector2.Distance(p, defaultAimBase) <= aimRadius * 1.5f && aimFingerId == -1)
            {
                aimFingerId = fingerId;
                touchProvider.IsAimPressed = true;
                HandleTouchMoved(fingerId, p);
            }
        }

        private void HandleTouchMoved(int fingerId, Vector2 p)
        {
            if (fingerId == moveFingerId)
            {
                Vector2 d = p - defaultMoveBase;
                if (d.magnitude > moveRadius) d = d.normalized * moveRadius;
                moveKnobPos = defaultMoveBase + d;
                touchProvider.MoveJoystickVector = new Vector2(d.x / moveRadius, -d.y / moveRadius);
            }
            else if (fingerId == aimFingerId)
            {
                Vector2 d = p - defaultAimBase;
                if (d.magnitude > aimRadius) d = d.normalized * aimRadius;
                aimKnobPos = defaultAimBase + d;
                touchProvider.AimJoystickVector = new Vector2(d.x / aimRadius, -d.y / aimRadius);
            }
        }

        private void HandleTouchEnded(int fingerId)
        {
            if (fingerId == moveFingerId)
            {
                moveFingerId = -1; moveKnobPos = defaultMoveBase;
                touchProvider.MoveJoystickVector = Vector2.zero;
            }
            else if (fingerId == aimFingerId)
            {
                aimFingerId = -1; aimKnobPos = defaultAimBase;
                touchProvider.AimJoystickVector = Vector2.zero; touchProvider.IsAimPressed = false;
            }
        }

        private void ProcessMouseFallback()
        {
            Vector2 mp = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            if (Input.GetMouseButtonDown(0)) HandleTouchBegan(99, mp);
            else if (Input.GetMouseButton(0))
            {
                HandleTouchMoved(99, mp);
                isFireHeld = shootingMode == TouchShootingMode.ModeB_AimAndFire && Vector2.Distance(mp, fireCenter) <= fireRadius;
                isContextHeld = hasInteractableTarget && Vector2.Distance(mp, contextCenter) <= contextRadius;
            }
            else if (Input.GetMouseButtonUp(0))
            {
                HandleTouchEnded(99);
                isFireHeld = false; isContextHeld = false;
            }
        }

        private void OnGUI()
        {
            if (Application.isEditor)
            {
                string toggleText = ForceMobileInEditor ? "VIEW: MOBILE UI [ON]" : "VIEW: DESKTOP PC";
                GUI.color = ForceMobileInEditor ? new Color(0.2f, 1f, 0.5f, 0.85f) : new Color(0.8f, 0.8f, 0.8f, 0.85f);
                if (GUI.Button(new Rect(Screen.width * 0.5f - 85f, 6f, 170f, 24f), toggleText))
                {
                    ForceMobileInEditor = !ForceMobileInEditor;
                    BindPlayer();
                }
                GUI.color = Color.white;
            }

            if (!IsMobileActive || HUDController.IsMenuOpen) return;
            MobileControlsDrawer.EnsureTextures();

            // Left Move Joystick (Fixed at bottom-left corner)
            MobileControlsDrawer.DrawCircle(defaultMoveBase, moveRadius, new Color(0.06f, 0.10f, 0.16f, 0.65f));
            MobileControlsDrawer.DrawRing(defaultMoveBase, moveRadius, new Color(0.2f, 0.8f, 1f, 0.55f));
            MobileControlsDrawer.DrawCircle(moveKnobPos, moveRadius * 0.40f, new Color(0.3f, 0.9f, 1f, 0.85f));

            // Right Aim Joystick (Fixed at bottom-right corner)
            bool isFiring = touchProvider != null && touchProvider.AimJoystickVector.magnitude >= 0.20f;
            Color aimRingColor = isFiring ? new Color(1f, 0.35f, 0.1f, 0.95f) : new Color(0.9f, 0.7f, 0.2f, 0.55f);
            Color aimKnobColor = isFiring ? new Color(1f, 0.35f, 0.1f, 0.95f) : new Color(0.9f, 0.75f, 0.25f, 0.85f);
            MobileControlsDrawer.DrawCircle(defaultAimBase, aimRadius, new Color(0.06f, 0.10f, 0.16f, 0.65f));
            MobileControlsDrawer.DrawRing(defaultAimBase, aimRadius, aimRingColor);
            MobileControlsDrawer.DrawCircle(aimKnobPos, aimRadius * 0.40f, aimKnobColor);

            if (shootingMode == TouchShootingMode.ModeB_AimAndFire)
            {
                MobileControlsDrawer.DrawButton(fireCenter, fireRadius, new Color(1f, 0.25f, 0.25f, 0.9f), "FIRE", isFireHeld);
            }

            MobileControlsDrawer.DrawButton(dashCenter, btnRadius, new Color(0.2f, 0.65f, 1f, 0.85f), "DASH", false);

            var weapon = player != null ? player.GetComponentInChildren<Weapon>() : null;
            string relText = (weapon != null && weapon.IsReloading) ? "REL..." : (weapon != null ? $"REL\n{weapon.CurrentAmmo}" : "RELOAD");
            MobileControlsDrawer.DrawButton(reloadCenter, btnRadius, new Color(1f, 0.85f, 0.2f, 0.85f), relText, false);

            int slotNum = player != null ? player.ActiveSlotIndex + 1 : 1;
            MobileControlsDrawer.DrawButton(swapCenter, btnRadius, new Color(0.7f, 0.35f, 0.95f, 0.85f), $"SWAP\nS{slotNum}", false);

            var pa = PlayerAbilities.Instance;
            string gText = (pa != null && !pa.IsGrenadeReady) ? $"{pa.GrenadeRemainingTime:F1}s" : "NADE";
            MobileControlsDrawer.DrawButton(grenadeCenter, btnRadius, new Color(1f, 0.55f, 0.1f, 0.85f), gText, false);

            string tText = (pa != null && !pa.IsTurretReady) ? $"{pa.TurretRemainingTime:F1}s" : "TURRET";
            MobileControlsDrawer.DrawButton(turretCenter, btnRadius, new Color(0.1f, 0.95f, 0.85f, 0.85f), tText, false);

            if (hasInteractableTarget)
            {
                string ctxText = $"HOLD\n{interactActionName}\n{(currentInteractionProgress * 100):F0}%";
                MobileControlsDrawer.DrawButton(contextCenter, contextRadius, new Color(0.2f, 1f, 0.4f, 0.95f), ctxText, isContextHeld);
            }
        }
    }
}
