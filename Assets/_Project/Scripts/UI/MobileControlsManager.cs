using UnityEngine;
using Deadzone.Player;

namespace Deadzone.UI
{
    public class MobileControlsManager : MonoBehaviour
    {
        public static MobileControlsManager Instance { get; private set; }
        public static bool ForceMobileInEditor { get; set; } = false;
        public static bool IsMobileActive => Application.isMobilePlatform || ForceMobileInEditor;

        [SerializeField] private TouchShootingMode shootingMode = TouchShootingMode.ModeA_PushToFire;
        [SerializeField] private bool enableAimAssist = true;

        private PlayerController player;
        private MobileTouchInputProvider touchProvider;
        private int moveFingerId = -1, aimFingerId = -1;
        private Vector2 moveBasePos, moveKnobPos, aimBasePos, aimKnobPos;
        private Vector2 defaultMoveBase, defaultAimBase;
        private float moveRadius, aimRadius, btnRadius, fireRadius, contextRadius;
        private Vector2 fireCenter, dashCenter, reloadCenter, swapCenter, grenadeCenter, turretCenter, contextCenter;
        private Rect modeToggleRect;
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

        private void BindPlayer()
        {
            if (PlayerController.Instance != null)
            {
                player = PlayerController.Instance;
                touchProvider = GetOrCreateProvider(player.transform);
                if (IsMobileActive) player.SetInputProvider(touchProvider);
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
            moveRadius = h * 0.13f; aimRadius = h * 0.13f;
            btnRadius = h * 0.052f; fireRadius = h * 0.076f; contextRadius = h * 0.065f;

            defaultMoveBase = new Vector2(h * 0.26f, h * 0.74f);
            defaultAimBase = new Vector2(w - h * 0.28f, h * 0.74f);
            if (moveFingerId == -1) { moveBasePos = defaultMoveBase; moveKnobPos = defaultMoveBase; }
            if (aimFingerId == -1) { aimBasePos = defaultAimBase; aimKnobPos = defaultAimBase; }

            fireCenter = new Vector2(w - h * 0.48f, h * 0.68f);
            dashCenter = new Vector2(w - h * 0.12f, h * 0.88f);
            reloadCenter = new Vector2(w - h * 0.12f, h * 0.52f);
            swapCenter = new Vector2(w - h * 0.25f, h * 0.42f);
            grenadeCenter = new Vector2(w - h * 0.39f, h * 0.42f);
            turretCenter = new Vector2(w - h * 0.53f, h * 0.46f);
            contextCenter = new Vector2(w - h * 0.32f, h * 0.24f);
            modeToggleRect = new Rect(w - 190f, 15f, 175f, 36f);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1))
            {
                ForceMobileInEditor = !ForceMobileInEditor;
                if (player != null)
                {
                    if (ForceMobileInEditor) player.SetInputProvider(touchProvider);
                    else player.SetInputProvider(new DesktopInputProvider(player.transform));
                }
            }

            if (player == null && PlayerController.Instance != null) BindPlayer();
            if (touchProvider == null) return;

            RecalculateLayout();
            hasInteractableTarget = MobileContextScanner.Poll(player, out currentInteractionProgress, out interactActionName);
            ProcessTouchInput();
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

                    if (Vector2.Distance(p, fireCenter) <= fireRadius) fireHeld = true;
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
            if (modeToggleRect.Contains(p)) { SetShootingMode(shootingMode == TouchShootingMode.ModeA_PushToFire ? TouchShootingMode.ModeB_AimAndFire : TouchShootingMode.ModeA_PushToFire); return; }
            if (Vector2.Distance(p, dashCenter) <= btnRadius) { touchProvider.DashTriggered = true; return; }
            if (Vector2.Distance(p, reloadCenter) <= btnRadius) { touchProvider.ReloadTriggered = true; return; }
            if (Vector2.Distance(p, swapCenter) <= btnRadius) { touchProvider.SwitchWeaponTriggered = true; return; }
            if (Vector2.Distance(p, grenadeCenter) <= btnRadius) { touchProvider.GrenadeTriggered = true; return; }
            if (Vector2.Distance(p, turretCenter) <= btnRadius) { touchProvider.TurretTriggered = true; return; }
            if (hasInteractableTarget && Vector2.Distance(p, contextCenter) <= contextRadius) return;
            if (Vector2.Distance(p, fireCenter) <= fireRadius) return;

            if (p.x < Screen.width * 0.45f && p.y > Screen.height * 0.25f && moveFingerId == -1)
            {
                moveFingerId = fingerId; moveBasePos = p; moveKnobPos = p;
                touchProvider.MoveJoystickVector = Vector2.zero;
            }
            else if (p.x >= Screen.width * 0.45f && p.y > Screen.height * 0.30f && aimFingerId == -1)
            {
                aimFingerId = fingerId; aimBasePos = p; aimKnobPos = p;
                touchProvider.AimJoystickVector = Vector2.zero; touchProvider.IsAimPressed = true;
            }
        }

        private void HandleTouchMoved(int fingerId, Vector2 p)
        {
            if (fingerId == moveFingerId)
            {
                Vector2 d = p - moveBasePos;
                if (d.magnitude > moveRadius) d = d.normalized * moveRadius;
                moveKnobPos = moveBasePos + d;
                touchProvider.MoveJoystickVector = new Vector2(d.x / moveRadius, -d.y / moveRadius);
            }
            else if (fingerId == aimFingerId)
            {
                Vector2 d = p - aimBasePos;
                if (d.magnitude > aimRadius) d = d.normalized * aimRadius;
                aimKnobPos = aimBasePos + d;
                touchProvider.AimJoystickVector = new Vector2(d.x / aimRadius, -d.y / aimRadius);
            }
        }

        private void HandleTouchEnded(int fingerId)
        {
            if (fingerId == moveFingerId)
            {
                moveFingerId = -1; moveBasePos = defaultMoveBase; moveKnobPos = defaultMoveBase;
                touchProvider.MoveJoystickVector = Vector2.zero;
            }
            else if (fingerId == aimFingerId)
            {
                aimFingerId = -1; aimBasePos = defaultAimBase; aimKnobPos = defaultAimBase;
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
                isFireHeld = Vector2.Distance(mp, fireCenter) <= fireRadius;
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
            if (!IsMobileActive) return;
            MobileControlsDrawer.EnsureTextures();

            MobileControlsDrawer.DrawRing(moveBasePos, moveRadius, new Color(0.2f, 0.8f, 1f, 0.35f));
            MobileControlsDrawer.DrawCircle(moveKnobPos, moveRadius * 0.42f, new Color(0.3f, 0.9f, 1f, 0.75f));

            Color aimColor = (shootingMode == TouchShootingMode.ModeA_PushToFire && touchProvider != null && touchProvider.AimJoystickVector.magnitude >= 0.28f)
                ? new Color(1f, 0.35f, 0.1f, 0.85f) : new Color(0.9f, 0.7f, 0.2f, 0.45f);
            MobileControlsDrawer.DrawRing(aimBasePos, aimRadius, aimColor * 0.6f);
            MobileControlsDrawer.DrawCircle(aimKnobPos, aimRadius * 0.42f, aimColor);

            MobileControlsDrawer.DrawButton(fireCenter, fireRadius, new Color(1f, 0.25f, 0.25f, 0.85f), "FIRE", isFireHeld);
            MobileControlsDrawer.DrawButton(dashCenter, btnRadius, new Color(0.2f, 0.65f, 1f, 0.85f), "DASH", false);
            MobileControlsDrawer.DrawButton(reloadCenter, btnRadius, new Color(1f, 0.85f, 0.2f, 0.85f), "RELOAD", false);

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
                MobileControlsDrawer.DrawButton(contextCenter, contextRadius, new Color(0.2f, 1f, 0.4f, 0.9f), ctxText, isContextHeld);
            }

            string modeText = shootingMode == TouchShootingMode.ModeA_PushToFire ? "[AUTO-FIRE ON]" : "[MANUAL FIRE]";
            GUI.color = shootingMode == TouchShootingMode.ModeA_PushToFire ? new Color(0.2f, 1f, 0.5f, 0.9f) : new Color(1f, 0.7f, 0.2f, 0.9f);
            if (GUI.Button(modeToggleRect, modeText))
            {
                SetShootingMode(shootingMode == TouchShootingMode.ModeA_PushToFire ? TouchShootingMode.ModeB_AimAndFire : TouchShootingMode.ModeA_PushToFire);
            }
            GUI.color = Color.white;
        }
    }
}
